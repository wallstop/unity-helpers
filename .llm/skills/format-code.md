# Skill: Format Code

**Trigger**: After creating or modifying ANY C# file.

---

## Command

```bash
dotnet tool run csharpier format .
```

---

## When to Run

Run CSharpier after:

- Creating a new `.cs` file
- Modifying an existing `.cs` file
- Batch edits across multiple files
- Before asking user to review changes

---

## What CSharpier Does

- Applies consistent formatting across all C# files
- Respects `.editorconfig` settings
- Handles indentation, line breaks, spacing
- Runs automatically via pre-commit hook

---

## Full Workflow

After making C# changes:

```bash
# 1. Format code
dotnet tool run csharpier format .

# 2. Generate meta files for any new files
./scripts/generate-meta.sh <new-file-path>

# 3. Check for errors (let user run in Unity)
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

---

## Integration with Editor

The repository's `.editorconfig` defines all formatting rules. CSharpier reads these settings automatically. Key rules enforced:

- 4-space indentation for `.cs` files
- `using` directives inside namespace
- Braces on new lines
- No trailing whitespace trimming (preserved)
