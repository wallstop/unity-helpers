# Skill: Git-Safe Operations in Scripts

## Purpose

Ensure all scripts and hooks that interact with the git index use proper locking,
retries, and coordination to prevent `index.lock` errors during concurrent operations.

## When to Use

- Creating or modifying any script that runs `git add`, `git reset`, or similar index commands
- Writing pre-commit, post-commit, or other git hooks
- Building automation that stages files after modification
- Any code that modifies files and re-stages them (formatters, linters with auto-fix)

---

## Background: The `index.lock` Problem

Git creates an `index.lock` file in `.git/` during any operation that modifies the index
(staging area). If a second git operation tries to run while the lock exists, it fails:

```text
fatal: Unable to create '/path/to/repo/.git/index.lock': File exists.
```

This commonly happens when:

1. **Multiple hooks run concurrently** — even with `require_serial: true` in pre-commit
2. **Interactive git tools (lazygit, GitKraken)** perform rapid operations
3. **Auto-fix scripts** call `git add` multiple times in quick succession
4. **File watchers** trigger git operations on save

---

## Required Pattern: Use `git-staging-helpers.ps1`

**All PowerShell scripts that interact with git MUST use the shared helpers.**

### 1. Load the Helpers

```powershell
$helpersPath = Join-Path -Path $PSScriptRoot -ChildPath 'git-staging-helpers.ps1'
. $helpersPath
```

### 2. Get Repository Info (for IndexLockPath)

```powershell
try {
    Assert-GitAvailable | Out-Null
    $repositoryInfo = Get-GitRepositoryInfo
} catch {
    Write-Error $_.Exception.Message
    exit 1
}
```

### 3. Stage Files with Retry

```powershell
# Single file
Invoke-GitAddWithRetry -Items @($filePath) -IndexLockPath $repositoryInfo.IndexLockPath

# Multiple files
Invoke-GitAddWithRetry -Items $filePaths -IndexLockPath $repositoryInfo.IndexLockPath

# Quiet mode (suppress warnings in loops)
Invoke-GitAddWithRetry -Items @($filePath) -IndexLockPath $repositoryInfo.IndexLockPath -Quiet
```

---

## Helper Functions Reference

### `Assert-GitAvailable`

Verifies git is on PATH. Throws if not found.

### `Get-GitRepositoryInfo`

Returns an object with:

- `Directory` — Path to `.git` directory
- `IndexLockPath` — Full path to `.git/index.lock`
- `RepositoryRoot` — Root of the repository

### `Wait-ForGitIndexLock`

Polls until `index.lock` is released or timeout (default 30s).

```powershell
Wait-ForGitIndexLock -IndexLockPath $repositoryInfo.IndexLockPath -MaxWaitMilliseconds 5000
```

### `Invoke-GitAddWithRetry`

**Primary function for staging files.** Features:

- Acquires a global mutex to coordinate across processes
- Waits for existing lock before attempting
- Exponential backoff with jitter on failure
- Up to 30 retry attempts by default

Parameters:

| Parameter                  | Type     | Default | Description                    |
| -------------------------- | -------- | ------- | ------------------------------ |
| `Items`                    | string[] | —       | Files to stage                 |
| `IndexLockPath`            | string   | —       | Path to `.git/index.lock`      |
| `MaxAttempts`              | int      | 30      | Maximum retry attempts         |
| `InitialDelayMilliseconds` | int      | 50      | Starting delay between retries |
| `MaxDelayMilliseconds`     | int      | 3000    | Maximum delay cap              |
| `Quiet`                    | switch   | false   | Suppress warning messages      |

### `Invoke-GitAddSingleFile`

Convenience wrapper for staging a single file quietly:

```powershell
Invoke-GitAddSingleFile -FilePath $path -IndexLockPath $repositoryInfo.IndexLockPath
```

### `Get-StagedPathsForGlobs`

Get staged files matching glob patterns:

```powershell
$paths = Get-StagedPathsForGlobs -Globs @('*.cs', '*.md')
```

### `Get-ExistingPaths`

Filter a list to only paths that exist on disk:

```powershell
$existing = Get-ExistingPaths -Candidates $paths
```

---

## Forbidden Patterns

### ❌ Never Use Raw `git add`

```powershell
# BAD - will fail under concurrent access
git add $filePath
& git add -- $files
```

