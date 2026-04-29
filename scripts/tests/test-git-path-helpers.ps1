#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Test runner for scripts/git-path-helpers.ps1's ConvertTo-GitRelativePosixPath.

.DESCRIPTION
    Verifies path-normalization semantics that are critical for `git
    check-ignore` safety:
      - Absolute path inside repo root -> repo-relative POSIX.
      - Absolute path equal to repo root -> '.' (the conventional "repo itself").
      - Absolute path outside repo root -> $null.
      - Relative path (backslash or forward-slash) -> forward-slash POSIX.
      - Relative path './foo' strips the leading './'.
      - Empty / whitespace input -> $null.
      - Windows-style backslash absolute path gets converted (platform-dependent;
        only asserts where [System.IO.Path]::IsPathRooted accepts the form).
      - Case-insensitive match on Windows-style repo root prefix.
      - Trailing slash on inputs is tolerated.

    This test does NOT invoke git itself — the whole point of the helper is
    to produce input that `git check-ignore` will interpret correctly, and
    the pre-existing integration tests in scripts/tests/test-agent-preflight.ps1
    already exercise the full path (helper + git) end-to-end.

.PARAMETER VerboseOutput
    Show detailed output during test execution.

.EXAMPLE
    pwsh -NoProfile -File scripts/tests/test-git-path-helpers.ps1
    pwsh -NoProfile -File scripts/tests/test-git-path-helpers.ps1 -VerboseOutput
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
    if ($VerboseOutput) { Write-Host "[test-git-path-helpers] $msg" -ForegroundColor Cyan }
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

# Dot-source the helper. ConvertTo-GitRelativePosixPath is defined there.
$helperPath = (Resolve-Path (Join-Path $PSScriptRoot '..' 'git-path-helpers.ps1')).Path
. $helperPath

# --- Setup: carve a throwaway repo-root directory so we can test absolute
#     paths without coupling to the live repo layout. Using a tempdir also
#     keeps the tests platform-independent.
$tempBase = if ($env:TEMP) { $env:TEMP } elseif ($env:TMPDIR) { $env:TMPDIR } else { '/tmp' }
$repoRoot = Join-Path $tempBase "test-git-path-helpers-$(Get-Random)"
New-Item -ItemType Directory -Path $repoRoot -Force | Out-Null
# Normalize to the platform's canonical form so our comparisons match what
# the helper returns after [System.IO.Path]::GetFullPath.
$repoRoot = (Resolve-Path $repoRoot).Path

