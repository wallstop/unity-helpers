Param(
  [switch]$VerboseOutput
)

<#
.SYNOPSIS
    Validates that .gitignore patterns do not accidentally exclude documentation or LLM files.

.DESCRIPTION
    This linter checks for three categories of .gitignore safety issues:

    1. No files under docs/ or .llm/ are gitignored - verifies that all tracked and untracked
       documentation and LLM configuration files are not excluded by .gitignore patterns.
    2. Wildcard pattern safety - checks that .gitignore patterns with wildcards do not
       accidentally match files in the docs/ or .llm/ directories.
    3. mkdocs.yml nav integrity - verifies that every file referenced in the mkdocs.yml
       nav section exists on disk and is not gitignored.

    These checks prevent the category of CI failure where a .gitignore pattern like
    "failed-tests-*" accidentally matches "docs/features/editor-tools/failed-tests-exporter.md".

.PARAMETER VerboseOutput
    Show detailed output during validation

.EXAMPLE
    ./scripts/lint-gitignore-docs.ps1
    ./scripts/lint-gitignore-docs.ps1 -VerboseOutput
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info($msg) {
  if ($VerboseOutput) { Write-Host "[lint-gitignore-docs] $msg" -ForegroundColor Cyan }
}

$hasErrors = $false
$errorCount = 0

# ---- Check 1: No docs/ or .llm/ files are gitignored ----
Write-Info "Check 1: Verifying no docs/ or .llm/ files are gitignored..."

$protectedDirs = @('docs', '.llm')
foreach ($protectedDir in $protectedDirs) {
  if (Test-Path $protectedDir) {
    $protectedFiles = Get-ChildItem -Recurse -File -Path $protectedDir -ErrorAction SilentlyContinue
    $ignoredFiles = @()

    foreach ($file in $protectedFiles) {
      $relativePath = $file.FullName
      $root = (Get-Location).Path
      if ($relativePath.StartsWith($root)) {
        $relativePath = $relativePath.Substring($root.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar)
      }
      $relativePath = $relativePath -replace '\\', '/'

      # Use git check-ignore to test if the file would be ignored
      $checkResult = & git check-ignore -q $relativePath 2>&1
      if ($LASTEXITCODE -eq 0) {
        $ignoredFiles += $relativePath
      }
    }

    if ($ignoredFiles.Count -gt 0) {
      $hasErrors = $true
      Write-Host "" -ForegroundColor Red
      Write-Host "ERROR: Files in $protectedDir/ are gitignored:" -ForegroundColor Red
      foreach ($ignored in $ignoredFiles) {
        $errorCount++
        Write-Host "::error file=$ignored::UNH-GITIGNORE: Protected file is gitignored: $ignored"
        Write-Host "  $ignored" -ForegroundColor Yellow
      }
      Write-Host ""
      Write-Host "Fix: Narrow the .gitignore pattern to avoid matching $protectedDir/ files." -ForegroundColor Cyan
      Write-Host "Example: Change 'failed-tests-*' to 'failed-tests-*.txt'" -ForegroundColor Cyan
      Write-Host ""
    } else {
      Write-Info "  No $protectedDir/ files are gitignored."
    }
  } else {
    Write-Info "  No $protectedDir/ directory found, skipping."
  }
}

# ---- Check 2: Wildcard pattern safety ----
Write-Info "Check 2: Checking .gitignore wildcard patterns against docs/ and .llm/ files..."

# Collect file names and relative paths from all protected directories
$allProtectedFileNames = @()
$allProtectedRelPaths = @()
$hasProtectedDirs = $false

foreach ($protectedDir in $protectedDirs) {
  if (Test-Path $protectedDir) {
    $hasProtectedDirs = $true
    $files = Get-ChildItem -Recurse -File -Path $protectedDir -ErrorAction SilentlyContinue
    foreach ($file in $files) {
      $allProtectedFileNames += $file.Name
      $relativePath = $file.FullName
      $root = (Get-Location).Path
      if ($relativePath.StartsWith($root)) {
        $relativePath = $relativePath.Substring($root.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar)
      }
      $allProtectedRelPaths += ($relativePath -replace '\\', '/')
    }
  }
}

