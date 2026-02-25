# Skill: Code Formatting

<!-- trigger: format, csharpier, prettier, style | After ANY file change (CSharpier/Prettier) | Core -->

**Trigger**: **MANDATORY** after creating or modifying ANY file.

---

## The Golden Rules

1. **Format IMMEDIATELY** after each file change (never batch)
2. **Run the correct formatter** for the file type
3. **Verify formatting worked** before moving on

---

## Quick Reference

| File Type                | Formatter | Command                              |
| ------------------------ | --------- | ------------------------------------ |
| C# (`.cs`)               | CSharpier | `dotnet tool run csharpier format .` |
| Markdown (`.md`)         | Prettier  | `npx prettier --write -- <file>`     |
| JSON (`.json`,`.asmdef`) | Prettier  | `npx prettier --write -- <file>`     |
| YAML (`.yml`,`.yaml`)    | Prettier  | `npx prettier --write -- <file>`     |
| Config files             | Prettier  | `npx prettier --write -- <file>`     |

---

## CSharpier (C# Files)

> **CRITICAL**: Pre-push hooks and CI/CD will REJECT commits with CSharpier formatting issues.

### When to Run

Run **IMMEDIATELY** after:

- Creating a new `.cs` file
- Modifying an existing `.cs` file (even a single line)
- ANY edit to ANY `.cs` file - no exceptions

**NEVER:**

- Batch multiple C# file edits before running CSharpier
- Wait until "before commit" to format
- Assume the code is already formatted correctly

### Commands

```bash
# Format all C# files
dotnet tool run csharpier format .

# Format specific file
dotnet tool run csharpier format Runtime/Core/Helper/Buffers.cs

# Check without modifying
dotnet tool run csharpier check .
```

### Troubleshooting

```bash
# If "tool not found" error:
dotnet tool restore
```

---

## Prettier (Non-C# Files)

> **CRITICAL**: Pre-push hooks REJECT commits with Prettier issues. Run Prettier IMMEDIATELY after editing ANY non-C# file.

### When to Run

Run **IMMEDIATELY** after editing:

- `.md` - Markdown documentation
- `.json`, `.asmdef`, `.asmref` - JSON and Unity assembly definitions
- `.yaml`, `.yml` - YAML configuration
- `.js`, `.ts`, `.jsx`, `.tsx` - JavaScript/TypeScript
- `.css`, `.scss` - Stylesheets
- `.html` - HTML files
- Config files (`.prettierrc`, `.eslintrc`)
- **`.devcontainer/devcontainer.json`** - Dev container configuration (often missed!)

### Commands

```bash
# Format a single file (RECOMMENDED)
npx prettier --write -- <file>

# Verify formatting
npx prettier --check -- <file>

# Check all files
npx prettier --check -- .

# Fix all files (emergency only)
npx prettier --write -- .
```

### Workflow Pattern

```text
1. Edit the file
2. Run: npx prettier --write -- <path/to/file>
3. Verify: npx prettier --check -- <path/to/file>
4. Move to next file
5. Repeat for each file
```

---

## Markdownlint (Structural Rules)

> **CRITICAL**: Prettier handles formatting but does NOT fix structural markdown issues. Run BOTH Prettier AND markdownlint on markdown files.

### What Each Tool Catches

| Tool         | Catches                                          | Misses                          |
| ------------ | ------------------------------------------------ | ------------------------------- |
| Prettier     | Spacing, indentation, line wrapping              | Structural rules (MD028, MD031) |
| markdownlint | Heading hierarchy, blank lines, code block rules | Formatting/spacing issues       |

### Required Workflow for Markdown

```bash
# STEP 1: Format with Prettier IMMEDIATELY after editing
npx prettier --write -- <file>

# STEP 2: Check structural rules
npm run lint:markdown

# STEP 3: Fix any errors, then re-run Prettier if you made changes

# STEP 4: Verify both pass
npx prettier --check -- <file>
npm run lint:markdown
```

### Common Markdownlint Rules

| Rule  | Issue                        | Fix                                          |
| ----- | ---------------------------- | -------------------------------------------- |
| MD028 | Blank line inside blockquote | Remove blank line between consecutive quotes |
| MD031 | No blank line around fences  | Add blank line before and after code blocks  |
| MD032 | No blank line around lists   | Add blank line before and after lists        |
| MD022 | No blank line after headings | Add blank line after `#` headings            |
| MD040 | Fenced code without language | Add language specifier (`csharp`, `bash`)    |

