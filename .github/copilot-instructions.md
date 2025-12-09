# GitHub Copilot Instructions

> **See [LLM_INSTRUCTIONS.md](../LLM_INSTRUCTIONS.md) for comprehensive AI agent guidelines.**

This repository uses consolidated LLM instructions. All guidelines for Copilot (and other AI assistants) are maintained in `LLM_INSTRUCTIONS.md` at the repository root.

## Package Information

- **Name**: `com.wallstop-studios.unity-helpers`
- **Unity Version**: 2021.3+
- **License**: MIT

## Critical Rules Summary

### Code Style

- Use **explicit types** instead of `var`
- **NO underscores in ANY method names** — PascalCase throughout (production AND test code)
- **NEVER use `#region`** — No regions anywhere in the codebase (production OR test code)
- **One file per MonoBehaviour/ScriptableObject** — Each must have its own dedicated `.cs` file
- Never use nullable reference types (`?` annotations)
- Place `using` directives inside namespace

### Testing

- Use `[Test]` for synchronous tests
- Use `[UnityTest]` with `IEnumerator` for async tests (NOT `async Task`)
- **NO underscores in test method names**: `MethodConditionExpectedResult` (PascalCase only)
- **NEVER use `#region`** in test code (or any code)
- **One file per MonoBehaviour/ScriptableObject** — Test helper components need their own files
- Check Unity Objects with `!= null` / `== null` directly

### Agent Behavior

- **NEVER** use `git add` or `git commit` — User handles all git operations
- Don't copy or clone the repository — Ask user to run tests
- Use relative paths or environment variables, never absolute paths

## File Reference

For complete guidelines including:

- Project structure details
- Reflection policies
- Testing patterns
- Naming conventions
- Commit guidelines

See **[LLM_INSTRUCTIONS.md](../LLM_INSTRUCTIONS.md)**
