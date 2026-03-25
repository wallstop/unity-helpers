#!/usr/bin/env bash
# =============================================================================
# Test Script: Pre-push changed-file detection
# =============================================================================
# Validates that the pre-push hook correctly parses stdin to determine
# which files have changed, handling edge cases like new branches,
# force pushes, and delete refs.
#
# Run: bash scripts/tests/test-pre-push-changed-files.sh
# Exit codes: 0 = all tests pass, 1 = test failure
# =============================================================================

set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
NC='\033[0m'

tests_run=0
tests_passed=0
tests_failed=0

pass() {
    tests_passed=$((tests_passed + 1))
    echo -e "${GREEN}PASS${NC} $1"
}

fail() {
    tests_failed=$((tests_failed + 1))
    echo -e "${RED}FAIL${NC} $1"
    echo -e "  ${RED}Expected:${NC} $2"
    echo -e "  ${RED}Actual:${NC}   $3"
}

run_test() {
    tests_run=$((tests_run + 1))
}

# Get repo root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PRE_PUSH="$REPO_ROOT/.githooks/pre-push"

# =============================================================================
# Test: Pre-push hook file exists and is executable
# =============================================================================
echo ""
echo "=== Testing pre-push hook existence and permissions ==="

run_test
if [ -f "$PRE_PUSH" ]; then
    pass "Pre-push hook exists"
else
    fail "Pre-push hook exists" "file exists" "file not found"
fi

run_test
if [ -x "$PRE_PUSH" ]; then
    pass "Pre-push hook is executable"
else
    fail "Pre-push hook is executable" "executable" "not executable"
fi

# =============================================================================
# Test: Pre-push hook reads stdin
# =============================================================================
echo ""
echo "=== Testing pre-push hook reads stdin ==="

run_test
if grep -q 'while read' "$PRE_PUSH"; then
    pass "Hook contains stdin read loop"
else
    fail "Hook contains stdin read loop" "'while read' present" "not found"
fi

run_test
if grep -q 'local_sha' "$PRE_PUSH"; then
    pass "Hook parses local_sha"
else
    fail "Hook parses local_sha" "'local_sha' present" "not found"
fi

run_test
if grep -q 'remote_sha' "$PRE_PUSH"; then
    pass "Hook parses remote_sha"
else
    fail "Hook parses remote_sha" "'remote_sha' present" "not found"
fi

run_test
if grep -q 'ZERO_SHA' "$PRE_PUSH"; then
    pass "Hook handles zero SHA (new branch/delete)"
else
    fail "Hook handles zero SHA" "'ZERO_SHA' present" "not found"
fi

# =============================================================================
# Test: Changed-file list management
# =============================================================================
echo ""
echo "=== Testing changed-file list management ==="

run_test
if grep -q 'CHANGED_FILES_LIST' "$PRE_PUSH"; then
    pass "Hook uses CHANGED_FILES_LIST temp file"
else
    fail "Hook uses CHANGED_FILES_LIST temp file" "CHANGED_FILES_LIST present" "not found"
fi

run_test
if grep -q 'mktemp' "$PRE_PUSH"; then
    pass "Hook creates temp file with mktemp"
else
    fail "Hook creates temp file with mktemp" "'mktemp' present" "not found"
fi

run_test
if grep -qE 'trap.*cleanup|trap.*rm' "$PRE_PUSH"; then
    pass "Hook has cleanup trap"
else
    fail "Hook has cleanup trap" "cleanup trap present" "not found"
fi

# =============================================================================
# Test: New branch handling (merge-base fallback)
# =============================================================================
echo ""
echo "=== Testing new branch handling ==="

run_test
if grep -qE 'merge-base|merge_base' "$PRE_PUSH"; then
    pass "Hook uses merge-base for new branches"
else
    fail "Hook uses merge-base for new branches" "merge-base present" "not found"
fi

# =============================================================================
# Test: No auto-fix behavior (validation-only)
# =============================================================================
echo ""
echo "=== Testing validation-only behavior ==="

