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

$prettierGlobs = @('*.md', '*.markdown', '*.json', '*.jsonc', '*.asmdef', '*.asmref', '*.yml', '*.yaml', '*.js')
$Paths = Get-StagedPathsForGlobs -DefaultPaths $Paths -Globs $prettierGlobs

$existingPaths = Get-ExistingPaths -Candidates $Paths

if ($existingPaths.Count -eq 0) {
  exit 0
}

$node = Get-Command node -ErrorAction SilentlyContinue
if (-not $node) {
  Write-Error "node not found; install Node.js and run npm install to run Prettier."
  exit 1
}

$prettierScript = Join-Path -Path $repositoryInfo.RepositoryRoot -ChildPath 'scripts/run-prettier.js'
$prettierArgs = @($prettierScript, '--write', '--')
$prettierArgs += $existingPaths
& node @prettierArgs
if ($LASTEXITCODE -ne 0) {
  Write-Error "Prettier formatting failed with exit code $LASTEXITCODE."
  exit $LASTEXITCODE
}

$gitAddExitCode = Invoke-GitAddWithRetry -Items $existingPaths -IndexLockPath $repositoryInfo.IndexLockPath
if ($gitAddExitCode -ne 0) {
  Write-Error "git add failed with exit code $gitAddExitCode."
  exit $gitAddExitCode
}
