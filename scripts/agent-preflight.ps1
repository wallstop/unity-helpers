Param(
    [string[]]$Paths,
    [switch]$Fix,
    [switch]$AllowCriticalSkillSize,
    [switch]$VerboseOutput,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$AdditionalPaths
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'git-staging-helpers.ps1')

function Write-Info($Message) {
    if ($VerboseOutput) {
        Write-Host "[agent-preflight] $Message" -ForegroundColor Cyan
    }
}

function Write-ErrorMsg($Message) {
    Write-Host "[agent-preflight] ERROR: $Message" -ForegroundColor Red
}

function Write-WarningMsg($Message) {
    Write-Host "[agent-preflight] WARNING: $Message" -ForegroundColor Yellow
}

function Resolve-RepoRelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $null
    }

    $normalizedRepoRoot = [System.IO.Path]::GetFullPath($RepoRoot)
    $normalizedRepoRoot = $normalizedRepoRoot.Replace('\\', '/')

    if ([System.IO.Path]::IsPathRooted($Path)) {
        $fullPath = [System.IO.Path]::GetFullPath($Path)
        $fullPath = $fullPath.Replace('\\', '/')
        $repoPrefix = "$normalizedRepoRoot/"
        if ($fullPath -eq $normalizedRepoRoot) {
            return '.'
        }
        if (-not $fullPath.StartsWith($repoPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $null
        }
        return $fullPath.Substring($repoPrefix.Length)
    }

    $normalizedPath = $Path.Replace('\\', '/')
    if ($normalizedPath.StartsWith('./')) {
        return $normalizedPath.Substring(2)
    }

    return $normalizedPath
}

function Get-GitChangedPaths {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $results = New-Object System.Collections.Generic.List[string]
    $commands = @(
        @('diff', '--name-only', '--diff-filter=ACMRTUXB'),
        @('diff', '--cached', '--name-only', '--diff-filter=ACMRTUXB'),
        @('ls-files', '--others', '--exclude-standard')
    )

    Push-Location $RepoRoot
    try {
        foreach ($command in $commands) {
            $output = & git @command 2>$null
            if ($LASTEXITCODE -ne 0) {
                continue
            }

            foreach ($line in $output) {
                if ([string]::IsNullOrWhiteSpace($line)) {
                    continue
                }
                $results.Add($line)
            }
        }
    }
    finally {
        Pop-Location
    }

    return @($results)
}

function Get-GitStagedPaths {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $stagedPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)

    Push-Location $RepoRoot
    try {
        $output = & git diff --cached --name-only --diff-filter=ACM 2>$null
        if ($LASTEXITCODE -ne 0) {
            return ,$stagedPaths
        }

        foreach ($line in $output) {
            if ([string]::IsNullOrWhiteSpace($line)) {
                continue
            }

            $normalizedPath = $line.Replace('\\', '/')
            $stagedPaths.Add($normalizedPath) | Out-Null
        }
    }
    finally {
        Pop-Location
    }

    return ,$stagedPaths
}

function Add-PathsToGitIndexWithRetry {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        [Parameter(Mandatory = $true)]
        [string[]]$Paths
    )

    if ($null -eq $Paths -or $Paths.Count -eq 0) {
        return $true
    }

    $uniquePaths = @($Paths | Sort-Object -Unique)
    $indexLockPath = Join-Path -Path (Join-Path -Path $RepoRoot -ChildPath '.git') -ChildPath 'index.lock'

    Push-Location $RepoRoot
    try {
        $exitCode = Invoke-GitAddWithRetry -Items $uniquePaths -IndexLockPath $indexLockPath
        return ($exitCode -eq 0)
    }
    finally {
        Pop-Location
    }
}

function Test-MetaRequiredPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    if ($RelativePath -notmatch '^(Runtime|Editor|Tests|Samples~|Shaders|Styles|URP|docs|scripts)/') {
        return $false
    }

    $leaf = Split-Path -Path $RelativePath -Leaf
    if ($RelativePath -like '*.meta') { return $false }
    if ($leaf -eq 'package-lock.json') { return $false }
    if ($leaf -eq 'Gemfile.lock') { return $false }
    if ($RelativePath -like '*.tmp') { return $false }
    if ($leaf -eq '.gitkeep') { return $false }
    if ($leaf -eq '.DS_Store') { return $false }
    if ($leaf -eq 'Thumbs.db') { return $false }
    if ($RelativePath -like '*.pyc') { return $false }
    if ($RelativePath -like '*.pyo') { return $false }
    if ($RelativePath -like '*.swp') { return $false }
    if ($RelativePath -like '*.swo') { return $false }

    return $true
}

