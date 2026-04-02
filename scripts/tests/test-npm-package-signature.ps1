Param(
  [switch]$VerboseOutput
)

<#
.SYNOPSIS
    Test runner for npm package.json signature field validation

.DESCRIPTION
    Validates that package.json contains the required "signature" field with
    value "unsigned", ensuring package integrity metadata presence.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:TestsPassed = 0
$script:TestsFailed = 0

function Write-Info($msg) {
  if ($VerboseOutput) { Write-Host "[test-npm-package-signature] $msg" -ForegroundColor Cyan }
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
  }
}

Write-Host "Testing package.json signature metadata..." -ForegroundColor White

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$packageJsonPath = Join-Path $repoRoot 'package.json'
Write-Info "Reading: $packageJsonPath"

if (-not (Test-Path $packageJsonPath)) {
  Write-TestResult -TestName 'package.json exists' -Passed $false -Message "Missing file: $packageJsonPath"
  Write-Host ""
  Write-Host "Results:" -ForegroundColor Magenta
  Write-Host "  Passed: $script:TestsPassed"
  Write-Host "  Failed: $script:TestsFailed"
  exit 1
}

$rawContent = Get-Content -Path $packageJsonPath -Raw
$parsedJson = $null
$jsonParseSucceeded = $true
try {
  $parsedJson = $rawContent | ConvertFrom-Json
}
catch {
  $jsonParseSucceeded = $false
}
Write-TestResult -TestName 'package.json is valid JSON' -Passed $jsonParseSucceeded

if ($jsonParseSucceeded) {
  $hasSignature = $null -ne $parsedJson.PSObject.Properties['signature']
  Write-TestResult -TestName 'package.json contains signature field' -Passed $hasSignature

  if ($hasSignature) {
    $signatureValue = $parsedJson.signature
    $signatureIsString = $signatureValue -is [string]
    Write-TestResult -TestName 'signature field is a string' -Passed $signatureIsString
    Write-TestResult -TestName 'signature field is set to unsigned' -Passed ($signatureValue -eq 'unsigned') -Message "Actual value: '$signatureValue'"
  }
}

$signatureMatches = [regex]::Matches($rawContent, '"signature"\s*:')
Write-TestResult -TestName 'package.json defines signature once' -Passed ($signatureMatches.Count -eq 1) -Message "Found $($signatureMatches.Count) signature keys"

Write-Host ""
Write-Host "Results:" -ForegroundColor Magenta
Write-Host "  Passed: $script:TestsPassed"
Write-Host "  Failed: $script:TestsFailed"

if ($script:TestsFailed -gt 0) {
  exit 1
}

exit 0
