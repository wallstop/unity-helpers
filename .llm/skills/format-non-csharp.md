# Skill: Format Non-C# Files

**Trigger**: **MANDATORY** — After editing ANY non-C# file (markdown, JSON, YAML, JavaScript, config files).

> ⚠️ **CRITICAL**: This skill is NOT optional. Every non-C# file you edit MUST be formatted with Prettier IMMEDIATELY after modification. Pre-push hooks will REJECT commits with unformatted files.

---

## Summary

All non-C# files must be formatted with Prettier immediately after modification. This includes:

- Markdown documentation
- JSON configuration and assembly definitions
- YAML configuration and GitHub workflows
- JavaScript files
- Config files (`.prettierrc`, `.eslintrc`, etc.)

**Do NOT batch formatting** — run Prettier on each file immediately after editing it.

---

## When to Use

Run Prettier **IMMEDIATELY** after editing ANY file with these extensions:

- `.md` — Markdown documentation
- `.json` — JSON configuration files
- `.asmdef` — Unity assembly definitions
- `.asmref` — Unity assembly references
- `.yaml`, `.yml` — YAML configuration
- `.js` — JavaScript files
- `.jsx` — JavaScript React files
- `.ts` — TypeScript files
- `.tsx` — TypeScript React files
- `.css` — CSS stylesheets
- `.scss` — SCSS stylesheets
- `.html` — HTML files
- Config files without extensions (`.prettierrc`, `.eslintrc`)

---

## The Rule

> **🚨 THE GOLDEN RULE 🚨**: Run `npx prettier --write <file>` IMMEDIATELY after editing each file — do NOT batch.

```bash
npx prettier --write <path/to/file>
```

**No exceptions. No batching. No waiting until the end.**

---

## Workflow Pattern

### ✅ CORRECT Workflow (ALWAYS DO THIS)

```text
1. Edit the file
2. Run: npx prettier --write <path/to/file>
3. Verify: npx prettier --check <path/to/file>
4. Move to next file
5. Repeat steps 1-4 for each file
```

### Example

```bash
# Edit a markdown file
# ... make your changes ...

# IMMEDIATELY format it
npx prettier --write docs/features/new-feature.md

# Verify formatting worked
npx prettier --check docs/features/new-feature.md

# Now move to the next file
```

---

## File Types Covered

Prettier handles all of these file types:

| Extension                  | Description                |
| -------------------------- | -------------------------- |
| `.md`                      | Markdown documentation     |
| `.json`                    | JSON configuration         |
| `.asmdef`                  | Unity assembly definitions |
| `.asmref`                  | Unity assembly references  |
| `.yaml`, `.yml`            | YAML configuration         |
| `.js`                      | JavaScript                 |
| `.jsx`                     | JavaScript React           |
| `.ts`                      | TypeScript                 |
| `.tsx`                     | TypeScript React           |
| `.css`                     | CSS stylesheets            |
| `.scss`                    | SCSS stylesheets           |
| `.html`                    | HTML files                 |
| `.prettierrc`, `.eslintrc` | Config files               |
| `.graphql`                 | GraphQL schemas            |

---

## Common Mistakes

### ❌ WRONG: Waiting Until End of Task to Format

```text
1. Edit file1.md
2. Edit file2.json
3. Edit file3.yml
4. Run prettier at the end  ← TOO LATE! You will forget files.
```

**Why this fails:**

- You WILL forget which files you touched
- Running `prettier --write .` may not format all files you expect
- Issues compound and become harder to track
- Pre-push hooks WILL reject your commit

### ❌ WRONG: Only Formatting Markdown

```text
1. Edit README.md
2. npx prettier --write README.md  ← Good!
3. Edit package.json
4. Don't format package.json  ← WRONG! JSON needs formatting too!
```

**Remember:** Prettier formats JSON, YAML, and JavaScript too — not just markdown!

### ❌ WRONG: Forgetting to Verify Formatting Worked

```text
1. Edit file.md
2. npx prettier --write file.md
3. Move on without checking  ← WRONG! Always verify!
```

**Always verify:** Run `npx prettier --check <file>` to confirm formatting succeeded.

---

## Commands

### Format a Single File (RECOMMENDED)

```bash
npx prettier --write <file>

# Examples:
npx prettier --write docs/features/new-feature.md
npx prettier --write package.json
npx prettier --write .github/workflows/ci.yml
npx prettier --write .llm/skills/new-skill.md
```

