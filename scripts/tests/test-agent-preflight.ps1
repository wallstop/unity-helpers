Param(
    [switch]$VerboseOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:TestsPassed = 0
$script:TestsFailed = 0
$script:FailedTests = @()

function Write-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Message = ''
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
        $script:FailedTests += $TestName
    }
}

function New-TestRepo {
    param(
        [switch]$ConfigurePushDefaults,
        [string[]]$GitIgnorePatterns
    )

    $tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) "agent-preflight-test-$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
    $scriptsDir = Join-Path $tempRoot 'scripts'
    New-Item -ItemType Directory -Path $scriptsDir -Force | Out-Null

    $repoRoot = Split-Path -Parent $PSScriptRoot | Split-Path -Parent
    Copy-Item (Join-Path $repoRoot 'scripts/agent-preflight.ps1') (Join-Path $scriptsDir 'agent-preflight.ps1') -Force
    Copy-Item (Join-Path $repoRoot 'scripts/git-staging-helpers.ps1') (Join-Path $scriptsDir 'git-staging-helpers.ps1') -Force
    Copy-Item (Join-Path $repoRoot 'scripts/generate-meta.sh') (Join-Path $scriptsDir 'generate-meta.sh') -Force
    Copy-Item (Join-Path $repoRoot 'scripts/configure-git-defaults.ps1') (Join-Path $scriptsDir 'configure-git-defaults.ps1') -Force

    if ($null -ne $GitIgnorePatterns -and $GitIgnorePatterns.Count -gt 0) {
        Set-Content -Path (Join-Path $tempRoot '.gitignore') -Value $GitIgnorePatterns -Encoding UTF8
    }

    Push-Location $tempRoot
    try {
        git init -q
        git add .
        git -c user.email=test@example.com -c user.name=test commit -q -m 'init'
        if ($ConfigurePushDefaults) {
            git config --local push.autoSetupRemote true
            git config --local push.default simple
        }
    }
    finally {
        Pop-Location
    }

    return $tempRoot
}

function Invoke-Preflight {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoPath,
        [string[]]$Arguments,
        [hashtable]$EnvOverrides
    )

    $previousValues = @{}
    if ($null -ne $EnvOverrides) {
        foreach ($key in $EnvOverrides.Keys) {
            if (Test-Path "Env:$key") {
                $previousValues[$key] = [Environment]::GetEnvironmentVariable($key)
            }
            else {
                $previousValues[$key] = $null
            }

            [Environment]::SetEnvironmentVariable($key, [string]$EnvOverrides[$key])
        }
    }

    Push-Location $RepoPath
    try {
        $output = & pwsh -NoProfile -File scripts/agent-preflight.ps1 @Arguments 2>&1
        return @{
            ExitCode = $LASTEXITCODE
            Output = ($output -join "`n")
        }
    }
    finally {
        Pop-Location

        if ($null -ne $EnvOverrides) {
            foreach ($key in $EnvOverrides.Keys) {
                [Environment]::SetEnvironmentVariable($key, $previousValues[$key])
            }
        }
    }
}

function Get-StagedPaths {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoPath
    )

    Push-Location $RepoPath
    try {
        $output = & git diff --cached --name-only --diff-filter=ACM 2>&1
        if ($LASTEXITCODE -ne 0) {
            return @()
        }

        return @($output | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    }
    finally {
        Pop-Location
    }
}