$repoRoot = (Get-Item $PSScriptRoot).Parent.FullName
$sourceRoots = @('Runtime', 'Editor', 'Tests', 'Samples~', 'Shaders', 'Styles', 'URP', 'docs', 'scripts')

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-ErrorMsg 'git is required to compute changed files.'
    exit 1
}

$candidatePaths = if ($null -ne $Paths -and $Paths.Count -gt 0) {
    $resolved = @($Paths)
    if ($null -ne $AdditionalPaths -and $AdditionalPaths.Count -gt 0) {
        $resolved += $AdditionalPaths
    }
    $resolved
}
else {
    Get-GitChangedPaths -RepoRoot $repoRoot
}

$dedupedPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$relativePaths = New-Object System.Collections.Generic.List[string]

foreach ($candidate in $candidatePaths) {
    $relative = Resolve-RepoRelativePath -Path $candidate -RepoRoot $repoRoot
    if ($null -eq $relative -or [string]::IsNullOrWhiteSpace($relative) -or $relative -eq '.') {
        continue
    }

    if ($dedupedPaths.Add($relative)) {
        $relativePaths.Add($relative) | Out-Null
    }
}

if ($relativePaths.Count -eq 0) {
    Write-Host '[agent-preflight] No changed files detected. Nothing to validate.' -ForegroundColor Green
    exit 0
}

Write-Info "Detected $($relativePaths.Count) changed path(s)."

$llmFiles = @($relativePaths | Where-Object { $_ -like '.llm/*' })
$llmSizeTargets = @(
    $relativePaths | Where-Object {
        $_ -eq '.llm/context.md' -or $_ -like '.llm/skills/*.md'
    }
)
$testFiles = @($relativePaths | Where-Object { $_ -like 'Tests/*.cs' })
$metaRelevantPaths = @($relativePaths | Where-Object { Test-MetaRequiredPath -RelativePath $_ })

if ($metaRelevantPaths.Count -gt 0 -and -not (Invoke-EnsureNoIndexLock)) {
    if ($Fix) {
        Write-WarningMsg 'index.lock still held after waiting; auto-stage operations may fail if contention persists.'
    }
    else {
        Write-WarningMsg 'index.lock is currently held by another process. Read-only checks can pass while commit-time auto-stage fails. Close competing git tools and run npm run agent:preflight:fix before committing.'
    }
}

$failureCount = 0

if ($llmSizeTargets.Count -gt 0) {
    Write-Host '[agent-preflight] Checking changed skill/context file sizes...' -ForegroundColor Blue
    $failOnCritical = -not $AllowCriticalSkillSize
    & (Join-Path $repoRoot 'scripts/lint-skill-sizes.ps1') -Paths $llmSizeTargets -FailOnCritical:$failOnCritical -VerboseOutput:$VerboseOutput
    if ($LASTEXITCODE -ne 0) {
        $failureCount++
    }
}

if ($llmFiles.Count -gt 0) {
    Write-Host '[agent-preflight] Validating LLM instruction consistency...' -ForegroundColor Blue
    & (Join-Path $repoRoot 'scripts/lint-llm-instructions.ps1') -Fix:$Fix -VerboseOutput:$VerboseOutput
    if ($LASTEXITCODE -ne 0) {
        $failureCount++
    }
}

# Validate changed markdown spelling early; full-repo spelling still runs in validate:prepush.
$spellingTargets = @(
    $relativePaths | Where-Object {
        $_ -like '*.md' -or $_ -like '*.markdown'
    }
)

