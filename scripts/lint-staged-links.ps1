#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Check for broken links in staged markdown files using lychee.

.DESCRIPTION
  This script checks staged markdown files for broken links using lychee.
  It respects the .lychee.toml configuration file in the repository root.

.EXAMPLE
  pwsh -NoProfile -File scripts/lint-staged-links.ps1
#>

param(
  [Parameter(ValueFromRemainingArguments = $true)]
  [string[]]$Paths
)

$helpersPath = Join-Path -Path $PSScriptRoot -ChildPath 'git-staging-helpers.ps1'
. $helpersPath

try {
  Assert-GitAvailable | Out-Null
  $repositoryInfo = Get-GitRepositoryInfo
} catch {
  Write-Error $_.Exception.Message
  exit 1
}

$markdownGlobs = @('*.md', '*.markdown')
$Paths = Get-StagedPathsForGlobs -DefaultPaths $Paths -Globs $markdownGlobs

$existingPaths = Get-ExistingPaths -Candidates $Paths

if ($existingPaths.Count -eq 0) {
  Write-Host "No staged markdown files to check."
  exit 0
}

# Check if lychee is available
$lychee = Get-Command lychee -ErrorAction SilentlyContinue
if (-not $lychee) {
  Write-Warning "lychee not installed; skipping link check. Install via: cargo install lychee or use the devcontainer."
  exit 0
}

# Check if config file exists
$configPath = Join-Path -Path $repositoryInfo.RepositoryRoot -ChildPath '.lychee.toml'
$lycheeArgs = @()

if (Test-Path $configPath) {
  $lycheeArgs += @('-c', $configPath)
}

$lycheeArgs += @(
  '--no-progress',
  '--include-fragments'
)

$lycheeArgs += $existingPaths

Write-Host "Checking links in $($existingPaths.Count) staged markdown file(s)..."
& lychee @lycheeArgs

if ($LASTEXITCODE -ne 0) {
  Write-Error "lychee found broken links (exit code $LASTEXITCODE). Fix the links before committing."
  exit $LASTEXITCODE
}

Write-Host "All links are valid." -ForegroundColor Green
