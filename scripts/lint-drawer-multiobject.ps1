Param(
  [switch]$VerboseOutput,
  [string[]]$Paths
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info($msg) {
  if ($VerboseOutput) { Write-Host "[lint-drawer-multiobject] $msg" -ForegroundColor Cyan }
}

# Patterns to detect potential multi-object editing issues

# Property write patterns - assignments to serialized property values
$propertyWritePattern = [regex]'property\.(intValue|stringValue|floatValue|boolValue|enumValueIndex|objectReferenceValue)\s*='

# Property read patterns - accessing serialized property values in index calculations
# This is the risky pattern: using property values to calculate indices without checking hasMultipleDifferentValues
$propertyReadForIndexPattern = [regex]'(Array\.IndexOf|IndexOf|\.Index\(|selectedIndex\s*=|currentIndex\s*=|index\s*=).*property\.(intValue|stringValue|floatValue|boolValue|enumValueIndex|objectReferenceValue)\b'

# Index clamping patterns - suspicious index assignments
$indexClampPattern = [regex]'(selectedIndex|currentIndex|index)\s*=\s*(0|Math\.Max|Mathf\.Max|Mathf\.Clamp)'

# Mixed value check pattern
$mixedValueCheckPattern = [regex]'hasMultipleDifferentValues'

# Callback/lambda context patterns - writes inside these are OK
$callbackStartPatterns = @(
  [regex]'=>',
  [regex]'delegate\s*\(',
  [regex]'AddItem\s*\(',
  [regex]'RegisterValueChangedCallback',
  [regex]'onClick\s*\+?=',
  [regex]'menu\.AddItem'
)

# Render method detection - OnGUI and similar draw methods
$renderMethodPatterns = @(
  [regex]'^\s*(public|private|protected|internal)?\s*(static)?\s*(override)?\s*void\s+OnGUI\s*\(',
  [regex]'^\s*(public|private|protected|internal)?\s*(static)?\s*void\s+Draw\w*\s*\(',
  [regex]'^\s*(public|private|protected|internal)?\s*(static)?\s*void\s+Render\w*\s*\('
)

# Methods that are explicitly for applying changes (not render methods)
$applyMethodPatterns = @(
  [regex]'^\s*(protected|public|private|internal)?\s*(override)?\s*void\s+Apply\w+\s*\(',
  [regex]'^\s*(protected|public|private|internal)?\s*(override)?\s*void\s+Set\w+\s*\(',
  [regex]'^\s*(protected|public|private|internal)?\s*(override)?\s*void\s+Update\w+\s*\('
)

# Suppression comment pattern
$suppressPattern = [regex]'UNH-SUPPRESS'

$warnings = @()

function Get-RelativePath([string]$path) {
  $root = (Get-Location).Path
  return ($path.Substring($root.Length).TrimStart([System.IO.Path]::DirectorySeparatorChar))
}

function Is-InsideCallback([string]$line) {
  foreach ($pattern in $callbackStartPatterns) {
    if ($pattern.IsMatch($line)) { return $true }
  }
  return $false
}

function Has-MixedValueCheck([string[]]$content, [int]$lineIndex, [int]$searchRange) {
  # Search backwards and forwards for hasMultipleDifferentValues check
  $startLine = [Math]::Max(0, $lineIndex - $searchRange)
  $endLine = [Math]::Min($content.Count - 1, $lineIndex + $searchRange)

  for ($i = $startLine; $i -le $endLine; $i++) {
    if ($mixedValueCheckPattern.IsMatch($content[$i])) {
      return $true
    }
  }
  return $false
}

function Get-ContainingMethodName([string[]]$content, [int]$lineIndex) {
  # Search backwards for method signature
  $braceCount = 0
  for ($i = $lineIndex - 1; $i -ge 0; $i--) {
    $line = $content[$i]
    # Count braces to track scope
    $openBraces = ([regex]::Matches($line, '\{')).Count
    $closeBraces = ([regex]::Matches($line, '\}')).Count
    $braceCount += $closeBraces
    $braceCount -= $openBraces

    # If we've exited the current method scope, stop
    if ($braceCount -gt 0) {
      return $null
    }

    # Check for method signature
    if ($line -match '^\s*(public|private|protected|internal)?\s*(static)?\s*(override)?\s*(void|VisualElement|[\w<>,\s]+)\s+(\w+)\s*\(') {
      return $Matches[5]
    }
  }
  return $null
}

function Is-InsideRenderMethod([string[]]$content, [int]$lineIndex) {
  $methodName = Get-ContainingMethodName $content $lineIndex
  if ($null -eq $methodName) { return $false }

  # Check if method name suggests render context
  $renderMethodNames = @('OnGUI', 'DrawPropertyLayout', 'DrawGenericMenuDropDown', 'DrawPopupDropDown')
  foreach ($name in $renderMethodNames) {
    if ($methodName -eq $name) { return $true }
  }

  # Method names starting with Draw suggest render context
  if ($methodName -match '^Draw') { return $true }

  return $false
}

function Is-InsideApplyMethod([string[]]$content, [int]$lineIndex) {
  $methodName = Get-ContainingMethodName $content $lineIndex
  if ($null -eq $methodName) { return $false }

  # Methods for applying changes - these are OK to write properties
  $applyMethodNames = @('ApplySelectionToProperty', 'ApplySelection', 'SetPropertyValue', 'UpdateProperty', 'WriteValue', 'WritePropertyValue')
  foreach ($name in $applyMethodNames) {
    if ($methodName -eq $name) { return $true }
  }

  # Method names starting with Apply or Set suggest apply context
  if ($methodName -match '^(Apply|Set|Update|Write)') { return $true }

  return $false
}

Write-Host "Checking multi-object editing patterns..." -ForegroundColor White

$filesToScan = @()
if ($Paths -and $Paths.Count -gt 0) {
  foreach ($p in $Paths) {
    try {
      $resolved = Resolve-Path $p -ErrorAction Stop
      if ($resolved -and ($resolved.Path -like '*Drawer.cs')) {
        $filesToScan += $resolved.Path
      }
    } catch {
      Write-Info "Skipping path '$p' because it was not found."
    }
  }
} else {
  $drawerPath = 'Editor/CustomDrawers'
  if (Test-Path $drawerPath) {
    $filesToScan += Get-ChildItem -Recurse -Include *Drawer.cs -Path $drawerPath | Select-Object -ExpandProperty FullName
  }
}

$filesToScan = $filesToScan | Sort-Object -Unique

foreach ($file in $filesToScan) {
  if ($file -like '*.meta') { continue }
  $rel = Get-RelativePath $file

  $content = Get-Content $file
  $text = $content -join "`n"

  $lineIndex = 0
  foreach ($line in $content) {
    $lineIndex++

    # Skip lines with UNH-SUPPRESS comment
    if ($suppressPattern.IsMatch($line)) { continue }

    # Pattern 1: Property write during render (outside callbacks and apply methods)
    if ($propertyWritePattern.IsMatch($line)) {
      $isInCallback = Is-InsideCallback $line
      $isInApplyMethod = Is-InsideApplyMethod $content $lineIndex

      if ((-not $isInCallback) -and (-not $isInApplyMethod)) {
        # Check if this is inside a render method (OnGUI, Draw*, etc.)
        $isInRenderMethod = Is-InsideRenderMethod $content $lineIndex

        if ($isInRenderMethod) {
          $match = $propertyWritePattern.Match($line)
          $propertyType = $match.Groups[1].Value
          $warnings += (@{
            Path=$rel; Line=$lineIndex; Message="Property assignment during render: property.$propertyType ="
          })
        }
      }
    }

    # Pattern 2: Property read for index calculation without hasMultipleDifferentValues check nearby
    if ($propertyReadForIndexPattern.IsMatch($line)) {
      $hasCheck = Has-MixedValueCheck $content $lineIndex 15

      if (-not $hasCheck) {
        $match = $propertyReadForIndexPattern.Match($line)
        $propertyType = $match.Groups[2].Value
        $warnings += (@{
          Path=$rel; Line=$lineIndex; Message="Property used in index calculation without hasMultipleDifferentValues check: property.$propertyType"
        })
      }
    }

    # Pattern 3: Index clamping without mixed value check
    if ($indexClampPattern.IsMatch($line)) {
      $hasCheck = Has-MixedValueCheck $content $lineIndex 10

      if (-not $hasCheck) {
        $match = $indexClampPattern.Match($line)
        $warnings += (@{
          Path=$rel; Line=$lineIndex; Message="Index clamping without hasMultipleDifferentValues check"
        })
      }
    }
  }
}

if ($warnings.Count -gt 0) {
  foreach ($w in $warnings) {
    Write-Host ("[WARN] {0}:{1} - {2}" -f $w.Path, $w.Line, $w.Message) -ForegroundColor Yellow
  }
  Write-Host ("Found {0} potential multi-object editing issues (warnings only)." -f $warnings.Count) -ForegroundColor Yellow
} else {
  Write-Host "No potential multi-object editing issues found." -ForegroundColor Green
}

# Always exit with 0 - warnings are advisory, not blocking
exit 0
