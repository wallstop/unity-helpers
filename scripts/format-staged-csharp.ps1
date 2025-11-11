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

if (-not $Paths -or $Paths.Count -eq 0) {
  $gitDiffArgs = @('diff', '--cached', '--name-only', '--diff-filter=ACM', '--', '*.cs')
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

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
  Write-Host "dotnet not found; skipping CSharpier formatting."
  exit 0
}

# Restore tools quietly in case the manifest changed.
& dotnet tool restore > $null 2>&1

$dotnetArgs = @('tool', 'run', 'csharpier')
$dotnetArgs += $existingPaths
& dotnet @dotnetArgs
if ($LASTEXITCODE -ne 0) {
  Write-Error "CSharpier formatting failed with exit code $LASTEXITCODE."
  exit $LASTEXITCODE
}

$gitArgs = @('add', '--')
$gitArgs += $existingPaths
& git @gitArgs
if ($LASTEXITCODE -ne 0) {
  Write-Error "git add failed with exit code $LASTEXITCODE."
  exit $LASTEXITCODE
}
