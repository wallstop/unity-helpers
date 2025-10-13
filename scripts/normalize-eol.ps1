param(
    [switch]$DryRun,
    [switch]$VerboseOutput
)

$ErrorActionPreference = 'Stop'

# Extensions to normalize (tracked by git)
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

function To-CrLf([string]$text) {
    $tmp = $text -replace "`r`n", "`n" -replace "`r", "`n"
    return $tmp -replace "`n", "`r`n"
}

$changed = 0
$eolFixed = 0
$bomRemoved = 0
$modified = New-Object System.Collections.Generic.List[string]

$tracked = Get-TrackedFiles
foreach ($path in $tracked) {
    try { $bytes = [System.IO.File]::ReadAllBytes($path) } catch { continue }

    $hasBom = $false
    if ($bytes.Length -ge 3) {
        $hasBom = ($bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
    }

    if ($hasBom) {
        $text = [System.Text.Encoding]::UTF8.GetString($bytes, 3, $bytes.Length - 3)
    } else {
        $text = [System.Text.Encoding]::UTF8.GetString($bytes)
    }

    $normalized = To-CrLf $text

    $fileChanged = $false
    if ($normalized -ne $text) { $fileChanged = $true; $eolFixed++ }
    # Remove BOM if present (we enforce UTF-8 without BOM)
    if ($hasBom) { $fileChanged = $true; $bomRemoved++ }

    if ($fileChanged) {
        if (-not $DryRun) {
            # Write UTF-8 without BOM
            [System.IO.File]::WriteAllBytes($path, [System.Text.Encoding]::UTF8.GetBytes($normalized))
        }
        $changed++
        $modified.Add($path) | Out-Null
        if ($VerboseOutput) { Write-Host "Fixed: $path" }
    }
}

Write-Host "Files fixed: $changed (EOL:$eolFixed, BOMRemoved:$bomRemoved)"
if ($DryRun -and $changed -gt 0) { exit 2 }
