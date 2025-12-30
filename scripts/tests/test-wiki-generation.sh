#!/usr/bin/env bash
# Test script for wiki generation sidebar link format
# Ensures we use Markdown link syntax [Display](Page) instead of MediaWiki [[Page|Display]]
#
# Run: bash scripts/tests/test-wiki-generation.sh
# Exit codes: 0 = all tests pass, 1 = test failure

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test counters
tests_run=0
tests_passed=0
tests_failed=0

# Test helper functions
pass() {
    tests_passed=$((tests_passed + 1))
    echo -e "${GREEN}✓${NC} $1"
}

fail() {
    tests_failed=$((tests_failed + 1))
    echo -e "${RED}✗${NC} $1"
    echo -e "  ${RED}Expected:${NC} $2"
    echo -e "  ${RED}Actual:${NC}   $3"
}

run_test() {
    tests_run=$((tests_run + 1))
}

# =============================================================================
# get_display_name function (copied from deploy-wiki.yml for testing)
# =============================================================================
get_display_name() {
    local wiki_name="$1"
    local display="$wiki_name"

    # Remove category prefixes
    display="${display#Overview-}"
    display="${display#Features-}"
    display="${display#Guides-}"
    display="${display#Performance-}"
    display="${display#Project-}"

    # Remove subcategory prefixes
    # shellcheck disable=SC2001
    display=$(echo "$display" | sed -E 's/^(Inspector|Effects|Relational-Components|Serialization|Spatial|Logging|Utilities|Editor-Tools)-//')

    # Convert remaining dashes to spaces
    display="${display//-/ }"
    echo "$display"
}

# =============================================================================
# Test: get_display_name produces correct output
# =============================================================================
echo ""
echo "=== Testing get_display_name function ==="

run_test
result=$(get_display_name "Overview-Getting-Started")
if [ "$result" = "Getting Started" ]; then
    pass "Overview-Getting-Started -> 'Getting Started'"
else
    fail "Overview-Getting-Started -> 'Getting Started'" "Getting Started" "$result"
fi

run_test
result=$(get_display_name "Features-Inspector-Buttons")
if [ "$result" = "Buttons" ]; then
    pass "Features-Inspector-Buttons -> 'Buttons'"
else
    fail "Features-Inspector-Buttons -> 'Buttons'" "Buttons" "$result"
fi

run_test
result=$(get_display_name "Features-Relational-Components-Overview")
if [ "$result" = "Overview" ]; then
    pass "Features-Relational-Components-Overview -> 'Overview'"
else
    fail "Features-Relational-Components-Overview -> 'Overview'" "Overview" "$result"
fi

run_test
result=$(get_display_name "Guides-Odin-Migration")
if [ "$result" = "Odin Migration" ]; then
    pass "Guides-Odin-Migration -> 'Odin Migration'"
else
    fail "Guides-Odin-Migration -> 'Odin Migration'" "Odin Migration" "$result"
fi

run_test
result=$(get_display_name "Performance-Benchmarks")
if [ "$result" = "Benchmarks" ]; then
    pass "Performance-Benchmarks -> 'Benchmarks'"
else
    fail "Performance-Benchmarks -> 'Benchmarks'" "Benchmarks" "$result"
fi

run_test
result=$(get_display_name "Features-Utilities-Data-Structures")
if [ "$result" = "Data Structures" ]; then
    pass "Features-Utilities-Data-Structures -> 'Data Structures'"
else
    fail "Features-Utilities-Data-Structures -> 'Data Structures'" "Data Structures" "$result"
fi

# =============================================================================
# Test: Markdown link format is correct
# =============================================================================
echo ""
echo "=== Testing Markdown link format ==="

# Generate a link using the correct format
generate_sidebar_link() {
    local wiki_name="$1"
    local display
    display=$(get_display_name "$wiki_name")
    echo "- [$display]($wiki_name)"
}

run_test
result=$(generate_sidebar_link "Overview-Getting-Started")
expected="- [Getting Started](Overview-Getting-Started)"
if [ "$result" = "$expected" ]; then
    pass "Generates correct Markdown link format"
else
    fail "Generates correct Markdown link format" "$expected" "$result"
fi

run_test
result=$(generate_sidebar_link "Features-Inspector-Buttons")
expected="- [Buttons](Features-Inspector-Buttons)"
if [ "$result" = "$expected" ]; then
    pass "Generates correct Markdown link for nested categories"
else
    fail "Generates correct Markdown link for nested categories" "$expected" "$result"
fi

# =============================================================================
# Test: MediaWiki syntax is NOT used (regression prevention)
# =============================================================================
echo ""
echo "=== Testing MediaWiki syntax is NOT present ==="

run_test
result=$(generate_sidebar_link "Overview-Getting-Started")
if [[ "$result" != *"[["* ]] && [[ "$result" != *"]]"* ]]; then
    pass "No MediaWiki [[ brackets in output"
