#!/usr/bin/env bash
# shellcheck shell=bash
# =============================================================================
# install-codex.sh
# -----------------------------------------------------------------------------
# Idempotently install the latest @openai/codex CLI as a user-global npm
# package. This script is safe to run on every container start.
#
# Behavior:
#   * Resolves npm latest dist-tag with a bounded timeout.
#   * Skips install when current version already matches latest.
#   * Installs into NPM_CONFIG_PREFIX (default: $HOME/.local) without sudo.
#   * Retries transient failures with small backoff.
#   * Never fails callers; offline and registry errors are non-fatal.
# =============================================================================

set -euo pipefail

PKG="@openai/codex"
BIN="codex"
NPM_PREFIX="${NPM_CONFIG_PREFIX:-${HOME}/.local}"
VIEW_TIMEOUT_SECONDS="${CODEX_NPM_VIEW_TIMEOUT_SECONDS:-20}"
INSTALL_TIMEOUT_SECONDS="${CODEX_NPM_INSTALL_TIMEOUT_SECONDS:-180}"
LOG_PREFIX="[install-codex]"

log() {
    echo "${LOG_PREFIX} $*"
}

warn() {
    echo "${LOG_PREFIX} WARN: $*" >&2
}

usage() {
    cat <<'EOF'
Usage: install-codex.sh [--force-latest-check]

Options:
  --force-latest-check   Accepted for compatibility. Latest is always checked.
EOF
}

while [ "$#" -gt 0 ]; do
    case "$1" in
        --force-latest-check)
            ;;
        --help|-h)
            usage
            exit 0
            ;;
        *)
            warn "Unknown argument '$1'; ignoring."
            ;;
    esac
    shift
done

if ! command -v npm >/dev/null 2>&1; then
    warn "npm not found; skipping ${PKG} install."
    exit 0
fi

export NPM_CONFIG_PREFIX="${NPM_PREFIX}"
export PATH="${NPM_PREFIX}/bin:${PATH}"

if ! mkdir -p "${NPM_PREFIX}"; then
    warn "Unable to create npm prefix directory: ${NPM_PREFIX}."
    exit 0
fi

if [[ ! -w "${NPM_PREFIX}" ]]; then
    warn "npm prefix '${NPM_PREFIX}' is not writable; skipping install."
    exit 0
fi

# Read currently installed version from user-global install location.
installed=""
pkg_json="${NPM_PREFIX}/lib/node_modules/@openai/codex/package.json"
if [[ -f "${pkg_json}" ]]; then
    if command -v jq >/dev/null 2>&1; then
        installed="$(jq -r '.version // empty' "${pkg_json}" 2>/dev/null || true)"
    else
        installed="$(grep -m1 '"version"' "${pkg_json}" | sed -E 's/.*"version"[[:space:]]*:[[:space:]]*"([^"]+)".*/\1/' || true)"
    fi
fi

latest="$(timeout "${VIEW_TIMEOUT_SECONDS}" npm view "${PKG}" version 2>/dev/null | tr -d '[:space:]' || true)"

if [[ -z "${latest}" ]]; then
    if [[ -n "${installed}" ]]; then
        log "Registry unreachable; keeping installed ${PKG}@${installed}."
    else
        warn "Registry unreachable and ${PKG} is not installed; will retry later."
    fi
    exit 0
fi

if [[ "${installed}" == "${latest}" ]]; then
    log "${PKG}@${installed} already up-to-date."
    exit 0
fi

log "Installing ${PKG}@${latest} (previously: ${installed:-not installed})"

for attempt in 1 2 3; do
    if timeout "${INSTALL_TIMEOUT_SECONDS}" npm install -g "${PKG}@${latest}" --silent --no-fund --no-audit; then
        hash -r 2>/dev/null || true
        if command -v "${BIN}" >/dev/null 2>&1; then
            log "${PKG} ready: $(${BIN} --version 2>/dev/null | head -1 || echo "${latest}")"
            exit 0
        fi
        warn "${BIN} binary missing from PATH after install attempt ${attempt}/3."
    else
        warn "npm install failed (attempt ${attempt}/3)."
    fi
    sleep "$((2 ** (attempt - 1)))"
done

warn "Failed to install ${PKG} after 3 attempts; continuing without it."
exit 0
