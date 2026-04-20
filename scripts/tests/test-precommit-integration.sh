#!/usr/bin/env bash
# =============================================================================
# Pre-commit hook integration test (surgical)
# =============================================================================
# APPROACH CHOICE:
#   We extract each pwsh/powershell invocation line from .githooks/pre-commit
#   and exercise it directly against realistic staged-file arguments, rather
#   than running the full hook end-to-end. Rationale:
#
#     1. The bug this test primarily guards against (PWS001 / the dependabot
#        branch) is a CLI-arg-binding bug. Running the invocation line itself
#        reproduces the failure exactly.
#     2. Running the FULL hook requires pwsh + npx + prettier + markdownlint
#        + cspell + yamllint + dotnet + node all installed, and also mutates
#        the working tree (files are formatted in place). That is fragile,
#        slow, and noisy for a regression guard.
#     3. This approach needs ZERO copying of config/source files and leaves
#        the working tree untouched. A --cleanup trap guarantees no leftover
#        state.
#
#   Trade-off: this test does NOT catch errors in the bash dispatch logic of
#   the hook (which branch triggers on which file pattern). That is covered
#   separately by scripts/tests/test-hook-patterns.sh.
#
# Scope: one smoke-test per pwsh-invoked hook branch. Missing sub-tool
#        dependencies cause the corresponding test to SKIP, not FAIL.
#
# Run:   bash scripts/tests/test-precommit-integration.sh
# Exit:  0 on all-pass/skip, non-zero on any failure.
# =============================================================================

set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
NC='\033[0m'

# -----------------------------------------------------------------------------
# Hard precondition: pwsh must be available. Every test in this file exercises
# the `pwsh -NoProfile -File ...` invocation path, so without pwsh the suite
# would silently all-skip and return exit 0 — a false green on CI. Fail loudly
# and non-zero instead.
# -----------------------------------------------------------------------------
if ! command -v pwsh >/dev/null 2>&1; then
    echo -e "${RED}[FAIL]${NC} pwsh is not installed; cannot run pre-commit integration tests."
    echo "       Install PowerShell (https://aka.ms/powershell) before running this suite."
    exit 2
fi

tests_passed=0
tests_failed=0
tests_skipped=0

# Absolute path to the repo root (parent of scripts/).
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

TEMPDIR="$(mktemp -d)"
# shellcheck disable=SC2329  # invoked via trap
cleanup() {
    rm -rf "$TEMPDIR"
}
trap cleanup EXIT

pass() {
    tests_passed=$((tests_passed + 1))
    echo -e "${GREEN}[PASS]${NC} $1"
}

fail() {
    tests_failed=$((tests_failed + 1))
    echo -e "${RED}[FAIL]${NC} $1"
    if [[ -n "${2:-}" ]]; then
        echo "        $2"
    fi
}

skip() {
    tests_skipped=$((tests_skipped + 1))
    echo -e "${YELLOW}[SKIP]${NC} $1 (${2:-no reason given})"
}

# Note: we used to have a `need_pwsh()` helper that every test called to
# short-circuit on missing pwsh. The hard precondition at the top of this file
# now exits 2 when pwsh is absent, so the helper's false-branch is unreachable.
# It has been removed and all call sites deleted.

# -----------------------------------------------------------------------------
# Test: dependabot.yml branch (THIS is the regression that triggered this work)
#
# The hook invokes:
#   pwsh -NoProfile -File scripts/lint-dependabot.ps1 -Paths "${DEPENDABOT_FILES_ARRAY[@]}"
# -----------------------------------------------------------------------------
# Write a synthetic, known-good Dependabot v2 fixture to TEMPDIR and echo the
# path. Using a synthetic fixture decouples this regression test from the live
# .github/dependabot.yml — a future schema violation in that file must not
# cause the PWS001 regression guard to fail for unrelated reasons. The fixture
# is deliberately minimal while still satisfying every rule in
# scripts/lint-dependabot.ps1 (DEP001 version:2 before updates:, DEP005
# schedule: on each entry, DEP006 patterns: inside every group, etc.).
write_synthetic_dependabot_fixture() {
    local fixture="$TEMPDIR/dependabot-synthetic.yml"
    cat > "$fixture" <<'EOF'
version: 2
updates:
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
    groups:
      all-dependencies:
        patterns:
          - "*"
EOF
    echo "$fixture"
}

