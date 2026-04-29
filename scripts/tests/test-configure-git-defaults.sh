#!/usr/bin/env bash
# =============================================================================
# Test Script: configure-git-defaults.sh
# =============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
CONFIGURE_SCRIPT="$REPO_ROOT/scripts/configure-git-defaults.sh"
INSTALL_HOOKS_SCRIPT="$REPO_ROOT/scripts/install-hooks.sh"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

tests_run=0
tests_passed=0
tests_failed=0
failed_tests=()

pass() {
    local name="$1"
    tests_run=$((tests_run + 1))
    tests_passed=$((tests_passed + 1))
    echo -e "  ${GREEN}PASS${NC} $name"
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

run_command_capture() {
    local output_file="$1"
    shift

    if "$@" >"$output_file" 2>&1; then
        return 0
    fi

    return 1
}

echo -e "${BLUE}Testing configure-git-defaults.sh...${NC}"

temp_repo="$(mktemp -d)"
temp_non_repo="$(mktemp -d)"
temp_stub_root="$(mktemp -d)"
trap 'rm -rf "$temp_repo" "$temp_non_repo" "$temp_stub_root"' EXIT

echo -e "${BLUE}Section: Real git repository behavior${NC}"
git -C "$temp_repo" init -q

real_output_file="$temp_stub_root/real-output.txt"
if run_command_capture "$real_output_file" bash "$CONFIGURE_SCRIPT" "$temp_repo"; then
    real_output="$(cat -- "$real_output_file")"
    pass "Fresh repo configures successfully"
else
    real_output="$(cat -- "$real_output_file")"
    fail "Fresh repo configures successfully" "$real_output"
fi

if [[ "$(git -C "$temp_repo" config --local --get push.autoSetupRemote 2>/dev/null)" == "true" ]]; then
    pass "push.autoSetupRemote persisted"
else
    fail "push.autoSetupRemote persisted" "Expected git config value 'true'"
fi

if [[ "$(git -C "$temp_repo" config --local --get push.default 2>/dev/null)" == "simple" ]]; then
    pass "push.default persisted"
else
    fail "push.default persisted" "Expected git config value 'simple'"
fi

if grep -q 'push.autoSetupRemote=true' <<<"$real_output" && grep -q 'push.default=simple' <<<"$real_output"; then
    pass "stdout reports normalized persisted values"
else
    fail "stdout reports normalized persisted values" "$real_output"
fi

echo -e "${BLUE}Section: Non-repo failure behavior${NC}"
non_repo_output_file="$temp_stub_root/non-repo-output.txt"
if run_command_capture "$non_repo_output_file" bash "$CONFIGURE_SCRIPT" "$temp_non_repo"; then
    non_repo_output="$(cat -- "$non_repo_output_file")"
    fail "Non-repo directory exits non-zero" "Expected failure, got success: $non_repo_output"
else
    non_repo_output="$(cat -- "$non_repo_output_file")"
    pass "Non-repo directory exits non-zero"
fi

if grep -q 'Not a git repository' <<<"$non_repo_output"; then
    pass "Non-repo diagnostic is explicit"
else
    fail "Non-repo diagnostic is explicit" "$non_repo_output"
fi

echo -e "${BLUE}Section: Trailing CR normalization${NC}"
stub_bin="$temp_stub_root/bin"
mkdir -p "$stub_bin"
cat >"$stub_bin/git" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail

if [[ "${1:-}" == "--version" ]]; then
    echo "git version 2.44.0"
    exit 0
fi

if [[ "${1:-}" == "-C" ]]; then
    shift 2
fi

if [[ "${1:-}" == "rev-parse" && "${2:-}" == "--git-dir" ]]; then
    echo ".git"
    exit 0
fi

if [[ "${1:-}" == "config" && "${2:-}" == "--local" && "${3:-}" == "push.autoSetupRemote" && "${4:-}" == "true" ]]; then
    exit 0
fi

if [[ "${1:-}" == "config" && "${2:-}" == "--local" && "${3:-}" == "push.default" && "${4:-}" == "simple" ]]; then
    exit 0
fi

if [[ "${1:-}" == "config" && "${2:-}" == "--local" && "${3:-}" == "--get" && "${4:-}" == "push.autoSetupRemote" ]]; then
    printf 'true\r\n'
    exit 0
fi

if [[ "${1:-}" == "config" && "${2:-}" == "--local" && "${3:-}" == "--get" && "${4:-}" == "push.default" ]]; then
    printf 'simple\r\n'
    exit 0
fi

echo "unexpected git invocation: $*" >&2
exit 1
EOF
chmod +x "$stub_bin/git"

cr_output_file="$temp_stub_root/cr-output.txt"
if run_command_capture "$cr_output_file" env PATH="$stub_bin:$PATH" bash "$CONFIGURE_SCRIPT" "$temp_stub_root/fake-repo"; then
    cr_output="$(cat -- "$cr_output_file")"
    pass "Trailing CR verification path succeeds"
else
    cr_output="$(cat -- "$cr_output_file")"
    fail "Trailing CR verification path succeeds" "$cr_output"
fi

if grep -q $'\r' <<<"$cr_output"; then
    fail "Trailing CR is stripped from reported values" "$cr_output"
else
    pass "Trailing CR is stripped from reported values"
fi

if grep -q 'push.autoSetupRemote=true' <<<"$cr_output" && grep -q 'push.default=simple' <<<"$cr_output"; then
    pass "Normalized values remain human-readable"
else
    fail "Normalized values remain human-readable" "$cr_output"
fi

echo -e "${BLUE}Section: install-hooks --check normalization${NC}"
install_stub_bin="$temp_stub_root/install-bin"
mkdir -p "$install_stub_bin"
cat >"$install_stub_bin/git" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail

if [[ "${1:-}" == "--version" ]]; then
    echo "git version 2.44.0"
    exit 0
fi

if [[ "${1:-}" == "-C" ]]; then
    shift 2
fi

if [[ "${1:-}" == "config" && "${2:-}" == "--get" && "${3:-}" == "core.hooksPath" ]]; then
    printf '.\\.githooks\\\r\n'
    exit 0
fi

if [[ "${1:-}" == "config" && "${2:-}" == "--local" && "${3:-}" == "--get" && "${4:-}" == "push.autoSetupRemote" ]]; then
    printf 'true\r\n'
    exit 0
fi

if [[ "${1:-}" == "config" && "${2:-}" == "--local" && "${3:-}" == "--get" && "${4:-}" == "push.default" ]]; then
    printf ' simple \r\n'
    exit 0
fi

echo "unexpected git invocation: $*" >&2
exit 1
EOF
chmod +x "$install_stub_bin/git"

install_output_file="$temp_stub_root/install-check-output.txt"
if run_command_capture "$install_output_file" env PATH="$install_stub_bin:$PATH" bash "$INSTALL_HOOKS_SCRIPT" --check; then
    install_output="$(cat -- "$install_output_file")"
    pass "install-hooks --check succeeds with normalized git outputs"
else
    install_output="$(cat -- "$install_output_file")"
    fail "install-hooks --check succeeds with normalized git outputs" "$install_output"
fi

if grep -q 'Git hooks path: .githooks' <<<"$install_output"; then
    pass "install-hooks recognizes equivalent hooksPath forms"
else
    fail "install-hooks recognizes equivalent hooksPath forms" "$install_output"
fi

if grep -q 'push.autoSetupRemote: true' <<<"$install_output" && grep -q 'push.default: simple' <<<"$install_output"; then
    pass "install-hooks normalizes push defaults before comparison"
else
    fail "install-hooks normalizes push defaults before comparison" "$install_output"
fi

if grep -qE 'push\.autoSetupRemote: .*unset|push\.default: .*unset' <<<"$install_output"; then
    fail "install-hooks avoids false unset warnings after normalization" "$install_output"
else
    pass "install-hooks avoids false unset warnings after normalization"
fi

echo ""
echo -e "${BLUE}Summary${NC}"
echo "  Tests run:    $tests_run"
echo "  Passed:       $tests_passed"
echo "  Failed:       $tests_failed"

if [[ $tests_failed -gt 0 ]]; then
    echo "Failed tests:"
    for test_name in "${failed_tests[@]}"; do
        echo "  - $test_name"
    done
fi

exit "$tests_failed"
