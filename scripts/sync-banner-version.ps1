<#
.SYNOPSIS
    Syncs the version from package.json to the SVG banner.
.DESCRIPTION
    Reads the version from package.json and updates the version badge in the
    unity-helpers-banner.svg file. Automatically stages the SVG if modified.
.PARAMETER StagedOnly
    Only run if package.json is staged for commit.
#>
[CmdletBinding()]
param(
    [switch]$StagedOnly
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Load shared git helpers for safe index operations
$helpersPath = Join-Path -Path $PSScriptRoot -ChildPath 'git-staging-helpers.ps1'
. $helpersPath

$repoRoot = Split-Path -Parent $PSScriptRoot
$packageJsonPath = Join-Path $repoRoot 'package.json'
$bannerSvgPath = Join-Path $repoRoot 'docs/images/unity-helpers-banner.svg'

# Get repository info for lock handling
try {
    Assert-GitAvailable | Out-Null
    $repositoryInfo = Get-GitRepositoryInfo
} catch {
    Write-Error $_.Exception.Message
    exit 1
}

# Check if we should run
if ($StagedOnly) {
    $stagedFiles = git diff --cached --name-only 2>$null
    if ($stagedFiles -notcontains 'package.json') {
        Write-Host 'package.json not staged, skipping banner version sync.'
        exit 0
    }
}

# Verify files exist
if (-not (Test-Path $packageJsonPath)) {
    Write-Error "package.json not found at: $packageJsonPath"
    exit 1
}

if (-not (Test-Path $bannerSvgPath)) {
    Write-Host "Banner SVG not found at: $bannerSvgPath, skipping."
    exit 0
}

# Read version from package.json
$packageJson = Get-Content $packageJsonPath -Raw | ConvertFrom-Json
$version = $packageJson.version

if ([string]::IsNullOrWhiteSpace($version)) {
    Write-Error 'Could not read version from package.json'
    exit 1
}

Write-Host "Found version: v$version"

# Read SVG content
$svgContent = Get-Content $bannerSvgPath -Raw

# Pattern to match the version text in the SVG
# Matches: >v1.2.3</text> or >v1.2.3-beta</text>
$versionPattern = '>v[\d]+\.[\d]+\.[\d]+[^<]*</text>'
$newVersionText = ">v$version</text>"

if ($svgContent -match $versionPattern) {
    $currentMatch = $Matches[0]
    if ($currentMatch -eq $newVersionText) {
        Write-Host "Banner already has correct version: v$version"
        exit 0
    }

    # Replace version
    $updatedContent = $svgContent -replace $versionPattern, $newVersionText
    Set-Content -Path $bannerSvgPath -Value $updatedContent -NoNewline -Encoding UTF8

    Write-Host "Updated banner version to: v$version"

    # Stage the modified SVG using safe retry helper
    $gitAddExitCode = Invoke-GitAddWithRetry -Items @($bannerSvgPath) -IndexLockPath $repositoryInfo.IndexLockPath
    if ($gitAddExitCode -ne 0) {
        Write-Error "git add failed with exit code $gitAddExitCode."
        exit $gitAddExitCode
    }
    Write-Host "Staged: $bannerSvgPath"
}
else {
    Write-Host 'No version pattern found in SVG, skipping.'
}

exit 0
