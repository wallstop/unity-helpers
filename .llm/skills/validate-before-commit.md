# Skill: Validate Before Commit

**Trigger**: **MANDATORY** before completing any task that modifies code or documentation.

---

## Quick Reference

```bash
# Run all validations before completing any task
npm run validate:prepush
```

This single command runs ALL CI/CD checks locally, ensuring your changes will pass in GitHub Actions.

---

## When to Run

**ALWAYS run validation before:**

- Completing any coding task
- Asking user to review changes
- Before any discussion of "done" or "complete"
- After making ANY modifications to files

---

## MANDATORY: Run Linters IMMEDIATELY After Every Change

> **⚠️ CRITICAL**: Do NOT wait until the end of a task to run linters. Run the appropriate linter IMMEDIATELY after modifying ANY file. Fix issues before moving to the next file.

### Linter Commands by File Type

| File Type Changed         | Command to Run IMMEDIATELY                     | Notes                                          |
| ------------------------- | ---------------------------------------------- | ---------------------------------------------- |
| Documentation (`.md`)     | `npx prettier --write <file>`                  | **MANDATORY** — Run FIRST after any edit       |
| Documentation (`.md`)     | `npm run lint:spelling`                        | Add valid terms to `cspell.json` if needed     |
| Documentation (`.md`)     | `npm run lint:docs`                            | Check for broken links, backtick `.md` refs    |
| Documentation (`.md`)     | `npm run lint:markdown`                        | Markdownlint rules (MD032, MD009, etc.)        |
| JSON/asmdef/asmref        | `npx prettier --write <file>`                  | **MANDATORY** — Prettier formats JSON too      |
| YAML (non-workflow)       | `npx prettier --write <file>`                  | **MANDATORY** — Prettier formats YAML too      |
| GitHub Workflows (`.yml`) | `npx prettier --write <file>`                  | Format FIRST, then run actionlint              |
| GitHub Workflows (`.yml`) | `actionlint`                                   | **MANDATORY** for `.github/workflows/*.yml`    |
| C# code (`.cs`)           | `dotnet tool run csharpier format .`           | Auto-fix formatting (CSharpier, not Prettier)  |
| C# code (`.cs`)           | `npm run lint:csharp-naming`                   | Check for underscore violations                |
| Test files (`.cs`)        | `pwsh -NoProfile -File scripts/lint-tests.ps1` | **MANDATORY** Track() usage, no manual destroy |

### Prettier/Markdown Formatting

> **⚠️ CRITICAL**: Pre-push hooks will REJECT commits with Prettier formatting issues. Run Prettier IMMEDIATELY after editing ANY non-C# file.

