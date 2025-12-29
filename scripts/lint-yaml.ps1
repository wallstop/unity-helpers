#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Lint YAML files using yamllint.

.DESCRIPTION
    Runs yamllint on YAML files to check for syntax and style issues.
    Can lint all YAML files or only staged files for pre-commit hooks.

.PARAMETER StagedOnly
    If specified, only lint files that are staged in git.

.PARAMETER Paths
    Specific paths to lint. If not provided, lints all YAML files.

.PARAMETER VerboseOutput
    If specified, show verbose output including which files are being checked.

.EXAMPLE
    ./lint-yaml.ps1
    Lint all YAML files in the repository.

.EXAMPLE
    ./lint-yaml.ps1 -StagedOnly
    Lint only staged YAML files (for pre-commit hooks).

.EXAMPLE
    ./lint-yaml.ps1 -Paths .github/workflows/ci.yml
    Lint a specific file.
#>
param(
    [switch]$StagedOnly,
    [switch]$VerboseOutput,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Paths
)

$ErrorActionPreference = 'Stop'

# Check for yamllint availability
$yamllint = Get-Command yamllint -ErrorAction SilentlyContinue
if (-not $yamllint) {
    # Try pip-installed yamllint on Windows
    $pipYamllint = Get-Command "$env:LOCALAPPDATA\Programs\Python\Python*\Scripts\yamllint.exe" -ErrorAction SilentlyContinue
    if ($pipYamllint) {
        $yamllint = $pipYamllint
    }
}

if (-not $yamllint) {
    Write-Warning "yamllint not found. Install with: pip install yamllint"
    Write-Warning "Skipping YAML lint check."
    exit 0
}

$configFile = Join-Path -Path $PSScriptRoot -ChildPath '..' -AdditionalChildPath '.yamllint.yaml'
$configFile = (Resolve-Path $configFile -ErrorAction SilentlyContinue).Path
if (-not $configFile -or -not (Test-Path $configFile)) {
    $configFile = Join-Path -Path (Get-Location) -ChildPath '.yamllint.yaml'
}

if (-not (Test-Path $configFile)) {
    Write-Error "yamllint config file not found at: $configFile"
    exit 1
}

# Get files to lint
$filesToLint = @()

if ($StagedOnly) {
    # Load git helpers if available
    $helpersPath = Join-Path -Path $PSScriptRoot -ChildPath 'git-staging-helpers.ps1'
    if (Test-Path $helpersPath) {
        . $helpersPath
        $yamlGlobs = @('*.yml', '*.yaml')
        $filesToLint = Get-StagedPathsForGlobs -DefaultPaths $Paths -Globs $yamlGlobs
        $filesToLint = Get-ExistingPaths -Candidates $filesToLint
    } else {
        # Fallback: get staged files manually
        $stagedFiles = git diff --cached --name-only --diff-filter=ACMR 2>$null
        if ($stagedFiles) {
            $filesToLint = $stagedFiles | Where-Object { $_ -match '\.(ya?ml)$' } | Where-Object { Test-Path $_ }
        }
    }
} elseif ($Paths -and $Paths.Count -gt 0) {
    $filesToLint = $Paths | Where-Object { Test-Path $_ }
} else {
    # Lint all YAML files (let yamllint handle discovery via config)
    $filesToLint = @('.')
}

if ($filesToLint.Count -eq 0) {
    if ($VerboseOutput) {
        Write-Host "No YAML files to lint."
    }
    exit 0
}

if ($VerboseOutput) {
    Write-Host "Linting YAML files with yamllint..."
    if ($filesToLint -ne @('.')) {
        foreach ($file in $filesToLint) {
            Write-Host "  - $file"
        }
    }
}

# Run yamllint
$yamllintArgs = @('-c', $configFile)
$yamllintArgs += $filesToLint

& $yamllint.Source @yamllintArgs
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0) {
    Write-Error "yamllint found issues. Exit code: $exitCode"
    exit $exitCode
}

if ($VerboseOutput) {
    Write-Host "yamllint passed." -ForegroundColor Green
}

exit 0