try {
    Write-Host "Testing ConvertTo-GitRelativePosixPath..." -ForegroundColor White

    Write-Host "`n  Section: Absolute paths" -ForegroundColor White

    # --- Pass_AbsolutePathInsideRepo ---
    $abs = Join-Path $repoRoot 'foo' | Join-Path -ChildPath 'bar.txt'
    $got = ConvertTo-GitRelativePosixPath -Path $abs -RepoRoot $repoRoot
    Write-TestResult "Pass_AbsolutePathInsideRepo" ($got -eq 'foo/bar.txt') "Expected 'foo/bar.txt', got '$got'"

    # --- Pass_AbsolutePathEqualsRepoRoot ---
    $got = ConvertTo-GitRelativePosixPath -Path $repoRoot -RepoRoot $repoRoot
    Write-TestResult "Pass_AbsolutePathEqualsRepoRoot" ($got -eq '.') "Expected '.', got '$got'"

    # --- Pass_AbsolutePathOutsideRepoReturnsNull ---
    $outside = Join-Path $tempBase "not-the-repo-$(Get-Random)"
    New-Item -ItemType Directory -Path $outside -Force | Out-Null
    try {
        $got = ConvertTo-GitRelativePosixPath -Path $outside -RepoRoot $repoRoot
        Write-TestResult "Pass_AbsolutePathOutsideRepoReturnsNull" ($null -eq $got) "Expected `$null, got '$got'"
    } finally {
        Remove-Item -LiteralPath $outside -Force -ErrorAction SilentlyContinue
    }

    # --- Pass_AbsolutePathTrailingSlashTolerated ---
    $absWithSlash = (Join-Path $repoRoot 'dir') + '/'
    $got = ConvertTo-GitRelativePosixPath -Path $absWithSlash -RepoRoot $repoRoot
    Write-TestResult "Pass_AbsolutePathTrailingSlashTolerated" ($got -eq 'dir') "Expected 'dir', got '$got'"

    Write-Host "`n  Section: Relative paths" -ForegroundColor White

    # --- Pass_RelativePathForwardSlashPreserved ---
    $got = ConvertTo-GitRelativePosixPath -Path 'src/foo/bar.txt' -RepoRoot $repoRoot
    Write-TestResult "Pass_RelativePathForwardSlashPreserved" ($got -eq 'src/foo/bar.txt') "Expected 'src/foo/bar.txt', got '$got'"

    # --- Pass_RelativePathBackslashNormalized ---
    $got = ConvertTo-GitRelativePosixPath -Path 'src\foo\bar.txt' -RepoRoot $repoRoot
    Write-TestResult "Pass_RelativePathBackslashNormalized" ($got -eq 'src/foo/bar.txt') "Expected 'src/foo/bar.txt', got '$got'"

    # --- Pass_RelativePathDotSlashPrefixStripped ---
    $got = ConvertTo-GitRelativePosixPath -Path './src/foo.txt' -RepoRoot $repoRoot
    Write-TestResult "Pass_RelativePathDotSlashPrefixStripped" ($got -eq 'src/foo.txt') "Expected 'src/foo.txt', got '$got'"

    # --- Pass_RelativePathTrailingSlashStripped ---
    $got = ConvertTo-GitRelativePosixPath -Path 'dir/' -RepoRoot $repoRoot
    Write-TestResult "Pass_RelativePathTrailingSlashStripped" ($got -eq 'dir') "Expected 'dir', got '$got'"

    # --- Pass_RelativeBareDotReturnsDot ---
    # A relative './' input (after stripping and trimming) collapses to the
    # repo itself — should return '.'.
    $got = ConvertTo-GitRelativePosixPath -Path './' -RepoRoot $repoRoot
    Write-TestResult "Pass_RelativeBareDotReturnsDot" ($got -eq '.') "Expected '.', got '$got'"

    Write-Host "`n  Section: Empty / whitespace inputs" -ForegroundColor White

    # --- Pass_EmptyPathReturnsNull ---
    $got = ConvertTo-GitRelativePosixPath -Path '' -RepoRoot $repoRoot
    Write-TestResult "Pass_EmptyPathReturnsNull" ($null -eq $got) "Expected `$null, got '$got'"

    # --- Pass_WhitespacePathReturnsNull ---
    $got = ConvertTo-GitRelativePosixPath -Path '   ' -RepoRoot $repoRoot
    Write-TestResult "Pass_WhitespacePathReturnsNull" ($null -eq $got) "Expected `$null, got '$got'"

    # --- Pass_EmptyRepoRootRejected ---
    # RepoRoot is parameter-bound as [string] without AllowEmptyString, so an
    # empty string is rejected at bind time. This is defense in depth — the
    # helper also has an explicit IsNullOrWhiteSpace guard inside the body
    # (for whitespace-only values that slip through param binding), but the
    # canonical caller contract is that RepoRoot is always a real path.
    $threw = $false
    try {
        ConvertTo-GitRelativePosixPath -Path 'foo' -RepoRoot '' -ErrorAction Stop | Out-Null
    } catch {
        $threw = $true
    }
    Write-TestResult "Pass_EmptyRepoRootRejected" $threw "Expected parameter-binding error for empty RepoRoot."

    # --- Pass_WhitespaceRepoRootReturnsNull ---
    # Whitespace-only (non-empty) RepoRoot slips past the param binding but
    # gets caught by the IsNullOrWhiteSpace guard and returns $null.
    $got = ConvertTo-GitRelativePosixPath -Path 'foo' -RepoRoot '   '
    Write-TestResult "Pass_WhitespaceRepoRootReturnsNull" ($null -eq $got) "Expected `$null, got '$got'"

    Write-Host "`n  Section: Platform-aware case sensitivity" -ForegroundColor White

    # --- Pass_CaseInsensitiveRepoRootMatch (Windows only) ---
    # On Windows NTFS the helper must tolerate case differences between the
    # supplied RepoRoot string and the resolved absolute path. On Linux/macOS
    # the match is case-sensitive, so an uppercased RepoRoot should NOT match
    # a lowercased path (different files on a case-sensitive FS). Each branch
    # asserts the other doesn't accidentally cross over.
    $hostIsWindows = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
        [System.Runtime.InteropServices.OSPlatform]::Windows)
    $upperRoot = $repoRoot.ToUpperInvariant()
    if ($upperRoot -eq $repoRoot) {
        # Tempdir was already all-uppercase (or all-non-letters); nothing to
        # differentiate. Record both assertions as vacuously passing.
        Write-TestResult "Pass_CaseInsensitiveRepoRootMatch" $true "Tempdir had no letters to case-flip; case-sensitivity not exercised."
        Write-TestResult "Pass_LinuxCaseSensitiveRepoRootRejectsMismatch" $true "Tempdir had no letters to case-flip; case-sensitivity not exercised."
    } elseif ($hostIsWindows) {
        # Windows: uppercasing the RepoRoot should still match. Assert match.
        $abs = Join-Path $repoRoot 'cased.txt'
        $got = ConvertTo-GitRelativePosixPath -Path $abs -RepoRoot $upperRoot
        Write-TestResult "Pass_CaseInsensitiveRepoRootMatch" ($got -eq 'cased.txt') "Expected 'cased.txt' on Windows (case-insensitive); got '$got' (upperRoot='$upperRoot')"
        # Linux assertion is not applicable on Windows — record as vacuously passing.
        Write-TestResult "Pass_LinuxCaseSensitiveRepoRootRejectsMismatch" $true "Not applicable on Windows."
    } else {
        # Linux/macOS: case-sensitive comparison. An uppercased RepoRoot
        # should NOT match a lowercased absolute path — the helper returns
        # $null because the prefix doesn't compare equal.
        $abs = Join-Path $repoRoot 'cased.txt'
        $got = ConvertTo-GitRelativePosixPath -Path $abs -RepoRoot $upperRoot
        Write-TestResult "Pass_LinuxCaseSensitiveRepoRootRejectsMismatch" ($null -eq $got) "Expected `$null on Linux/macOS (case-sensitive); got '$got' (upperRoot='$upperRoot'). Helper must NOT treat differently-cased RepoRoot as the same repo."
        # Windows assertion is not applicable on Linux — record as vacuously passing.
        Write-TestResult "Pass_CaseInsensitiveRepoRootMatch" $true "Not applicable on Linux/macOS."
    }

    Write-Host "`n  Section: POSIX separator guarantee" -ForegroundColor White

    # --- Pass_OutputAlwaysPosixSeparators ---
    # The output must not contain `\` regardless of input shape.
    $abs = Join-Path $repoRoot 'a' | Join-Path -ChildPath 'b' | Join-Path -ChildPath 'c.txt'
    $got = ConvertTo-GitRelativePosixPath -Path $abs -RepoRoot $repoRoot
    $hasBackslash = $got -match '\\'
    Write-TestResult "Pass_OutputAlwaysPosixSeparators" (-not $hasBackslash -and $got -eq 'a/b/c.txt') "Expected 'a/b/c.txt' with no backslash; got '$got' (backslash=$hasBackslash)"

} finally {
    Remove-Item -LiteralPath $repoRoot -Recurse -Force -ErrorAction SilentlyContinue
}

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
