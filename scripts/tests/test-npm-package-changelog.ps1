Param(
  [switch]$VerboseOutput
)

<#
.SYNOPSIS
    Test runner for UPM inline changelog rendering contract validation

.DESCRIPTION
    Validates that the Unity package is correctly configured for the Unity
    Package Manager to RENDER the changelog inline (not just link to it).

    Unity's UPM renders CHANGELOG.md inline only when:
      1. CHANGELOG.md exists at the package root (next to package.json)
      2. CHANGELOG.md.meta companion exists (Unity asset tracking)
      3. changelogUrl is NOT present in package.json (its presence causes
         UPM to show an external link instead of inline rendering)
      4. CHANGELOG.md is included in the npm package (not excluded by .npmignore)
      5. CHANGELOG.md has a version entry matching the current package version

    References:
      - https://docs.unity3d.com/6000.3/Documentation/Manual/cus-changelog.html
      - https://docs.unity3d.com/6000.0/Documentation/Manual/cus-layout.html
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:TestsPassed = 0
$script:TestsFailed = 0

function Write-Info($msg) {
  if ($VerboseOutput) { Write-Host "[test-npm-package-changelog] $msg" -ForegroundColor Cyan }
}

function Write-TestResult {
  param(
    [string]$TestName,
    [bool]$Passed,
    [string]$Message = ""
  )

  if ($Passed) {
    Write-Host "  [PASS] $TestName" -ForegroundColor Green
    $script:TestsPassed++
  }
  else {
    Write-Host "  [FAIL] $TestName" -ForegroundColor Red
    if ($Message) {
      Write-Host "         $Message" -ForegroundColor Yellow
    }
    $script:TestsFailed++
  }
}

Write-Host "Testing UPM inline changelog rendering contract..." -ForegroundColor White

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$packageJsonPath = Join-Path $repoRoot 'package.json'
$changelogPath = Join-Path $repoRoot 'CHANGELOG.md'
$changelogMetaPath = Join-Path $repoRoot 'CHANGELOG.md.meta'
$npmignorePath = Join-Path $repoRoot '.npmignore'
Write-Info "Repo root: $repoRoot"

# ── Section: Required files ──────────────────────────────────────────────────

Write-Host ""
Write-Host "  Section: Required files" -ForegroundColor White

$packageJsonExists = Test-Path $packageJsonPath
Write-TestResult -TestName 'package.json exists' -Passed $packageJsonExists -Message "Missing: $packageJsonPath"

$changelogExists = Test-Path $changelogPath
Write-TestResult -TestName 'CHANGELOG.md exists at package root' -Passed $changelogExists -Message "Missing: $changelogPath"

$changelogMetaExists = Test-Path $changelogMetaPath
Write-TestResult -TestName 'CHANGELOG.md.meta companion exists' -Passed $changelogMetaExists -Message "Missing: $changelogMetaPath (required for Unity asset tracking)"

# ── Section: package.json contract ───────────────────────────────────────────

Write-Host ""
Write-Host "  Section: package.json contract" -ForegroundColor White

$rawContent = ''
$parsedJson = $null
$jsonParseSucceeded = $false

if ($packageJsonExists) {
  $rawContent = Get-Content -Path $packageJsonPath -Raw
  try {
    $parsedJson = $rawContent | ConvertFrom-Json
    $jsonParseSucceeded = $true
  }
  catch {
    $jsonParseSucceeded = $false
  }
}

Write-TestResult -TestName 'package.json is valid JSON' -Passed $jsonParseSucceeded

$versionValue = ''

