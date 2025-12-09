# LLM Agent Instructions

This document provides comprehensive guidelines for AI assistants working with this repository.
All agent configuration files (AGENTS.md, CLAUDE.md, .github/copilot-instructions.md) should reference this file.

---

## Repository Overview

**Package**: `com.wallstop-studios.unity-helpers` (Unity 2021.3+)
**License**: MIT
**Repository**: <https://github.com/wallstop/unity-helpers>

A Unity package providing inspector attributes, custom drawers, editor tools, spatial data structures (QuadTree, KDTree, RTree, OcTree), PRNGs, serialization utilities, effects systems, relational components, and DI integrations.

---

## Project Structure

```text
Runtime/           # Runtime C# libraries
  Core/            # Attributes, DataStructures, Extensions, Helpers, Math, Models, Random, Serialization, Threading
  Utils/           # Utility components (singletons, comparers, texture tools, etc.)
  Binaries/        # Binary dependencies
  Integrations/    # DI framework integrations (VContainer, Zenject, Reflex)
  Protobuf-Net/    # Protocol Buffers support
  Settings/        # Runtime settings
  Tags/            # Tag system
  Visuals/         # Visual components

Editor/            # Editor-only tooling and UIElements/USS
  AssetProcessors/ # Asset import/export processors
  Core/            # Core editor utilities
  CustomDrawers/   # Property drawers for attributes
  CustomEditors/   # Custom inspectors
  Extensions/      # Editor extension methods
  Internal/        # Internal editor utilities
  Persistence/     # Editor persistence helpers
  Settings/        # Editor settings
  Sprites/         # Sprite tools
  Styles/          # USS styles
  Tags/            # Tag editor tools
  Tools/           # Editor tools and windows
  Utils/           # Editor utilities
  Visuals/         # Visual editor components

Tests/
  Runtime/         # PlayMode tests mirroring Runtime/ structure
  Editor/          # EditMode tests mirroring Editor/ structure
  Core/            # Shared test utilities

Shaders/           # Shader files
Styles/            # Global USS styles
URP/               # Universal Render Pipeline assets
Samples~/          # Sample projects (DI integrations)
docs/              # Documentation
scripts/           # Build/lint scripts
```

---

## Build, Test, and Development Commands

### Setup

```bash
npm run hooks:install      # Install git hooks
dotnet tool restore        # Restore .NET tools (CSharpier, etc.)
```

### Formatting

```bash
dotnet tool run CSharpier format   # Format C# (also runs automatically via pre-commit)
```

### Linting

```bash
npm run lint:docs                              # Lint documentation links
node ./scripts/run-doc-link-lint.js --verbose  # Verbose link linting
```

### Testing

Tests require Unity 2021.3+. Add this package to a Unity project and use the Test Runner.

```bash
# CLI example (EditMode)
Unity -batchmode -projectPath <Project> -runTests -testPlatform EditMode -testResults ./TestResults.xml -quit

# CLI example (PlayMode)
Unity -batchmode -projectPath <Project> -runTests -testPlatform PlayMode -testResults ./TestResults.xml -quit
```

**Important**: Do not copy or clone this repository elsewhere. If you need test results, ask the user—they will run the tests and provide the output.

---

## Coding Style & Naming Conventions

### Formatting Rules

| File Type                      | Indentation | Notes           |
| ------------------------------ | ----------- | --------------- |
| `*.cs`                         | 4 spaces    | C# source files |
| `*.json`, `*.yaml`, `*.asmdef` | 2 spaces    | Config files    |

- **Line endings**: CRLF
- **Encoding**: UTF-8 (no BOM)
- **Trailing whitespace**: Preserved (see `.editorconfig`)

### C# Style

- **Explicit types over `var`**: Always use explicit type declarations
- **Braces required**: All control structures must use braces
- **Using placement**: `using` directives go inside namespace
- **No nullable reference types**: Do not use `?` nullable annotations
- **No regions**: Never use `#region` anywhere

### Naming Conventions

| Element               | Convention  | Example                                |
| --------------------- | ----------- | -------------------------------------- |
| Types, public members | PascalCase  | `SerializableDictionary`, `GetValue()` |
| Fields, locals        | camelCase   | `keyValue`, `itemCount`                |
| Interfaces            | `I` prefix  | `IResolver`, `ISpatialTree`            |
| Type parameters       | `T` prefix  | `TKey`, `TValue`                       |
| Events                | `On` prefix | `OnValueChanged`, `OnItemAdded`        |
| Constants (public)    | PascalCase  | `DefaultCapacity`                      |

### Critical Rules

1. **NO underscores in ANY method names** — This applies to ALL code (production AND test):
   - ✅ `GetInvocationStatusNoActiveInvocationReturnsZeroRunningCount`
   - ❌ `GetInvocationStatus_NoActiveInvocation_ReturnsZeroRunningCount`
   - ✅ `ValidateInputReturnsErrorWhenEmpty`
   - ❌ `ValidateInput_Returns_Error_When_Empty`

2. **Avoid `var`**: Use expressive types everywhere

3. **NEVER use `#region`** — No regions anywhere in the codebase (production OR test code):
   - ❌ `#region Helper Methods`
   - ❌ `#endregion`
   - Organize code through class structure and file organization instead

4. **No nullable reference types**: Don't use `string?`, `object?`, etc.

5. **One file per MonoBehaviour/ScriptableObject** — Each `MonoBehaviour` or `ScriptableObject` class MUST have its own dedicated `.cs` file (production AND test code):
   - ✅ `MyTestComponent.cs` containing only `class MyTestComponent : MonoBehaviour`
   - ✅ `TestScriptableObject.cs` containing only `class TestScriptableObject : ScriptableObject`
   - ❌ Multiple MonoBehaviours in the same file
   - ❌ Test helper MonoBehaviours defined inside test class files
   - This is a Unity requirement for proper serialization and asset creation

