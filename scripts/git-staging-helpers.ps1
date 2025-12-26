# Shared helpers for git-aware formatter/linter scripts to avoid duplicated logic.
#
# IMPORTANT: All git index operations (git add, git reset, git checkout-index, etc.)
# MUST use the retry helpers from this module to handle index.lock contention properly.
# Never call `git add` directly in scripts - always use Invoke-GitAddWithRetry.
#
# The index.lock file is created by git during any operation that modifies the index.
# When using tools like lazygit or running multiple hooks, concurrent operations can
# cause "fatal: Unable to create '.git/index.lock': File exists" errors.

# Script-scoped mutex for coordinating git operations within the same PowerShell session.
# This prevents race conditions when multiple hooks run in parallel within a single process.
$script:GitOperationMutex = $null

function Get-GitOperationMutex {
    if ($null -eq $script:GitOperationMutex) {
        # Use a named mutex so all PowerShell processes coordinate
        $mutexName = "Global\UnityHelpers_GitIndexOperation"
        try {
            $script:GitOperationMutex = [System.Threading.Mutex]::new($false, $mutexName)
        } catch {
            # Fallback: create a local mutex if global fails (e.g., permissions)
            $script:GitOperationMutex = [System.Threading.Mutex]::new($false)
        }
    }
    return $script:GitOperationMutex
}

function Assert-GitAvailable {
    $git = Get-Command git -ErrorAction SilentlyContinue
    if (-not $git) {
        throw "git not found; cannot inspect or stage files."
    }

    return $git
}

function Get-GitRepositoryInfo {
    $gitDirResult = & git rev-parse --git-dir 2>$null
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($gitDirResult)) {
        throw "git rev-parse --git-dir failed; ensure this script runs inside a git repository."
    }

    try {
        $resolvedGitDir = (Resolve-Path -LiteralPath $gitDirResult.Trim()).Path
    } catch {
        throw "Unable to resolve git directory path: $gitDirResult"
    }

    # Also get repository root for scripts that need it
    $repoRoot = & git rev-parse --show-toplevel 2>$null
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repoRoot)) {
        $repoRoot = Split-Path -Parent $resolvedGitDir
    }

    return [pscustomobject]@{
        Directory      = $resolvedGitDir
        IndexLockPath  = (Join-Path -Path $resolvedGitDir -ChildPath 'index.lock')
        RepositoryRoot = $repoRoot.Trim()
    }
}

function Wait-ForGitIndexLock {
    param(
        [string]$IndexLockPath,
        [int]$MaxWaitMilliseconds = 30000,
        [int]$PollIntervalMilliseconds = 50
    )

    if (-not $IndexLockPath) {
        return $true
    }

    $elapsed = 0
    while ((Test-Path -LiteralPath $IndexLockPath) -and $elapsed -lt $MaxWaitMilliseconds) {
        Start-Sleep -Milliseconds $PollIntervalMilliseconds
        $elapsed += $PollIntervalMilliseconds
    }

    return -not (Test-Path -LiteralPath $IndexLockPath)
}

function Get-StagedPathsForGlobs {
    param(
        [string[]]$DefaultPaths,
        [string[]]$Globs
    )

    if ($DefaultPaths -and $DefaultPaths.Count -gt 0) {
        return $DefaultPaths
    }

    if (-not $Globs -or $Globs.Count -eq 0) {
        return @()
    }

    $gitDiffArgs = @('diff', '--cached', '--name-only', '--diff-filter=ACM', '--')
    $gitDiffArgs += $Globs
    $paths = & git @gitDiffArgs
    if ($LASTEXITCODE -ne 0 -or -not $paths) {
        return @()
    }

    return $paths
}

function Get-ExistingPaths {
    param([string[]]$Candidates)

    $existingPaths = @()
    foreach ($candidate in $Candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }

        if (Test-Path -LiteralPath $candidate) {
            $existingPaths += $candidate
        }
    }

    return $existingPaths
}

function Invoke-GitAddWithRetry {
    param(
        [string[]]$Items,
        [string]$IndexLockPath,
        [int]$MaxAttempts = 30,
        [int]$InitialDelayMilliseconds = 50,
        [int]$MaxDelayMilliseconds = 3000,
        [switch]$Quiet
    )

    if (-not $Items -or $Items.Count -eq 0) {
        return 0
    }

    $gitArgs = @('add', '--')
    $gitArgs += $Items

    $mutex = Get-GitOperationMutex
    $mutexAcquired = $false

    try {
        # Try to acquire the mutex with a timeout
        try {
            $mutexAcquired = $mutex.WaitOne(10000) # 10 second timeout
        } catch [System.Threading.AbandonedMutexException] {
            # Another process crashed while holding the mutex; we now own it
            $mutexAcquired = $true
        } catch {
            # Mutex failed, continue without it
            $mutexAcquired = $false
        }

        for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
            # Wait for any existing lock to be released before attempting
            if ($IndexLockPath -and (Test-Path -LiteralPath $IndexLockPath)) {
                $lockCleared = Wait-ForGitIndexLock -IndexLockPath $IndexLockPath -MaxWaitMilliseconds 5000 -PollIntervalMilliseconds 50
                if (-not $lockCleared) {
                    if (-not $Quiet) {
                        Write-Warning "git index.lock still exists after waiting; attempting git add anyway (attempt $attempt/$MaxAttempts)"
                    }
                }
            }

            & git @gitArgs 2>$null
            if ($LASTEXITCODE -eq 0) {
                return 0
            }

            $lockExists = $IndexLockPath -and (Test-Path -LiteralPath $IndexLockPath)
            if ($LASTEXITCODE -eq 128 -and $attempt -lt $MaxAttempts) {
                # Exponential backoff with jitter, capped at MaxDelayMilliseconds
                $baseDelay = [Math]::Min($InitialDelayMilliseconds * [Math]::Pow(1.4, $attempt - 1), $MaxDelayMilliseconds)
                $jitter = Get-Random -Minimum 0 -Maximum ([int]($baseDelay * 0.4))
                $delay = [int]$baseDelay + $jitter
                if (-not $Quiet) {
                    Write-Warning "git add failed (exit code 128), retrying in ${delay}ms (attempt $attempt/$MaxAttempts)..."
                }
                Start-Sleep -Milliseconds $delay
                continue
            }

            return $LASTEXITCODE
        }

        return 128
    } finally {
        if ($mutexAcquired -and $null -ne $mutex) {
            try {
                $mutex.ReleaseMutex()
            } catch {
                # Ignore errors releasing mutex
            }
        }
    }
}

# Convenience function for scripts that modify files and need to re-stage them.
# Use this instead of raw `git add` calls to ensure proper lock handling.
function Invoke-GitAddSingleFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [string]$IndexLockPath
    )

    return Invoke-GitAddWithRetry -Items @($FilePath) -IndexLockPath $IndexLockPath -Quiet
}

