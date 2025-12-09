# Claude Configuration

> **See [LLM_INSTRUCTIONS.md](./LLM_INSTRUCTIONS.md) for comprehensive AI agent guidelines.**

This repository uses consolidated LLM instructions. All guidelines for Claude (and other AI assistants) are maintained in `LLM_INSTRUCTIONS.md` to ensure consistency across tools.

## Quick Reference

- **Package**: `com.wallstop-studios.unity-helpers` (Unity 2021.3+)
- **Language**: C# with Unity-specific patterns
- **Style**: Explicit types, PascalCase naming, no underscores in method names

## Critical Rules

1. **NEVER use `git add` or `git commit`** — User handles git operations
2. **NO underscores in function names** — Use PascalCase throughout
3. **Avoid `var`** — Use explicit types everywhere
4. **No `async Task` tests** — Use `IEnumerator` with `[UnityTest]`
5. **Don't clone/copy repo** — Ask user to run tests and provide output

## File Reference

| File                              | Purpose                                  |
| --------------------------------- | ---------------------------------------- |
| `LLM_INSTRUCTIONS.md`             | **Primary source** — Complete guidelines |
| `AGENTS.md`                       | Quick reference for agents               |
| `.github/copilot-instructions.md` | GitHub Copilot configuration             |
