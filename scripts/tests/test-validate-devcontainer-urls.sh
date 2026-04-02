#!/usr/bin/env bash
# =============================================================================
# Test Script: validate-devcontainer-urls parser contracts
# =============================================================================
# Validates that scripts/validate-devcontainer-urls.sh continues to discover
# required tool URL definitions from .devcontainer/Dockerfile.
#
# Run: bash scripts/tests/test-validate-devcontainer-urls.sh
# Exit codes: 0 = all tests pass, 1 = one or more tests fail
# =============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
DOCKERFILE="$REPO_ROOT/.devcontainer/Dockerfile"
VALIDATOR="$REPO_ROOT/scripts/validate-devcontainer-urls.sh"

RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'

tests_run=0
tests_passed=0
tests_failed=0

pass() {
    tests_run=$((tests_run + 1))
    tests_passed=$((tests_passed + 1))
    echo -e "  ${GREEN}PASS${NC} $1"
}

fail() {
    tests_run=$((tests_run + 1))
    tests_failed=$((tests_failed + 1))
    echo -e "  ${RED}FAIL${NC} $1"
    if [[ -n "${2:-}" ]]; then
        echo -e "       ${RED}$2${NC}"
    fi
}

assert_file_contains_literal() {
    local file_path="$1"
    local needle="$2"
    local test_name="$3"
    local failure_message="$4"

    if grep -Fq -- "$needle" "$file_path"; then
        pass "$test_name"
    else
        fail "$test_name" "$failure_message (literal: $needle)"
    fi
}

echo -e "${BLUE}── validate-devcontainer-urls parser contracts ──${NC}"
echo ""

if [[ -x "$VALIDATOR" ]]; then
    pass "validate-devcontainer-urls.sh is executable"
else
    fail "validate-devcontainer-urls.sh is executable" "Expected +x permission on scripts/validate-devcontainer-urls.sh"
fi

if [[ -f "$DOCKERFILE" ]]; then
    pass ".devcontainer/Dockerfile exists"
else
    fail ".devcontainer/Dockerfile exists" "Missing .devcontainer/Dockerfile"
fi

assert_file_contains_literal "$DOCKERFILE" \
    'POWERSHELL_VERSION=' \
    "Dockerfile defines POWERSHELL_VERSION for release URL validation" \
    "Missing POWERSHELL_VERSION assignment"

assert_file_contains_literal "$DOCKERFILE" \
    'https://github.com/PowerShell/PowerShell/releases/download/v${POWERSHELL_VERSION}/powershell-${POWERSHELL_VERSION}-linux-${POWERSHELL_ARCH}.tar.gz' \
    "Dockerfile contains inline PowerShell release URL template" \
    "Expected inline PowerShell GitHub release URL pattern"

if grep -Fq -- 'amd64) POWERSHELL_ARCH="x64"' "$DOCKERFILE" \
    && grep -Fq -- 'arm64) POWERSHELL_ARCH="arm64"' "$DOCKERFILE"; then
    pass "Dockerfile maps TARGETARCH to PowerShell asset architecture"
else
    fail "Dockerfile maps TARGETARCH to PowerShell asset architecture" "Expected amd64->x64 and arm64->arm64 mappings"
fi

assert_file_contains_literal "$DOCKERFILE" \
    'sha256sum -c -' \
    "Dockerfile verifies PowerShell tarball checksum" \
    "Expected checksum verification for PowerShell download"

# Contract: validator parser should still discover powershell tool entry from Dockerfile.
if validator_output="$("$VALIDATOR" --dockerfile .devcontainer/Dockerfile 2>&1)"; then
    if grep -Fiq -- 'powershell' <<< "$validator_output"; then
        pass "validator output includes powershell tool entry"
    else
        fail "validator output includes powershell tool entry" "Validator succeeded but did not report powershell entry. Output: $validator_output"
    fi
else
    fail "validator script exits successfully on current Dockerfile" "$validator_output"
fi

echo ""
echo -e "${BLUE}── Summary ──${NC}"
echo "  Tests run:    $tests_run"
echo -e "  ${GREEN}Passed${NC}:      $tests_passed"
echo -e "  ${RED}Failed${NC}:      $tests_failed"

if [[ $tests_failed -gt 0 ]]; then
    exit 1
fi

exit 0
