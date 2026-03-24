#!/usr/bin/env bash
# =============================================================================
# Test Script: Post-Create Script Validation
# =============================================================================
# Validates the devcontainer post-create.sh script for correctness:
#   - Shell syntax (bash -n)
#   - Executable permission
#   - Proper shebang line
#   - Volume mount directories in post-create.sh match devcontainer.json mounts
#   - Dockerfile pre-creates the same volume mount directories
#   - devcontainer.json postCreateCommand references the script
#   - No hardcoded user IDs (should use $(id -u) pattern)
#
# This is a data-driven test: volume mount definitions are extracted from
# devcontainer.json and cross-checked against both the setup script and
# the Dockerfile.
#
# Usage:
#   bash scripts/tests/test-post-create.sh
#   bash scripts/tests/test-post-create.sh --verbose
#
# Exit codes:
#   0 - All checks passed
#   1 - One or more checks failed
# =============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

VERBOSE=0
case "${1:-}" in
    --verbose|-v) VERBOSE=1 ;;
esac

# Files under test
POST_CREATE="$REPO_ROOT/.devcontainer/post-create.sh"
DEVCONTAINER_JSON="$REPO_ROOT/.devcontainer/devcontainer.json"
DOCKERFILE="$REPO_ROOT/.devcontainer/Dockerfile"

# Counters
tests_run=0
tests_passed=0
tests_failed=0
failed_tests=()

# ── Test Helpers ─────────────────────────────────────────────────────────────

pass() {
    local name="$1"
    tests_run=$((tests_run + 1))
    tests_passed=$((tests_passed + 1))
    if [[ $VERBOSE -eq 1 ]]; then
        echo -e "  ${GREEN}PASS${NC} $name"
    fi
}

fail() {
    local name="$1"
    local detail="${2:-}"
    tests_run=$((tests_run + 1))
    tests_failed=$((tests_failed + 1))
    failed_tests+=("$name")
    echo -e "  ${RED}FAIL${NC} $name"
    if [[ -n "$detail" ]]; then
        echo -e "       ${YELLOW}$detail${NC}"
    fi
}

# ── Prerequisite Checks ─────────────────────────────────────────────────────

echo -e "${BLUE}── Post-Create Script Validation ──${NC}"
echo ""

if [[ ! -f "$POST_CREATE" ]]; then
    fail "post-create.sh exists" "File not found: .devcontainer/post-create.sh"
    echo ""
    echo -e "${RED}Cannot continue without post-create.sh${NC}"
    exit 1
fi

if [[ ! -f "$DEVCONTAINER_JSON" ]]; then
    fail "devcontainer.json exists" "File not found: .devcontainer/devcontainer.json"
    echo ""
    echo -e "${RED}Cannot continue without devcontainer.json${NC}"
    exit 1
fi

if [[ ! -f "$DOCKERFILE" ]]; then
    fail "Dockerfile exists" "File not found: .devcontainer/Dockerfile"
    echo ""
    echo -e "${RED}Cannot continue without Dockerfile${NC}"
    exit 1
fi

# ── Test 1: Shell syntax check ───────────────────────────────────────────────

echo -e "${BLUE}Checking script syntax...${NC}"

if bash -n "$POST_CREATE" 2>/dev/null; then
    pass "post-create.sh passes bash -n syntax check"
else
    fail "post-create.sh passes bash -n syntax check" "$(bash -n "$POST_CREATE" 2>&1)"
fi

# ── Test 2: Executable permission ────────────────────────────────────────────

echo -e "${BLUE}Checking permissions...${NC}"

if [[ -x "$POST_CREATE" ]]; then
    pass "post-create.sh is executable"
else
    fail "post-create.sh is executable" "Missing +x permission"
fi

# ── Test 3: Proper shebang ───────────────────────────────────────────────────

echo -e "${BLUE}Checking shebang...${NC}"

SHEBANG=$(head -1 "$POST_CREATE")
if [[ "$SHEBANG" == "#!/usr/bin/env bash" ]] || [[ "$SHEBANG" == "#!/bin/bash" ]]; then
    pass "post-create.sh has valid bash shebang"
else
    fail "post-create.sh has valid bash shebang" "Got: $SHEBANG"
fi

# ── Test 4: Uses set -euo pipefail ───────────────────────────────────────────

echo -e "${BLUE}Checking error handling...${NC}"

