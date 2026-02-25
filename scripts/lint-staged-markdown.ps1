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

$markdownGlobs = @('*.md', '*.markdown')
$Paths = Get-StagedPathsForGlobs -DefaultPaths $Paths -Globs $markdownGlobs

$existingPaths = Get-ExistingPaths -Candidates $Paths

if ($existingPaths.Count -eq 0) {
  exit 0
}

$npx = Get-Command npx -ErrorAction SilentlyContinue
if (-not $npx) {
  Write-Error "npx not found; install Node.js to run markdownlint."
  exit 1
}

$npxArgs = @(
  '--yes',
  '--package',
  'markdownlint-cli',
  'markdownlint',
  '--config',
  '.markdownlint.json',
  '--ignore-path',
  '.markdownlintignore',
  '--fix'
)
$npxArgs += '--'
$npxArgs += $existingPaths
& npx @npxArgs
if ($LASTEXITCODE -ne 0) {
  Write-Error "markdownlint failed with exit code $LASTEXITCODE."
  exit $LASTEXITCODE
}

$gitAddExitCode = Invoke-GitAddWithRetry -Items $existingPaths -IndexLockPath $repositoryInfo.IndexLockPath
if ($gitAddExitCode -ne 0) {
  Write-Error "git add failed with exit code $gitAddExitCode."
  exit $gitAddExitCode
}
