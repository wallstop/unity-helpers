Param(
  [switch]$VerboseOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info($msg) {
  if ($VerboseOutput) { Write-Host "[lint-asmdef] $msg" -ForegroundColor Cyan }
}

function Write-WarningMsg($msg) {
  Write-Host "[lint-asmdef] WARNING: $msg" -ForegroundColor Yellow
}

function Write-ErrorMsg($msg) {
  Write-Host "[lint-asmdef] ERROR: $msg" -ForegroundColor Red
}

function Write-SuccessMsg($msg) {
  Write-Host "[lint-asmdef] $msg" -ForegroundColor Green
}

# Directories to scan for asmdef files
$sourceRoots = @('Runtime', 'Editor', 'Tests', 'Samples~', 'Styles', 'URP', 'Shaders')

# Directories to exclude
$excludeDirs = @('node_modules', '.git', 'obj', 'bin', 'Library', 'Temp')

# Well-known Unity assembly references that don't have .asmdef files in the repo
$unityBuiltInAssemblies = @(
  'UnityEngine',
  'UnityEngine.UI',
  'UnityEngine.TestRunner',
  'UnityEditor',
  'UnityEditor.UI',
  'UnityEditor.TestRunner',
  'Unity.TextMeshPro',
  'Unity.TextMeshPro.Editor',
  'Unity.InputSystem',
  'Unity.InputSystem.Editor',
  'Unity.Addressables',
  'Unity.Addressables.Editor',
  'Unity.ResourceManager',
  'Unity.Burst',
  'Unity.Burst.Editor',
  'Unity.Collections',
  'Unity.Jobs',
  'Unity.Mathematics',
  'Unity.Mathematics.Editor',
  'Unity.RenderPipelines.Core.Runtime',
  'Unity.RenderPipelines.Core.Editor',
  'Unity.RenderPipelines.Universal.Runtime',
  'Unity.RenderPipelines.Universal.Editor',
  'Unity.RenderPipelines.HighDefinition.Runtime',
  'Unity.RenderPipelines.HighDefinition.Editor',
  'Unity.Netcode.Runtime',
  'Unity.Netcode.Editor',
  'Unity.Netcode.Components',
  'Unity.Services.Core',
  'Unity.Services.Authentication',
  'Unity.VisualScripting.Core',
  'Unity.VisualScripting.Flow',
  'Unity.Localization',
  'Unity.Localization.Editor',
  'Unity.2D.Animation.Runtime',
  'Unity.2D.Animation.Editor',
  'Unity.2D.SpriteShape.Runtime',
  'Unity.2D.SpriteShape.Editor',
  'Unity.2D.Tilemap.Extras',
  'Unity.Timeline',
  'Unity.Timeline.Editor',
  'Unity.Cinemachine',
  'Unity.Cinemachine.Editor',
  'Unity.Entities',
  'Unity.Entities.Editor',
  'Unity.Transforms',
  'Unity.Physics',
  'Unity.Rendering.Hybrid',
  'Unity.ProBuilder',
  'Unity.ProBuilder.Editor',
  'Unity.Polybrush',
  'Unity.Polybrush.Editor',
  'Unity.Recorder',
  'Unity.Recorder.Editor'
)

# Well-known third-party packages (installed via Package Manager or Asset Store)
$thirdPartyAssemblies = @(
  # Dependency Injection frameworks
  'Zenject',
  'Zenject-usage',
  'Zenject.Editor',
  'VContainer',
  'VContainer.Unity',
  'VContainer.Editor',
  'Reflex',
  'Reflex.Editor',
  # Odin Inspector
  'Sirenix.OdinInspector.Attributes',
  'Sirenix.OdinInspector.Editor',
  'Sirenix.Serialization',
  'Sirenix.Serialization.Config',
  'Sirenix.Utilities',
  'Sirenix.Utilities.Editor',
  # UniTask
  'UniTask',
  'UniTask.Linq',
  'UniTask.Addressables',
  'UniTask.DOTween',
  'UniTask.TextMeshPro',
  # DOTween
  'DOTween.Modules',
  'DG.Tweening',
  # R3 (Reactive Extensions)
  'R3.Unity',
  'R3',
  # UniRx
  'UniRx',
  'UniRx.Async',
  # Newtonsoft Json
  'Newtonsoft.Json',
  'Unity.Nuget.Newtonsoft-Json',
  # MessagePack
  'MessagePack',
  'MessagePack.Unity',
  'MessagePack.Annotations',
  # Mirror Networking
  'Mirror',
  'Mirror.Components',
  # Photon
  'PhotonUnityNetworking',
  'PhotonRealtime',
  # NaughtyAttributes
  'NaughtyAttributes.Core',
  'NaughtyAttributes.Editor',
  # Other common packages
  'Spine.Unity',
  'Spine.Unity.Editor'
)

Write-Info "Starting Assembly Definition file validation..."

$repoRoot = (Get-Item $PSScriptRoot).Parent.FullName

# Collect all asmdef files and their names for reference validation
$allAsmdefFiles = @{}
$asmdefFilesToValidate = @()

foreach ($root in $sourceRoots) {
  $rootPath = Join-Path -Path $repoRoot -ChildPath $root
  if (-not (Test-Path $rootPath)) {
    Write-Info "Skipping $root (directory not found)"
    continue
  }

  $asmdefFiles = Get-ChildItem -Path $rootPath -Filter "*.asmdef" -Recurse -File | Where-Object {
    $path = $_.FullName
    $excluded = $false
    foreach ($dir in $excludeDirs) {
      if ($path -match [regex]::Escape("\$dir\") -or $path -match [regex]::Escape("/$dir/")) {
        $excluded = $true
        break
      }
    }
    -not $excluded
  }

  foreach ($file in $asmdefFiles) {
    $asmdefFilesToValidate += $file
    # Parse the file to get its name for reference checking
    try {
      $content = Get-Content -Path $file.FullName -Raw
      $json = $content | ConvertFrom-Json
      if ($json.name) {
        $allAsmdefFiles[$json.name] = $file.FullName
      }
    }
    catch {
      # Will be caught during validation
    }
  }
}

Write-Info "Found $($asmdefFilesToValidate.Count) .asmdef files to validate"
Write-Info "Building reference map with $($allAsmdefFiles.Count) assembly names"

$errorList = @()
$warningList = @()
$checkedCount = 0

foreach ($file in $asmdefFilesToValidate) {
  $checkedCount++
  $relativePath = $file.FullName.Replace($repoRoot, '').TrimStart('\', '/')
  $expectedName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)

  Write-Info "Validating: $relativePath"

  # 1. Validate JSON syntax
  $content = $null
  $json = $null
  try {
    $content = Get-Content -Path $file.FullName -Raw
    $json = $content | ConvertFrom-Json
  }
  catch {
    $errorList += "[$relativePath] Invalid JSON syntax: $($_.Exception.Message)"
    continue
  }

  # 2. Check that 'name' field matches filename
  if (-not $json.name) {
    $errorList += "[$relativePath] Missing required 'name' field"
  }
  elseif ($json.name -ne $expectedName) {
    $errorList += "[$relativePath] Name mismatch: 'name' field is '$($json.name)' but filename suggests '$expectedName'"
  }
  else {
    Write-Info "  Name field matches filename: $($json.name)"
  }

  # 3. Check that 'rootNamespace' is set (warning only)
  if (-not $json.rootNamespace -or $json.rootNamespace -eq '') {
    $warningList += "[$relativePath] Missing 'rootNamespace' field (recommended for code organization)"
  }
  else {
    Write-Info "  Root namespace: $($json.rootNamespace)"
  }

  # 4. Verify referenced assemblies exist
  if ($json.references -and $json.references.Count -gt 0) {
    Write-Info "  Checking $($json.references.Count) assembly references..."

    foreach ($ref in $json.references) {
      # Handle both string references and GUID references
      $refName = $ref
      if ($ref -match '^GUID:') {
        Write-Info "    GUID reference: $ref (skipping validation)"
        continue
      }

      # Check if it's a Unity built-in assembly
      if ($unityBuiltInAssemblies -contains $refName) {
        Write-Info "    Unity built-in: $refName"
        continue
      }

      # Check if it's a known third-party package
      if ($thirdPartyAssemblies -contains $refName) {
        Write-Info "    Third-party package: $refName"
        continue
      }

      # Check if it matches a known pattern for Unity packages
      if ($refName -match '^Unity\.' -or $refName -match '^UnityEngine\.' -or $refName -match '^UnityEditor\.') {
        Write-Info "    Unity package: $refName (assumed valid)"
        continue
      }

      # Check if assembly exists in the repo
      if ($allAsmdefFiles.ContainsKey($refName)) {
        Write-Info "    Found in repo: $refName"
      }
      else {
        $errorList += "[$relativePath] Referenced assembly '$refName' not found in repository"
      }
    }
  }
  else {
    Write-Info "  No assembly references"
  }
}

# Also validate any .asmref files
$asmrefFiles = @()
foreach ($root in $sourceRoots) {
  $rootPath = Join-Path -Path $repoRoot -ChildPath $root
  if (-not (Test-Path $rootPath)) {
    continue
  }

  $files = Get-ChildItem -Path $rootPath -Filter "*.asmref" -Recurse -File -ErrorAction SilentlyContinue | Where-Object {
    $path = $_.FullName
    $excluded = $false
    foreach ($dir in $excludeDirs) {
      if ($path -match [regex]::Escape("\$dir\") -or $path -match [regex]::Escape("/$dir/")) {
        $excluded = $true
        break
      }
    }
    -not $excluded
  }

  if ($files) {
    $asmrefFiles += $files
  }
}

if ($asmrefFiles.Count -gt 0) {
  Write-Info ""
  Write-Info "Validating $($asmrefFiles.Count) .asmref files..."

  foreach ($file in $asmrefFiles) {
    $checkedCount++
    $relativePath = $file.FullName.Replace($repoRoot, '').TrimStart('\', '/')

    Write-Info "Validating: $relativePath"

    # Validate JSON syntax
    try {
      $content = Get-Content -Path $file.FullName -Raw
      $json = $content | ConvertFrom-Json
    }
    catch {
      $errorList += "[$relativePath] Invalid JSON syntax: $($_.Exception.Message)"
      continue
    }

    # Check that 'reference' field points to a valid assembly
    if (-not $json.reference) {
      $errorList += "[$relativePath] Missing required 'reference' field"
    }
    else {
      $refName = $json.reference
      if ($refName -match '^GUID:') {
        Write-Info "  GUID reference: $refName (skipping validation)"
      }
      elseif ($unityBuiltInAssemblies -contains $refName -or $thirdPartyAssemblies -contains $refName -or $refName -match '^Unity\.' -or $refName -match '^UnityEngine\.' -or $refName -match '^UnityEditor\.') {
        Write-Info "  Unity/third-party assembly: $refName"
      }
      elseif ($allAsmdefFiles.ContainsKey($refName)) {
        Write-Info "  Found in repo: $refName"
      }
      else {
        $errorList += "[$relativePath] Referenced assembly '$refName' not found in repository"
      }
    }
  }
}

Write-Info ""
Write-Info "Summary:"
Write-Info "  Files checked: $checkedCount"
Write-Info "  Errors: $($errorList.Count)"
Write-Info "  Warnings: $($warningList.Count)"

# Print warnings
if ($warningList.Count -gt 0) {
  Write-Host ""
  Write-WarningMsg "Warnings found:"
  foreach ($warn in $warningList) {
    Write-WarningMsg "  $warn"
  }
}

# Print errors and exit with failure if any
if ($errorList.Count -gt 0) {
  Write-Host ""
  Write-ErrorMsg "Errors found:"
  foreach ($err in $errorList) {
    Write-ErrorMsg "  $err"
  }
  Write-Host ""
  Write-ErrorMsg "Assembly Definition validation failed with $($errorList.Count) error(s)."
  exit 1
}

Write-Host ""
Write-SuccessMsg "All Assembly Definition files are valid!"
exit 0
