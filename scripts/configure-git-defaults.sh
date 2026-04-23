#!/usr/bin/env bash
set -euo pipefail

# =============================================================================
# Git Push Defaults Configuration (Bash)
# =============================================================================
# Idempotently sets local-only git push defaults so that `git push` on a new
# branch sets tracking automatically and uses the "simple" push strategy.
#
# Usage:
#   ./scripts/configure-git-defaults.sh            # uses script's parent dir
#   ./scripts/configure-git-defaults.sh <repo-root>
#
# Effects (local config only; NEVER global):
#   git config --local push.autoSetupRemote true
#   git config --local push.default simple
# =============================================================================

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
DEFAULT_REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
REPO_ROOT="${1:-$DEFAULT_REPO_ROOT}"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

print_success() {
    echo -e "${GREEN}✓${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

print_error() {
    echo -e "${RED}✗${NC} $1"
}

print_info() {
    echo -e "${BLUE}ℹ${NC} $1"
}

if ! command -v git >/dev/null 2>&1; then
    print_error "git is not installed or not on PATH."
    exit 1
fi

if ! git -C "$REPO_ROOT" rev-parse --git-dir >/dev/null 2>&1; then
    print_error "Not a git repository: $REPO_ROOT"
    exit 1
fi

# push.autoSetupRemote requires git >= 2.37.
git_version_raw="$(git --version 2>/dev/null | awk '{print $3}')"
git_major="$(echo "$git_version_raw" | awk -F. '{print $1+0}')"
git_minor="$(echo "$git_version_raw" | awk -F. '{print $2+0}')"
if [ -n "$git_major" ] && [ -n "$git_minor" ]; then
    if [ "$git_major" -lt 2 ] || { [ "$git_major" -eq 2 ] && [ "$git_minor" -lt 37 ]; }; then
        print_warning "git $git_version_raw detected. push.autoSetupRemote requires git >= 2.37; older clients will silently ignore it."
    fi
fi

print_info "Configuring git push defaults (local only) in $REPO_ROOT"

git -C "$REPO_ROOT" config --local push.autoSetupRemote true
print_success "push.autoSetupRemote = true"

git -C "$REPO_ROOT" config --local push.default simple
print_success "push.default = simple"

# Echo the resulting values so callers/tests can assert behavior.
final_auto_setup="$(git -C "$REPO_ROOT" config --local --get push.autoSetupRemote || echo '')"
final_default="$(git -C "$REPO_ROOT" config --local --get push.default || echo '')"

echo "push.autoSetupRemote=$final_auto_setup"
echo "push.default=$final_default"

if [ "$final_auto_setup" != "true" ] || [ "$final_default" != "simple" ]; then
    print_error "Post-configure verification failed (push.autoSetupRemote=$final_auto_setup push.default=$final_default)"
    exit 1
fi
