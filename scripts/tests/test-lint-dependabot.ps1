Param(
  [switch]$VerboseOutput
)

<#
.SYNOPSIS
    Test runner for lint-dependabot.ps1

.DESCRIPTION
    Tests that lint-dependabot.ps1 correctly:
    - Passes a fully valid Dependabot v2 config
    - Passes a config with multiple named groups
    - Passes when validating multiple valid files at once
    - Fails when one of multiple provided files is invalid
    - Detects DEP001 (missing version: 2)
    - Detects DEP001 when version: 2 appears after updates: instead of before
    - Detects DEP002 (multi-ecosystem-groups: top-level key)
    - Detects DEP003 (multi-ecosystem-group: inside an entry)
    - Detects DEP004 (patterns: at entry level instead of inside groups)
    - Detects DEP005 (missing schedule: from an entry)
    - Detects DEP006 (groups entry missing patterns:)
    - Fails on the exact broken config that was previously shipped

.PARAMETER VerboseOutput
    Show detailed output during test execution

.EXAMPLE
    ./scripts/tests/test-lint-dependabot.ps1
    ./scripts/tests/test-lint-dependabot.ps1 -VerboseOutput
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:TestsPassed = 0
$script:TestsFailed = 0
$script:FailedTests = @()

function Write-Info($msg) {
    if ($VerboseOutput) { Write-Host "[test-lint-dependabot] $msg" -ForegroundColor Cyan }
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

$lintScriptPath = Join-Path $PSScriptRoot '..' 'lint-dependabot.ps1'

# Use a temp directory for fixture files
$tempBase = if ($env:TEMP) { $env:TEMP } elseif ($env:TMPDIR) { $env:TMPDIR } else { '/tmp' }
$tempDir = Join-Path $tempBase "test-lint-dependabot-$(Get-Random)"
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

Write-Host "Testing lint-dependabot.ps1..." -ForegroundColor White

# Helper: run the lint script against a fixture string, return exit code + output
function Invoke-LintOnContent {
    param([string]$YamlContent)
    $fixturePath = Join-Path $tempDir "dependabot-$(Get-Random).yml"
    Set-Content -Path $fixturePath -Value $YamlContent -NoNewline
    Write-Info "Testing fixture: $fixturePath"
    $output = & $lintScriptPath -- $fixturePath *>&1
    $exitCode = $LASTEXITCODE
    return @{ ExitCode = $exitCode; Output = ($output | Out-String) }
}

# Helper: run the lint script against two fixture strings, return exit code + output
function Invoke-LintOnTwoContents {
    param([string]$YamlContent1, [string]$YamlContent2)
    $fixturePath1 = Join-Path $tempDir "dependabot-$(Get-Random).yml"
    $fixturePath2 = Join-Path $tempDir "dependabot-$(Get-Random).yml"
    Set-Content -Path $fixturePath1 -Value $YamlContent1 -NoNewline
    Set-Content -Path $fixturePath2 -Value $YamlContent2 -NoNewline
    Write-Info "Testing fixtures: $fixturePath1 and $fixturePath2"
    $output = & $lintScriptPath -- $fixturePath1 $fixturePath2 *>&1
    $exitCode = $LASTEXITCODE
    return @{ ExitCode = $exitCode; Output = ($output | Out-String) }
}

try {

# ── Pass_ValidConfig ─────────────────────────────────────────────────────────
Write-Host "`n  Section: Valid configurations" -ForegroundColor White

$validConfig = @'
version: 2
updates:
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
    groups:
      all-dependencies:
        patterns:
          - "*"
    assignees:
      - wallstop
    reviewers:
      - wallstop
'@

$result = Invoke-LintOnContent $validConfig
Write-TestResult "Pass_ValidConfig" ($result.ExitCode -eq 0) "Expected exit 0, got $($result.ExitCode). Output: $($result.Output)"

# ── Pass_GroupsWithPatterns ───────────────────────────────────────────────────
$groupsConfig = @'
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "daily"
    groups:
      production-dependencies:
        patterns:
          - "Microsoft.*"
      development-dependencies:
        patterns:
          - "xunit*"
    assignees:
      - wallstop
'@

$result = Invoke-LintOnContent $groupsConfig
Write-TestResult "Pass_GroupsWithPatterns" ($result.ExitCode -eq 0) "Expected exit 0, got $($result.ExitCode). Output: $($result.Output)"

# ── Pass_MultipleValidFiles ───────────────────────────────────────────────────
$validConfig2 = @'
version: 2
updates:
  - package-ecosystem: "npm"
    directory: "/"
    schedule:
      interval: "weekly"
    groups:
      all-dependencies:
        patterns:
          - "*"
'@

$result = Invoke-LintOnTwoContents $validConfig $validConfig2
Write-TestResult "Pass_MultipleValidFiles" ($result.ExitCode -eq 0) "Expected exit 0 for two valid files, got $($result.ExitCode). Output: $($result.Output)"

# ── Fail_MultipleFilesOneInvalid ──────────────────────────────────────────────
$invalidForMulti = @'
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    assignees:
      - wallstop
'@

$result = Invoke-LintOnTwoContents $validConfig $invalidForMulti
$hasDEP005multi = $result.Output -match 'DEP005'
Write-TestResult "Fail_MultipleFilesOneInvalid" ($result.ExitCode -ne 0 -and $hasDEP005multi) "Expected non-zero + DEP005 when one of two files is invalid. Exit: $($result.ExitCode), Output: $($result.Output)"

# ── Fail_MissingVersion ───────────────────────────────────────────────────────
Write-Host "`n  Section: Error detection" -ForegroundColor White

$noVersionConfig = @'
updates:
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
'@

$result = Invoke-LintOnContent $noVersionConfig
$hasDEP001 = $result.Output -match 'DEP001'
Write-TestResult "Fail_MissingVersion" ($result.ExitCode -ne 0 -and $hasDEP001) "Expected non-zero + DEP001. Exit: $($result.ExitCode), Output: $($result.Output)"

# ── Fail_VersionAfterUpdates ──────────────────────────────────────────────────
# version: 2 must appear BEFORE updates:, not after
$versionAfterUpdatesConfig = @'
updates:
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
version: 2
'@

$result = Invoke-LintOnContent $versionAfterUpdatesConfig
$hasDEP001pos = $result.Output -match 'DEP001'
Write-TestResult "Fail_VersionAfterUpdates" ($result.ExitCode -ne 0 -and $hasDEP001pos) "Expected non-zero + DEP001 when version: 2 is after updates:. Exit: $($result.ExitCode), Output: $($result.Output)"

# ── Fail_MultiEcosystemGroups ─────────────────────────────────────────────────
$multiGroupsConfig = @'
version: 2

multi-ecosystem-groups:
  all-dependencies:
    schedule:
      interval: weekly

updates:
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
'@

$result = Invoke-LintOnContent $multiGroupsConfig
$hasDEP002 = $result.Output -match 'DEP002'
Write-TestResult "Fail_MultiEcosystemGroups" ($result.ExitCode -ne 0 -and $hasDEP002) "Expected non-zero + DEP002. Exit: $($result.ExitCode), Output: $($result.Output)"

# ── Fail_MultiEcosystemGroupKey ───────────────────────────────────────────────
$multiGroupKeyConfig = @'
version: 2
updates:
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
    multi-ecosystem-group: all-dependencies
'@

$result = Invoke-LintOnContent $multiGroupKeyConfig
$hasDEP003 = $result.Output -match 'DEP003'
Write-TestResult "Fail_MultiEcosystemGroupKey" ($result.ExitCode -ne 0 -and $hasDEP003) "Expected non-zero + DEP003. Exit: $($result.ExitCode), Output: $($result.Output)"

# ── Fail_PatternsAtWrongLevel ─────────────────────────────────────────────────
$patternsWrongConfig = @'
version: 2
updates:
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
    patterns:
      - "*"
'@

$result = Invoke-LintOnContent $patternsWrongConfig
$hasDEP004 = $result.Output -match 'DEP004'
Write-TestResult "Fail_PatternsAtWrongLevel" ($result.ExitCode -ne 0 -and $hasDEP004) "Expected non-zero + DEP004. Exit: $($result.ExitCode), Output: $($result.Output)"

# ── Fail_MissingSchedule ──────────────────────────────────────────────────────
$noScheduleConfig = @'
version: 2
updates:
  - package-ecosystem: "github-actions"
    directory: "/"
    assignees:
      - wallstop
'@

$result = Invoke-LintOnContent $noScheduleConfig
$hasDEP005 = $result.Output -match 'DEP005'
Write-TestResult "Fail_MissingSchedule" ($result.ExitCode -ne 0 -and $hasDEP005) "Expected non-zero + DEP005. Exit: $($result.ExitCode), Output: $($result.Output)"

# ── Fail_GroupsMissingPatterns ────────────────────────────────────────────────
$groupsNoPatternsConfig = @'
version: 2
updates:
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
    groups:
      all-dependencies:
        apply-security-updates-only: true
'@

$result = Invoke-LintOnContent $groupsNoPatternsConfig
$hasDEP006 = $result.Output -match 'DEP006'
Write-TestResult "Fail_GroupsMissingPatterns" ($result.ExitCode -ne 0 -and $hasDEP006) "Expected non-zero + DEP006. Exit: $($result.ExitCode), Output: $($result.Output)"

# ── Regression_OriginalBrokenConfig ──────────────────────────────────────────
Write-Host "`n  Section: Regression" -ForegroundColor White

$brokenConfig = @'
version: 2

multi-ecosystem-groups:
  all-dependencies:
    schedule:
      interval: weekly

updates:
  # GitHub Actions, NuGet, and npm updates are grouped into one large PR.
  - package-ecosystem: github-actions
    directory: /
    assignees:
      - wallstop
    reviewers:
      - wallstop
    patterns:
      - "*"
    multi-ecosystem-group: all-dependencies

  - package-ecosystem: nuget
    directory: /
    assignees:
      - wallstop
    reviewers:
      - wallstop
    patterns:
      - "*"
    multi-ecosystem-group: all-dependencies

  - package-ecosystem: npm
    directory: /
    versioning-strategy: increase
    assignees:
      - wallstop
    reviewers:
      - wallstop
    patterns:
      - "*"
    multi-ecosystem-group: all-dependencies
'@

$result = Invoke-LintOnContent $brokenConfig
$hasDEP002r = $result.Output -match 'DEP002'
$hasDEP003r = $result.Output -match 'DEP003'
$hasDEP004r = $result.Output -match 'DEP004'
$hasDEP005r = $result.Output -match 'DEP005'
$allDetected = $hasDEP002r -and $hasDEP003r -and $hasDEP004r -and $hasDEP005r
Write-TestResult "Regression_OriginalBrokenConfig" ($result.ExitCode -ne 0 -and $allDetected) (
    "Expected non-zero with DEP002/DEP003/DEP004/DEP005. Exit: $($result.ExitCode). " +
    "DEP002=$hasDEP002r DEP003=$hasDEP003r DEP004=$hasDEP004r DEP005=$hasDEP005r. Output: $($result.Output)"
)

} finally {
    Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue
}

# ── Summary ──────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host ("Tests passed: {0}" -f $script:TestsPassed) -ForegroundColor Green
Write-Host ("Tests failed: {0}" -f $script:TestsFailed) -ForegroundColor $(if ($script:TestsFailed -gt 0) { "Red" } else { "Green" })

if ($script:FailedTests.Count -gt 0) {
    Write-Host "Failed tests:" -ForegroundColor Red
    foreach ($t in $script:FailedTests) {
        Write-Host "  - $t" -ForegroundColor Red
    }
}

exit $script:TestsFailed
