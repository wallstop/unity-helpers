Param(
  [switch]$VerboseOutput
)

<#
.SYNOPSIS
    Test runner for npm package.json changelog metadata validation

.DESCRIPTION
    Validates that package.json contains the required changelog metadata for
    Unity Package Manager discoverability and that CHANGELOG.md contains an
    entry for the current package version or [Unreleased].
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:TestsPassed = 0
$script:TestsFailed = 0

function Write-Info($msg) {
  if ($VerboseOutput) { Write-Host "[test-npm-package-changelog] $msg" -ForegroundColor Cyan }
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

Write-Host "Testing package.json changelog metadata..." -ForegroundColor White

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$packageJsonPath = Join-Path $repoRoot 'package.json'
$changelogPath = Join-Path $repoRoot 'CHANGELOG.md'
Write-Info "Reading: $packageJsonPath"
Write-Info "Reading: $changelogPath"

if (-not (Test-Path $packageJsonPath)) {
  Write-TestResult -TestName 'package.json exists' -Passed $false -Message "Missing file: $packageJsonPath"
}
else {
  Write-TestResult -TestName 'package.json exists' -Passed $true
}

if (-not (Test-Path $changelogPath)) {
  Write-TestResult -TestName 'CHANGELOG.md exists' -Passed $false -Message "Missing file: $changelogPath"
}
else {
  Write-TestResult -TestName 'CHANGELOG.md exists' -Passed $true
}

$rawContent = ''
$parsedJson = $null
$jsonParseSucceeded = $false

if (Test-Path $packageJsonPath) {
  $rawContent = Get-Content -Path $packageJsonPath -Raw
  try {
    $parsedJson = $rawContent | ConvertFrom-Json
    $jsonParseSucceeded = $true
  }
  catch {
    $jsonParseSucceeded = $false
  }
}

Write-TestResult -TestName 'package.json is valid JSON' -Passed $jsonParseSucceeded

$changelogUrlValue = ''
$versionValue = ''

if ($jsonParseSucceeded) {
  $hasChangelogUrl = $null -ne $parsedJson.PSObject.Properties['changelogUrl']
  Write-TestResult -TestName 'package.json contains changelogUrl field' -Passed $hasChangelogUrl

  $hasVersion = $null -ne $parsedJson.PSObject.Properties['version']
  Write-TestResult -TestName 'package.json contains version field' -Passed $hasVersion

  if ($hasVersion) {
    $versionValue = [string]$parsedJson.version
    Write-TestResult -TestName 'version field is non-empty' -Passed (-not [string]::IsNullOrWhiteSpace($versionValue))
  }

  if ($hasChangelogUrl) {
    $changelogUrlValue = [string]$parsedJson.changelogUrl
    Write-TestResult -TestName 'changelogUrl is non-empty' -Passed (-not [string]::IsNullOrWhiteSpace($changelogUrlValue))

    $changelogUri = $null
    $isValidHttpsUri = [uri]::TryCreate($changelogUrlValue, [System.UriKind]::Absolute, [ref]$changelogUri) -and $changelogUri.Scheme -eq 'https'
    Write-TestResult -TestName 'changelogUrl is valid absolute HTTPS URL' -Passed $isValidHttpsUri
    Write-TestResult -TestName 'changelogUrl targets changelog resource' -Passed ($changelogUrlValue -match '(?i)CHANGELOG(\.md)?([?#].*)?$')
  }
}

$changelogKeyMatches = [regex]::Matches($rawContent, '"changelogUrl"\s*:')
Write-TestResult -TestName 'package.json defines changelogUrl once' -Passed ($changelogKeyMatches.Count -eq 1) -Message "Found $($changelogKeyMatches.Count) changelogUrl keys"

if ((Test-Path $changelogPath) -and (-not [string]::IsNullOrWhiteSpace($versionValue))) {
  $changelogRaw = Get-Content -Path $changelogPath -Raw
  $hasVersionHeader = $changelogRaw -match ("(?mi)^##\s+\[" + [regex]::Escape($versionValue) + "\](?:\s+-.*)?\s*$")
  $hasUnreleasedHeader = $changelogRaw -match '(?m)^##\s+\[Unreleased\]\s*$'
  Write-TestResult -TestName 'CHANGELOG.md has current version section or [Unreleased]' -Passed ($hasVersionHeader -or $hasUnreleasedHeader) -Message "Expected section: ## [$versionValue] or ## [Unreleased]"
}

Write-Host ""
Write-Host "Results:" -ForegroundColor Magenta
Write-Host "  Passed: $script:TestsPassed"
Write-Host "  Failed: $script:TestsFailed"

if ($script:TestsFailed -gt 0) {
  exit 1
}

exit 0
