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

# Normalize both for comparison (remove markers, timestamps, summary lines, and normalize whitespace)
function Normalize-Index($indexLines) {
    $lines = $indexLines -split "`n" | 
        Where-Object { 
            $_ -notmatch '<!-- Generated:' -and 
            $_ -notmatch '<!-- Command:' -and
            $_ -notmatch '<!-- BEGIN GENERATED' -and
            $_ -notmatch '<!-- END GENERATED' -and
            $_ -notmatch '^\[skills-index\]' -and
            $_ -notmatch '^={10,}$' -and
            $_ -notmatch '^\s*#\s*$'
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

$normalizedExpected = Normalize-Index ($expectedIndex -join "`n")
$normalizedCurrent = Normalize-Index $currentIndex

if ($normalizedExpected -ne $normalizedCurrent) {
    Write-ErrorMsg "Skills index in context.md is out of date!"
    Write-Host ""
    
    if ($Fix) {
        Write-Host "Regenerating skills index..." -ForegroundColor Yellow
        
        # Build the new content - expectedIndex already contains BEGIN/END markers
        $expectedFull = $expectedIndex -join "`n"
        # Replace the entire block including markers with the new generated content
        $newContent = $contextContent -replace $pattern, $expectedFull
        
        # Write back to file
        Set-Content -Path $contextFile -Value $newContent -NoNewline
        Write-SuccessMsg "Skills index has been regenerated in context.md"
        
        # Re-run prettier to format
        Write-Info "Running prettier to format context.md..."
        Push-Location $repoRoot
        try {
            npx --no-install prettier --write -- .llm/context.md 2>$null
        }
        finally {
            Pop-Location
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
