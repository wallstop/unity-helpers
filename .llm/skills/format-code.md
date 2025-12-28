# Skill: Format Code

**Trigger**: After creating or modifying ANY file.

---

## Commands

### Prettier (Markdown, JSON, YAML, Config Files)

> **⚠️ CRITICAL**: Pre-push hooks REJECT commits with Prettier issues. Run Prettier IMMEDIATELY after editing ANY non-C# file.

```bash
# Format a specific file (RECOMMENDED - run immediately after each edit)
npx prettier --write <file>

# Examples:
npx prettier --write .llm/skills/create-test.md
npx prettier --write package.json
npx prettier --write .github/workflows/ci.yml

# Check all files for formatting issues
npx prettier --check .

# Auto-fix all files at once
npx prettier --write .
```

**File types handled by Prettier:**

- Markdown (`.md`)
- JSON (`.json`, `.asmdef`, `.asmref`)
- YAML (`.yml`, `.yaml`)
- Config files (`.prettierrc`, etc.)

### C# Formatting (CSharpier)

````bash
dotnet tool run csharpier format .

### EOL Normalization (Critical!)

**All files must have CRLF line endings** (except `.sh` files which use LF).

```bash
# Check for EOL issues
npm run eol:check

# Auto-fix EOL issues
npm run eol:fix
````

> **Why CRLF?** Unity projects require consistent line endings. Linux dev containers create files with LF by default, causing diffs and CI failures.

---

## When to Run

### Prettier (non-C# files)

Run **IMMEDIATELY** after:

- Editing ANY markdown file (`.md`)
- Editing ANY JSON file (`.json`, `.asmdef`, `.asmref`)
- Editing ANY YAML file (`.yml`, `.yaml`)
- Editing config files
- Before asking user to review changes

### CSharpier (C# files)

Run after:

- Creating a new `.cs` file
- Modifying an existing `.cs` file
- Batch edits across multiple files
- Before asking user to review changes

### EOL Normalization

Run:

- After creating ANY new file (`.cs`, `.meta`, `.json`, `.md`, `.yaml`, etc.)
- Before committing (auto-runs in pre-commit hook)
- When CI fails with "LF issues" error

---

## What These Tools Do

### CSharpier

- Applies consistent formatting across all C# files
- Respects `.editorconfig` settings
- Handles indentation, line breaks, spacing
- Runs automatically via pre-commit hook

### EOL Normalization (`normalize-eol.ps1`)

- Converts LF → CRLF for most files
- Keeps LF for shell scripts (`.sh`)
- Removes UTF-8 BOM (disallowed)
- Runs automatically via pre-commit hook

---

## Full Workflow

After making changes:

```bash
# 1. Format non-C# files with Prettier
npx prettier --write <file>  # Or: npx prettier --write .

# 2. Format C# code with CSharpier
dotnet tool run csharpier format .

# 3. Normalize line endings (for all file types)
npm run eol:fix

# 4. Generate meta files for any new files
./scripts/generate-meta.sh <new-file-path>

# 5. Check for errors (let user run in Unity)
```

### Quick Reference: Which Formatter?

| File Type         | Formatter | Command                              |
| ----------------- | --------- | ------------------------------------ |
| `.cs`             | CSharpier | `dotnet tool run csharpier format .` |
| `.md`             | Prettier  | `npx prettier --write <file>`        |
| `.json`/`.asmdef` | Prettier  | `npx prettier --write <file>`        |
| `.yml`/`.yaml`    | Prettier  | `npx prettier --write <file>`        |

---

## Troubleshooting

### Tool Not Found

```bash
# Restore .NET tools
dotnet tool restore
```

### Format Specific File

```bash
dotnet tool run csharpier format Runtime/Core/Helper/Buffers.cs
```

### Check Without Modifying

```bash
dotnet tool run csharpier check .
```

### LF Issues on Push

If push fails with "LF issues: N", run:

```bash
npm run eol:fix
```

This is auto-fixed by the pre-commit and pre-push hooks, but may be needed if hooks didn't run.

---

## Integration with Editor

The repository's `.editorconfig` defines all formatting rules. CSharpier reads these settings automatically. Key rules enforced:

- 4-space indentation for `.cs` files
- `using` directives inside namespace
- Braces on new lines
- No trailing whitespace trimming (preserved)
- **CRLF line endings** for all files (except `.sh`)

---

## Git Hooks (Automatic)

Both hooks auto-fix EOL issues:

- **pre-commit**: Normalizes EOL for all tracked files before commit
- **pre-push**: Normalizes and verifies EOL before push
