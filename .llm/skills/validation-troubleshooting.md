# Skill: Validation Troubleshooting

<!-- trigger: error, ci, failure, troubleshoot, fix, dead link, lychee, broken link | Common validation errors, CI failures, fixes | Core -->

**Trigger**: When you encounter validation errors, CI failures, or linting issues.

---

## When to Use

Use this guide when you encounter:

- CI/CD pipeline failures
- Linter error messages you need to fix
- Spelling or formatting issues
- Link validation failures (internal or external/dead links)
- Test lifecycle violations

For the quick validation workflow, see [validate-before-commit](./validate-before-commit.md).
For detailed linter commands, see [linter-reference](./linter-reference.md).

---

## Most Common CI Failures

### 1. Spelling Errors (Most Frequent)

**Symptom**: `npm run lint:spelling` fails

**Fix Options**:

1. **Correct the spelling** if it's actually wrong
2. **Add to dictionary** if it's a valid technical term:

   ```bash
   # Add to cspell.json
   {
     "words": [
       "PRNG",
       "Sirenix",
       "MonoBehaviour"
     ]
   }
   ```

3. **Inline ignore** for single occurrences:

```markdown
<!-- cspell:ignore someword -->
```

### 2. Prettier Formatting Failures

**Symptom**: `format:md:check`, `format:json:check`, or `format:yaml:check` fails

**Fix**: Run Prettier to auto-fix:

```bash
npx prettier --write -- <file>

# Or fix all:
npx prettier --write -- .
```

**Common gotcha (missing final newline)**: If `format:json:check` fails on `package.json` or another file with a diff showing only `\ No newline at end of file`, the file is missing a trailing newline. Prettier requires files to end with a newline character. Fix with:

```bash
# Auto-fix with Prettier
npx prettier --write -- <file>

# Or use the validate-formatting script with --fix
./scripts/validate-formatting.sh --fix

# Or manually add a newline
printf '\n' >> <file>
```

**Prevention**: The pre-commit hook (step 5) auto-fixes missing final newlines on staged text files. Run `npm run test:final-newline` to check all tracked files. Editors with `insert_final_newline = true` in `.editorconfig` also help (though this repo currently sets it to `false` for most files).

**Common gotcha (devcontainer.json)**: If `format:json:check` fails on `.devcontainer/devcontainer.json`, the file was likely edited (e.g., adding extensions, features, or updating settings) without running prettier. Arrays that fit within `printWidth: 100` get collapsed to single lines. Fix with:

```bash
npx prettier --write -- .devcontainer/devcontainer.json
```

**Common gotcha (dotnet tools manifest)**: If `format:json:check` fails on `.config/dotnet-tools.json`, the file usually has LF line endings from a Linux update step. Fix with:

```bash
npm run format:json -- .config/dotnet-tools.json
```

If the issue persists, normalize line endings:

```bash
pwsh -NoProfile -File scripts/normalize-eol.ps1 -VerboseOutput
```

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

**Cause**: Using backtick-wrapped filenames instead of proper links

```markdown
<!-- ❌ WRONG -->

See `context.md` for guidelines.
Update `CHANGELOG.md` with changes.

<!-- ✅ CORRECT -->

See [context](./context.md) for guidelines.
Update [CHANGELOG](../CHANGELOG.md) with changes.
```

### 5. Link Without Relative Prefix

**Symptom**: `npm run lint:docs` fails with relative path warning

```markdown
<!-- ❌ WRONG -->

[create-test](create-test.md)
[context](skills/context.md)

<!-- ✅ CORRECT -->

[create-test](./create-test.md)
[context](./skills/context.md)
```

### 6. Broken Internal Links

**Symptom**: `npm run lint:docs` fails with "file not found"

**Causes**:

- Typo in filename
- File was moved or renamed
- Wrong path prefix

**Fix**: Verify the file exists and path is correct:

```bash
ls -la <path-to-file>
```

### 7. Missing Track() in Tests

**Symptom**: `npm run validate:tests` fails

