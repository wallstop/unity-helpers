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
5. **Pre-commit hooks start before external tools finish** — lazygit may still hold the lock when hooks begin

---

## Critical Rules

### 1. Never Parallel Git Operations

Git's index file (`/.git/index`) doesn't support concurrent access. Running multiple git commands in parallel can corrupt the index.

```bash
# BAD: Parallel git operations
git status & git diff &

# GOOD: Sequential operations
git status
git diff
```

### 2. Use Porcelain Output for Parsing

Use `--porcelain` flags when parsing git output programmatically. Human-readable output changes between git versions.

```bash
# BAD: Parsing human-readable output (also: never use grep, use rg)
git status | grep "modified"

# GOOD: Porcelain format (machine-parseable)
git status --porcelain
git diff --name-status
```

### 3. Lock-Aware Operations

For long-running scripts that may conflict with IDE git integrations:

```bash
# Wait for index lock
while [ -f ".git/index.lock" ]; do
  sleep 0.1
done
```

### 4. Wait for External Tools at Hook Start

**CRITICAL**: At the start of any git hook, wait for external tools (lazygit, IDE, etc.) to release the index.lock before performing any operations:

```bash
# Bash: At the start of your hook
source "$SCRIPT_DIR/scripts/git-staging-helpers.sh"
ensure_no_index_lock || {
    echo "Warning: index.lock still held. Proceeding anyway, but operations may fail." >&2
}
```

```powershell
# PowerShell: At the start of your script
. $PSScriptRoot/git-staging-helpers.ps1
if (-not (Invoke-EnsureNoIndexLock)) {
    Write-Warning "index.lock still held after waiting. Proceeding anyway."
}
```

### 5. Agent-Specific: No Staging or Committing

**AI agents must NEVER stage or commit changes.** All git state modifications are the user's responsibility.

✅ Allowed commands:

- `git status`
- `git log`
- `git diff`
- `git show`
- `git blame`
- `git ls-files`

❌ Forbidden commands:

- `git add`
- `git commit`
- `git push`
- `git reset`
- `git checkout` (for file modifications)
- `git stash`
- `git merge`
- `git rebase`

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

### `Invoke-EnsureNoIndexLock`

**Call at the start of hooks/scripts.** Waits for external tools (lazygit, IDE) to release the lock before proceeding.

```powershell
# At the start of your script, after loading helpers
if (-not (Invoke-EnsureNoIndexLock)) {
    Write-Warning "index.lock still held after waiting."
}
```

Parameters:

| Parameter                  | Type | Default | Description       |
| -------------------------- | ---- | ------- | ----------------- |
| `MaxWaitMilliseconds`      | int  | 10000   | Maximum wait time |
| `PollIntervalMilliseconds` | int  | 50      | Poll frequency    |

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

**All bash scripts that interact with git MUST use the shared helpers.**

### 1. Source the Helpers

```bash
#!/usr/bin/env bash
set -e

# Source the shared git helpers
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/git-staging-helpers.sh"
```

### 2. Stage Files with Retry

```bash
# Single file
git_add_with_retry "path/to/file.cs"

# Multiple files
git_add_with_retry file1.txt file2.txt file3.md

# From a list (word-split)
FILES="file1.txt file2.txt"
git_add_with_retry $FILES
```

### 3. Helper Functions Reference (Bash)

| Function                     | Description                                                       |
| ---------------------------- | ----------------------------------------------------------------- |
| `assert_git_available`       | Verify git is on PATH                                             |
| `get_git_dir`                | Get `.git` directory path (cached)                                |
| `get_index_lock_path`        | Get path to `.git/index.lock`                                     |
| `get_repository_root`        | Get repository root path                                          |
| `wait_for_git_index_lock`    | Poll until lock released (default 30s)                            |
| `ensure_no_index_lock`       | **Call at hook start** — waits for external tools to release lock |
| `git_add_with_retry`         | **Primary staging function** with exponential backoff and flock   |
| `git_add_single_file`        | Convenience wrapper for single file                               |
| `get_staged_paths_for_globs` | Get staged files matching patterns                                |
| `get_existing_paths`         | Filter stdin to existing paths                                    |

### `ensure_no_index_lock` (Bash)

**Call at the start of hooks/scripts.** Waits for external tools (lazygit, IDE) to release the lock.