run_test
# Check that the hook does not EXECUTE prettier --write (auto-fix)
# Mentioning it in user-facing hints (echo "Run: npx prettier --write") is fine
if grep -v '^[[:space:]]*echo' "$PRE_PUSH" | grep -q 'prettier --write'; then
    fail "No prettier --write execution in pre-push" "no auto-fix" "prettier --write found"
else
    pass "No prettier --write execution in pre-push (validation-only)"
fi

run_test
if grep -q 'normalize-eol' "$PRE_PUSH"; then
    fail "No normalize-eol in pre-push" "no EOL auto-fix" "normalize-eol found"
else
    pass "No normalize-eol in pre-push (validation-only)"
fi

# =============================================================================
# Test: Parallel execution
# =============================================================================
echo ""
echo "=== Testing parallel execution ==="

run_test
if grep -q 'run_node_checks &' "$PRE_PUSH"; then
    pass "Node checks run in background"
else
    fail "Node checks run in background" "background execution" "not found"
fi

run_test
if grep -q 'run_pwsh_checks &' "$PRE_PUSH"; then
    pass "PowerShell checks run in background"
else
    fail "PowerShell checks run in background" "background execution" "not found"
fi

run_test
if grep -q 'run_bash_checks &' "$PRE_PUSH"; then
    pass "Bash checks run in background"
else
    fail "Bash checks run in background" "background execution" "not found"
fi

run_test
if grep -q 'wait.*PID' "$PRE_PUSH"; then
    pass "Hook waits for background PIDs"
else
    fail "Hook waits for background PIDs" "wait for PIDs" "not found"
fi

run_test
if grep -q 'HOOK_FAILED' "$PRE_PUSH"; then
    pass "Hook tracks failure status across parallel groups"
else
    fail "Hook tracks failure status" "HOOK_FAILED tracking" "not found"
fi

# =============================================================================
# Test: POSIX sh compatibility
# =============================================================================
echo ""
echo "=== Testing POSIX sh compatibility ==="

run_test
SHEBANG=$(head -1 "$PRE_PUSH")
if [ "$SHEBANG" = "#!/usr/bin/env sh" ]; then
    pass "Shebang is #!/usr/bin/env sh"
else
    fail "Shebang is #!/usr/bin/env sh" "#!/usr/bin/env sh" "$SHEBANG"
fi

# =============================================================================
# Test: Emergency skip documentation
# =============================================================================
echo ""
echo "=== Testing emergency skip documentation ==="

run_test
if grep -q 'no-verify' "$PRE_PUSH"; then
    pass "Hook documents --no-verify escape hatch"
else
    fail "Hook documents --no-verify" "--no-verify mentioned" "not found"
fi

# =============================================================================
# Test: Behavioral regex tests (verify patterns actually match correctly)
# =============================================================================
echo ""
echo "=== Testing changed-file detection regex patterns ==="

# Extract the changed_files_matching function behavior:
# It uses grep -E against a file list. Test regexes from the hook.

# Test CHANGED_LLM regex (C1 regression test - was ^\\.llm/ which matched backslash)
run_test
TEST_FILE=$(mktemp)
printf '.llm/context.md\n.llm/skills/foo.md\nRuntime/Foo.cs\n' > "$TEST_FILE"
# The hook uses '^\.llm/' in single quotes — in grep -E, \. matches literal dot
LLM_PATTERN='^\.llm/'
RESULT=$(grep -Ec "$LLM_PATTERN" "$TEST_FILE" 2>/dev/null || echo "0")
if [ "$RESULT" = "2" ]; then
    pass "LLM regex matches .llm/ paths correctly (matched $RESULT/2)"
else
    fail "LLM regex matches .llm/ paths" "2 matches" "$RESULT matches"
fi

# Verify it does NOT match with double-backslash (the former bug)
run_test
BAD_LLM_PATTERN='^\\.llm/'
BAD_RESULT=$(grep -Ec "$BAD_LLM_PATTERN" "$TEST_FILE" 2>/dev/null) || true
if [ "$BAD_RESULT" = "0" ]; then
    pass "Double-backslash regex correctly does NOT match .llm/ paths"
else
    fail "Double-backslash regex should not match" "0 matches" "$BAD_RESULT matches"
fi

