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

## 🚨🚨🚨 CRITICAL WARNING: PRETTIER MUST RUN IMMEDIATELY 🚨🚨🚨

> **THIS IS THE #1 CAUSE OF CI/CD FAILURES. READ THIS CAREFULLY.**

**Prettier MUST be run IMMEDIATELY after EVERY change to a non-C# file.** Not at the end. Not in batches. **IMMEDIATELY after each individual file modification.**

### ❌ WRONG Workflow (DO NOT DO THIS)

```text
1. Edit file1.md
2. Edit file2.json
3. Edit file3.yml
4. Run prettier at the end  ← TOO LATE! You will forget files and CI will fail.
```

### ✅ CORRECT Workflow (ALWAYS DO THIS)

```text
1. Edit file1.md
2. npx prettier --write file1.md  ← IMMEDIATELY
3. Verify: npx prettier --check file1.md
4. Edit file2.json
5. npx prettier --write file2.json  ← IMMEDIATELY
6. Verify: npx prettier --check file2.json
7. Edit file3.yml
8. npx prettier --write file3.yml  ← IMMEDIATELY
9. Verify: npx prettier --check file3.yml
```

**Why "at the end" fails:**

- You WILL forget which files you touched
- Running `prettier --write .` may not format all files you expect
- Issues compound and become harder to track
- Pre-push hooks WILL reject your commit
- You waste time debugging what could have been caught instantly

**The rule is simple:** Edit a file → Run Prettier on that file → Move on. No exceptions.

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
| C# code (`.cs`)           | `dotnet tool run csharpier format .`           | **RUN IMMEDIATELY** after ANY edit (not later) |
| C# code (`.cs`)           | `npm run lint:csharp-naming`                   | Check for underscore violations                |
| Test files (`.cs`)        | `pwsh -NoProfile -File scripts/lint-tests.ps1` | **MANDATORY** Track() usage, no manual destroy |

### Prettier/Markdown Formatting

> **🚨🚨🚨 ABSOLUTE REQUIREMENT 🚨🚨🚨**: Run Prettier **IMMEDIATELY** after editing **EACH** non-C# file. Not later. Not at the end. Not in batches. **IMMEDIATELY** after **EACH** file. Pre-push hooks **WILL REJECT** commits with Prettier formatting issues.

