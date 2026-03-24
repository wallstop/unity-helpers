#!/usr/bin/env bash
# Validates that all GitHub release download URLs in the devcontainer Dockerfile
# are reachable (HTTP 200). This catches broken URLs (wrong version, wrong arch
# suffix like gnu vs musl, yanked releases) before they break the container build.
#
# Usage:
#   ./scripts/validate-devcontainer-urls.sh
#   ./scripts/validate-devcontainer-urls.sh --dockerfile .devcontainer/Dockerfile
#
# Requires: bash 4+, curl, sed

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default Dockerfile path (relative to repo root)
DOCKERFILE=".devcontainer/Dockerfile"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case "$1" in
        --dockerfile)
            DOCKERFILE="$2"
            shift 2
            ;;
        *)
            echo "Usage: $0 [--dockerfile <path>]" >&2
            exit 1
            ;;
    esac
done

# Resolve to repo root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DOCKERFILE_PATH="$REPO_ROOT/$DOCKERFILE"

if [ ! -f "$DOCKERFILE_PATH" ]; then
    echo "Error: Dockerfile not found at $DOCKERFILE_PATH" >&2
    exit 1
fi

echo "========================================"
echo "Devcontainer Download URL Validation"
echo "Dockerfile: $DOCKERFILE"
echo "========================================"
echo ""

FAILED=0
PASSED=0
CHECKED=0

# Extract tool definitions: VERSION, ARCH mappings, and URL template
# Each RUN block that downloads from GitHub follows a pattern:
#   *_VERSION="X.Y.Z"
#   case ... amd64) *_ARCH="..." ;; arm64) *_ARCH="..." ;;
#   curl ... "https://github.com/.../${VERSION}/...-${ARCH}..."

# Data-driven tool definitions extracted from the Dockerfile.
# Format: TOOL_NAME|VERSION|AMD64_ARCH|ARM64_ARCH|URL_TEMPLATE
# URL_TEMPLATE uses {VERSION} and {ARCH} as placeholders.
tools=()

extract_tools() {
    local version=""
    local amd64_arch=""
    local arm64_arch=""
    local url_template=""
    local tool_name=""
    local in_run_block=false
    local version_var=""

    while IFS= read -r line; do
        # Detect VERSION assignment (e.g., FD_VERSION="10.4.2")
        if [[ "$line" =~ ([A-Z_]+)_VERSION=\"([^\"]+)\" ]]; then
            version_var="${BASH_REMATCH[1]}"
            version="${BASH_REMATCH[2]}"
            tool_name=$(echo "$version_var" | tr '[:upper:]' '[:lower:]')
            amd64_arch=""
            arm64_arch=""
            url_template=""
            in_run_block=true
        fi

        if [ "$in_run_block" = true ]; then
            # Extract amd64 arch mapping
            if [[ "$line" =~ amd64\)\ +[A-Z_]+=\"([^\"]+)\" ]]; then
                amd64_arch="${BASH_REMATCH[1]}"
            fi

            # Extract arm64 arch mapping
            if [[ "$line" =~ arm64\)\ +[A-Z_]+=\"([^\"]+)\" ]]; then
                arm64_arch="${BASH_REMATCH[1]}"
            fi

            # Extract URL template from curl command
            if [[ "$line" =~ curl[[:space:]]+-[a-zA-Z]*[[:space:]]+\"(https://github\.com/[^\"]+)\" ]]; then
                url_template="${BASH_REMATCH[1]}"

                # Replace variable references with placeholders
                # Handle ${VAR_VERSION} and ${VAR_ARCH} patterns
                # Specific variable first, then generic catch-all
                url_template=$(echo "$url_template" | sed -E "s/\\$\\{${version_var}_VERSION\\}/{VERSION}/g")
                url_template=$(echo "$url_template" | sed -E "s/\\$\\{[A-Z_]+_VERSION\\}/{VERSION}/g")
                url_template=$(echo "$url_template" | sed -E "s/\\$\\{[A-Z_]+_ARCH\\}/{ARCH}/g")

                # Only add if we have all required fields
                if [ -n "$version" ] && [ -n "$url_template" ]; then
                    # Some tools (like yq) don't have arch case blocks
                    if [ -z "$amd64_arch" ]; then
                        amd64_arch="NONE"
                    fi
                    if [ -z "$arm64_arch" ]; then
                        arm64_arch="NONE"
                    fi
                    tools+=("${tool_name}|${version}|${amd64_arch}|${arm64_arch}|${url_template}")
                fi
                in_run_block=false
            fi
        fi
    done < "$DOCKERFILE_PATH"
}

check_url() {
    local tool="$1"
    local arch_label="$2"
    local url="$3"

    CHECKED=$((CHECKED + 1))
    local status
    status=$(curl -sI -o /dev/null -w "%{http_code}" -L --max-time 30 "$url" 2>/dev/null || echo "000")

    if [ "$status" = "200" ]; then
        printf "  ${GREEN}✓${NC} %-8s %s\n" "[$arch_label]" "$url"
        PASSED=$((PASSED + 1))
        return 0
    else
        printf "  ${RED}✗${NC} %-8s HTTP %s  %s\n" "[$arch_label]" "$status" "$url"
        FAILED=$((FAILED + 1))
        return 1
    fi
}

# Extract tools from Dockerfile
extract_tools

if [ ${#tools[@]} -eq 0 ]; then
    echo "Warning: No tool definitions found in Dockerfile" >&2
    exit 1
fi

echo "Found ${#tools[@]} tools to validate"
echo ""

# Check each tool's URLs for both architectures
for entry in "${tools[@]}"; do
    IFS='|' read -r tool_name version amd64_arch arm64_arch url_template <<< "$entry"

    printf "${YELLOW}%s${NC} v%s\n" "$tool_name" "$version"

    # Check amd64
    if [ "$amd64_arch" != "NONE" ]; then
        amd64_url=$(echo "$url_template" | sed "s/{VERSION}/$version/g; s/{ARCH}/$amd64_arch/g")
        check_url "$tool_name" "amd64" "$amd64_url" || true
    else
        amd64_url=$(echo "$url_template" | sed "s/{VERSION}/$version/g; s/{ARCH}//g")
        check_url "$tool_name" "amd64" "$amd64_url" || true
    fi

    # Check arm64
    if [ "$arm64_arch" != "NONE" ]; then
        arm64_url=$(echo "$url_template" | sed "s/{VERSION}/$version/g; s/{ARCH}/$arm64_arch/g")
        check_url "$tool_name" "arm64" "$arm64_url" || true
    else
        arm64_url=$(echo "$url_template" | sed "s/{VERSION}/$version/g; s/{ARCH}//g")
        check_url "$tool_name" "arm64" "$arm64_url" || true
    fi

    echo ""
done

echo "========================================"
echo "Results: $PASSED passed, $FAILED failed ($CHECKED total)"
echo "========================================"

if [ "$FAILED" -gt 0 ]; then
    printf "${RED}%d URL(s) returned non-200 status!${NC}\n" "$FAILED"
    echo ""
    echo "Common causes:"
    echo "  - Wrong libc suffix (gnu vs musl) — check release assets"
    echo "  - Version yanked or doesn't exist"
    echo "  - Archive naming convention changed in new release"
    echo "  - arm64 builds not published for this version"
    exit 1
else
    printf "${GREEN}All download URLs are valid!${NC}\n"
    exit 0
fi
