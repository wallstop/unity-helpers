# GitHub Actions Shell Scripting

<!-- trigger: workflow shell, actions bash, gh api, runner temp, heredoc | Shell scripting best practices for GitHub Actions | Core -->

## Summary

When shell scripting is unavoidable in GitHub Actions workflows, follow these patterns to prevent common pitfalls: whitespace corruption, silent failures, race conditions, and security issues. Prefer extracting logic to testable scripts per [github-actions-script-pattern](./github-actions-script-pattern.md), but when inline shell is necessary, apply these defensive patterns.

## When to Use

- Writing `run:` steps in GitHub Actions workflows
- Using `gh api` or `curl` for GitHub API calls
- Processing output from commands with AWK/sed/grep
- Polling for async operations to complete
- Working with temporary files in CI environments
- Multi-line string handling in YAML

## Patterns

### Pattern 1: Always Use Strict Mode

Every shell script block must start with strict mode to catch errors early.

```bash
# ❌ BAD: No error handling - failures silently ignored
run: |
  some_command
  another_command  # Runs even if some_command failed

# ✅ GOOD: Strict mode catches all errors
run: |
  set -euo pipefail
  some_command
  another_command  # Only runs if some_command succeeded
```

**Flags explained:**

- `-e`: Exit immediately on any command failure
- `-u`: Error on undefined variables
- `-o pipefail`: Pipeline fails if any command in pipe fails

### Pattern 2: Use $RUNNER_TEMP for Temporary Files

GitHub Actions provides `$RUNNER_TEMP` which is cleaned up automatically and isolated per job.

```bash
# ❌ BAD: /tmp is shared, may have stale files, not cleaned
run: |
  echo "$CONTENT" > /tmp/body.md
  gh api ... -F body=@/tmp/body.md

# ✅ GOOD: $RUNNER_TEMP is job-isolated and auto-cleaned
run: |
  set -euo pipefail
  BODY_FILE="${RUNNER_TEMP}/pr-body.md"
  echo "$CONTENT" > "$BODY_FILE"
  gh api ... -F body=@"$BODY_FILE"
```

### Pattern 3: File-Based API Bodies Instead of Inline

Multiline strings with `-f` parameters corrupt whitespace. Use file-based `-F field=@file` instead.

```bash
# ❌ BAD: Inline heredoc mangles whitespace and newlines
run: |
  gh api repos/$REPO/pulls -X POST \
    -f title="$TITLE" \
    -f body="$(cat <<'EOF'
  ## Summary
  This PR does things.

  ## Changes
  - Item 1
  - Item 2
  EOF
  )"

# ✅ GOOD: Write to file, then use -F with @file syntax
run: |
  set -euo pipefail
  BODY_FILE="${RUNNER_TEMP}/pr-body.md"
  cat > "$BODY_FILE" << 'EOF'
  ## Summary
  This PR does things.

  ## Changes
  - Item 1
  - Item 2
  EOF
  gh api repos/${{ github.repository }}/pulls -X POST \
    -f title="$TITLE" \
    -F body=@"$BODY_FILE"
```

### Pattern 4: Heredoc Indentation Control

Use `<<-` with tabs for indented heredocs, or unindent the content entirely.

```bash
# ❌ BAD: Spaces in heredoc become part of content
run: |
  if true; then
    cat << EOF
      This line has 6 leading spaces in the output!
    EOF
  fi

# ✅ GOOD: Unindent heredoc content to column 0
run: |
  set -euo pipefail
  if true; then
    # Heredoc content starts at column 0 to avoid whitespace issues
  cat << 'EOF'
  No indentation issues here.
  Content starts at column 0.
  EOF
  fi
```

**Notes:**

- Quote the delimiter (`'EOF'`) to prevent variable expansion when you want literal content
- The `<<-` operator strips leading tabs (not spaces) but requires literal tab characters

### Pattern 5: Polling with Exponential Backoff

Async operations (merge queues, deployments, checks) need proper polling with timeouts.

```bash
# ❌ BAD: No timeout, fixed delay, silent failures
run: |
  while true; do
    STATUS=$(gh api ...)
    if [ "$STATUS" = "completed" ]; then break; fi
    sleep 10
  done

# ✅ GOOD: Timeout, exponential backoff, clear error messages
run: |
  set -euo pipefail
  MAX_ATTEMPTS=10
  DELAY=5

  for attempt in $(seq 1 $MAX_ATTEMPTS); do
    echo "Attempt $attempt/$MAX_ATTEMPTS: Checking status..."

    STATUS=$(gh api repos/${{ github.repository }}/actions/runs/$RUN_ID \
      --jq '.status' 2>/dev/null || echo "error")

    if [ "$STATUS" = "completed" ]; then
      echo "Operation completed successfully"
      exit 0
    fi

    if [ "$attempt" -eq "$MAX_ATTEMPTS" ]; then
      echo "::error::Timeout after $MAX_ATTEMPTS attempts"
      exit 1
    fi

    echo "Status: $STATUS. Waiting ${DELAY}s..."
    sleep "$DELAY"
    DELAY=$((DELAY * 2))  # Exponential backoff
  done
```

### Pattern 6: AWK/sed Exact Field Matching

Partial matches cause false positives. Use field delimiters and exact patterns.

