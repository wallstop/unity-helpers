#!/usr/bin/env bash
# =============================================================================
# Git Hook Permissions Validation Script
# =============================================================================
# Checks that all files in .githooks/ are tracked with executable permissions
# (100755) in git. Git silently skips non-executable hook files, which causes
# CI/CD failures that are difficult to diagnose.
#
# Usage:
#   ./scripts/validate-hook-permissions.sh          # Check permissions
#   ./scripts/validate-hook-permissions.sh --fix    # Fix permissions
#   ./scripts/validate-hook-permissions.sh --help   # Show help
#
# Exit codes:
#   0 - All hook files have executable permissions
#   1 - One or more hook files lack executable permissions (or other error)
# =============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

HOOKS_DIR=".githooks"
FAILED=0
FIX_MODE=0
CHECKED=0

print_header() {
    echo ""
    echo -e "${BLUE}── $1 ──${NC}"
}

print_success() {
    echo -e "${GREEN}  PASS${NC} $1"
}

print_fail() {
    echo -e "${RED}  FAIL${NC} $1"
}

print_info() {
    echo -e "${BLUE}  INFO${NC} $1"
}

show_help() {
    echo "Git Hook Permissions Validation Script"
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --fix     Fix non-executable hook files (git update-index --chmod=+x)"
    echo "  --help    Show this help message"
    echo ""
    echo "Checks:"
    echo "  Verifies all files in $HOOKS_DIR/ are tracked as 100755 (executable)"
    echo "  in the git index. Git silently skips non-executable hook files."
    echo ""
    echo "Fix manually:"
    echo "  git update-index --chmod=+x .githooks/<file>"
}

# Parse arguments
case "${1:-}" in
    --fix)
        FIX_MODE=1
        ;;
    --help|-h)
        show_help
        exit 0
        ;;
    "")
        # Default: check mode
        ;;
    *)
        echo "Unknown option: $1"
        show_help
        exit 1
        ;;
esac

cd "$REPO_ROOT"

# Verify prerequisites
if ! command -v git >/dev/null 2>&1; then
    echo -e "${RED}Error: git is not available.${NC}"
    exit 1
fi

if [ ! -d "$HOOKS_DIR" ]; then
    echo -e "${RED}Error: $HOOKS_DIR directory not found.${NC}"
    exit 1
fi

print_header "Validating git hook file permissions"

# Get all tracked files in .githooks/ with their mode
# git ls-files -s outputs: <mode> <hash> <stage> <path>
while IFS= read -r line; do
    # Skip empty lines
    if [ -z "$line" ]; then
        continue
    fi

    # Parse the mode and path from git ls-files -s output
    mode=$(echo "$line" | awk '{print $1}')
    file_path=$(echo "$line" | awk '{print $4}')

    CHECKED=$((CHECKED + 1))

    if [ "$mode" = "100755" ]; then
        print_success "$file_path ($mode - executable)"
    else
        if [ "$FIX_MODE" -eq 1 ]; then
            print_info "$file_path: fixing permissions ($mode -> 100755)..."
            git update-index --chmod=+x "$file_path"
            print_success "$file_path (fixed to 100755)"
        else
            print_fail "$file_path ($mode - NOT executable)"
            echo -e "         Expected: ${GREEN}100755${NC} (executable)"
            echo -e "         Actual:   ${RED}$mode${NC} (non-executable)"
            echo -e "         Fix with: ${YELLOW}git update-index --chmod=+x $file_path${NC}"
            FAILED=$((FAILED + 1))
        fi
    fi
done < <(git ls-files -s "$HOOKS_DIR/")

# Handle case where no files were found
if [ "$CHECKED" -eq 0 ]; then
    echo -e "${YELLOW}  WARNING: No tracked files found in $HOOKS_DIR/${NC}"
    echo -e "  Make sure hook files are committed to git."
    exit 1
fi

# Summary
echo ""
echo -e "${BLUE}── Summary ──${NC}"
echo -e "  Files checked: $CHECKED"

if [ "$FAILED" -gt 0 ]; then
    echo ""
    echo -e "${RED}$FAILED file(s) missing executable permissions.${NC}"
    echo -e "Git silently skips non-executable hook files."
    echo ""
    echo -e "Fix all at once:"
    echo -e "  ${YELLOW}$0 --fix${NC}"
    echo -e "Or fix individually:"
    echo -e "  ${YELLOW}git update-index --chmod=+x .githooks/<file>${NC}"
    exit 1
else
    if [ "$FIX_MODE" -eq 1 ]; then
        echo -e "${GREEN}All hook file permissions are correct (fixed).${NC}"
    else
        echo -e "${GREEN}All hook files have executable permissions.${NC}"
    fi
    exit 0
fi