**Cause**: Unity objects created without Track():

```csharp
// ❌ WRONG
GameObject obj = new GameObject("Test");
MyComponent comp = obj.AddComponent<MyComponent>();

// ✅ CORRECT
GameObject obj = Track(new GameObject("Test"));
MyComponent comp = Track(obj.AddComponent<MyComponent>());
```

### 8. C# Naming Convention Violations

**Symptom**: `npm run lint:csharp-naming` fails

**Common issues**:

```csharp
// ❌ WRONG: lowercase method
public void processData() { }

// ✅ CORRECT
public void ProcessData() { }

// ❌ WRONG: missing underscore on private field
private int count;

// ✅ CORRECT
private int _count;
```

### 9. Line Ending Issues

**Symptom**: `npm run eol:check` fails

**Fix**:

```bash
npm run eol:fix
```

### 10. Dead Link Failures (External URLs)

**Symptom**: `Check dead links (lychee)` step fails in CI

**Cause**: External URL in a `.md` file is broken, redirects, or times out.

**Important**: Lychee only scans `.md` files. URLs in `.cs` source files are NOT checked.

#### Investigation Process

1. **Check the CI output** — Lychee reports which file and URL failed
2. **Verify the URL manually** — Open in browser, check for redirects
3. **Identify the failure type** and apply the appropriate fix:

| Failure Type             | Example                                                 | Fix                                                             |
| ------------------------ | ------------------------------------------------------- | --------------------------------------------------------------- |
| HTTP to HTTPS redirect   | `http://example.com` redirects to `https://example.com` | Update URL to use `https://`                                    |
| Domain migration         | `xoshiro.di.unimi.it` moved to `prng.di.unimi.it`       | Update to new domain                                            |
| Permanently defunct site | Academic site went offline                              | Add regex to `.lychee.toml` exclude list                        |
| GitHub repo deleted      | Third-party repo removed                                | Add to `.lychee.toml` exclude list                              |
| Transient server error   | 5xx errors                                              | Already handled — `.lychee.toml` accepts 5xx                    |
| Bot protection           | Site returns 403 for automated requests                 | Add to `.lychee.toml` exclude list                              |
| URL in source code only  | URL in `.cs` file metadata but not in docs              | No action needed for CI (but consider updating for consistency) |

#### Common Fix Patterns

**HTTP to HTTPS upgrade:**

```markdown
<!-- Before -->

<http://example.com/resource>

<!-- After -->

<https://example.com/resource>
```

**Domain migration:**

```markdown
<!-- Before (old domain) -->

<https://xoshiro.di.unimi.it>

<!-- After (new domain) -->

<https://prng.di.unimi.it>
```

**Add permanently defunct site to `.lychee.toml`:**

```toml
exclude = [
  # ... existing exclusions ...
  # Site permanently offline (reason)
  "^https?://defunct-site\\.example\\.com"
]
```

#### Source Code and Documentation Consistency

When updating URLs, check for consistency between:

1. **Source code metadata** — Attribution comments, XML docs in `.cs` files
2. **Documentation** — References in `.md` files
3. **Auto-generated docs** — Files in `docs/features/` that are generated from source metadata

URLs in source code are not checked by lychee, but inconsistent URLs between source and docs create confusion.

#### Quick Reference: `.lychee.toml` Location

The lychee configuration is at the repository root: `.lychee.toml`

Current exclusion categories:

- Local/test URLs (localhost, 127.0.0.1)
- Sites with bot protection (npmjs.com, doi.org)
- Known flaky sites (bugs.python.org)
- Defunct sites (wiki.unity3d.com, grepcode.com)
- Offline GitHub repositories

---

## Debugging Failed CI Runs

### Step 1: Check Which Check Failed

Look at the GitHub Actions output. Each job has a name indicating what failed:

- `validate:content` — Documentation/formatting issue
- `lint:csharp-naming` — C# naming convention
- `eol:check` — Line endings
- `validate:tests` — Test lifecycle issue

### Step 2: Reproduce Locally

