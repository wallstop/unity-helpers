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

## Integration with Other Validations

This skill is part of the full validation workflow. Before completing any task:

```bash
# Run all validations (includes Prettier check)
npm run validate:prepush
```

See also:

- [Format Code](./format-code.md) — Full formatting guide including C#
- [Validate Before Commit](./validate-before-commit.md) — Complete validation checklist
