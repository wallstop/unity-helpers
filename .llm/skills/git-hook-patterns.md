# Skill: Git Hook Patterns

<!-- trigger: git hook, pre-commit, hook safety, hook patterns, hook permissions | Pre-commit hook safety and configuration | Core -->

## Purpose

Patterns and best practices for writing safe, reliable git hooks that interact with
the git index without causing `index.lock` errors or corruption.

## When to Use This Skill

- Writing or modifying pre-commit, post-commit, or other git hooks
- Creating hooks that validate staged files
- Building hooks that auto-fix and re-stage files
- Debugging `index.lock` errors in hook execution
- Configuring pre-commit framework for safe parallel execution

---

## Critical: Wait for External Tools at Hook Start

**CRITICAL**: At the start of any git hook, wait for external tools (lazygit, IDE, etc.)
to release the index.lock before performing any operations:

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

---

## Git Hook Regex Patterns (CRITICAL)

> **CRITICAL**: Regex patterns in bash git hooks require SINGLE backslashes.
> Double escaping causes patterns to match NOTHING silently!

```bash
# CORRECT - Single backslash
git diff --cached --name-only | grep -E '\.(md|markdown)$'

# WRONG - Double-escaped (matches nothing!)
git diff --cached --name-only | grep -E '\\.(md|markdown)$'
```

**ALWAYS test patterns manually after modification:**

```bash
# Test that pattern matches expected files
echo "test.md" | grep -E '\.(md|markdown)$'  # Should output: test.md
```