```bash
# Run the exact same check locally
npm run validate:prepush
```

### Step 3: Fix and Verify

1. Fix the issue
2. Run the specific failing command
3. Run full validation: `npm run validate:prepush`

---

## Special Cases

### Pre-Existing Warnings

Some warnings may exist in the main branch. To determine if a warning is pre-existing:

```bash
# Check if warning exists on main
git stash
git checkout main
npm run <failing-command>
git checkout -
git stash pop
```

If the warning exists on main, it's pre-existing and doesn't block your PR.

### Conflicts Between Linters

Occasionally linters may conflict. Resolution priority:

1. **Prettier** for formatting (it wins on spacing, line breaks)
2. **Markdownlint** for structure (it wins on heading levels, list structure)
3. **CSpell** for spelling (always fix or add to dictionary)

### Files That Should Be Ignored

If a file shouldn't be linted, check if it should be in:

- `.prettierignore` — Skip Prettier formatting
- `.markdownlintignore` — Skip markdown linting
- `cspell.json` ignorePaths — Skip spell checking

---

## Error Message Reference

### Prettier Errors

| Message                       | Meaning             | Fix                  |
| ----------------------------- | ------------------- | -------------------- |
| "Code style issues found"     | Formatting mismatch | Run `--write`        |
| "Unexpected identifier"       | Invalid syntax      | Check file syntax    |
| "No parser could be inferred" | Unknown file type   | Check file extension |

### Markdownlint Errors

| Rule  | Description                   | Quick Fix                           |
| ----- | ----------------------------- | ----------------------------------- |
| MD001 | Heading levels increment by 1 | Don't skip heading levels           |
| MD007 | Unordered list indentation    | Use 2 spaces                        |
| MD009 | Trailing spaces               | Delete trailing whitespace          |
| MD010 | Hard tabs                     | Convert to spaces                   |
| MD011 | Reversed link syntax          | Use `[text](url)` not `(url)[text]` |
| MD012 | Multiple blank lines          | Reduce to single blank              |
| MD022 | Headings blank lines          | Add blank before/after heading      |
| MD023 | Headings start at line start  | Remove leading spaces               |
| MD031 | Fenced code blank lines       | Add blank before/after fence        |
| MD032 | Lists blank lines             | Add blank before/after list         |
| MD034 | Bare URL                      | Use `<url>` or `[text](url)`        |
| MD037 | Spaces inside emphasis        | Remove `** text **`                 |
| MD038 | Spaces inside code            | Remove `` ` code ` ``               |
| MD039 | Spaces inside links           | Remove `[ text ](url)`              |
| MD040 | Fenced code no language       | Add language after ` ``` `          |
| MD047 | No newline at end of file     | Add trailing newline                |

### CSpell Errors

| Message           | Meaning              | Fix                               |
| ----------------- | -------------------- | --------------------------------- |
| "Unknown word"    | Not in dictionary    | Fix spelling or add to dictionary |
| "Multiple errors" | Several misspellings | Fix each one individually         |

### C# Naming Errors

| Pattern             | Expected             | Example Fix                   |
| ------------------- | -------------------- | ----------------------------- |
| `method_name`       | `MethodName`         | PascalCase                    |
| `processData`       | `ProcessData`        | Capitalize first letter       |
| `private int count` | `private int _count` | Add underscore prefix         |
| `public int _value` | `public int Value`   | Remove underscore, capitalize |

---

## Quick Recovery Commands

```bash
# Fix ALL Prettier formatting
npx prettier --write -- .

# Fix line endings
npm run eol:fix

# Format C#
dotnet tool run csharpier format .

# Full validation
npm run validate:prepush
```

---

## Related Skills

- [validate-before-commit](./validate-before-commit.md) — Quick validation workflow
- [linter-reference](./linter-reference.md) — Detailed linter commands
- [formatting](./formatting.md) — CSharpier, Prettier, markdownlint workflow
- [markdown-reference](./markdown-reference.md) — Link formatting, structural rules
