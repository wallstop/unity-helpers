[CmdletBinding()]
param(
    [string] $UnityPath,
    [string] $ProjectPath,
    [string[]] $Platforms = @("EditMode", "PlayMode"),
    [string] $ResultsDirectory,
    [string[]] $AdditionalArguments = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-PathOrDefault {
    param(
        [string] $Value,
        [string] $Fallback
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        $Value = $Fallback
    }

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $null
    }

    $expanded = [Environment]::ExpandEnvironmentVariables($Value)
    return (Resolve-Path -LiteralPath $expanded).Path
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$defaultProject = Join-Path $scriptRoot ".." "TestProjects" "UnityPackageTests"
$defaultResults = Join-Path $scriptRoot ".." "artifacts" "unity-tests"

$UnityPath = Resolve-PathOrDefault $UnityPath ($env:UNITY_EDITOR_PATH)
if (-not $UnityPath) {
    throw "Unity editor path not provided. Set UNITY_EDITOR_PATH or pass -UnityPath."
}
if (-not (Test-Path $UnityPath)) {
    throw "Unity editor not found at '$UnityPath'."
}

$ProjectPath = Resolve-PathOrDefault $ProjectPath ($env:UNITY_PROJECT_PATH)
if (-not $ProjectPath) {
    $ProjectPath = Resolve-Path $defaultProject
}
if (-not (Test-Path $ProjectPath)) {
    throw "Unity project path '$ProjectPath' does not exist."
}

$ResultsDirectory = if (-not [string]::IsNullOrWhiteSpace($ResultsDirectory)) {
    $ResultsDirectory
} elseif (-not [string]::IsNullOrWhiteSpace($env:UNITY_TEST_RESULTS_DIR)) {
    $env:UNITY_TEST_RESULTS_DIR
} else {
    $defaultResults
}
$ResultsDirectory = [System.IO.Path]::GetFullPath($ResultsDirectory)
New-Item -ItemType Directory -Path $ResultsDirectory -Force | Out-Null

Write-Host "Unity Editor: $UnityPath"
Write-Host "Project Path: $ProjectPath"
Write-Host "Results Directory: $ResultsDirectory"
Write-Host "Platforms: $($Platforms -join ', ')"

if ($Platforms.Count -eq 0) {
    throw "Specify at least one platform (EditMode / PlayMode)."
}

$testFilter = $env:UNITY_TEST_FILTER
$testCategory = $env:UNITY_TEST_CATEGORY
$extraArgsEnv = $env:UNITY_TEST_EXTRA_ARGS
if (-not [string]::IsNullOrWhiteSpace($extraArgsEnv)) {
    $AdditionalArguments += $extraArgsEnv -split '\s+'
}

function Invoke-UnityTestRun {
    param(
        [string] $Platform
    )

    $resultFile = Join-Path $ResultsDirectory "$Platform-TestResults.xml"
    $logFile = Join-Path $ResultsDirectory "$Platform.log"

    $args = @(
        "-batchmode",
        "-quit",
        "-projectPath", $ProjectPath,
        "-runTests",
        "-testPlatform", $Platform,
        "-testResults", $resultFile,
        "-logFile", $logFile
    )

    if (-not [string]::IsNullOrWhiteSpace($testFilter)) {
        $args += @("-testFilter", $testFilter)
    }
    if (-not [string]::IsNullOrWhiteSpace($testCategory)) {
        $args += @("-testCategory", $testCategory)
    }
    if ($AdditionalArguments.Count -gt 0) {
        $args += $AdditionalArguments
    }

    Write-Host ""
    Write-Host "Running Unity tests for $Platform..."
    Write-Host "$UnityPath $($args -join ' ')"

    $process = Start-Process -FilePath $UnityPath -ArgumentList $args -NoNewWindow -PassThru -Wait
    if ($process.ExitCode -ne 0) {
        throw "Unity exited with code $($process.ExitCode) for $Platform. See $logFile."
    }
}

foreach ($platform in $Platforms) {
    Invoke-UnityTestRun -Platform $platform
}

Write-Host ""
Write-Host "Unity tests completed successfully."
