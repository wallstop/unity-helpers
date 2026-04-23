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

function Test-GitPushConfig {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        [Parameter(Mandatory = $true)]
        [ref]$FailureCount,
        [switch]$Fix
    )

    $expected = @{
        'push.autoSetupRemote' = 'true'
        'push.default' = 'simple'
    }

    $mismatches = New-Object System.Collections.Generic.List[string]

    Push-Location $RepoRoot
    try {
        foreach ($key in $expected.Keys) {
            $actual = & git config --local --get $key 2>$null
            if ($LASTEXITCODE -ne 0) { $actual = '' }
            if ($actual -ne $expected[$key]) {
                $display = if ([string]::IsNullOrWhiteSpace($actual)) { 'unset' } else { $actual }
                $mismatches.Add("$key is '$display' (expected '$($expected[$key])')") | Out-Null
            }
        }
    }
    finally {
        Pop-Location
    }

    if ($mismatches.Count -eq 0) {
        Write-Info 'Git push defaults OK (push.autoSetupRemote=true, push.default=simple).'
        return
    }

    if ($Fix) {
        Write-Host '[agent-preflight] Fixing git push defaults via scripts/configure-git-defaults.ps1...' -ForegroundColor Blue
        $configureScript = Join-Path $RepoRoot 'scripts/configure-git-defaults.ps1'
        if (-not (Test-Path -LiteralPath $configureScript)) {
            Write-ErrorMsg "configure-git-defaults.ps1 not found at $configureScript"
            $FailureCount.Value++
            return
        }

        & pwsh -NoProfile -File $configureScript -RepoRoot $RepoRoot
        if ($LASTEXITCODE -ne 0) {
            Write-ErrorMsg 'scripts/configure-git-defaults.ps1 failed; git push defaults were NOT applied.'
            $FailureCount.Value++
            return
        }

        # Exit code of the configure script is necessary but not sufficient —
        # re-read both local config values to verify persistence is the contract
        # check. Anything that could silently swallow the write (permissions on
        # .git/config, a wrapper shim, a concurrent external edit) would be
        # caught here rather than reported as a false "fixed" state.
        $verifyMismatches = New-Object System.Collections.Generic.List[string]
        Push-Location $RepoRoot
        try {
            foreach ($key in $expected.Keys) {
                $verified = & git config --local --get $key 2>$null
                if ($LASTEXITCODE -ne 0) { $verified = '' }
                if ($verified -ne $expected[$key]) {
                    $display = if ([string]::IsNullOrWhiteSpace($verified)) { 'unset' } else { $verified }
                    $verifyMismatches.Add("$key is '$display' (expected '$($expected[$key])')") | Out-Null
                }
            }
        }
        finally {
            Pop-Location
        }

        if ($verifyMismatches.Count -gt 0) {
            Write-ErrorMsg 'scripts/configure-git-defaults.ps1 exited 0 but git push defaults did NOT persist:'
            foreach ($item in $verifyMismatches) {
                Write-Host "  $item" -ForegroundColor Yellow
            }
            Write-Host 'Run scripts/configure-git-defaults.ps1 manually and inspect .git/config for permission or wrapper issues.' -ForegroundColor Cyan
            $FailureCount.Value++
            return
        }

        Write-Host '[agent-preflight] Git push defaults applied and verified.' -ForegroundColor Green
        return
    }

    Write-ErrorMsg 'Git push defaults are not configured for this repository:'
    foreach ($item in $mismatches) {
        Write-Host "  $item" -ForegroundColor Yellow
    }
    Write-Host 'Run: npm run agent:preflight:fix, or manually: git config --local push.autoSetupRemote true && git config --local push.default simple' -ForegroundColor Cyan
    $FailureCount.Value++
}

