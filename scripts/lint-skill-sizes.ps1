<#
.SYNOPSIS
    Validates LLM skill file sizes against documented limits.

.DESCRIPTION
    Checks all .llm/skills/*.md files against size thresholds:
    - >500 lines: ERROR (MUST split per manage-skills.md)
    - >300 lines: WARNING (consider splitting)

.PARAMETER VerboseOutput
    If specified, outputs detailed information during validation including
    OK status for files within limits and WARNING status for files over 300 lines.

.EXAMPLE
    pwsh -NoProfile -File scripts/lint-skill-sizes.ps1
    pwsh -NoProfile -File scripts/lint-skill-sizes.ps1 -VerboseOutput
#>

Param(
    [switch]$VerboseOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-ErrorMsg($msg) {
    Write-Host "[skill-sizes] ERROR: $msg" -ForegroundColor Red
}

function Write-WarningMsg($msg) {
    Write-Host "[skill-sizes] WARNING: $msg" -ForegroundColor Yellow
}

function Write-SuccessMsg($msg) {
    Write-Host "[skill-sizes] OK: $msg" -ForegroundColor Green
}

$repoRoot = (Get-Item $PSScriptRoot).Parent.FullName
$skillsDir = Join-Path -Path $repoRoot -ChildPath '.llm/skills'
$maxLines = 500
$warningLines = 300
$exitCode = 0

# Validate skills directory exists
if (-not (Test-Path $skillsDir)) {
    Write-ErrorMsg "Skills directory not found at: $skillsDir"
    exit 1
}

$skillFiles = Get-ChildItem -Path $skillsDir -Filter '*.md' | Sort-Object Name
$errorCount = 0
$warningCount = 0
$okCount = 0

foreach ($file in $skillFiles) {
    $lineCount = (Get-Content $file.FullName).Count

    if ($lineCount -gt $maxLines) {
        Write-ErrorMsg "$($file.Name): $lineCount lines (max: $maxLines) - MUST split"
        $exitCode = 1
        $errorCount++
    }
    elseif ($lineCount -gt $warningLines) {
        if ($VerboseOutput) {
            Write-WarningMsg "$($file.Name): $lineCount lines (consider splitting)"
        }
        $warningCount++
    }
    else {
        if ($VerboseOutput) {
            Write-SuccessMsg "$($file.Name): $lineCount lines"
        }
        $okCount++
    }
}

# Summary
Write-Host ""
Write-Host ("=" * 60)
Write-Host "[skill-sizes] Summary: $($skillFiles.Count) files checked"
if ($VerboseOutput) {
    Write-Host "[skill-sizes]   OK: $okCount, Warnings: $warningCount, Errors: $errorCount"
}

if ($exitCode -eq 0) {
    Write-Host "[skill-sizes] All skill files within size limits" -ForegroundColor Green
}
else {
    Write-Host "[skill-sizes] Some skill files exceed size limits - see errors above" -ForegroundColor Red
}
Write-Host ("=" * 60)

exit $exitCode
