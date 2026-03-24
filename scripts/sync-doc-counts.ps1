<#
.SYNOPSIS
    Syncs documentation counts with actual codebase metrics.
.DESCRIPTION
    Runs generate-doc-metadata.ps1 to count tests, PRNGs, editor tools, etc.,
    then updates all user-facing documentation files with the correct counts.

    Uses an explicit list of target files to prevent false positives in
    skill files, session logs, or other internal documentation.

    Use -Check to validate without modifying files (for CI/lint).
.EXAMPLE
    pwsh -NoProfile -File scripts/sync-doc-counts.ps1
    pwsh -NoProfile -File scripts/sync-doc-counts.ps1 -Check
#>
[CmdletBinding()]
param(
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = (Get-Item $PSScriptRoot).Parent.FullName
$generateScript = Join-Path $PSScriptRoot 'generate-doc-metadata.ps1'

if (-not (Test-Path $generateScript)) {
    Write-Error "generate-doc-metadata.ps1 not found at: $generateScript"
    exit 1
}

# Run generate script (status goes to stderr, JSON to stdout)
$rawOutput = & pwsh -NoProfile -File $generateScript 2>&1
$stdoutLines = $rawOutput | Where-Object { $_ -is [string] }
$metadata = ($stdoutLines -join "`n") | ConvertFrom-Json

$testDisplay = $metadata.testCount.display -replace '\+$', ''
$prngDisplay = $metadata.prngCount.display -replace '\+$', ''
$toolsDisplay = $metadata.editorToolCount.display -replace '\+$', ''

Write-Host "Syncing documentation counts:"
Write-Host "  Tests:        $($metadata.testCount.exact) -> $($metadata.testCount.display)"
Write-Host "  PRNGs:        $($metadata.prngCount.exact) -> $($metadata.prngCount.display)"
Write-Host "  Editor tools: $($metadata.editorToolCount.exact) -> $($metadata.editorToolCount.display)"
Write-Host ""

# Explicit list of user-facing documentation files to sync.
# This prevents false positives in skill files, session logs, etc.
$targetFiles = @(
    'README.md',
    'llms.txt',
    '_config.yml',
    'index.md',
    '.llm/context.md',
    '.llm/skills/documentation-consistency.md',
    'docs/readme.md',
    'docs/index.md',
    'docs/overview/getting-started.md',
    'docs/overview/roadmap.md',
    'docs/overview/index.md',
    'docs/features/utilities/random-generators.md',
    'docs/features/editor-tools/editor-tools-guide.md',
    'docs/features/inspector/inspector-overview.md',
    'docs/images/unity-helpers-banner.svg'
) | ForEach-Object { Join-Path $repoRoot $_ } | Where-Object { Test-Path $_ }

$hasChanges = $false
$updatedFiles = @()

foreach ($filePath in $targetFiles) {
    $content = Get-Content -Path $filePath -Raw
    if (-not $content) { continue }
    $original = $content

    # Use MatchEvaluator (script block) for replacements to avoid
    # .NET regex backreference ambiguity with multi-digit group numbers.

    # ==========================================================
    # Test count replacements
    # ==========================================================
    # "8,000+ tests", "11,000+ automated tests", "### 11,000+ Tests"
    $content = [regex]::Replace($content,
        '(\d[\d,]*)\+(\s+(?:automated\s+)?test(?:s?\b|\s+cases?\b))',
        { param($m) "${testDisplay}+" + $m.Groups[2].Value },
        [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

    # ==========================================================
    # PRNG count replacements
    # Covered phrasings (add new regex if a new phrasing is introduced):
    #   - "N+ PRNG implementations"
    #   - "N+ high-performance PRNG implementations"
    #   - "N+ high-performance pseudo-random number generators"
    #   - "N+ high-quality RNG implementations"
    #   - "N+ high-quality PRNGs"
    #   - "N+ high-quality random number generators"
    # ==========================================================
    # "15+ PRNG implementations"
    $content = [regex]::Replace($content,
        '\b(\d+)\+?(\s+PRNG\s+implementations?\b)',
        { param($m) "${prngDisplay}+" + $m.Groups[2].Value })

    # "15+ high-performance PRNG implementations"
    $content = [regex]::Replace($content,
        '\b(\d+)\+?(\s+high-performance\s+PRNG\s+implementations?\b)',
        { param($m) "${prngDisplay}+" + $m.Groups[2].Value })

    # "15+ high-performance pseudo-random number generators (PRNGs)"
    $content = [regex]::Replace($content,
        '\b(\d+)\+?(\s+high-performance\s+pseudo-random\s+number\s+generators?\b)',
        { param($m) "${prngDisplay}+" + $m.Groups[2].Value })

    # "15+ high-quality RNG implementations"
    $content = [regex]::Replace($content,
        '\b(\d+)\+?(\s+high-quality\s+RNG\s+implementations?\b)',
        { param($m) "${prngDisplay}+" + $m.Groups[2].Value })

    # "20+ high-quality PRNGs"
    $content = [regex]::Replace($content,
        '\b(\d+)\+?(\s+high-quality\s+PRNGs?\b)',
        { param($m) "${prngDisplay}+" + $m.Groups[2].Value })

    # "15 high-quality random number generators" (exact count, with or without +)
    $content = [regex]::Replace($content,
        '\b(\d+)\+?(\s+high-quality\s+random\s+number\s+generators?\b)',
        { param($m) "${prngDisplay}+" + $m.Groups[2].Value })

    # ==========================================================
    # Editor tools count replacements
    # Covered phrasings (add new regex if a new phrasing is introduced):
    #   - "N+ editor tools"
    #   - "N+ automation tools"
    #   - "N+ tools for/that ..."
    #   - "N+ sprite/texture/prefab utilities"
    # ==========================================================
    # "20+ editor tools"
    $content = [regex]::Replace($content,
        '\b(\d+)\+(\s+editor\s+tools?\b)',
        { param($m) "${toolsDisplay}+" + $m.Groups[2].Value })

    # "20+ automation tools"
    $content = [regex]::Replace($content,
        '\b(\d+)\+(\s+automation\s+tools?\b)',
        { param($m) "${toolsDisplay}+" + $m.Groups[2].Value })

    # "20+ tools for/that"
    $content = [regex]::Replace($content,
        '\b(\d+)\+(\s+tools?\s+(?:for|that)\b)',
        { param($m) "${toolsDisplay}+" + $m.Groups[2].Value })

    # "20+ sprite/texture/prefab utilities"
    $content = [regex]::Replace($content,
        '\b(\d+)\+(\s+sprite/texture/prefab\s+utilities?\b)',
        { param($m) "${toolsDisplay}+" + $m.Groups[2].Value })

    # ==========================================================
    # SVG-specific replacements (for banner)
    # ==========================================================
    if ($filePath -match '\.svg$') {
        $content = [regex]::Replace($content,
            '(\d[\d,]*)\+(\s+tests)',
            { param($m) "${testDisplay}+" + $m.Groups[2].Value })
    }

    # Check if content changed
    if ($content -ne $original) {
        $relativePath = $filePath.Substring($repoRoot.Length + 1) -replace '\\', '/'
        if ($Check) {
            Write-Host "  MISMATCH: $relativePath" -ForegroundColor Yellow
            $hasChanges = $true
        }
        else {
            Set-Content -Path $filePath -Value $content -NoNewline -Encoding UTF8
            Write-Host "  Updated: $relativePath" -ForegroundColor Green
            $updatedFiles += $filePath
        }
    }
}

if ($Check) {
    Write-Host ""
    if ($hasChanges) {
        Write-Host "Documentation counts are out of sync." -ForegroundColor Red
        Write-Host "Run: pwsh -NoProfile -File scripts/sync-doc-counts.ps1" -ForegroundColor Cyan
        exit 1
    }
    else {
        Write-Host "All documentation counts are in sync." -ForegroundColor Green
        exit 0
    }
}

if ($updatedFiles.Count -gt 0) {
    Write-Host ""
    Write-Host "Updated $($updatedFiles.Count) file(s)." -ForegroundColor Green
}
else {
    Write-Host "All documentation counts are already in sync." -ForegroundColor Green
}

exit 0
