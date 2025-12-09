# Repository Guidelines

> **See [LLM_INSTRUCTIONS.md](./LLM_INSTRUCTIONS.md) for comprehensive, up-to-date AI agent guidelines.**

This file provides a quick reference. For full details, refer to the consolidated instructions.

---

## Quick Reference

### Project Structure

- `Runtime/` — Runtime C# libraries (assembly definitions per area)
- `Editor/` — Editor-only tooling and UIElements/USS
- `Tests/Runtime`, `Tests/Editor` — NUnit/UTF tests mirroring source folders
- `Shaders/`, `Styles/`, `URP/` — Rendering assets with `.meta` files
- `docs/` — Developer guides and references
- `package.json` — Unity package metadata + helper scripts

### Essential Commands

```bash
npm run hooks:install              # Install git hooks
dotnet tool restore                # Restore .NET tools
dotnet tool run CSharpier format   # Format C#
npm run lint:docs                  # Lint documentation links
```

### Critical Coding Rules

1. **NO underscores in ANY method names** — Use PascalCase throughout (production AND test code)
2. **NEVER use `#region`** — No regions anywhere (production OR test code)
3. **One file per MonoBehaviour/ScriptableObject** — Each must have its own dedicated `.cs` file
4. **Avoid `var`** — Use explicit types
5. **No nullable reference types** — Don't use `?` annotations
6. **No `async Task` tests** — Use `IEnumerator` with `[UnityTest]`

### Agent Rules

- **NEVER use `git add` or `git commit`** — User handles git operations
- **Don't clone/copy repo** — Ask user to run tests
- **No absolute paths** — Use relative paths or environment variables

---

For complete guidelines including reflection policies, testing patterns, naming conventions, and more, see **[LLM_INSTRUCTIONS.md](./LLM_INSTRUCTIONS.md)**.
