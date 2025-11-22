#!/usr/bin/env pwsh
param(
  [Parameter(ValueFromRemainingArguments = $true)]
  [string[]]$Paths
)

$helpersPath = Join-Path -Path $PSScriptRoot -ChildPath 'git-staging-helpers.ps1'
. $helpersPath

try {
  Assert-GitAvailable | Out-Null
  $repositoryInfo = Get-GitRepositoryInfo
} catch {
  Write-Error $_.Exception.Message
  exit 1
}

if (-not $Paths -or $Paths.Count -eq 0) {
  $Paths = Get-StagedPathsForGlobs -Globs @('*.cs')
}

$existingPaths = Get-ExistingPaths -Candidates $Paths

if ($existingPaths.Count -eq 0) {
  exit 0
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
  Write-Host "dotnet not found; skipping CSharpier formatting."
  exit 0
}

# Restore tools quietly in case the manifest changed.
& dotnet tool restore > $null 2>&1

$dotnetArgs = @('tool', 'run', 'csharpier')
$dotnetArgs += 'format'
$dotnetArgs += $existingPaths
& dotnet @dotnetArgs
if ($LASTEXITCODE -ne 0) {
  Write-Error "CSharpier formatting failed with exit code $LASTEXITCODE."
  exit $LASTEXITCODE
}

$gitAddExitCode = Invoke-GitAddWithRetry -Items $existingPaths -IndexLockPath $repositoryInfo.IndexLockPath
if ($gitAddExitCode -ne 0) {
  Write-Error "git add failed with exit code $gitAddExitCode."
  exit $gitAddExitCode
}
