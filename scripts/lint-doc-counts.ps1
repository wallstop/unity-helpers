<#
.SYNOPSIS
    Validates that documentation counts match actual codebase metrics.
.DESCRIPTION
    Runs sync-doc-counts.ps1 in check mode to verify that all documentation
    files have correct counts for tests, PRNGs, editor tools, etc.

    Exits with code 1 if any counts are out of sync.
.EXAMPLE
    pwsh -NoProfile -File scripts/lint-doc-counts.ps1
#>
# lint-pwsh-invocations: allow-subprocess-pwsh sync-doc-counts.ps1 uses `exit` extensively for CI exit-code propagation; subprocess isolation preserves that contract without tangling parent host state.
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$syncScript = Join-Path $PSScriptRoot 'sync-doc-counts.ps1'

if (-not (Test-Path $syncScript)) {
    Write-Error "sync-doc-counts.ps1 not found at: $syncScript"
    exit 1
}

& pwsh -NoProfile -File $syncScript -Check
exit $LASTEXITCODE