```bash
# At the start of your hook, after sourcing helpers
ensure_no_index_lock || {
    echo "Warning: index.lock still held after waiting." >&2
}
```

Arguments:

| Argument | Default | Description            |
| -------- | ------- | ---------------------- |
| `$1`     | 10000   | Maximum wait time (ms) |
| `$2`     | 50      | Poll interval (ms)     |

### 4. Retry Algorithm (Both PowerShell and Bash)

Both implementations use identical retry parameters:

- **Max attempts**: 30
- **Initial delay**: 50ms
- **Max delay cap**: 3000ms
- **Backoff multiplier**: 1.4x per attempt
- **Jitter**: 0-40% of base delay
- **Lock wait**: 5 seconds before each attempt
- **Initial wait** (at hook start): 10 seconds (via `ensure_no_index_lock`)
- **Cross-process coordination**: Named mutex (PowerShell) / flock (Bash)

---

## Environment Variables

Configure the git staging helpers via environment variables:

| Variable                    | Default                               | Description                                |
| --------------------------- | ------------------------------------- | ------------------------------------------ |
| `GIT_STAGING_VERBOSE`       | `0`                                   | Set to `1` to enable verbose debug logging |
| `GIT_LOCK_MAX_ATTEMPTS`     | `30`                                  | Maximum retry attempts                     |
| `GIT_LOCK_INITIAL_DELAY_MS` | `50`                                  | Initial backoff delay (ms)                 |
| `GIT_LOCK_MAX_DELAY_MS`     | `3000`                                | Maximum backoff delay cap (ms)             |
| `GIT_LOCK_WAIT_TIMEOUT_MS`  | `30000`                               | Max wait for lock per attempt (ms)         |
| `GIT_LOCK_POLL_INTERVAL_MS` | `50`                                  | Lock polling frequency (ms)                |
| `GIT_LOCK_INITIAL_WAIT_MS`  | `10000`                               | Initial wait at hook start (ms)            |
| `GIT_HELPERS_LOCK_FILE`     | `/tmp/unity-helpers-git-staging.lock` | Bash flock file path                       |

### Enabling Verbose Debug Logging

When troubleshooting index.lock issues, enable verbose logging:

```bash
# Bash
export GIT_STAGING_VERBOSE=1
git commit -m "test"
```

```powershell
# PowerShell
$env:GIT_STAGING_VERBOSE = "1"
git commit -m "test"
```

Verbose output includes:

- Lock presence checks
- Wait times and progress
- Retry attempts and delays
- Mutex/flock acquisition status
- Actual git stderr on failures

---

## Pre-Commit Hook Safety

When writing pre-commit hooks:

### Git Hook Regex Patterns (CRITICAL)

> **🚨 CRITICAL**: Regex patterns in bash git hooks require SINGLE backslashes. Double escaping causes patterns to match NOTHING silently!

```bash
# ✅ CORRECT - Single backslash
git diff --cached --name-only | grep -E '\.(md|markdown)$'

# ❌ WRONG - Double-escaped (matches nothing!)
git diff --cached --name-only | grep -E '\\.(md|markdown)$'
```

**ALWAYS test patterns manually after modification:**

```bash
# Test that pattern matches expected files
echo "test.md" | grep -E '\.(md|markdown)$'  # Should output: test.md
```