function Test-StrayArtifactFiles {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        [Parameter(Mandatory = $true)]
        [ref]$FailureCount,
        [switch]$Fix
    )

    $hooksDir = Join-Path $RepoRoot '.githooks'
    if (-not (Test-Path -LiteralPath $hooksDir -PathType Container)) {
        return
    }

    $hookNames = @(
        Get-ChildItem -LiteralPath $hooksDir -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -notlike '*.sample' } |
            ForEach-Object {
                if ($_.Name -like '*.*') {
                    [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
                }
                else {
                    $_.Name
                }
            } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            Sort-Object -Unique
    )

    $artifactExtensions = @('txt', 'log', 'out', 'err', 'tmp')
    $strayFiles = New-Object System.Collections.Generic.List[string]

    foreach ($hook in $hookNames) {
        foreach ($ext in $artifactExtensions) {
            $candidate = Join-Path $RepoRoot "$hook.$ext"
            if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                $strayFiles.Add($candidate) | Out-Null
            }
        }
    }

    $hooksScanExts = @('tmp', 'log', 'out', 'err')
    foreach ($ext in $hooksScanExts) {
        $matched = Get-ChildItem -LiteralPath $hooksDir -Filter "*.$ext" -File -ErrorAction SilentlyContinue
        foreach ($file in $matched) {
            $strayFiles.Add($file.FullName) | Out-Null
        }
    }

    $uniqueStrayFiles = @($strayFiles | Sort-Object -Unique)
    if ($uniqueStrayFiles.Count -eq 0) {
        return
    }

    # Safety gate: only delete files that git confirms are gitignored. A file
    # matching the error-log pattern that is NOT gitignored may be a legitimate
    # user artifact (e.g. a committed or intentionally-tracked "pre-commit.log"
    # note) and must not be silently removed.
    #
    # git check-ignore exit codes:
    #   0   -> ignored
    #   1   -> not ignored
    #   128 -> error (e.g. not a git repo, IO failure)
    $ignoredFiles = New-Object System.Collections.Generic.List[string]
    $unignoredFiles = New-Object System.Collections.Generic.List[string]
    $checkIgnoreErrors = New-Object System.Collections.Generic.List[string]
    foreach ($file in $uniqueStrayFiles) {
        & git -C $RepoRoot check-ignore -q -- "$file" 2>$null
        $checkExit = $LASTEXITCODE
        switch ($checkExit) {
            0 { $ignoredFiles.Add($file) | Out-Null }
            1 { $unignoredFiles.Add($file) | Out-Null }
            default {
                # Treat check-ignore failures as "unsafe to delete". The file is
                # still a match for an error-log pattern so we surface it, but
                # we refuse to delete it without a clean ignore confirmation.
                $checkIgnoreErrors.Add("${file}: git check-ignore exit $checkExit") | Out-Null
                $unignoredFiles.Add($file) | Out-Null
            }
        }
    }

    if ($Fix) {
        if ($ignoredFiles.Count -gt 0) {
            Write-Host '[agent-preflight] Removing stray git-hook artifact file(s) (verified gitignored):' -ForegroundColor Blue
            foreach ($file in $ignoredFiles) {
                try {
                    Remove-Item -LiteralPath $file -Force -ErrorAction Stop
                    Write-Host "  removed: $file" -ForegroundColor Green
                }
                catch {
                    Write-ErrorMsg "Failed to remove ${file}: $($_.Exception.Message)"
                    $FailureCount.Value++
                }
            }
        }

        if ($unignoredFiles.Count -gt 0) {
            Write-WarningMsg 'Skipped deletion of stray artifact file(s) not confirmed as gitignored:'
            foreach ($file in $unignoredFiles) {
                Write-Host "  $file" -ForegroundColor Yellow
            }
            if ($checkIgnoreErrors.Count -gt 0) {
                Write-Host 'git check-ignore encountered errors on:' -ForegroundColor Yellow
                foreach ($entry in $checkIgnoreErrors) {
                    Write-Host "  $entry" -ForegroundColor Yellow
                }
            }
            Write-Host 'These files match an error-log pattern but are not gitignored. Delete manually if stale, or add a .gitignore entry and re-run.' -ForegroundColor Cyan
            $FailureCount.Value++
        }
        return
    }

    Write-ErrorMsg 'Stray git-hook artifact file(s) detected (likely redirected hook output):'
    if ($ignoredFiles.Count -gt 0) {
        Write-Host '  gitignored (safe to auto-delete with -Fix):' -ForegroundColor Yellow
        foreach ($file in $ignoredFiles) {
            Write-Host "    $file" -ForegroundColor Yellow
        }
    }
    if ($unignoredFiles.Count -gt 0) {
        Write-Host '  NOT gitignored (manual review required):' -ForegroundColor Yellow
        foreach ($file in $unignoredFiles) {
            Write-Host "    $file" -ForegroundColor Yellow
        }
    }
    if ($checkIgnoreErrors.Count -gt 0) {
        Write-Host '  git check-ignore errors:' -ForegroundColor Yellow
        foreach ($entry in $checkIgnoreErrors) {
            Write-Host "    $entry" -ForegroundColor Yellow
        }
    }
    if ($ignoredFiles.Count -gt 0) {
        Write-Host 'Run with -Fix to delete the gitignored files (npm run agent:preflight:fix). Never redirect git command output to files in the working tree.' -ForegroundColor Cyan
    }
    if ($unignoredFiles.Count -gt 0) {
        Write-Host 'Files matching an error-log pattern but not gitignored will NOT be auto-deleted - delete manually if stale, or add a .gitignore entry and re-run.' -ForegroundColor Cyan
    }
    $FailureCount.Value++
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

