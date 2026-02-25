Param(
  [switch]$VerboseOutput
)

<#
.SYNOPSIS
    Tests for sync-issue-template-versions.ps1 and pre-commit CLI safety.

.DESCRIPTION
    Validates:
    1. System.Version.ToString(3) produces exactly 3-component versions (no ".0" suffix)
    2. Pre-commit hook uses end-of-options separator (--) before all file arrays

.PARAMETER VerboseOutput
    Show detailed output during test execution

.EXAMPLE
    ./scripts/tests/test-sync-issue-template-versions.ps1
    ./scripts/tests/test-sync-issue-template-versions.ps1 -VerboseOutput
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:TestsPassed = 0
$script:TestsFailed = 0
$script:FailedTests = @()

function Write-Info($msg) {
  if ($VerboseOutput) { Write-Host "[test-sync-issue-template-versions] $msg" -ForegroundColor Cyan }
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
  } else {
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

# ── Version.ToString(3) Tests ──────────────────────────────────────────────

function Run-VersionToStringTests {
  Write-Host ""
  Write-Host "Version.ToString(3) format preservation:" -ForegroundColor Magenta
  Write-Host ""

  # Test 1: 3-component version stays 3-component
  $version = [System.Version]::new("3.2.0")
  $result = $version.ToString(3)
  Write-Info "Version '3.2.0' -> ToString(3) = '$result'"
  Write-TestResult -TestName "ToString(3) of '3.2.0' returns '3.2.0'" `
    -Passed ($result -ceq "3.2.0") `
    -Message "Expected '3.2.0', got '$result'"

  # Test 2: Verify default ToString() can produce 4 components (the bug scenario)
  $version4 = [System.Version]::new(3, 2, 0, 0)
  $defaultStr = $version4.ToString()
  Write-Info "Version(3,2,0,0) -> ToString() = '$defaultStr'"
  Write-TestResult -TestName "Default ToString() of 4-component version produces 4 parts" `
    -Passed ($defaultStr -ceq "3.2.0.0") `
    -Message "Expected '3.2.0.0', got '$defaultStr'"

  # Test 3: ToString(3) always trims to 3 components
  $result3 = $version4.ToString(3)
  Write-Info "Version(3,2,0,0) -> ToString(3) = '$result3'"
  Write-TestResult -TestName "ToString(3) of 4-component version returns only 3 parts" `
    -Passed ($result3 -ceq "3.2.0") `
    -Message "Expected '3.2.0', got '$result3'"

  # Test 4: Various real-world versions
  $testVersions = @("1.0.0", "0.9.1", "10.20.30", "2.0.0")
  foreach ($v in $testVersions) {
    $parsed = [System.Version]::new($v)
    $str = $parsed.ToString(3)
    Write-Info "Version '$v' -> ToString(3) = '$str'"
    Write-TestResult -TestName "ToString(3) roundtrip for '$v'" `
      -Passed ($str -ceq $v) `
      -Message "Expected '$v', got '$str'"
  }
}

# ── Pre-commit End-of-Options Tests ────────────────────────────────────────

function Run-PreCommitEndOfOptionsTests {
  Write-Host ""
  Write-Host "Pre-commit hook end-of-options (--) safety:" -ForegroundColor Magenta
  Write-Host ""

  $repoRoot = Get-RepoRoot
  $preCommitPath = Join-Path $repoRoot '.githooks' 'pre-commit'

  if (-not (Test-Path $preCommitPath)) {
    Write-Host "  [SKIP] Pre-commit hook not found at: $preCommitPath" -ForegroundColor Yellow
    return
  }

  $content = Get-Content $preCommitPath -Raw

  # All prettier invocations should have -- before file arrays
  $prettierLines = @(Get-Content $preCommitPath | Where-Object { $_ -match 'npx\s+--no-install\s+prettier' })
  Write-Info "Found $($prettierLines.Count) prettier invocation(s)"

  $allPrettierSafe = $true
  $unsafePrettierLines = @()
  foreach ($line in $prettierLines) {
    if ($line -notmatch '--\s+"\$\{') {
      $allPrettierSafe = $false
      $unsafePrettierLines += $line.Trim()
    }
  }
  Write-TestResult -TestName "All prettier invocations use -- before file arrays" `
    -Passed $allPrettierSafe `
    -Message "Unsafe lines: $($unsafePrettierLines -join '; ')"

  # All markdownlint invocations should have -- before file arrays
  $markdownlintLines = @(Get-Content $preCommitPath | Where-Object { $_ -match 'npx\s+--no-install\s+markdownlint' })
  Write-Info "Found $($markdownlintLines.Count) markdownlint invocation(s)"

  $allMarkdownlintSafe = $true
  $unsafeMarkdownlintLines = @()
  foreach ($line in $markdownlintLines) {
    if ($line -notmatch '--\s+"\$\{') {
      $allMarkdownlintSafe = $false
      $unsafeMarkdownlintLines += $line.Trim()
    }
  }
  Write-TestResult -TestName "All markdownlint invocations use -- before file arrays" `
    -Passed $allMarkdownlintSafe `
    -Message "Unsafe lines: $($unsafeMarkdownlintLines -join '; ')"

  # All yamllint invocations should have -- before file arrays
  $yamllintLines = @(Get-Content $preCommitPath | Where-Object { $_ -match 'yamllint\s+-c' })
  Write-Info "Found $($yamllintLines.Count) yamllint invocation(s)"

  $allYamllintSafe = $true
  $unsafeYamllintLines = @()
  foreach ($line in $yamllintLines) {
    if ($line -notmatch '--\s+"\$\{') {
      $allYamllintSafe = $false
      $unsafeYamllintLines += $line.Trim()
    }
  }
  Write-TestResult -TestName "All yamllint invocations use -- before file arrays" `
    -Passed $allYamllintSafe `
    -Message "Unsafe lines: $($unsafeYamllintLines -join '; ')"
}

function Run-PowerShellScriptEndOfOptionsTests {
  Write-Host ""
  Write-Host "PowerShell script end-of-options (--) safety:" -ForegroundColor Magenta
  Write-Host ""

  $repoRoot = Get-RepoRoot

  # Check format-staged-prettier.ps1 for '--' before file paths
  $prettierScript = Join-Path $repoRoot 'scripts' 'format-staged-prettier.ps1'
  if (Test-Path $prettierScript) {
    $prettierContent = Get-Content $prettierScript -Raw
    # Look for '--' in the prettier args array construction
    $prettierHasSeparator = $prettierContent -match "'--'" -and $prettierContent -match 'prettier'
    Write-TestResult -TestName "format-staged-prettier.ps1 uses '--' before file paths" `
      -Passed $prettierHasSeparator `
      -Message "Missing '--' separator in prettier argument construction"
  } else {
    Write-Host "  [SKIP] format-staged-prettier.ps1 not found at: $prettierScript" -ForegroundColor Yellow
  }

  # Check lint-staged-markdown.ps1 for '--' before file paths
  $markdownScript = Join-Path $repoRoot 'scripts' 'lint-staged-markdown.ps1'
  if (Test-Path $markdownScript) {
    $markdownContent = Get-Content $markdownScript -Raw
    # Look for '--' in the markdownlint args array construction
    $markdownHasSeparator = $markdownContent -match "'--'" -and $markdownContent -match 'markdownlint'
    Write-TestResult -TestName "lint-staged-markdown.ps1 uses '--' before file paths" `
      -Passed $markdownHasSeparator `
      -Message "Missing '--' separator in markdownlint argument construction"
  } else {
    Write-Host "  [SKIP] lint-staged-markdown.ps1 not found at: $markdownScript" -ForegroundColor Yellow
  }

  # Check lint-yaml.ps1 for '--' before file paths
  $yamlScript = Join-Path $repoRoot 'scripts' 'lint-yaml.ps1'
  if (Test-Path $yamlScript) {
    $yamlContent = Get-Content $yamlScript -Raw
    # Look for '--' in the yamllint args array construction
    $yamlHasSeparator = $yamlContent -match "'--'" -and $yamlContent -match 'yamllint'
    Write-TestResult -TestName "lint-yaml.ps1 uses '--' before file paths" `
      -Passed $yamlHasSeparator `
      -Message "Missing '--' separator in yamllint argument construction"
  } else {
    Write-Host "  [SKIP] lint-yaml.ps1 not found at: $yamlScript" -ForegroundColor Yellow
  }
}

# ── Main Execution ─────────────────────────────────────────────────────────

Write-Host ""
Write-Host "========================================" -ForegroundColor White
Write-Host "Sync Issue Template Versions Tests" -ForegroundColor White
Write-Host "========================================" -ForegroundColor White

Run-VersionToStringTests
Run-PreCommitEndOfOptionsTests
Run-PowerShellScriptEndOfOptionsTests

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor White
Write-Host "Test Summary" -ForegroundColor White
Write-Host "========================================" -ForegroundColor White
Write-Host ""

$totalTests = $script:TestsPassed + $script:TestsFailed

if ($script:TestsFailed -eq 0) {
  Write-Host "All $totalTests tests passed!" -ForegroundColor Green
  exit 0
} else {
  Write-Host "Passed: $($script:TestsPassed) / $totalTests" -ForegroundColor Yellow
  Write-Host "Failed: $($script:TestsFailed) / $totalTests" -ForegroundColor Red
  Write-Host ""
  Write-Host "Failed tests:" -ForegroundColor Red
  foreach ($test in $script:FailedTests) {
    Write-Host "  - $test" -ForegroundColor Red
  }
  exit 1
}
