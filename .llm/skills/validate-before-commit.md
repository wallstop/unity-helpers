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

| File Type Changed         | Command to Run IMMEDIATELY                     | Notes                                                                                    |
| ------------------------- | ---------------------------------------------- | ---------------------------------------------------------------------------------------- |
| Documentation (`.md`)     | `npx prettier --write <file>`                  | **MANDATORY** — Run FIRST after any edit                                                 |
| Documentation (`.md`)     | `npm run lint:spelling`                        | **🚨 MANDATORY** — #1 CI failure cause; add valid terms to `cspell.json`                 |
| Documentation (`.md`)     | `npm run lint:docs`                            | **CRITICAL** — Validates link targets, format (`./` or `../` prefix), AND absolute paths |
| Documentation (`.md`)     | `npm run lint:markdown`                        | Markdownlint rules (MD032, MD009, etc.)                                                  |
| JSON/asmdef/asmref        | `npx prettier --write <file>`                  | **MANDATORY** — Prettier formats JSON too                                                |
| YAML (all `.yml`/`.yaml`) | `npx prettier --write <file>`                  | **MANDATORY** — Prettier formats YAML too                                                |
| YAML (all `.yml`/`.yaml`) | `npm run lint:yaml`                            | **MANDATORY** — yamllint checks trailing spaces, syntax, style                           |
| GitHub Workflows (`.yml`) | `actionlint`                                   | **MANDATORY** for `.github/workflows/*.yml`                                              |
| C# code (`.cs`)           | `dotnet tool run csharpier format .`           | **RUN IMMEDIATELY** after ANY edit (not later)                                           |
| C# code (`.cs`)           | `npm run lint:spelling`                        | **🚨 MANDATORY** for XML docs and code comments                                          |
| C# code (`.cs`)           | `npm run lint:csharp-naming`                   | Check for underscore violations                                                          |
| Test files (`.cs`)        | `pwsh -NoProfile -File scripts/lint-tests.ps1` | **MANDATORY** Track() usage, no manual destroy                                           |

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

# 2. 🚨 Check spelling IMMEDIATELY (most common CI failure!)
npm run lint:spelling

# 3. If spelling errors found with valid technical terms:
#    - Edit cspell.json and add terms to the appropriate dictionary
#    - Run: npx prettier --write cspell.json
#    - Re-run: npm run lint:spelling (must pass with 0 errors)

# 4. Check links and formatting — MANDATORY for ALL markdown files
npm run lint:docs      # Catches backtick .md refs AND inline code + link anti-patterns
npm run lint:markdown  # Markdownlint rules (MD032, MD009, etc.)
```

> **🚨 CRITICAL**: Spelling errors are the **#1 cause of CI failures** for documentation changes. ALWAYS run `npm run lint:spelling` IMMEDIATELY after editing ANY markdown file or C# comments.
>
> **⚠️ IMPORTANT**: `npm run lint:docs` must be run after editing **ANY** markdown file in the entire repository — including `docs/`, the README, the CHANGELOG, the `.llm/` directory, and any other location. This catches broken links, backtick-wrapped markdown references, and the [inline code + link anti-pattern](#markdown-inline-code--link-anti-pattern).

---

## 🚨 Mandatory Linter Execution After Every Change

> **⚠️ CRITICAL**: This is a non-negotiable requirement. Linters MUST be run IMMEDIATELY after EVERY change — not at the end of a task, not in batches. IMMEDIATELY.

---

## 🚨🚨🚨 CRITICAL: SPELLING CHECKS — #1 CI FAILURE CAUSE 🚨🚨🚨

> **Spelling errors are the MOST COMMON cause of CI/CD failures for documentation changes.** Run `npm run lint:spelling` **IMMEDIATELY** after ANY change to documentation or code comments — NOT at task completion, NOT batched with other files.

### When to Run Spelling Check

**Run `npm run lint:spelling` IMMEDIATELY after:**

- ANY change to markdown files (`.md`) — including `.llm/`, `docs/`, README, CHANGELOG
- ANY change to XML documentation comments (`///`) in C# files
- ANY change to regular code comments (`//`) in C# files
- Adding new class names, method names, or technical terms
- Creating new files with documentation

### ✅ CORRECT Spelling Workflow

```text
1. Edit a file (markdown, C# with comments, etc.)
2. IMMEDIATELY run: npm run lint:spelling
3. If errors found:
   - Fix actual typos (misspellings)
   - Add valid technical terms to cspell.json (see dictionary guide below)
4. Format cspell.json if modified: npx prettier --write cspell.json
5. Re-run: npm run lint:spelling (must pass with 0 errors)
6. Only then proceed to next file
```

### ❌ WRONG Workflow (DO NOT DO THIS)

```text
1. Edit file1.md
2. Edit file2.md
3. Edit file3.cs (with XML docs)
4. Run lint:spelling at the end ← TOO LATE! Errors compound, harder to fix.
```

### How to Fix Spelling Errors

When `npm run lint:spelling` reports unknown words:

#### Step 1: Determine if It's a Typo or Valid Term

| If the word is...           | Action                            |
| --------------------------- | --------------------------------- |
| A typo/misspelling          | Fix the typo in your file         |
| A valid Unity API           | Add to `unity-terms` dictionary   |
| A valid C# language feature | Add to `csharp-terms` dictionary  |
| A package-specific type     | Add to `package-terms` dictionary |
| A general tech term/tool    | Add to `tech-terms` dictionary    |
| A general word/proper noun  | Add to top-level `words` array    |