See [validate-before-commit](./validate-before-commit.md#git-hook-regex-pattern-testing-critical) for full testing checklist.

### Capture State Early

```bash
#!/bin/bash
# Capture staged files ONCE at the start
STAGED_FILES=$(git diff --cached --name-only --diff-filter=ACMR)

# Process files without re-querying git
for file in $STAGED_FILES; do
  # validate file
done
```

### Don't Modify Index in Hooks

Pre-commit hooks should only validate, never modify. If validation fails, exit non-zero and let the user fix issues.

```bash
# BAD: Auto-fixing in hook
npm run format
git add .  # DON'T DO THIS

# GOOD: Report issues and fail
npm run format:check
if [ $? -ne 0 ]; then
  echo "Run 'npm run format' to fix formatting"
  exit 1
fi
```

### Handle Partial Staging

Users may stage only part of a file. Validate against staged content, not working directory:

```bash
# Get staged content of a file
git show :path/to/file.cs

# Or use git stash for complex validations
git stash --keep-index --include-untracked
# run validation
git stash pop
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

## Safe Patterns for Scripts

### Checking for Changes

```bash
# Check if working tree is clean
if git diff --quiet && git diff --cached --quiet; then
  echo "No changes"
fi

# Check for specific file changes (use rg, not grep)
if git diff --name-only | rg -q "\.cs$"; then
  echo "C# files modified"
fi
```

### Getting File Lists

```bash
# All tracked files
git ls-files

# Modified files (unstaged)
git diff --name-only

# Staged files
git diff --cached --name-only

# Untracked files
git ls-files --others --exclude-standard
```

### Comparing Versions

```bash
# Changes since last commit
git diff HEAD

# Changes between branches
git diff main..feature-branch --name-only

# Changes in specific directory
git diff --name-only -- "path/to/dir/"
```

---

## Error Handling

Always handle git command failures:

```bash
# Check if in git repository
if ! git rev-parse --git-dir > /dev/null 2>&1; then
  echo "Not a git repository"
  exit 1
fi

# Handle command failure
if ! git status --porcelain > /tmp/status.txt; then
  echo "Git status failed"
  exit 1
fi
```

---

## Debugging Lock Issues

If you still see `index.lock` errors:

1. **Enable verbose logging**: `export GIT_STAGING_VERBOSE=1` before running git commands
2. **Check for stale locks**: `ls -la .git/index.lock` — if present when no git operation running, delete it
3. **Increase timeouts**: Set `GIT_LOCK_INITIAL_WAIT_MS=20000` for slower tools
4. **Check for external tools**: IDE git integrations, file watchers, etc.
5. **Verify `require_serial`**: Ensure it's set for hooks that call `git add`
6. **Check `ensure_no_index_lock`**: Verify hooks call this at the start
7. **Check flock availability**: On non-Linux systems, cross-process coordination may be limited

### Example Debug Session

```bash
# Enable verbose logging
export GIT_STAGING_VERBOSE=1

# Increase initial wait to 20 seconds (useful for slow external tools)
export GIT_LOCK_INITIAL_WAIT_MS=20000

# Now commit and watch the verbose output
git commit -m "test commit"
```

The verbose output will show exactly when locks are detected, how long waits take, and what git operations are attempted.

---

## CI/CD Environments

GitHub Actions and other CI environments generally **do not require** the retry helpers because:

1. **Single-process execution** — Only one git operation runs at a time
2. **No interactive tools** — No lazygit, GitKraken, or IDE integrations competing
3. **Ephemeral environments** — Fresh container per run, no stale locks

However, for consistency, you may still use the helpers in CI scripts. The overhead is negligible.

---

## Checklist for Modifying Git-Interacting Scripts

### PowerShell Scripts

1. [ ] Load `git-staging-helpers.ps1` at the top
2. [ ] Call `Assert-GitAvailable` and `Get-GitRepositoryInfo`
3. [ ] **Call `Invoke-EnsureNoIndexLock` immediately after getting repository info**
4. [ ] Use `Invoke-GitAddWithRetry` for ALL staging operations
5. [ ] Batch files together in a single call when possible
6. [ ] Add `require_serial: true` to `.pre-commit-config.yaml` if the hook stages files

### Bash Scripts

1. [ ] Source `git-staging-helpers.sh` at the top
2. [ ] **Call `ensure_no_index_lock` at the start (before any git operations)**
3. [ ] Use `git_add_with_retry` for ALL staging operations
4. [ ] Batch files together in a single call when possible
5. [ ] Test with lazygit to verify no `index.lock` errors
6. [ ] Set `GIT_STAGING_VERBOSE=1` during testing to verify lock handling

---

## Related Skills

- [validate-before-commit](./validate-before-commit.md) — Pre-commit validation commands
- [format-code](./format-code.md) — Code formatting before commits

## Related Files

- [scripts/git-staging-helpers.ps1](../../scripts/git-staging-helpers.ps1) — PowerShell shared helper module
- [scripts/git-staging-helpers.sh](../../scripts/git-staging-helpers.sh) — Bash shared helper module
- [.githooks/pre-commit](../../.githooks/pre-commit) — Local pre-commit hook (uses helpers)
- [.pre-commit-config.yaml](../../.pre-commit-config.yaml) — Pre-commit framework configuration
- [scripts/format-staged-csharp.ps1](../../scripts/format-staged-csharp.ps1) — Example formatter script
- [scripts/lint-csharp-naming.ps1](../../scripts/lint-csharp-naming.ps1) — Example linter with auto-fix