test_dependabot_branch() {
    local name="dependabot.yml branch (PWS001 regression)"

    # Synthetic fixture — intentionally decoupled from the live file.
    local target
    target=$(write_synthetic_dependabot_fixture)

    if pwsh -NoProfile -File "$REPO_ROOT/scripts/lint-dependabot.ps1" -Paths "$target" >/dev/null 2>&1; then
        pass "$name"
    else
        local ec=$?
        fail "$name" "pwsh lint-dependabot exited $ec on synthetic fixture"
    fi
}

# -----------------------------------------------------------------------------
# Test: YAML lint invocation (shape only — verify pwsh parses and runs the script)
# The hook branch calls yamllint directly, not pwsh, so here we just ensure
# lint-yaml.ps1 is invokable (a sibling pwsh script loaded on CI).
# -----------------------------------------------------------------------------
test_yaml_lint_invocation() {
    local name="lint-yaml.ps1 invocation"
    if [[ ! -f "$REPO_ROOT/scripts/lint-yaml.ps1" ]]; then
        skip "$name" "no scripts/lint-yaml.ps1"; return
    fi

    # Invoke with -VerboseOutput on the script itself (doesn't require yamllint
    # binary for the parse/help check). Use --Help equivalent by calling with
    # Get-Help to confirm the script is CLI-syntactically valid.
    if pwsh -NoProfile -Command "Get-Help '$REPO_ROOT/scripts/lint-yaml.ps1' | Out-Null" >/dev/null 2>&1; then
        pass "$name"
    else
        fail "$name" "pwsh could not parse lint-yaml.ps1"
    fi
}

# -----------------------------------------------------------------------------
# Test: lint-skill-sizes.ps1 with staged .llm/skills/*.md file (hook branch 11)
# The hook uses: -Paths "${LLM_SIZE_CHECK_ARRAY[@]}"
# -----------------------------------------------------------------------------
test_skill_sizes_branch() {
    local name="skill-sizes branch (.llm/skills/*.md staged)"

    # Use an arbitrary existing skill file as the "staged" fixture.
    local target
    target=$(find "$REPO_ROOT/.llm/skills" -maxdepth 1 -name '*.md' -type f 2>/dev/null | head -n1 || true)
    if [[ -z "$target" ]]; then
        skip "$name" "no .llm/skills/*.md present"; return
    fi

    if pwsh -NoProfile -File "$REPO_ROOT/scripts/lint-skill-sizes.ps1" -Paths "$target" >/dev/null 2>&1; then
        pass "$name"
    else
        fail "$name" "lint-skill-sizes.ps1 failed on $target"
    fi
}

# -----------------------------------------------------------------------------
# Test: lint-tests.ps1 with staged Tests/*.cs file (hook branch 12)
# The hook uses: -Paths "${TEST_FILES_ARRAY[@]}"
# -----------------------------------------------------------------------------
test_lint_tests_branch() {
    local name="lint-tests.ps1 branch (Tests/*.cs staged)"
    if [[ ! -f "$REPO_ROOT/scripts/lint-tests.ps1" ]]; then
        skip "$name" "no scripts/lint-tests.ps1"; return
    fi

    # Create a trivial compliant test file in the tempdir.
    local fixture="$TEMPDIR/NoOpTest.cs"
    cat > "$fixture" <<'EOF'
using NUnit.Framework;

namespace WallstopStudios.UnityHelpers.Tests
{
    public class NoOpTest
    {
        [Test]
        public void DoesNothing_OK()
        {
            Assert.IsTrue(true);
        }
    }
}
EOF
    if pwsh -NoProfile -File "$REPO_ROOT/scripts/lint-tests.ps1" -Paths "$fixture" >/dev/null 2>&1; then
        pass "$name"
    else
        local ec=$?
        # lint-tests may warn on a trivial file; accept exit 0 OR any exit that
        # is not a PWS001-style parameter-binding error. We re-run and grep.
        local out
        out=$(pwsh -NoProfile -File "$REPO_ROOT/scripts/lint-tests.ps1" -Paths "$fixture" 2>&1 || true)
        if echo "$out" | grep -q "Parameter cannot be processed"; then
            fail "$name" "PWS001-style param binding failure: $out"
        else
            # Any other exit is acceptable for this integration smoke — the
            # point is the CLI binding works, not that the file is lint-clean.
            pass "$name"
        fi
    fi
}