#### Step 2: Add to the Correct Dictionary in cspell.json

| Dictionary       | Use For                                                               | Examples                                       |
| ---------------- | --------------------------------------------------------------------- | ---------------------------------------------- |
| `unity-terms`    | Unity Engine API names, Unity-specific terms                          | `MonoBehaviour`, `SerializeField`, `OnGUI`     |
| `csharp-terms`   | C# language features, .NET types, C# patterns                         | `struct`, `Nullable`, `IEnumerable`            |
| `package-terms`  | This package's custom types, class names, method names                | `WButtonEditor`, `UnityHelpers`, `QuadTree2D`  |
| `tech-terms`     | General programming terms, tools, external libraries                  | `actionlint`, `async`, `middleware`, `prepush` |
| `words` (global) | General words that don't fit above categories, proper nouns, acronyms | `changelog`, `submodule`, `boilerplate`        |

#### Step 3: Edit cspell.json

```jsonc
// In cspell.json, find the appropriate dictionaryDefinitions entry:
{
  "dictionaryDefinitions": [
    {
      "name": "unity-terms",
      "words": [
        "AddComponent",
        "Awake",
        "MonoBehaviour", // ← Add new Unity terms here (alphabetically)
        "OnDestroy"
      ]
    }
    // ... other dictionaries
  ],
  // For general words, use the top-level "words" array:
  "words": [
    "changelog",
    "prepush" // ← Add general words here (alphabetically)
  ]
}
```

> **⚠️ IMPORTANT**: Always keep dictionary entries sorted alphabetically within each `words` array.

#### Step 4: Verify and Format

```bash
# Verify the spelling fix
npm run lint:spelling

# Format the cspell.json file
npx prettier --write cspell.json
```

### Common Spelling Mistakes to Avoid

| ❌ WRONG                              | ✅ RIGHT                                                  |
| ------------------------------------- | --------------------------------------------------------- |
| Ignoring spelling errors              | Fix typos OR add valid terms to dictionary                |
| Adding typos to dictionary            | Only add legitimate technical terms                       |
| Adding words to wrong dictionary      | Match term type to dictionary (Unity→unity-terms, etc.)   |
| Running spelling check at end of task | Run IMMEDIATELY after EACH file change                    |
| Forgetting to format cspell.json      | Always run `npx prettier --write cspell.json` after edits |
| Adding words in random order          | Keep words sorted alphabetically in each dictionary       |

### Proactive Spelling Management

When writing documentation or code comments with technical terms:

1. **Proactively add known terms** to `cspell.json` BEFORE running the linter
2. **Common terms to add**: Unity API names, package types, custom class names, acronyms
3. **Run `npm run lint:spelling`** immediately after each file to catch any missed terms

---

### Link Validation After Markdown Changes

> **🚨 IMPORTANT**: The doc link linter runs automatically in **pre-commit hooks** and in the **`validate-docs.yml` CI workflow**. Catching issues locally saves time and prevents CI failures.

**Run `npm run lint:docs` IMMEDIATELY after:**

- ANY change to markdown files
- Adding or modifying links
- Moving or renaming files that other markdown files link to

```bash
# After editing any markdown file
npm run lint:docs

# If errors are found, fix them before proceeding
# The linter provides helpful fix suggestions in its output!
```

**What `npm run lint:docs` catches:**

| Issue Type                      | Description                           | How to Fix                        |
| ------------------------------- | ------------------------------------- | --------------------------------- |
| Missing relative prefix         | Link without `./` or `../`            | Add relative prefix to the path   |
| **Absolute GitHub Pages paths** | Links starting with `/unity-helpers/` | Convert to relative path          |
| Broken file references          | Path points to non-existent file      | Fix path or create the file       |
| Inline code with file paths     | File paths in backticks               | Use proper markdown links instead |

> **⚠️ If you see a warning about `/unity-helpers/`**: This is an absolute GitHub Pages path that won't work locally or in other deployment contexts. **Convert to a relative path immediately** using `./` or `../` prefix.

### Markdown Link Path Requirements

> **🚨🚨🚨 CRITICAL: ALL internal markdown links MUST use `./` or `../` prefix 🚨🚨🚨**
>
> This is a **MANDATORY** requirement. The `npm run lint:docs` command validates:
>
> 1. **Link targets exist** — the referenced file must be present
> 2. **Link format is correct** — paths MUST start with `./` or `../`
> 3. **No absolute GitHub Pages paths** — paths like `/unity-helpers/...` are detected and flagged
>
> **Why this matters**: The Jekyll site uses `jekyll-relative-links` which requires explicit relative paths to resolve links correctly. Bare paths without a relative prefix will NOT work. Absolute paths like `/unity-helpers/docs/...` break when the site is accessed locally, in forks, or in different deployment contexts.
>
> **CI Integration**: These rules are enforced by the `validate-docs.yml` workflow. Violations **WILL** cause CI failures.

#### Common Mistakes (AVOID THESE)

```markdown
<!-- ❌ WRONG: Bare filename without ./ prefix -->

See [context](context.md) for guidelines.

<!-- ❌ WRONG: Path to subdirectory without ./ prefix -->

Refer to [create-test](skills/create-test.md) for details.

<!-- ❌ WRONG: Path starting with folder name, not ./ -->

Check [features](docs/features/overview.md) for documentation.

<!-- ❌ WRONG: Absolute GitHub Pages path (site-specific) -->

See [overview](/unity-helpers/docs/overview/) for introduction.
```

