<#
.SYNOPSIS
    Validates that pre-commit hook calls all required sync scripts and
    pre-push hook reads stdin for changed-file detection.
.DESCRIPTION
    Checks that the pre-commit hook invokes all required version sync scripts:
      - sync-banner-version.ps1
      - sync-issue-template-versions.ps1

    Also validates the pre-push hook reads stdin to determine changed files
    (critical for performance — without this, pre-push scans the entire repo).

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

# ---- Validate pre-push hook reads stdin for changed-file detection ----
$prePushPath = Join-Path $repoRoot '.githooks' 'pre-push'

if (-not (Test-Path $prePushPath)) {
    Write-Error "pre-push hook not found at: $prePushPath"
    exit 1
}

$prePushContent = Get-Content $prePushPath -Raw

# The pre-push hook MUST read stdin to determine changed files.
# Without this, all checks scan the entire repository (~60s).
$requiredPrePushPatterns = @(
    @{ Pattern = 'while read'; Description = 'reads stdin (while read loop)' },
    @{ Pattern = 'local_sha'; Description = 'parses local SHA from stdin' },
    @{ Pattern = 'remote_sha'; Description = 'parses remote SHA from stdin' },
    @{ Pattern = 'CHANGED_FILES_LIST'; Description = 'stores changed files in temp file' }
)

$prePushMissing = @()
foreach ($entry in $requiredPrePushPatterns) {
    if ($prePushContent -notmatch [regex]::Escape($entry.Pattern)) {
        $prePushMissing += $entry
    }
}

if ($prePushMissing.Count -gt 0) {
    Write-Host ''
    Write-Warning "The pre-push hook is missing required changed-file detection patterns:"
    foreach ($m in $prePushMissing) {
        Write-Warning "  - $($m.Description) (expected: '$($m.Pattern)')"
    }
    Write-Host ''
    Write-Error "Pre-push hook is missing $($prePushMissing.Count) required pattern(s). The hook must read stdin to detect changed files for performance."
    exit 1
}

if ($VerboseOutput) {
    Write-Host "Pre-push hook correctly reads stdin for changed-file detection." -ForegroundColor Green
}
exit 0