if ((Test-Path '.gitignore') -and $hasProtectedDirs) {
  $gitignoreLines = Get-Content '.gitignore' -Encoding UTF8

  $lineNumber = 0
  $dangerousPatterns = @()
  foreach ($line in $gitignoreLines) {
    $lineNumber++
    $trimmed = $line.Trim()

    # Skip comments and empty lines
    if ($trimmed -eq '' -or $trimmed.StartsWith('#')) { continue }
    # Skip negation patterns (they un-ignore files)
    if ($trimmed.StartsWith('!')) { continue }

    # Only check patterns with wildcards that could match broadly
    if ($trimmed -notmatch '\*') { continue }

    # Skip patterns that are directory-scoped (contain / prefix or /) and clearly not matching docs/ or .llm/
    # e.g., /Unity/*, node_modules/, etc.
    # But DO check patterns without directory scope since they match everywhere
    $isDirectoryScoped = $trimmed.StartsWith('/') -or ($trimmed -match '^[^*]*/' -and -not $trimmed.StartsWith('*'))

    if ($isDirectoryScoped) {
      # Only check directory-scoped patterns if they could match under docs/ or .llm/
      if (-not ($trimmed -match '^docs/' -or $trimmed -match '^\.llm/' -or $trimmed -match '^\*\*/' -or $trimmed.StartsWith('*'))) {
        continue
      }
    }

    # Convert gitignore glob to a simple regex for matching against filenames
    # This is a simplified check - we test if any protected filename matches
    $regexPattern = $trimmed
    # Remove leading / (anchored patterns)
    $regexPattern = $regexPattern -replace '^\/', ''
    # Remove trailing / (directory-only patterns)
    $regexPattern = $regexPattern.TrimEnd('/')
    # Escape regex special chars except * and ?
    $regexPattern = $regexPattern -replace '\.', '\.'
    $regexPattern = $regexPattern -replace '\+', '\+'
    $regexPattern = $regexPattern -replace '\[', '\['
    $regexPattern = $regexPattern -replace '\]', '\]'
    $regexPattern = $regexPattern -replace '\(', '\('
    $regexPattern = $regexPattern -replace '\)', '\)'
    # Convert gitignore globs to regex
    $regexPattern = $regexPattern -replace '\*\*/', '(.+/)?'
    $regexPattern = $regexPattern -replace '\*\*', '.*'
    $regexPattern = $regexPattern -replace '(?<!\.)(\*)', '[^/]*'
    $regexPattern = $regexPattern -replace '\?', '[^/]'

    # Test against protected file basenames for non-path patterns
    foreach ($fileName in $allProtectedFileNames) {
      try {
        if ($fileName -match "^$regexPattern$") {
          $dangerousPatterns += @{
            Line = $lineNumber
            Pattern = $trimmed
            MatchedFile = $fileName
          }
        }
      } catch {
        # Regex parsing failed, skip this pattern
        Write-Info "  Skipping pattern '$trimmed' (regex parse error)"
      }
    }
  }

  if ($dangerousPatterns.Count -gt 0) {
    # Only report as errors if the files are actually gitignored (check 1 caught them).
    # Otherwise report as warnings since the pattern *could* match but git's path
    # scoping may protect the files.
    foreach ($dp in $dangerousPatterns) {
      # Verify with git check-ignore whether any matching protected file is actually ignored
      $actuallyIgnored = $false
      foreach ($protectedPath in $allProtectedRelPaths) {
        $protectedFileName = Split-Path -Leaf $protectedPath
        if ($protectedFileName -eq $dp.MatchedFile) {
          $checkResult = & git check-ignore -q $protectedPath 2>&1
          if ($LASTEXITCODE -eq 0) {
            $actuallyIgnored = $true
            break
          }
        }
      }

      if ($actuallyIgnored) {
        $hasErrors = $true
        $errorCount++
        Write-Host "::error file=.gitignore,line=$($dp.Line)::UNH-GITIGNORE: Pattern '$($dp.Pattern)' ignores protected file: $($dp.MatchedFile)"
        Write-Host "  [ERROR] .gitignore:$($dp.Line): Pattern '$($dp.Pattern)' matches protected file '$($dp.MatchedFile)'" -ForegroundColor Red
      } else {
        Write-Info "  [OK] .gitignore:$($dp.Line): Pattern '$($dp.Pattern)' matches name '$($dp.MatchedFile)' but file is not actually ignored (path-scoped)"
      }
    }
  } else {
    Write-Info "  No wildcard patterns match docs/ or .llm/ filenames."
  }
} else {
  Write-Info "  Skipping wildcard check (.gitignore or protected directories not found)."
}

