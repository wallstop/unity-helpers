# =============================================================================
# Git Push Defaults Configuration (PowerShell)
# =============================================================================
# Idempotently sets local-only git push defaults so that `git push` on a new
# branch sets tracking automatically and uses the "simple" push strategy.
#
# Usage:
#   ./scripts/configure-git-defaults.ps1                 # uses script's parent dir
#   ./scripts/configure-git-defaults.ps1 -RepoRoot <path>
#
# Effects (local config only; NEVER global):
#   git config --local push.autoSetupRemote true
#   git config --local push.default simple
# =============================================================================

[CmdletBinding()]
param(
    [string]$RepoRoot
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent $ScriptDir
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
}

function Write-ErrorMsg {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ $Message" -ForegroundColor Cyan
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-ErrorMsg "git is not installed or not on PATH."
    exit 1
}

$null = git -C $RepoRoot rev-parse --git-dir 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-ErrorMsg "Not a git repository: $RepoRoot"
    exit 1
}

# push.autoSetupRemote requires git >= 2.37.
try {
    $gitVersionRaw = (& git --version 2>$null) -replace '^git version ', ''
    $parts = $gitVersionRaw.Split('.')
    if ($parts.Length -ge 2) {
        $gitMajor = [int]($parts[0] -replace '[^0-9]', '')
        $gitMinor = [int]($parts[1] -replace '[^0-9]', '')
        if ($gitMajor -lt 2 -or ($gitMajor -eq 2 -and $gitMinor -lt 37)) {
            Write-Warning "git $gitVersionRaw detected. push.autoSetupRemote requires git >= 2.37; older clients will silently ignore it."
        }
    }
}
catch {
    # Version parse best-effort; do not fail the script.
}

Write-Info "Configuring git push defaults (local only) in $RepoRoot"

& git -C $RepoRoot config --local push.autoSetupRemote true
if ($LASTEXITCODE -ne 0) {
    Write-ErrorMsg "Failed to set push.autoSetupRemote"
    exit 1
}
Write-Success "push.autoSetupRemote = true"

& git -C $RepoRoot config --local push.default simple
if ($LASTEXITCODE -ne 0) {
    Write-ErrorMsg "Failed to set push.default"
    exit 1
}
Write-Success "push.default = simple"

$finalAutoSetup = (& git -C $RepoRoot config --local --get push.autoSetupRemote 2>$null)
$finalDefault = (& git -C $RepoRoot config --local --get push.default 2>$null)

Write-Host "push.autoSetupRemote=$finalAutoSetup"
Write-Host "push.default=$finalDefault"

if ($finalAutoSetup -ne "true" -or $finalDefault -ne "simple") {
    Write-ErrorMsg "Post-configure verification failed (push.autoSetupRemote=$finalAutoSetup push.default=$finalDefault)"
    exit 1
}

exit 0