See [validate-before-commit](./validate-before-commit.md#git-hook-regex-pattern-testing-critical)
for full testing checklist.

---

## Capture State Early

```bash
#!/bin/bash
# Capture staged files ONCE at the start
STAGED_FILES=$(git diff --cached --name-only --diff-filter=ACMR)

# Process files without re-querying git
for file in $STAGED_FILES; do
  # validate file
done
```

---

## Validation vs Modification

### Validation-Only Hooks (Recommended)

Pre-commit hooks should ideally only validate, never modify. If validation fails,
exit non-zero and let the user fix issues.

```bash
# GOOD: Report issues and fail
npm run format:check
if [ $? -ne 0 ]; then
  echo "Run 'npm run format' to fix formatting"
  exit 1
fi
```

### Auto-Fix Hooks (Use with Caution)

If you must auto-fix and re-stage, use the staging helpers:

```bash
# BAD: Auto-fixing without helpers
npm run format
git add .  # DON'T DO THIS - race conditions!

# GOOD: Auto-fixing with helpers
source "$SCRIPT_DIR/git-staging-helpers.sh"
ensure_no_index_lock
npm run format
git_add_with_retry $modified_files
```

---

## Handle Partial Staging

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

## Pre-Commit Framework Configuration

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

### Why `require_serial: true` Matters

Even with `require_serial: true` in pre-commit, external tools like lazygit may still
hold the lock when hooks begin. The `ensure_no_index_lock` function handles this case.

---

## Safe Patterns for Hook Scripts

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

## CLI Argument Safety: End-of-Options Separator

When passing staged file lists to CLI tools in hooks, ALWAYS place `--` between options and file arguments:

```bash
# WRONG - filenames can be interpreted as options
npx --no-install prettier --write "${FILES[@]}"

# CORRECT - `--` prevents option injection from filenames
npx --no-install prettier --write -- "${FILES[@]}"
markdownlint --fix --config .markdownlint.json -- "${FILES[@]}"
yamllint -c .yamllint.yaml -- "${FILES[@]}"
```

**Why**: A staged filename like `--plugin=./evil.js` would be parsed as a CLI flag without `--`. This is an option injection vulnerability.

### File-Reading Commands Also Need `--`

Commands that read file contents (`tail`, `head`, `cat`, `od`, etc.) are also vulnerable to option injection and must use `--`:

```bash
# WRONG - filename starting with '-' is interpreted as option
tail -n 1 "$file"
head -c 10 "$file"
cat "$file"
od -c "$file"

# CORRECT - `--` ensures filename is never parsed as an option
tail -n 1 -- "$file"
head -c 10 -- "$file"
cat -- "$file"
od -c -- "$file"
```

**Why**: A file named `-n` or `--help` would cause unexpected behavior or errors without `--`.

---

## CRLF-Aware Newline Handling

When fixing missing final newlines in hook scripts, you MUST detect the file's existing line ending style before appending. Blindly appending `\n` (LF) to a CRLF file creates mixed line endings that fail `eol:check`.

### Detecting Line Endings Before Appending

```bash
# WRONG - Always appends LF, breaks CRLF files
if [ "$(tail -c 1 -- "$file" | od -An -tx1 | tr -d ' ')" != "0a" ]; then
    printf '\n' >> "$file"
fi

# CORRECT - Detect existing line endings and match them
if [ "$(tail -c 1 -- "$file" | od -An -tx1 | tr -d ' ')" != "0a" ]; then
    # Check if file uses CRLF by looking for carriage return characters
    if grep -q $'\r' "$file" 2>/dev/null; then
        printf '\r\n' >> "$file"  # CRLF file - append CRLF
    else
        printf '\n' >> "$file"    # LF file - append LF
    fi
fi
```

### Why This Matters

1. Windows-style files use CRLF (`\r\n`) line endings
2. Unix-style files use LF (`\n`) line endings
3. Appending LF to a CRLF file creates a "mixed" file with both styles
4. The `eol:check` validation fails on mixed line endings
5. This is a common cause of CI failures after "fixing" final newlines

### PowerShell Equivalent

```powershell
# WRONG - -NoNewline then manual newline can create mixed endings
$content = Get-Content -Raw $file
# ... process content ...
Set-Content -NoNewline -Path $file -Value $content
Add-Content -Path $file -Value ""  # Adds OS-default line ending

# CORRECT - Detect and preserve existing line endings
$content = Get-Content -Raw $file
$useCRLF = $content -match "`r`n"
# ... process content ...
$newline = if ($useCRLF) { "`r`n" } else { "`n" }
[System.IO.File]::WriteAllText($file, $content + $newline)
```

---

## Error Handling in Hooks

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

## Debugging Hook Lock Issues

If you still see `index.lock` errors:

1. **Enable verbose logging**: `export GIT_STAGING_VERBOSE=1` before running git commands
2. **Check for stale locks**: `ls -la .git/index.lock` - if present when no git operation running, delete it
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

The verbose output will show exactly when locks are detected, how long waits take,
and what git operations are attempted.

---

## CI/CD Environments

GitHub Actions and other CI environments generally **do not require** the retry helpers because:

1. **Single-process execution** - Only one git operation runs at a time
2. **No interactive tools** - No lazygit, GitKraken, or IDE integrations competing
3. **Ephemeral environments** - Fresh container per run, no stale locks

However, for consistency, you may still use the helpers in CI scripts. The overhead is negligible.

---

## Hook File Permissions (Critical)

Git silently skips hook files that are not executable. If `.githooks/*` files are tracked with `100644` (non-executable) permissions, hooks will never run and all pre-commit validation is bypassed.

- The `hooks:install` script sets executable permissions automatically
- If hooks are not running, check permissions: `git ls-files -s .githooks/`
- Fix tracked permissions: `git update-index --chmod=+x .githooks/pre-commit .githooks/pre-push`
- Fix local permissions: `chmod +x .githooks/*`
- Use `npm run validate:hook-perms` to verify hook file permissions

---

## Hook Template

Here's a complete template for a safe pre-commit hook:

```bash
#!/usr/bin/env bash
set -e

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# Source helpers
source "$SCRIPT_DIR/../scripts/git-staging-helpers.sh"

# Wait for external tools to release lock
ensure_no_index_lock || {
    echo "Warning: index.lock still held after waiting." >&2
}

# Capture staged files ONCE
STAGED_FILES=$(git diff --cached --name-only --diff-filter=ACMR)

# Exit early if no files
if [ -z "$STAGED_FILES" ]; then
    exit 0
fi

# Filter for specific file types (SINGLE backslash!)
CS_FILES=$(echo "$STAGED_FILES" | grep -E '\.(cs)$' || true)

if [ -n "$CS_FILES" ]; then
    # Run validation
    echo "Validating C# files..."
    # your validation here

    # If auto-fixing and re-staging:
    # git_add_with_retry $CS_FILES
fi

exit 0
```

---

## Hook Description Accuracy

**CRITICAL**: When adding or removing script invocations from hooks, ALWAYS update
the corresponding comments/descriptions in the hook file AND in related documentation
(skill files, README, etc.). Stale descriptions mislead developers and AI agents.

### Checklist for Hook Changes

1. Update the step comment in the hook file itself
2. Update [formatting-and-linting](./formatting-and-linting.md) "What the Hook Does" list
3. Update any other skill files that describe the hook steps
4. Verify the hook description matches all script calls in that step

### Current Pre-Commit Step 0: Version Syncing

Step 0 runs two PowerShell scripts on every commit:

- [sync-banner-version.ps1](../../scripts/sync-banner-version.ps1) — Syncs banner SVG + [LLM context](../context.md) from `package.json`
- [sync-issue-template-versions.ps1](../../scripts/sync-issue-template-versions.ps1) — Syncs issue template dropdowns from `package.json`, the [CHANGELOG](../../CHANGELOG.md), and git tags

Both scripts auto-stage modified files.

---

## Related Skills

- [git-safe-operations](./git-safe-operations.md) - Core git safety patterns and critical rules
- [git-staging-helpers](./git-staging-helpers.md) - PowerShell/Bash helper functions reference
- [validate-before-commit](./validate-before-commit.md) - Pre-commit validation commands
- [formatting](./formatting.md) - CSharpier, Prettier, markdownlint workflow

## Related Files

- [.githooks/pre-commit](../../.githooks/pre-commit) - Local pre-commit hook (uses helpers)
- [.pre-commit-config.yaml](../../.pre-commit-config.yaml) - Pre-commit framework configuration
- [scripts/format-staged-csharp.ps1](../../scripts/format-staged-csharp.ps1) - Example formatter script
- [scripts/lint-csharp-naming.ps1](../../scripts/lint-csharp-naming.ps1) - Example linter with auto-fix
- [scripts/sync-banner-version.ps1](../../scripts/sync-banner-version.ps1) - Banner and [LLM context](../context.md) version sync
- [scripts/sync-issue-template-versions.ps1](../../scripts/sync-issue-template-versions.ps1) - Issue template version sync
