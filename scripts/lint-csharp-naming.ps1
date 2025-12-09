Param(
  [switch]$VerboseOutput,
  [switch]$StagedOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info($msg) {
  if ($VerboseOutput) { Write-Host "[lint-csharp-naming] $msg" -ForegroundColor Cyan }
}

# Directories to scan
$sourceRoots = @('Runtime', 'Editor', 'Tests')

# Directories to exclude
$excludeDirs = @('node_modules', '.git', 'obj', 'bin', 'Library', 'Temp')

# Pattern to match C# method declarations with underscores in name
# This pattern requires:
# - Line start (after optional whitespace)
# - Optional access modifier (public/private/protected/internal)
# - Optional modifiers (static/virtual/override/abstract/sealed/async/new/extern/partial/unsafe)
# - Return type (must be a valid identifier, not starting with underscore)
# - Method name (captured)
# - Opening parenthesis for parameters
# The key improvement: return type must start with uppercase letter (valid C# type)
# or be a keyword like void, int, bool, etc.
$methodPattern = [regex]'(?m)^\s*(?:(?:\[[\w\s,\(\)\"=\.]+\]\s*)*)(?:(?<access>public|private|protected|internal)\s+)?(?:(?<modifiers>(?:(?:static|virtual|override|abstract|sealed|async|new|extern|partial|unsafe|readonly)\s+)*))(?<return>(?:void|bool|byte|sbyte|char|decimal|double|float|int|uint|long|ulong|short|ushort|string|object|dynamic|var|(?:[A-Z]\w*(?:\s*<[^>]+>)?(?:\s*\[\s*,?\s*\])*(?:\s*\?)?)))(?:\s+)(?<name>[A-Z]\w*)\s*(?:<[^>]+>)?\s*\('

# Pattern specifically for underscore in method name
$underscoreInNamePattern = [regex]'_'

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

Write-Info "Scanning for C# method names with underscores..."

$files = Get-FilesToCheck -StagedOnly:$StagedOnly

foreach ($file in $files) {
  $filePath = $file.FullName
  $rel = Get-RelativePath $filePath

  # Skip .meta files
  if ($filePath -like '*.meta') { continue }

  # Allow underscores in test files (common naming convention: Method_Scenario_Expected)
  if ($rel -match '[\\/]Tests[\\/]' -or $rel -match '^Tests[\\/]') {
    Write-Info "Skipping test file: $rel"
    continue
  }

  $content = Get-Content $filePath -Raw
  if ([string]::IsNullOrWhiteSpace($content)) { continue }

  # Find all method declarations
  $matches = $methodPattern.Matches($content)

  foreach ($m in $matches) {
    $methodName = $m.Groups['name'].Value

    # Skip if method name doesn't contain underscore
    if (-not $underscoreInNamePattern.IsMatch($methodName)) { continue }

    # Skip operator overloads (op_Addition, op_Equality, etc.)
    if ($methodName -match '^op_') { continue }

    # Calculate line number
    $prefix = $content.Substring(0, $m.Index)
    $lineNo = ($prefix -split "`n").Length

    $violations += @{
      Path    = $rel
      Line    = $lineNo
      Method  = $methodName
      Message = "UNH004: Method name '$methodName' contains underscore(s). Use PascalCase without underscores."
    }
  }
}

if ($violations.Count -gt 0) {
  Write-Host "C# naming convention lint failed:" -ForegroundColor Red
  Write-Host ""
  foreach ($v in $violations) {
    # Output in format compatible with GitHub Actions annotations
    $ghAnnotation = "::error file=$($v.Path),line=$($v.Line)::$($v.Message)"
    Write-Host $ghAnnotation
    Write-Host ("{0}:{1}: {2}" -f $v.Path, $v.Line, $v.Message) -ForegroundColor Yellow
  }
  Write-Host ""
  Write-Host "Found $($violations.Count) method(s) with underscores in name." -ForegroundColor Red
  Write-Host "Method names should use PascalCase without underscores (e.g., 'DoSomething' not 'Do_Something')." -ForegroundColor Yellow
  exit 1
} else {
  Write-Info "No naming convention violations found."
  if (-not $StagedOnly) {
    Write-Host "All C# method names follow naming conventions." -ForegroundColor Green
  }
}