#### Correct Format (ALWAYS DO THIS)

```markdown
<!-- ✅ CORRECT: Same directory — use ./ prefix -->

See [context](./context.md) for guidelines.

<!-- ✅ CORRECT: Subdirectory — use ./ prefix -->

Refer to [create-test](./skills/create-test.md) for details.

<!-- ✅ CORRECT: Parent directory — use ../ prefix -->

Check [features](../docs/features/overview.md) for documentation.
```

**Fixing "missing relative prefix" errors:**

1. Identify the link causing the error in the lint output
2. Add `./` prefix for files in the same directory or subdirectories
3. Add `../` prefix (one or more) for files in parent directories
4. Re-run `npm run lint:docs` to verify the fix

**Quick Reference Table:**

```text
Link Pattern                       Status     Fix / Explanation
---------------------------------  ---------  -----------------------------------------
[text](file.md)                    ❌ WRONG   [text](./file.md)
[text](folder/file.md)             ❌ WRONG   [text](./folder/file.md)
[text](skills/file.md)             ❌ WRONG   [text](./skills/file.md)
[text](/unity-helpers/docs/...)    ❌ WRONG   [text](./docs/...) — absolute paths break portability
[text](./file.md)                  ✅ OK      Same directory
[text](./folder/file.md)           ✅ OK      Subdirectory
[text](../file.md)                 ✅ OK      Parent directory
[text](../../folder/file.md)       ✅ OK      Multiple parent levels
[text](https://example.com)        ✅ OK      External links don't need relative prefix
```

**Examples by link location:**

```text
Current File Location          | Link Target                  | Correct Link Syntax
-------------------------------|------------------------------|--------------------------------------------
.llm/context.md                | .llm/skills/create-test.md   | [create-test](./skills/create-test.md)
.llm/skills/create-test.md     | .llm/context.md              | [context](../context.md)
docs/features/overview.md      | docs/guides/setup.md         | [setup](../guides/setup.md)
README.md                      | docs/overview/index.md       | [overview](./docs/overview/index.md)
```

### YAML File Formatting and Linting (MANDATORY)

> **⚠️ CRITICAL**: YAML linting (yamllint) runs in CI and will **FAIL** on trailing spaces, improper indentation, and style violations. ALWAYS run both Prettier AND yamllint locally after editing ANY YAML file.

**Run IMMEDIATELY after ANY change to YAML files (`.yml`, `.yaml`):**

```bash
# Step 1: Format with Prettier FIRST
npx prettier --write <file>

# Step 2: Run yamllint to catch style issues (trailing spaces, etc.)
npm run lint:yaml

# Step 3: For workflow files (.github/workflows/*.yml), also run actionlint
actionlint .github/workflows/<file>
```

**Concrete workflow example:**

```bash
# Editing .github/workflows/ci.yml

# 1. Make your edits
# 2. Format with Prettier
npx prettier --write .github/workflows/ci.yml

# 3. Run yamllint to catch trailing spaces and style issues
npm run lint:yaml

# 4. Run actionlint to validate workflow syntax
actionlint .github/workflows/ci.yml

# Only then proceed to next file
```

**Common yamllint failures (all caught by `npm run lint:yaml`):**

| Issue                  | Error Message          | Fix                                |
| ---------------------- | ---------------------- | ---------------------------------- |
| Trailing spaces        | `trailing spaces`      | Remove spaces at end of lines      |
| Inconsistent indent    | `wrong indentation`    | Use consistent 2-space indentation |
| Line too long          | `line too long`        | Break long lines (max 200 chars)   |
| Missing newline at EOF | `no new line at end`   | Add empty line at end of file      |
| Too many blank lines   | `too many blank lines` | Maximum 1 consecutive blank line   |

> **Note**: Prettier fixes formatting but does NOT catch trailing spaces in multiline strings or some edge cases. yamllint catches ALL trailing space issues.

### Line Ending Configuration Consistency (CRITICAL)

> **🚨🚨🚨 CRITICAL**: Line ending configuration must be synchronized across ALL config files. Mismatches cause CI failures because files are checked out with one line ending but linters expect another.

**The Bug Pattern**: When `.gitattributes` specifies LF for certain files but `.prettierrc.json` uses CRLF globally (without overrides) and `.yamllint.yaml` expects a different ending, CI will fail even though files pass locally.

#### Configuration Files That Control Line Endings

| File               | Purpose                                   | Current YAML Setting         |
| ------------------ | ----------------------------------------- | ---------------------------- |
| `.gitattributes`   | Controls git checkout line endings        | `*.yml text eol=lf`          |
| `.prettierrc.json` | Controls Prettier formatting line endings | `endOfLine: lf` for YAML     |
| `.yamllint.yaml`   | Controls yamllint line ending validation  | `new-lines: type: unix` (LF) |
| `.editorconfig`    | Controls IDE line endings for new files   | `end_of_line = lf` for YAML  |

#### Current Line Ending Settings

| File Type                    | Line Ending | Why                                          |
| ---------------------------- | ----------- | -------------------------------------------- |
| YAML files (`.yml`, `.yaml`) | LF (unix)   | GitHub Actions runners use LF checkout       |
| GitHub workflow files        | LF (unix)   | `.github/**` uses LF in `.gitattributes`     |
| `package.json`               | LF (unix)   | Explicit in `.gitattributes`                 |
| Most other text files        | CRLF        | Default for cross-platform Unity development |

