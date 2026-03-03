Param(
  [switch]$VerboseOutput
)

<#
.SYNOPSIS
    Test runner for lint-tests.ps1

.DESCRIPTION
    Tests that lint-tests.ps1 correctly:
    - Validates allowlisted helper file paths exist on disk
    - Detects UNH001 (direct destroy without Track)
    - Detects UNH002 (untracked Unity object allocation)
    - Detects UNH003 (missing CommonTestBase inheritance)
    - Passes clean files that follow all conventions
    - Allowlists known helper files correctly

.PARAMETER VerboseOutput
    Show detailed output during test execution

.EXAMPLE
    ./scripts/tests/test-lint-tests.ps1
    ./scripts/tests/test-lint-tests.ps1 -VerboseOutput
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:TestsPassed = 0
$script:TestsFailed = 0
$script:FailedTests = @()

function Write-Info($msg) {
  if ($VerboseOutput) { Write-Host "[test-lint-tests] $msg" -ForegroundColor Cyan }
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

$lintScriptPath = Join-Path $PSScriptRoot '..' 'lint-tests.ps1'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path

Write-Host "Testing lint-tests.ps1..." -ForegroundColor White

# ── Test 1: All allowlisted paths exist on disk ──────────────────────────────
Write-Host "`n  Section: Allowlist path validation" -ForegroundColor White

$lintContent = Get-Content $lintScriptPath -Raw
# Extract the $allowedHelperFiles array entries
$pathMatches = [regex]::Matches($lintContent, "'\s*([^']+\.cs)\s*'")
$allowlistPaths = @()
foreach ($m in $pathMatches) {
  $p = $m.Groups[1].Value
  # Only include paths that look like test helper files
  if ($p -match '^Tests/') {
    $allowlistPaths += $p
  }
}

foreach ($relPath in $allowlistPaths) {
  $fullPath = Join-Path $repoRoot $relPath
  $exists = Test-Path $fullPath
  Write-TestResult "AllowlistPathExists.$($relPath -replace '[/\\]','.')" $exists "File not found at: $fullPath"
}

# ── Test 2: Lint passes on a clean test file (known good) ────────────────────
Write-Host "`n  Section: Clean file acceptance" -ForegroundColor White

# Create a temporary clean test file
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "lint-tests-test-$(Get-Random)"
$tempTestDir = Join-Path $tempDir 'Tests' 'Editor'
New-Item -ItemType Directory -Path $tempTestDir -Force | Out-Null

try {

$cleanContent = @'
namespace WallstopStudios.UnityHelpers.Tests
{
    using NUnit.Framework;

    public sealed class CleanTest : CommonTestBase
    {
        [Test]
        public void MyTest()
        {
            var go = Track(new GameObject("test"));
            Assert.IsTrue(go != null);
        }
    }
}
'@

$cleanFile = Join-Path $tempTestDir 'CleanTest.cs'
Set-Content -Path $cleanFile -Value $cleanContent -NoNewline

try {
  Push-Location $tempDir
  $output = & $lintScriptPath -Paths (Join-Path 'Tests' 'Editor' 'CleanTest.cs') *>&1
  $exitCode = $LASTEXITCODE
  Pop-Location
  Write-TestResult "CleanFile.PassesLint" ($exitCode -eq 0) "Expected exit 0, got $exitCode. Output: $($output | Out-String)"
} catch {
  Pop-Location
  Write-TestResult "CleanFile.PassesLint" $false "Exception: $_"
}

# ── Test 3: UNH001 detected (direct destroy without Track) ──────────────────
Write-Host "`n  Section: UNH001 detection" -ForegroundColor White

$unh001Content = @'
namespace WallstopStudios.UnityHelpers.Tests
{
    using NUnit.Framework;
    using UnityEngine;

    public sealed class DestroyTest : CommonTestBase
    {
        [Test]
        public void MyTest()
        {
            var go = Track(new GameObject("test"));
            Object.DestroyImmediate(go);
        }
    }
}
'@

$unh001File = Join-Path $tempTestDir 'DestroyTest.cs'
Set-Content -Path $unh001File -Value $unh001Content -NoNewline

try {
  Push-Location $tempDir
  $output = & $lintScriptPath -Paths (Join-Path 'Tests' 'Editor' 'DestroyTest.cs') *>&1
  $exitCode = $LASTEXITCODE
  Pop-Location
  $outputStr = $output | Out-String
  $hasUNH001 = $outputStr -match 'UNH001'
  Write-TestResult "UNH001.DetectsDirectDestroy" ($exitCode -ne 0 -and $hasUNH001) "Expected non-zero exit with UNH001. Exit: $exitCode, Output: $outputStr"
} catch {
  Pop-Location
  Write-TestResult "UNH001.DetectsDirectDestroy" $false "Exception: $_"
}

# ── Test 4: UNH002 detected (untracked allocation) ──────────────────────────
Write-Host "`n  Section: UNH002 detection" -ForegroundColor White

$unh002Content = @'
namespace WallstopStudios.UnityHelpers.Tests
{
    using NUnit.Framework;
    using UnityEngine;

    public sealed class UntrackedTest : CommonTestBase
    {
        [Test]
        public void MyTest()
        {
            Texture2D texture = new Texture2D(64, 64);
            Assert.IsTrue(texture != null);
        }
    }
}
'@

$unh002File = Join-Path $tempTestDir 'UntrackedTest.cs'
Set-Content -Path $unh002File -Value $unh002Content -NoNewline

try {
  Push-Location $tempDir
  $output = & $lintScriptPath -Paths (Join-Path 'Tests' 'Editor' 'UntrackedTest.cs') *>&1
  $exitCode = $LASTEXITCODE
  Pop-Location
  $outputStr = $output | Out-String
  $hasUNH002 = $outputStr -match 'UNH002'
  Write-TestResult "UNH002.DetectsUntrackedAlloc" ($exitCode -ne 0 -and $hasUNH002) "Expected non-zero exit with UNH002. Exit: $exitCode, Output: $outputStr"
} catch {
  Pop-Location
  Write-TestResult "UNH002.DetectsUntrackedAlloc" $false "Exception: $_"
}

# ── Test 5: UNH003 detected (missing CommonTestBase) ────────────────────────
Write-Host "`n  Section: UNH003 detection" -ForegroundColor White

$unh003Content = @'
namespace WallstopStudios.UnityHelpers.Tests
{
    using NUnit.Framework;
    using UnityEngine;

    public sealed class NoBaseTest
    {
        [Test]
        public void MyTest()
        {
            Texture2D texture = new Texture2D(64, 64);
            Assert.IsTrue(texture != null);
        }
    }
}
'@

$unh003File = Join-Path $tempTestDir 'NoBaseTest.cs'
Set-Content -Path $unh003File -Value $unh003Content -NoNewline

try {
  Push-Location $tempDir
  $output = & $lintScriptPath -Paths (Join-Path 'Tests' 'Editor' 'NoBaseTest.cs') *>&1
  $exitCode = $LASTEXITCODE
  Pop-Location
  $outputStr = $output | Out-String
  $hasUNH003 = $outputStr -match 'UNH003'
  Write-TestResult "UNH003.DetectsMissingBase" ($exitCode -ne 0 -and $hasUNH003) "Expected non-zero exit with UNH003. Exit: $exitCode, Output: $outputStr"
} catch {
  Pop-Location
  Write-TestResult "UNH003.DetectsMissingBase" $false "Exception: $_"
}

# ── Test 6: Allowlisted file is skipped ──────────────────────────────────────
Write-Host "`n  Section: Allowlist filtering" -ForegroundColor White

# Extract the Is-AllowlistedFile function
$funcPattern = '(?s)(function Is-AllowlistedFile\([^)]*\)\s*\{.*?\n\})'
if ($lintContent -match $funcPattern) {
  Invoke-Expression $Matches[1]

  # Also extract $allowedHelperFiles
  $arrayPattern = '(?s)\$allowedHelperFiles\s*=\s*@\((.*?)\)'
  if ($lintContent -match $arrayPattern) {
    $arrayExpr = '@(' + $Matches[1] + ')'
    $allowedHelperFiles = Invoke-Expression $arrayExpr
  }
} else {
  Write-Host "  FATAL: Could not extract Is-AllowlistedFile function" -ForegroundColor Red
}

if ($allowlistPaths.Count -gt 0) {
  # Test that an exact-match path returns true
  $testPath = $allowlistPaths[0]
  $result = Is-AllowlistedFile $testPath
  Write-TestResult "Allowlist.ExactMatch" $result "Expected true for '$testPath'"

  # Test that a backslash-normalized path returns true
  $backslashPath = $testPath -replace '/', '\'
  $result2 = Is-AllowlistedFile $backslashPath
  Write-TestResult "Allowlist.BackslashNormalized" $result2 "Expected true for '$backslashPath'"

  # Test that a path with leading ./ is handled
  $dotSlashPath = "./$testPath"
  $result3 = Is-AllowlistedFile $dotSlashPath
  Write-TestResult "Allowlist.DotSlashPrefix" $result3 "Expected true for '$dotSlashPath'"

  # Test that a non-allowlisted path returns false
  $result4 = Is-AllowlistedFile 'Tests/Editor/SomeRandomTest.cs'
  Write-TestResult "Allowlist.NonMatchReturnsFalse" (-not $result4) "Expected false for non-allowlisted path"
}

# ── Test 7: UNH-SUPPRESS comment skips violation ────────────────────────────
Write-Host "`n  Section: UNH-SUPPRESS handling" -ForegroundColor White

$suppressContent = @'
namespace WallstopStudios.UnityHelpers.Tests
{
    using NUnit.Framework;
    using UnityEngine;

    public sealed class SuppressTest : CommonTestBase
    {
        [Test]
        public void MyTest()
        {
            var go = Track(new GameObject("test"));
            Object.DestroyImmediate(go); // UNH-SUPPRESS
        }
    }
}
'@

$suppressFile = Join-Path $tempTestDir 'SuppressTest.cs'
Set-Content -Path $suppressFile -Value $suppressContent -NoNewline

try {
  Push-Location $tempDir
  $output = & $lintScriptPath -Paths (Join-Path 'Tests' 'Editor' 'SuppressTest.cs') *>&1
  $exitCode = $LASTEXITCODE
  Pop-Location
  Write-TestResult "UNH-SUPPRESS.SkipsViolation" ($exitCode -eq 0) "Expected exit 0 with suppress comment. Exit: $exitCode, Output: $($output | Out-String)"
} catch {
  Pop-Location
  Write-TestResult "UNH-SUPPRESS.SkipsViolation" $false "Exception: $_"
}

# ── Test 8: UNH004 detected (underscores in test names) ─────────────────────
Write-Host "`n  Section: UNH004 detection" -ForegroundColor White

$unh004Content = @'
namespace WallstopStudios.UnityHelpers.Tests
{
    using NUnit.Framework;
    using System.Collections.Generic;

    public sealed class NamingTest : CommonTestBase
    {
        private static IEnumerable<TestCaseData> MyTestData()
        {
            yield return new TestCaseData(1).SetName("Some_Bad_Name");
        }

        [TestCaseSource(nameof(MyTestData))]
        public void MyTest(int value)
        {
            Assert.IsTrue(value > 0);
        }
    }
}
'@

$unh004File = Join-Path $tempTestDir 'NamingTest.cs'
Set-Content -Path $unh004File -Value $unh004Content -NoNewline

try {
  Push-Location $tempDir
  $output = & $lintScriptPath -Paths (Join-Path 'Tests' 'Editor' 'NamingTest.cs') *>&1
  $exitCode = $LASTEXITCODE
  Pop-Location
  $outputStr = $output | Out-String
  $hasUNH004 = $outputStr -match 'UNH004'
  Write-TestResult "UNH004.DetectsUnderscoreInSetName" ($exitCode -ne 0 -and $hasUNH004) "Expected non-zero exit with UNH004. Exit: $exitCode, Output: $outputStr"
} catch {
  Pop-Location
  Write-TestResult "UNH004.DetectsUnderscoreInSetName" $false "Exception: $_"
}

# ── Test 9: UNH005 detected (Assert.IsNull / Assert.IsNotNull) ──────────────
Write-Host "`n  Section: UNH005 detection" -ForegroundColor White

$unh005Content = @'
namespace WallstopStudios.UnityHelpers.Tests
{
    using NUnit.Framework;
    using UnityEngine;

    public sealed class NullAssertTest : CommonTestBase
    {
        [Test]
        public void MyTest()
        {
            var go = Track(new GameObject("test"));
            Assert.IsNotNull(go);
        }
    }
}
'@

$unh005File = Join-Path $tempTestDir 'NullAssertTest.cs'
Set-Content -Path $unh005File -Value $unh005Content -NoNewline

try {
  Push-Location $tempDir
  $output = & $lintScriptPath -Paths (Join-Path 'Tests' 'Editor' 'NullAssertTest.cs') *>&1
  $exitCode = $LASTEXITCODE
  Pop-Location
  $outputStr = $output | Out-String
  $hasUNH005 = $outputStr -match 'UNH005'
  Write-TestResult "UNH005.DetectsAssertIsNotNull" ($exitCode -ne 0 -and $hasUNH005) "Expected non-zero exit with UNH005. Exit: $exitCode, Output: $outputStr"
} catch {
  Pop-Location
  Write-TestResult "UNH005.DetectsAssertIsNotNull" $false "Exception: $_"
}

# ── Test 10: Stale allowlist path causes failure ─────────────────────────────
Write-Host "`n  Section: Stale allowlist validation" -ForegroundColor White

# Create a modified copy of lint-tests.ps1 with a fake allowlist entry
$staleTempDir = Join-Path ([System.IO.Path]::GetTempPath()) "lint-tests-stale-$(Get-Random)"
New-Item -ItemType Directory -Path $staleTempDir -Force | Out-Null
# Create package.json to trigger the allowlist validation
Set-Content -Path (Join-Path $staleTempDir 'package.json') -Value '{}' -NoNewline
# Create Tests dir structure
$staleTestDir = Join-Path $staleTempDir 'Tests' 'Editor'
New-Item -ItemType Directory -Path $staleTestDir -Force | Out-Null

# Copy lint script and inject a fake path
$staleLintContent = $lintContent -replace [regex]::Escape("'Tests/Core/TextureTestHelper.cs',"), "'Tests/Core/TextureTestHelper.cs',`n  'Tests/NonExistent/FakeFile.cs',"
$staleLintPath = Join-Path $staleTempDir 'lint-tests-stale.ps1'
Set-Content -Path $staleLintPath -Value $staleLintContent -NoNewline

$staleCleanFile = Join-Path $staleTestDir 'Clean.cs'
Set-Content -Path $staleCleanFile -Value $cleanContent -NoNewline

try {
  Push-Location $staleTempDir
  $output = & $staleLintPath -Paths (Join-Path 'Tests' 'Editor' 'Clean.cs') *>&1
  $exitCode = $LASTEXITCODE
  Pop-Location
  $outputStr = $output | Out-String
  $hasError = $outputStr -match 'Allowlisted helper file not found'
  Write-TestResult "StaleAllowlist.FailsOnMissingPath" ($exitCode -ne 0 -and $hasError) "Expected non-zero exit with error message. Exit: $exitCode, Output: $outputStr"
} catch {
  Pop-Location
  Write-TestResult "StaleAllowlist.FailsOnMissingPath" $false "Exception: $_"
}

Remove-Item -Recurse -Force $staleTempDir -ErrorAction SilentlyContinue

} finally {
  # ── Cleanup ──────────────────────────────────────────────────────────────────
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
