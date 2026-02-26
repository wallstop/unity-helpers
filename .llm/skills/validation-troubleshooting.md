# Skill: Validation Troubleshooting

<!-- trigger: error, ci, failure, troubleshoot, fix, dead link, lychee, broken link | Common validation errors, CI failures, fixes | Core -->

**Trigger**: When you encounter validation errors, CI failures, or linting issues.

For the quick validation workflow, see [validate-before-commit](./validate-before-commit.md).
For detailed linter commands, see [linter-reference](./linter-reference.md).

---

## Most Common CI Failures

### 1. Spelling Errors (Most Frequent)

**Symptom**: `npm run lint:spelling` fails

**Fix Options**:

1. **Correct the spelling** if it's actually wrong
2. **Add to dictionary** — add the word to `cspell.json` `"words"` array
3. **Inline ignore** for single occurrences: `<!-- cspell:ignore someword -->`

### 2. Prettier Formatting Failures

**Symptom**: `format:md:check`, `format:json:check`, or `format:yaml:check` fails

**Fix**: Run Prettier to auto-fix:

```bash
npx prettier --write -- <file>
# Or fix all:
npx prettier --write -- .
```

**Common gotchas**:

- **Missing final newline**: Prettier requires files to end with a newline. Fix with `npx prettier --write -- <file>` or `printf '\n' >> <file>`
- **devcontainer.json**: Arrays within `printWidth: 100` get collapsed. Fix with `npx prettier --write -- .devcontainer/devcontainer.json`
- **dotnet-tools.json**: LF line endings from Linux. Fix with `npm run format:json -- .config/dotnet-tools.json`; if persists, run `pwsh -NoProfile -File scripts/normalize-eol.ps1 -VerboseOutput`

### 3. Markdownlint Violations

**Symptom**: `npm run lint:markdown` fails

**Common fixes**:

| Error Code | Issue                    | Fix                                  |
| ---------- | ------------------------ | ------------------------------------ |
| MD007      | Wrong list indentation   | Use 2 spaces for nested lists        |
| MD009      | Trailing whitespace      | Remove trailing spaces               |
| MD012      | Multiple blank lines     | Reduce to single blank line          |
| MD022      | No blank around headings | Add blank line before/after headings |
| MD032      | No blank around lists    | Add blank line before/after lists    |

### 4. Backtick File Reference Errors

**Symptom**: `npm run lint:docs` fails with backtick reference warning

**Fix**: Use proper links instead of backtick-wrapped filenames:

```markdown
<!-- ❌ WRONG -->

See `context.md` for guidelines.

<!-- ✅ CORRECT -->

See [context](./context.md) for guidelines.
```

### 5. Link Without Relative Prefix

**Symptom**: `npm run lint:docs` fails with relative path warning

```markdown
<!-- ❌ WRONG -->

[create-test](create-test.md)

<!-- ✅ CORRECT -->

[create-test](./create-test.md)
```

### 6. Broken Internal Links

**Symptom**: `npm run lint:docs` fails with "file not found"

**Fix**: Verify file exists and path is correct — check for typos, moved/renamed files, or wrong prefix.

### 7. Missing Track() in Tests

**Symptom**: `npm run validate:tests` fails

**Fix**: Wrap Unity object creation with `Track()`. See [UnityObjectLifecycleTests.cs](../code-samples/testing/UnityObjectLifecycleTests.cs) for complete examples.

### 8. C# Naming Convention Violations

**Symptom**: `npm run lint:csharp-naming` fails

**Fix**: Methods use PascalCase (`ProcessData`), private fields use underscore prefix (`_count`), public members use PascalCase without underscore.

### 9. Line Ending Issues

**Symptom**: `npm run eol:check` fails

**Fix**: `npm run eol:fix`

**Mixed endings after newline fix**: If a script appended LF to a CRLF file, detect existing endings first. See [`crlf_aware_append_newline`](../code-samples/patterns/ValidationFixPatterns.sh) and [git-hook-patterns](./git-hook-patterns.md#crlf-aware-newline-handling) for patterns.

**PowerShell `-NoNewline`**: Avoid `Set-Content -NoNewline` — it removes the final newline Prettier requires.

### 10. Pre-Commit Hooks Not Catching CI Failures

**Symptom**: CI fails on issues hooks should have caught locally.

**Cause**: Hook files in `.githooks/` are not executable.

**Fix**: See [`fix_hook_permissions`](../code-samples/patterns/ValidationFixPatterns.sh) for the full sequence, or run:
`chmod +x .githooks/* && git update-index --chmod=+x .githooks/pre-commit .githooks/pre-push`

### 11. Dead Link Failures (External URLs)

**Symptom**: `Check dead links (lychee)` step fails in CI

**Important**: Lychee only scans `.md` files, not `.cs` source files.

**Investigation**: Check CI output for the failing URL, verify manually, then apply the fix:

| Failure Type             | Fix                                          |
| ------------------------ | -------------------------------------------- |
| HTTP to HTTPS redirect   | Update URL to use `https://`                 |
| Domain migration         | Update to new domain                         |
| Permanently defunct site | Add regex to `.lychee.toml` exclude list     |
| Bot protection / 403     | Add to `.lychee.toml` exclude list           |
| Transient 5xx error      | Already handled in `.lychee.toml`            |
| URL in source code only  | No CI action needed (consider updating docs) |

**Adding exclusions to `.lychee.toml`** (at repo root):

```toml
exclude = [
  # Site permanently offline (reason)
  "^https?://defunct-site\\.example\\.com"
]
```

When updating URLs, check consistency between source code metadata (`.cs` files) and documentation (`.md` files).

---

## Debugging Failed CI Runs

1. **Check which check failed** in GitHub Actions output:
   - `validate:content` — Documentation/formatting
   - `lint:csharp-naming` — C# naming convention
   - `eol:check` — Line endings
   - `validate:tests` — Test lifecycle

2. **Reproduce locally**: `npm run validate:prepush`

3. **Fix and verify**: Fix the issue, run the specific command, then run `npm run validate:prepush`

---

## Special Cases

### Pre-Existing Warnings

Check if a warning exists on main before fixing (see [`check_preexisting`](../code-samples/patterns/ValidationFixPatterns.sh)). If it exists on main, it's pre-existing and doesn't block your PR.

### Conflicts Between Linters

Resolution priority: **Prettier** (formatting) > **Markdownlint** (structure) > **CSpell** (spelling — always fix or add to dictionary).

### Files That Should Be Ignored

Add files to `.prettierignore`, `.markdownlintignore`, or `cspell.json` `ignorePaths` as appropriate.

---

## Quick Recovery Commands

See [`quick_recovery`](../code-samples/patterns/ValidationFixPatterns.sh) for the full script, or run individually:
`npx prettier --write -- .` | `npm run eol:fix` | `dotnet tool run csharpier format .` | `npm run validate:prepush`

---

## Related Skills

- [validate-before-commit](./validate-before-commit.md) — Quick validation workflow
- [linter-reference](./linter-reference.md) — Detailed linter commands
- [formatting](./formatting.md) — CSharpier, Prettier, markdownlint workflow
- [markdown-reference](./markdown-reference.md) — Link formatting, structural rules