**Prettier applies to ALL of these file types** (not just C#):

- Markdown (`.md`)
- JSON (`.json`, `.asmdef`, `.asmref`)
- YAML (`.yml`, `.yaml`)
- Config files

```bash
# Fix a specific file IMMEDIATELY after editing
npx prettier --write <file>

# Examples:
npx prettier --write .llm/skills/create-test.md
npx prettier --write package.json
npx prettier --write .github/workflows/ci.yml

# Verify ALL files before commit/push
npx prettier --check .

# Auto-fix ALL files at once
npx prettier --write .
```

**Key distinction:**

| File Type       | Formatter | Command                              |
| --------------- | --------- | ------------------------------------ |
| C# (`.cs`)      | CSharpier | `dotnet tool run csharpier format .` |
| Everything else | Prettier  | `npx prettier --write <file>`        |

---

### Documentation Changes Workflow

After **ANY** change to markdown files:

```bash
# 1. Format with Prettier FIRST (pre-push hook requirement)
npx prettier --write <file>

# 2. Check spelling (most common failure)
npm run lint:spelling

# 3. If spelling errors found with valid technical terms, add to cspell.json:
# Edit cspell.json and add terms to the "words" array

# 4. Check links and formatting
npm run lint:docs
npm run lint:markdown
```

**Proactive Spelling Management**:

- When adding technical terms (class names, method names, package names), **proactively add them to `cspell.json`** BEFORE running the linter
- Common terms to add: Unity API names, package identifiers, custom type names, acronyms
- Keep the `words` array in `cspell.json` sorted alphabetically

### Workflow Changes Workflow

After **ANY** change to `.github/workflows/*.yml`:

```bash
# MANDATORY - run actionlint immediately
actionlint

# Fix ALL errors before committing
# Common issues: SC2129 (grouped redirects), missing config-name, etc.
```

### C# Changes Workflow

After **ANY** change to `.cs` files:

```bash
# 1. Auto-format code
dotnet tool run csharpier format .

# 2. Check naming conventions
npm run lint:csharp-naming
```

### Test File Changes Workflow (MANDATORY)

After **ANY** change to test files in `Tests/`:

```bash
# MANDATORY - check test lifecycle patterns
pwsh -NoProfile -File scripts/lint-tests.ps1
```

**What the lint checks**:

| Rule     | Description                                                            |
| -------- | ---------------------------------------------------------------------- |
| `UNH001` | No manual `DestroyImmediate`/`Destroy` — use `Track()` for cleanup     |
| `UNH002` | All Unity object allocations must be wrapped with `Track()`            |
| `UNH003` | Test classes creating Unity objects must inherit from `CommonTestBase` |

**How to fix**:

```csharp
// ❌ UNH001 violation - manual destroy
Editor editor = Editor.CreateEditor(target);
try { ... }
finally { UnityEngine.Object.DestroyImmediate(editor); }

// ✅ Correct - Track() handles cleanup
Editor editor = Track(Editor.CreateEditor(target));
// ... test code (no try-finally needed)

// For intentional destroy tests, add UNH-SUPPRESS comment:
UnityEngine.Object.DestroyImmediate(target); // UNH-SUPPRESS: Test verifies destroy behavior
_trackedObjects.Remove(target);
```

See [create-test](create-test.md#unity-object-lifecycle-management-critical) for detailed patterns.

### The "Fix Before Moving On" Rule

1. **Make a change** to a file
2. **Run the appropriate linter(s)** for that file type
3. **Fix any issues** found
4. **Only then** move to the next file or task

**NEVER:**

- Accumulate multiple file changes before running linters
- Say "I'll fix linting at the end" — issues compound and become harder to debug
- Commit files that haven't been linted
- Assume previous code was correct — always verify after your changes

---

## CRITICAL: Markdown Link Formatting

**NEVER use backtick-wrapped `.md` file references.** This is a MANDATORY rule.

```markdown
<!-- ❌ WRONG: Will FAIL lint-doc-links -->

See `context.md` for guidelines.
Refer to `skills/create-test.md` for details.

<!-- ✅ CORRECT: Proper markdown links -->

See [context](context.md) for guidelines.
Refer to [create-test](skills/create-test.md) for details.
```

**The `npm run lint:docs` check will FAIL if backtick-wrapped `.md` references are found.**

---

## What Gets Validated

The `npm run validate:prepush` command runs these checks in order:

1. **validate:content** — Documentation and formatting
   - `lint:docs` — **CRITICAL**: Check markdown links (no backtick `.md` refs!)
   - `lint:markdown` — Markdownlint rules
   - `format:md:check` — Prettier markdown formatting
   - `format:json:check` — Prettier JSON/asmdef formatting
   - `format:yaml:check` — Prettier YAML formatting

2. **eol:check** — Line endings
   - Ensures CRLF line endings
   - No BOM markers

3. **validate:tests** — Test lifecycle lint
   - Unity object tracking in tests
   - CommonTestBase inheritance

4. **lint:csharp-naming** — C# naming conventions
   - No underscores in method names
   - PascalCase for all methods

**Note**: GitHub Actions workflows are validated by the separate `actionlint` CI job. See [GitHub Actions Workflow Linting](#github-actions-workflow-linting-mandatory) for local validation.

---

## Individual Check Commands

### C# Formatting (CSharpier)

```bash
# Check formatting (will fail if changes needed)
dotnet tool run csharpier check .

# Auto-fix formatting
dotnet tool run csharpier format .
```

### Markdown Formatting

```bash
# Check markdown formatting
npm run format:md:check

# Auto-fix markdown formatting
npm run format:md
```

### Markdownlint

```bash
# Check markdown lint rules
npm run lint:markdown
```

Common fixes:

- **MD032**: Add blank line before and after lists
- **MD009**: Remove trailing spaces
- **MD022**: Add blank line after headings
- **MD031**: Add blank line around fenced code blocks
- **MD040**: Add language specifier to fenced code blocks (`csharp`, `bash`, `text`)
- **MD036**: Don't use emphasis (bold/italic) as headings - use proper `#` headings

### Fenced Code Block Language Specifiers

**ALL fenced code blocks MUST have a language specifier.** Common specifiers:

| Content Type      | Specifier    |
| ----------------- | ------------ |
| C# code           | `csharp`     |
| Shell commands    | `bash`       |
| PowerShell        | `powershell` |
| JSON              | `json`       |
| YAML              | `yaml`       |
| XML               | `xml`        |
| Plain text/output | `text`       |
| Markdown examples | `markdown`   |

### JSON/asmdef/asmref Formatting

```bash
# Check JSON formatting
npm run format:json:check

# Auto-fix JSON formatting
npm run format:json
```

### YAML Formatting

```bash
# Check YAML formatting
npm run format:yaml:check

# Auto-fix YAML formatting
npm run format:yaml
```

### C# Naming Conventions

```bash
# Check for underscore violations
npm run lint:csharp-naming
```

**Rules:**

- NO underscores in method names (including tests)
- Use PascalCase: `WhenInputIsNullReturnsDefault` NOT `When_Input_Is_Null_Returns_Default`
- Test data names: Use dots `TestCase.Scenario.Expected` NOT underscores

### Documentation Links (MANDATORY)

```bash
# Check for broken links in docs - MUST PASS before completing any doc task
npm run lint:docs

# For verbose output showing all checks:
pwsh ./scripts/lint-doc-links.ps1 -VerboseOutput
```

**This check catches:**

- Broken links to non-existent files
- Backtick-wrapped markdown file references (FORBIDDEN)
- Invalid anchor links
- Malformed markdown link syntax

**Common failures and fixes:**

| Error Type                               | Fix                                             |
| ---------------------------------------- | ----------------------------------------------- |
| Backtick-wrapped markdown file reference | Use `[readable-name](path/to/file)` link format |
| Link target does not resolve             | Verify file exists and path is correct          |
| Bare markdown mention without link       | Convert to proper `[text](target)` link         |

### End-of-Line Characters

```bash
# Check EOL characters
npm run eol:check

# Auto-fix EOL characters
npm run eol:fix
```

---

## CI/CD Check Mapping

| GitHub Action Workflow       | Local Command                       | Auto-Fix Command                     |
| ---------------------------- | ----------------------------------- | ------------------------------------ |
| CSharpier Auto Format        | `dotnet tool run csharpier check .` | `dotnet tool run csharpier format .` |
| Prettier Auto Fix (Markdown) | `npm run format:md:check`           | `npm run format:md`                  |
| Prettier Auto Fix (JSON)     | `npm run format:json:check`         | `npm run format:json`                |
| Prettier Auto Fix (YAML)     | `npm run format:yaml:check`         | `npm run format:yaml`                |
| Markdown & JSON Lint/Format  | `npm run validate:content`          | Run individual fix commands          |
| YAML Format + Lint           | `npm run format:yaml:check`         | `npm run format:yaml`                |
| C# Naming Convention Lint    | `npm run lint:csharp-naming`        | Rename methods manually              |
| Lint Docs Links              | `npm run lint:docs`                 | Fix broken links manually            |
| **actionlint**               | `actionlint`                        | Fix shell/YAML errors manually       |

---

## Common Failures and Solutions

### CSharpier Formatting Failed

```text
Error ./Editor/Utils/WButton/WButtonEditorHelper.cs - Was not formatted.
```

**Fix:**

```bash
dotnet tool run csharpier format .
```

### Markdownlint MD032 (Blanks Around Lists)

```text
docs/features/inspector/inspector-button.md:1154 MD032/blanks-around-lists
```

**Fix:** Add a blank line before and after the list:

```markdown
<!-- WRONG -->

Text before list:

- Item 1
- Item 2
  Text after list

<!-- CORRECT -->

Text before list:

- Item 1
- Item 2

Text after list
```

### C# Naming Lint (Underscores in Method Names)

```text
UNH004: Method name 'When_Input_Is_Null' contains underscore(s).
```

**Fix:** Rename method to PascalCase without underscores:

```csharp
// WRONG
public void When_Input_Is_Null_Returns_Default() { }

// CORRECT
public void WhenInputIsNullReturnsDefault() { }
```

### Prettier Markdown Check Failed

```text
Checking formatting...
[warn] docs/features/inspector/inspector-button.md
```

**Fix:**

```bash
npm run format:md
```

### EOL Check Failed

```text
LF issues: 3
Files with BOM: 1
```

**Fix:**

```bash
npm run eol:fix
```

---

## Full Validation Workflow

After completing any task:

```bash
# 1. Format all code and docs
dotnet tool run csharpier format .
npm run format:md
npm run format:json
npm run format:yaml

# 2. Run full validation
npm run validate:prepush

# 3. If validation passes, task is ready for review
# 4. If validation fails, fix issues and re-run
```

---

## Documentation Checklist

Before completing ANY task that adds features or fixes bugs, verify:

### For New Features

- [ ] Feature documentation added/updated in `docs/features/<category>/`
- [ ] XML documentation on all public types/members
- [ ] At least one working code sample in docs
- [ ] CHANGELOG entry in `### Added` section under `## [Unreleased]`
- [ ] llms.txt updated if feature adds new capabilities

### For Bug Fixes

- [ ] CHANGELOG entry in `### Fixed` section under `## [Unreleased]`
- [ ] Documentation corrected if it described wrong behavior

### For API Changes

- [ ] All documentation referencing old API updated
- [ ] CHANGELOG entry (in `### Changed` section, marked Breaking if applicable)
- [ ] XML docs updated with new parameter names/types
- [ ] Code samples updated throughout docs

See [update-documentation](update-documentation.md) for complete guidelines.

---

## Pre-Existing Warnings

Some lint warnings may exist in the main branch (e.g., test lifecycle warnings in `validate:tests`). Focus on:

1. **New warnings** introduced by your changes
2. **Failing checks** (exit code 1)

If `validate:content` and `lint:csharp-naming` pass, your changes are ready.

---

## Troubleshooting

### npm Command Not Found

```bash
npm install  # Install dependencies
```

### dotnet Tool Not Found

```bash
dotnet tool restore  # Restore .NET tools
```

### PowerShell Script Errors

Ensure you're running in a shell that supports PowerShell:

```bash
pwsh -NoProfile -File scripts/check-eol.ps1
```

---

## GitHub Actions Workflow Linting (MANDATORY)

> **⚠️ CRITICAL**: CI/CD failures caused by workflow syntax errors are expensive to debug. ALWAYS validate workflows locally before committing!

**ALWAYS run actionlint after ANY changes to `.github/workflows/*.yml` files.** The CI pipeline runs actionlint and will fail if errors are found.

### Why This Matters

- **Workflow syntax errors only surface at runtime** — You won't know about issues until CI fails
- **Missing required parameters** cause cryptic error messages in GitHub Actions logs
- **Security issues** (hardcoded secrets, injection vulnerabilities) are caught early
- **Saves time** — Local validation takes seconds vs. waiting for CI to fail

### Running actionlint Locally

```bash
# Install actionlint (one-time setup in dev container)
curl -sfL https://raw.githubusercontent.com/rhysd/actionlint/main/scripts/download-bash.sh | bash -s -- -b /usr/local/bin

# Run actionlint on all workflow files (ALWAYS DO THIS)
actionlint

# Run on a specific workflow file
actionlint .github/workflows/specific-workflow.yml

# Run with shellcheck integration (recommended for shell script validation)
actionlint -shellcheck=/usr/bin/shellcheck
```

### Common Workflow Configuration Errors

These errors are often NOT caught by actionlint but cause runtime failures:

| Issue                         | Symptom                                           | Fix                                                     |
| ----------------------------- | ------------------------------------------------- | ------------------------------------------------------- |
| Missing `config-name`         | "Unable to find configuration" in release-drafter | Add `config-name: release-drafter.yml` to action inputs |
| Missing required action input | "Input required and not supplied"                 | Check action documentation for required inputs          |
| Invalid trigger event         | Workflow never runs                               | Verify event name matches GitHub's supported events     |
| Wrong `runs-on` value         | "No runner matching" error                        | Use valid runner labels (e.g., `ubuntu-latest`)         |
| Missing `permissions`         | "Resource not accessible by integration"          | Add explicit permissions block for required scopes      |
| Typo in secret name           | Empty value or "secret not found"                 | Verify secret name matches repository settings          |

### Common actionlint/shellcheck Errors

| Error Code | Description                      | Fix                                                |
| ---------- | -------------------------------- | -------------------------------------------------- |
| SC2129     | Multiple redirects to same file  | Use grouped commands: `{ cmd1; cmd2; } >> file`    |
| SC2034     | Variable declared but not used   | Remove unused variable or use it                   |
| SC2086     | Double quote to prevent globbing | Use `"$variable"` instead of `$variable`           |
| SC2046     | Quote to prevent word splitting  | Use `"$(command)"` instead of `$(command)`         |
| SC2155     | Declare and assign separately    | Use `local var; var=$(cmd)` not `local var=$(cmd)` |

### SC2129: Grouped Redirects

When writing multiple lines to the same file (e.g., `$GITHUB_STEP_SUMMARY`), use grouped commands:

```yaml
# ❌ WRONG - Multiple individual redirects (SC2129)
- name: Summary
  run: |
    echo "## Summary" >> "$GITHUB_STEP_SUMMARY"
    echo "" >> "$GITHUB_STEP_SUMMARY"
    echo "| Key | Value |" >> "$GITHUB_STEP_SUMMARY"
    echo "|-----|-------|" >> "$GITHUB_STEP_SUMMARY"

# ✅ CORRECT - Grouped redirects
- name: Summary
  run: |
    {
      echo "## Summary"
      echo ""
      echo "| Key | Value |"
      echo "|-----|-------|"
    } >> "$GITHUB_STEP_SUMMARY"
```

### SC2034: Unused Variables

```yaml
# ❌ WRONG - Variable declared but never used
- name: Process
  run: |
    unused_var=0
    echo "Processing..."

# ✅ CORRECT - Remove unused variable OR use it
- name: Process
  run: |
    echo "Processing..."
```

### Workflow Validation Checklist

Before committing workflow changes:

- [ ] Run `actionlint` locally — **MANDATORY, NO EXCEPTIONS**
- [ ] Fix ALL errors (CI will fail otherwise)
- [ ] Verify all action inputs are provided (especially `config-name` for reusable configs)
- [ ] Check that all referenced secrets exist in repository settings
- [ ] Test workflows in a branch before merging to main
- [ ] Use `${{ secrets.* }}` for sensitive values
- [ ] Quote all variable expansions in shell scripts
- [ ] Verify `runs-on` uses valid, available runner labels
- [ ] Confirm trigger events are spelled correctly and valid for the workflow type