function New-NpxStub {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoPath,
        [Parameter(Mandatory = $true)]
        [ValidateSet('Pass', 'FailLint')]
        [string]$Mode
    )

    $lintExitCode = if ($Mode -eq 'FailLint') { '1' } else { '0' }

    if ($IsWindows) {
        $stubPath = Join-Path $RepoPath 'npx-stub.cmd'
        $script = @"
@echo off
if "%~1"=="--no-install" if "%~2"=="cspell" if "%~3"=="--version" exit /b 0
if "%~1"=="--no-install" if "%~2"=="cspell" if "%~3"=="lint" exit /b $lintExitCode
exit /b 0
"@
        Set-Content -Path $stubPath -Value $script -Encoding ascii
    }
    else {
        $stubPath = Join-Path $RepoPath 'npx-stub.sh'
        $script = @'
#!/usr/bin/env bash
set -e

if [[ "${1:-}" == "--no-install" && "${2:-}" == "cspell" && "${3:-}" == "--version" ]]; then
  exit 0
fi

if [[ "${1:-}" == "--no-install" && "${2:-}" == "cspell" && "${3:-}" == "lint" ]]; then
  exit __LINT_EXIT_CODE__
fi

exit 0
'@
        $script = $script.Replace('__LINT_EXIT_CODE__', $lintExitCode)
        $script = $script -replace "`r`n", "`n"
        Set-Content -Path $stubPath -Value $script -Encoding ascii
        & chmod +x $stubPath | Out-Null
    }

    return $stubPath
}

Write-Host 'Testing agent-preflight.ps1...' -ForegroundColor White

# Test 1: No changed files should exit successfully
Write-Host "`nTest group: baseline behavior" -ForegroundColor Magenta
$repo1 = New-TestRepo -ConfigurePushDefaults
try {
    $result1 = Invoke-Preflight -RepoPath $repo1 -Arguments @()
    Write-TestResult 'NoChanges_ExitCode0' ($result1.ExitCode -eq 0) "Expected exit code 0, got $($result1.ExitCode)"
    Write-TestResult 'NoChanges_Message' ($result1.Output -match 'No changed files detected') 'Expected no-changes message'
}
finally {
    Remove-Item -Path $repo1 -Recurse -Force -ErrorAction SilentlyContinue
}

# Test 2: Missing meta file should fail
Write-Host "`nTest group: missing meta detection" -ForegroundColor Magenta
$repo2 = New-TestRepo -ConfigurePushDefaults
try {
    $runtimeDir = Join-Path $repo2 'Runtime'
    New-Item -ItemType Directory -Path $runtimeDir -Force | Out-Null
    $filePath = Join-Path $runtimeDir 'MyFeature.cs'
    Set-Content -Path $filePath -Value 'public sealed class MyFeature {}' -Encoding UTF8

    $result2 = Invoke-Preflight -RepoPath $repo2 -Arguments @('-Paths', 'Runtime/MyFeature.cs')
    Write-TestResult 'MissingMeta_ExitCode1' ($result2.ExitCode -eq 1) "Expected exit code 1, got $($result2.ExitCode)"
    Write-TestResult 'MissingMeta_ErrorMessage' ($result2.Output -match 'Missing \.meta files detected') 'Expected missing meta error message'
    Write-TestResult 'MissingMeta_ListsPath' ($result2.Output -match 'Runtime/MyFeature\.cs') 'Expected missing path to be listed in output'
}
finally {
    Remove-Item -Path $repo2 -Recurse -Force -ErrorAction SilentlyContinue
}

# Test 3: Fix mode should auto-generate missing meta files
Write-Host "`nTest group: auto-fix mode" -ForegroundColor Magenta
$repo3 = New-TestRepo -ConfigurePushDefaults
try {
    $editorNestedDir = Join-Path $repo3 'Editor/Nested'
    New-Item -ItemType Directory -Path $editorNestedDir -Force | Out-Null
    $filePath = Join-Path $editorNestedDir 'Tool.cs'
    Set-Content -Path $filePath -Value 'public sealed class Tool {}' -Encoding UTF8

    Push-Location $repo3
    try {
        git add Editor/Nested/Tool.cs
    }
    finally {
        Pop-Location
    }

    $result3 = Invoke-Preflight -RepoPath $repo3 -Arguments @('-Fix', '-Paths', 'Editor/Nested/Tool.cs')
    Write-TestResult 'FixMode_ExitCode0' ($result3.ExitCode -eq 0) "Expected exit code 0, got $($result3.ExitCode). Output: $($result3.Output)"
    Write-TestResult 'FixMode_FileMetaCreated' (Test-Path (Join-Path $repo3 'Editor/Nested/Tool.cs.meta')) 'Expected file .meta to be created'
    Write-TestResult 'FixMode_DirMetaCreated' (Test-Path (Join-Path $repo3 'Editor/Nested.meta')) 'Expected directory .meta to be created'

    $staged3 = Get-StagedPaths -RepoPath $repo3
    Write-TestResult 'FixMode_FileMetaStaged' ($staged3 -contains 'Editor/Nested/Tool.cs.meta') 'Expected file .meta to be staged by -Fix mode'
    Write-TestResult 'FixMode_DirMetaStaged' ($staged3 -contains 'Editor/Nested.meta') 'Expected directory .meta to be staged by -Fix mode'
}
finally {
    Remove-Item -Path $repo3 -Recurse -Force -ErrorAction SilentlyContinue
}

