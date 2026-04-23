# =============================================================================
# Git Push Defaults Helpers (PowerShell, dot-sourceable)
# =============================================================================
# Shared helper module for configuring and verifying local-only git push
# defaults. Dot-source this file from any PowerShell script that needs to
# apply the repository's push configuration contract:
#
#     git config --local push.autoSetupRemote true
#     git config --local push.default simple
#
# The canonical CLI wrapper is scripts/configure-git-defaults.ps1.
# Callers that already run in a PowerShell host (install-hooks.ps1,
# agent-preflight.ps1) dot-source this module and invoke the function
# directly, which avoids the Windows PowerShell 5.1 host breakage caused
# by invoking `pwsh -NoProfile -File <sibling>.ps1` unconditionally.
#
# Contract:
#   - Set-RepoGitPushDefaults never calls `exit` (would kill dot-source
#     callers) and never throws on configuration failure — it returns a
#     hashtable with @{ Success; Errors; Values } so callers can decide.
#   - It writes clear colored Write-Host messages.
#   - It validates git availability, repo-root, and warns on git < 2.37
#     (push.autoSetupRemote support).
#
# Usage:
#   . (Join-Path $PSScriptRoot 'git-push-defaults-helpers.ps1')
#   $result = Set-RepoGitPushDefaults -RepoRoot $RepoRoot
#   if (-not $result.Success) { ... }
# =============================================================================

Set-StrictMode -Version Latest

function Write-PushDefaultsSuccess {
    param([string]$Message)
    Write-Host "[git-push-defaults] $Message" -ForegroundColor Green
}

function Write-PushDefaultsWarning {
    param([string]$Message)
    Write-Host "[git-push-defaults] WARNING: $Message" -ForegroundColor Yellow
}

function Write-PushDefaultsError {
    param([string]$Message)
    Write-Host "[git-push-defaults] ERROR: $Message" -ForegroundColor Red
}

function Write-PushDefaultsInfo {
    param([string]$Message)
    Write-Host "[git-push-defaults] $Message" -ForegroundColor Cyan
}

<#
.SYNOPSIS
    Apply the repository's local-only git push defaults and verify persistence.

.DESCRIPTION
    Idempotently sets:
        push.autoSetupRemote = true
        push.default         = simple
    in the LOCAL .git/config of the supplied repository root. After writing,
    re-reads both values to confirm persistence (catches wrapper shims,
    permission issues, or concurrent external edits silently swallowing
    the write).

    The function is safe to dot-source: it never calls `exit`. It never
    throws on configuration failure either — instead it returns a hashtable
    so callers can branch on Success and surface Errors to the user.

.PARAMETER RepoRoot
    Absolute path to the repository root. Required. The helper verifies the
    path is a valid git work tree before attempting writes.

.OUTPUTS
    Hashtable:
        Success = [bool]    — true if both values wrote AND verified
        Errors  = [string[]] — actionable error descriptions (empty on success)
        Values  = @{         — the post-write observed values (diagnostic)
            'push.autoSetupRemote' = <string>
            'push.default'         = <string>
        }

.EXAMPLE
    . (Join-Path $PSScriptRoot 'git-push-defaults-helpers.ps1')
    $result = Set-RepoGitPushDefaults -RepoRoot (Split-Path -Parent $PSScriptRoot)
    if (-not $result.Success) {
        foreach ($err in $result.Errors) { Write-Host $err }
    }
#>
function Set-RepoGitPushDefaults {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $result = @{
        Success = $false
        Errors = @()
        Values = @{
            'push.autoSetupRemote' = ''
            'push.default' = ''
        }
    }

    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        Write-PushDefaultsError 'git is not installed or not on PATH.'
        $result.Errors += 'git is not installed or not on PATH.'
        return $result
    }

    if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
        Write-PushDefaultsError 'RepoRoot is required and must be non-empty.'
        $result.Errors += 'RepoRoot is required and must be non-empty.'
        return $result
    }

    if (-not (Test-Path -LiteralPath $RepoRoot -PathType Container)) {
        Write-PushDefaultsError "RepoRoot does not exist or is not a directory: $RepoRoot"
        $result.Errors += "RepoRoot does not exist or is not a directory: $RepoRoot"
        return $result
    }

    $null = & git -C $RepoRoot rev-parse --git-dir 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-PushDefaultsError "Not a git repository: $RepoRoot"
        $result.Errors += "Not a git repository: $RepoRoot"
        return $result
    }

    # push.autoSetupRemote requires git >= 2.37; older clients silently ignore.
    try {
        $gitVersionRaw = (& git --version 2>$null) -replace '^git version ', ''
        $parts = $gitVersionRaw.Split('.')
        if ($parts.Length -ge 2) {
            $gitMajor = [int]($parts[0] -replace '[^0-9]', '')
            $gitMinor = [int]($parts[1] -replace '[^0-9]', '')
            if ($gitMajor -lt 2 -or ($gitMajor -eq 2 -and $gitMinor -lt 37)) {
                Write-PushDefaultsWarning "git $gitVersionRaw detected. push.autoSetupRemote requires git >= 2.37; older clients will silently ignore it."
            }
        }
    }
    catch {
        # Version parse best-effort; do not fail the helper.
    }

    Write-PushDefaultsInfo "Configuring git push defaults (local only) in $RepoRoot"

    & git -C $RepoRoot config --local push.autoSetupRemote true 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-PushDefaultsError 'Failed to set push.autoSetupRemote'
        $result.Errors += 'Failed to set push.autoSetupRemote'
        return $result
    }
    Write-PushDefaultsSuccess 'push.autoSetupRemote = true'

    & git -C $RepoRoot config --local push.default simple 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-PushDefaultsError 'Failed to set push.default'
        $result.Errors += 'Failed to set push.default'
        return $result
    }
    Write-PushDefaultsSuccess 'push.default = simple'

    # Re-read both values to verify persistence. Anything that could silently
    # swallow the write (permissions, wrapper shim, concurrent edit) is caught
    # here rather than reported as a false "fixed" state.
    # Some git builds emit a trailing CR (especially on Windows / MSYS mounts)
    # or a trailing newline the caller has already stripped — .Trim() keeps
    # the compare against the expected literal robust either way.
    $finalAutoSetup = (& git -C $RepoRoot config --local --get push.autoSetupRemote 2>$null)
    if ($LASTEXITCODE -ne 0) { $finalAutoSetup = '' }
    $finalAutoSetup = ([string]$finalAutoSetup).Trim()
    $finalDefault = (& git -C $RepoRoot config --local --get push.default 2>$null)
    if ($LASTEXITCODE -ne 0) { $finalDefault = '' }
    $finalDefault = ([string]$finalDefault).Trim()

    $result.Values['push.autoSetupRemote'] = $finalAutoSetup
    $result.Values['push.default'] = $finalDefault

    Write-Host "push.autoSetupRemote=$finalAutoSetup"
    Write-Host "push.default=$finalDefault"

    if ($finalAutoSetup -ne 'true' -or $finalDefault -ne 'simple') {
        Write-PushDefaultsError "Post-configure verification failed (push.autoSetupRemote=$finalAutoSetup push.default=$finalDefault)"
        $result.Errors += "Post-configure verification failed (push.autoSetupRemote=$finalAutoSetup push.default=$finalDefault)"
        return $result
    }

    $result.Success = $true
    return $result
}