if ($spellingTargets.Count -gt 0) {
    Write-Host '[agent-preflight] Checking spelling on changed markdown files...' -ForegroundColor Blue
    $npxCommand = if ([string]::IsNullOrWhiteSpace($env:AGENT_PREFLIGHT_NPX_COMMAND)) { 'npx' } else { $env:AGENT_PREFLIGHT_NPX_COMMAND }

    if (-not (Get-Command $npxCommand -ErrorAction SilentlyContinue)) {
        Write-ErrorMsg 'npx is required for spelling checks. Install Node.js/npm and run npm install.'
        $failureCount++
    }
    else {
        $spellingFileList = $null
        try {
            $spellingFileList = [System.IO.Path]::GetTempFileName()
            Set-Content -LiteralPath $spellingFileList -Value $spellingTargets -Encoding UTF8
        }
        catch {
            Write-ErrorMsg "Failed to prepare temporary spelling file list: $($_.Exception.Message)"
            $failureCount++
        }

        if ($null -ne $spellingFileList) {
            try {
                Push-Location $repoRoot
                try {
                    & $npxCommand --no-install cspell --version *> $null
                    if ($LASTEXITCODE -ne 0) {
                        Write-ErrorMsg 'cspell is not installed in this repository. Run npm install to enable spelling checks.'
                        $failureCount++
                    }
                    else {
                        & $npxCommand --no-install cspell lint --no-must-find-files --no-progress --show-suggestions --file-list $spellingFileList
                        if ($LASTEXITCODE -ne 0) {
                            Write-ErrorMsg 'Spelling errors detected in changed markdown files.'
                            Write-Host 'Run: npm run lint:spelling' -ForegroundColor Cyan
                            $failureCount++
                        }
                    }
                }
                finally {
                    Pop-Location
                }
            }
            finally {
                Remove-Item -LiteralPath $spellingFileList -ErrorAction SilentlyContinue
            }
        }
    }
}

if ($testFiles.Count -gt 0) {
    if ($Fix) {
        Write-Host '[agent-preflight] Auto-fixing Unity null assertions in changed tests...' -ForegroundColor Blue
        & (Join-Path $repoRoot 'scripts/lint-tests.ps1') -FixNullChecks -Paths $testFiles
        if ($LASTEXITCODE -ne 0) {
            $failureCount++
        }
    }

    Write-Host '[agent-preflight] Running test linter on changed tests...' -ForegroundColor Blue
    & (Join-Path $repoRoot 'scripts/lint-tests.ps1') -Paths $testFiles -VerboseOutput:$VerboseOutput
    if ($LASTEXITCODE -ne 0) {
        $failureCount++
    }
}