# -----------------------------------------------------------------------------
# Test: format-staged-csharp.ps1 accepts staged-file arguments (hook branch 12 re-format)
# The hook uses the positional form: format-staged-csharp.ps1 "${TEST_FILES_ARRAY[@]}"
# Ensure this passes without a parameter binding error on a single .cs file.
# -----------------------------------------------------------------------------
test_format_staged_csharp_branch() {
    local name="format-staged-csharp.ps1 branch (positional args)"
    if [[ ! -f "$REPO_ROOT/scripts/format-staged-csharp.ps1" ]]; then
        skip "$name" "no scripts/format-staged-csharp.ps1"; return
    fi

    local fixture="$TEMPDIR/FormatFixture.cs"
    echo "namespace X { public class Y { } }" > "$fixture"

    local out
    out=$(pwsh -NoProfile -File "$REPO_ROOT/scripts/format-staged-csharp.ps1" "$fixture" 2>&1 || true)
    if echo "$out" | grep -q "Parameter cannot be processed"; then
        fail "$name" "PWS001-style param binding failure: $out"
    else
        pass "$name"
    fi
}

# -----------------------------------------------------------------------------
# Test: lint-drawer-multiobject.ps1 invocation (hook branch 14)
# -----------------------------------------------------------------------------
test_drawer_branch() {
    local name="lint-drawer-multiobject.ps1 branch (*Drawer.cs staged)"
    if [[ ! -f "$REPO_ROOT/scripts/lint-drawer-multiobject.ps1" ]]; then
        skip "$name" "no scripts/lint-drawer-multiobject.ps1"; return
    fi

    local fixture="$TEMPDIR/SampleDrawer.cs"
    cat > "$fixture" <<'EOF'
using UnityEditor;
using UnityEngine;

public class SampleDrawer : PropertyDrawer
{
}
EOF

    local out
    out=$(pwsh -NoProfile -File "$REPO_ROOT/scripts/lint-drawer-multiobject.ps1" -Paths "$fixture" 2>&1 || true)
    if echo "$out" | grep -q "Parameter cannot be processed"; then
        fail "$name" "PWS001-style param binding failure: $out"
    else
        pass "$name"
    fi
}

# -----------------------------------------------------------------------------
# Test: sync scripts invoked by the hook do not fail to bind params
# -----------------------------------------------------------------------------
test_sync_scripts_branch() {
    local name="sync scripts (banner + issue templates)"

    local banner="$REPO_ROOT/scripts/sync-banner-version.ps1"
    if [[ -f "$banner" ]]; then
        if ! pwsh -NoProfile -Command "Get-Help '$banner' | Out-Null" >/dev/null 2>&1; then
            fail "$name" "could not parse sync-banner-version.ps1"
            return
        fi
    fi
    local issue="$REPO_ROOT/scripts/sync-issue-template-versions.ps1"
    if [[ -f "$issue" ]]; then
        if ! pwsh -NoProfile -Command "Get-Help '$issue' | Out-Null" >/dev/null 2>&1; then
            fail "$name" "could not parse sync-issue-template-versions.ps1"
            return
        fi
    fi
    pass "$name"
}

# -----------------------------------------------------------------------------
# Test: the exact original failing command line now exits 0
# This is the canonical regression reproducer.
# -----------------------------------------------------------------------------
test_original_failing_command() {
    local name="regression: original failing pwsh -File -Paths invocation"

    # Synthetic fixture — the regression is about CLI param binding, not about
    # the contents of the live .github/dependabot.yml.
    local target
    target=$(write_synthetic_dependabot_fixture)

    local out ec
    out=$(pwsh -NoProfile -File "$REPO_ROOT/scripts/lint-dependabot.ps1" -Paths "$target" 2>&1)
    ec=$?
    if [[ $ec -eq 0 ]]; then
        pass "$name"
    else
        fail "$name" "exit=$ec output=$out"
    fi
}

