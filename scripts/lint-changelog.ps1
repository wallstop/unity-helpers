Param(
  [switch]$VerboseOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info($msg) {
  if ($VerboseOutput) { Write-Host "[changelog-lint] $msg" -ForegroundColor Cyan }
}

function Write-WarningMsg($msg) {
  Write-Host "[changelog-lint] WARNING: $msg" -ForegroundColor Yellow
}

function Write-ErrorMsg($msg) {
  Write-Host "[changelog-lint] ERROR: $msg" -ForegroundColor Red
}

function Write-SuccessMsg($msg) {
  Write-Host "[changelog-lint] $msg" -ForegroundColor Green
}

# Standard Keep a Changelog change type headers
$validChangeTypes = @(
  'Added',
  'Changed',
  'Deprecated',
  'Removed',
  'Fixed',
  'Security',
  'Improved'  # Common extension
)

Write-Info "Starting CHANGELOG.md validation..."

$repoRoot = (Get-Item $PSScriptRoot).Parent.FullName
$changelogPath = Join-Path -Path $repoRoot -ChildPath 'CHANGELOG.md'

$errorList = @()
$warningList = @()

# Check if CHANGELOG.md exists
if (-not (Test-Path $changelogPath)) {
  Write-ErrorMsg "CHANGELOG.md not found at: $changelogPath"
  exit 1
}

Write-Info "Found CHANGELOG.md at: $changelogPath"

$content = Get-Content -Path $changelogPath -Raw
$lines = Get-Content -Path $changelogPath

# Check for title header
if ($lines.Count -gt 0 -and $lines[0] -notmatch '^#\s+Changelog') {
  $warningList += "Line 1: Expected '# Changelog' as the first line"
}

# Track sections found
$hasUnreleased = $false
$unreleasedLineNumber = -1
$unreleasedHasSubsections = $false
$versionHeaders = @()
$changeTypeHeaders = @()
$currentSection = $null
$currentSectionLine = 0

for ($i = 0; $i -lt $lines.Count; $i++) {
  $line = $lines[$i]
  $lineNumber = $i + 1

  # Check for [Unreleased] section
  if ($line -match '^##\s+\[Unreleased\]\s*$') {
    $hasUnreleased = $true
    $unreleasedLineNumber = $lineNumber
    $currentSection = 'Unreleased'
    $currentSectionLine = $lineNumber
    Write-Info "Found [Unreleased] section at line $lineNumber"
    continue
  }

  # Check for version headers: ## [X.Y.Z] - YYYY-MM-DD or ## [X.Y.Z]
  if ($line -match '^##\s+\[(\d+\.\d+\.\d+)\]') {
    $version = $Matches[1]
    
    # Validate date format if present
    if ($line -match '^##\s+\[\d+\.\d+\.\d+\]\s+-\s+(\d{4}-\d{2}-\d{2})\s*$') {
      $dateStr = $Matches[1]
      # Validate date is reasonable (not future, not too old)
      try {
        $date = [DateTime]::ParseExact($dateStr, 'yyyy-MM-dd', $null)
        Write-Info "Line $lineNumber`: Found version [$version] with date $dateStr"
      }
      catch {
        $errorList += "Line $lineNumber`: Invalid date format '$dateStr' - expected YYYY-MM-DD"
      }
    }
    elseif ($line -match '^##\s+\[\d+\.\d+\.\d+\]\s*$') {
      # Version without date - just a warning since some formats allow this
      $warningList += "Line $lineNumber`: Version [$version] has no date - recommended format is '## [$version] - YYYY-MM-DD'"
      Write-Info "Line $lineNumber`: Found version [$version] (no date)"
    }
    elseif ($line -match '^##\s+\[\d+\.\d+\.\d+\]') {
      # Version with malformed date part
      $errorList += "Line $lineNumber`: Malformed version header - expected '## [$version] - YYYY-MM-DD' or '## [$version]'"
    }

    $versionHeaders += @{
      Version = $version
      Line = $lineNumber
    }
    $currentSection = $version
    $currentSectionLine = $lineNumber
    continue
  }

  # Check for change type headers (### Added, ### Changed, etc.)
  if ($line -match '^###\s+(\w+)\s*$') {
    $changeType = $Matches[1]
    
    # Mark that Unreleased has content if we're in that section
    if ($currentSection -eq 'Unreleased') {
      $unreleasedHasSubsections = $true
    }

    if ($changeType -notin $validChangeTypes) {
      $warningList += "Line $lineNumber`: Non-standard change type '### $changeType' - standard types are: $($validChangeTypes -join ', ')"
    }
    else {
      Write-Info "Line $lineNumber`: Found change type '### $changeType'"
    }

    $changeTypeHeaders += @{
      Type = $changeType
      Line = $lineNumber
      Section = $currentSection
    }
    continue
  }

  # Check for malformed section headers
  if ($line -match '^##[^\s]' -or $line -match '^###[^\s]') {
    $errorList += "Line $lineNumber`: Malformed header - missing space after ## or ###"
  }

  # Check for version headers without brackets
  if ($line -match '^##\s+\d+\.\d+\.\d+') {
    $warningList += "Line $lineNumber`: Version should be in brackets, e.g., '## [1.0.0] - YYYY-MM-DD'"
  }
}

# Validate [Unreleased] section exists and is at the top
if (-not $hasUnreleased) {
  $errorList += "Missing required '## [Unreleased]' section"
}
else {
  # Check that [Unreleased] comes before any version headers
  foreach ($vh in $versionHeaders) {
    if ($vh.Line -lt $unreleasedLineNumber) {
      $errorList += "Line $($vh.Line): Version [$($vh.Version)] appears before [Unreleased] section (line $unreleasedLineNumber)"
    }
  }

  # Warn if [Unreleased] section is empty
  if (-not $unreleasedHasSubsections) {
    $warningList += "Line $unreleasedLineNumber`: [Unreleased] section has no change type subsections (e.g., ### Added)"
  }
}

# Check for duplicate versions
$versionNames = $versionHeaders | ForEach-Object { $_.Version }
$duplicateVersions = $versionNames | Group-Object | Where-Object { $_.Count -gt 1 }
foreach ($dup in $duplicateVersions) {
  $dupLines = ($versionHeaders | Where-Object { $_.Version -eq $dup.Name } | ForEach-Object { $_.Line }) -join ', '
  $errorList += "Duplicate version [$($dup.Name)] found at lines: $dupLines"
}

# Check version ordering (should be descending - newest first)
$previousVersion = $null
foreach ($vh in $versionHeaders) {
  if ($null -ne $previousVersion) {
    try {
      $prev = [Version]$previousVersion.Version
      $curr = [Version]$vh.Version
      if ($curr -gt $prev) {
        $warningList += "Line $($vh.Line): Version [$($vh.Version)] is newer than [$($previousVersion.Version)] (line $($previousVersion.Line)) - versions should be in descending order"
      }
    }
    catch {
      # Skip version comparison if parsing fails
    }
  }
  $previousVersion = $vh
}

# Summary
Write-Host ""
Write-Host "=" * 60

if ($warningList.Count -gt 0) {
  Write-Host ""
  Write-WarningMsg "Found $($warningList.Count) warning(s):"
  foreach ($warning in $warningList) {
    Write-Host "  - $warning" -ForegroundColor Yellow
  }
}

if ($errorList.Count -gt 0) {
  Write-Host ""
  Write-ErrorMsg "Found $($errorList.Count) error(s):"
  foreach ($error in $errorList) {
    Write-Host "  - $error" -ForegroundColor Red
  }
  Write-Host ""
  Write-Host "=" * 60
  Write-ErrorMsg "CHANGELOG.md validation FAILED"
  exit 1
}

Write-Host ""
Write-SuccessMsg "CHANGELOG.md validation passed!"
Write-Info "Checked: $($lines.Count) lines, $($versionHeaders.Count) version(s), $($changeTypeHeaders.Count) change type header(s)"
Write-Host "=" * 60
exit 0
