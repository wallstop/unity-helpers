#!/usr/bin/env pwsh
param(
  [Parameter(ValueFromRemainingArguments = $true)]
  [string[]]$Paths
)

$git = Get-Command git -ErrorAction SilentlyContinue
if (-not $git) {
  Write-Error "git not found; cannot inspect or stage files."
  exit 1
}

$markdownGlobs = @('*.md', '*.markdown')
if (-not $Paths -or $Paths.Count -eq 0) {
  $gitDiffArgs = @('diff', '--cached', '--name-only', '--diff-filter=ACM', '--')
  $gitDiffArgs += $markdownGlobs
  $Paths = & git @gitDiffArgs
  if ($LASTEXITCODE -ne 0 -or -not $Paths) {
    exit 0
  }
}

$existingPaths = @()
foreach ($path in $Paths) {
  if ([string]::IsNullOrWhiteSpace($path)) {
    continue
  }

  if (Test-Path -LiteralPath $path) {
    $existingPaths += $path
  }
}

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
  'markdownlint',
  '--config',
  '.markdownlint.json',
  '--ignore-path',
  '.markdownlintignore',
  '--fix'
)
$npxArgs += $existingPaths
& npx @npxArgs
if ($LASTEXITCODE -ne 0) {
  Write-Error "markdownlint failed with exit code $LASTEXITCODE."
  exit $LASTEXITCODE
}

$gitArgs = @('add', '--')
$gitArgs += $existingPaths
& git @gitArgs
if ($LASTEXITCODE -ne 0) {
  Write-Error "git add failed with exit code $LASTEXITCODE."
  exit $LASTEXITCODE
}