if ($jsonParseSucceeded) {
  # changelogUrl must NOT be present - it causes UPM to show an external link
  # instead of rendering the local CHANGELOG.md inline
  $hasChangelogUrl = $null -ne $parsedJson.PSObject.Properties['changelogUrl']
  Write-TestResult -TestName 'changelogUrl is absent (required for inline rendering)' -Passed (-not $hasChangelogUrl) -Message "Remove changelogUrl from package.json; it overrides inline CHANGELOG.md rendering"

  $hasVersion = $null -ne $parsedJson.PSObject.Properties['version']
  Write-TestResult -TestName 'package.json contains version field' -Passed $hasVersion

  if ($hasVersion) {
    $versionValue = [string]$parsedJson.version
    Write-TestResult -TestName 'version field is non-empty' -Passed (-not [string]::IsNullOrWhiteSpace($versionValue))
  }
}

# Also check raw content for stray changelogUrl keys (catches duplicate/commented-out entries)
$changelogKeyMatches = [regex]::Matches($rawContent, '"changelogUrl"\s*:')
Write-TestResult -TestName 'no changelogUrl keys in package.json raw content' -Passed ($changelogKeyMatches.Count -eq 0) -Message "Found $($changelogKeyMatches.Count) changelogUrl key(s) in raw content"

# ── Section: npm pack inclusion ──────────────────────────────────────────────

Write-Host ""
Write-Host "  Section: npm pack inclusion" -ForegroundColor White

if (Test-Path $npmignorePath) {
  $npmignoreContent = Get-Content -Path $npmignorePath -Raw
  # Check that CHANGELOG.md is not excluded by .npmignore
  # Look for patterns that would exclude it: /CHANGELOG.md, CHANGELOG.md, CHANGELOG*, *.md at root
  $excludesChangelog = $npmignoreContent -match '(?m)^/?CHANGELOG\.md\s*$'
  $excludesChangelogMeta = $npmignoreContent -match '(?m)^/?CHANGELOG\.md\.meta\s*$'
  Write-TestResult -TestName 'CHANGELOG.md not excluded by .npmignore' -Passed (-not $excludesChangelog) -Message ".npmignore explicitly excludes CHANGELOG.md"
  Write-TestResult -TestName 'CHANGELOG.md.meta not excluded by .npmignore' -Passed (-not $excludesChangelogMeta) -Message ".npmignore explicitly excludes CHANGELOG.md.meta"
}
else {
  Write-Info "No .npmignore found (all files included by default)"
  Write-TestResult -TestName 'CHANGELOG.md not excluded by .npmignore' -Passed $true
  Write-TestResult -TestName 'CHANGELOG.md.meta not excluded by .npmignore' -Passed $true
}

# ── Section: CHANGELOG.md content ────────────────────────────────────────────

Write-Host ""
Write-Host "  Section: CHANGELOG.md content" -ForegroundColor White

if ($changelogExists -and (-not [string]::IsNullOrWhiteSpace($versionValue))) {
  $changelogRaw = Get-Content -Path $changelogPath -Raw
  $hasVersionHeader = $changelogRaw -match ("(?mi)^##\s+\[" + [regex]::Escape($versionValue) + "\](?:\s+-.*)?\s*$")
  $hasUnreleasedHeader = $changelogRaw -match '(?m)^##\s+\[Unreleased\]\s*$'
  Write-TestResult -TestName 'CHANGELOG.md has current version section or [Unreleased]' -Passed ($hasVersionHeader -or $hasUnreleasedHeader) -Message "Expected section: ## [$versionValue] or ## [Unreleased]"

  # Verify the changelog starts with a heading (Unity parser expects this)
  $lines = Get-Content -Path $changelogPath
  $startsWithHeading = $false
  if ($lines.Count -gt 0) {
    $startsWithHeading = $lines[0] -match '^#\s+'
  }
  Write-TestResult -TestName 'CHANGELOG.md starts with markdown heading' -Passed $startsWithHeading -Message "First line should be a markdown heading (e.g., '# Changelog')"
}

Write-Host ""
Write-Host "Results:" -ForegroundColor Magenta
Write-Host "  Passed: $script:TestsPassed"
Write-Host "  Failed: $script:TestsFailed"

if ($script:TestsFailed -gt 0) {
  exit 1
}

exit 0
