# Shared helpers for git-aware formatter/linter scripts to avoid duplicated logic.
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

    return [pscustomobject]@{
        Directory     = $resolvedGitDir
        IndexLockPath = (Join-Path -Path $resolvedGitDir -ChildPath 'index.lock')
    }
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
        [int]$MaxAttempts = 10,
        [int]$InitialDelayMilliseconds = 200
    )

    if (-not $Items -or $Items.Count -eq 0) {
        return 0
    }

    $gitArgs = @('add', '--')
    $gitArgs += $Items

    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        & git @gitArgs
        if ($LASTEXITCODE -eq 0) {
            return 0
        }

        $lockExists = $IndexLockPath -and (Test-Path -LiteralPath $IndexLockPath)
        if ($LASTEXITCODE -eq 128 -and $lockExists -and $attempt -lt $MaxAttempts) {
            Start-Sleep -Milliseconds ($InitialDelayMilliseconds * $attempt)
            continue
        }

        return $LASTEXITCODE
    }

    return 128
}
