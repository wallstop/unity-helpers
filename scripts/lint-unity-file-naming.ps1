Param(
  [switch]$VerboseOutput,
  [switch]$StagedOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info($msg) {
  if ($VerboseOutput) { Write-Host "[lint-unity-file-naming] $msg" -ForegroundColor Cyan }
}

# Directories to scan
$sourceRoots = @('Runtime', 'Editor', 'Tests', 'Samples~')

# Directories to exclude
$excludeDirs = @('node_modules', '.git', 'obj', 'bin', 'Library', 'Temp')

# Unity base types that require one-per-file naming
# Classes inheriting from these (directly or indirectly via common patterns) must be in their own file
$unityObjectBaseTypes = @(
  'MonoBehaviour',
  'ScriptableObject',
  'ScriptableObjectSingleton',
  'Editor',
  'EditorWindow',
  'PropertyDrawer',
  'DecoratorDrawer',
  'AssetPostprocessor',
  'AssetModificationProcessor',
  'StateMachineBehaviour',
  'NetworkBehaviour'
)

# Build regex pattern for inheritance detection
# Matches: class ClassName : BaseType or class ClassName<T> : BaseType<T>
$baseTypePattern = ($unityObjectBaseTypes | ForEach-Object { [regex]::Escape($_) }) -join '|'

# Pattern to find non-nested class declarations that inherit from Unity types
# This pattern:
# - Matches public/internal/sealed/abstract class declarations
# - Captures the class name
# - Looks for inheritance from Unity base types
# - Excludes nested classes by requiring the class to not be indented significantly
$classPattern = [regex]"(?m)^(?<indent>\s*)(?:(?<modifiers>(?:public|internal|private|protected|sealed|abstract|partial)\s+)*)class\s+(?<name>\w+)(?:\s*<[^>]+>)?\s*:\s*(?<base>[^\r\n{]+)"

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

function Test-InheritsFromUnityObject([string]$baseClause) {
  # Check if the base clause contains any Unity base type
  foreach ($baseType in $unityObjectBaseTypes) {
    # Match the base type as a whole word, accounting for generics
    if ($baseClause -match "\b$baseType\b") {
      return $true
    }
  }
  return $false
}

function Get-ExpectedFileName([string]$className) {
  return "$className.cs"
}

$violations = @()

Write-Info "Scanning for Unity Object file naming violations..."

$files = Get-FilesToCheck -StagedOnly:$StagedOnly

foreach ($file in $files) {
  $filePath = $file.FullName
  $rel = Get-RelativePath $filePath
  $fileName = $file.Name

  # Skip .meta files
  if ($filePath -like '*.meta') { continue }

  $content = Get-Content $filePath -Raw
  if ([string]::IsNullOrWhiteSpace($content)) { continue }

  # Check for lint disable comment
  if ($content -match '//\s*lint-disable\s+unity-file-naming') {
    Write-Info "Skipping file with lint-disable comment: $rel"
    continue
  }

  # Find all class declarations that inherit from Unity types
  $matches = $classPattern.Matches($content)

  # Track top-level Unity classes found in this file
  $topLevelUnityClasses = @()

  foreach ($m in $matches) {
    $indent = $m.Groups['indent'].Value
    $className = $m.Groups['name'].Value
    $baseClauses = $m.Groups['base'].Value

    # Skip nested classes (indented more than 4 spaces or 1 tab typically indicates nesting)
    # A top-level class in a namespace is usually indented 0-4 spaces
    $indentLength = $indent.Replace("`t", "    ").Length
    if ($indentLength -gt 4) {
      Write-Info "Skipping nested class: $className in $rel (indent: $indentLength)"
      continue
    }

    # Check if this class inherits from a Unity Object type
    if (-not (Test-InheritsFromUnityObject $baseClauses)) {
      continue
    }

    Write-Info "Found Unity class: $className in $rel (base: $baseClauses)"

    $topLevelUnityClasses += @{
      Name    = $className
      Index   = $m.Index
      Base    = $baseClauses.Trim()
    }
  }

  # Validate findings
  if ($topLevelUnityClasses.Count -eq 0) {
    # No Unity classes found, skip
    continue
  }

  if ($topLevelUnityClasses.Count -gt 1) {
    # Multiple top-level Unity classes in one file - violation
    $classNames = ($topLevelUnityClasses | ForEach-Object { $_.Name }) -join ', '

    # Calculate line number for first class
    $prefix = $content.Substring(0, $topLevelUnityClasses[0].Index)
    $lineNo = ($prefix -split "`n").Length

    $violations += @{
      Path    = $rel
      Line    = $lineNo
      Classes = $classNames
      Message = "UNH005: File contains multiple Unity Object types: $classNames. Each MonoBehaviour/ScriptableObject must be in its own file."
    }
    continue
  }

  # Single Unity class - check file naming
  $unityClass = $topLevelUnityClasses[0]
  $expectedFileName = Get-ExpectedFileName $unityClass.Name

  if ($fileName -cne $expectedFileName) {
    # File name doesn't match class name - violation
    $prefix = $content.Substring(0, $unityClass.Index)
    $lineNo = ($prefix -split "`n").Length

    $violations += @{
      Path     = $rel
      Line     = $lineNo
      Class    = $unityClass.Name
      FileName = $fileName
      Expected = $expectedFileName
      Message  = "UNH006: Unity Object '$($unityClass.Name)' must be in a file named '$expectedFileName', but found in '$fileName'."
    }
  }
}

if ($violations.Count -gt 0) {
  Write-Host "Unity file naming convention lint failed:" -ForegroundColor Red
  Write-Host ""
  foreach ($v in $violations) {
    # Output in format compatible with GitHub Actions annotations
    $ghAnnotation = "::error file=$($v.Path),line=$($v.Line)::$($v.Message)"
    Write-Host $ghAnnotation
    Write-Host ("{0}:{1}: {2}" -f $v.Path, $v.Line, $v.Message) -ForegroundColor Yellow
  }
  Write-Host ""
  Write-Host "Found $($violations.Count) Unity file naming violation(s)." -ForegroundColor Red
  Write-Host ""
  Write-Host "Unity requires:" -ForegroundColor Yellow
  Write-Host "  1. Each MonoBehaviour/ScriptableObject must be in its own dedicated file" -ForegroundColor Yellow
  Write-Host "  2. The file name must exactly match the class name (case-sensitive)" -ForegroundColor Yellow
  Write-Host "  3. Example: 'PlayerController.cs' for 'public class PlayerController : MonoBehaviour'" -ForegroundColor Yellow
  exit 1
} else {
  Write-Info "No Unity file naming violations found."
  if (-not $StagedOnly) {
    Write-Host "All Unity Object types follow file naming conventions." -ForegroundColor Green
  }
}
