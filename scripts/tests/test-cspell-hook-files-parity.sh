#!/usr/bin/env bash
# =============================================================================
# Test: cspell.json `files` glob vs pre-push spell-check extension parity
# =============================================================================
# Ensures that every file extension spell-checked by .githooks/pre-push
# (CHANGED_SPELL) is ALSO covered by cspell.json's top-level `files` glob.
#
# Why this matters:
#   The hook invokes `cspell lint ... -- "${CHANGED_SPELL[@]}"` which respects
#   cspell.json's `files` glob. If the hook passes a path whose extension is
#   NOT in `files`, cspell skips it silently -- except the hook already passed
#   the file on the CLI so it also runs. That divergence meant `npm run
#   lint:spelling` (which honors `files`) reported PASS while the same content
#   failed at pre-push with a narrower safety net.
#
#   This test closes that gap: the `files` glob MUST cover every extension the
#   hooks check. Otherwise `npm run lint:spelling` silently lies.
#
# Run: bash scripts/tests/test-cspell-hook-files-parity.sh
# Exit codes: 0 = parity, 1 = drift detected
# =============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PRE_PUSH="$REPO_ROOT/.githooks/pre-push"
CSPELL_JSON="$REPO_ROOT/cspell.json"

if [ ! -f "$PRE_PUSH" ]; then
    echo "FAIL: $PRE_PUSH not found" >&2
    exit 1
fi
if [ ! -f "$CSPELL_JSON" ]; then
    echo "FAIL: $CSPELL_JSON not found" >&2
    exit 1
fi

# -----------------------------------------------------------------------------
# Extract extensions linted by pre-push's CHANGED_SPELL array.
# Mirrors the extraction in test-hook-spell-parity.sh so the two tests are
# apples-to-apples.
# -----------------------------------------------------------------------------
extract_prepush_exts() {
    local changed_spell_line
    changed_spell_line=$(grep -E '^CHANGED_SPELL=\(' "$PRE_PUSH" || true)
    if [ -z "$changed_spell_line" ]; then
        echo "FAIL: could not locate CHANGED_SPELL= in pre-push" >&2
        return 1
    fi

    local feeders
    feeders=$(printf '%s\n' "$changed_spell_line" \
        | grep -oE 'CHANGED_[A-Z_]+\[@\]' \
        | sed 's/\[@\]//' \
        | sort -u)

    local feeder line group_part bare_part
    for feeder in $feeders; do
        line=$(grep -E "${feeder}\+=" "$PRE_PUSH" | head -1 || true)
        if [ -z "$line" ]; then
            continue
        fi
        group_part=$(printf '%s\n' "$line" \
            | grep -oE '\\\.\([^)]+\)\$' \
            | sed -e 's/^\\\.(//' -e 's/)\$$//' \
            | tr '|' '\n')
        bare_part=$(printf '%s\n' "$line" \
            | sed -E 's/\\\.\([^)]+\)\$//g' \
            | grep -oE '\\\.[A-Za-z0-9]+\$' \
            | sed -e 's/^\\\.//' -e 's/\$$//')
        printf '%s\n' "$group_part" "$bare_part"
    done | grep -v '^$' | sort -u
}