else
    fail "No MediaWiki [[ brackets in output" "No [[ or ]]" "$result"
fi

run_test
# MediaWiki format would be [[Page|Display]] - pipe inside brackets
if [[ "$result" != *"[[*|*]]"* ]]; then
    pass "No MediaWiki pipe syntax in output"
else
    fail "No MediaWiki pipe syntax in output" "No [[...|...]] pattern" "$result"
fi

# =============================================================================
# Test: Validation regex captures correct patterns
# =============================================================================
echo ""
echo "=== Testing validation regex ==="

# The validation regex from deploy-wiki.yml
validate_sidebar_link() {
    local line="$1"
    # Extract page name from Markdown link [Display](Page)
    echo "$line" | grep -oE '\]\([A-Za-z0-9_.-]+\)' | sed 's/\](//;s/)//'
}

run_test
result=$(validate_sidebar_link "- [Getting Started](Overview-Getting-Started)")
if [ "$result" = "Overview-Getting-Started" ]; then
    pass "Regex extracts page name from Markdown link"
else
    fail "Regex extracts page name from Markdown link" "Overview-Getting-Started" "$result"
fi

run_test
result=$(validate_sidebar_link "- [Data_Structures](Features-Utilities-Data_Structures)")
if [ "$result" = "Features-Utilities-Data_Structures" ]; then
    pass "Regex handles underscores in page names"
else
    fail "Regex handles underscores in page names" "Features-Utilities-Data_Structures" "$result"
fi

run_test
result=$(validate_sidebar_link "- [Version 2.0](Release-Notes-2.0)")
if [ "$result" = "Release-Notes-2.0" ]; then
    pass "Regex handles periods in page names"
else
    fail "Regex handles periods in page names" "Release-Notes-2.0" "$result"
fi

# Negative test - should NOT match MediaWiki syntax
run_test
result=$(echo "- [[Overview-Getting-Started|Getting Started]]" | grep -oE '\]\([A-Za-z0-9_.-]+\)' | sed 's/\](//;s/)//' || echo "")
if [ -z "$result" ]; then
    pass "Regex does NOT match MediaWiki syntax (regression prevention)"
else
    fail "Regex does NOT match MediaWiki syntax" "(empty)" "$result"
fi

# =============================================================================
# Test: Workflow file contains correct syntax
# =============================================================================
echo ""
echo "=== Testing deploy-wiki.yml syntax ==="

WORKFLOW_FILE=".github/workflows/deploy-wiki.yml"

if [ -f "$WORKFLOW_FILE" ]; then
    run_test
    # Check that we use Markdown syntax for Home link
    if grep -q 'echo "- \[Home\](Home)"' "$WORKFLOW_FILE"; then
        pass "Workflow uses Markdown syntax for Home link"
    else
        fail "Workflow uses Markdown syntax for Home link" 'echo "- [Home](Home)"' "(not found)"
    fi

    run_test
    # Check that we use Markdown syntax for dynamic links
    if grep -q 'echo "- \[\$display\](\$wiki_name)"' "$WORKFLOW_FILE"; then
        pass "Workflow uses Markdown syntax for dynamic links"
    else
        fail "Workflow uses Markdown syntax for dynamic links" 'echo "- [$display]($wiki_name)"' "(not found)"
    fi

    run_test
    # Check that we use Markdown syntax for CHANGELOG link
    if grep -q 'echo "- \[Changelog\](CHANGELOG)"' "$WORKFLOW_FILE"; then
        pass "Workflow uses Markdown syntax for CHANGELOG link"
    else
        fail "Workflow uses Markdown syntax for CHANGELOG link" 'echo "- [Changelog](CHANGELOG)"' "(not found)"
    fi

    run_test
    # Regression test: MediaWiki syntax should NOT be used for sidebar links
    # (It's OK in comments, but not in echo statements generating links)
    mediawiki_links=$(grep -E 'echo.*\[\[.*\]\]' "$WORKFLOW_FILE" 2>/dev/null || echo "")
    if [ -z "$mediawiki_links" ]; then
        pass "No MediaWiki [[...]] syntax in echo statements (regression prevention)"
    else
        fail "No MediaWiki [[...]] syntax in echo statements" "(none)" "$mediawiki_links"
    fi

    run_test
    # Check validation regex includes underscores
    if grep -q '\[A-Za-z0-9_\.-\]' "$WORKFLOW_FILE"; then
        pass "Validation regex includes underscores"
    else
        fail "Validation regex includes underscores" "[A-Za-z0-9_.-]" "(not found or incomplete)"
    fi
else
    echo -e "${YELLOW}⚠${NC} Workflow file not found: $WORKFLOW_FILE (skipping workflow tests)"
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
