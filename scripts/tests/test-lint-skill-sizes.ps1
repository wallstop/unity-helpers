Param(
  [switch]$VerboseOutput
)

<#
.SYNOPSIS
    Test runner for lint-skill-sizes.ps1

.DESCRIPTION
    Tests that lint-skill-sizes.ps1 correctly classifies skill files by size:
    - >500 lines: ERROR (exit code 1)
    - 480-500 lines: CRITICAL warning (exit code 0, but warning shown)
    - 300-500 lines: WARNING (exit code 0)
    - <300 lines: OK (exit code 0)

    Uses temporary directories with generated fixture files to test each threshold.

.PARAMETER VerboseOutput
    Show detailed output during test execution

.EXAMPLE
    ./scripts/tests/test-lint-skill-sizes.ps1
    ./scripts/tests/test-lint-skill-sizes.ps1 -VerboseOutput
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:TestsPassed = 0
$script:TestsFailed = 0
$script:FailedTests = @()

function Write-Info($msg) {
  if ($VerboseOutput) { Write-Host "[test-lint-skill-sizes] $msg" -ForegroundColor Cyan }
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

function New-SkillFixture {
  param(
    [string]$Dir,
    [string]$FileName,
    [int]$LineCount
  )
  $filePath = Join-Path $Dir $FileName
  $lines = @("# Skill: Test Fixture")
  $lines += ""
  $lines += "<!-- trigger: test | Test fixture | Core -->"
  $lines += ""
  for ($i = 4; $i -lt $LineCount; $i++) {
    $lines += "Line $i of test fixture content."
  }
  $lines | Set-Content -Path $filePath -Encoding UTF8
  return $filePath
}

function Invoke-Linter {
  param(
    [string]$SkillsDir,
    [switch]$Verbose
  )
  # Run the linter against a custom skills directory by temporarily creating the expected structure
  $tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "test-skill-sizes-$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
  $tempSkillsDir = Join-Path $tempRoot '.llm' 'skills'
  $tempScriptsDir = Join-Path $tempRoot 'scripts'

  New-Item -ItemType Directory -Path $tempSkillsDir -Force | Out-Null
  New-Item -ItemType Directory -Path $tempScriptsDir -Force | Out-Null

  # Copy the linter script
  $lintScript = Join-Path $PSScriptRoot '..' 'lint-skill-sizes.ps1'
  Copy-Item $lintScript (Join-Path $tempScriptsDir 'lint-skill-sizes.ps1')

  # Copy fixture files
  Get-ChildItem -Path $SkillsDir -Filter '*.md' -Recurse | ForEach-Object {
    $relativePath = $_.FullName.Substring($SkillsDir.Length + 1)
    $destPath = Join-Path $tempSkillsDir $relativePath
    $destDir = Split-Path $destPath -Parent
    if (-not (Test-Path $destDir)) {
      New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }
    Copy-Item $_.FullName $destPath
  }

  $linterArgs = @((Join-Path $tempScriptsDir 'lint-skill-sizes.ps1'))
  if ($Verbose) { $linterArgs += '-VerboseOutput' }

  try {
    $output = & pwsh -NoProfile -File @linterArgs 2>&1
    $exitCode = $LASTEXITCODE
  }
  finally {
    Remove-Item -Path $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
  }

  return @{
    ExitCode = $exitCode
    Output = ($output -join "`n")
  }
}

# ---- Test Cases ----

Write-Host "Testing lint-skill-sizes.ps1 thresholds..." -ForegroundColor White

# Test 1: File under 300 lines should be OK (exit 0)
Write-Host "`nTest group: Files under limits" -ForegroundColor Magenta
$tempDir1 = Join-Path ([System.IO.Path]::GetTempPath()) "skill-test-ok-$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
New-Item -ItemType Directory -Path $tempDir1 -Force | Out-Null
New-SkillFixture -Dir $tempDir1 -FileName "small-skill.md" -LineCount 100
$result1 = Invoke-Linter -SkillsDir $tempDir1
Write-TestResult "FileUnder300Lines_ExitCode0" ($result1.ExitCode -eq 0) "Expected exit code 0, got $($result1.ExitCode)"
Write-TestResult "FileUnder300Lines_NoError" (-not ($result1.Output -match '\] ERROR:')) "Output contained ERROR: $($result1.Output)"
Remove-Item -Path $tempDir1 -Recurse -Force -ErrorAction SilentlyContinue

# Test 2: File at exactly 300 lines should be OK (exit 0, no warning in non-verbose)
$tempDir2 = Join-Path ([System.IO.Path]::GetTempPath()) "skill-test-300-$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
New-Item -ItemType Directory -Path $tempDir2 -Force | Out-Null
New-SkillFixture -Dir $tempDir2 -FileName "boundary-skill.md" -LineCount 300
$result2 = Invoke-Linter -SkillsDir $tempDir2
Write-TestResult "FileAt300Lines_ExitCode0" ($result2.ExitCode -eq 0) "Expected exit code 0, got $($result2.ExitCode)"
Write-TestResult "FileAt300Lines_NoError" (-not ($result2.Output -match '\] ERROR:')) "Output contained ERROR"
Remove-Item -Path $tempDir2 -Recurse -Force -ErrorAction SilentlyContinue

# Test 3: File at 350 lines should be WARNING (exit 0, warning in verbose)
Write-Host "`nTest group: Warning threshold (300-479)" -ForegroundColor Magenta
$tempDir3 = Join-Path ([System.IO.Path]::GetTempPath()) "skill-test-warn-$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
New-Item -ItemType Directory -Path $tempDir3 -Force | Out-Null
New-SkillFixture -Dir $tempDir3 -FileName "warning-skill.md" -LineCount 350
$result3v = Invoke-Linter -SkillsDir $tempDir3 -Verbose
Write-TestResult "FileAt350Lines_ExitCode0" ($result3v.ExitCode -eq 0) "Expected exit code 0, got $($result3v.ExitCode)"
Write-TestResult "FileAt350Lines_WarningInVerbose" ($result3v.Output -match 'WARNING') "Expected WARNING in verbose output"

# Test 3b: Same file - WARNING suppressed in non-verbose mode
$result3nv = Invoke-Linter -SkillsDir $tempDir3
Write-TestResult "FileAt350Lines_WarningSuppressedNonVerbose" (-not ($result3nv.Output -match 'WARNING')) "WARNING should not appear without -VerboseOutput"
Remove-Item -Path $tempDir3 -Recurse -Force -ErrorAction SilentlyContinue

# Test 3c: File at exactly 479 lines should be WARNING, not CRITICAL
$tempDir3c = Join-Path ([System.IO.Path]::GetTempPath()) "skill-test-479-$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
New-Item -ItemType Directory -Path $tempDir3c -Force | Out-Null
New-SkillFixture -Dir $tempDir3c -FileName "at-479-skill.md" -LineCount 479
$result3c = Invoke-Linter -SkillsDir $tempDir3c -Verbose
Write-TestResult "FileAt479Lines_ExitCode0" ($result3c.ExitCode -eq 0) "Expected exit code 0, got $($result3c.ExitCode)"
Write-TestResult "FileAt479Lines_WarningNotCritical" ($result3c.Output -match 'WARNING' -and -not ($result3c.Output -match '\] CRITICAL:')) "Expected WARNING but not CRITICAL message"
Remove-Item -Path $tempDir3c -Recurse -Force -ErrorAction SilentlyContinue

# Test 4: File at exactly 480 lines should be CRITICAL warning (exit 0, always shown)
Write-Host "`nTest group: Critical warning threshold (480-500)" -ForegroundColor Magenta
$tempDir4a = Join-Path ([System.IO.Path]::GetTempPath()) "skill-test-480-$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
New-Item -ItemType Directory -Path $tempDir4a -Force | Out-Null
New-SkillFixture -Dir $tempDir4a -FileName "at-480-skill.md" -LineCount 480
$result4a = Invoke-Linter -SkillsDir $tempDir4a
Write-TestResult "FileAt480Lines_ExitCode0" ($result4a.ExitCode -eq 0) "Expected exit code 0, got $($result4a.ExitCode)"
Write-TestResult "FileAt480Lines_CriticalWarning" ($result4a.Output -match 'CRITICAL') "Expected CRITICAL at boundary, got: $($result4a.Output)"
Remove-Item -Path $tempDir4a -Recurse -Force -ErrorAction SilentlyContinue

# Test 4b: File at 490 lines should be CRITICAL warning
$tempDir4b = Join-Path ([System.IO.Path]::GetTempPath()) "skill-test-crit-$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
New-Item -ItemType Directory -Path $tempDir4b -Force | Out-Null
New-SkillFixture -Dir $tempDir4b -FileName "critical-skill.md" -LineCount 490
$result4b = Invoke-Linter -SkillsDir $tempDir4b
Write-TestResult "FileAt490Lines_ExitCode0" ($result4b.ExitCode -eq 0) "Expected exit code 0, got $($result4b.ExitCode)"
Write-TestResult "FileAt490Lines_CriticalWarning" ($result4b.Output -match 'CRITICAL') "Expected CRITICAL in output, got: $($result4b.Output)"
Remove-Item -Path $tempDir4b -Recurse -Force -ErrorAction SilentlyContinue

# Test 5: File at exactly 500 lines should be CRITICAL warning, NOT error (exit 0)
$tempDir5 = Join-Path ([System.IO.Path]::GetTempPath()) "skill-test-500-$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
New-Item -ItemType Directory -Path $tempDir5 -Force | Out-Null
New-SkillFixture -Dir $tempDir5 -FileName "boundary-500-skill.md" -LineCount 500
$result5 = Invoke-Linter -SkillsDir $tempDir5
Write-TestResult "FileAt500Lines_ExitCode0" ($result5.ExitCode -eq 0) "Expected exit code 0, got $($result5.ExitCode)"
Write-TestResult "FileAt500Lines_CriticalNotError" ($result5.Output -match 'CRITICAL' -and -not ($result5.Output -match '\] ERROR:')) "Expected CRITICAL without ERROR message"
Remove-Item -Path $tempDir5 -Recurse -Force -ErrorAction SilentlyContinue

# Test 6: File at exactly 501 lines should be ERROR (exit 1)
Write-Host "`nTest group: Error threshold (>500)" -ForegroundColor Magenta
$tempDir6a = Join-Path ([System.IO.Path]::GetTempPath()) "skill-test-501-$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
New-Item -ItemType Directory -Path $tempDir6a -Force | Out-Null
New-SkillFixture -Dir $tempDir6a -FileName "just-over-skill.md" -LineCount 501
$result6a = Invoke-Linter -SkillsDir $tempDir6a
Write-TestResult "FileAt501Lines_ExitCode1" ($result6a.ExitCode -eq 1) "Expected exit code 1 at boundary, got $($result6a.ExitCode)"
Write-TestResult "FileAt501Lines_ErrorMsg" ($result6a.Output -match '\] ERROR:') "Expected ERROR at exact boundary"
Remove-Item -Path $tempDir6a -Recurse -Force -ErrorAction SilentlyContinue

# Test 6b: File at 520 lines should be ERROR (exit 1)
$tempDir6b = Join-Path ([System.IO.Path]::GetTempPath()) "skill-test-err-$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
New-Item -ItemType Directory -Path $tempDir6b -Force | Out-Null
New-SkillFixture -Dir $tempDir6b -FileName "oversized-skill.md" -LineCount 520
$result6b = Invoke-Linter -SkillsDir $tempDir6b
Write-TestResult "FileAt520Lines_ExitCode1" ($result6b.ExitCode -eq 1) "Expected exit code 1, got $($result6b.ExitCode)"
Write-TestResult "FileAt520Lines_ErrorMsg" ($result6b.Output -match '\] ERROR:') "Expected ERROR in output"
Write-TestResult "FileAt520Lines_MustSplitMsg" ($result6b.Output -match 'MUST split') "Expected 'MUST split' in output"
Remove-Item -Path $tempDir6b -Recurse -Force -ErrorAction SilentlyContinue

# Test 7: Mixed files - one OK, one error -> should fail (exit 1)
Write-Host "`nTest group: Mixed file scenarios" -ForegroundColor Magenta
$tempDir7 = Join-Path ([System.IO.Path]::GetTempPath()) "skill-test-mix-$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
New-Item -ItemType Directory -Path $tempDir7 -Force | Out-Null
New-SkillFixture -Dir $tempDir7 -FileName "good-skill.md" -LineCount 100
New-SkillFixture -Dir $tempDir7 -FileName "bad-skill.md" -LineCount 520
$result7 = Invoke-Linter -SkillsDir $tempDir7
Write-TestResult "MixedFiles_ExitCode1" ($result7.ExitCode -eq 1) "Expected exit code 1, got $($result7.ExitCode)"
Write-TestResult "MixedFiles_ErrorForBadFile" ($result7.Output -match 'bad-skill\.md') "Expected error for bad-skill.md"
Remove-Item -Path $tempDir7 -Recurse -Force -ErrorAction SilentlyContinue

# Test 8: Subdirectory files should also be checked
Write-Host "`nTest group: Subdirectory support" -ForegroundColor Magenta
$tempDir8 = Join-Path ([System.IO.Path]::GetTempPath()) "skill-test-sub-$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
$subDir8 = Join-Path $tempDir8 "code-samples"
New-Item -ItemType Directory -Path $subDir8 -Force | Out-Null
New-SkillFixture -Dir $tempDir8 -FileName "root-skill.md" -LineCount 100
New-SkillFixture -Dir $subDir8 -FileName "sub-skill.md" -LineCount 520
$result8 = Invoke-Linter -SkillsDir $tempDir8
Write-TestResult "SubdirFile_ExitCode1" ($result8.ExitCode -eq 1) "Expected exit code 1 for oversized subdirectory file, got $($result8.ExitCode)"
Write-TestResult "SubdirFile_ErrorDetected" ($result8.Output -match 'sub-skill\.md') "Expected error for code-samples/sub-skill.md"
Remove-Item -Path $tempDir8 -Recurse -Force -ErrorAction SilentlyContinue

# Test 9: Empty directory should pass (exit 0)
Write-Host "`nTest group: Edge cases" -ForegroundColor Magenta
$tempDir9 = Join-Path ([System.IO.Path]::GetTempPath()) "skill-test-empty-$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
New-Item -ItemType Directory -Path $tempDir9 -Force | Out-Null
$result9 = Invoke-Linter -SkillsDir $tempDir9
Write-TestResult "EmptyDir_ExitCode0" ($result9.ExitCode -eq 0) "Expected exit code 0 for empty dir, got $($result9.ExitCode)"
Remove-Item -Path $tempDir9 -Recurse -Force -ErrorAction SilentlyContinue

# ---- Summary ----

Write-Host ""
Write-Host ("=" * 60)
Write-Host ("Tests passed: {0}" -f $script:TestsPassed) -ForegroundColor Green
Write-Host ("Tests failed: {0}" -f $script:TestsFailed) -ForegroundColor $(if ($script:TestsFailed -gt 0) { "Red" } else { "Green" })

if ($script:FailedTests.Count -gt 0) {
  Write-Host "Failed tests:" -ForegroundColor Red
  foreach ($t in $script:FailedTests) {
    Write-Host "  - $t" -ForegroundColor Red
  }
}

exit $script:TestsFailed