```bash
# ❌ BAD: Partial match - "feature-test" also matches "test"
run: |
  echo "$BRANCHES" | grep "test"

# ❌ BAD: AWK partial match on field
run: |
  echo "$OUTPUT" | awk '/test/ {print $2}'

# ✅ GOOD: Exact word boundary matching with grep
run: |
  set -euo pipefail
  echo "$BRANCHES" | grep -w "test"        # Word boundary
  echo "$BRANCHES" | grep "^test$"         # Exact line match
  echo "$BRANCHES" | grep -F "test"        # Fixed string (no regex)

# ✅ GOOD: AWK exact field comparison
run: |
  set -euo pipefail
  echo "$OUTPUT" | awk '$1 == "test" {print $2}'           # Exact field match
  echo "$OUTPUT" | awk -F'\t' '$1 == "test" {print $2}'    # With delimiter
```

### Pattern 7: Validate Commands and Files Exist

Check prerequisites before operations to provide clear error messages.

```bash
# ❌ BAD: Cryptic error if file missing
run: |
  cat config.json | jq '.version'

# ✅ GOOD: Validate before use with clear errors
run: |
  set -euo pipefail

  if ! command -v jq &> /dev/null; then
    echo "::error::jq is required but not installed"
    exit 1
  fi

  if [ ! -f "config.json" ]; then
    echo "::error::config.json not found in $(pwd)"
    exit 1
  fi

  jq '.version' config.json
```

### Pattern 8: Safe Variable Expansion

Quote variables and handle empty/unset cases explicitly.

```bash
# ❌ BAD: Unquoted variables break on whitespace, unset vars ignored
run: |
  FILES=$(git diff --name-only)
  for file in $FILES; do
    process $file
  done

# ✅ GOOD: Quoted variables, array handling, empty checks
run: |
  set -euo pipefail

  mapfile -t FILES < <(git diff --name-only)

  if [ ${#FILES[@]} -eq 0 ]; then
    echo "No files changed"
    exit 0
  fi

  for file in "${FILES[@]}"; do
    if [ -f "$file" ]; then
      process "$file"
    fi
  done
```

### Pattern 9: GitHub API Error Handling

Always check API responses and handle rate limits.

```bash
# ❌ BAD: No error checking, silent failures
run: |
  RESULT=$(gh api repos/$REPO/pulls)
  echo "$RESULT" | jq '.[0].number'

# ✅ GOOD: Check response, handle errors, respect rate limits
run: |
  set -euo pipefail

  if ! RESULT=$(gh api repos/${{ github.repository }}/pulls \
    --header "Accept: application/vnd.github+json" \
    --jq '.[0].number' 2>&1); then

    if echo "$RESULT" | grep -q "rate limit"; then
      echo "::warning::Rate limited, waiting 60s..."
      sleep 60
      RESULT=$(gh api repos/${{ github.repository }}/pulls --jq '.[0].number')
    else
      echo "::error::API call failed: $RESULT"
      exit 1
    fi
  fi

  echo "PR Number: $RESULT"
```

### Pattern 10: Arithmetic Without Subshell Errors

Bash arithmetic with `set -e` can cause unexpected exits.

```bash
# ❌ BAD: ((count++)) returns 1 when count=0, causing exit with set -e
run: |
  set -euo pipefail
  count=0
  ((count++))  # Exits here because (( )) returns 0's exit code as 1

# ✅ GOOD: Use arithmetic expansion or explicit assignment
run: |
  set -euo pipefail
  count=0
  count=$((count + 1))  # Safe - assignment always succeeds

  # Or use : prefix to discard exit code
  : $((count++))
```

## GitHub Actions Annotations

Use workflow commands for structured output:

```bash
run: |
  set -euo pipefail

  # Errors (fail the step visually)
  echo "::error file=src/main.cs,line=10::Null reference found"

  # Warnings (yellow badge)
  echo "::warning::Deprecated API usage detected"

  # Debug (only shown with debug logging enabled)
  echo "::debug::Processing file: $FILE"

  # Group output for collapsible sections
  echo "::group::Installation logs"
  npm install
  echo "::endgroup::"

  # Mask sensitive values
  echo "::add-mask::$SECRET_VALUE"
```

## Environment Variables Best Practices

```bash
run: |
  set -euo pipefail

  # ✅ Set output for other steps
  echo "version=1.2.3" >> "$GITHUB_OUTPUT"

  # ✅ Set environment for subsequent steps
  echo "MY_VAR=value" >> "$GITHUB_ENV"

  # ✅ Add to PATH for subsequent steps
  echo "$HOME/.local/bin" >> "$GITHUB_PATH"

  # ✅ Multiline output (use delimiter)
  {
    echo "changelog<<EOF"
    cat CHANGELOG.md
    echo "EOF"
  } >> "$GITHUB_OUTPUT"
```

## Checklist

- [ ] Every `run:` block starts with `set -euo pipefail`
- [ ] Using `$RUNNER_TEMP` instead of `/tmp` for temp files
- [ ] Multiline API bodies use `-F body=@file` not `-f body="..."`
- [ ] Heredocs use `<<-` with tabs or unindented content
- [ ] Polling loops have timeouts and exponential backoff
- [ ] AWK/sed use exact field matching (`$1 == "value"`)
- [ ] All variables are quoted (`"$VAR"` not `$VAR`)
- [ ] File/command existence checked before use
- [ ] API errors handled with clear messages
- [ ] Arithmetic uses `$((x + 1))` not `((x++))`
- [ ] Sensitive values masked with `::add-mask::`
- [ ] Run `actionlint` on workflow files before commit

## Related Skills

- [github-actions-script-pattern](./github-actions-script-pattern.md) — Extract complex logic to testable scripts (preferred approach)
- [validate-before-commit](./validate-before-commit.md) — Includes actionlint for workflow validation
- [defensive-programming](./defensive-programming.md) — General defensive programming patterns
- [git-safe-operations](./git-safe-operations.md) — Safe git operations in scripts