---

## Reflection & API Access

### General Principles

- **Avoid runtime reflection** in favor of explicit APIs and compiler-checked contracts
- **Prefer `internal` visibility** with `[InternalsVisibleTo]` over reflection for test access
- **Use `nameof(...)`** instead of magic strings when referencing members
- **Centralize string references** when Unity serialization mandates them, and document why

### Guidelines by Context

| Context                | Approach                                               |
| ---------------------- | ------------------------------------------------------ |
| Test access to helpers | Promote to `internal` + `[InternalsVisibleTo]`         |
| Unity serialization    | Centralize strings, document necessity                 |
| Editor tooling         | Use `internal` visibility when possible                |
| Shipping/runtime code  | **Never use reflection** unless absolutely unavoidable |

If reflection is unavoidable (e.g., Unity serialization callbacks), document the reason and constrain it to the narrowest surface possible.

---

## Testing Guidelines

### Frameworks

- NUnit
- Unity Test Framework (`[Test]`, `[UnityTest]`)

### Structure

- Mirror source folders: `Tests/Runtime/` mirrors `Runtime/`, `Tests/Editor/` mirrors `Editor/`
- Name test files `*Tests.cs` (e.g., `SerializableDictionaryTests.cs`)

### Test Method Naming

**Use PascalCase with NO underscores**:

- ✅ `AsyncTaskInvocationCompletesAndRecordsHistory`
- ❌ `AsyncTask_Invocation_Completes_And_Records_History`

### Data-Driven Test Naming

When using `[TestCase]`, `[Values]`, or other data-driven attributes with string names, use `.` (dot) or no delimiter—**never underscores**:

- ✅ `[TestCase("Input.Valid")]` or `[TestCase("InputValid")]`
- ✅ `[Values("Option.First", "Option.Second")]`
- ❌ `[TestCase("Input_Valid")]`
- ❌ `[Values("Option_First", "Option_Second")]`

### Rules

1. **NO underscores in test method names**: Use PascalCase throughout (see Critical Rules above)
2. **NEVER use `#region`**: No regions in test code (or any code)
3. **One file per MonoBehaviour/ScriptableObject**: Test helper components must be in their own dedicated files (see Critical Rules above)
4. **No `async Task` test methods**: Unity Test Runner doesn't support them. Use `IEnumerator` with `[UnityTest]`
5. **No `Assert.ThrowsAsync`**: It doesn't exist in Unity's NUnit version
6. **Unity Object null checks**: Use `thing != null` / `thing == null` directly (Unity's Object equality)
7. **Minimal comments**: Rely on expressive naming and assertions
8. **No `[Description]` annotations**: Don't use Description attributes on tests
9. **Prefer EditMode**: Use fast EditMode tests where possible
10. **Deterministic tests**: Avoid flaky tests; use timeouts for long-running tests (see `Tests/Runtime/RuntimeTestTimeouts.cs`)

---

## Commit & Pull Request Guidelines

### Commits

- Short, imperative summaries: "Fix JSON serialization for FastVector"
- Group related changes in single commits

### Pull Requests

- Clear description of changes
- Link related issues (`#123`)
- Include before/after screenshots for editor UI changes
- Update relevant documentation
- Ensure tests and linters pass
- Version bumps in `package.json` should be deliberate (typically in release PRs)

---

## Security & Configuration

- **Do not commit**: `Library/`, `obj/`, secrets, tokens
- **Do commit**: `.meta` files for all assets
- **Target Unity version**: 2021.3+
- **Verify `.asmdef` references** when adding new namespaces
- **NPM publishing**: Uses GitHub Secrets; never commit tokens

---

## Agent-Specific Rules

### Scope & Behavior

- Keep changes minimal and focused
- Follow `.editorconfig` formatting rules
- Respect folder boundaries (Runtime vs Editor)
- Update docs and tests alongside code changes

### Git Operations

**CRITICAL: NEVER use `git add` or `git commit` commands.**

The user will handle all git staging and committing. You may:

- ✅ Use read-only git commands: `git status`, `git log`, `git diff`
- ❌ Never use: `git add`, `git commit`, `git push`, `git reset`

Share diffs/changes and wait for user approval.

### Paths

Never hard-code or document machine-specific absolute paths. Use:

- Paths relative to repo root
- Environment variables

### Test Execution

Do not copy or clone this repository elsewhere. If you need test results, ask the user to run the tests and provide the output.

---

## Assembly Definitions

| Assembly                                     | Purpose       |
| -------------------------------------------- | ------------- |
| `WallstopStudios.UnityHelpers`               | Runtime code  |
| `WallstopStudios.UnityHelpers.Editor`        | Editor code   |
| `WallstopStudios.UnityHelpers.Tests.Runtime` | Runtime tests |
| `WallstopStudios.UnityHelpers.Tests.Editor`  | Editor tests  |

---

## Key Features Reference

- **Inspector Attributes**: `[WRequired]`, `[WReadOnly]`, `[WInLine]`, `[WButton]`, `[WEnumToggleButtons]`, etc.
- **Data Structures**: `SerializableDictionary<K,V>`, `SerializableHashSet<T>`, QuadTree, KDTree, RTree, OcTree
- **PRNGs**: `DotNetRandom`, `XorShiftRandom`, `PcgRandom`, etc.
- **Serialization**: JSON, Protobuf support, `IBufferWriter` helpers
- **DI Integrations**: VContainer, Zenject, Reflex
- **Editor Tools**: Animation Event Editor, Sprite Tools, Prefab Checker, Multi-File Selector
