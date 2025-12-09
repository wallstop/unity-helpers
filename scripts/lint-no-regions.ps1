Param(
  [switch]$VerboseOutput,
  [switch]$StagedOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info($msg) {
  if ($VerboseOutput) { Write-Host "[lint-no-regions] $msg" -ForegroundColor Cyan }
}

# Directories to scan
$sourceRoots = @('Runtime', 'Editor', 'Tests')

# Directories to exclude
$excludeDirs = @('node_modules', '.git', 'obj', 'bin', 'Library', 'Temp')

# Pattern to match #region or #endregion directives
$regionPattern = [regex]'(?m)^\s*#\s*(region|endregion)\b'

# Get files to check
function Get-FilesToCheck {
  param([switch]$StagedOnly)

  if ($StagedOnly) {
    # Get staged C# files
    $stagedFiles = & git diff --cached --name-only --diff-filter=ACM -- '*.cs' 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $stagedFiles) {
      return @()
    }
    $files = @()
    foreach ($f in $stagedFiles) {
      if (Test-Path -LiteralPath $f) {
        $files += (Get-Item -LiteralPath $f)
      }
    }
    return $files
  }

  $files = @()
  foreach ($root in $sourceRoots) {
    if (-not (Test-Path $root)) { continue }
    $files += Get-ChildItem -Recurse -Include *.cs -Path $root | Where-Object {
      $excluded = $false
      foreach ($dir in $excludeDirs) {
        if ($_.FullName -like "*\$dir\*" -or $_.FullName -like "*/$dir/*") {
          $excluded = $true
          break
        }
      }
      -not $excluded
    }
  }
  return $files
}

function Get-RelativePath([string]$path) {
  $root = (Get-Location).Path
  if ($path.StartsWith($root)) {
    return ($path.Substring($root.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar))
  }
  return $path
}

$violations = @()

Write-Info "Scanning for #region directives in C# files..."

$files = Get-FilesToCheck -StagedOnly:$StagedOnly

foreach ($file in $files) {
  $filePath = $file.FullName
  $rel = Get-RelativePath $filePath

  # Skip .meta files
  if ($filePath -like '*.meta') { continue }

  $content = Get-Content $filePath -Raw
  if ([string]::IsNullOrWhiteSpace($content)) { continue }

  # Find all #region/#endregion directives
  $matches = $regionPattern.Matches($content)

  foreach ($m in $matches) {
    $directive = $m.Groups[1].Value
    $fullMatch = $m.Value.Trim()

    # Calculate line number
    $prefix = $content.Substring(0, $m.Index)
    $lineNo = ($prefix -split "`n").Length

    $violations += @{
      Path      = $rel
      Line      = $lineNo
      Directive = $fullMatch
      Message   = "UNH005: #$directive directive found. Remove #region/#endregion directives from code."
    }
  }
}

if ($violations.Count -gt 0) {
  Write-Host "#region lint failed:" -ForegroundColor Red
  Write-Host ""
  foreach ($v in $violations) {
    # Output in format compatible with GitHub Actions annotations
    $ghAnnotation = "::error file=$($v.Path),line=$($v.Line)::$($v.Message)"
    Write-Host $ghAnnotation
    Write-Host ("{0}:{1}: {2}" -f $v.Path, $v.Line, $v.Message) -ForegroundColor Yellow
  }
  Write-Host ""
  Write-Host "Found $($violations.Count) #region directive(s)." -ForegroundColor Red
  Write-Host "#region directives are not allowed. Use proper code organization instead." -ForegroundColor Yellow
  exit 1
} else {
  Write-Info "No #region directives found."
  if (-not $StagedOnly) {
    Write-Host "No #region directives found in C# files." -ForegroundColor Green
  }
}
