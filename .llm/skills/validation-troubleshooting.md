# Skill: Validation Troubleshooting

<!-- trigger: error, ci, failure, troubleshoot, fix, dead link, lychee, broken link, transient, network, timeout, exclusion | Common validation errors, CI failures, fixes | Core -->

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

#### Diagnosing Link Failures

1. **Check CI output** for the failing URL and HTTP status code
2. **Verify manually** — open the URL in a browser from different networks
3. **Determine failure type** — transient (retry works) or permanent (site down/moved)

| Symptom                                | Type      | Evidence                                               |
| -------------------------------------- | --------- | ------------------------------------------------------ |
| Works in browser, fails in CI          | Transient | GitHub Actions runners have network restrictions       |
| 5xx errors that succeed on retry       | Transient | Server overload, temporary outage                      |
| Timeout with no response               | Transient | Network routing issues from specific datacenters       |
| 403/404 consistently across networks   | Permanent | Bot protection or content removed                      |
| Domain no longer resolves              | Permanent | Site shut down                                         |
| Redirects to different domain/homepage | Permanent | Content restructured, URL changed                      |
| Root domain works, specific paths fail | Transient | Academic/research sites with inconsistent availability |

#### Fix Strategies

| Failure Type                       | Fix                                              |
| ---------------------------------- | ------------------------------------------------ |
| HTTP to HTTPS redirect             | Update URL to use `https://`                     |
| Domain migration                   | Update to new domain                             |
| Permanently defunct site           | Add regex to `.lychee.toml` exclude list         |
| Bot protection / 403               | Add to `.lychee.toml` exclude list               |
| Transient 5xx error                | Already handled in `.lychee.toml` accept ranges  |
| Transient timeout (academic sites) | Add specific path to `.lychee.toml` exclude list |
| URL in source code only            | No CI action needed (consider updating docs)     |

#### When to Use Exclusions vs Update Links

**Use exclusions** when:

- Link is valid but site has bot protection (returns 403 to automated checks)
- Site is flaky but content is correct (academic sites, small servers)
- Transient network issues from GitHub Actions runners specifically
- Site returns errors but link is the canonical/correct reference

**Update documentation links** when:

- Content has moved to a new URL
- A better/more authoritative source exists
- Original site is permanently offline

#### Adding Exclusions to .lychee.toml

The configuration file is at repository root. Use regex patterns:

```toml
exclude = [
  # Academic sites with intermittent connectivity from GitHub Actions runners
  # Root domains work but specific paths timeout inconsistently
  "^https?://www\\.example-academic\\.org/paper\\.html",

  # Site permanently offline (reason)
  "^https?://defunct-site\\.example\\.com",

  # Bot protection (403 but link is valid)
  "^https?://protected-site\\.com"
]
```

**Best practices for exclusions**:

- Add a comment explaining WHY the exclusion is needed
- Use domain-level exclusions for network/connectivity issues (timeouts, unreachable)
- Use specific paths when only certain content paths fail consistently
- Escape dots in domain names (`\\.`)
- Use `^https?://` to match both HTTP and HTTPS
- Consider referencing the GitHub issue where the failure was investigated

#### Network Tuning in .lychee.toml

For transient failures, the config already includes:

```toml
timeout = 30            # seconds per request (increased for slow servers)
max_retries = 5         # retry transient failures
retry_wait_time = 3     # seconds between retries
accept = ["200..=299", "429", "500..=599"]  # Accept server errors as transient
```

If a site fails despite these settings, it likely needs an exclusion.

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