### Verify a Single File

```bash
npx prettier --check <file>
```

### Verification Command (Entire Workspace)

```bash
# Check all files for formatting issues
npx prettier --check .
```

### Fix All Files (Emergency Only)

```bash
# Auto-fix all files at once (use only if you've made many mistakes)
npx prettier --write .
```

---

## Pre-push Hook Enforcement

> **⚠️ WARNING**: The pre-push hook enforces Prettier formatting. Commits will be REJECTED if files aren't formatted.

The pre-push hook runs `npx prettier --check .` automatically. If any files have formatting issues:

1. Your push will be rejected
2. You'll need to run `npx prettier --write .` to fix
3. You'll need to commit the formatting changes
4. Then push again

**Save yourself the trouble:** Format each file immediately after editing.

---

## Line Ending Configuration

Prettier uses line ending settings from `.prettierrc.json`. This repository has specific overrides:

| File Type                           | Line Ending | Configured In                                             |
| ----------------------------------- | ----------- | --------------------------------------------------------- |
| Markdown files (`.md`)              | LF (unix)   | `*.md` global pattern in `.prettierrc.json` override      |
| YAML files (`.yml`, `.yaml`)        | LF (unix)   | `*.yml`, `*.yaml` global patterns in `.prettierrc.json`   |
| GitHub workflow files               | LF (unix)   | Covered by `*.yml` global pattern (no separate override!) |
| `.github/**/*.md` files             | LF (unix)   | Covered by `*.md` global pattern (no separate override!)  |
| Shell scripts (`.sh`)               | LF (unix)   | `*.sh` global pattern in `.prettierrc.json` override      |
| `package.json`, `package-lock.json` | LF (unix)   | Explicit file patterns in `.prettierrc.json` override     |
| Most other files                    | CRLF        | `.prettierrc.json` default `endOfLine`                    |

> **Note**: Global patterns like `*.yml` match ALL files with that extension regardless of directory. Patterns like `.github/**/*.yml` are redundant because `*.yml` already covers them.

**Important**: Line ending configuration must be synchronized across multiple files:

- `.gitattributes` — Controls git checkout
- `.prettierrc.json` — Controls Prettier formatting
- `.yamllint.yaml` — Controls YAML linting (`new-lines: type: unix`)
- `.editorconfig` — Controls IDE behavior

