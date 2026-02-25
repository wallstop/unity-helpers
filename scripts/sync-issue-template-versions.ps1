<#
.SYNOPSIS
    Syncs package versions into GitHub issue template dropdowns.
.DESCRIPTION
    Collects versions from package.json, CHANGELOG.md, git tags, and an
    optional AdditionalVersions parameter.  Deduplicates, sorts descending
    by semantic version, and writes the options list between sentinel
    comments in bug_report.yml and feature_request.yml.
    Automatically stages modified files.
.PARAMETER AdditionalVersions
    Comma-separated list of additional version strings to include (e.g., from
    GitHub Releases API in CI).
#>
[CmdletBinding()]
param(
    [string]$AdditionalVersions = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Load shared git helpers for safe index operations
$helpersPath = Join-Path -Path $PSScriptRoot -ChildPath 'git-staging-helpers.ps1'
. $helpersPath

$repoRoot = Split-Path -Parent $PSScriptRoot
$packageJsonPath = Join-Path $repoRoot 'package.json'
$changelogPath = Join-Path $repoRoot 'CHANGELOG.md'

$templateDir = Join-Path $repoRoot '.github' 'ISSUE_TEMPLATE'
$templateFiles = @(
    (Join-Path $templateDir 'bug_report.yml'),
    (Join-Path $templateDir 'feature_request.yml')
)

$sentinelStart = '# <!-- AUTO-UPDATED: package-versions -->'
$sentinelEnd = '# <!-- END AUTO-UPDATED: package-versions -->'

# Valid semver-like pattern: exactly digits.digits.digits (no prerelease suffix)
$semverPattern = '^\d+\.\d+\.\d+$'

# ── Git availability & lock handling ────────────────────────────────────────

$repositoryInfo = $null
$gitAvailable = $false
try {
    Assert-GitAvailable | Out-Null
    $repositoryInfo = Get-GitRepositoryInfo
    $gitAvailable = $true
} catch {
    Write-Warning "Git is not available or not in a repository: $($_.Exception.Message)"
    Write-Warning "Continuing without git tags and without staging."
}

if ($gitAvailable) {
    if (-not (Invoke-EnsureNoIndexLock)) {
        Write-Warning "index.lock still held after waiting. Proceeding anyway, but operations may fail."
    }
}

# ── Collect versions ────────────────────────────────────────────────────────

$versions = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase
)

# 1. package.json
if (Test-Path $packageJsonPath) {
    try {
        $packageJson = Get-Content $packageJsonPath -Raw | ConvertFrom-Json
        $pkgVersion = $packageJson.version
        if (-not [string]::IsNullOrWhiteSpace($pkgVersion)) {
            $pkgVersion = $pkgVersion.Trim().TrimStart('v', 'V')
            if ($pkgVersion -match $semverPattern) {
                [void]$versions.Add($pkgVersion)
                Write-Host "package.json version: $pkgVersion"
            }
        }
    } catch {
        Write-Warning "Failed to parse package.json: $($_.Exception.Message)"
    }
} else {
    Write-Warning "package.json not found at: $packageJsonPath"
}

# 2. CHANGELOG.md
if (Test-Path $changelogPath) {
    try {
        $changelogLines = Get-Content $changelogPath -Encoding UTF8
        foreach ($line in $changelogLines) {
            if ($line -match '^## \[(\d+\.\d+\.\d+)') {
                [void]$versions.Add($Matches[1])
            }
        }
        Write-Host "CHANGELOG.md: found versions from changelog entries."
    } catch {
        Write-Warning "Failed to read CHANGELOG.md: $($_.Exception.Message)"
    }
} else {
    Write-Warning "CHANGELOG.md not found at: $changelogPath"
}

# 3. Git tags
if ($gitAvailable) {
    try {
        $tags = & git tag --list 2>$null
        if ($LASTEXITCODE -eq 0 -and $tags) {
            foreach ($tag in $tags) {
                $cleaned = $tag.Trim().TrimStart('v', 'V')
                if ($cleaned -match $semverPattern) {
                    [void]$versions.Add($cleaned)
                }
            }
            Write-Host "git tags: processed $($tags.Count) tag(s)."
        }
    } catch {
        Write-Warning "Failed to list git tags: $($_.Exception.Message)"
    }
}

# 4. AdditionalVersions parameter
if (-not [string]::IsNullOrWhiteSpace($AdditionalVersions)) {
    foreach ($entry in $AdditionalVersions.Split(',')) {
        $cleaned = $entry.Trim().TrimStart('v', 'V')
        if (-not [string]::IsNullOrWhiteSpace($cleaned) -and $cleaned -match $semverPattern) {
            [void]$versions.Add($cleaned)
        }
    }
    Write-Host "AdditionalVersions: processed parameter."
}

