Param(
  [switch]$VerboseOutput
)

<#
.SYNOPSIS
    Test runner for lint-drawer-multiobject.ps1 Get-ContainingMethodName function

.DESCRIPTION
    Tests that Get-ContainingMethodName correctly identifies the containing method
    across various nesting depths (if blocks, for loops, lambdas, etc.).

.PARAMETER VerboseOutput
    Show detailed output during test execution

.EXAMPLE
    ./scripts/tests/test-lint-drawer-multiobject.ps1
    ./scripts/tests/test-lint-drawer-multiobject.ps1 -VerboseOutput
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:TestsPassed = 0
$script:TestsFailed = 0
$script:FailedTests = @()

function Write-Info($msg) {
  if ($VerboseOutput) { Write-Host "[test-lint-drawer-multiobject] $msg" -ForegroundColor Cyan }
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

# Source the function from the lint script by dot-sourcing it in a controlled way.
# We extract just the function definition to avoid running the main script body.
$lintScriptPath = Join-Path $PSScriptRoot '..' 'lint-drawer-multiobject.ps1'
$lintContent = Get-Content $lintScriptPath -Raw

# Extract and define Get-ContainingMethodName function
$functionPattern = '(?s)(function Get-ContainingMethodName\([^)]*\)\s*\{.*?\n\})'
if ($lintContent -match $functionPattern) {
  Invoke-Expression $Matches[1]
} else {
  Write-Host "FATAL: Could not extract Get-ContainingMethodName from lint script" -ForegroundColor Red
  exit 1
}

Write-Host "Testing Get-ContainingMethodName brace counting..." -ForegroundColor White

# Test 1: Simple method - no nesting
$content1 = @(
  'public class MyDrawer : PropertyDrawer',  # 0
  '{',                                        # 1
  '    public override void OnGUI(Rect pos)',  # 2
  '    {',                                     # 3
  '        property.intValue = 5;',            # 4 (target: lineIndex=5, 1-based)
  '    }',                                     # 5
  '}'                                          # 6
)
$result1 = Get-ContainingMethodName $content1 5
Write-TestResult "SimpleMethodNoNesting" ($result1 -eq 'OnGUI') "Expected 'OnGUI', got '$result1'"

# Test 2: One level of if nesting
$content2 = @(
  'public override void OnGUI(Rect pos)',  # 0
  '{',                                      # 1
  '    if (condition) {',                    # 2
  '        property.intValue = 5;',          # 3 (target: lineIndex=4)
  '    }',                                   # 4
  '}'                                        # 5
)
$result2 = Get-ContainingMethodName $content2 4
Write-TestResult "OneIfNesting" ($result2 -eq 'OnGUI') "Expected 'OnGUI', got '$result2'"

# Test 3: Two levels of nesting (if + for)
$content3 = @(
  'public override void OnGUI(Rect pos) {',  # 0
  '    if (something) {',                      # 1
  '        for (int i = 0; i < 10; i++) {',    # 2
  '            property.intValue = 5;',         # 3 (target: lineIndex=4)
  '        }',                                  # 4
  '    }',                                      # 5
  '}'                                           # 6
)
$result3 = Get-ContainingMethodName $content3 4
Write-TestResult "TwoLevelsNesting" ($result3 -eq 'OnGUI') "Expected 'OnGUI', got '$result3'"

# Test 4: Three levels of nesting
$content4 = @(
  'public override void OnGUI(Rect pos) {',  # 0
  '    if (a) {',                              # 1
  '        if (b) {',                          # 2
  '            for (int i = 0; i < 5; i++) {', # 3
  '                property.intValue = 5;',     # 4 (target: lineIndex=5)
  '            }',                              # 5
  '        }',                                  # 6
  '    }',                                      # 7
  '}'                                           # 8
)
$result4 = Get-ContainingMethodName $content4 5
Write-TestResult "ThreeLevelsNesting" ($result4 -eq 'OnGUI') "Expected 'OnGUI', got '$result4'"

# Test 5: Inside a lambda (callback context)
$content5 = @(
  'public override void OnGUI(Rect pos) {',  # 0
  '    menu.AddItem(new GUIContent("X"), false, () =>',  # 1
  '    {',                                     # 2
  '        property.intValue = 5;',            # 3 (target: lineIndex=4)
  '    });',                                   # 4
  '}'                                          # 5
)
$result5 = Get-ContainingMethodName $content5 4
Write-TestResult "InsideLambda" ($result5 -eq 'OnGUI') "Expected 'OnGUI', got '$result5'"

# Test 6: Method with brace on separate line
$content6 = @(
  'public override void OnGUI(Rect pos)',  # 0
  '{',                                      # 1
  '    if (x)',                              # 2
  '    {',                                   # 3
  '        property.intValue = 5;',          # 4 (target: lineIndex=5)
  '    }',                                   # 5
  '}'                                        # 6
)
$result6 = Get-ContainingMethodName $content6 5
Write-TestResult "BraceOnSeparateLine" ($result6 -eq 'OnGUI') "Expected 'OnGUI', got '$result6'"

# Test 7: DrawToggleWithValue (private static method)
$content7 = @(
  'private static void DrawToggleWithValue(SerializedProperty prop) {',  # 0
  '    if (condition) {',                                                 # 1
  '        property.boolValue = true;',                                   # 2 (target: lineIndex=3)
  '    }',                                                                # 3
  '}'                                                                     # 4
)
$result7 = Get-ContainingMethodName $content7 3
Write-TestResult "PrivateStaticMethod" ($result7 -eq 'DrawToggleWithValue') "Expected 'DrawToggleWithValue', got '$result7'"

# Test 8: No containing method (at class level)
$content8 = @(
  'public class MyDrawer {',              # 0
  '    private int _value = 5;',           # 1 (target: lineIndex=2)
  '}'                                      # 2
)
$result8 = Get-ContainingMethodName $content8 2
Write-TestResult "NoContainingMethod" ($null -eq $result8) "Expected null, got '$result8'"

# Test 9: if statement at class level should NOT match as method signature (regex regression)
# This tests that the method signature regex requires a word character to start the return type,
# preventing 'if' from being matched as a method name when braceCount <= 0.
$content9 = @(
  'if (condition)',              # 0
  '{',                           # 1
  '    property.intValue = 5;', # 2 (target: lineIndex=3)
  '}'                            # 3
)
$result9 = Get-ContainingMethodName $content9 3
Write-TestResult "IfNotMatchedAsMethod" ($null -eq $result9) "Expected null (if should not match as method), got '$result9'"

# Test 10: for loop at class level should NOT match as method signature
$content10 = @(
  'for (int i = 0; i < 10; i++)',  # 0
  '{',                              # 1
  '    property.intValue = 5;',    # 2 (target: lineIndex=3)
  '}'                               # 3
)
$result10 = Get-ContainingMethodName $content10 3
Write-TestResult "ForNotMatchedAsMethod" ($null -eq $result10) "Expected null (for should not match as method), got '$result10'"

# Print summary
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
