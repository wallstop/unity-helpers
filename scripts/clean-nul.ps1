Param(
    [switch]$VerboseOutput
)

$ErrorActionPreference = 'Stop'

function Remove-NulFile {
    $cwd = (Get-Location).Path
    $nulPath = Join-Path $cwd 'nul'
    $longPath = "\\?\$nulPath"

    if ([System.IO.File]::Exists($longPath)) {
        if ($VerboseOutput) { Write-Host "Deleting reserved-name file: $nulPath" -ForegroundColor Yellow }
        [System.IO.File]::Delete($longPath)
        if ($VerboseOutput) { Write-Host "Deleted." -ForegroundColor Green }
        return $true
    }
    return $false
}

try {
    if (-not (Remove-NulFile)) {
        if ($VerboseOutput) { Write-Host "No 'nul' file found at repo root." -ForegroundColor DarkGray }
    }
}
catch {
    Write-Error $_
    exit 1
}
