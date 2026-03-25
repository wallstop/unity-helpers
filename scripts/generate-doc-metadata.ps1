<#
.SYNOPSIS
    Generates documentation metadata by counting codebase items deterministically.
.DESCRIPTION
    Single source of truth for all documentation counts. Scans the codebase to
    count tests, PRNG implementations, editor tools, and other metrics.
    Outputs JSON to stdout.

    Run sync-doc-counts.ps1 to apply these counts to documentation files.
.EXAMPLE
    pwsh -NoProfile -File scripts/generate-doc-metadata.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = (Get-Item $PSScriptRoot).Parent.FullName

# --- Helper: round down to a "nice" display number ---
function Get-RoundedFloor([int]$exact) {
    if ($exact -lt 20) {
        $rounded = [Math]::Floor($exact / 5) * 5
        if ($rounded -eq 0) { $rounded = $exact }
    }
    elseif ($exact -lt 100) {
        $rounded = [Math]::Floor($exact / 10) * 10
    }
    elseif ($exact -lt 1000) {
        $rounded = [Math]::Floor($exact / 100) * 100
    }
    else {
        $rounded = [Math]::Floor($exact / 1000) * 1000
    }
    return [int]$rounded
}

function Format-DisplayCount([int]$exact) {
    $rounded = Get-RoundedFloor $exact
    $formatted = $rounded.ToString('N0', [System.Globalization.CultureInfo]::InvariantCulture)
    return "${formatted}+"
}

# ============================================================
# Count test methods
# ============================================================
$testsDir = Join-Path $repoRoot 'Tests'
$testCount = 0

if (Test-Path $testsDir) {
    $testFiles = Get-ChildItem -Path $testsDir -Recurse -Filter '*.cs'
    foreach ($file in $testFiles) {
        $lines = Get-Content -Path $file.FullName
        foreach ($line in $lines) {
            if ($line -match '^\s*\[(Test|UnityTest)\]') {
                $testCount++
            }
            elseif ($line -match '^\s*\[TestCase\(') {
                $testCount++
            }
            elseif ($line -match '^\s*\[TestCaseSource\(') {
                $testCount++
            }
        }
    }
}

[Console]::Error.WriteLine("[doc-metadata] Tests: $testCount exact -> $(Format-DisplayCount $testCount)")

# ============================================================
# Count PRNG implementations
# ============================================================
$randomDir = Join-Path $repoRoot 'Runtime/Core/Random'
$prngCount = 0

# Known non-PRNG types in the Random directory
$excludedTypes = @(
    'IRandom', 'AbstractRandom', 'PRNG', 'ThreadLocalRandom',
    'RandomGeneratorMetadata', 'RandomState', 'RandomUtilities',
    'RandomComparer', 'RandomSpeedBucket', 'PerlinNoise'
)

if (Test-Path $randomDir) {
    $randomFiles = Get-ChildItem -Path $randomDir -Recurse -Filter '*.cs'
    foreach ($file in $randomFiles) {
        if ($file.BaseName -in $excludedTypes) { continue }

        $content = Get-Content -Path $file.FullName -Raw
        # Match class/struct that inherits AbstractRandom or implements IRandom
        if ($content -match '(?:sealed\s+)?(?:public\s+)?(?:class|struct)\s+\w+[^{]*?(?:AbstractRandom|IRandom)') {
            $prngCount++
        }
    }
}

[Console]::Error.WriteLine("[doc-metadata] PRNGs: $prngCount exact -> $(Format-DisplayCount $prngCount)")

# ============================================================
# Count editor tools (EditorWindow, ScriptableWizard, standalone MenuItem)
# ============================================================
$editorDir = Join-Path $repoRoot 'Editor'
$editorToolCount = 0
$windowFiles = @()

if (Test-Path $editorDir) {
    $editorFiles = Get-ChildItem -Path $editorDir -Recurse -Filter '*.cs'

    # Count EditorWindow and ScriptableWizard subclasses
    foreach ($file in $editorFiles) {
        $content = Get-Content -Path $file.FullName -Raw
        if ($content -match ':\s*(?:EditorWindow|ScriptableWizard)') {
            $editorToolCount++
            $windowFiles += $file.FullName
        }
    }

    # Count standalone [MenuItem] providers (not already counted)
    foreach ($file in $editorFiles) {
        if ($file.FullName -in $windowFiles) { continue }
        $content = Get-Content -Path $file.FullName -Raw
        if ($content -match '\[MenuItem\(') {
            $editorToolCount++
        }
    }
}

[Console]::Error.WriteLine("[doc-metadata] Editor tools: $editorToolCount exact -> $(Format-DisplayCount $editorToolCount)")

# ============================================================
# Output JSON
# ============================================================
$metadata = [ordered]@{
    generatedAt    = (Get-Date).ToUniversalTime().ToString('yyyy-MM-dd HH:mm:ss') + ' UTC'
    generatedBy    = 'scripts/generate-doc-metadata.ps1'
    testCount      = [ordered]@{
        exact   = $testCount
        display = (Format-DisplayCount $testCount)
    }
    prngCount      = [ordered]@{
        exact   = $prngCount
        display = (Format-DisplayCount $prngCount)
    }
    editorToolCount = [ordered]@{
        exact   = $editorToolCount
        display = (Format-DisplayCount $editorToolCount)
    }
}

$metadata | ConvertTo-Json -Depth 3 | Write-Output
