# Skill: Ship Changes

<!-- trigger: ship, release, finalize, pre-landing, merge-ready, pr-ready | End-to-end workflow for shipping changes: validate, review, version, changelog, commit | Core -->

**Trigger**: When changes are ready to be finalized, committed, and prepared for merge.

---

## When to Use

- After implementation is complete and you want to ship
- When preparing a PR or final commit
- When asked to "ship it", "finalize", or "wrap up"

---

## When NOT to Use

- When implementation is still in progress
- When there are known failing tests or unresolved issues
- For documentation-only changes (use [update-documentation](./update-documentation.md) directly)

---

## Ship Workflow

Execute these steps in order. Each step must pass before proceeding.

### Step 1: Pre-Flight Checks

Run the full validation suite:

```bash
npm run validate:prepush
```

This executes all linting, formatting, and convention checks. **All must pass.**

If any check fails:

1. Fix the issue
2. Re-run the failing check
3. When all pass, proceed

### Step 2: Test Verification

Verify tests pass. If this is a Unity package change:

1. Confirm all existing tests still pass conceptually (note: Unity tests require Unity Editor)
2. Verify new tests exist for new functionality
3. Check test naming follows conventions: `MethodName_Condition_ExpectedResult`

### Step 3: Pre-Landing Review

Execute a [review-code-changes](./review-code-changes.md) pass on all staged/modified files:

1. Run two-pass review (Critical, then Informational)
2. Auto-fix mechanical issues (formatting, spelling, missing null checks)
3. Track risk score per [self-regulate-changes](./self-regulate-changes.md)
4. If critical issues found, fix and restart from Step 1

### Step 4: CHANGELOG Update

If changes include user-facing modifications:

1. Add entry under `## [Unreleased]` section
2. Use correct subsection: `### Added`, `### Fixed`, `### Changed`, `### Removed`
3. Reference issue numbers where applicable: `[#NNN](https://github.com/wallstop/unity-helpers/issues/NNN)`
4. Keep entries concise — one line per change

### Step 5: Documentation Check

Per [update-documentation](./update-documentation.md):

1. Public API changes have XML doc comments
2. README updated if public-facing behavior changed
3. Skill files updated if workflow changed
4. `.meta` files exist for all new assets

### Step 6: Version Assessment

Assess whether version bump is needed (do NOT bump automatically — note for human):

| Change Type                      | Version Impact        |
| -------------------------------- | --------------------- |
| Bug fix, no API change           | Patch (3.2.1 → 3.2.2) |
| New feature, backward compatible | Minor (3.2.1 → 3.3.0) |
| Breaking API change              | Major (3.2.1 → 4.0.0) |
| Internal refactor only           | No bump needed        |

Report assessment but do not modify `package.json` version without explicit approval.

### Step 7: Commit Hygiene

Ensure commits are bisectable:

| Rule                      | Description                                              |
| ------------------------- | -------------------------------------------------------- |
| **Each commit compiles**  | No commit should leave the project in a broken state     |
| **Each commit is atomic** | One logical change per commit                            |
| **Message format**        | Imperative mood, <72 chars first line, body explains why |
| **No fixup commits**      | Squash "fix typo" commits into the original              |

### Step 8: Ship Summary

Output a final summary:

```text
Ship Summary:
  Pre-flight: PASS
  Tests: PASS | N/A (requires Unity Editor)
  Review: PASS (risk score: N/25)
  CHANGELOG: Updated | Not needed
  Documentation: Updated | Not needed
  Version: No bump needed | Recommend PATCH/MINOR/MAJOR
  Commits: N commits, all bisectable
  Ready to merge: YES | NO (blockers: list)
```

---

## Related Skills

- [review-code-changes](./review-code-changes.md) - Pre-landing review (Step 3)
- [self-regulate-changes](./self-regulate-changes.md) - Risk scoring during review
- [validate-before-commit](./validate-before-commit.md) - Pre-flight checks (Step 1)
- [update-documentation](./update-documentation.md) - Documentation check (Step 5)
- [apply-completeness](./apply-completeness.md) - Don't ship incomplete work
