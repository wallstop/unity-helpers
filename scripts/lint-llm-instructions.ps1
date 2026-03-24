<#
.SYNOPSIS
    Validates LLM instruction files (.llm/) are correct and up-to-date.

.DESCRIPTION
    This script validates:
    1. All skill files have proper trigger comments
    2. The skills index in context.md is up-to-date
    3. All referenced skill files exist
    4. No orphaned skill files (skills not in index)

.PARAMETER Fix
    If specified, regenerates the skills index to fix out-of-date issues.

.PARAMETER VerboseOutput
    If specified, outputs detailed information during validation.

.EXAMPLE
    pwsh -NoProfile -File scripts/lint-llm-instructions.ps1
    pwsh -NoProfile -File scripts/lint-llm-instructions.ps1 -Fix
#>

Param(
    [switch]$Fix,
    [switch]$VerboseOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info($msg) {
    if ($VerboseOutput) { Write-Host "[llm-lint] $msg" -ForegroundColor Cyan }
}

function Write-WarningMsg($msg) {
    Write-Host "[llm-lint] WARNING: $msg" -ForegroundColor Yellow
}

function Write-ErrorMsg($msg) {
    Write-Host "[llm-lint] ERROR: $msg" -ForegroundColor Red
}

function Write-SuccessMsg($msg) {
    Write-Host "[llm-lint] $msg" -ForegroundColor Green
}

$repoRoot = (Get-Item $PSScriptRoot).Parent.FullName
$skillsDir = Join-Path -Path $repoRoot -ChildPath '.llm/skills'
$contextFile = Join-Path -Path $repoRoot -ChildPath '.llm/context.md'

$exitCode = 0

# =============================================================================
# 1. Validate skills directory exists
# =============================================================================
Write-Info "Checking skills directory..."
if (-not (Test-Path $skillsDir)) {
    Write-ErrorMsg "Skills directory not found at: $skillsDir"
    exit 1
}

# =============================================================================
# 2. Validate context.md exists
# =============================================================================
Write-Info "Checking context.md..."
if (-not (Test-Path $contextFile)) {
    Write-ErrorMsg "context.md not found at: $contextFile"
    exit 1
}

# =============================================================================
# 3. Validate all skill files have trigger comments
# =============================================================================
Write-Host ""
Write-Host "Validating skill trigger comments..." -ForegroundColor Blue

$skillFiles = Get-ChildItem -Path $skillsDir -Filter '*.md' | Sort-Object Name
$missingTriggers = @()

foreach ($file in $skillFiles) {
    $content = Get-Content -Path $file.FullName -Raw
    
    # Check for trigger comment: <!-- trigger: keywords | description --> or <!-- trigger: keywords | description | category -->
    # Description may contain dashes and other characters
    # Pattern: <!-- trigger: <keywords> | <description> [| <category>] -->
    if ($content -notmatch '<!--\s*trigger:\s*[^|]+\|[^>]+-->') {
        $missingTriggers += $file.Name
    }
}

if ($missingTriggers.Count -gt 0) {
    Write-ErrorMsg "The following skill files are missing trigger comments:"
    foreach ($file in $missingTriggers) {
        Write-Host "  - $file" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "Required format: <!-- trigger: keyword1, keyword2 | Description | Category -->" -ForegroundColor Yellow
    Write-Host "Categories: Core, Performance, Feature (default: Feature)" -ForegroundColor Yellow
    $exitCode = 1
}
else {
    Write-SuccessMsg "All $($skillFiles.Count) skill files have trigger comments"
}

# =============================================================================
# 4. Validate skills index is up-to-date
# =============================================================================
Write-Host ""
Write-Host "Validating skills index..." -ForegroundColor Blue

# Generate expected index
$generateScript = Join-Path -Path $repoRoot -ChildPath 'scripts/generate-skills-index.ps1'
if (-not (Test-Path $generateScript)) {
    Write-ErrorMsg "generate-skills-index.ps1 not found at: $generateScript"
    exit 1
}

# Run the generator and capture output
$expectedIndex = & pwsh -NoProfile -File $generateScript 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-ErrorMsg "Generator script failed with exit code $LASTEXITCODE"
    exit 1
}

# Read current context.md
$contextContent = Get-Content -Path $contextFile -Raw

# Extract current index from context.md
$beginMarker = '<!-- BEGIN GENERATED SKILLS INDEX -->'
$endMarker = '<!-- END GENERATED SKILLS INDEX -->'

if ($contextContent -notmatch [regex]::Escape($beginMarker)) {
    Write-ErrorMsg "Missing BEGIN marker in context.md"
    exit 1
}

if ($contextContent -notmatch [regex]::Escape($endMarker)) {
    Write-ErrorMsg "Missing END marker in context.md"
    exit 1
}

# Extract current index
$pattern = "(?s)$([regex]::Escape($beginMarker))(.*)$([regex]::Escape($endMarker))"
if ($contextContent -match $pattern) {
    $currentIndex = $Matches[1].Trim()
}
else {
    Write-ErrorMsg "Could not extract skills index from context.md"
    exit 1
}

# Extract content between BEGIN/END markers from a raw string
function Extract-MarkerContent($raw) {
    # Normalize CRLF to LF for cross-platform consistency
    $raw = $raw -replace "`r`n", "`n"
    $beginMarkerLocal = '<!-- BEGIN GENERATED SKILLS INDEX -->'
    $endMarkerLocal = '<!-- END GENERATED SKILLS INDEX -->'
    $patternLocal = "(?s)$([regex]::Escape($beginMarkerLocal))(.*)$([regex]::Escape($endMarkerLocal))"
    if ($raw -match $patternLocal) {
        return $Matches[1].Trim()
    }
    return $null
}

# Normalize for comparison (remove timestamps, normalize whitespace)
function Normalize-Index($indexContent) {
    # Normalize CRLF to LF for cross-platform consistency
    $indexContent = $indexContent -replace "`r`n", "`n"
    $lines = $indexContent -split "`n" | 
        Where-Object { 
            $_ -notmatch '<!-- Generated:' -and 
            $_ -notmatch '<!-- Command:'
        } |
        ForEach-Object { 
            $line = $_.TrimEnd()
            # Normalize table rows: collapse multiple spaces to single space around pipe separators
            $line = $line -replace '\s+\|\s+', ' | '
            $line = $line -replace '\|\s+', '| '
            $line = $line -replace '\s+\|', ' |'
            # Collapse multiple consecutive dashes in separator rows
            $line = $line -replace '-{2,}', '-'
            $line
        } |
        Where-Object { $_ -ne '' }
    return ($lines -join "`n").Trim()
}

# Extract only the content between markers from the generated output
$expectedRaw = $expectedIndex -join "`n"
$expectedContent = Extract-MarkerContent $expectedRaw
if (-not $expectedContent) {
    Write-ErrorMsg "Generated output missing BEGIN/END markers"
    exit 1
}

$normalizedExpected = Normalize-Index $expectedContent
$normalizedCurrent = Normalize-Index $currentIndex

if ($normalizedExpected -ne $normalizedCurrent) {
    Write-ErrorMsg "Skills index in context.md is out of date!"
    Write-Host ""
    
    # Show first few differences to help diagnose
    $expectedLines = $normalizedExpected -split "`n"
    $currentLines = $normalizedCurrent -split "`n"
    $maxLines = [Math]::Max($expectedLines.Count, $currentLines.Count)
    $diffCount = 0
    for ($i = 0; $i -lt $maxLines -and $diffCount -lt 5; $i++) {
        $exp = if ($i -lt $expectedLines.Count) { $expectedLines[$i] } else { "(missing)" }
        $cur = if ($i -lt $currentLines.Count) { $currentLines[$i] } else { "(missing)" }
        if ($exp -ne $cur) {
            Write-Host "  Line $($i+1):" -ForegroundColor Yellow
            Write-Host "    Expected: $exp" -ForegroundColor Green
            Write-Host "    Current:  $cur" -ForegroundColor Red
            $diffCount++
        }
    }
    if ($diffCount -eq 0) {
        Write-Host "  Expected line count: $($expectedLines.Count)" -ForegroundColor Yellow
        Write-Host "  Current line count:  $($currentLines.Count)" -ForegroundColor Yellow
    }
    
    if ($Fix) {
        Write-Host "Regenerating skills index..." -ForegroundColor Yellow
        
        # Build the new content - expectedIndex already contains BEGIN/END markers
        # Filter to only lines between BEGIN and END markers (inclusive) to exclude
        # any summary/diagnostic output from the generator script
        $inBlock = $false
        $filteredLines = @()
        foreach ($line in $expectedIndex) {
            if ($line -match [regex]::Escape($beginMarker)) { $inBlock = $true }
            if ($inBlock) { $filteredLines += $line }
            if ($line -match [regex]::Escape($endMarker)) { $inBlock = $false; break }
        }
        $expectedFull = $filteredLines -join "`n"
        # Replace the entire block including markers with the new generated content
        $newContent = $contextContent -replace $pattern, $expectedFull

        # Trim trailing whitespace and add exactly one LF (Markdown files require LF per .editorconfig)
        $newContent = $newContent.TrimEnd() + "`n"
        Set-Content -Path $contextFile -Value $newContent -NoNewline -Encoding UTF8
        Write-SuccessMsg "Skills index has been regenerated in context.md"
        
        # Re-run prettier to format
        Write-Info "Running prettier to format context.md..."
        Push-Location $repoRoot
        try {
            npx --no-install prettier --write -- .llm/context.md 2>$null
            if ($LASTEXITCODE -ne 0) {
                Write-ErrorMsg "Prettier formatting failed (exit code $LASTEXITCODE). Rolling back changes..."
                Set-Content -Path $contextFile -Value $contextContent -NoNewline -Encoding UTF8
                exit 1
            }
        }
        catch {
            Write-ErrorMsg "Prettier formatting failed: $_. Rolling back changes..."
            Set-Content -Path $contextFile -Value $contextContent -NoNewline -Encoding UTF8
            exit 1
        }
        finally {
            Pop-Location
        }

        # Post-fix validation: ensure no NEW H1 headings were introduced (MD025)
        $originalH1Count = @($contextContent -split "`n" | Where-Object { $_ -match '^# ' }).Count
        $fixedContent = Get-Content -Path $contextFile
        $h1Lines = @($fixedContent | Where-Object { $_ -match '^# ' })
        if ($h1Lines.Count -gt $originalH1Count) {
            Write-ErrorMsg "Fix introduced new H1 headings (MD025 violation):"
            $h1Lines | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
            Write-ErrorMsg "Rolling back changes..."
            Set-Content -Path $contextFile -Value $contextContent -NoNewline -Encoding UTF8
            exit 1
        }
    }
    else {
        Write-Host "Run with -Fix to regenerate, or manually run:" -ForegroundColor Yellow
        Write-Host "  pwsh -NoProfile -File scripts/generate-skills-index.ps1" -ForegroundColor Cyan
        Write-Host "Then update context.md with the generated content." -ForegroundColor Yellow
        $exitCode = 1
    }
}
else {
    Write-SuccessMsg "Skills index is up to date"
}

# =============================================================================
# 5. Validate all skills in index exist as files
# =============================================================================
Write-Host ""
Write-Host "Validating skill file references..." -ForegroundColor Blue

$skillFileNames = $skillFiles | ForEach-Object { $_.BaseName }
$missingFiles = @()
$orphanedFiles = @()

# Extract skill names from the current index
$indexSkillPattern = '\[([a-z0-9-]+)\]\(\.\/skills\/([a-z0-9-]+)\.md\)'
$indexMatches = [regex]::Matches($currentIndex, $indexSkillPattern)
$indexedSkills = $indexMatches | ForEach-Object { $_.Groups[2].Value } | Sort-Object -Unique

# Check for missing files (referenced in index but don't exist)
foreach ($skill in $indexedSkills) {
    if ($skill -notin $skillFileNames) {
        $missingFiles += $skill
    }
}

# Check for orphaned files (exist but not in index)
foreach ($skillFile in $skillFileNames) {
    if ($skillFile -notin $indexedSkills) {
        $orphanedFiles += $skillFile
    }
}

if ($missingFiles.Count -gt 0) {
    Write-ErrorMsg "Skills referenced in index but files don't exist:"
    foreach ($skill in $missingFiles) {
        Write-Host "  - $skill.md" -ForegroundColor Red
    }
    $exitCode = 1
}

if ($orphanedFiles.Count -gt 0) {
    Write-WarningMsg "Skill files exist but not in index (may need trigger comments):"
    foreach ($skill in $orphanedFiles) {
        Write-Host "  - $skill.md" -ForegroundColor Yellow
    }
    # This is a warning, not an error - the trigger comment check above will catch it
}

if ($missingFiles.Count -eq 0 -and $orphanedFiles.Count -eq 0) {
    Write-SuccessMsg "All skill references are valid"
}

# =============================================================================
# Summary
# =============================================================================
Write-Host ""
Write-Host ("=" * 60)
if ($exitCode -eq 0) {
    Write-SuccessMsg "LLM instructions validation passed!"
}
else {
    Write-ErrorMsg "LLM instructions validation failed!"
}
Write-Host ("=" * 60)

exit $exitCode