# ---- Check 3: mkdocs.yml nav file existence and gitignore status ----
Write-Info "Check 3: Validating mkdocs.yml nav references..."

if (Test-Path 'mkdocs.yml') {
  $mkdocsContent = Get-Content 'mkdocs.yml' -Encoding UTF8
  $navFiles = @()

  # Extract file references from nav section
  # Nav entries look like: "  - Title: path/to/file.md"
  $inNav = $false
  foreach ($line in $mkdocsContent) {
    if ($line -match '^\s*nav\s*:') {
      $inNav = $true
      continue
    }

    # Nav section ends when we hit a non-indented, non-empty line that starts a new top-level key
    if ($inNav -and $line -match '^\S' -and $line -notmatch '^\s*$' -and $line -notmatch '^\s*#') {
      $inNav = $false
      continue
    }

    if ($inNav -and $line -match ':\s+(\S+\.md)\s*$') {
      $navFiles += $Matches[1]
    }
  }

  Write-Info "  Found $($navFiles.Count) nav file references in mkdocs.yml."

  $missingFiles = @()
  $ignoredNavFiles = @()

  foreach ($navFile in $navFiles) {
    $fullPath = Join-Path 'docs' $navFile

    # Check if file exists
    if (-not (Test-Path $fullPath)) {
      $missingFiles += @{
        NavPath = $navFile
        FullPath = $fullPath
      }
    } else {
      # Check if file is gitignored
      $relativePath = $fullPath -replace '\\', '/'
      $checkResult = & git check-ignore -q $relativePath 2>&1
      if ($LASTEXITCODE -eq 0) {
        $ignoredNavFiles += @{
          NavPath = $navFile
          FullPath = $relativePath
        }
      }
    }
  }

  if ($missingFiles.Count -gt 0) {
    $hasErrors = $true
    Write-Host ""
    Write-Host "ERROR: mkdocs.yml nav references missing files:" -ForegroundColor Red
    foreach ($mf in $missingFiles) {
      $errorCount++
      Write-Host "::error file=mkdocs.yml::UNH-GITIGNORE: Nav references missing file: $($mf.FullPath)"
      Write-Host "  $($mf.NavPath) -> $($mf.FullPath) (NOT FOUND)" -ForegroundColor Yellow
    }
    Write-Host ""
  }

  if ($ignoredNavFiles.Count -gt 0) {
    $hasErrors = $true
    Write-Host ""
    Write-Host "ERROR: mkdocs.yml nav references gitignored files:" -ForegroundColor Red
    foreach ($inf in $ignoredNavFiles) {
      $errorCount++
      Write-Host "::error file=mkdocs.yml::UNH-GITIGNORE: Nav references gitignored file: $($inf.FullPath)"
      Write-Host "  $($inf.NavPath) -> $($inf.FullPath) (GITIGNORED)" -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Host "Fix: Update .gitignore to not exclude these documentation files." -ForegroundColor Cyan
    Write-Host ""
  }

  if ($missingFiles.Count -eq 0 -and $ignoredNavFiles.Count -eq 0) {
    Write-Info "  All mkdocs.yml nav files exist and are not gitignored."
  }
} else {
  Write-Info "  No mkdocs.yml found, skipping nav validation."
}

# ---- Summary ----
if ($hasErrors) {
  Write-Host ""
  Write-Host "Gitignore docs safety check failed with $errorCount error(s)." -ForegroundColor Red
  Write-Host ""
  Write-Host "Common fixes:" -ForegroundColor Cyan
  Write-Host "  - Narrow wildcard patterns: 'failed-tests-*' -> 'failed-tests-*.txt'" -ForegroundColor Cyan
  Write-Host "  - Add negation patterns: '!docs/**' or '!.llm/**'" -ForegroundColor Cyan
  Write-Host "  - Use path-anchored patterns: '/failed-tests-*' (root only)" -ForegroundColor Cyan
  exit 1
} else {
  Write-Info "All gitignore docs/llm safety checks passed."
  Write-Host "Gitignore docs/llm safety: OK" -ForegroundColor Green
}
