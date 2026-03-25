Param(
  [switch]$VerboseOutput
)

<#
.SYNOPSIS
    Regression tests for sync script newline safety and cspell linter contract drift.

.DESCRIPTION
    Validates:
    1. Production scripts that write with Set-Content -NoNewline normalize content to a final LF.
    2. lint-cspell-config.js header-declared checks match implemented "Check N" sections.

.PARAMETER VerboseOutput
    Show detailed output during test execution.

.EXAMPLE
    pwsh -NoProfile -File scripts/tests/test-sync-script-contracts.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:TestsPassed = 0
$script:TestsFailed = 0
$script:FailedTests = @()

function Write-Info {
  param([string]$Message)
  if ($VerboseOutput) {
    Write-Host "[test-sync-script-contracts] $Message" -ForegroundColor Cyan
  }
}

function Write-TestResult {
  param(
    [string]$TestName,
    [bool]$Passed,
    [string]$Message = ""
  )

  if ($Passed) {
    Write-Host "  [PASS] $TestName" -ForegroundColor Green
    $script:TestsPassed++
  }
  else {
    Write-Host "  [FAIL] $TestName" -ForegroundColor Red
    if ($Message) {
      Write-Host "         $Message" -ForegroundColor Yellow
    }
    $script:TestsFailed++
    $script:FailedTests += $TestName
  }
}

function Get-RepoRoot {
  return (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
}

function Assert-NoNewlineWriteHasFinalLfNormalization {
  param(
    [string]$ScriptPath,
    [string]$ValueVariable,
    [string]$TestName
  )

  if (-not (Test-Path $ScriptPath)) {
    Write-TestResult -TestName $TestName -Passed $false -Message "Missing file: $ScriptPath"
    return
  }

  $lines = @(Get-Content -Path $ScriptPath)
  $setContentNeedle = '-Value $' + $ValueVariable + ' -NoNewline'
  $normalizationNeedle = '$' + $ValueVariable + ' = $' + $ValueVariable + '.TrimEnd() + "`n"'

  $matchingIndices = @()
  for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i].Contains('Set-Content') -and $lines[$i].Contains($setContentNeedle)) {
      $matchingIndices += $i
    }
  }

  if ($matchingIndices.Count -eq 0) {
    Write-TestResult -TestName $TestName -Passed $false -Message "No Set-Content -NoNewline write found for variable '$ValueVariable'."
    return
  }

  $violations = @()
  foreach ($index in $matchingIndices) {
    # Keep this window broad enough to tolerate nearby comment churn while still
    # requiring normalization to occur before the write call.
    $windowStart = [Math]::Max(0, $index - 10)
    $windowEnd = $index - 1
    $window = @()
    if ($windowStart -le $windowEnd) {
      $window = $lines[$windowStart..$windowEnd]
    }
    $hasNormalization = $false
    foreach ($windowLine in $window) {
      if ($windowLine.Contains($normalizationNeedle)) {
        $hasNormalization = $true
        break
      }
    }

    if (-not $hasNormalization) {
      $lineNumber = $index + 1
      $violations += "line $lineNumber"
    }
  }

  if ($violations.Count -gt 0) {
    Write-TestResult -TestName $TestName -Passed $false -Message "Missing final-LF normalization near $($violations -join ', ')."
  }
  else {
    Write-TestResult -TestName $TestName -Passed $true
  }
}

function Run-SyncScriptContractTests {
  Write-Host ""
  Write-Host "Sync script newline-safety contracts:" -ForegroundColor Magenta
  Write-Host ""

  $repoRoot = Get-RepoRoot

  Assert-NoNewlineWriteHasFinalLfNormalization `
    -ScriptPath (Join-Path $repoRoot 'scripts/sync-doc-counts.ps1') `
    -ValueVariable 'content' `
    -TestName 'sync-doc-counts.ps1 normalizes content before -NoNewline write'

  Assert-NoNewlineWriteHasFinalLfNormalization `
    -ScriptPath (Join-Path $repoRoot 'scripts/sync-banner-version.ps1') `
    -ValueVariable 'updatedContent' `
    -TestName 'sync-banner-version.ps1 normalizes SVG content before -NoNewline write'

  Assert-NoNewlineWriteHasFinalLfNormalization `
    -ScriptPath (Join-Path $repoRoot 'scripts/sync-banner-version.ps1') `
    -ValueVariable 'updatedContextContent' `
    -TestName 'sync-banner-version.ps1 normalizes context content before -NoNewline write'
}

function Run-CspellContractTests {
  Write-Host ""
  Write-Host "cspell config linter header/implementation contracts:" -ForegroundColor Magenta
  Write-Host ""

  $repoRoot = Get-RepoRoot
  $linterPath = Join-Path $repoRoot 'scripts/lint-cspell-config.js'

  if (-not (Test-Path $linterPath)) {
    Write-TestResult -TestName 'lint-cspell-config.js exists' -Passed $false -Message "Missing file: $linterPath"
    return
  }

  $lines = @(Get-Content -Path $linterPath)

  $headerCheckNumbers = @()
  $checkSectionNumbers = @()
  $mentionsRemovedCheck = $false

  $inHeader = $false
  foreach ($line in $lines) {
    $trimmed = $line.Trim()
    if ($trimmed -match '^//\s+Lints\s+cspell\.json\s+for\s+common\s+configuration\s+issues:') {
      $inHeader = $true
      continue
    }

    if ($inHeader -and $trimmed -match '^//\s+([0-9]+)\.\s+') {
      $headerCheckNumbers += [int]$Matches[1]
    }

    if ($inHeader -and -not $trimmed.StartsWith('//')) {
      $inHeader = $false
    }

    if ($trimmed -match '^//\s+[^\n]*?Check\s+([0-9]+):') {
      $checkSectionNumbers += [int]$Matches[1]
    }

    if ($trimmed -match 'Root words that belong in a categorized dictionary') {
      $mentionsRemovedCheck = $true
    }
  }

  $headerUnique = @($headerCheckNumbers | Sort-Object -Unique)
  $sectionsUnique = @($checkSectionNumbers | Sort-Object -Unique)

  Write-Info "Header checks: $($headerUnique -join ', ')"
  Write-Info "Section checks: $($sectionsUnique -join ', ')"

  $headerMatchesSections = ($headerUnique.Count -eq $sectionsUnique.Count) -and
    (@($headerUnique) -join ',') -ceq (@($sectionsUnique) -join ',')

  Write-TestResult `
    -TestName 'lint-cspell-config.js header check list matches implemented Check sections' `
    -Passed $headerMatchesSections `
    -Message "Header: [$($headerUnique -join ', ')], Sections: [$($sectionsUnique -join ', ')]"

  Write-TestResult `
    -TestName 'lint-cspell-config.js does not claim removed check 3 in header' `
    -Passed (-not $mentionsRemovedCheck) `
    -Message 'Found stale header text: "Root words that belong in a categorized dictionary"'
}

function Print-SummaryAndExit {
  Write-Host ""
  Write-Host "Results:" -ForegroundColor Magenta
  Write-Host "  Passed: $script:TestsPassed"
  Write-Host "  Failed: $script:TestsFailed"

  if ($script:TestsFailed -gt 0) {
    Write-Host ""
    Write-Host "Failed tests:" -ForegroundColor Red
    foreach ($failedTest in $script:FailedTests) {
      Write-Host "  - $failedTest" -ForegroundColor Yellow
    }
    exit 1
  }

  exit 0
}

Run-SyncScriptContractTests
Run-CspellContractTests
Print-SummaryAndExit