#### When Modifying Line Ending Configuration

**If you change line endings in ANY config file, you MUST update ALL of them:**

```bash
# After modifying any line ending configuration
# Verify YAML files are correct:
npm run lint:yaml
npx prettier --check "**/*.yml" "**/*.yaml"

# Verify other files (if applicable):
npx prettier --check .
```

#### Common Mismatch Scenarios

| ❌ WRONG Configuration                        | ✅ CORRECT Configuration                         |
| --------------------------------------------- | ------------------------------------------------ |
| `.gitattributes` has LF, Prettier uses CRLF   | Both must match (LF for YAML, CRLF for others)   |
| yamllint expects CRLF, git checks out LF      | yamllint must use `type: unix` for YAML files    |
| `.editorconfig` differs from `.gitattributes` | Both must specify the same endings per file type |

#### Verification Commands

```bash
# Verify YAML linting passes (checks line endings)
npm run lint:yaml

# Verify Prettier formatting (includes line ending check)
npx prettier --check "**/*.yml" "**/*.yaml"

# Full pre-push validation (catches all issues)
npm run validate:prepush
```

### Workflow Changes Workflow

After **ANY** change to `.github/workflows/*.yml`:

```bash
# MANDATORY - run all three tools in order
npx prettier --write .github/workflows/<file>.yml  # Format first
npm run lint:yaml                                   # Catch trailing spaces
actionlint                                          # Validate workflow syntax

# Fix ALL errors before committing
# Common issues: SC2129 (grouped redirects), missing config-name, trailing spaces
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

See [create-test](./create-test.md#unity-object-lifecycle-management-critical) for detailed patterns.

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

<!-- ✅ CORRECT: Proper markdown links WITH relative prefix -->

See [context](./context.md) for guidelines.
Refer to [create-test](./skills/create-test.md) for details.
```

**The `npm run lint:docs` check validates TWO things:**

1. **No backtick-wrapped `.md` references** — Use proper links instead
2. **All internal links use `./` or `../` prefix** — Required for jekyll-relative-links

> **Remember**: Even when converting backtick references to links, you MUST include the `./` or `../` prefix!

---

## Markdown Inline Code + Link Anti-pattern

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

## Escaping Example Links in Documentation

> **CRITICAL**: When writing documentation that SHOWS link syntax (teaching users proper format), example links MUST be escaped so the linter doesn't parse them as real links.

### The Problem

Documentation often needs to show examples of correct vs incorrect link format. If these examples are written as real markdown links, the linter will:

1. Try to resolve them as actual links
2. Report "file not found" errors for made-up example paths
3. Flag them for missing `./` prefix (intentionally shown as "wrong")

### How to Escape Example Links

#### Method 1: Fenced Code Blocks (Recommended)

Use fenced code blocks with `text` language specifier and escaped brackets:

```text
<!-- Examples with escaped brackets are NOT parsed by linter -->
Correct format: ]\(./path/to/file)
Wrong format: ]\(path/to/file) -- missing ./
```

#### Method 2: Inline Backticks for Short Examples

For brief inline examples, escape brackets:

```text
Use `]\(./file)` format for links.
Wrong: `]\(file)` — missing prefix.
```

#### Method 3: HTML Comments for Hidden Examples

For reference patterns that shouldn't render:

```markdown
<!-- Example pattern: [text](./relative/path.md) -->
```

### Common Escaping Mistakes

| Mistake                                     | Why It Fails                         | Fix                              |
| ------------------------------------------- | ------------------------------------ | -------------------------------- |
| Showing example link outside code block     | Linter parses it as real link        | Wrap in code fence               |
| Using `text` specifier for markdown syntax  | Syntax highlighting missing (minor)  | Use `markdown` specifier         |
| Forgetting to escape "wrong" examples       | Linter reports false errors          | Wrap ALL examples, good and bad  |
| Mixing real and example links in same block | Linter may catch unintended patterns | Separate real links from samples |

### Verification

After adding or editing example links in documentation:

```bash
# This must pass without false positives from examples
npm run lint:docs

# If examples trigger errors, they need better escaping
```

---

## CI Markdown Link Validation

> **⚠️ IMPORTANT**: The CI pipeline runs TWO separate link validation jobs with different purposes. Understanding both is critical to avoiding CI failures.

### CI Link Validation Jobs

The `validate-docs.yml` workflow runs two complementary link checks:

| Job                    | Purpose                                                               | Script                              |
| ---------------------- | --------------------------------------------------------------------- | ----------------------------------- |
| `validate-links`       | Verifies all internal markdown links point to existing files          | `.github/scripts/validate-links.sh` |
| `validate-link-format` | Ensures all internal links use proper `./` or `../` relative prefixes | `.github/scripts/validate-links.sh` |

**Both jobs must pass for CI to succeed.**

### What CI Validates

#### `validate-links` Job: Link Targets Exist

This job checks that every internal markdown link points to a file that actually exists:

- Handles URL-encoded paths (e.g., `%20` for spaces, `%28` for `(`)
- Resolves paths relative to the linking file's location
- Reports specific line numbers for broken links

#### `validate-link-format` Job: Proper Relative Prefixes

This job ensures all internal links use explicit relative path prefixes:

- ✅ Links starting with `./` (same directory or subdirectory)
- ✅ Links starting with `../` (parent directory)
- ❌ Bare paths without `./` or `../` prefix (e.g., just the filename)

### Content CI Automatically Skips