### ❌ Never Ignore Exit Codes

```powershell
# BAD - hides failures
git add $file 2>$null
```

### ❌ Never Use `git add` in a Loop Without Batching

```powershell
# BAD - many separate git operations
foreach ($file in $files) {
    git add $file  # Race condition on each iteration
}
```

### ✅ Correct Pattern

```powershell
# GOOD - single batched operation with retries
Invoke-GitAddWithRetry -Items $files -IndexLockPath $repositoryInfo.IndexLockPath
```

---

## Bash/Shell Scripts

For bash scripts, use similar retry logic:

```bash
#!/bin/bash
set -e

GIT_DIR=$(git rev-parse --git-dir)
INDEX_LOCK="$GIT_DIR/index.lock"
MAX_ATTEMPTS=30
BASE_DELAY_MS=50

git_add_with_retry() {
    local files=("$@")
    local attempt=1
    local delay_ms=$BASE_DELAY_MS

    while [ $attempt -le $MAX_ATTEMPTS ]; do
        # Wait for lock (poll every 50ms)
        local wait_count=0
        while [ -f "$INDEX_LOCK" ] && [ $wait_count -lt 100 ]; do
            sleep 0.05
            ((wait_count++))
        done

        if git add -- "${files[@]}" 2>/dev/null; then
            return 0
        fi

        # Exponential backoff: multiply by 1.4 each attempt, cap at 3000ms
        # Use integer math: delay = delay * 14 / 10
        delay_ms=$((delay_ms * 14 / 10))
        [ $delay_ms -gt 3000 ] && delay_ms=3000

        # Add jitter (0-30% of delay)
        local jitter=$((RANDOM % (delay_ms / 3 + 1)))
        local total_delay_ms=$((delay_ms + jitter))

        # Convert milliseconds to fractional seconds for sleep
        # e.g., 150ms -> 0.150
        local sleep_secs
        sleep_secs=$(printf "0.%03d" $total_delay_ms)
        [ $total_delay_ms -ge 1000 ] && sleep_secs=$(awk "BEGIN {printf \"%.3f\", $total_delay_ms/1000}")

        sleep "$sleep_secs"
        ((attempt++))
    done

    return 128
}

# Usage
git_add_with_retry file1.txt file2.txt
```

---

## Pre-Commit Hook Configuration

Ensure hooks that modify and re-stage files use `require_serial: true`:

```yaml
# .pre-commit-config.yaml
repos:
  - repo: local
    hooks:
      - id: my-formatter
        name: Format and stage
        entry: pwsh -NoProfile -File scripts/my-formatter.ps1
        language: system
        require_serial: true # CRITICAL: prevents parallel execution
        pass_filenames: false
```

---

## Checklist for New Git-Interacting Scripts

1. [ ] Load `git-staging-helpers.ps1` at the top
2. [ ] Call `Assert-GitAvailable` and `Get-GitRepositoryInfo`
3. [ ] Use `Invoke-GitAddWithRetry` for ALL staging operations
4. [ ] Batch files together in a single `Invoke-GitAddWithRetry` call when possible
5. [ ] Add `require_serial: true` to `.pre-commit-config.yaml` if the hook stages files
6. [ ] Test with lazygit to verify no `index.lock` errors under rapid operations

---

## Debugging Lock Issues

If you still see `index.lock` errors:

1. **Check for stale locks**: `ls -la .git/index.lock` — if present when no git operation running, delete it
2. **Increase timeouts**: Adjust `MaxWaitMilliseconds` and `MaxAttempts` parameters
3. **Check for external tools**: IDE git integrations, file watchers, etc.
4. **Verify `require_serial`**: Ensure it's set for hooks that call `git add`
5. **Check mutex acquisition**: Add logging to verify the global mutex is being acquired

---

## Related Files

- [scripts/git-staging-helpers.ps1](../../scripts/git-staging-helpers.ps1) — Shared helper module
- [.pre-commit-config.yaml](../../.pre-commit-config.yaml) — Hook configuration
- [scripts/format-staged-csharp.ps1](../../scripts/format-staged-csharp.ps1) — Example formatter script
- [scripts/lint-csharp-naming.ps1](../../scripts/lint-csharp-naming.ps1) — Example linter with auto-fix
