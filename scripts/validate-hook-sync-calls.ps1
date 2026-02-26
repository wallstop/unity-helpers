<#
.SYNOPSIS
    Validates that pre-commit hook step 0 calls all required sync scripts.
.DESCRIPTION
    Checks that the pre-commit hook invokes all required version sync scripts:
      - sync-banner-version.ps1
      - sync-issue-template-versions.ps1

    This prevents regressions where new sync scripts are added to the
    repository but not wired into the pre-commit hook.
.EXAMPLE
    pwsh -NoProfile -File scripts/validate-hook-sync-calls.ps1
#>
[CmdletBinding()]
param(
    [switch]$VerboseOutput
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$preCommitPath = Join-Path $repoRoot '.githooks' 'pre-commit'

if (-not (Test-Path $preCommitPath)) {
    Write-Error "pre-commit hook not found at: $preCommitPath"
    exit 1
}

$hookContent = Get-Content $preCommitPath -Raw

# Define required sync script calls
# MAINTENANCE: When adding a new sync script to the repository, add it here
# so the validator catches missing invocations in the pre-commit hook.
$requiredSyncScripts = @(
    'scripts/sync-banner-version.ps1',
    'scripts/sync-issue-template-versions.ps1'
)

$missing = @()
foreach ($script in $requiredSyncScripts) {
    if ($hookContent -notmatch [regex]::Escape($script)) {
        $missing += $script
    }
}

if ($missing.Count -gt 0) {
    Write-Host ''
    Write-Warning "The pre-commit hook is missing calls to the following sync scripts:"
    foreach ($m in $missing) {
        Write-Warning "  - $m"
    }
    Write-Host ''
    Write-Error "Pre-commit hook is missing $($missing.Count) required sync script call(s). Add them to step 0 in .githooks/pre-commit."
    exit 1
}

if ($VerboseOutput) {
    Write-Host "Pre-commit hook correctly invokes all required sync scripts." -ForegroundColor Green
}
exit 0
