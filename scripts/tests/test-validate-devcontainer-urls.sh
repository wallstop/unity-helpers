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

if grep -q 'POWERSHELL_VERSION=' "$DOCKERFILE"; then
    pass "Dockerfile defines POWERSHELL_VERSION for release URL validation"
else
    fail "Dockerfile defines POWERSHELL_VERSION for release URL validation" "Missing POWERSHELL_VERSION assignment"
fi

if grep -q 'https://github.com/PowerShell/PowerShell/releases/download/v${POWERSHELL_VERSION}/powershell-${POWERSHELL_VERSION}-linux-${POWERSHELL_ARCH}.tar.gz' "$DOCKERFILE"; then
    pass "Dockerfile contains inline PowerShell release URL template"
else
    fail "Dockerfile contains inline PowerShell release URL template" "Expected inline PowerShell GitHub release URL pattern"
fi

if grep -q 'amd64) POWERSHELL_ARCH="x64"' "$DOCKERFILE" \
    && grep -q 'arm64) POWERSHELL_ARCH="arm64"' "$DOCKERFILE"; then
    pass "Dockerfile maps TARGETARCH to PowerShell asset architecture"
else
    fail "Dockerfile maps TARGETARCH to PowerShell asset architecture" "Expected amd64->x64 and arm64->arm64 mappings"
fi

if grep -q 'sha256sum -c -' "$DOCKERFILE"; then
    pass "Dockerfile verifies PowerShell tarball checksum"
else
    fail "Dockerfile verifies PowerShell tarball checksum" "Expected checksum verification for PowerShell download"
fi

# Contract: validator parser should still discover powershell tool entry from Dockerfile.
if validator_output="$("$VALIDATOR" --dockerfile .devcontainer/Dockerfile 2>&1)"; then
    if echo "$validator_output" | grep -q 'powershell'; then
        pass "validator output includes powershell tool entry"
    else
        fail "validator output includes powershell tool entry" "Validator succeeded but did not report powershell entry"
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