# -----------------------------------------------------------------------------
# Extract extensions covered by cspell.json's top-level `files` glob array.
# We parse JSON with node (cspell already requires node) to avoid a jq dep.
# For each glob we collect the file extensions it could match -- supporting
# simple literal forms ("**/*.cs"), brace expansions ("**/*.{md,markdown}"),
# and basename patterns ("README.md"). A glob without a recognizable
# extension (e.g. a directory glob like "foo/**") contributes nothing.
# -----------------------------------------------------------------------------
extract_cspell_files_exts() {
    node -e '
        const fs = require("fs");
        const cfg = JSON.parse(fs.readFileSync(process.argv[1], "utf8"));
        const files = Array.isArray(cfg.files) ? cfg.files : [];
        const out = new Set();
        for (const pat of files) {
            // Expand brace groups of the form {a,b,c}. We only handle a single
            // outermost brace group per glob, which is what our config uses.
            const expand = (g) => {
                const m = g.match(/\{([^{}]+)\}/);
                if (!m) return [g];
                const pieces = m[1].split(",");
                return pieces.map((p) => g.slice(0, m.index) + p + g.slice(m.index + m[0].length));
            };
            for (const expanded of expand(pat)) {
                // Extension is the part after the final dot of the basename.
                const base = expanded.replace(/.*\//, "");
                const dot = base.lastIndexOf(".");
                if (dot < 0) continue;
                const ext = base.slice(dot + 1).toLowerCase();
                // Skip wildcards in extension position (e.g. "foo.*").
                if (ext && !ext.includes("*")) out.add(ext);
            }
        }
        for (const ext of [...out].sort()) console.log(ext);
    ' "$CSPELL_JSON"
}

PRE_PUSH_EXTS=$(extract_prepush_exts)
CSPELL_FILES_EXTS=$(extract_cspell_files_exts)

echo "pre-push spell-check extensions:"
# shellcheck disable=SC2086
printf '  %s\n' $PRE_PUSH_EXTS
echo "cspell.json files-glob extensions:"
# shellcheck disable=SC2086
printf '  %s\n' $CSPELL_FILES_EXTS

# Every hook extension MUST be covered by cspell.json files.
MISSING=()
while IFS= read -r ext; do
    [ -z "$ext" ] && continue
    if ! printf '%s\n' "$CSPELL_FILES_EXTS" | grep -qx "$ext"; then
        MISSING+=("$ext")
    fi
done <<< "$PRE_PUSH_EXTS"

if [ ${#MISSING[@]} -gt 0 ]; then
    echo "" >&2
    echo "FAIL: cspell.json 'files' glob does NOT cover every extension the hooks check." >&2
    echo "Missing from cspell.json:" >&2
    for ext in "${MISSING[@]}"; do
        echo "  .$ext" >&2
    done
    echo "" >&2
    echo "Resolution:" >&2
    echo "  Broaden cspell.json 'files' so each hook extension is covered. A good" >&2
    echo "  default is:" >&2
    echo '    "files": [' >&2
    echo '      "**/*.{md,markdown}",' >&2
    echo '      "**/*.cs",' >&2
    echo '      "**/*.{yml,yaml}",' >&2
    echo '      "**/*.{json,jsonc,asmdef,asmref}",' >&2
    echo '      "**/*.js"' >&2
    echo '    ]' >&2
    echo "  Never narrow 'files' back down to avoid npm run lint:spelling silently" >&2
    echo "  skipping files the hooks catch." >&2
    echo "" >&2
    exit 1
fi

# Sanity: require a baseline extension set so an accidental empty match on
# both sides does not silently "pass" the parity check.
#
# CONTRACT: this REQUIRED array encodes a MINIMUM guarantee -- every listed
# extension MUST remain present in BOTH pre-push's CHANGED_SPELL feeders AND
# cspell.json's `files` glob. DO NOT narrow this list to make a failing test
# pass; narrowing defeats the drift-detection purpose (a coordinated narrowing
# of hook + cspell config + this baseline would leave no alarm). Only widen
# when adding a new hook-spell-checked extension; document any (rare) removal
# with a code comment explaining why the hook no longer needs that extension.
REQUIRED=("md" "markdown" "json" "jsonc" "asmdef" "asmref" "yml" "yaml" "js" "cs")
for ext in "${REQUIRED[@]}"; do
    if ! printf '%s\n' "$PRE_PUSH_EXTS" | grep -qx "$ext"; then
        echo "FAIL: required extension '$ext' missing from pre-push spell-check set" >&2
        exit 1
    fi
    if ! printf '%s\n' "$CSPELL_FILES_EXTS" | grep -qx "$ext"; then
        echo "FAIL: required extension '$ext' missing from cspell.json files glob" >&2
        exit 1
    fi
done

echo ""
echo "PASS: cspell.json files glob covers every pre-push spell-check extension."
exit 0