Both CI validation scripts intelligently skip content that shouldn't be validated:

| Skipped Content                | Example                                    | Why Skipped                         |
| ------------------------------ | ------------------------------------------ | ----------------------------------- |
| Fenced code blocks             | ` ```markdown ... ``` ` or `~~~ ...`       | Example/documentation code          |
| Inline code backticks          | Code wrapped in single backticks           | Code references, not links          |
| External links (http/https)    | Links to `https://` or `http://` URLs      | External URLs, not local files      |
| Anchor-only links              | Links like `#section-name`                 | Same-page navigation                |
| mailto: links                  | Links starting with `mailto:`              | Email links, not file references    |
| Image references               | Image syntax (exclamation mark + brackets) | Images handled differently          |
| Reference-style link footnotes | Footnote definitions like `[1]: path`      | Processed separately where relevant |

### Why Local Linting Might Miss Issues

> **⚠️ WARNING**: The local `npm run lint:docs` command and the CI bash scripts use different implementations. Always run BOTH to catch all issues.

**Potential differences between local and CI validation:**

| Aspect                | Local (`lint:docs`)      | CI (bash scripts)             |
| --------------------- | ------------------------ | ----------------------------- |
| Implementation        | PowerShell script        | Bash scripts                  |
| Code block detection  | Regex-based              | State machine in bash         |
| URL decoding          | PowerShell methods       | `sed` transformations         |
| Path resolution       | PowerShell path handling | Bash path resolution          |
| Edge case handling    | May differ slightly      | May catch different edge case |
| Inline code detection | May use different regex  | Uses awk/sed patterns         |

**Best Practice:**

```bash
# ALWAYS run local linting before pushing
npm run lint:docs

# This catches most issues locally, but CI may still find edge cases
# If CI fails with link errors that local linting missed, investigate the specific pattern
```

### Debugging CI Link Validation Failures

When CI reports a link error that local linting missed:

1. **Check the exact error message** — CI reports file path and line number
2. **Look for edge cases:**
   - URL-encoded characters in paths (`%20`, `%28`, `%29`)
   - Links inside complex markdown structures
   - Mixed content on the same line (inline code + links)
3. **Verify the fix locally:**

   ```bash
   npm run lint:docs
   ```

4. **If the pattern is a known false positive**, consider whether the content structure can be refactored

### Quick Reference: CI Validation Rules

```text
Link Pattern                         CI Result    Notes
------------------------------------  -----------  -------------------------------------
[text](./file.md)                     ✅ PASS      Proper relative prefix, file exists
[text](../folder/file.md)             ✅ PASS      Parent directory navigation
[text](file.md)                       ❌ FAIL      Missing ./ prefix
[text](folder/file.md)                ❌ FAIL      Missing ./ prefix
[text](./nonexistent.md)              ❌ FAIL      File does not exist
[text](https://example.com)           ⏭️ SKIP      External link
[text](#anchor)                       ⏭️ SKIP      Anchor-only link
`some-file.md`                        ⏭️ SKIP      Inline code, not a link
```

---

## Known Limitations of Link Validation

The link validation scripts (both local PowerShell and CI bash) have some known limitations. These are documented here for completeness and to explain why certain edge cases may not be caught.

### Parentheses in URLs

The regex pattern `\]\([^)]+\)` cannot match URLs containing parentheses, such as:

- `[text](./path/file(1).md)` — File with parentheses in name
- `[wiki](https://en.wikipedia.org/wiki/Example_(disambiguation))` — Wikipedia-style URLs

