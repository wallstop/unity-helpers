#!/usr/bin/env bash
# Regression tests for scripts/validate-devcontainer-urls.sh contract checks.
# Focus: prevent reintroduction of apt-based PowerShell installs that break
# multi-arch buildx runs.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
VALIDATOR="$REPO_ROOT/scripts/validate-devcontainer-urls.sh"

if [[ ! -x "$VALIDATOR" ]]; then
    echo "Error: validator script is not executable: $VALIDATOR" >&2
    exit 1
fi

TMP_DIR="$(mktemp -d)"
cleanup() {
    rm -rf "$TMP_DIR"
}
trap cleanup EXIT

pass_count=0
fail_count=0

echo "Running validate-devcontainer-urls contract tests..."

write_fixture_apt_powershell() {
    cat > "$1" <<'EOF'
FROM debian:12
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    git \
    powershell \
    curl
EOF
}

write_fixture_release_tarball() {
    cat > "$1" <<'EOF'
FROM debian:12
ARG TARGETARCH
RUN POWERSHELL_VERSION="7.5.1" \
    && case "${TARGETARCH}" in \
        amd64) POWERSHELL_ARCH="x64" ;; \
        arm64) POWERSHELL_ARCH="arm64" ;; \
        *) exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/PowerShell/PowerShell/releases/download/v${POWERSHELL_VERSION}/powershell-${POWERSHELL_VERSION}-linux-${POWERSHELL_ARCH}.tar.gz" \
    | tar -xz -C /opt/pwsh
EOF
}

write_fixture_commented_apt() {
    cat > "$1" <<'EOF'
FROM debian:12
# RUN apt-get install -y powershell
RUN echo "no apt powershell here"
EOF
}

write_fixture_case_variant_apt() {
    cat > "$1" <<'EOF'
FROM debian:12
RUN apt-get install -y PowerShell
EOF
}

write_fixture_chained_apt() {
    cat > "$1" <<'EOF'
FROM debian:12
RUN apt-get update && apt-get install -y git && apt-get install -y powershell
EOF
}

run_case() {
    local name="$1"
    local expected_rc="$2"
    local fixture="$3"

    if "$VALIDATOR" --contracts-only --dockerfile "$fixture" >/dev/null 2>&1; then
        rc=0
    else
        rc=$?
    fi

    if [[ "$rc" -eq "$expected_rc" ]]; then
        echo "  PASS: $name"
        pass_count=$((pass_count + 1))
    else
        echo "  FAIL: $name (expected rc=$expected_rc, got rc=$rc)"
        fail_count=$((fail_count + 1))
    fi
}

fixture_fail="$TMP_DIR/fail-apt-powershell.Dockerfile"
fixture_pass="$TMP_DIR/pass-release-tarball.Dockerfile"
fixture_comment_ok="$TMP_DIR/pass-commented-apt.Dockerfile"
fixture_case_fail="$TMP_DIR/fail-case-variant-apt.Dockerfile"
fixture_chain_fail="$TMP_DIR/fail-chained-apt.Dockerfile"

write_fixture_apt_powershell "$fixture_fail"
write_fixture_release_tarball "$fixture_pass"
write_fixture_commented_apt "$fixture_comment_ok"
write_fixture_case_variant_apt "$fixture_case_fail"
write_fixture_chained_apt "$fixture_chain_fail"

# Data-driven cases: name|expected_rc|fixture
cases=(
    "reject apt powershell|1|$fixture_fail"
    "allow release tarball install|0|$fixture_pass"
    "ignore commented apt powershell|0|$fixture_comment_ok"
    "reject case variant powershell|1|$fixture_case_fail"
    "reject chained apt powershell|1|$fixture_chain_fail"
)

for case in "${cases[@]}"; do
    IFS='|' read -r name expected_rc fixture <<< "$case"
    run_case "$name" "$expected_rc" "$fixture"
done

echo "Summary: passed=$pass_count failed=$fail_count"
if [[ "$fail_count" -gt 0 ]]; then
    exit 1
fi