if grep -q 'set -euo pipefail' "$POST_CREATE"; then
    pass "post-create.sh uses 'set -euo pipefail'"
else
    fail "post-create.sh uses 'set -euo pipefail'" "Missing strict error handling"
fi

# ── Test 5: No hardcoded UID/GID ─────────────────────────────────────────────

echo -e "${BLUE}Checking for hardcoded UIDs...${NC}"

# The script should use $(id -u):$(id -g), not 1000:1000 or vscode:vscode in chown
if grep -qE 'chown.*1000:1000' "$POST_CREATE"; then
    fail "post-create.sh avoids hardcoded UID 1000" "Found hardcoded 1000:1000 — use \$(id -u):\$(id -g) instead"
else
    pass "post-create.sh avoids hardcoded UID 1000"
fi

# ── Test 6: devcontainer.json references post-create.sh ──────────────────────

echo -e "${BLUE}Checking devcontainer.json integration...${NC}"

if grep -q 'post-create\.sh' "$DEVCONTAINER_JSON"; then
    pass "devcontainer.json postCreateCommand references post-create.sh"
else
    fail "devcontainer.json postCreateCommand references post-create.sh" \
        "postCreateCommand should call bash .devcontainer/post-create.sh"
fi

# ── Test 7: Data-driven volume mount cross-check ─────────────────────────────
# Extract volume mount targets from devcontainer.json and verify they are
# handled in both post-create.sh and Dockerfile.

echo -e "${BLUE}Cross-checking volume mount directories...${NC}"

# System-managed volumes that are intentionally root-owned and do NOT need
# pre-creation or chown. These are managed by devcontainer features (e.g.,
# Docker-in-Docker manages /var/lib/docker).
SYSTEM_MANAGED_PATHS=("/var/lib/docker")

is_system_managed() {
    local target="$1"
    for sys_path in "${SYSTEM_MANAGED_PATHS[@]}"; do
        if [[ "$target" == "$sys_path" ]]; then
            return 0
        fi
    done
    return 1
}

