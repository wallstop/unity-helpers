Param(
  [switch]$VerboseOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info($msg) {
  if ($VerboseOutput) { Write-Host "[lint-tests] $msg" -ForegroundColor Cyan }
}

# Heuristics and allowlists
$testRoots = @('Tests')
$allowedHelperFiles = @(
  'Tests/Runtime/Visuals/VisualsTestHelpers.cs'
)

$destroyPattern = [regex]'(?<!UNH-SUPPRESS).*\b(?:UnityEngine\.)?Object\.(?:DestroyImmediate|Destroy)\s*\((?<arg>[^)]*)\)'
$createAssignObjectPattern = [regex]'(?<var>\b\w+)\s*=\s*new\s+(?<type>GameObject|Texture2D|Material|Mesh|Camera)\s*\('
$createInlineTrackPattern = [regex]'\bTrack\s*\(\s*new\s+(?:GameObject|Texture2D|Material|Mesh|Camera)\s*\('
$createSoAssignPattern = [regex]'(?<var>\b\w+)\s*=\s*ScriptableObject\.CreateInstance\s*<'

# Returns true if line contains an allowlisted helper file path
function Is-AllowlistedFile([string]$relPath) {
  foreach ($a in $allowedHelperFiles) {
    if ($relPath -replace '\\','/' -ieq $a) { return $true }
  }
  return $false
}

function Get-RelativePath([string]$path) {
  $root = (Get-Location).Path
  return ($path.Substring($root.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar))
}

$violations = @()

foreach ($root in $testRoots) {
  if (-not (Test-Path $root)) { continue }
  Get-ChildItem -Recurse -Include *.cs -Path $root | ForEach-Object {
    $file = $_.FullName
    $rel = Get-RelativePath $file
    $content = Get-Content $file
    $text = $content -join "`n"

    if (Is-AllowlistedFile $rel) {
      return
    }

    # Skip meta or non-source
    if ($file -like '*.meta') { return }

    # Check destroy calls; allow if argument var was tracked earlier in file
    $lineIndex = 0
    foreach ($line in $content) {
      $lineIndex++
      if ($destroyPattern.IsMatch($line)) {
        $m = $destroyPattern.Match($line)
        $arg = ($m.Groups['arg'].Value).Trim()
        # Extract variable token before any commas or closing paren
        $varName = $arg -replace ',.*','' -replace '\)',''
        $allowed = $false
        if (-not [string]::IsNullOrWhiteSpace($varName)) {
          # Search up to 100 lines above for Track(varName)
          $searchStart = [Math]::Max(0, $lineIndex - 100)
          for ($i = $searchStart; $i -lt $lineIndex; $i++) {
            if ($content[$i] -match "Track\s*\(\s*$varName\b") { $allowed = $true; break }
          }
        }
        if (-not $allowed) {
          $violations += (@{
            Path=$rel; Line=$lineIndex; Message="UNH001: Avoid direct destroy in tests; track object and let teardown clean up (or add // UNH-SUPPRESS)"
          })
        }
      }
    }

    # Check untracked new allocations via assignment (var = new Type(...))
    $assignMatches = $createAssignObjectPattern.Matches($text)
    foreach ($am in $assignMatches) {
      $var = $am.Groups['var'].Value
      if ([string]::IsNullOrWhiteSpace($var)) { continue }
      # Find the index of this match in terms of line
      $prefix = $text.Substring(0, $am.Index)
      $lineNo = ($prefix -split "`n").Length
      # Look ahead 10 lines for Track(var)
      $endLine = [Math]::Min($content.Count, $lineNo + 10)
      $found = $false
      for ($j = $lineNo; $j -le $endLine; $j++) {
        if ($content[$j-1] -match "Track\s*\(\s*$var\b") { $found = $true; break }
      }
      if (-not $found) {
        $violations += (@{
          Path=$rel; Line=$lineNo; Message="UNH002: Unity object allocation should be tracked: add Track($var)"
        })
      }
    }

    # Check inline Track(new ...) OK; but find bare inline new ... in args without Track
    if ($text -match '\bnew\s+(GameObject|Texture2D|Material|Mesh|Camera)\s*\(') {
      # If Track(new ...) not present at all, flag a generic warning at file level
      if (-not $createInlineTrackPattern.IsMatch($text)) {
        # locate first occurrence for line number
        $m = [regex]::Match($text, '\bnew\s+(GameObject|Texture2D|Material|Mesh|Camera)\s*\(')
        $lineNo = (($text.Substring(0, $m.Index)) -split "`n").Length
        $violations += (@{
          Path=$rel; Line=$lineNo; Message="UNH002: Inline Unity object creation should be passed to Track(new …)"
        })
      }
    }

    # Check ScriptableObject.CreateInstance<T>() assigned, ensure tracked
    $soMatches = $createSoAssignPattern.Matches($text)
    foreach ($sm in $soMatches) {
      $var = $sm.Groups['var'].Value
      if ([string]::IsNullOrWhiteSpace($var)) { continue }
      $prefix = $text.Substring(0, $sm.Index)
      $lineNo = ($prefix -split "`n").Length
      $found = $false
      $endLine = [Math]::Min($content.Count, $lineNo + 10)
      for ($j = $lineNo; $j -le $endLine; $j++) {
        if ($content[$j-1] -match "Track\s*\(\s*$var\b") { $found = $true; break }
      }
      if (-not $found) {
        $violations += (@{
          Path=$rel; Line=$lineNo; Message="UNH002: ScriptableObject instance should be tracked: add Track($var)"
        })
      }
    }

    # Enforce CommonTestBase inheritance only if file creates Unity objects and is under Runtime/ or Editor/
    $createsUnity = ($assignMatches.Count -gt 0) -or ($text -match '\bnew\s+(GameObject|Texture2D|Material|Mesh|Camera)\s*\(') -or ($soMatches.Count -gt 0)
    if ($createsUnity) {
      $usesBase = ($text -match ':\s*CommonTestBase')
      if (-not $usesBase) {
        # Only enforce for test classes; skip helper-only files
        if ($text -match '\bnamespace\s+WallstopStudios') {
          $violations += (@{
            Path=$rel; Line=1; Message="UNH003: Test classes creating Unity objects should inherit CommonTestBase (Editor or Runtime variant)"
          })
        }
      }
    }
  }
}

if ($violations.Count -gt 0) {
  Write-Host "Test lifecycle lint failed:" -ForegroundColor Red
  foreach ($v in $violations) {
    Write-Host ("{0}:{1}: {2}" -f $v.Path, $v.Line, $v.Message) -ForegroundColor Yellow
  }
  exit 1
} else {
  Write-Info "No issues found in test code."
}