For complete markdown rules, see [markdown-reference](./markdown-reference.md).

---

## EOL Normalization

**All files must have CRLF line endings** (except `.sh` files which use LF).

### Commands

```bash
# Check for EOL issues
npm run eol:check

# Auto-fix EOL issues
npm run eol:fix
```

### When to Run

- After creating ANY new file
- Before committing (auto-runs in pre-commit hook)
- When CI fails with "LF issues" error

> **Why CRLF?** Unity projects require consistent line endings. Linux dev containers create files with LF by default, causing diffs and CI failures.

---

## Line Ending Configuration

Line endings are configured across multiple files that must stay synchronized:

| File               | Purpose                      |
| ------------------ | ---------------------------- |
| `.gitattributes`   | Controls git checkout        |
| `.prettierrc.json` | Controls Prettier formatting |
| `.yamllint.yaml`   | Controls YAML linting        |
| `.editorconfig`    | Controls IDE behavior        |

### LF Exceptions

These file types use LF (unix) endings:

- `.md` - Markdown files
- `.yml`, `.yaml` - YAML files
- `.sh` - Shell scripts
- `package.json`, `package-lock.json`

If you see line ending errors, verify all config files are synchronized. See [validation-troubleshooting](./validation-troubleshooting.md) for details.

---

## Full Workflow

After making changes:

```bash
# 1. Format non-C# files with Prettier
npx prettier --write -- <file>

# 2. Format C# code with CSharpier
dotnet tool run csharpier format .

# 3. Normalize line endings
npm run eol:fix

# 4. Generate meta files for any new files
./scripts/generate-meta.sh <new-file-path>

# 5. Run all validations
npm run validate:prepush
```

---

## Common Mistakes

### Wrong: Batching Formatting Until End

```text
1. Edit file1.md
2. Edit file2.json
3. Edit file3.cs
4. Run formatters at the end  <-- TOO LATE! You will forget files.
```

### Correct: Format Immediately After Each

```text
1. Edit file1.md -> npx prettier --write -- file1.md
2. Edit file2.json -> npx prettier --write -- file2.json
3. Edit file3.cs -> dotnet tool run csharpier format .
```

### Wrong: Only Formatting One Type

Prettier formats JSON, YAML, and JavaScript too, not just markdown.

### Wrong: Forgetting Config Files

Files like `.devcontainer/devcontainer.json`, `.config/dotnet-tools.json`, and `package.json` are all checked by prettier. When these files are updated (by tooling, CI, or manual edits), they must be formatted before committing.

### Wrong: Skipping Verification

Always verify with `--check` to confirm formatting succeeded.

---

## Pre-Push Hook Enforcement

The pre-push hook enforces all formatting. Commits will be REJECTED if files are not formatted.

If push fails:

1. Run `npx prettier --write -- .` to fix non-C# files
2. Run `dotnet tool run csharpier format .` to fix C# files
3. Run `npm run eol:fix` to fix line endings
4. Commit the formatting changes
5. Push again

---

## Integration with Editor

The repository's `.editorconfig` defines all formatting rules. CSharpier reads these settings automatically. Key rules enforced:

- 4-space indentation for `.cs` files
- `using` directives inside namespace
- Braces on new lines
- No trailing whitespace trimming (preserved)
- CRLF line endings for most files

---

## Skill File Additional Requirements

Skill files (`.llm/skills/*.md`) have additional size constraints beyond formatting:

```bash
# After editing ANY skill file, also run:
pwsh -NoProfile -File scripts/lint-skill-sizes.ps1
```

Files exceeding 500 lines will be rejected by the pre-commit hook. See [manage-skills](./manage-skills.md) for the complete skill editing workflow.

---

## Related Skills

- [markdown-reference](./markdown-reference.md) - Link formatting, structural rules
- [linter-reference](./linter-reference.md) - Detailed linter commands
- [validate-before-commit](./validate-before-commit.md) - Pre-commit validation workflow
- [validation-troubleshooting](./validation-troubleshooting.md) - Common errors and fixes
- [manage-skills](./manage-skills.md) - Skill file maintenance and size limits
