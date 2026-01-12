Param(
  [switch]$VerboseOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info($msg) {
  if ($VerboseOutput) { Write-Host "[skills-index] $msg" -ForegroundColor Cyan }
}

function Write-WarningMsg($msg) {
  Write-Host "[skills-index] WARNING: $msg" -ForegroundColor Yellow
}

function Write-ErrorMsg($msg) {
  Write-Host "[skills-index] ERROR: $msg" -ForegroundColor Red
}

function Write-SuccessMsg($msg) {
  Write-Host "[skills-index] $msg" -ForegroundColor Green
}

# Convert kebab-case to Title Case
function ConvertTo-TitleCase($kebab) {
  $words = $kebab -split '-'
  $titled = $words | ForEach-Object {
    if ($_.Length -gt 0) {
      $_.Substring(0, 1).ToUpper() + $_.Substring(1).ToLower()
    }
  }
  return $titled -join ' '
}

Write-Info "Starting skills index generation..."

$repoRoot = (Get-Item $PSScriptRoot).Parent.FullName
$skillsDir = Join-Path -Path $repoRoot -ChildPath '.llm/skills'

if (-not (Test-Path $skillsDir)) {
  Write-ErrorMsg "Skills directory not found at: $skillsDir"
  exit 1
}

Write-Info "Found skills directory at: $skillsDir"

$skillFiles = Get-ChildItem -Path $skillsDir -Filter '*.md' | Sort-Object Name
Write-Info "Found $($skillFiles.Count) skill files"

$skills = @()
$warnings = @()

foreach ($file in $skillFiles) {
  $filename = $file.BaseName
  $content = Get-Content -Path $file.FullName -Raw
  $lines = Get-Content -Path $file.FullName

  Write-Info "Processing: $filename"

  # Look for trigger comment: <!-- trigger: keywords | description -->
  $triggerComment = $null
  $keywords = @()
  $description = $null
  $category = 'Feature'  # Default category

  # Pattern to match the trigger comment
  if ($content -match '<!--\s*trigger:\s*([^|]+)\s*\|\s*([^-]+)\s*-->' -or
      $content -match '<!--\s*trigger:\s*([^|]+)\s*\|\s*(.+?)\s*-->') {
    $keywordsRaw = $Matches[1].Trim()
    $description = $Matches[2].Trim()
    $keywords = ($keywordsRaw -split ',') | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }
    Write-Info "  Found trigger comment: keywords=[$keywordsRaw], desc=[$description]"
  }
  else {
    $warnings += "No trigger comment in: $filename.md"
    Write-WarningMsg "No trigger comment in: $filename.md"

    # Fallback: try to extract from **Trigger**: line
    foreach ($line in $lines) {
      if ($line -match '^\*\*Trigger\*\*:\s*(.+)$') {
        $description = $Matches[1].Trim()
        # Remove markdown formatting
        $description = $description -replace '\*\*', ''
        # Truncate if too long
        if ($description.Length -gt 80) {
          $description = $description.Substring(0, 77) + '...'
        }
        Write-Info "  Fallback: extracted description from **Trigger**: line"
        break
      }
    }

    if (-not $description) {
      $description = '(No description)'
    }
  }

  # Determine category from trigger comment if present
  # Format can be: <!-- trigger: keywords | description | category -->
  if ($content -match '<!--\s*trigger:\s*([^|]+)\s*\|\s*([^|]+)\s*\|\s*(\w+)\s*-->') {
    $keywordsRaw = $Matches[1].Trim()
    $description = $Matches[2].Trim()
    $category = $Matches[3].Trim()
    $keywords = ($keywordsRaw -split ',') | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }
    Write-Info "  Found category: $category"
  }

  $skills += @{
    Filename = $filename
    DisplayName = ConvertTo-TitleCase $filename
    Keywords = $keywords
    Description = $description
    Category = $category
    FilePath = "./skills/$filename.md"
  }
}

# Group skills by category
$coreSkills = @($skills | Where-Object { $_.Category -eq 'Core' })
$perfSkills = @($skills | Where-Object { $_.Category -eq 'Performance' })
$featureSkills = @($skills | Where-Object { $_.Category -eq 'Feature' })

# Generate output
$output = @()

$output += "<!-- BEGIN GENERATED SKILLS INDEX -->"
$output += "<!-- Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') UTC -->"
$output += "<!-- Command: pwsh -NoProfile -File scripts/generate-skills-index.ps1 -->"
$output += ""

# Core Skills section
if ($coreSkills.Count -gt 0) {
  $output += "### Core Skills (Always Consider)"
  $output += ""
  $output += "| Skill | When to Use |"
  $output += "| ----- | ----------- |"
  foreach ($skill in $coreSkills | Sort-Object { $_.Filename }) {
    $link = "[$($skill.Filename)]($($skill.FilePath))"
    $output += "| $link | $($skill.Description) |"
  }
  $output += ""
}

# Performance Skills section
if ($perfSkills.Count -gt 0) {
  $output += "### Performance Skills"
  $output += ""
  $output += "| Skill | When to Use |"
  $output += "| ----- | ----------- |"
  foreach ($skill in $perfSkills | Sort-Object { $_.Filename }) {
    $link = "[$($skill.Filename)]($($skill.FilePath))"
    $output += "| $link | $($skill.Description) |"
  }
  $output += ""
}

# Feature Skills section
if ($featureSkills.Count -gt 0) {
  $output += "### Feature Skills"
  $output += ""
  $output += "| Skill | When to Use |"
  $output += "| ----- | ----------- |"
  foreach ($skill in $featureSkills | Sort-Object { $_.Filename }) {
    $link = "[$($skill.Filename)]($($skill.FilePath))"
    $output += "| $link | $($skill.Description) |"
  }
  $output += ""
}

$output += "<!-- END GENERATED SKILLS INDEX -->"

# Output to stdout
$output | ForEach-Object { Write-Output $_ }

# Summary to stderr
Write-Host "" -NoNewline
Write-Host ("=" * 60)
if ($warnings.Count -gt 0) {
  Write-Host ""
  Write-WarningMsg "Found $($warnings.Count) skill(s) without trigger comments:"
  foreach ($w in $warnings) {
    Write-Host "  - $w" -ForegroundColor Yellow
  }
}
Write-Host ""
Write-SuccessMsg "Generated skills index:"
Write-Info "  Core skills: $($coreSkills.Count)"
Write-Info "  Performance skills: $($perfSkills.Count)"
Write-Info "  Feature skills: $($featureSkills.Count)"
Write-Info "  Total: $($skills.Count)"
Write-Host ("=" * 60)