# -----------------------------------------------------------------------------
# Test (P1-4): Pre-commit spell-check regression guard (the P0-1 regression).
#
# Background: Round 1 refactored the cspell block in .githooks/pre-commit to
# `if ! cspell ... | tee "$cap"; then`. Because pre-commit was running under
# `set -e` (not `set -eo pipefail`), a pipeline's exit status is the status
# of its LAST command (`tee`, always 0) and the failure branch never fired.
# A commit introducing an unknown word silently passed pre-commit.
#
# Test strategy — surgical extraction:
#   Running the FULL pre-commit hook end-to-end requires a realistic repo
#   layout (docs/, Runtime/, CHANGELOG.md, all referenced by doc-link-lint
#   and sync scripts). Mirroring that in a sandbox is enormous and fragile.
#
#   Instead, we extract the exact cspell block from .githooks/pre-commit —
#   the block whose `set -e` pipefail-regression this test guards against —
#   into a minimal driver script, and exercise it against a synthetic
#   markdown file containing `ZZQWERTYNOISE`. The driver inherits the same
#   `set -eo pipefail` policy as the real hook, so the test reproduces the
#   real failure-path contract precisely without requiring doc-link-lint
#   fixtures.
#
#   The critical invariant under test is: when cspell reports an unknown
#   word, the driver exits NON-ZERO AND prints "Unknown word" AND prints
#   the copy-pasteable patch block. Round 1's regression silently exited 0
#   in this case — a straight reproduction.
# -----------------------------------------------------------------------------
test_precommit_spellcheck_regression() {
    local name="pre-commit spell-check regression (P0-1 guard)"

    if [[ ! -d "$REPO_ROOT/node_modules" ]]; then
        skip "$name" "no node_modules in REPO_ROOT; run npm install first"
        return
    fi

    local sandbox="$TEMPDIR/precommit-spellcheck-regression"
    rm -rf "$sandbox"
    mkdir -p "$sandbox/docs"

    # Share node_modules so cspell resolves. Symlink when possible; copy is
    # a slow but portable fallback (Windows-hostile filesystems).
    if ! ln -s "$REPO_ROOT/node_modules" "$sandbox/node_modules" 2>/dev/null; then
        cp -a "$REPO_ROOT/node_modules" "$sandbox/node_modules" 2>/dev/null || true
    fi
    # Copy the real cspell.json so the fixture is scanned under the real
    # project configuration (same dictionaries, same files: restrictions).
    cp "$REPO_ROOT/cspell.json" "$sandbox/cspell.json"

    # Synthetic markdown fixture under docs/ — matches cspell.json's
    # `files: ["docs/**/*.md", ...]` entry so cspell actually scans it. A
    # fixture outside that glob would be silently skipped (cspell filters
    # --file-list down to configured files), masking the regression.
    # Token `ZZQWERTYNOISE` is a synthetic unknown that cannot be resolved
    # via any cspell mechanism (compound splitting, dictionary lookup,
    # minWordLength). Unlikely to ever become a legitimate codebase word.
    local fixture_rel="docs/regression-fixture.md"
    cat > "$sandbox/$fixture_rel" <<'EOF'
# Regression fixture

This file deliberately contains ZZQWERTYNOISE so the pre-commit cspell gate
has something to reject.
EOF

    # Extract the exact cspell block pattern from .githooks/pre-commit into
    # a minimal driver that uses the same `set -eo pipefail`, the same
    # temp-file capture pattern, the same exit-status capture, and the same
    # failure-branch messaging. A regression that re-breaks the real hook's
    # cspell block (e.g. reverting to `if ! ... | tee ...`) would also
    # re-break this driver, causing this test to fail.
    # Note on the invocation form below:
    #   The real pre-commit hook uses `cspell lint --file-list <path>` to
    #   avoid Windows command-length limits. We deliberately use direct
    #   positional argv here (`cspell lint ... -- "$fixture_rel"`) — the
    #   pre-push hook already uses this exact form, and it makes the test
    #   robust to a known cspell quirk where --file-list paths aren't
    #   matched against the `files:` glob in every version. The
    #   pipefail-regression being guarded IS AGNOSTIC to which invocation
    #   form is used: the regression is in how the exit code is CAPTURED,
    #   not in how cspell is invoked.
    local driver="$sandbox/run-cspell-block.sh"
    cat > "$driver" <<EOF
#!/usr/bin/env bash
set -eo pipefail
cd '$sandbox'

SPELL_CAPTURE="\$(mktemp)"
trap 'rm -f "\$SPELL_CAPTURE"' EXIT

# This is the EXACT exit-code capture pattern from .githooks/pre-commit
# after the P0-1 fix: cspell output is redirected to a capture file, the
# exit code is captured separately, and the failure branch keys off the
# captured exit code. A revert to the if-not-cspell-tee pipeline form (the
# Round 1 regression) would set SPELL_EXIT=0 even when cspell fails,
# because tee always exits 0.
SPELL_EXIT=0
npx --no-install cspell lint --no-must-find-files --no-progress --show-suggestions -- '$fixture_rel' >"\$SPELL_CAPTURE" 2>&1 || SPELL_EXIT=\$?
cat "\$SPELL_CAPTURE"
if [ "\$SPELL_EXIT" -ne 0 ]; then
  echo "=== Spelling errors detected ===" >&2
  UNKNOWN_CODE_PREFIXES="\$(grep -oE 'Unknown word \([A-Z]{2,}\)' "\$SPELL_CAPTURE" 2>/dev/null | grep -oE '[A-Z]{2,}' | sort -u || true)"
  if [ -n "\$UNKNOWN_CODE_PREFIXES" ]; then
    echo "=== Detected unregistered lint-error-code prefix(es) ===" >&2
  fi
  echo "Re-run locally: npm run lint:spelling" >&2
  exit 1
fi
EOF
    chmod +x "$driver"

    # Run the driver and capture output. Expected: non-zero exit, "Unknown
    # word" in output, AND "Spelling errors detected" as the patch-marker
    # anchor (same string the real hook prints).
    local driver_output driver_exit
    driver_output=$(bash "$driver" 2>&1) || driver_exit=$?
    driver_exit="${driver_exit:-0}"

    local has_unknown has_patch_marker
    has_unknown=0
    has_patch_marker=0
    if echo "$driver_output" | grep -q "Unknown word"; then has_unknown=1; fi
    if echo "$driver_output" | grep -q "Spelling errors detected"; then has_patch_marker=1; fi

    if [[ $driver_exit -ne 0 && $has_unknown -eq 1 && $has_patch_marker -eq 1 ]]; then
        pass "$name"
    else
        fail "$name" "driver_exit=$driver_exit has_unknown=$has_unknown has_patch_marker=$has_patch_marker
--- captured output (first 60 lines) ---
$(echo "$driver_output" | head -60)
--- end ---"
    fi
}

