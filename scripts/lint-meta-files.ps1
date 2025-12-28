Param(
  [switch]$VerboseOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info($msg) {
  if ($VerboseOutput) { Write-Host "[lint-meta-files] $msg" -ForegroundColor Cyan }
}

# Directories to scan for meta files
# Note: .github is excluded from scanning entirely because it's GitHub-specific,
# not part of the Unity package distribution
$sourceRoots = @('Runtime', 'Editor', 'Tests', 'Samples~', 'Shaders', 'Styles', 'URP', 'docs', 'scripts')

# Directories to exclude entirely
$excludeDirs = @('node_modules', '.git', 'obj', 'bin', 'Library', 'Temp')

# File patterns to exclude from requiring meta files
$excludeFilePatterns = @(
  '*.meta',                    # Meta files themselves don't need meta files
  'package-lock.json',         # npm lock file doesn't need meta
  'Gemfile.lock',              # Ruby lock file doesn't need meta
  '*.tmp'                      # Temporary files don't need meta
)

# Directory patterns to exclude from requiring meta files
# Folders ending with ~ are ignored by Unity (used for package samples)
$excludeDirPatterns = @(
  'Samples~'                   # Unity sample folder convention (~ suffix ignored by Unity)
)

function Get-RelativePath([string]$path) {
  $root = (Get-Location).Path
  if ($path.StartsWith($root)) {
    $relative = $path.Substring($root.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar)
    # Normalize to forward slashes for consistency
    return $relative -replace '\\', '/'
  }
  return $path -replace '\\', '/'
}

function Test-ShouldExclude([string]$relativePath, [bool]$isDirectory) {
  # Check directory exclusions
  foreach ($dir in $excludeDirs) {
    if ($relativePath -like "*/$dir/*" -or $relativePath -like "$dir/*" -or $relativePath -eq $dir) {
      return $true
    }
  }

  if ($isDirectory) {
    # Check directory pattern exclusions
    foreach ($pattern in $excludeDirPatterns) {
      if ($relativePath -like $pattern -or $relativePath -eq $pattern) {
        return $true
      }
    }
  } else {
    # Check file pattern exclusions
    foreach ($pattern in $excludeFilePatterns) {
      if ($relativePath -like $pattern) {
        return $true
      }
      # Also check just the filename
      $fileName = Split-Path -Leaf $relativePath
      if ($fileName -like $pattern) {
        return $true
      }
    }
  }

  return $false
}

function Get-AllItems {
  param([string]$root)

  $items = @{
    Files = @()
    Directories = @()
  }

  if (-not (Test-Path $root)) {
    return $items
  }

  # Get all files
  $allFiles = Get-ChildItem -Recurse -File -Path $root -ErrorAction SilentlyContinue
  foreach ($file in $allFiles) {
    $rel = Get-RelativePath $file.FullName
    if (-not (Test-ShouldExclude $rel $false)) {
      $items.Files += @{
        FullPath = $file.FullName
        RelativePath = $rel
      }
    }
  }

  # Get all directories
  $allDirs = Get-ChildItem -Recurse -Directory -Path $root -ErrorAction SilentlyContinue
  foreach ($dir in $allDirs) {
    $rel = Get-RelativePath $dir.FullName
    if (-not (Test-ShouldExclude $rel $true)) {
      $items.Directories += @{
        FullPath = $dir.FullName
        RelativePath = $rel
      }
    }
  }

  return $items
}

$missingMeta = @()
$orphanedMeta = @()

Write-Info "Scanning for Unity meta file issues..."

# Collect all items from all source roots
$allFiles = @()
$allDirectories = @()

foreach ($root in $sourceRoots) {
  Write-Info "Scanning root: $root"

  # Also check if the root directory itself needs a meta file
  if (Test-Path $root) {
    $rootRel = $root -replace '\\', '/'
    if (-not (Test-ShouldExclude $rootRel $true)) {
      $allDirectories += @{
        FullPath = (Resolve-Path $root).Path
        RelativePath = $rootRel
      }
    }

    $items = Get-AllItems $root
    $allFiles += $items.Files
    $allDirectories += $items.Directories
  }
}

Write-Info "Found $($allFiles.Count) files and $($allDirectories.Count) directories to check"

# Check for missing meta files (files/directories without .meta)
foreach ($file in $allFiles) {
  # Skip .meta files when checking for missing meta
  if ($file.RelativePath -like '*.meta') {
    continue
  }

  $metaPath = "$($file.FullPath).meta"
  if (-not (Test-Path -LiteralPath $metaPath)) {
    $missingMeta += @{
      Path = $file.RelativePath
      Type = 'File'
    }
  }
}

foreach ($dir in $allDirectories) {
  $metaPath = "$($dir.FullPath).meta"
  if (-not (Test-Path -LiteralPath $metaPath)) {
    $missingMeta += @{
      Path = $dir.RelativePath
      Type = 'Directory'
    }
  }
}

# Check for orphaned meta files (meta exists but source doesn't)
foreach ($file in $allFiles) {
  if ($file.RelativePath -like '*.meta') {
    # This is a meta file - check if the source exists
    $sourcePath = $file.FullPath -replace '\.meta$', ''
    $sourceRel = $file.RelativePath -replace '\.meta$', ''

    # Check if this meta file's source should be excluded
    $sourceIsDir = Test-Path -LiteralPath $sourcePath -PathType Container
    if (Test-ShouldExclude $sourceRel $sourceIsDir) {
      continue
    }

    if (-not (Test-Path -LiteralPath $sourcePath)) {
      $orphanedMeta += @{
        Path = $file.RelativePath
        ExpectedSource = $sourceRel
      }
    }
  }
}

# Output results
$hasErrors = $false

if ($missingMeta.Count -gt 0) {
  $hasErrors = $true
  Write-Host "Missing .meta files:" -ForegroundColor Red
  Write-Host ""
  foreach ($item in $missingMeta | Sort-Object { $_.Path }) {
    $typeLabel = if ($item.Type -eq 'Directory') { '[DIR]' } else { '[FILE]' }
    # Output in format compatible with GitHub Actions annotations
    $ghAnnotation = "::error file=$($item.Path)::UNH005: Missing .meta file for $($item.Type.ToLower()): $($item.Path)"
    Write-Host $ghAnnotation
    Write-Host "  $typeLabel $($item.Path)" -ForegroundColor Yellow
  }
  Write-Host ""
}

if ($orphanedMeta.Count -gt 0) {
  $hasErrors = $true
  Write-Host "Orphaned .meta files (source file/directory missing):" -ForegroundColor Red
  Write-Host ""
  foreach ($item in $orphanedMeta | Sort-Object { $_.Path }) {
    # Output in format compatible with GitHub Actions annotations
    $ghAnnotation = "::error file=$($item.Path)::UNH006: Orphaned .meta file (missing source): $($item.ExpectedSource)"
    Write-Host $ghAnnotation
    Write-Host "  $($item.Path) -> missing: $($item.ExpectedSource)" -ForegroundColor Yellow
  }
  Write-Host ""
}

if ($hasErrors) {
  Write-Host "Unity meta file lint failed:" -ForegroundColor Red
  Write-Host "  Missing meta files: $($missingMeta.Count)" -ForegroundColor Yellow
  Write-Host "  Orphaned meta files: $($orphanedMeta.Count)" -ForegroundColor Yellow
  Write-Host ""
  Write-Host "To fix missing meta files, run: ./scripts/generate-meta.sh <path>" -ForegroundColor Cyan
  Write-Host "To fix orphaned meta files, delete the .meta file or restore the source." -ForegroundColor Cyan
  exit 1
} else {
  Write-Info "No meta file issues found."
  Write-Host "All Unity meta files are valid." -ForegroundColor Green
}