# Test 4: Preflight without -Fix should fail when staged source has unstaged .meta companion
Write-Host "`nTest group: staged companion drift detection" -ForegroundColor Magenta
$repo4 = New-TestRepo -ConfigurePushDefaults
try {
    $runtimeDir = Join-Path $repo4 'Runtime'
    New-Item -ItemType Directory -Path $runtimeDir -Force | Out-Null
    Set-Content -Path (Join-Path $runtimeDir 'StagedOnly.cs') -Value 'public sealed class StagedOnly {}' -Encoding UTF8
    Set-Content -Path (Join-Path $runtimeDir 'StagedOnly.cs.meta') -Value @'
fileFormatVersion: 2
guid: 0123456789abcdef0123456789abcdef
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData:
  assetBundleName:
  assetBundleVariant:
'@ -Encoding UTF8

    Push-Location $repo4
    try {
        git add Runtime/StagedOnly.cs
    }
    finally {
        Pop-Location
    }

    $result4 = Invoke-Preflight -RepoPath $repo4 -Arguments @('-Paths', 'Runtime/StagedOnly.cs')
    Write-TestResult 'UnstagedCompanion_ExitCode1' ($result4.ExitCode -eq 1) "Expected exit code 1, got $($result4.ExitCode)"
    Write-TestResult 'UnstagedCompanion_ErrorMessage' ($result4.Output -match 'Unstaged \.meta companion files detected') 'Expected unstaged companion error message'

    $staged4 = Get-StagedPaths -RepoPath $repo4
    Write-TestResult 'UnstagedCompanion_NotAutoStagedWithoutFix' (-not ($staged4 -contains 'Runtime/StagedOnly.cs.meta')) 'Did not expect .meta to be staged without -Fix'
}
finally {
    Remove-Item -Path $repo4 -Recurse -Force -ErrorAction SilentlyContinue
}

# Test 5: Preflight -Fix should auto-stage unstaged .meta companions
Write-Host "`nTest group: staged companion auto-stage" -ForegroundColor Magenta
$repo5 = New-TestRepo -ConfigurePushDefaults
try {
    $editorDir = Join-Path $repo5 'Editor/Tools'
    New-Item -ItemType Directory -Path $editorDir -Force | Out-Null
    Set-Content -Path (Join-Path $editorDir 'Window.cs') -Value 'public sealed class Window {}' -Encoding UTF8
    Set-Content -Path (Join-Path $editorDir 'Window.cs.meta') -Value @'
fileFormatVersion: 2
guid: fedcba9876543210fedcba9876543210
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData:
  assetBundleName:
  assetBundleVariant:
'@ -Encoding UTF8
    Set-Content -Path (Join-Path $repo5 'Editor.meta') -Value @'
fileFormatVersion: 2
guid: 11111111111111111111111111111111
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
'@ -Encoding UTF8
    Set-Content -Path (Join-Path $repo5 'Editor/Tools.meta') -Value @'
fileFormatVersion: 2
guid: 22222222222222222222222222222222
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
'@ -Encoding UTF8

    Push-Location $repo5
    try {
        git add Editor/Tools/Window.cs
    }
    finally {
        Pop-Location
    }

    $result5 = Invoke-Preflight -RepoPath $repo5 -Arguments @('-Fix', '-Paths', 'Editor/Tools/Window.cs')
    Write-TestResult 'UnstagedCompanionFix_ExitCode0' ($result5.ExitCode -eq 0) "Expected exit code 0, got $($result5.ExitCode). Output: $($result5.Output)"
    Write-TestResult 'UnstagedCompanionFix_StageMessage' ($result5.Output -match 'Auto-staging unstaged \.meta companions') 'Expected auto-stage message'

    $staged5 = Get-StagedPaths -RepoPath $repo5
    Write-TestResult 'UnstagedCompanionFix_FileMetaStaged' ($staged5 -contains 'Editor/Tools/Window.cs.meta') 'Expected file companion .meta to be staged'
    Write-TestResult 'UnstagedCompanionFix_DirMetaStaged' ($staged5 -contains 'Editor/Tools.meta') 'Expected directory companion .meta to be staged'
}
finally {
    Remove-Item -Path $repo5 -Recurse -Force -ErrorAction SilentlyContinue
}

