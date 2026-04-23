# =============================================================================
# Git Push Defaults Configuration (PowerShell CLI wrapper)
# =============================================================================
# Thin CLI wrapper around Set-RepoGitPushDefaults (defined in
# scripts/git-push-defaults-helpers.ps1). Idempotently sets local-only git
# push defaults so that `git push` on a new branch sets tracking automatically
# and uses the "simple" push strategy.
#
# Usage:
#   ./scripts/configure-git-defaults.ps1                 # uses script's parent dir
#   ./scripts/configure-git-defaults.ps1 -RepoRoot <path>
#
# Effects (local config only; NEVER global):
#   git config --local push.autoSetupRemote true
#   git config --local push.default simple
#
# Exit codes:
#   0 — push defaults applied and verified
#   1 — git missing, RepoRoot invalid, or write/verification failed
#
# When running from another PowerShell script in this repo, prefer dot-
# sourcing scripts/git-push-defaults-helpers.ps1 and calling
# Set-RepoGitPushDefaults directly. That avoids the Windows PowerShell 5.1
# host breakage caused by `pwsh -NoProfile -File <sibling>.ps1` when pwsh
# is not on PATH.
# =============================================================================

[CmdletBinding()]
param(
    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Split-Path -Parent $ScriptDir
}

. (Join-Path $ScriptDir 'git-push-defaults-helpers.ps1')

$result = Set-RepoGitPushDefaults -RepoRoot $RepoRoot
if (-not $result.Success) {
    exit 1
}

exit 0
