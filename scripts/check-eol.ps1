param(
    [switch]$VerboseOutput
)

$ErrorActionPreference = 'Stop'

$extensions = @(
    'cs','csproj','sln',
    'json','yaml','yml','md','xml','uxml','uss',
    'shader','hlsl','compute','cginc',
    'asmdef','asmref','meta','ps1'
)

function Get-TrackedFiles {
    $files = (git ls-files -z) -split "`0" | Where-Object { $_ -ne '' }
    return $files | Where-Object {
        $ext = [System.IO.Path]::GetExtension($_).TrimStart('.').ToLowerInvariant()
        $extensions -contains $ext
    }
}

$lfIssues = New-Object System.Collections.Generic.List[string]
$bomIssues = New-Object System.Collections.Generic.List[string]

foreach ($path in Get-TrackedFiles) {
    try { $bytes = [System.IO.File]::ReadAllBytes($path) } catch { continue }
    # BOM
    $hasBom = $false
    if ($bytes.Length -ge 3) { $hasBom = ($bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) }
    if (-not $hasBom) { $bomIssues.Add($path) | Out-Null }
    # CRLF check
    $hasLfOnly = $false
    for ($i = 0; $i -lt $bytes.Length; $i++) {
        if ($bytes[$i] -eq 0x0A -and ($i -eq 0 -or $bytes[$i-1] -ne 0x0D)) { $hasLfOnly = $true; break }
    }
    if ($hasLfOnly) { $lfIssues.Add($path) | Out-Null }
}

if ($VerboseOutput) {
    if ($lfIssues.Count -gt 0) {
        Write-Host "LF-only or mixed EOL files:"; $lfIssues | Sort-Object -Unique | ForEach-Object { " - $_" }
    }
    if ($bomIssues.Count -gt 0) {
        Write-Host "Missing UTF-8 BOM:"; $bomIssues | Sort-Object -Unique | ForEach-Object { " - $_" }
    }
}

Write-Host "LF issues: $($lfIssues | Sort-Object -Unique | Measure-Object | % Count)"
Write-Host "Missing BOM: $($bomIssues | Sort-Object -Unique | Measure-Object | % Count)"

if ($lfIssues.Count -gt 0 -or $bomIssues.Count -gt 0) { exit 3 }