**Workaround:** Avoid parentheses in filenames. For external URLs with parentheses, validation may produce false negatives (link won't be checked).

### Inline Code Edge Cases

The inline code stripping regex handles common cases but has limitations:

| Pattern                     | Handled? | Notes                                  |
| --------------------------- | -------- | -------------------------------------- |
| `` `normal code` ``         | ✅ Yes   | Standard inline code                   |
| ` `` `double backtick` `` ` | ✅ Yes   | Double-backtick code spans             |
| `` `escaped \` backtick` `` | ❌ No    | Escaped backticks not recognized       |
| `` `text with ` inside` ``  | ❌ No    | Nested backticks not standard markdown |
| ` `triple+ on same line`    | ⚠️ Maybe | May interact unexpectedly              |

**Workaround:** Place complex code examples in fenced code blocks (triple backticks on their own lines), which are properly skipped.

### URL Decoding

The CI bash `urldecode` function uses `printf '%b'` with hex escape sequences:

- **Safe for:** Repository-owned markdown files (trusted source)
- **Edge case:** `%` followed by non-hex characters produces undefined output
- **Not safe for:** Untrusted user input (potential injection vector)

The local PowerShell script uses `[System.Uri]::UnescapeDataString()` which handles edge cases more gracefully.

### External Scheme Detection

The regex `^[a-zA-Z][a-zA-Z0-9+\.-]*:` correctly identifies URI schemes, but the initial broad pattern `\]\((?<target>[a-zA-Z][^)]*)\)` matches all links starting with a letter. This is intentional — the filtering happens afterward to ensure no internal links slip through.

**Schemes correctly skipped:** `http:`, `https:`, `mailto:`, `ftp:`, `file:`, `data:`, etc.

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

See [update-documentation](./update-documentation.md) for complete guidelines.

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

#### Example: Safe Release Drafter Setup

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

### Subshell Variable Propagation (CRITICAL)

> **⚠️ CRITICAL**: Variables modified inside a pipeline or `while read` loop do NOT propagate to the parent shell. This causes silent bugs where counters/state changes are lost.

**The Problem:**

When you pipe data into a `while read` loop, bash creates a **subshell** to execute the loop. Any variables set or modified inside the loop exist only in that subshell and are lost when the loop ends.

```yaml
# ❌ BUG - Variable modified in subshell, parent sees original value
- name: Count errors
  run: |
    set -euo pipefail
    errors=0

    # WARNING: while loop runs in a subshell due to pipe!
    find . -name "*.md" -print0 | while IFS= read -r -d '' file; do
      if ! validate "$file"; then
        errors=$((errors + 1))  # ← This modifies subshell's copy!
      fi
    done

    echo "Found $errors errors"  # ← ALWAYS PRINTS 0!
    if [ "$errors" -gt 0 ]; then exit 1; fi  # ← NEVER FAILS!
```

**Why This Happens:**

The pipe (`|`) creates a subshell for the right side. Variables modified in that subshell don't affect the parent:

```bash
count=0
echo "a b c" | while read -r word; do
  count=$((count + 1))  # Subshell's count is now 1, 2, 3...
done
echo $count  # Prints: 0 (parent's count unchanged!)
```

**Safe Alternatives:**

| Pattern                      | Description                              | Recommendation     |
| ---------------------------- | ---------------------------------------- | ------------------ |
| Process substitution         | `while read ... done < <(cmd)`           | **RECOMMENDED**    |
| Temp file for counter        | Write count to file, read back in parent | Reliable           |
| Process substitution + shopt | `shopt -s lastpipe` (bash 4.2+ only)     | Not portable       |
| Output accumulation          | Echo results, count lines in parent      | Works for counting |

#### Pattern 1: Process Substitution (RECOMMENDED)

```yaml
# ✅ CORRECT - Process substitution avoids subshell for the loop
- name: Count errors
  run: |
    set -euo pipefail
    errors=0

    # Process substitution keeps loop in parent shell
    while IFS= read -r -d '' file; do
      if ! validate "$file"; then
        errors=$((errors + 1))
      fi
    done < <(find . -name "*.md" -print0)

    echo "Found $errors errors"  # ← Now works correctly!
    if [ "$errors" -gt 0 ]; then exit 1; fi
```

#### Pattern 2: Temp File Counter (Most Reliable)

```yaml
# ✅ CORRECT - Temp file persists across subshell boundaries
- name: Count errors
  run: |
    set -euo pipefail

    # Use temp file to persist counter across subshells
    error_file=$(mktemp)
    echo "0" > "$error_file"

    find . -name "*.md" -print0 | while IFS= read -r -d '' file; do
      if ! validate "$file"; then
        # Read, increment, write back to temp file
        count=$(cat "$error_file")
        echo $((count + 1)) > "$error_file"
      fi
    done

    errors=$(cat "$error_file")
    rm -f "$error_file"

    echo "Found $errors errors"  # ← Works reliably!
    if [ "$errors" -gt 0 ]; then exit 1; fi
```

#### Pattern 3: Output Accumulation

```yaml
# ✅ CORRECT - Accumulate output, count in parent
- name: Find broken links
  run: |
    set -euo pipefail

    # Collect broken links (each on own line)
    broken_links=$(find . -name "*.md" -print0 | while IFS= read -r -d '' file; do
      if ! validate "$file"; then
        echo "$file"  # Output broken files
      fi
    done || true)

    # Count in parent shell
    if [ -n "$broken_links" ]; then
      error_count=$(echo "$broken_links" | wc -l)
      echo "Found $error_count broken links:"
      echo "$broken_links"
      exit 1
    fi

    echo "All links valid"
```

**When to Watch Out:**

- Any `cmd | while read` pattern
- Counter/accumulator variables in pipelines
- State tracking inside loops fed by pipes
- Any variable you need AFTER a piped loop

**Common Bug Pattern — Unused Variable Due to Subshell:**

```yaml
# ❌ BUG - sidebar_errors is set but NEVER used (Copilot will flag this)
sidebar_errors=0
grep ... | while read -r page; do
  sidebar_errors=$((sidebar_errors + 1))  # Modifies subshell copy
done
# sidebar_errors is still 0 here, so any check using it is broken!
```

**Detection:**

- Copilot code review flags variables that appear unused
- shellcheck may warn about variables set but not used
- Manually review any variable incremented inside `while read` loops with pipes

---

### Word Splitting and Special Characters (CRITICAL)

> **⚠️ CRITICAL**: Using `for item in $variable` causes word splitting on spaces, tabs, and newlines. This silently breaks iteration when items contain spaces or special characters.

**The Problem:**

When you expand a variable without quotes in `for ... in $var`, bash performs **word splitting** based on IFS (Internal Field Separator, defaults to space/tab/newline). This breaks items that contain spaces:

```yaml
# ❌ BUG - Word splitting breaks links with spaces
- name: Check links
  run: |
    set -euo pipefail

    # Extract markdown links (some may have spaces like %20)
    links=$(grep -oE '\]\([^)]+\)' "$file")

    # WARNING: This breaks on ANY space in any link!
    for link in $links; do  # ← $links is UNQUOTED, triggers word splitting
      # If links contains "](./my%20file.md)", this iterates:
      #   1. "](./my%20file.md)"  ← OK if no spaces
      # But if decoded or has actual spaces:
      #   1. "](./my"
      #   2. "file.md)"  ← BROKEN!
      check_link "$link"
    done
```

**Why This Happens:**

1. `$links` (unquoted) triggers word splitting
2. Each word (separated by space/tab/newline) becomes a separate iteration
3. A single markdown link with spaces becomes 3 broken fragments

**Safe Alternative: Use `while read` Loop**

```yaml
# ✅ CORRECT - while read preserves entire lines including spaces
- name: Check links
  run: |
    set -euo pipefail

    # Use while read loop with process substitution
    while IFS= read -r link; do
      # Skip empty lines
      [ -z "$link" ] && continue

      check_link "$link"
    done < <(grep -oE '\]\([^)]+\)' "$file" 2>/dev/null || true)
```

**Key Elements of the Safe Pattern:**

| Element                 | Purpose                                     |
| ----------------------- | ------------------------------------------- |
| `IFS=`                  | Disable field splitting (preserve spaces)   |
| `read -r`               | Don't interpret backslashes                 |
| `< <(cmd)`              | Process substitution avoids subshell issues |
| `[ -z "$link" ]`        | Handle empty output from grep               |
| `2>/dev/null \|\| true` | Gracefully handle no matches                |

#### Alternative: Array with Proper Quoting (Bash 4+)

```yaml
# ✅ CORRECT - Store in array, quote expansion
- name: Check links
  run: |
    set -euo pipefail

    # Read into array (one element per line)
    mapfile -t links < <(grep -oE '\]\([^)]+\)' "$file" 2>/dev/null || true)

    # Iterate with quoted expansion
    for link in "${links[@]}"; do
      check_link "$link"
    done
```

**When to Watch Out:**

- Any `for item in $variable` pattern (unquoted variable)
- Variables containing grep/find output (may have special chars)
- File paths or URLs (commonly contain spaces, encoded chars)
- User-supplied data of any kind

**Detection:**

- shellcheck warns about unquoted variables (`SC2086`)
- Code review should flag any `for ... in $var` pattern
- Test with deliberately pathological input (spaces, tabs, quotes, etc.)

---

### Hardcoded Executable Paths (Portability)

> **⚠️ CRITICAL**: Never use absolute paths like `/bin/sed` or `/usr/bin/awk` in scripts. Use bare command names and let PATH resolution find the correct binary.

**The Problem:**

Different operating systems and container images have executables in different locations:

- Ubuntu: `/bin/sed`, `/usr/bin/awk`
- Alpine: `/bin/sed` (BusyBox), `/usr/bin/awk` (may not exist)
- macOS: `/usr/bin/sed`, `/usr/bin/awk`

```yaml
# ❌ BUG - Hardcoded path may not exist on all runners
- name: Process files
  run: |
    echo "$content" | /bin/sed 's/old/new/'        # ← Fails on some images
    echo "$data" | /usr/bin/awk '{print $1}'       # ← Fails on some images
```

#### Fix: Use Bare Command Names

```yaml
# ✅ CORRECT - Let PATH find the right binary
- name: Process files
  run: |
    echo "$content" | sed 's/old/new/'
    echo "$data" | awk '{print $1}'
```

**Detection:**

- Copilot code review flags hardcoded paths
- grep for `/bin/`, `/usr/bin/` in workflow files
- actionlint may warn about non-portable commands

---

### Portable Shell Scripting in Workflows (CRITICAL)

> **⚠️ CRITICAL**: CI/CD workflows run on various platforms. Scripts MUST use POSIX-compliant commands to ensure portability across Linux, macOS runners, and different shell implementations.

**The Problem:**

GitHub Actions runners use Ubuntu Linux by default, but:

- Self-hosted runners may use macOS, which has BSD tools (not GNU)
- Container jobs may use Alpine Linux with BusyBox
- Different tool versions have different feature sets

**GNU-Specific `grep -oP` (Perl Regex) — NEVER USE:**

```yaml
# ❌ BUG - grep -oP is GNU-only, fails on macOS and Alpine
- name: Extract links
  run: |
    echo "$content" | grep -oP '\]\(\K[^)]+(?=\))' # ← -P is GNU-only!
```

The `-P` flag enables Perl-compatible regular expressions (PCRE) which:

- Is NOT available on macOS BSD `grep`
- Is NOT available on Alpine Linux BusyBox `grep`
- Will cause "grep: invalid option -- P" errors

**Portable Alternatives:**

| GNU-Specific               | Portable Alternative                | Notes                                  |
| -------------------------- | ----------------------------------- | -------------------------------------- |
| `grep -oP` (Perl regex)    | `grep -oE` (extended regex) + `sed` | `-E` is POSIX-compliant                |
| `grep -oP '\K'` lookbehind | `grep -oE` + `sed 's/prefix//'`     | Post-process to remove unwanted prefix |
| `grep -oP '(?=...)'`       | `grep -oE` then post-process        | Lookaheads aren't portable             |
| `sed -i '' file` (macOS)   | `sed ... > tmp && mv tmp file`      | In-place edit syntax differs           |
| `sed -i file` (GNU)        | `sed ... > tmp && mv tmp file`      | Use temp file for portability          |
| `readarray` / `mapfile`    | `while read` loop                   | Bash 4+ only                           |

**Example: Extract Markdown Links Portably:**

```yaml
# ❌ WRONG - GNU-only Perl regex
- name: Extract links (broken on macOS)
  run: |
    echo "$line" | grep -oP '\]\(\K[^)]+(?=\))'

# ✅ CORRECT - POSIX-compliant extended regex + sed
- name: Extract links (portable)
  run: |
    echo "$line" | grep -oE '\]\([^)]+\)' | sed 's/^](//;s/)$//'
```

**When to Use What:**

| Context                         | Approach               | Why                                         |
| ------------------------------- | ---------------------- | ------------------------------------------- |
| GitHub Actions (Ubuntu runners) | POSIX tools            | Future-proofs for self-hosted/macOS runners |
| Local development               | Modern tools (`rg`)    | Fast, best UX                               |
| Dev container                   | Modern tools available | Controlled environment                      |
| Git hooks (`.githooks/`)        | POSIX tools            | Developers may use macOS                    |
| Bash scripts (`scripts/*.sh`)   | POSIX tools            | Maximum portability                         |
| PowerShell scripts              | N/A                    | PowerShell is consistent across platforms   |

**Quick Reference — Portable vs Non-Portable:**

```bash
# ❌ Non-portable (GNU-specific)
grep -oP 'pattern'        # Perl regex not portable
sed -i 's/a/b/' file      # In-place syntax varies
readarray -t arr < file   # Bash 4+ only

# ✅ Portable (POSIX-compliant)
grep -oE 'pattern'        # Extended regex is standard
sed 's/a/b/' file > tmp && mv tmp file  # Works everywhere
while IFS= read -r line; do arr+=("$line"); done < file  # Standard loop
```

See [search-codebase](./search-codebase.md#portable-shell-scripting-cicd--bash-scripts) for more examples.

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

---

## Git Hook Regex Pattern Testing (CRITICAL)

> **🚨🚨🚨 CRITICAL**: Git hook regex patterns require SINGLE backslashes, NOT double-escaped. Double escaping causes patterns to silently match NOTHING, making hooks useless while appearing to work.

### The Problem

In bash git hooks (`.githooks/pre-commit`, `.githooks/pre-push`), grep/sed regex patterns use the standard regex escaping — one backslash:

```bash
# ✅ CORRECT - Single backslash in grep pattern
git diff --cached --name-only | grep -E '\.(md|markdown)$'

# ❌ WRONG - Double-escaped backslash (matches NOTHING!)
git diff --cached --name-only | grep -E '\\.(md|markdown)$'
```

The double-escaped pattern `\\.(md|markdown)$` looks for a literal backslash followed by a dot, NOT just a dot. Since filenames don't contain literal backslashes, **zero files match** and the hook silently skips all processing.

### Why This Is Dangerous

- **Silent failure**: The hook exits successfully (exit code 0) but does nothing
- **No warnings**: grep returns empty output, loop iterates zero times
- **Appears to work**: Commits succeed, pushes succeed — no visible error
- **CI catches it later**: Files reach CI unformatted, causing failures

### Testing Git Hooks After Modification

**ALWAYS run these tests after modifying ANY git hook:**

```bash
# 1. Check the pattern matches expected files
echo "test.md" | grep -E '\.(md|markdown)$'     # Should output: test.md
echo "test.md" | grep -E '\\.(md|markdown)$'    # Should output: (nothing - WRONG!)

# 2. Test the hook manually with a real file
# Stage a markdown file
git add docs/some-file.md

# Run the pre-commit hook manually
.githooks/pre-commit

# Verify the file was actually processed (check output for the filename)

# 3. Test the full commit cycle
git commit -m "test: verify hook works" --dry-run --verbose
```

### Checklist: After Modifying Git Hooks

- [ ] **Verify regex patterns use SINGLE backslashes** (not `\\` when you want `\`)
- [ ] **Test pattern matching manually** with `echo "filename" | grep -E 'pattern'`
- [ ] **Run the hook directly** (e.g., `.githooks/pre-commit`) and verify files are processed
- [ ] **Check hook output** — if it reports "0 files" or skips everything, the pattern is wrong
- [ ] **Test with actual files** — stage files of each type the hook should process
- [ ] **Verify CI passes** — run `npm run validate:prepush` before pushing
- [ ] **Cross-check `.prettierrc.json` overrides** — ensure Prettier overrides match `.gitattributes` for line endings

### Common Escaping Mistakes in Git Hooks

| Pattern Context      | ❌ Wrong (Double-Escaped) | ✅ Correct (Single Backslash) |
| -------------------- | ------------------------- | ----------------------------- | --------------- | -------- |
| Match file extension | `grep -E '\\.(md          | json)$'`                      | `grep -E '\.(md | json)$'` |
| Match any digit      | `grep -E '\\d+'`          | `grep -E '[0-9]+'`            |
| Match whitespace     | `grep -E '\\s+'`          | `grep -E '[[:space:]]+'`      |
| Match word boundary  | `grep -E '\\bword\\b'`    | `grep -E '\bword\b'`          |

### Prettier Configuration for `.github/**` Files

When modifying git hooks that format files, ensure `.prettierrc.json` has matching overrides for all file types in `.gitattributes`:

```jsonc
// .prettierrc.json - MUST include .github/** override for LF
{
  "overrides": [
    {
      "files": [
        "*.yml",
        "*.yaml",
        ".github/**/*.yml",
        ".github/**/*.yaml",
        ".github/**/*.md", // ← Critical! .github/** uses LF per .gitattributes
        "package.json",
        "*.sh"
      ],
      "options": {
        "endOfLine": "lf"
      }
    }
  ]
}
```

If `.gitattributes` specifies LF for `.github/**` but `.prettierrc.json` doesn't have a matching override, formatted files will have CRLF endings, causing CI failures.