# Test 6: -Paths scoping should only touch staged files in the specified scope
Write-Host "`nTest group: path-scoped staged companion behavior" -ForegroundColor Magenta
$repo6 = New-TestRepo -ConfigurePushDefaults
try {
    $runtimeDir = Join-Path $repo6 'Runtime'
    New-Item -ItemType Directory -Path $runtimeDir -Force | Out-Null

    Set-Content -Path (Join-Path $runtimeDir 'ScopedA.cs') -Value 'public sealed class ScopedA {}' -Encoding UTF8
    Set-Content -Path (Join-Path $runtimeDir 'ScopedB.cs') -Value 'public sealed class ScopedB {}' -Encoding UTF8

    Set-Content -Path (Join-Path $runtimeDir 'ScopedA.cs.meta') -Value @'
fileFormatVersion: 2
guid: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData:
  assetBundleName:
  assetBundleVariant:
'@ -Encoding UTF8
    Set-Content -Path (Join-Path $runtimeDir 'ScopedB.cs.meta') -Value @'
fileFormatVersion: 2
guid: bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData:
  assetBundleName:
  assetBundleVariant:
'@ -Encoding UTF8

    Push-Location $repo6
    try {
        git add Runtime/ScopedA.cs Runtime/ScopedB.cs
    }
    finally {
        Pop-Location
    }

    $result6 = Invoke-Preflight -RepoPath $repo6 -Arguments @('-Fix', '-Paths', 'Runtime/ScopedA.cs')
    Write-TestResult 'ScopedPaths_ExitCode0' ($result6.ExitCode -eq 0) "Expected exit code 0, got $($result6.ExitCode). Output: $($result6.Output)"

    $staged6 = Get-StagedPaths -RepoPath $repo6
    Write-TestResult 'ScopedPaths_StagesScopedCompanion' ($staged6 -contains 'Runtime/ScopedA.cs.meta') 'Expected scoped .meta companion to be staged'
    Write-TestResult 'ScopedPaths_DoesNotStageUnscopedCompanion' (-not ($staged6 -contains 'Runtime/ScopedB.cs.meta')) 'Did not expect unscoped .meta companion to be staged'
}
finally {
    Remove-Item -Path $repo6 -Recurse -Force -ErrorAction SilentlyContinue
}

