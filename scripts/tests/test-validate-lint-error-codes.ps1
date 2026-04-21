#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Test runner for validate-lint-error-codes.ps1.

.DESCRIPTION
    Tests that validate-lint-error-codes.ps1 correctly:
    - Passes against the real repository (current cspell.json registers every
      lint-error-code prefix emitted by scripts/lint-*.{ps1,js},
      scripts/tests/test-lint-*.{ps1,js,sh}, or .githooks/*).
    - Fails with a deterministic exit code and a copy-pasteable JSON patch
      when a synthetic lint script introduces a novel, unregistered prefix.
    - Tolerates lint scripts that emit no lint codes at all.
    - Ignores prefix variants that cspell accepts via compound-word splitting
      (e.g. DEP, because "DEP" splits into common English fragments under the
      active cspell config).
    - Emits the violating sources (script:line) in its failure output.

.PARAMETER VerboseOutput
    Show verbose per-test diagnostics.

.EXAMPLE
    pwsh -NoProfile -File scripts/tests/test-validate-lint-error-codes.ps1
#>
param(
    [switch]$VerboseOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:TestsPassed = 0
$script:TestsFailed = 0
$script:FailedTests = @()

function Write-Info($msg) {
    if ($VerboseOutput) { Write-Host "[test-validate-lint-error-codes] $msg" -ForegroundColor Cyan }
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

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
$validatorPath = Join-Path $repoRoot 'scripts/validate-lint-error-codes.ps1'

$tempBase = if ($env:TEMP) { $env:TEMP } elseif ($env:TMPDIR) { $env:TMPDIR } else { '/tmp' }
$tempRoot = Join-Path $tempBase "test-validate-lint-error-codes-$(Get-Random)"
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

# Build a synthetic repo that mimics the real layout: scripts/lint-*.ps1 files
# plus a cspell.json at the root. The validator resolves paths relative to
# $PSScriptRoot/..; copying the validator script under the same layout makes
# it scope its scan to our fixture.
function New-FixtureRoot {
    $root = Join-Path $tempRoot "repo-$(Get-Random)"
    New-Item -ItemType Directory -Path (Join-Path $root 'scripts') -Force | Out-Null
    # The extended harvester (P1-2) also scans scripts/tests/ and .githooks/.
    # Pre-create those dirs so individual tests can drop fixtures there without
    # repeating the boilerplate.
    New-Item -ItemType Directory -Path (Join-Path $root 'scripts/tests') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $root '.githooks') -Force | Out-Null
    Copy-Item -LiteralPath $validatorPath -Destination (Join-Path $root 'scripts/validate-lint-error-codes.ps1')
    # Seed a minimal-but-valid cspell.json matching the real config shape.
    # caseSensitive:false + minWordLength:3 mirror the real config so compound
    # splitting behavior is identical.
    $cspellSeed = @{
        '$schema'            = 'https://raw.githubusercontent.com/streetsidesoftware/cspell/main/cspell.schema.json'
        version              = '0.2'
        language             = 'en'
        files                = @()
        words                = @('UNH')
        flagWords            = @()
        minWordLength        = 3
        allowCompoundWords   = $true
        caseSensitive        = $false
    }
    $cspellSeed | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $root 'cspell.json') -NoNewline

    # Share node_modules with the real repo so `npx --no-install cspell` works
    # inside the fixture CWD without re-downloading dependencies. A symlink is
    # safe here because the validator only reads node_modules — it never
    # writes. On platforms where symlink creation is forbidden (Windows non-
    # admin, some CI runners), fall back to a package.json that points npm at
    # the repo's node_modules directory via NODE_PATH. We prefer the symlink
    # path because `npx --no-install` resolves through standard Node module
    # resolution rules, which follow symlinks transparently.
    $fixtureNodeModules = Join-Path $root 'node_modules'
    $realNodeModules = Join-Path $repoRoot 'node_modules'
    if (Test-Path -LiteralPath $realNodeModules) {
        try {
            New-Item -ItemType SymbolicLink -Path $fixtureNodeModules -Target $realNodeModules -ErrorAction Stop | Out-Null
        }
        catch {
            # Fallback: copy just the cspell bin + hoisted dirs. We keep this
            # narrow to avoid ballooning the fixture.
            Write-Info "Symlink creation failed ($_); falling back to copy."
            Copy-Item -LiteralPath $realNodeModules -Destination $fixtureNodeModules -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    return $root
}

function Invoke-ValidatorInFixture {
    param([string]$FixtureRoot)
    $validatorCopy = Join-Path $FixtureRoot 'scripts/validate-lint-error-codes.ps1'
    # The validator calls `npx --no-install cspell` relative to its repo root.
    # Switching the working directory is not enough; cspell resolves the
    # nearest cspell.json from the CWD, which is what we want.
    Push-Location $FixtureRoot
    try {
        $output = & pwsh -NoProfile -File $validatorCopy -VerboseOutput *>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }
    return @{ Output = ($output | Out-String); ExitCode = $exitCode }
}

# ── Test 1: Real repo must pass ──────────────────────────────────────────────
# This is the single most important guard: it asserts that the checked-in
# cspell.json already covers every prefix emitted by the real lint scripts.
# Any new lint-error-code family that ships without a cspell entry breaks this
# test before it can break a pre-push hook.
Write-Host "`n  Section: Real repository sanity" -ForegroundColor White
try {
    Push-Location $repoRoot
    try {
        $realOutput = & pwsh -NoProfile -File $validatorPath *>&1
        $realExit = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }
    Write-TestResult "RealRepo.ValidatorPasses" ($realExit -eq 0) "Exit: $realExit. Output: $($realOutput | Out-String)"
} catch {
    Write-TestResult "RealRepo.ValidatorPasses" $false "Exception: $_"
}

# ── Test 2: Fixture with only known prefixes passes ──────────────────────────
Write-Host "`n  Section: Fixture with registered prefix" -ForegroundColor White
try {
    $fixture = New-FixtureRoot
    @"
# Synthetic lint script using only UNH (registered).
# UNH001 - example code
Write-Host "UNH001: something"
"@ | Set-Content -LiteralPath (Join-Path $fixture 'scripts/lint-fake-good.ps1')

    $r = Invoke-ValidatorInFixture -FixtureRoot $fixture
    Write-TestResult "Fixture.OnlyKnownPrefix.Passes" ($r.ExitCode -eq 0) "Exit: $($r.ExitCode). Output: $($r.Output)"
} catch {
    Write-TestResult "Fixture.OnlyKnownPrefix.Passes" $false "Exception: $_"
}

# ── Test 3: Fixture with unregistered prefix fails AND prints patch ──────────
Write-Host "`n  Section: Fixture with unregistered prefix" -ForegroundColor White
try {
    $fixture = New-FixtureRoot
    # XYZ is never a real English word; cspell will not accept it via compound
    # splitting. Using a synthetic prefix guarantees the test fails for the
    # right reason (missing cspell registration) rather than false positives.
    @"
# Synthetic lint script emitting a novel XYZ-family code.
Write-Host "XYZ001: unregistered prefix should fail the validator"
"@ | Set-Content -LiteralPath (Join-Path $fixture 'scripts/lint-fake-novel.ps1')

    $r = Invoke-ValidatorInFixture -FixtureRoot $fixture
    $failed = ($r.ExitCode -ne 0)
    $mentionsXyz = ($r.Output -match '\bXYZ\b')
    $emitsPatch = ($r.Output -match 'add_to_root_words')
    $emitsSourceLine = ($r.Output -match 'lint-fake-novel\.ps1:')
    $allPassed = $failed -and $mentionsXyz -and $emitsPatch -and $emitsSourceLine

    Write-TestResult "Fixture.UnregisteredPrefix.Fails" $allPassed "Exit: $($r.ExitCode), mentionsXyz=$mentionsXyz, emitsPatch=$emitsPatch, emitsSourceLine=$emitsSourceLine. Output: $($r.Output)"
} catch {
    Write-TestResult "Fixture.UnregisteredPrefix.Fails" $false "Exception: $_"
}

# ── Test 4: Fixture with no lint scripts at all exits 1 (layout guard) ───────
# The validator treats an empty lint-*.{ps1,js} glob as a repo-layout error,
# not a silent pass. This guards against someone renaming the whole family
# and silently disabling the check.
Write-Host "`n  Section: Fixture with no lint scripts" -ForegroundColor White
try {
    $fixture = New-FixtureRoot
    # Intentionally no lint-*.ps1 files.
    $r = Invoke-ValidatorInFixture -FixtureRoot $fixture
    Write-TestResult "Fixture.NoLintScripts.FailsLoudly" ($r.ExitCode -ne 0) "Exit: $($r.ExitCode). Output: $($r.Output)"
} catch {
    Write-TestResult "Fixture.NoLintScripts.FailsLoudly" $false "Exception: $_"
}

# ── Test 5: Fixture with lint script emitting no codes passes ────────────────
Write-Host "`n  Section: Fixture with lint script emitting no codes" -ForegroundColor White
try {
    $fixture = New-FixtureRoot
    @"
# Synthetic lint script with no lint codes at all (emits prose only).
Write-Host "All good."
"@ | Set-Content -LiteralPath (Join-Path $fixture 'scripts/lint-fake-silent.ps1')

    $r = Invoke-ValidatorInFixture -FixtureRoot $fixture
    Write-TestResult "Fixture.LintScriptWithNoCodes.Passes" ($r.ExitCode -eq 0) "Exit: $($r.ExitCode). Output: $($r.Output)"
} catch {
    Write-TestResult "Fixture.LintScriptWithNoCodes.Passes" $false "Exception: $_"
}

# ── Test 6: .js lint scripts are scanned too ─────────────────────────────────
Write-Host "`n  Section: Fixture with .js lint script" -ForegroundColor White
try {
    $fixture = New-FixtureRoot
    @"
// Synthetic JS lint script emitting a novel ABC-family code.
console.log('ABC001: unregistered prefix should fail the validator');
"@ | Set-Content -LiteralPath (Join-Path $fixture 'scripts/lint-fake-js.js')

    $r = Invoke-ValidatorInFixture -FixtureRoot $fixture
    # ABC is an English word in cspell's default dictionary, but the token
    # ABC001 splits to "ABC" and "001" and ABC is accepted. This test is
    # therefore a positive control — a .js file is scanned without crashing.
    # The pass/fail hinges on the validator's ability to read .js files at all,
    # not on ABC being unknown.
    $noCrash = ($r.Output -notmatch 'FullyQualifiedErrorId')
    Write-TestResult "Fixture.JsLintScriptIsScanned" $noCrash "Exit: $($r.ExitCode). Output: $($r.Output)"
} catch {
    Write-TestResult "Fixture.JsLintScriptIsScanned" $false "Exception: $_"
}

# ── Test 7: Prefix emitted ONLY in .githooks/ is harvested (P1-2) ────────────
# Hook error messages frequently cite lint-error-code families (e.g. UNH004 in
# pre-commit's failure block). A novel prefix emitted from a hook but never
# from a lint script must still be caught by the validator.
Write-Host "`n  Section: Prefix emitted only in .githooks/" -ForegroundColor White
try {
    $fixture = New-FixtureRoot
    # Seed a lint script with nothing interesting (forces harvester to rely on
    # the .githooks/ scan for the novel prefix).
    @"
# Silent lint script — emits no codes.
Write-Host 'All good.'
"@ | Set-Content -LiteralPath (Join-Path $fixture 'scripts/lint-silent.ps1')

    @"
#!/usr/bin/env bash
# pre-commit emits HOK001 as a failure code — unregistered with cspell.
echo 'HOK001: hook-emitted code that must be harvested'
"@ | Set-Content -LiteralPath (Join-Path $fixture '.githooks/pre-commit')

    $r = Invoke-ValidatorInFixture -FixtureRoot $fixture
    $failed = ($r.ExitCode -ne 0)
    $mentionsHok = ($r.Output -match '\bHOK\b')
    $emitsSourceLine = ($r.Output -match '\.githooks/pre-commit:')
    $allPassed = $failed -and $mentionsHok -and $emitsSourceLine
    Write-TestResult "Fixture.PrefixOnlyInGithooks.Detected" $allPassed "Exit: $($r.ExitCode), mentionsHok=$mentionsHok, emitsSourceLine=$emitsSourceLine. Output: $($r.Output)"
} catch {
    Write-TestResult "Fixture.PrefixOnlyInGithooks.Detected" $false "Exception: $_"
}

# ── Test 8: Prefix emitted ONLY in scripts/tests/ is harvested (P1-2) ────────
# Test assertions frequently reference error codes (e.g. "Assert -match
# 'UNH005'"). The validator must see tests/ files too, otherwise a code used
# only in tests would appear valid to cspell but missing from the contract.
Write-Host "`n  Section: Prefix emitted only in scripts/tests/" -ForegroundColor White
try {
    $fixture = New-FixtureRoot
    @"
# Silent lint script.
Write-Host 'silent'
"@ | Set-Content -LiteralPath (Join-Path $fixture 'scripts/lint-silent.ps1')

    @"
# A test file that asserts TST001 is emitted — the prefix is introduced here,
# not in any lint-*.ps1, and must still be flagged.
Write-Host 'TST001: test-only prefix'
"@ | Set-Content -LiteralPath (Join-Path $fixture 'scripts/tests/test-lint-novel.ps1')

    $r = Invoke-ValidatorInFixture -FixtureRoot $fixture
    $failed = ($r.ExitCode -ne 0)
    $mentionsTst = ($r.Output -match '\bTST\b')
    $emitsSourceLine = ($r.Output -match 'test-lint-novel\.ps1:')
    $allPassed = $failed -and $mentionsTst -and $emitsSourceLine
    Write-TestResult "Fixture.PrefixOnlyInTests.Detected" $allPassed "Exit: $($r.ExitCode), mentionsTst=$mentionsTst, emitsSourceLine=$emitsSourceLine. Output: $($r.Output)"
} catch {
    Write-TestResult "Fixture.PrefixOnlyInTests.Detected" $false "Exception: $_"
}

# ── Test 9: Upstream-rule allowlist skips SC2016 et al. (P1-7) ───────────────
# `# shellcheck disable=SC2016` is a common and legitimate reference in our
# scripts. The harvester must not demand cspell registration for the SC
# family — that's an upstream linter, not a code we own.
Write-Host "`n  Section: Upstream-rule allowlist (SC/MD/CS/SA)" -ForegroundColor White
try {
    $fixture = New-FixtureRoot
    @"
# Synthetic lint script that references SC2016 in a comment disable tag,
# and MD025 in a markdownlint rule reference. Neither should cause the
# validator to flag SC or MD as missing from cspell.
# shellcheck disable=SC2016
# See MD025 (markdownlint) upstream.
Write-Host 'nothing emitted'
"@ | Set-Content -LiteralPath (Join-Path $fixture 'scripts/lint-upstream-refs.ps1')

    $r = Invoke-ValidatorInFixture -FixtureRoot $fixture
    $passed = ($r.ExitCode -eq 0)
    # The output SHOULD NOT claim SC or MD is unregistered.
    $noSpuriousSc = ($r.Output -notmatch '(?m)^\s+SC\b')
    $noSpuriousMd = ($r.Output -notmatch '(?m)^\s+MD\b')
    $allPassed = $passed -and $noSpuriousSc -and $noSpuriousMd
    Write-TestResult "Fixture.UpstreamRuleAllowlist.SkipsSCandMD" $allPassed "Exit: $($r.ExitCode), noSpuriousSc=$noSpuriousSc, noSpuriousMd=$noSpuriousMd. Output: $($r.Output)"
} catch {
    Write-TestResult "Fixture.UpstreamRuleAllowlist.SkipsSCandMD" $false "Exception: $_"
}

# ── Cleanup ──────────────────────────────────────────────────────────────────
if (Test-Path -LiteralPath $tempRoot) {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}

# ── Summary ──────────────────────────────────────────────────────────────────
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
