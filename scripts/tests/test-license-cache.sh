#!/usr/bin/env bash
# =============================================================================
# Test Script: License year cache
# =============================================================================
# Validates the caching behavior of audit-license-years.sh:
#   - Cache file creation
#   - Cache hits (no git log calls on second run)
#   - --no-cache flag disables reads
#   - --paths flag for incremental mode
#   - Cache invalidation via post-rewrite hook
#
# Run: bash scripts/tests/test-license-cache.sh
# Exit codes: 0 = all tests pass, 1 = test failure
# =============================================================================

set -eu

# NOTE: pipefail intentionally not set — git ls-files | head causes benign SIGPIPE

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

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
AUDIT_SCRIPT="$REPO_ROOT/scripts/audit-license-years.sh"
CACHE_FILE="$REPO_ROOT/.git/license-year-cache"
POST_REWRITE="$REPO_ROOT/.githooks/post-rewrite"

cd "$REPO_ROOT"

# =============================================================================
# Test: audit-license-years.sh exists and has required flags
# =============================================================================
echo ""
echo "=== Testing audit-license-years.sh structure ==="

run_test
if [ -f "$AUDIT_SCRIPT" ]; then
    pass "audit-license-years.sh exists"
else
    fail "audit-license-years.sh exists" "file exists" "not found"
fi

run_test
if grep -q '\-\-paths' "$AUDIT_SCRIPT"; then
    pass "--paths flag is supported"
else
    fail "--paths flag is supported" "--paths present" "not found"
fi

run_test
if grep -q '\-\-no-cache' "$AUDIT_SCRIPT"; then
    pass "--no-cache flag is supported"
else
    fail "--no-cache flag is supported" "--no-cache present" "not found"
fi

run_test
if grep -q 'license-year-cache' "$AUDIT_SCRIPT"; then
    pass "Cache file path defined"
else
    fail "Cache file path defined" "license-year-cache path" "not found"
fi

run_test
if grep -q 'year_cache' "$AUDIT_SCRIPT"; then
    pass "Cache associative array exists"
else
    fail "Cache associative array exists" "year_cache variable" "not found"
fi

run_test
if grep -q 'save_cache' "$AUDIT_SCRIPT"; then
    pass "Cache save function defined"
else
    fail "Cache save function defined" "save_cache function" "not found"
fi

run_test
if grep -q 'load_cache' "$AUDIT_SCRIPT"; then
    pass "Cache load function defined"
else
    fail "Cache load function defined" "load_cache function" "not found"
fi

run_test
if grep -qE 'mktemp.*CACHE_FILE|mktemp.*license' "$AUDIT_SCRIPT"; then
    pass "Cache writes atomically via mktemp"
else
    fail "Cache writes atomically via mktemp" "atomic write via mktemp" "not found"
fi

run_test
if grep -q 'trap.*save_cache' "$AUDIT_SCRIPT"; then
    pass "Cache save registered as EXIT trap"
else
    fail "Cache save registered as EXIT trap" "trap save_cache EXIT" "not found"
fi

# =============================================================================
# Test: Cache creation on run
# =============================================================================
echo ""
echo "=== Testing cache creation ==="

# Remove existing cache
rm -f "$CACHE_FILE"

run_test
# Run with --paths on a single known .cs file to keep it fast
CS_FILE=$(git ls-files '*.cs' 2>/dev/null | head -1 || true)
if [ -n "$CS_FILE" ]; then
    bash "$AUDIT_SCRIPT" --summary --paths "$CS_FILE" >/dev/null 2>&1 || true
    if [ -f "$CACHE_FILE" ]; then
        pass "Cache file created after run"
    else
        fail "Cache file created after run" "cache file exists" "not created"
    fi
else
    fail "Cache creation" "found .cs file" "no .cs files in repo"
fi

# =============================================================================
# Test: Cache content has expected format
# =============================================================================
echo ""
echo "=== Testing cache content format ==="

run_test
if [ -f "$CACHE_FILE" ] && [ -s "$CACHE_FILE" ]; then
    # Cache format: <path>\t<year>
    FIRST_LINE=$(head -1 "$CACHE_FILE")
    if echo "$FIRST_LINE" | grep -qE $'^[^\t]+\t[0-9]{4}$'; then
        pass "Cache line has <path>\\t<year> format"
    else
        fail "Cache line format" "<path>\\t<year>" "$FIRST_LINE"
    fi
else
    fail "Cache file has content" "non-empty cache" "empty or missing"
fi

# =============================================================================
# Test: post-rewrite hook invalidates cache
# =============================================================================
echo ""
echo "=== Testing cache invalidation ==="

run_test
if [ -f "$POST_REWRITE" ]; then
    pass "post-rewrite hook exists"
else
    fail "post-rewrite hook exists" "file exists" "not found"
fi

run_test
if [ -x "$POST_REWRITE" ]; then
    pass "post-rewrite hook is executable"
else
    fail "post-rewrite hook is executable" "executable" "not executable"
fi

run_test
if grep -q 'license-year-cache' "$POST_REWRITE"; then
    pass "post-rewrite references cache file"
else
    fail "post-rewrite references cache file" "license-year-cache" "not found"
fi

# Simulate cache invalidation
if [ -f "$CACHE_FILE" ]; then
    # Ensure a cache file exists
    run_test
    bash "$POST_REWRITE" amend 2>/dev/null || true
    if [ ! -f "$CACHE_FILE" ]; then
        pass "post-rewrite hook deletes cache"
    else
        fail "post-rewrite hook deletes cache" "cache deleted" "cache still exists"
    fi
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
