# Skill: Format Code

**Trigger**: After creating or modifying ANY C# file.

---

## Commands

### C# Formatting

```bash
dotnet tool run csharpier format .
```

### EOL Normalization (Critical!)

**All files must have CRLF line endings** (except `.sh` files which use LF).

```bash
# Check for EOL issues
npm run eol:check

# Auto-fix EOL issues
npm run eol:fix
```

> **Why CRLF?** Unity projects require consistent line endings. Linux dev containers create files with LF by default, causing diffs and CI failures.

---

## When to Run

Run formatting after:

- Creating a new `.cs` file
- Modifying an existing `.cs` file
- Batch edits across multiple files
- Before asking user to review changes

Run EOL normalization:

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
# 1. Normalize line endings (for all file types)
npm run eol:fix

# 2. Format C# code
dotnet tool run csharpier format .

# 3. Generate meta files for any new files
./scripts/generate-meta.sh <new-file-path>

# 4. Check for errors (let user run in Unity)
```

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