if ($metaRelevantPaths.Count -gt 0) {
    Write-Host '[agent-preflight] Checking Unity .meta coverage for changed paths...' -ForegroundColor Blue

    $missingMetaTargets = New-Object System.Collections.Generic.List[string]
    $dirSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

    foreach ($path in $metaRelevantPaths) {
        $fullPath = Join-Path -Path $repoRoot -ChildPath $path
        if (-not (Test-Path -LiteralPath $fullPath)) {
            continue
        }

        if (-not (Test-Path -LiteralPath "$fullPath.meta")) {
            $missingMetaTargets.Add($path) | Out-Null
        }

        $directory = if (Test-Path -LiteralPath $fullPath -PathType Container) {
            $path
        }
        else {
            Split-Path -Path $path -Parent
        }

        while (-not [string]::IsNullOrWhiteSpace($directory) -and $directory -ne '.') {
            if ($sourceRoots -contains $directory) {
                break
            }

            $dirSet.Add($directory) | Out-Null
            $directory = Split-Path -Path $directory -Parent
        }
    }

    foreach ($directory in $dirSet) {
        $dirPath = Join-Path -Path $repoRoot -ChildPath $directory
        if (-not (Test-Path -LiteralPath $dirPath -PathType Container)) {
            continue
        }

        if (-not (Test-Path -LiteralPath "$dirPath.meta")) {
            $missingMetaTargets.Add($directory) | Out-Null
        }
    }

    if ($missingMetaTargets.Count -gt 0 -and $Fix) {
        if (Get-Command bash -ErrorAction SilentlyContinue) {
            foreach ($target in ($missingMetaTargets | Sort-Object -Unique)) {
                Write-Info "Generating meta for $target"
                & bash (Join-Path $repoRoot 'scripts/generate-meta.sh') $target
                if ($LASTEXITCODE -ne 0) {
                    Write-ErrorMsg "Failed to auto-generate .meta for: $target"
                    $failureCount++
                }
            }

            # Re-check after generation
            $remaining = @()
            foreach ($target in ($missingMetaTargets | Sort-Object -Unique)) {
                $targetPath = Join-Path -Path $repoRoot -ChildPath $target
                if (-not (Test-Path -LiteralPath "$targetPath.meta")) {
                    $remaining += $target
                }
            }

            $missingMetaTargets = New-Object System.Collections.Generic.List[string]
            foreach ($target in $remaining) {
                $missingMetaTargets.Add($target) | Out-Null
            }
        }
        else {
            Write-WarningMsg 'bash not found; cannot auto-generate missing .meta files.'
        }
    }

    if ($missingMetaTargets.Count -gt 0) {
        Write-ErrorMsg 'Missing .meta files detected for changed paths:'
        foreach ($target in ($missingMetaTargets | Sort-Object -Unique)) {
            Write-Host "  $target" -ForegroundColor Yellow
        }
        Write-Host 'Generate with: ./scripts/generate-meta.sh <path>' -ForegroundColor Cyan
        $failureCount++
    }

        $stagedPaths = Get-GitStagedPaths -RepoRoot $repoRoot
    if ($stagedPaths.Count -gt 0) {
        $unstagedMetaCompanionsSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
        $pathScopedMetaRelevantSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
        foreach ($metaPath in $metaRelevantPaths) {
            $pathScopedMetaRelevantSet.Add($metaPath) | Out-Null
        }

        foreach ($stagedPath in $stagedPaths) {
            if (($null -ne $Paths -and $Paths.Count -gt 0) -and -not $pathScopedMetaRelevantSet.Contains($stagedPath)) {
                continue
            }

            if (-not (Test-MetaRequiredPath -RelativePath $stagedPath)) {
                continue
            }

            $stagedFullPath = Join-Path -Path $repoRoot -ChildPath $stagedPath
            if (-not (Test-Path -LiteralPath $stagedFullPath)) {
                continue
            }

            $fileMetaPath = "$stagedPath.meta"
            $fileMetaFullPath = Join-Path -Path $repoRoot -ChildPath $fileMetaPath
            if ((Test-Path -LiteralPath $fileMetaFullPath) -and -not $stagedPaths.Contains($fileMetaPath)) {
                $unstagedMetaCompanionsSet.Add($fileMetaPath) | Out-Null
            }

            $directory = if (Test-Path -LiteralPath $stagedFullPath -PathType Container) {
                $stagedPath
            }
            else {
                (Split-Path -Path $stagedPath -Parent).Replace('\\', '/')
            }

            while (-not [string]::IsNullOrWhiteSpace($directory) -and $directory -ne '.') {
                if ($sourceRoots -contains $directory) {
                    break
                }

                $directoryMetaPath = "$directory.meta"
                $directoryMetaFullPath = Join-Path -Path $repoRoot -ChildPath $directoryMetaPath
                if ((Test-Path -LiteralPath $directoryMetaFullPath) -and -not $stagedPaths.Contains($directoryMetaPath)) {
                    $unstagedMetaCompanionsSet.Add($directoryMetaPath) | Out-Null
                }

                $directory = Split-Path -Path $directory -Parent
                if (-not [string]::IsNullOrWhiteSpace($directory)) {
                    $directory = $directory.Replace('\\', '/')
                }
            }
        }

        $unstagedMetaCompanions = @($unstagedMetaCompanionsSet | Sort-Object)
        if ($unstagedMetaCompanions.Count -gt 0) {
            if ($Fix) {
                Write-Host '[agent-preflight] Auto-staging unstaged .meta companions for staged files...' -ForegroundColor Blue
                if (Add-PathsToGitIndexWithRetry -RepoRoot $repoRoot -Paths $unstagedMetaCompanions) {
                    Write-Host "[agent-preflight] Staged $($unstagedMetaCompanions.Count) .meta companion file(s)." -ForegroundColor Green
                }
                else {
                    Write-ErrorMsg 'Failed to stage one or more .meta companion files. Git index.lock contention or another git error is likely.'
                    foreach ($metaPath in $unstagedMetaCompanions) {
                        Write-Host "  $metaPath" -ForegroundColor Yellow
                    }
                    Write-Host 'Close other git operations, then re-run npm run agent:preflight:fix.' -ForegroundColor Cyan
                    $failureCount++
                }
            }
            else {
                Write-ErrorMsg 'Unstaged .meta companion files detected for staged paths:'
                foreach ($metaPath in $unstagedMetaCompanions) {
                    Write-Host "  $metaPath" -ForegroundColor Yellow
                }
                Write-Host 'Run with -Fix to auto-stage these files (npm run agent:preflight:fix).' -ForegroundColor Cyan
                $failureCount++
            }
        }
    }
}

if ($failureCount -gt 0) {
    Write-Host "[agent-preflight] Failed with $failureCount check group(s) reporting issues." -ForegroundColor Red
    exit 1
}

Write-Host '[agent-preflight] All relevant changed-file checks passed.' -ForegroundColor Green
exit 0
