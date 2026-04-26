#!/usr/bin/env bash
# =============================================================================
# Test: pre-commit / pre-push spell-check extension parity
# =============================================================================
# Ensures that .githooks/pre-commit (SPELL_FILES_ARRAY bucket building loop)
# and .githooks/pre-push (CHANGED_SPELL array) lint the SAME set of file
# extensions. Any drift between the two is a structural regression: a commit
# can slip past the narrower hook (e.g. via --no-verify, a wrong hooksPath,
# or a hook sync miss) and land spelling violations on origin.
#
# Run: bash scripts/tests/test-hook-spell-parity.sh
# Exit codes: 0 = parity, 1 = drift detected
# =============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PRE_COMMIT="$REPO_ROOT/.githooks/pre-commit"
PRE_PUSH="$REPO_ROOT/.githooks/pre-push"

if [ ! -f "$PRE_COMMIT" ]; then
    echo "FAIL: $PRE_COMMIT not found" >&2
    exit 1
fi
if [ ! -f "$PRE_PUSH" ]; then
    echo "FAIL: $PRE_PUSH not found" >&2
    exit 1
fi

# -----------------------------------------------------------------------------
# Extract extensions linted by pre-commit's SPELL_FILES_ARRAY
# -----------------------------------------------------------------------------
# pre-commit uses bash `case "$file" in` patterns like *.md|*.markdown that
# append to SPELL_FILES_ARRAY. Harvest the shape `*.<ext>` tokens from the
# span that starts at the SPELL_FILES_ARRAY filter loop and ends at the next
# blank line after "done".
#
# Strategy: locate the block that initializes SPELL_FILES_ARRAY through the
# matching `done`, collect every `*.<ext>` token inside it.
# -----------------------------------------------------------------------------

extract_precommit_exts() {
    awk '
        /SPELL_FILES_ARRAY=\(\)/ { in_block = 1 }
        in_block {
            print
            if ($0 ~ /^[[:space:]]*done[[:space:]]*$/) { in_block = 0 }
        }
    ' "$PRE_COMMIT" \
        | grep -oE '\*\.[A-Za-z0-9]+' \
        | sed 's/^\*\.//' \
        | sort -u
}

# -----------------------------------------------------------------------------
# Extract extensions linted by pre-push's CHANGED_SPELL array
# -----------------------------------------------------------------------------
# pre-push sets CHANGED_SPELL to the union of pre-computed per-extension
# arrays (CHANGED_MD, CHANGED_JSON, CHANGED_YAML, CHANGED_JS, CHANGED_CS).
# Each of those arrays is populated via a [[ "$file" =~ \.(ext1|ext2)$ ]] test.
# Harvest the extension list by:
#   1. Reading the CHANGED_SPELL=(...) line to get which CHANGED_* feeder
#      arrays are in scope.
#   2. For each feeder, locating its populating regex and extracting the
#      extensions captured inside \.(...)$.
# -----------------------------------------------------------------------------

extract_prepush_exts() {
    # Extract the CHANGED_SPELL definition and all CHANGED_<TYPE> feeders.
    local changed_spell_line
    changed_spell_line=$(grep -E '^CHANGED_SPELL=\(' "$PRE_PUSH" || true)
    if [ -z "$changed_spell_line" ]; then
        echo "FAIL: could not locate CHANGED_SPELL= in pre-push" >&2
        return 1
    fi

    # Pull every CHANGED_<NAME>[@] reference from the CHANGED_SPELL assignment.
    local feeders
    feeders=$(printf '%s\n' "$changed_spell_line" \
        | grep -oE 'CHANGED_[A-Z_]+\[@\]' \
        | sed 's/\[@\]//' \
        | sort -u)

    # For each feeder, locate its populating `[[ "$file" =~ \.<regex>$ ]]` test.
    # Handles both grouped form `\.(ext1|ext2)$` and bare form `\.ext$`.
    local feeder line group_part bare_part
    for feeder in $feeders; do
        line=$(grep -E "${feeder}\+=" "$PRE_PUSH" | head -1 || true)
        if [ -z "$line" ]; then
            continue
        fi
        # 1) Grouped form: \.(ext1|ext2|...)$
        group_part=$(printf '%s\n' "$line" \
            | grep -oE '\\\.\([^)]+\)\$' \
            | sed -e 's/^\\\.(//' -e 's/)\$$//' \
            | tr '|' '\n')
        # 2) Bare form: \.ext$ (no surrounding parens)
        # Strip any grouped occurrences first so we don't double-count.
        bare_part=$(printf '%s\n' "$line" \
            | sed -E 's/\\\.\([^)]+\)\$//g' \
            | grep -oE '\\\.[A-Za-z0-9]+\$' \
            | sed -e 's/^\\\.//' -e 's/\$$//')
        printf '%s\n' "$group_part" "$bare_part"
    done | grep -v '^$' | sort -u
}

PRE_COMMIT_EXTS=$(extract_precommit_exts)
PRE_PUSH_EXTS=$(extract_prepush_exts)

echo "pre-commit spell-check extensions:"
# shellcheck disable=SC2086  # intentional word-splitting: one ext per line
printf '  %s\n' $PRE_COMMIT_EXTS
echo "pre-push   spell-check extensions:"
# shellcheck disable=SC2086  # intentional word-splitting: one ext per line
printf '  %s\n' $PRE_PUSH_EXTS

DIFF=$(diff <(printf '%s\n' "$PRE_COMMIT_EXTS") <(printf '%s\n' "$PRE_PUSH_EXTS") || true)

if [ -n "$DIFF" ]; then
    echo ""
    echo "FAIL: pre-commit and pre-push spell-check extension lists differ." >&2
    echo "  (< pre-commit only, > pre-push only)" >&2
    echo "$DIFF" >&2
    echo "" >&2
    echo "Resolution:" >&2
    echo "  Update SPELL_FILES_ARRAY in .githooks/pre-commit and/or CHANGED_SPELL" >&2
    echo "  (plus its feeder arrays) in .githooks/pre-push so both match." >&2
    echo "" >&2
    exit 1
fi

# Sanity: require a baseline set so an accidental empty match on both sides
# does not silently "pass" the parity check.
REQUIRED=("md" "markdown" "json" "jsonc" "asmdef" "asmref" "yml" "yaml" "js" "cs")
for ext in "${REQUIRED[@]}"; do
    if ! printf '%s\n' "$PRE_COMMIT_EXTS" | grep -qx "$ext"; then
        echo "FAIL: required extension '$ext' missing from pre-commit spell-check set" >&2
        exit 1
    fi
    if ! printf '%s\n' "$PRE_PUSH_EXTS" | grep -qx "$ext"; then
        echo "FAIL: required extension '$ext' missing from pre-push spell-check set" >&2
        exit 1
    fi
done

echo ""
echo "PASS: pre-commit and pre-push spell-check extension sets match."
exit 0
