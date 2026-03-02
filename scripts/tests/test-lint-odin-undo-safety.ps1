Param(
  [switch]$VerboseOutput
)

<#
.SYNOPSIS
    Test runner for lint-odin-undo-safety.ps1

.DESCRIPTION
    Tests that lint-odin-undo-safety.ps1 correctly:
    - Passes safe WeakTargets filtering with pattern-match null check
    - Detects UNH006 for unsafe "as UnityEngine.Object" casts on WeakTargets
    - Detects UNH006 for direct casts of WeakTargets to UnityEngine.Object[]
    - Honors UNH-SUPPRESS comments to skip violations
    - Passes serializedObject.targetObjects (always safe)
    - Passes OfType<UnityEngine.Object> filtering (filters nulls)
    - Detects UNH006 for inline unsafe casts inside Undo.RecordObjects calls

.PARAMETER VerboseOutput
    Show detailed output during test execution

.EXAMPLE
    ./scripts/tests/test-lint-odin-undo-safety.ps1
    ./scripts/tests/test-lint-odin-undo-safety.ps1 -VerboseOutput
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:TestsPassed = 0
$script:TestsFailed = 0
$script:FailedTests = @()

function Write-Info($msg) {
  if ($VerboseOutput) { Write-Host "[test-lint-odin-undo-safety] $msg" -ForegroundColor Cyan }
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

$lintScriptPath = Join-Path $PSScriptRoot '..' 'lint-odin-undo-safety.ps1'

Write-Host "Testing lint-odin-undo-safety.ps1..." -ForegroundColor White

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "lint-odin-undo-safety-test-$(Get-Random)"
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

try {

# ── Test 1: Safe code passes ────────────────────────────────────────────────
Write-Host "`n  Section: Safe pattern acceptance" -ForegroundColor White

$safeContent = @'
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SafeDrawer
{
    public void ApplySelection()
    {
        IList weakTargets = Property.Tree.WeakTargets;
        List<UnityEngine.Object> validTargets = new(weakTargets.Count);
        for (int i = 0; i < weakTargets.Count; i++)
        {
            if (weakTargets[i] is UnityEngine.Object obj && obj != null)
                validTargets.Add(obj);
        }
        Undo.RecordObjects(validTargets.ToArray(), "Change Selection");
    }
}
'@

$safeFile = Join-Path $tempDir 'SafeDrawer.cs'
Set-Content -Path $safeFile -Value $safeContent -NoNewline

try {
  $output = & $lintScriptPath -Paths $safeFile *>&1
  $exitCode = $LASTEXITCODE
  Write-TestResult "SafeCode.PassesLint" ($exitCode -eq 0) "Expected exit 0, got $exitCode. Output: $($output | Out-String)"
} catch {
  Write-TestResult "SafeCode.PassesLint" $false "Exception: $_"
}

# ── Test 2: Unsafe "as" cast detected ───────────────────────────────────────
Write-Host "`n  Section: UNH006 as-cast detection" -ForegroundColor White

$unsafeAsContent = @'
using System.Collections;
using UnityEditor;
using UnityEngine;

public class UnsafeAsDrawer
{
    public void ApplySelection()
    {
        IList weakTargets = Property.Tree.WeakTargets;
        UnityEngine.Object[] targets = new UnityEngine.Object[weakTargets.Count];
        for (int i = 0; i < weakTargets.Count; i++)
        {
            targets[i] = Property.Tree.WeakTargets[i] as UnityEngine.Object;
        }
        Undo.RecordObjects(targets, "Change Selection");
    }
}
'@

$unsafeAsFile = Join-Path $tempDir 'UnsafeAsDrawer.cs'
Set-Content -Path $unsafeAsFile -Value $unsafeAsContent -NoNewline

try {
  $output = & $lintScriptPath -Paths $unsafeAsFile *>&1
  $exitCode = $LASTEXITCODE
  $outputStr = $output | Out-String
  $hasUNH006 = $outputStr -match 'UNH006'
  Write-TestResult "UNH006.DetectsUnsafeAsCast" ($exitCode -ne 0 -and $hasUNH006) "Expected non-zero exit with UNH006. Exit: $exitCode, Output: $outputStr"
} catch {
  Write-TestResult "UNH006.DetectsUnsafeAsCast" $false "Exception: $_"
}

# ── Test 3: Unsafe direct cast detected ─────────────────────────────────────
Write-Host "`n  Section: UNH006 direct-cast detection" -ForegroundColor White

$unsafeDirectContent = @'
using System.Collections;
using UnityEditor;
using UnityEngine;

public class UnsafeDirectDrawer
{
    public void ApplySelection()
    {
        UnityEngine.Object[] targets = (UnityEngine.Object[])Property.Tree.WeakTargets;
        Undo.RecordObjects(targets, "Change Selection");
    }
}
'@

$unsafeDirectFile = Join-Path $tempDir 'UnsafeDirectDrawer.cs'
Set-Content -Path $unsafeDirectFile -Value $unsafeDirectContent -NoNewline

try {
  $output = & $lintScriptPath -Paths $unsafeDirectFile *>&1
  $exitCode = $LASTEXITCODE
  $outputStr = $output | Out-String
  $hasUNH006 = $outputStr -match 'UNH006'
  Write-TestResult "UNH006.DetectsDirectCast" ($exitCode -ne 0 -and $hasUNH006) "Expected non-zero exit with UNH006. Exit: $exitCode, Output: $outputStr"
} catch {
  Write-TestResult "UNH006.DetectsDirectCast" $false "Exception: $_"
}

# ── Test 4: UNH-SUPPRESS skips violation ────────────────────────────────────
Write-Host "`n  Section: UNH-SUPPRESS handling" -ForegroundColor White

$suppressContent = @'
using System.Collections;
using UnityEditor;
using UnityEngine;

public class SuppressedDrawer
{
    public void ApplySelection()
    {
        IList weakTargets = Property.Tree.WeakTargets;
        UnityEngine.Object[] targets = new UnityEngine.Object[weakTargets.Count];
        for (int i = 0; i < weakTargets.Count; i++)
        {
            targets[i] = Property.Tree.WeakTargets[i] as UnityEngine.Object; // UNH-SUPPRESS
        }
        Undo.RecordObjects(targets, "Change Selection");
    }
}
'@

$suppressFile = Join-Path $tempDir 'SuppressedDrawer.cs'
Set-Content -Path $suppressFile -Value $suppressContent -NoNewline

try {
  $output = & $lintScriptPath -Paths $suppressFile *>&1
  $exitCode = $LASTEXITCODE
  Write-TestResult "UNH-SUPPRESS.SkipsViolation" ($exitCode -eq 0) "Expected exit 0 with suppress comment. Exit: $exitCode, Output: $($output | Out-String)"
} catch {
  Write-TestResult "UNH-SUPPRESS.SkipsViolation" $false "Exception: $_"
}

# ── Test 5: serializedObject.targetObjects passes ───────────────────────────
Write-Host "`n  Section: Safe serializedObject.targetObjects" -ForegroundColor White

$targetObjectsContent = @'
using UnityEditor;
using UnityEngine;

public class SafeTargetObjectsDrawer
{
    public void ApplySelection()
    {
        Undo.RecordObjects(serializedObject.targetObjects, "Change Selection");
    }
}
'@

$targetObjectsFile = Join-Path $tempDir 'SafeTargetObjectsDrawer.cs'
Set-Content -Path $targetObjectsFile -Value $targetObjectsContent -NoNewline

try {
  $output = & $lintScriptPath -Paths $targetObjectsFile *>&1
  $exitCode = $LASTEXITCODE
  Write-TestResult "SerializedObject.TargetObjectsPasses" ($exitCode -eq 0) "Expected exit 0, got $exitCode. Output: $($output | Out-String)"
} catch {
  Write-TestResult "SerializedObject.TargetObjectsPasses" $false "Exception: $_"
}

# ── Test 6: OfType<UnityEngine.Object> passes ──────────────────────────────
Write-Host "`n  Section: Safe OfType filtering" -ForegroundColor White

$ofTypeContent = @'
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SafeOfTypeDrawer
{
    public void ApplySelection()
    {
        IList weakTargets = Property.Tree.WeakTargets;
        UnityEngine.Object[] targets = weakTargets.OfType<UnityEngine.Object>().ToArray();
        Undo.RecordObjects(targets, "Change Selection");
    }
}
'@

$ofTypeFile = Join-Path $tempDir 'SafeOfTypeDrawer.cs'
Set-Content -Path $ofTypeFile -Value $ofTypeContent -NoNewline

try {
  $output = & $lintScriptPath -Paths $ofTypeFile *>&1
  $exitCode = $LASTEXITCODE
  Write-TestResult "OfType.FilteringPasses" ($exitCode -eq 0) "Expected exit 0, got $exitCode. Output: $($output | Out-String)"
} catch {
  Write-TestResult "OfType.FilteringPasses" $false "Exception: $_"
}

# ── Test 7: Inline unsafe cast inside Undo.RecordObjects detected ───────────
Write-Host "`n  Section: UNH006 inline Undo.RecordObjects unsafe cast" -ForegroundColor White

$inlineUnsafeContent = @'
using System.Collections;
using UnityEditor;
using UnityEngine;

public class InlineUnsafeDrawer
{
    public void ApplySelection()
    {
        Undo.RecordObjects((UnityEngine.Object[])Property.Tree.WeakTargets, "Change Selection");
    }
}
'@

$inlineUnsafeFile = Join-Path $tempDir 'InlineUnsafeDrawer.cs'
Set-Content -Path $inlineUnsafeFile -Value $inlineUnsafeContent -NoNewline

try {
  $output = & $lintScriptPath -Paths $inlineUnsafeFile *>&1
  $exitCode = $LASTEXITCODE
  $outputStr = $output | Out-String
  $hasUNH006 = $outputStr -match 'UNH006'
  Write-TestResult "UNH006.DetectsInlineUnsafeCast" ($exitCode -ne 0 -and $hasUNH006) "Expected non-zero exit with UNH006. Exit: $exitCode, Output: $outputStr"
} catch {
  Write-TestResult "UNH006.DetectsInlineUnsafeCast" $false "Exception: $_"
}

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