**Prettier applies to ALL of these file types** (not just C#):

- Markdown (`.md`) — **including `.llm/` directory files!**
- JSON (`.json`, `.asmdef`, `.asmref`)
- YAML (`.yml`, `.yaml`)
- Config files

> **⚠️ REMINDER**: The `.llm/` directory is **NOT EXEMPT** from Prettier formatting. All markdown files in `.llm/`, `.llm/skills/`, and subdirectories MUST be formatted with Prettier after every edit.

### The ONLY Correct Pattern

**Every single time you modify a non-C# file, immediately run:**

```bash
# Step 1: Edit the file
# Step 2: IMMEDIATELY run Prettier on that specific file
npx prettier --write <file>

# Step 3: Verify the file is formatted
npx prettier --check <file>

# Step 4: Only then move to the next file
```

**Concrete workflow example:**

```bash
# You need to edit 3 files: context.md, package.json, and ci.yml

# File 1: context.md
# ... make your edits to context.md ...
npx prettier --write .llm/context.md
npx prettier --check .llm/context.md  # Verify - should show "All matched files use Prettier code style!"

# File 2: package.json
# ... make your edits to package.json ...
npx prettier --write package.json
npx prettier --check package.json  # Verify

# File 3: ci.yml
# ... make your edits to ci.yml ...
npx prettier --write .github/workflows/ci.yml
npx prettier --check .github/workflows/ci.yml  # Verify

# Final verification before commit (catches anything you might have missed)
npx prettier --check .
```

> **⚠️ "I'll run prettier at the end" is a mistake.** You WILL forget files. You WILL have CI failures. Run it after EACH file.

**Key distinction:**

| File Type       | Formatter | Command                              |
| --------------- | --------- | ------------------------------------ |
| C# (`.cs`)      | CSharpier | `dotnet tool run csharpier format .` |
| Everything else | Prettier  | `npx prettier --write <file>`        |

---

### Documentation Changes Workflow

After **ANY** change to markdown files (anywhere in the repository, not just `.llm/`):

```bash
# 1. Format with Prettier FIRST (pre-push hook requirement)
npx prettier --write <file>

# 2. Check spelling (most common failure)
npm run lint:spelling

# 3. If spelling errors found with valid technical terms, add to cspell.json:
# Edit cspell.json and add terms to the "words" array

# 4. Check links and formatting — MANDATORY for ALL markdown files
npm run lint:docs      # Catches backtick .md refs AND inline code + link anti-patterns
npm run lint:markdown  # Markdownlint rules (MD032, MD009, etc.)
```

> **⚠️ IMPORTANT**: `npm run lint:docs` must be run after editing **ANY** markdown file in the entire repository — including `docs/`, the README, the CHANGELOG, the `.llm/` directory, and any other location. This catches broken links, backtick-wrapped markdown references, and the [inline code + link anti-pattern](#️-markdown-inline-code--link-anti-pattern).

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

> **⚠️ CRITICAL**: Run CSharpier **IMMEDIATELY** after EVERY C# file modification. Do NOT accumulate changes. Extra blank lines, spacing issues, and formatting inconsistencies are common CI/CD failures that are easily preventable.

After **ANY** change to `.cs` files (even a single line):

```bash
# 1. Auto-format code — RUN IMMEDIATELY, NOT LATER!
dotnet tool run csharpier format .

# 2. Check naming conventions
npm run lint:csharp-naming
```

**Common CSharpier Failures:**

- Extra blank lines between methods or at end of file
- Incorrect indentation after editing
- Spacing around operators or braces
- Line length violations

**The "Format After Every Edit" Rule:**

1. Edit a `.cs` file
2. **IMMEDIATELY** run `dotnet tool run csharpier format .`
3. Only then proceed to the next edit or file

**NEVER:**

- "I'll format everything at the end" — Issues compound and are harder to track
- "It's just a small change" — Small changes still cause formatting drift
- "The existing code was already formatted" — Your edit may have introduced issues

### Test File Changes Workflow (MANDATORY)

> **⚠️ CRITICAL**: The test lifecycle linter runs on **pre-push**. Failing to run it locally will result in **rejected pushes**.

After **ANY** change to test files in `Tests/`:

```bash
# MANDATORY - run IMMEDIATELY after each test file change
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

// For intentional destroy tests, add UNH-SUPPRESS comment on the SAME LINE:
UnityEngine.Object.DestroyImmediate(target); // UNH-SUPPRESS: Test verifies destroy behavior
_trackedObjects.Remove(target);
```

**When to use `// UNH-SUPPRESS`:**

- Testing behavior after object destruction
- Verifying error handling for destroyed objects
- Testing cleanup edge cases

**When NOT to use `// UNH-SUPPRESS`:**

- Normal test cleanup (use `Track()` instead)
- Avoiding linter errors for convenience (fix the underlying issue)

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

## ⚠️ Markdown Inline Code + Link Anti-pattern

> **CRITICAL**: The doc link linter uses a regex that can produce false positives when backticks and `.md` links appear on the same line.

### The Problem

The linter regex `` \`[^\`\n]*[A-Za-z0-9_\-]+\.md[^\`\n]*\` `` looks for backtick-wrapped text containing `.md`. However, it can match **across** separate backtick sections if a markdown link appears between them.

### Problematic Pattern

```markdown
<!-- ❌ WRONG: Linter matches from first ` to second `, capturing the link target -->

Files in `.llm/` should follow [context](../context.md) guidelines in `.llm/skills/`.

<!-- The regex sees: `.llm/` should follow [context](../context.md) guidelines in `.llm/skills/`
     and matches "context.md" as a backtick-wrapped .md reference -->
```

### Solutions

```markdown
<!-- ✅ Option 1: Move link to a different line -->

Files in `.llm/` and `.llm/skills/` should follow guidelines.
See [context](../context.md) for details.

<!-- ✅ Option 2: Restructure to avoid multiple backticks with link between -->

The `.llm/` directory contains context files. See [context](../context.md) for guidelines.
The `.llm/skills/` directory contains skill definitions.

<!-- ✅ Option 3: Use the link before any backticks -->

See [context](../context.md) for guidelines about the `.llm/` and `.llm/skills/` directories.
```

### Detection

This issue is caught by `npm run lint:docs`. Always run this command after **ANY** markdown file change (not just in `.llm/` but anywhere in the repository).

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

### 🚨 FIRST: Prettier Self-Check (MANDATORY)

- [ ] **Did I run `npx prettier --write <file>` IMMEDIATELY after EVERY non-C# file I touched?**
- [ ] Did I verify each file with `npx prettier --check <file>` after formatting?
- [ ] Final check: `npx prettier --check .` passes with no warnings?

> If you cannot answer "yes" to all three, go back and run Prettier on each file you modified NOW.

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

### GitHub Actions Configuration File Requirements (MANDATORY)

> **⚠️ CRITICAL**: Many GitHub Actions workflows depend on external configuration files. Missing configuration files cause runtime CI/CD failures that are NOT caught by `actionlint`.

**Before creating or modifying a workflow, verify ALL required configuration files exist:**

| Workflow                            | Required Configuration File         | Location                        | Default Branch Required? |
| ----------------------------------- | ----------------------------------- | ------------------------------- | ------------------------ |
| `release-drafter.yml`               | `release-drafter.yml`               | `.github/release-drafter.yml`   | **YES** (at runtime)     |
| Dependabot workflows                | `dependabot.yml`                    | `.github/dependabot.yml`        | YES                      |
| Labeler workflows                   | `labeler.yml` or similar            | `.github/labeler.yml`           | YES (typically)          |
| Workflows using `config-name` input | The file specified in `config-name` | Usually `.github/<config-name>` | Check action docs        |

**Checklist when adding/modifying workflows:**

- [ ] Identify all `config-name` or similar inputs in actions
- [ ] Verify referenced configuration files exist in `.github/`
- [ ] **Check if config must exist on default branch** (see bootstrapping section below)
- [ ] Create missing configuration files BEFORE the workflow runs
- [ ] Run `actionlint` IMMEDIATELY after any workflow changes
- [ ] Test workflow in a branch if possible

### Bootstrapping: Config Files Required on Default Branch

> **⚠️ CRITICAL**: Some GitHub Actions read configuration from the **default branch (main)** at runtime, NOT from the PR branch. This creates a "chicken and egg" problem.

**The Problem:**

1. You add a workflow file (e.g., `release-drafter.yml`) that references an external config
2. You add the config file (e.g., `.github/release-drafter.yml`) in the same PR
3. The workflow triggers on `push` or `pull_request`
4. The action runs and tries to read config from **main** (not your PR branch)
5. Config doesn't exist on main yet → **Workflow fails**

**Actions Known to Require Default Branch Config:**

| Action                            | Config Location               | Reads From        |
| --------------------------------- | ----------------------------- | ----------------- |
| `release-drafter/release-drafter` | `.github/release-drafter.yml` | Default branch    |
| `actions/labeler`                 | `.github/labeler.yml`         | Default branch    |
| `peter-evans/create-pull-request` | Various config files          | Check action docs |
| Dependabot                        | `.github/dependabot.yml`      | Default branch    |

**Solutions (choose one):**

1. **Disable triggers until config is merged** (RECOMMENDED):

   ```yaml
   # Comment out triggers until .github/release-drafter.yml is merged to main
   # on:
   #   push:
   #     branches: [main]
   #   pull_request:
   #     types: [opened, reopened, synchronize]

   # Temporary: Only allow manual triggering
   on:
     workflow_dispatch: # Manual trigger only
   ```

   After merging, uncomment the real triggers in a follow-up PR.

2. **Two-PR approach**:
   - PR #1: Add ONLY the config file (e.g., `.github/release-drafter.yml`)
   - Merge PR #1 to main
   - PR #2: Add the workflow file with full triggers

3. **Use `workflow_dispatch` only for initial merge**:
   - Add workflow with only `workflow_dispatch` trigger
   - Add config file in same PR
   - Merge to main
   - Update workflow to add real triggers (push, pull_request, etc.)

**Example: Safe Release Drafter Setup**

```yaml
# .github/workflows/release-drafter.yml
# Step 1: Initial merge with workflow_dispatch only
name: Release Drafter
on:
  workflow_dispatch: # Manual trigger for bootstrapping
  # TODO: Uncomment after .github/release-drafter.yml is on main:
  # push:
  #   branches: [main]
  # pull_request:
  #   types: [opened, reopened, synchronize]
```

**Example: Release Drafter requires `.github/release-drafter.yml`**

```yaml
# .github/workflows/release-drafter.yml
- uses: release-drafter/release-drafter@v6
  with:
    config-name: release-drafter.yml # ← References .github/release-drafter.yml
```

If `.github/release-drafter.yml` doesn't exist, the workflow will fail at runtime with "Unable to find configuration".

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

### Bash Arithmetic Safety in CI/CD (CRITICAL)

> **⚠️ CRITICAL**: The expression `((var++))` where `var` is 0 will cause scripts using `set -e` to exit immediately. This is a subtle bug that causes CI/CD failures.

**The Problem:**

When using `set -euo pipefail` (common in CI/CD for strict error handling):

- `((var++))` returns the value BEFORE increment
- If `var` is `0`, the return value is `0`
- Bash treats `0` as falsy/failure (exit code 1)
- `set -e` causes the script to exit on non-zero exit codes
- **Result**: Script exits unexpectedly on the first iteration

```yaml
# ❌ BUG - Script exits when count is 0
- name: Process items
  run: |
    set -euo pipefail
    count=0
    for item in "${items[@]}"; do
      process "$item"
      ((count++))  # ← FAILS when count=0, script exits!
    done
    echo "Processed $count items"
```

**Why This Happens:**

```bash
count=0
((count++))   # Returns 0 (pre-increment value), exit code 1
echo $?       # Prints: 1 (failure!)

count=1
((count++))   # Returns 1 (pre-increment value), exit code 0
echo $?       # Prints: 0 (success)
```

**Safe Alternatives:**

| Pattern               | Description                                | Recommendation       |
| --------------------- | ------------------------------------------ | -------------------- |
| `var=$((var + 1))`    | Assignment always succeeds                 | **RECOMMENDED**      |
| `((var++)) \|\| true` | Always succeeds, ignores return value      | Acceptable           |
| `: $((var++))`        | Null command with arithmetic side-effect   | Alternative          |
| `((++var))`           | Pre-increment returns new value (1, not 0) | Works but less clear |

```yaml
# ✅ CORRECT - Assignment always succeeds (RECOMMENDED)
- name: Process items
  run: |
    set -euo pipefail
    count=0
    for item in "${items[@]}"; do
      process "$item"
      count=$((count + 1))  # ← Assignment always succeeds
    done
    echo "Processed $count items"
```

**When to Watch Out:**

- Counter variables starting at 0
- Loop iteration counters
- Any arithmetic expression used as a statement (not in a condition)
- Scripts using `set -e` or `set -euo pipefail`

**Detection:**

actionlint with shellcheck integration may not catch this pattern. Manually review any `((var++))` expressions in workflow scripts, especially when `set -e` is enabled.

---

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