# Test 7: -Fix should fail with clear diagnostics if index.lock contention blocks staging
Write-Host "`nTest group: lock contention diagnostics" -ForegroundColor Magenta
$repo7 = New-TestRepo -ConfigurePushDefaults
try {
    $runtimeDir = Join-Path $repo7 'Runtime'
    New-Item -ItemType Directory -Path $runtimeDir -Force | Out-Null

    Set-Content -Path (Join-Path $runtimeDir 'LockCase.cs') -Value 'public sealed class LockCase {}' -Encoding UTF8
    Set-Content -Path (Join-Path $runtimeDir 'LockCase.cs.meta') -Value @'
fileFormatVersion: 2
guid: cccccccccccccccccccccccccccccccc
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData:
  assetBundleName:
  assetBundleVariant:
'@ -Encoding UTF8

    Push-Location $repo7
    try {
        git add Runtime/LockCase.cs
        Set-Content -Path (Join-Path $repo7 '.git/index.lock') -Value 'lock' -Encoding UTF8
    }
    finally {
        Pop-Location
    }

    $result7 = Invoke-Preflight -RepoPath $repo7 -Arguments @('-Fix', '-Paths', 'Runtime/LockCase.cs') -EnvOverrides @{
        GIT_LOCK_MAX_ATTEMPTS = '2'
        GIT_LOCK_INITIAL_DELAY_MS = '1'
        GIT_LOCK_MAX_DELAY_MS = '2'
        GIT_LOCK_WAIT_TIMEOUT_MS = '1'
        GIT_LOCK_POLL_INTERVAL_MS = '1'
        GIT_LOCK_INITIAL_WAIT_MS = '1'
    }

    Write-TestResult 'LockContention_ExitCode1' ($result7.ExitCode -eq 1) "Expected exit code 1, got $($result7.ExitCode)"
    Write-TestResult 'LockContention_ErrorMessage' ($result7.Output -match 'Failed to stage one or more \.meta companion files') 'Expected lock contention staging failure message'
    Write-TestResult 'LockContention_RecoveryHint' ($result7.Output -match 'Close other git operations') 'Expected actionable recovery hint in output'
}
finally {
    Remove-Item -Path $repo7 -Recurse -Force -ErrorAction SilentlyContinue
}

# Test 8: Changed markdown files should pass when cspell lint succeeds
Write-Host "`nTest group: spelling checks on changed files" -ForegroundColor Magenta
$repo8 = New-TestRepo -ConfigurePushDefaults
try {
    Set-Content -Path (Join-Path $repo8 'README.md') -Value 'Spelling check baseline.' -Encoding UTF8
    $npxStub8 = New-NpxStub -RepoPath $repo8 -Mode Pass
    $result8 = Invoke-Preflight -RepoPath $repo8 -Arguments @('-Paths', 'README.md') -EnvOverrides @{
        AGENT_PREFLIGHT_NPX_COMMAND = $npxStub8
    }

    Write-TestResult 'SpellingChecks_ExitCode0' ($result8.ExitCode -eq 0) "Expected exit code 0, got $($result8.ExitCode). Output: $($result8.Output)"
    Write-TestResult 'SpellingChecks_Message' ($result8.Output -match 'Checking spelling on changed markdown files') 'Expected spelling check status message'
}
finally {
    Remove-Item -Path $repo8 -Recurse -Force -ErrorAction SilentlyContinue
}

# Test 9: Changed markdown typos should fail preflight with actionable output
Write-Host "`nTest group: spelling failure diagnostics" -ForegroundColor Magenta
$repo9 = New-TestRepo -ConfigurePushDefaults
try {
    Set-Content -Path (Join-Path $repo9 'README.md') -Value 'teh typo to trigger spell failure.' -Encoding UTF8
    $npxStub9 = New-NpxStub -RepoPath $repo9 -Mode FailLint
    $result9 = Invoke-Preflight -RepoPath $repo9 -Arguments @('-Paths', 'README.md') -EnvOverrides @{
        AGENT_PREFLIGHT_NPX_COMMAND = $npxStub9
    }

    Write-TestResult 'SpellingFailure_ExitCode1' ($result9.ExitCode -eq 1) "Expected exit code 1, got $($result9.ExitCode). Output: $($result9.Output)"
    Write-TestResult 'SpellingFailure_ErrorMessage' ($result9.Output -match 'Spelling errors detected in changed markdown files') 'Expected spelling failure message'
    Write-TestResult 'SpellingFailure_RecoveryHint' ($result9.Output -match 'npm run lint:spelling') 'Expected recovery command hint'
}
finally {
    Remove-Item -Path $repo9 -Recurse -Force -ErrorAction SilentlyContinue
}

# Test 10: Missing cspell should fail with an actionable dependency message
Write-Host "`nTest group: spelling missing dependency diagnostics" -ForegroundColor Magenta
$repo10 = New-TestRepo -ConfigurePushDefaults
try {
    Set-Content -Path (Join-Path $repo10 'README.md') -Value 'Spelling check baseline.' -Encoding UTF8
    $result10 = Invoke-Preflight -RepoPath $repo10 -Arguments @('-Paths', 'README.md')

    Write-TestResult 'SpellingMissingDependency_ExitCode1' ($result10.ExitCode -eq 1) "Expected exit code 1 when cspell is unavailable, got $($result10.ExitCode). Output: $($result10.Output)"
    Write-TestResult 'SpellingMissingDependency_Message' ($result10.Output -match 'cspell is not installed in this repository') 'Expected missing-cspell diagnostic message'
}
finally {
    Remove-Item -Path $repo10 -Recurse -Force -ErrorAction SilentlyContinue
}