If you see line ending errors, verify all config files are synchronized. See [validate-before-commit](./validate-before-commit.md#line-ending-configuration-consistency-critical) for details.

---

## Markdownlint Structural Rules

> **⚠️ CRITICAL**: Prettier handles formatting (spacing, indentation) but does NOT fix structural markdown issues. You MUST run BOTH Prettier AND markdownlint.

### Why Prettier Is Not Enough

Prettier and markdownlint catch **different issues**:

| Tool         | What It Handles                                          | What It Misses                           |
| ------------ | -------------------------------------------------------- | ---------------------------------------- |
| Prettier     | Spacing, indentation, line length, consistent formatting | Structural rules, semantic issues        |
| markdownlint | Structural rules, heading hierarchy, code block context  | Formatting/spacing (handled by Prettier) |

**Both must pass.** A file can pass Prettier but fail markdownlint.

### Common Markdownlint Rules Prettier Doesn't Fix

| Rule  | Issue                               | Example Problem                         | Fix                                     |
| ----- | ----------------------------------- | --------------------------------------- | --------------------------------------- |
| MD028 | Blank line inside blockquote        | Two blockquotes with blank line between | Remove blank line or merge blockquotes  |
| MD031 | Fenced code blocks need blank lines | Code fence immediately after text       | Add blank line before and after fences  |
| MD032 | Lists need surrounding blank lines  | List immediately after paragraph        | Add blank line before and after lists   |
| MD022 | Headings need blank line after      | Text immediately after heading          | Add blank line after every heading      |
| MD040 | Fenced code without language        | Opening fence without specifier         | Add language (`csharp`, `bash`, `text`) |

### MD028: Blank Line Inside Blockquote

```markdown
<!-- ❌ WRONG: Blank line between consecutive blockquotes (MD028) -->

> First blockquote paragraph.

> Second blockquote paragraph.

<!-- ✅ CORRECT: No blank line (continuous blockquote) -->

> First blockquote paragraph.
> Second blockquote paragraph.

<!-- ✅ ALSO CORRECT: Separate blockquotes with non-blank content -->

> First blockquote.

Some regular text between them.

> Second blockquote.
```

### MD031: Fenced Code Blocks Need Blank Lines

```markdown
<!-- ❌ WRONG: No blank line before code fence (MD031) -->

Here is some code:
\`\`\`csharp
public void Example() { }
\`\`\`

<!-- ✅ CORRECT: Blank lines around code fence -->

Here is some code:

\`\`\`csharp
public void Example() { }
\`\`\`

More text after the code.
```

### Required Workflow for Markdown Files

```bash
# STEP 1: Format with Prettier IMMEDIATELY after editing
npx prettier --write <file>

# STEP 2: Run markdownlint to catch structural issues
npm run lint:markdown

# STEP 3: Fix any markdownlint errors and repeat
# (Prettier may need to run again after manual fixes)

# STEP 4: Verify both pass before proceeding
npx prettier --check <file>
npm run lint:markdown
```

### Full Markdown Validation

```bash
# Run all markdown checks
npm run lint:markdown     # Structural rules (MD028, MD031, etc.)
npm run lint:docs         # Link validation
npm run lint:spelling     # Spell check
npx prettier --check .    # Formatting
```

---

## Verifying Prettier Config Matches `.gitattributes`

> **🚨 CRITICAL**: When `.gitattributes` and `.prettierrc.json` disagree on line endings, CI will fail because git checks out files with one ending but Prettier expects another.

### The Configuration Mismatch Problem

If `.gitattributes` specifies LF for certain paths but `.prettierrc.json` doesn't have matching overrides, you get this failure pattern:

1. Git checks out file with LF endings (per `.gitattributes`)
2. Prettier formats the file but uses CRLF (default `endOfLine`)
3. The file now has CRLF endings
4. CI's line ending check fails

### Checking for Mismatches

When modifying either `.gitattributes` or `.prettierrc.json`:

```bash
# 1. List all patterns with explicit LF in .gitattributes
grep -E 'eol=lf|text=lf' .gitattributes

# 2. Verify each pattern has a matching Prettier override
# Open .prettierrc.json and check the "overrides" array
cat .prettierrc.json | jq '.overrides'

# 3. Test specific file types (global patterns match all directories)
npx prettier --check "*.md"   # Matches all .md files including .github/
npx prettier --check "*.yml"  # Matches all .yml files including .github/workflows/
```

### Current `.gitattributes` LF Patterns vs `.prettierrc.json` Overrides

| `.gitattributes` Pattern   | `.prettierrc.json` Override                                     |
| -------------------------- | --------------------------------------------------------------- |
| `*.yml text eol=lf`        | `"*.yml"` in overrides with `endOfLine: lf`                     |
| `*.yaml text eol=lf`       | `"*.yaml"` in overrides with `endOfLine: lf`                    |
| `*.md text eol=lf`         | `"*.md"` in overrides with `endOfLine: lf`                      |
| `.github/** text eol=lf`   | **None needed** — covered by `*.yml`, `*.yaml`, `*.md` patterns |
| `package.json text eol=lf` | `"package.json"` in overrides                                   |
| `*.sh text eol=lf`         | `"*.sh"` in overrides                                           |

> **Important**: Prettier glob patterns match files **globally** across all directories. The pattern `*.yml` matches `file.yml`, `dir/file.yml`, and `.github/workflows/ci.yml` equally. You do NOT need separate `.github/**/*.yml` patterns.

### Adding Missing Overrides

If you find a mismatch, update `.prettierrc.json`:

```jsonc
{
  "overrides": [
    {
      "files": [
        // Global extension patterns - these match ALL files with these extensions
        // including .github/**, .llm/**, and all subdirectories
        "*.yml",
        "*.yaml",
        "*.md",
        "*.sh",
        // Specific file overrides for files at specific paths
        "package.json",
        "package-lock.json",
        "_includes/*.html"
      ],
      "options": {
        "endOfLine": "lf"
      }
    }
  ]
}
```

> **❌ Do NOT add redundant patterns like `.github/**/_.yml`** — the global `_.yml` pattern already matches these files.

After updating, format the config file:

```bash
npx prettier --write .prettierrc.json
```

---

## Integration with Other Validations

This skill is part of the full validation workflow. Before completing any task:

```bash
# Run all validations (includes Prettier check)
npm run validate:prepush
```

See also:

- [Format Code](./format-code.md) — Full formatting guide including C#
- [Validate Before Commit](./validate-before-commit.md) — Complete validation checklist