# ── Guard: nothing collected ────────────────────────────────────────────────

if ($versions.Count -eq 0) {
    Write-Error "No versions collected from any source. Cannot update templates."
    exit 1
}

Write-Host "Total unique versions collected: $($versions.Count)"

# ── Sort descending by System.Version ───────────────────────────────────────

$parseable = [System.Collections.Generic.List[System.Version]]::new()
$unparseable = [System.Collections.Generic.List[string]]::new()

foreach ($v in $versions) {
    try {
        $parsed = [System.Version]::new($v)
        [void]$parseable.Add($parsed)
    } catch {
        [void]$unparseable.Add($v)
    }
}

$parseable.Sort()
$parseable.Reverse()

$unparseable.Sort()
$unparseable.Reverse()

$sortedVersions = [System.Collections.Generic.List[string]]::new()
foreach ($sv in $parseable) {
    [void]$sortedVersions.Add($sv.ToString())
}
foreach ($uv in $unparseable) {
    [void]$sortedVersions.Add($uv)
}

Write-Host "Versions (sorted): $($sortedVersions -join ', ')"

# ── Update template files ──────────────────────────────────────────────────

$filesToStage = [System.Collections.Generic.List[string]]::new()

foreach ($templatePath in $templateFiles) {
    $templateName = Split-Path -Leaf $templatePath
    if (-not (Test-Path $templatePath)) {
        Write-Warning "Template file not found, skipping: $templatePath"
        continue
    }

    # Read raw content preserving line endings
    $rawContent = [System.IO.File]::ReadAllText($templatePath)

    # Normalise to LF for consistent processing
    $content = $rawContent -replace "`r`n", "`n"

    # Locate sentinel comments
    $startIdx = $content.IndexOf($sentinelStart)
    $endIdx = $content.IndexOf($sentinelEnd)

    if ($startIdx -lt 0 -or $endIdx -lt 0) {
        Write-Warning "Sentinel comments not found in $templateName, skipping."
        continue
    }

    if ($endIdx -le $startIdx) {
        Write-Warning "Sentinel comments are misordered in $templateName, skipping."
        continue
    }

    # Find the start-of-line for the opening sentinel
    $lineStart = $content.LastIndexOf("`n", $startIdx)
    if ($lineStart -lt 0) {
        $lineStart = 0
    } else {
        $lineStart += 1  # skip the newline character itself
    }

    # Detect indentation from existing sentinel line
    $detectedIndent = $content.Substring($lineStart, $startIdx - $lineStart)

    # Build options block using detected indent
    $optionLines = [System.Collections.Generic.List[string]]::new()
    [void]$optionLines.Add("$detectedIndent$sentinelStart")
    foreach ($ver in $sortedVersions) {
        [void]$optionLines.Add("$detectedIndent- `"$ver`"")
    }
    [void]$optionLines.Add("$detectedIndent- `"Other`"")
    [void]$optionLines.Add("$detectedIndent$sentinelEnd")

    $optionsBlock = $optionLines -join "`n"

    # Find the end-of-line for the closing sentinel
    $lineEnd = $content.IndexOf("`n", $endIdx)
    if ($lineEnd -lt 0) {
        $lineEnd = $content.Length
    }

    $before = $content.Substring(0, $lineStart)
    $after = $content.Substring($lineEnd)

    $updatedContent = $before + $optionsBlock + $after

    # Check if anything actually changed (case-sensitive to catch casing fixes)
    if ($updatedContent -ceq $content) {
        Write-Host "${templateName}: already up-to-date."
        continue
    }

    # Write with LF line endings (no BOM)
    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($templatePath, $updatedContent, $utf8NoBom)

    Write-Host "${templateName}: updated version dropdown."
    [void]$filesToStage.Add($templatePath)
}

# ── Stage changed files ─────────────────────────────────────────────────────

if ($filesToStage.Count -gt 0 -and $gitAvailable) {
    $gitAddExitCode = Invoke-GitAddWithRetry -Items $filesToStage.ToArray() -IndexLockPath $repositoryInfo.IndexLockPath
    if ($gitAddExitCode -ne 0) {
        Write-Error "git add failed with exit code $gitAddExitCode."
        exit $gitAddExitCode
    }
    Write-Host "Staged: $($filesToStage -join ', ')"
} elseif ($filesToStage.Count -eq 0) {
    Write-Host "No template files were modified."
}

exit 0