# Test CHANGED_CS regex
run_test
printf 'Runtime/Foo.cs\nEditor/Bar.cs\nRuntime/Foo.cs.meta\ndocs/readme.md\n' > "$TEST_FILE"
CS_RESULT=$(grep -Ec '\.cs$' "$TEST_FILE" 2>/dev/null || echo "0")
if [ "$CS_RESULT" = "2" ]; then
    pass "CS regex matches .cs files (matched $CS_RESULT/2, excludes .cs.meta)"
else
    fail "CS regex matches .cs files" "2 matches" "$CS_RESULT matches"
fi

# Test region check pattern (POSIX ERE, not GNU BRE \|)
run_test
printf '  #region Foo\n  #endregion\n  // normal code\n#REGION Upper\n' > "$TEST_FILE"
REGION_RESULT=$(grep -E -c -i '^[[:space:]]*#[[:space:]]*(region|endregion)' "$TEST_FILE" 2>/dev/null || echo "0")
if [ "$REGION_RESULT" = "3" ]; then
    pass "Region regex matches #region/#endregion with POSIX character classes ($REGION_RESULT/3)"
else
    fail "Region regex matches correctly" "3 matches" "$REGION_RESULT matches"
fi

# Test CHANGED_GITIGNORE regex (pre-captured for C3 fix)
run_test
printf '.gitignore\ndocs/.gitignore\n.gitignore-backup\n' > "$TEST_FILE"
GITIGNORE_RESULT=$(grep -Ec '^\.gitignore$' "$TEST_FILE" 2>/dev/null || echo "0")
if [ "$GITIGNORE_RESULT" = "1" ]; then
    pass "Gitignore regex matches only root .gitignore ($GITIGNORE_RESULT/1)"
else
    fail "Gitignore regex matches only root .gitignore" "1 match" "$GITIGNORE_RESULT matches"
fi

# Cleanup test temp file
rm -f "$TEST_FILE"

# =============================================================================
# Test: POSIX compliance checks (no bashisms in pre-push)
# =============================================================================
echo ""
echo "=== Testing no bashisms in pre-push ==="

# Verify no \s escape sequences in grep patterns (non-POSIX)
run_test
# Use -F for fixed-string search to find literal \s (not interpreted as whitespace class)
# Skip comment lines
BACKSLASH_S_COUNT=$(grep -v '^[[:space:]]*#' "$PRE_PUSH" | grep -cF '\s' 2>/dev/null) || true
if [ "$BACKSLASH_S_COUNT" = "0" ]; then
    pass "No \\s escape sequences in non-comment lines"
else
    fail "No \\s in non-comment lines" "0 occurrences" "$BACKSLASH_S_COUNT occurrences"
fi

# Verify no GNU BRE \| alternation (should use grep -E with |)
run_test
# Use -F for fixed-string search to find literal \|
BRE_ALT_COUNT=$(grep -v '^[[:space:]]*#' "$PRE_PUSH" | grep -cF '\|' 2>/dev/null) || true
if [ "$BRE_ALT_COUNT" = "0" ]; then
    pass "No GNU BRE \\| alternation in non-comment lines"
else
    fail "No GNU BRE \\| in non-comment lines" "0 occurrences" "$BRE_ALT_COUNT occurrences"
fi

# Verify grep -E used (not plain grep) for alternation patterns
run_test
if grep -q 'grep -E' "$PRE_PUSH"; then
    pass "Uses grep -E for extended regex (POSIX ERE)"
else
    fail "Uses grep -E" "grep -E present" "not found"
fi

# =============================================================================
# Summary
# =============================================================================
echo ""
echo "=== Test Summary ==="
echo "Tests run:    $tests_run"
echo -e "Tests passed: ${GREEN}$tests_passed${NC}"
if [ "$tests_failed" -gt 0 ]; then
    echo -e "Tests failed: ${RED}$tests_failed${NC}"
    echo ""
    echo -e "${RED}FAILED${NC}"
    exit 1
else
    echo -e "Tests failed: ${GREEN}0${NC}"
    echo ""
    echo -e "${GREEN}ALL TESTS PASSED${NC}"
    exit 0
fi