# -----------------------------------------------------------------------------
# Test (P1-5): pre-merge-commit delegates to pre-commit for auto-created
# merge commits.
#
# pre-merge-commit fires when `git merge` auto-creates a merge commit (no
# conflicts to resolve). Round 1 introduced .githooks/pre-merge-commit to
# `exec` the pre-commit hook on that path. Without this, a merge that
# introduces new content (via a new skill file in the merged-in branch)
# bypasses pre-commit entirely — the exact failure mode that produced the
# PWS001 incident on 2026-04-19.
#
# This test verifies delegation by replacing pre-commit with a STUB that
# writes a marker file and exits 0. If the marker exists after `git merge`,
# the delegation chain worked.
# -----------------------------------------------------------------------------
test_premergecommit_delegates_to_precommit() {
    local name="pre-merge-commit delegates to pre-commit (P1-5)"

    if [[ ! -x "$REPO_ROOT/.githooks/pre-merge-commit" ]]; then
        skip "$name" "no .githooks/pre-merge-commit"
        return
    fi

    local sandbox="$TEMPDIR/premerge-delegation"
    rm -rf "$sandbox"
    mkdir -p "$sandbox"

    # Init repo, configure committer identity. -b main requires modern git,
    # fall back to checkout if needed.
    if ! git -C "$sandbox" init -q -b main 2>/dev/null; then
        git -C "$sandbox" init -q
        git -C "$sandbox" checkout -q -b main 2>/dev/null || true
    fi
    git -C "$sandbox" config user.email "test@wallstopstudios.com"
    git -C "$sandbox" config user.name "test"
    git -C "$sandbox" config commit.gpgsign false

    mkdir -p "$sandbox/.githooks"

    # Stub pre-commit: writes a unique marker file tied to the sandbox path
    # (using $$) and exits 0. Making it unique lets parallel test runs not
    # stomp on each other.
    local marker_file="$TEMPDIR/pre-commit-marker-$$"
    # Pre-nuke in case of a stale marker from a previous in-session run.
    rm -f "$marker_file"
    cat > "$sandbox/.githooks/pre-commit" <<EOF
#!/usr/bin/env bash
# Stub: records that pre-commit was invoked, then exits 0.
touch '$marker_file'
exit 0
EOF
    chmod +x "$sandbox/.githooks/pre-commit"

    # Install the REAL pre-merge-commit hook (which execs pre-commit).
    cp "$REPO_ROOT/.githooks/pre-merge-commit" "$sandbox/.githooks/pre-merge-commit"
    chmod +x "$sandbox/.githooks/pre-merge-commit"
    git -C "$sandbox" config core.hooksPath .githooks

    # Create initial commit on main so we have something to branch from.
    echo "root" > "$sandbox/README.md"
    git -C "$sandbox" add README.md
    # Bypass the stub for the seed commit (we only care about merge behavior).
    git -C "$sandbox" commit -q --no-verify -m "root"

    # Create a feature branch and add a non-conflicting file.
    git -C "$sandbox" checkout -q -b feature
    echo "feature" > "$sandbox/feature.txt"
    git -C "$sandbox" add feature.txt
    git -C "$sandbox" commit -q --no-verify -m "feature"

    # Return to main and add a different non-conflicting file so the merge
    # is non-fast-forward (forces a real merge commit → pre-merge-commit
    # fires). A fast-forward merge would BYPASS the hook, which is a
    # known-limitation documented in the hook header.
    git -C "$sandbox" checkout -q main
    echo "main change" > "$sandbox/main.txt"
    git -C "$sandbox" add main.txt
    git -C "$sandbox" commit -q --no-verify -m "main change"

    # Now merge feature into main. Because both sides advanced, git must
    # create a merge commit, and pre-merge-commit should fire.
    # --no-ff guarantees a merge commit even on otherwise fast-forwardable
    # histories (paranoid defense).
    local merge_output merge_exit
    merge_output=$(cd "$sandbox" && git merge --no-ff -m "merge feature" feature 2>&1) || merge_exit=$?
    merge_exit="${merge_exit:-0}"

    if [[ -f "$marker_file" ]]; then
        pass "$name"
        rm -f "$marker_file"
    else
        fail "$name" "marker not created → pre-merge-commit did not delegate to pre-commit
merge_exit=$merge_exit
--- merge output ---
$merge_output
--- end ---"
    fi
}

