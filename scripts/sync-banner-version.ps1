<#
.SYNOPSIS
    Syncs the version from package.json to the SVG banner and .llm/context.md.
.DESCRIPTION
    Reads the version from package.json and updates the version badge in the
    unity-helpers-banner.svg file and the version field in .llm/context.md.
    Automatically stages modified files. Runs on every commit to ensure
    version references are always in sync.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Load shared git helpers for safe index operations
$helpersPath = Join-Path -Path $PSScriptRoot -ChildPath 'git-staging-helpers.ps1'
. $helpersPath

$repoRoot = Split-Path -Parent $PSScriptRoot
$packageJsonPath = Join-Path $repoRoot 'package.json'
$bannerSvgPath = Join-Path $repoRoot 'docs/images/unity-helpers-banner.svg'

# Get repository info for lock handling
$repositoryInfo = $null
try {
    Assert-GitAvailable | Out-Null
    $repositoryInfo = Get-GitRepositoryInfo
} catch {
    Write-Error $_.Exception.Message
    exit 1
}

# Wait for any external tool (lazygit, IDE, etc.) to release the index.lock
# before starting operations. This prevents contention with interactive git tools.
if (-not (Invoke-EnsureNoIndexLock)) {
    Write-Warning "index.lock still held after waiting. Proceeding anyway, but operations may fail."
}

# Verify files exist
if (-not (Test-Path $packageJsonPath)) {
    Write-Error "package.json not found at: $packageJsonPath"
    exit 1
}

# Read version from package.json
$packageJson = Get-Content $packageJsonPath -Raw | ConvertFrom-Json
$version = $packageJson.version

if ([string]::IsNullOrWhiteSpace($version)) {
    Write-Error 'Could not read version from package.json'
    exit 1
}

Write-Host "Found version: v$version"

# --- Sync SVG banner version ---
if (-not (Test-Path $bannerSvgPath)) {
    Write-Host "Banner SVG not found at: $bannerSvgPath, skipping."
}
else {
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
        }
        else {
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
    }
    else {
        Write-Host 'No version pattern found in SVG, skipping.'
    }
}

# --- Sync .llm/context.md version ---
$contextMdPath = Join-Path $repoRoot '.llm/context.md'
if (Test-Path $contextMdPath) {
    $contextContent = Get-Content $contextMdPath -Raw
    $contextPattern = '(?m)^\*\*Version\*\*:\s+\d+\.\d+\.\d+[^\r\n]*'
    $contextReplacement = "**Version**: $version"

    if ($contextContent -match $contextPattern) {
        $currentContextVersion = $Matches[0]
        if ($currentContextVersion -ne $contextReplacement) {
            $updatedContextContent = $contextContent -replace $contextPattern, $contextReplacement
            # Trim trailing whitespace and add exactly one LF (Markdown files require LF per .editorconfig)
            $updatedContextContent = $updatedContextContent.TrimEnd() + "`n"
            Set-Content -Path $contextMdPath -Value $updatedContextContent -NoNewline -Encoding UTF8
            Write-Host "Updated .llm/context.md version to: $version"

            $gitAddExitCode = Invoke-GitAddWithRetry `
                -Items @($contextMdPath) `
                -IndexLockPath $repositoryInfo.IndexLockPath
            if ($gitAddExitCode -ne 0) {
                Write-Error "git add failed for context.md with exit code $gitAddExitCode."
                exit $gitAddExitCode
            }
            Write-Host "Staged: $contextMdPath"
        }
        else {
            Write-Host ".llm/context.md already has correct version: $version"
        }
    }
    else {
        Write-Host "No version pattern found in .llm/context.md, skipping."
    }
}
else {
    Write-Host ".llm/context.md not found, skipping."
}

exit 0