$failureCount = 0

Test-GitPushConfig -RepoRoot $repoRoot -FailureCount ([ref]$failureCount) -Fix:$Fix
Test-StrayArtifactFiles -RepoRoot $repoRoot -FailureCount ([ref]$failureCount) -Fix:$Fix

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
    if ($failureCount -gt 0) {
        Write-Host "[agent-preflight] Failed with $failureCount check group(s) reporting issues." -ForegroundColor Red
        exit 1
    }
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
                        # Capture cspell output so we can (a) surface it
                        # verbatim to the caller and (b) extract lint-error-
                        # code-shaped unknown tokens and print a copy-pasteable
                        # cspell.json patch. This makes the agent preflight
                        # the EARLIEST point at which a new lint-error-code
                        # family without a cspell entry is caught — before
                        # any hook runs.
                        $spellingOutput = & $npxCommand --no-install cspell lint --no-must-find-files --no-progress --show-suggestions --file-list $spellingFileList 2>&1
                        $spellingExit = $LASTEXITCODE
                        foreach ($line in $spellingOutput) { Write-Host $line }
                        if ($spellingExit -ne 0) {
                            Write-ErrorMsg 'Spelling errors detected in changed markdown files.'
                            $unknownPrefixes = @()
                            foreach ($line in $spellingOutput) {
                                $text = [string]$line
                                # Width: unbounded (>=2) because cspell never
                                # emits monster tokens and a narrow upper
                                # bound (originally 5) let prefixes longer
                                # than 5 chars slip past the patch emitter —
                                # the exact fragility reviewed in P0-3.
                                $codeMatch = [regex]::Match($text, 'Unknown word \(([A-Z]{2,})\)')
                                if ($codeMatch.Success) {
                                    $unknownPrefixes += $codeMatch.Groups[1].Value
                                }
                            }
                            $unknownPrefixes = @($unknownPrefixes | Sort-Object -Unique)
                            if ($unknownPrefixes.Count -gt 0) {
                                Write-Host ''
                                Write-Host '=== Detected unregistered lint-error-code prefix(es) ===' -ForegroundColor Red
                                Write-Host 'Copy-paste this patch into the root "words" array in cspell.json' -ForegroundColor Yellow
                                Write-Host '(append each quoted prefix as a new array element):' -ForegroundColor Yellow
                                Write-Host ''
                                foreach ($prefix in $unknownPrefixes) {
                                    Write-Host ('    "{0}",' -f $prefix) -ForegroundColor White
                                }
                                Write-Host ''
                                Write-Host 'See scripts/validate-lint-error-codes.ps1 for the contract that' -ForegroundColor Cyan
                                Write-Host 'enforces this requirement once the prefix is registered.' -ForegroundColor Cyan
                                Write-Host ''
                            }
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