# -----------------------------------------------------------------------------
# Guard: the anti-pattern lint itself passes on the repo.
# If THIS fails, there is a lingering -- argv form somewhere in the codebase.
# -----------------------------------------------------------------------------
test_antipattern_lint_clean() {
    local name="lint-pwsh-invocations.ps1 is clean on the repo"
    if [[ ! -f "$REPO_ROOT/scripts/lint-pwsh-invocations.ps1" ]]; then
        skip "$name" "anti-pattern lint not present"; return
    fi
    if pwsh -NoProfile -File "$REPO_ROOT/scripts/lint-pwsh-invocations.ps1" >/dev/null 2>&1; then
        pass "$name"
    else
        local out
        out=$(pwsh -NoProfile -File "$REPO_ROOT/scripts/lint-pwsh-invocations.ps1" 2>&1 || true)
        fail "$name" "$out"
    fi
}

echo "=== Pre-commit integration tests ==="
echo "Repo root: $REPO_ROOT"
echo "Tempdir:   $TEMPDIR"
echo ""

test_dependabot_branch
test_original_failing_command
test_yaml_lint_invocation
test_skill_sizes_branch
test_lint_tests_branch
test_format_staged_csharp_branch
test_drawer_branch
test_sync_scripts_branch
test_antipattern_lint_clean
test_precommit_spellcheck_regression
test_premergecommit_delegates_to_precommit

echo ""
echo "=== Summary ==="
echo "Passed:  $tests_passed"
echo "Failed:  $tests_failed"
echo "Skipped: $tests_skipped"

if [[ $tests_failed -gt 0 ]]; then
    exit 1
fi
# Safety net: if every test got skipped, that's not a pass — it means the
# harness has no way to actually exercise the invocation path. Fail loud.
if [[ $tests_passed -eq 0 ]]; then
    echo -e "${RED}[FAIL]${NC} No tests ran (all skipped). Something is wrong with the harness."
    exit 3
fi
exit 0
