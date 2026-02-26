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

# Wait for any external tool (lazygit, IDE, etc.) to release the index.lock
# before starting operations. This prevents contention with interactive git tools.
if (-not (Invoke-EnsureNoIndexLock)) {
  Write-Warning "index.lock still held after waiting. Proceeding anyway, but operations may fail."
}

$prettierGlobs = @('*.md', '*.markdown', '*.json', '*.asmdef', '*.asmref', '*.yml', '*.yaml')
$Paths = Get-StagedPathsForGlobs -DefaultPaths $Paths -Globs $prettierGlobs

$existingPaths = Get-ExistingPaths -Candidates $Paths

if ($existingPaths.Count -eq 0) {
  exit 0
}

$npx = Get-Command npx -ErrorAction SilentlyContinue
if (-not $npx) {
  Write-Error "npx not found; install Node.js to run Prettier."
  exit 1
}

$npxArgs = @('--yes', 'prettier', '--write', '--')
$npxArgs += $existingPaths
& npx @npxArgs
if ($LASTEXITCODE -ne 0) {
  Write-Error "Prettier formatting failed with exit code $LASTEXITCODE."
  exit $LASTEXITCODE
}

$gitAddExitCode = Invoke-GitAddWithRetry -Items $existingPaths -IndexLockPath $repositoryInfo.IndexLockPath
if ($gitAddExitCode -ne 0) {
  Write-Error "git add failed with exit code $gitAddExitCode."
  exit $gitAddExitCode
}