# Test 11: Missing push.autoSetupRemote should fail preflight
Write-Host "`nTest group: git push config detection" -ForegroundColor Magenta
$repo11 = New-TestRepo
try {
    $result11 = Invoke-Preflight -RepoPath $repo11 -Arguments @('-Paths', 'nonexistent/should-not-match')
    Write-TestResult 'PushConfigMissing_ExitCode1' ($result11.ExitCode -eq 1) "Expected exit code 1 when push.autoSetupRemote unset, got $($result11.ExitCode). Output: $($result11.Output)"
    Write-TestResult 'PushConfigMissing_ErrorMessage' ($result11.Output -match 'Git push defaults are not configured') 'Expected push config error message'
    Write-TestResult 'PushConfigMissing_RemediationHint' ($result11.Output -match 'npm run agent:preflight:fix') 'Expected remediation hint referencing agent:preflight:fix'
}
finally {
    Remove-Item -Path $repo11 -Recurse -Force -ErrorAction SilentlyContinue
}

# Test 12: -Fix mode should restore push config and re-run green
Write-Host "`nTest group: git push config auto-fix" -ForegroundColor Magenta
$repo12 = New-TestRepo
try {
    $result12 = Invoke-Preflight -RepoPath $repo12 -Arguments @('-Fix', '-Paths', 'nonexistent/should-not-match')
    Write-TestResult 'PushConfigFix_ExitCode0' ($result12.ExitCode -eq 0) "Expected exit code 0 after -Fix, got $($result12.ExitCode). Output: $($result12.Output)"

    Push-Location $repo12
    try {
        $autoSetup = git config --local --get push.autoSetupRemote 2>$null
        $pushDefault = git config --local --get push.default 2>$null
    }
    finally {
        Pop-Location
    }
    Write-TestResult 'PushConfigFix_AutoSetupRemote' ($autoSetup -eq 'true') "Expected push.autoSetupRemote=true after -Fix, got '$autoSetup'"
    Write-TestResult 'PushConfigFix_PushDefault' ($pushDefault -eq 'simple') "Expected push.default=simple after -Fix, got '$pushDefault'"

    $result12b = Invoke-Preflight -RepoPath $repo12 -Arguments @('-Paths', 'nonexistent/should-not-match')
    Write-TestResult 'PushConfigFix_RerunGreen' ($result12b.ExitCode -eq 0) "Expected rerun to be green, got $($result12b.ExitCode). Output: $($result12b.Output)"
}
finally {
    Remove-Item -Path $repo12 -Recurse -Force -ErrorAction SilentlyContinue
}

# Test 13: pre-push.txt at repo root should fail and -Fix removes it (gitignored)
Write-Host "`nTest group: stray pre-push.txt detection" -ForegroundColor Magenta
$repo13 = New-TestRepo -ConfigurePushDefaults -GitIgnorePatterns @('pre-push.txt*')
try {
    $hooksDir = Join-Path $repo13 '.githooks'
    New-Item -ItemType Directory -Path $hooksDir -Force | Out-Null
    Set-Content -Path (Join-Path $hooksDir 'pre-push') -Value '#!/usr/bin/env bash' -Encoding UTF8
    Set-Content -Path (Join-Path $repo13 'pre-push.txt') -Value 'fatal: ... no upstream branch' -Encoding UTF8

    $result13 = Invoke-Preflight -RepoPath $repo13 -Arguments @('-Paths', 'nonexistent/should-not-match')
    Write-TestResult 'StrayPrePushTxt_ExitCode1' ($result13.ExitCode -eq 1) "Expected exit code 1 when pre-push.txt exists, got $($result13.ExitCode). Output: $($result13.Output)"
    Write-TestResult 'StrayPrePushTxt_ErrorMessage' ($result13.Output -match 'Stray git-hook artifact file') 'Expected stray artifact error message'
    Write-TestResult 'StrayPrePushTxt_ListsPath' ($result13.Output -match 'pre-push\.txt') 'Expected pre-push.txt path in output'

    $result13fix = Invoke-Preflight -RepoPath $repo13 -Arguments @('-Fix', '-Paths', 'nonexistent/should-not-match')
    Write-TestResult 'StrayPrePushTxtFix_ExitCode0' ($result13fix.ExitCode -eq 0) "Expected exit code 0 after -Fix, got $($result13fix.ExitCode). Output: $($result13fix.Output)"
    Write-TestResult 'StrayPrePushTxtFix_FileDeleted' (-not (Test-Path (Join-Path $repo13 'pre-push.txt'))) 'Expected pre-push.txt to be deleted by -Fix'
}
finally {
    Remove-Item -Path $repo13 -Recurse -Force -ErrorAction SilentlyContinue
}