# Extract volume mount target paths from devcontainer.json
# Matches: "source=...,target=/some/path,type=volume"
VOLUME_TARGETS=()
while IFS= read -r line; do
    if [[ "$line" =~ target=([^,\"]+),type=volume ]]; then
        VOLUME_TARGETS+=("${BASH_REMATCH[1]}")
    fi
done < "$DEVCONTAINER_JSON"

if [[ ${#VOLUME_TARGETS[@]} -eq 0 ]]; then
    fail "devcontainer.json has volume mounts" "No volume mounts found"
else
    pass "devcontainer.json has ${#VOLUME_TARGETS[@]} volume mount(s)"

    for target in "${VOLUME_TARGETS[@]}"; do
        # Skip system-managed volumes (e.g., /var/lib/docker from DinD feature)
        if is_system_managed "$target"; then
            pass "volume target is system-managed (skip chown check): $target"
            continue
        fi

        # The script may reference the target directly or its parent directory.
        # e.g., /home/vscode/.nuget/packages is covered by /home/vscode/.nuget

        # Check post-create.sh references this path in a chown/VOLUME_DIRS context
        # (not just in comments). We search non-comment lines for the path or a parent.
        found_in_script=false
        check_path="$target"
        while [[ "$check_path" != "/" && "$check_path" != "/home/vscode" && "$check_path" != "/home" ]]; do
            # Match the path in non-comment lines (lines not starting with #)
            if grep -v '^\s*#' "$POST_CREATE" | grep -qF "$check_path"; then
                found_in_script=true
                break
            fi
            check_path="$(dirname "$check_path")"
        done

        if [[ "$found_in_script" == true ]]; then
            pass "post-create.sh handles volume target: $target"
        else
            fail "post-create.sh handles volume target: $target" \
                "Volume mount target not found in post-create.sh chown commands"
        fi

        # Check Dockerfile pre-creates this path (or a parent) in mkdir/chown context
        found_in_dockerfile=false
        check_path="$target"
        while [[ "$check_path" != "/" && "$check_path" != "/home/vscode" && "$check_path" != "/home" ]]; do
            if grep -v '^\s*#' "$DOCKERFILE" | grep -qF "$check_path"; then
                found_in_dockerfile=true
                break
            fi
            check_path="$(dirname "$check_path")"
        done

        if [[ "$found_in_dockerfile" == true ]]; then
            pass "Dockerfile pre-creates volume target: $target"
        else
            fail "Dockerfile pre-creates volume target: $target" \
                "Volume mount target not found in Dockerfile mkdir/chown commands"
        fi
    done
fi

# ── Test 8: chown runs before tool commands ──────────────────────────────────
# The chown must appear before dotnet/npm commands in the script.

echo -e "${BLUE}Checking command ordering...${NC}"

# Only check non-comment lines (skip lines starting with optional whitespace + #)
CHOWN_LINE=$(grep -n 'sudo chown' "$POST_CREATE" | grep -v '^\s*[0-9]*:\s*#' | head -1 | cut -d: -f1)
DOTNET_LINE=$(grep -n 'dotnet tool restore' "$POST_CREATE" | grep -v '^\s*[0-9]*:\s*#' | head -1 | cut -d: -f1)
NPM_LINE=$(grep -n 'npm ci\|npm i ' "$POST_CREATE" | grep -v '^\s*[0-9]*:\s*#' | head -1 | cut -d: -f1)

if [[ -n "$CHOWN_LINE" && -n "$DOTNET_LINE" ]]; then
    if [[ "$CHOWN_LINE" -lt "$DOTNET_LINE" ]]; then
        pass "chown runs before dotnet tool restore (line $CHOWN_LINE < $DOTNET_LINE)"
    else
        fail "chown runs before dotnet tool restore" \
            "chown on line $CHOWN_LINE, dotnet on line $DOTNET_LINE"
    fi
else
    if [[ -z "$CHOWN_LINE" ]]; then
        fail "chown command exists in post-create.sh" "No sudo chown found"
    fi
fi

if [[ -n "$CHOWN_LINE" && -n "$NPM_LINE" ]]; then
    if [[ "$CHOWN_LINE" -lt "$NPM_LINE" ]]; then
        pass "chown runs before npm install (line $CHOWN_LINE < $NPM_LINE)"
    else
        fail "chown runs before npm install" \
            "chown on line $CHOWN_LINE, npm on line $NPM_LINE"
    fi
fi

# ── Test 9: Script handles workspace folder detection ────────────────────────

echo -e "${BLUE}Checking workspace directory handling...${NC}"

if grep -qE 'git rev-parse --show-toplevel|CODESPACE_VSCODE_FOLDER' "$POST_CREATE"; then
    pass "post-create.sh detects workspace directory dynamically"
else
    fail "post-create.sh detects workspace directory dynamically" \
        "Should use git rev-parse or CODESPACE_VSCODE_FOLDER for detection"
fi

# ── Test 10: chown uses sudo ─────────────────────────────────────────────────

echo -e "${BLUE}Checking sudo usage...${NC}"

# chown on volume dirs requires sudo since we run as vscode user
if grep -v '^\s*#' "$POST_CREATE" | grep -q 'sudo chown'; then
    pass "post-create.sh uses sudo with chown"
else
    fail "post-create.sh uses sudo with chown" \
        "chown on volume mount dirs requires sudo (running as non-root vscode user)"
fi

# ── Test 11: sudo availability guard ─────────────────────────────────────────

if grep -q 'command -v sudo' "$POST_CREATE"; then
    pass "post-create.sh checks for sudo availability"
else
    fail "post-create.sh checks for sudo availability" \
        "Script should verify sudo exists before using it"
fi

# ── Test 12: shellcheck (if available) ───────────────────────────────────────

echo -e "${BLUE}Checking shellcheck...${NC}"

if command -v shellcheck >/dev/null 2>&1; then
    if shellcheck_output=$(shellcheck "$POST_CREATE" 2>&1); then
        pass "post-create.sh passes shellcheck"
    else
        fail "post-create.sh passes shellcheck" "$shellcheck_output"
    fi
else
    pass "shellcheck not available (skipped)"
fi

# ── Summary ──────────────────────────────────────────────────────────────────

echo ""
echo -e "${BLUE}── Summary ──${NC}"
echo -e "  Tests run:    $tests_run"
echo -e "  ${GREEN}Passed${NC}:      $tests_passed"
echo -e "  ${RED}Failed${NC}:      $tests_failed"

if [[ $tests_failed -gt 0 ]]; then
    echo ""
    echo -e "${RED}Failed tests:${NC}"
    for t in "${failed_tests[@]}"; do
        echo -e "  ${RED}-${NC} $t"
    done
    exit 1
else
    echo ""
    echo -e "${GREEN}All post-create script checks passed!${NC}"
    exit 0
fi