# Test 14: .githooks/pre-merge-commit.tmp should fail and -Fix removes it (gitignored)
Write-Host "`nTest group: stray hook tmp artifact detection" -ForegroundColor Magenta
$repo14 = New-TestRepo -ConfigurePushDefaults -GitIgnorePatterns @('*.tmp')
try {
    $hooksDir = Join-Path $repo14 '.githooks'
    New-Item -ItemType Directory -Path $hooksDir -Force | Out-Null
    Set-Content -Path (Join-Path $hooksDir 'pre-merge-commit') -Value '#!/usr/bin/env bash' -Encoding UTF8
    Set-Content -Path (Join-Path $hooksDir 'pre-merge-commit.tmp') -Value 'temp output' -Encoding UTF8

    $result14 = Invoke-Preflight -RepoPath $repo14 -Arguments @('-Paths', 'nonexistent/should-not-match')
    Write-TestResult 'StrayHookTmp_ExitCode1' ($result14.ExitCode -eq 1) "Expected exit code 1 when hook .tmp exists, got $($result14.ExitCode). Output: $($result14.Output)"
    Write-TestResult 'StrayHookTmp_ListsPath' ($result14.Output -match 'pre-merge-commit\.tmp') 'Expected .githooks/pre-merge-commit.tmp in output'

    $result14fix = Invoke-Preflight -RepoPath $repo14 -Arguments @('-Fix', '-Paths', 'nonexistent/should-not-match')
    Write-TestResult 'StrayHookTmpFix_ExitCode0' ($result14fix.ExitCode -eq 0) "Expected exit code 0 after -Fix, got $($result14fix.ExitCode). Output: $($result14fix.Output)"
    Write-TestResult 'StrayHookTmpFix_FileDeleted' (-not (Test-Path (Join-Path $hooksDir 'pre-merge-commit.tmp'))) 'Expected pre-merge-commit.tmp to be deleted by -Fix'
}
finally {
    Remove-Item -Path $repo14 -Recurse -Force -ErrorAction SilentlyContinue
}

# Test 15: Generalized discovery - a custom hook file drives detection of <name>.txt
Write-Host "`nTest group: generalized stray artifact discovery" -ForegroundColor Magenta
$repo15 = New-TestRepo -ConfigurePushDefaults
try {
    $hooksDir = Join-Path $repo15 '.githooks'
    New-Item -ItemType Directory -Path $hooksDir -Force | Out-Null
    Set-Content -Path (Join-Path $hooksDir 'post-checkout') -Value '#!/usr/bin/env bash' -Encoding UTF8
    Set-Content -Path (Join-Path $repo15 'post-checkout.txt') -Value 'redirected output' -Encoding UTF8

    $result15 = Invoke-Preflight -RepoPath $repo15 -Arguments @('-Paths', 'nonexistent/should-not-match')
    Write-TestResult 'GeneralizedDiscovery_ExitCode1' ($result15.ExitCode -eq 1) "Expected exit code 1 when post-checkout.txt exists, got $($result15.ExitCode). Output: $($result15.Output)"
    Write-TestResult 'GeneralizedDiscovery_CatchesNewHook' ($result15.Output -match 'post-checkout\.txt') 'Expected discovery to catch artifact derived from custom hook name'
}
finally {
    Remove-Item -Path $repo15 -Recurse -Force -ErrorAction SilentlyContinue
}

# Test 16: -Fix must NOT delete stray-pattern files that are not gitignored
Write-Host "`nTest group: gitignore-safety gate on auto-deletion" -ForegroundColor Magenta
# Deliberately construct a repo WITHOUT a .gitignore entry for pre-push.txt.
# The file still matches the error-log pattern, so it must be reported as a
# failure — but -Fix must refuse to delete it (safety).
$repo16 = New-TestRepo -ConfigurePushDefaults
try {
    $hooksDir = Join-Path $repo16 '.githooks'
    New-Item -ItemType Directory -Path $hooksDir -Force | Out-Null
    Set-Content -Path (Join-Path $hooksDir 'pre-push') -Value '#!/usr/bin/env bash' -Encoding UTF8
    $strayPath = Join-Path $repo16 'pre-push.txt'
    Set-Content -Path $strayPath -Value 'intentional user note; not gitignored' -Encoding UTF8

    # Sanity: confirm the file is NOT gitignored in this test repo.
    Push-Location $repo16
    try {
        & git check-ignore -q -- 'pre-push.txt' 2>$null | Out-Null
        $preCheckExit = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }
    Write-TestResult 'GitignoreSafety_PreconditionNotIgnored' ($preCheckExit -eq 1) "Expected pre-push.txt to be NOT gitignored in test repo (git check-ignore exit 1), got $preCheckExit"

    # Check-only mode: must fail with differentiated messaging.
    $result16 = Invoke-Preflight -RepoPath $repo16 -Arguments @('-Paths', 'nonexistent/should-not-match')
    Write-TestResult 'GitignoreSafety_CheckExitCode1' ($result16.ExitCode -eq 1) "Expected exit code 1 when stray pre-push.txt is not gitignored, got $($result16.ExitCode). Output: $($result16.Output)"
    Write-TestResult 'GitignoreSafety_CheckListsPath' ($result16.Output -match 'pre-push\.txt') 'Expected pre-push.txt in check-only output'
    Write-TestResult 'GitignoreSafety_CheckDifferentiates' ($result16.Output -match 'NOT gitignored') 'Expected check-only output to surface the "NOT gitignored" category'
    Write-TestResult 'GitignoreSafety_CheckFileStillExists' (Test-Path -LiteralPath $strayPath) 'Expected pre-push.txt to still exist after check-only run'

    # -Fix mode: must NOT delete and MUST still fail.
    $result16fix = Invoke-Preflight -RepoPath $repo16 -Arguments @('-Fix', '-Paths', 'nonexistent/should-not-match')
    Write-TestResult 'GitignoreSafety_FixExitCode1' ($result16fix.ExitCode -eq 1) "Expected -Fix exit code 1 (refused delete counts as failure), got $($result16fix.ExitCode). Output: $($result16fix.Output)"
    Write-TestResult 'GitignoreSafety_FixDidNotDelete' (Test-Path -LiteralPath $strayPath) 'Expected pre-push.txt to NOT be deleted under -Fix when not gitignored'
    Write-TestResult 'GitignoreSafety_FixMentionsGitignore' ($result16fix.Output -match 'gitignore') 'Expected -Fix output to reference gitignore safety/remediation'
}
finally {
    Remove-Item -Path $repo16 -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host ''
Write-Host ('=' * 60)
Write-Host ("Tests passed: {0}" -f $script:TestsPassed) -ForegroundColor Green
Write-Host ("Tests failed: {0}" -f $script:TestsFailed) -ForegroundColor $(if ($script:TestsFailed -gt 0) { 'Red' } else { 'Green' })

if ($script:FailedTests.Count -gt 0) {
    Write-Host 'Failed tests:' -ForegroundColor Red
    foreach ($failed in $script:FailedTests) {
        Write-Host "  - $failed" -ForegroundColor Red
    }
}

exit $script:TestsFailed
