# LLM Agent Instructions

This document provides guidelines for AI assistants working with this repository. Procedural skills are in the [skills/](./skills/) directory.

---

## Repository Overview

**Package**: `com.wallstop-studios.unity-helpers`
**Version**: 3.0.0
**Unity Version**: 2021.3+ (LTS)
**License**: MIT
**Repository**: <https://github.com/wallstop/unity-helpers>
**Root Namespace**: `WallstopStudios.UnityHelpers`

### Package Goals

Unity Helpers eliminates repetitive boilerplate code with production-ready utilities that are 10-100x faster than writing from scratch. It provides:

- **Professional Inspector Tooling** — Odin Inspector-level features for free (grouping, buttons, conditional display, toggle grids)
- **Zero-Boilerplate Component Wiring** — Auto-wire components using hierarchy-aware attributes
- **High-Performance Utilities** — 10-15x faster PRNGs, O(log n) spatial queries, zero-allocation pooling
- **Production-Ready Serialization** — Unity-aware JSON/Protobuf with schema evolution
- **Data-Driven Effects System** — Designer-friendly buffs/debuffs as ScriptableObjects
- **20+ Editor Tools** — Automate sprite, animation, texture, and prefab workflows

### Core Design Principles

1. **Zero boilerplate** — APIs handle tedious work; expressive, self-documenting code
2. **Performance-proven** — Measurable speed improvements with benchmarks
3. **Production-ready** — 8,000+ tests, IL2CPP/WebGL compatible, shipped in commercial games
4. **DRY architecture** — Extract common patterns into lightweight, reusable abstractions
5. **Self-documenting code** — Minimal comments; rely on descriptive names and obvious call patterns

---

## Project Structure

```text
Runtime/                   # Runtime C# libraries
  Core/
    Attributes/            # Inspector & component attributes
    DataStructure/         # Spatial trees, heaps, queues, tries, cyclic buffers
    Extension/             # Extension methods for Unity types, collections, strings, math
    Helper/                # Buffers, pooling, singletons, compression, logging
    Math/                  # Math utilities, ballistics, geometry
    Model/                 # Serializable types (Dictionary, HashSet, Nullable, Type, Guid)
    OneOf/                 # Discriminated unions
    Random/                # 15+ PRNG implementations with IRandom interface
    Serialization/         # JSON/Protobuf serialization with Unity type converters
    Threading/             # Thread pools, main thread dispatcher, guards
  Tags/                    # Effects/attribute system (AttributeEffect, TagHandler, Cosmetics)
  Visuals/                 # Visual components (EnhancedImage, LayeredImage)

Editor/                    # Editor-only tooling
  CustomDrawers/           # Property drawers for all custom attributes
    Odin/                  # Odin Inspector-specific drawers (9 files)
    Utils/                 # Shared drawer utilities (planned)
  CustomEditors/           # Custom inspectors (including Odin inspectors)
  Tools/                   # Editor windows (Animation Creator, Texture tools, etc.)

Tests/
  Runtime/                 # PlayMode tests mirroring Runtime/ structure
  Editor/                  # EditMode tests mirroring Editor/ structure
    CustomDrawers/Odin/    # Odin drawer tests
    CustomEditors/         # Inspector tests (including Odin)
    TestTypes/             # Editor test helper types (SO/MB test targets)
      Odin/                # Odin-specific test SO/MB types
      SharedEnums/         # Shared test enums
  Core/                    # Shared test utilities
    TestTypes/             # Shared test helper types (relational component testers)
      Enums/               # Shared test enums (TestAttributeType, TestValidationLevel)

Samples~/                  # Sample projects (imported via Package Manager)
```

---

## Skills Reference

Invoke these skills for specific tasks.

**Regenerate with**: `pwsh -NoProfile -File scripts/generate-skills-index.ps1`

<!-- BEGIN GENERATED SKILLS INDEX -->
<!-- Generated: 2026-01-09 18:37:57 UTC -->
<!-- Command: pwsh -NoProfile -File scripts/generate-skills-index.ps1 -->

### Core Skills (Always Consider)

| Skill                                                                    | When to Use                                                      |
| ------------------------------------------------------------------------ | ---------------------------------------------------------------- |
| [avoid-magic-strings](./skills/avoid-magic-strings.md)                   | ALL code - use nameof() not strings                              |
| [avoid-reflection](./skills/avoid-reflection.md)                         | ALL code - never reflect on our own types                        |
| [create-csharp-file](./skills/create-csharp-file.md)                     | Creating any new .cs file                                        |
| [create-editor-tool](./skills/create-editor-tool.md)                     | Creating Editor windows and inspectors                           |
| [create-enum](./skills/create-enum.md)                                   | Creating a new enum type                                         |
| [create-property-drawer](./skills/create-property-drawer.md)             | Creating PropertyDrawers for custom attributes                   |
| [create-scriptable-object](./skills/create-scriptable-object.md)         | Creating ScriptableObject data assets                            |
| [create-test](./skills/create-test.md)                                   | Writing or modifying test files                                  |
| [create-unity-meta](./skills/create-unity-meta.md)                       | After creating ANY new file or folder                            |
| [defensive-editor-programming](./skills/defensive-editor-programming.md) | Editor code - handle Unity Editor edge cases                     |
| [defensive-programming](./skills/defensive-programming.md)               | ALL code - never throw, handle gracefully                        |
| [editor-caching-patterns](./skills/editor-caching-patterns.md)           | Caching strategies for Editor code                               |
| [formatting](./skills/formatting.md)                                     | After ANY file change (CSharpier/Prettier)                       |
| [git-hook-patterns](./skills/git-hook-patterns.md)                       | Pre-commit hook safety and configuration                         |
| [git-safe-operations](./skills/git-safe-operations.md)                   | Scripts or hooks that interact with git index                    |
| [git-staging-helpers](./skills/git-staging-helpers.md)                   | PowerShell/Bash helpers for safe git staging                     |
| [high-performance-csharp](./skills/high-performance-csharp.md)           | ALL code - zero allocation patterns                              |
| [investigate-test-failures](./skills/investigate-test-failures.md)       | ANY test failure - investigate before fixing                     |
| [linter-reference](./skills/linter-reference.md)                         | Detailed linter commands, configurations                         |
| [manage-skills](./skills/manage-skills.md)                               | Creating, updating, splitting, consolidating, or removing skills |
| [markdown-reference](./skills/markdown-reference.md)                     | Link formatting, escaping, linting rules                         |
| [no-regions](./skills/no-regions.md)                                     | ALL C# code - never use #region/#endregion                       |
| [prefer-logging-extensions](./skills/prefer-logging-extensions.md)       | Unity logging in UnityEngine.Object classes                      |
| [search-codebase](./skills/search-codebase.md)                           | Finding code, files, or patterns                                 |
| [test-data-driven](./skills/test-data-driven.md)                         | Data-driven testing with TestCase and TestCaseSource             |
| [test-naming-conventions](./skills/test-naming-conventions.md)           | Test method and TestName naming rules                            |
| [test-odin-drawers](./skills/test-odin-drawers.md)                       | Odin Inspector drawer testing patterns                           |
| [test-parallelization-rules](./skills/test-parallelization-rules.md)     | Unity Editor test threading constraints                          |
| [test-unity-lifecycle](./skills/test-unity-lifecycle.md)                 | Track(), DestroyImmediate, object cleanup                        |
| [update-documentation](./skills/update-documentation.md)                 | After ANY feature/bug fix/API change                             |
| [validate-before-commit](./skills/validate-before-commit.md)             | Before completing any task (run linters!)                        |
| [validation-troubleshooting](./skills/validation-troubleshooting.md)     | Common validation errors, CI failures, fixes                     |

### Performance Skills

| Skill                                                                | When to Use                                       |
| -------------------------------------------------------------------- | ------------------------------------------------- |
| [avoid-allocations](./skills/avoid-allocations.md)                   | Avoiding heap allocations and boxing              |
| [gc-architecture-unity](./skills/gc-architecture-unity.md)           | Understanding Unity GC, incremental GC, manual GC |
| [linq-elimination-patterns](./skills/linq-elimination-patterns.md)   | Converting LINQ to zero-allocation loops          |
| [memory-allocation-traps](./skills/memory-allocation-traps.md)       | Finding hidden allocation sources                 |
| [mobile-xr-optimization](./skills/mobile-xr-optimization.md)         | Mobile, VR/AR, 90+ FPS targets                    |
| [optimize-unity-physics](./skills/optimize-unity-physics.md)         | Physics colliders, raycasts, non-alloc            |
| [optimize-unity-rendering](./skills/optimize-unity-rendering.md)     | Materials, shaders, batching                      |
| [performance-audit](./skills/performance-audit.md)                   | Reviewing performance-sensitive code              |
| [profile-debug-performance](./skills/profile-debug-performance.md)   | Profiling, debugging, measuring performance       |
| [refactor-to-zero-alloc](./skills/refactor-to-zero-alloc.md)         | Converting allocating code to zero-allocation     |
| [unity-performance-patterns](./skills/unity-performance-patterns.md) | Unity-specific optimizations (APIs, pooling)      |
| [use-array-pool](./skills/use-array-pool.md)                         | Working with temporary arrays                     |
| [use-pooling](./skills/use-pooling.md)                               | Working with temporary collections                |

### Feature Skills

| Skill                                                                          | When to Use                                            |
| ------------------------------------------------------------------------------ | ------------------------------------------------------ |
| [add-inspector-attribute](./skills/add-inspector-attribute.md)                 | Improving editor UX with attributes                    |
| [debug-il2cpp](./skills/debug-il2cpp.md)                                       | IL2CPP build issues or AOT errors                      |
| [github-actions-script-pattern](./skills/github-actions-script-pattern.md)     | Extract GHA logic to testable scripts                  |
| [github-pages](./skills/github-pages.md)                                       | GitHub Pages, Jekyll, markdown link format             |
| [integrate-odin-inspector](./skills/integrate-odin-inspector.md)               | Odin Inspector integration patterns                    |
| [integrate-optional-dependency](./skills/integrate-optional-dependency.md)     | Odin, VContainer, Zenject integration patterns         |
| [use-algorithmic-structures](./skills/use-algorithmic-structures.md)           | Connectivity, prefix search, bit manipulation, caching |
| [use-data-structures](./skills/use-data-structures.md)                         | Selecting appropriate data structures                  |
| [use-discriminated-union](./skills/use-discriminated-union.md)                 | OneOf/Result types, type-safe unions                   |
| [use-effects-system](./skills/use-effects-system.md)                           | Buffs, debuffs, stat modifications                     |
| [use-extension-methods](./skills/use-extension-methods.md)                     | Collection, string, color utilities                    |
| [use-priority-structures](./skills/use-priority-structures.md)                 | Priority ordering or task scheduling                   |
| [use-prng](./skills/use-prng.md)                                               | Implementing randomization                             |
| [use-queue-structures](./skills/use-queue-structures.md)                       | Rolling history, double-ended queues                   |
| [use-relational-attributes](./skills/use-relational-attributes.md)             | Auto-wiring components via hierarchy                   |
| [use-serializable-types](./skills/use-serializable-types.md)                   | Dictionaries, HashSets, Nullable, Type, Guid           |
| [use-serializable-types-patterns](./skills/use-serializable-types-patterns.md) | Common patterns for serializable collections           |
| [use-serialization](./skills/use-serialization.md)                             | Save files, network, persistence                       |
| [use-singleton](./skills/use-singleton.md)                                     | Global managers, service locators, configuration       |
| [use-spatial-structure](./skills/use-spatial-structure.md)                     | Spatial queries or proximity logic                     |
| [use-threading](./skills/use-threading.md)                                     | Main thread dispatch, thread safety                    |
| [wiki-generation](./skills/wiki-generation.md)                                 | GitHub Wiki deployment, sidebar links                  |

<!-- END GENERATED SKILLS INDEX -->

<!-- [skills-index] Generated skills index -->

---

## Documentation Is a Deliverable (MANDATORY)

**Documentation is NOT optional.** Every customer-visible change MUST include documentation updates. Incomplete documentation = incomplete work.

### What Requires Documentation Updates

| Change Type                     | Required Documentation                                           |
| ------------------------------- | ---------------------------------------------------------------- |
| **New feature/class/method**    | CHANGELOG, XML docs, feature docs in `docs/`, code samples       |
| **Bug fix**                     | CHANGELOG, fix any docs describing wrong behavior                |
| **API change (breaking)**       | CHANGELOG (with **Breaking:** prefix), migration notes, all docs |
| **API change (non-breaking)**   | CHANGELOG, XML docs, affected feature docs                       |
| **Behavior change**             | CHANGELOG, all docs describing the behavior, migration notes     |
| **New inspector attribute**     | CHANGELOG, attribute docs, usage examples                        |
| **Performance improvement**     | CHANGELOG, performance docs if metrics change                    |
| **Editor tool change**          | CHANGELOG, tool documentation                                    |
| **CI/CD, build scripts, tests** | NO CHANGELOG (internal), but update `.llm/` if workflow changes  |

### Documentation Locations

| Content                   | Location                                        |
| ------------------------- | ----------------------------------------------- |
| User-facing changes       | [CHANGELOG](../CHANGELOG.md)                    |
| Feature documentation     | `docs/features/<category>/`                     |
| API reference             | XML comments (`///`) on public members          |
| Package overview          | [llms.txt](../llms.txt), [README](../README.md) |
| Agent/skill documentation | `.llm/skills/`                                  |

### Enforcement

1. **Before marking any task complete**, verify documentation is updated
2. **Run `npm run lint:docs`** to validate links
3. **Check CHANGELOG** — did you add an entry for user-facing changes?
4. **Check XML docs** — do all public members have `<summary>` tags?

See [update-documentation](./skills/update-documentation.md) for detailed standards and checklists.

---

## Build & Development Commands

### Setup

```bash
npm run hooks:install      # Install git hooks
dotnet tool restore        # Restore .NET tools (CSharpier, etc.)
```

### Formatting & Linting

```bash
dotnet tool run csharpier format .   # Format C#
npm run lint:spelling                # Spell check
npm run lint:docs                    # Lint documentation links
npm run lint:markdown                # Markdownlint rules
npm run lint:yaml                    # YAML style
pwsh -NoProfile -File scripts/lint-tests.ps1   # Lint test lifecycle
pwsh -NoProfile -File scripts/lint-skill-sizes.ps1  # Skill file sizes
```

Tests require Unity 2021.3+. Ask user to run tests and provide output.

---

## Naming Conventions

| Element               | Convention  | Example                     |
| --------------------- | ----------- | --------------------------- |
| Types, public members | PascalCase  | `SerializableDictionary`    |
| Fields, locals        | camelCase   | `keyValue`, `itemCount`     |
| Interfaces            | `I` prefix  | `IResolver`, `ISpatialTree` |
| Type parameters       | `T` prefix  | `TKey`, `TValue`            |
| Events                | `On` prefix | `OnValueChanged`            |
| Constants (public)    | PascalCase  | `DefaultCapacity`           |

### File Naming

- C# files: 4 spaces indentation
- Config files (`.json`, `.yaml`, `.asmdef`): 2 spaces
- Line endings: CRLF for most files; YAML/`.github/**`/Markdown/Jekyll includes use LF
- Encoding: UTF-8 (no BOM)

---

## Critical Rules Summary

See [create-csharp-file](./skills/create-csharp-file.md) for detailed rules. Key points:

1. `using` directives INSIDE namespace
2. NO underscores in method names (including tests)
3. Explicit types over `var`
4. **NEVER use `#region` or `#endregion`** (see [no-regions](./skills/no-regions.md))
5. NEVER use nullable reference types (`string?`)
6. One file per MonoBehaviour/ScriptableObject (production AND tests)
7. NEVER use `?.`, `??`, `??=` on UnityEngine.Object types
8. Minimal comments — only explain **why**, never **what**
9. `#if` blocks INSIDE namespace; `#define` at file top
10. Generate `.meta` files after creating ANY file/folder (see [create-unity-meta](./skills/create-unity-meta.md))
11. Update documentation after ANY change (see [update-documentation](./skills/update-documentation.md))
12. Write exhaustive tests — see [create-test](./skills/create-test.md)
13. Enums: explicit values, `None`/`Unknown` = 0 with `[Obsolete]` (see [create-enum](./skills/create-enum.md))
14. Never use backtick-wrapped markdown file references; use proper links
15. Never reflect on our own code; use `internal` + `[InternalsVisibleTo]` (see [avoid-reflection](./skills/avoid-reflection.md))
16. Never use magic strings; use `nameof()` (see [avoid-magic-strings](./skills/avoid-magic-strings.md))
17. Markdown code blocks require language specifiers
18. Never use emphasis as headings; use `#` syntax

### Formatting & Validation (Run Immediately After Each Change)

Run formatters and linters **immediately after each file change**, not batched at task end:

- **C# files**: `dotnet tool run csharpier format .`
- **Non-C# files** (`.md`, `.json`, `.yaml`, `.yml`): `npx prettier --write <file>`
- **Markdown**: `npm run lint:docs` + `npm run lint:markdown`
- **YAML**: `npm run lint:yaml` (then `actionlint` for workflows)
- **Spelling**: `npm run lint:spelling` (add valid terms to `cspell.json`)
- **Tests**: `pwsh -NoProfile -File scripts/lint-tests.ps1 -FixNullChecks -Paths <changed test files>` (auto-fixes Unity null asserts; run after every test edit)
- **Skill files** (`.llm/skills/*.md`): `pwsh -NoProfile -File scripts/lint-skill-sizes.ps1` (500-line limit, no auto-fix)

See [formatting](./skills/formatting.md) and [validate-before-commit](./skills/validate-before-commit.md) for details.

### Markdown & Links

- Internal links MUST use `./` or `../` prefix (e.g., `[Guide](./docs/guide)`)
- Never use absolute GitHub Pages paths (`/unity-helpers/...`)
- Escape example links in documentation with code blocks or backticks
- Pipe characters in tables must be escaped with `\|`

### Additional Technical Rules

- Verify GitHub Actions config files exist AND are on default branch
- Never use `((var++))` in bash with `set -e`; use `var=$((var + 1))`
- Line endings must be synchronized across `.gitattributes`, `.prettierrc.json`, `.yamllint.yaml`, `.editorconfig`
- Git hook regex patterns use single backslashes, not double-escaped

---

## Unity Meta Files

Every file and folder in this Unity package MUST have a corresponding `.meta` file. Missing meta files break Unity asset references.

**Exception**: Do NOT generate `.meta` files for any folder or file inside a dot folder (folders starting with `.`). Unity ignores these folders entirely. Examples: `.llm/`, `.github/`, `.git/`, `.vscode/`.

```bash
./scripts/generate-meta.sh <path>  # Generate meta file
```

**Order**: Create parent folder → generate its meta → create file → generate its meta → format code.

See [create-unity-meta](./skills/create-unity-meta.md) for full details.

---

## cspell Dictionary Quick Reference

Add unknown words to the appropriate dictionary in `cspell.json`:

| Dictionary      | Purpose                                  | Examples                                |
| --------------- | ---------------------------------------- | --------------------------------------- |
| `unity-terms`   | Unity Engine APIs, components, lifecycle | MonoBehaviour, GetComponent, OnValidate |
| `csharp-terms`  | C# language features, .NET types         | readonly, nullable, LINQ, StringBuilder |
| `package-terms` | This package's public API and type names | WallstopStudios, IRandom, SpatialHash   |
| `tech-terms`    | General programming/tooling terms        | async, config, JSON, middleware         |

---

## Software Architecture Principles

Apply modern software engineering principles to ALL code:

- **SOLID** — Single responsibility, open/closed, Liskov, interface segregation, dependency inversion
- **DRY** — Extract common patterns; prefer `readonly struct` or static functions; zero allocation
- **Clean Architecture** — Clear boundaries (Runtime/Editor/Tests), explicit dependencies, design patterns

---

## High-Performance C# Requirements

All code must follow [high-performance-csharp](./skills/high-performance-csharp.md). Unity uses the Boehm-Demers-Weiser GC (non-generational, no compaction, stop-the-world). See [gc-architecture-unity](./skills/gc-architecture-unity.md) for details.

For forbidden patterns and alternatives, see [forbidden-patterns reference](./references/forbidden-patterns.md).

### Required Patterns

```csharp
using PooledResource<List<T>> lease = Buffers<T>.List.Get(out List<T> buffer);     // Collection pooling
using PooledArray<T> pooled = SystemArrayPool<T>.Get(count, out T[] array);  // Array pooling
[MethodImpl(MethodImplOptions.AggressiveInlining)] public int GetHashCode() => _cachedHash;
public override int GetHashCode() => Objects.HashCode(_field1, _field2);  // Deterministic hash
```

---

## Reflection & Magic Strings

Reflection and magic strings are FORBIDDEN for WallstopStudios code. See [avoid-reflection](./skills/avoid-reflection.md) and [avoid-magic-strings](./skills/avoid-magic-strings.md).

**Test Access Pattern**: Use `internal` + `[InternalsVisibleTo]` instead of reflection.

---

## Defensive Programming

All production code must follow [defensive-programming](./skills/defensive-programming.md):

- Never throw from public APIs; return `default`, empty, or `false`
- Use `TryXxx` patterns; bounds-check all indexing
- Handle all inputs gracefully (null, empty, invalid)

---

## Testing Requirements

Every production change requires exhaustive tests. See [create-test](./skills/create-test.md) for:

- Test coverage categories (normal, negative, edge, extreme, impossible, concurrent)
- Data-driven testing with `[TestCase]` / `[TestCaseSource]`
- Zero-flaky test policy

---

## Agent-Specific Rules

### Scope & Behavior

- Keep changes minimal and focused
- Strictly follow `.editorconfig` formatting rules
- Respect folder boundaries (Runtime vs Editor)
- NEVER pipe output to `/dev/null`

### Documentation Updates

All documentation must be updated after ANY feature/bug fix. See [update-documentation](./skills/update-documentation.md).

CHANGELOG is for USER-FACING changes ONLY. Internal changes (CI/CD, build scripts, dev tooling) do NOT belong.

### Shell Tool Requirements

Use high-performance tools. See [search-codebase](./skills/search-codebase.md):

| Forbidden | Use Instead          |
| --------- | -------------------- |
| `grep`    | `rg` (ripgrep)       |
| `find`    | `fd`                 |
| `cat`     | `bat --paging=never` |

### Portable Shell Scripting

For CI/CD and bash scripts, use POSIX-compliant tools. See [validate-before-commit](./skills/validate-before-commit.md#portable-shell-scripting-in-workflows-critical).

### Git Operations

NEVER use `git add` or `git commit` commands. User handles all staging/committing.

For scripts that interact with git, use retry helpers from `scripts/git-staging-helpers.sh`. See [git-safe-operations](./skills/git-safe-operations.md).

### Test Execution

Do not copy/clone this repository. Ask user to run tests and provide output.

### Paths

Never hard-code machine-specific absolute paths. Use relative paths or environment variables.

---

## Assembly Definitions

| Assembly                                     | Purpose               |
| -------------------------------------------- | --------------------- |
| `WallstopStudios.UnityHelpers`               | Runtime code          |
| `WallstopStudios.UnityHelpers.Editor`        | Editor code           |
| `WallstopStudios.UnityHelpers.Tests.Runtime` | Runtime tests         |
| `WallstopStudios.UnityHelpers.Tests.Editor`  | Editor tests          |
| `WallstopStudios.UnityHelpers.Tests.Core`    | Shared test utilities |

---

## Key Features Quick Reference

### Inspector Attributes

`[WGroup]`, `[WGroupEnd]`, `[WButton]`, `[WShowIf]`, `[WEnumToggleButtons]`, `[WInLineEditor]`, `[WValueDropDown]`, `[WSerializableCollectionFoldout]`, `[WReadOnly]`, `[WNotNull]`, `[ValidateAssignment]`, `[StringInList]`, `[IntDropDown]`, `[EnumDisplayName]`

### Relational Component Attributes

`[SiblingComponent]`, `[ParentComponent]`, `[ChildComponent]`
Options: `Optional`, `MaxDepth`, `OnlyAncestors`, `IncludeInactive`

### Serializable Data Structures

`SerializableDictionary<K,V>`, `SerializableSortedDictionary<K,V>`, `SerializableHashSet<T>`, `SerializableNullable<T>`, `SerializableType`, `WGuid`

### Spatial Data Structures

- **2D**: `QuadTree2D<T>`, `KdTree2D<T>`, `RTree2D<T>`, `SpatialHash2D<T>`
- **3D**: `OctTree3D<T>`, `KdTree3D<T>`, `RTree3D<T>`, `SpatialHash3D<T>`

### Other Data Structures

`CyclicBuffer<T>`, `Heap<T>`, `PriorityQueue<T>`, `Deque<T>`, `DisjointSet`, `Trie`, `SparseSet`, `BitSet`, `ImmutableBitSet`, `TimedCache<T>`

### Random Number Generators

`PRNG.Instance` (thread-local default), `IllusionFlow`, `PcgRandom`, `XorShiftRandom`, `XoroShiroRandom`, `SplitMix64`, `RomuDuo`, `WyRandom`, and more.

### Effects System

`AttributeEffect`, `AttributesComponent`, `TagHandler`, `EffectHandle`, `CosmeticEffectData`

### Serialization

- **JSON**: `Serializer.JsonSerialize()` / `JsonDeserialize()`
- **Protobuf**: `Serializer.ProtoSerialize()` / `ProtoDeserialize()`

### Pooling

**Namespace**: `WallstopStudios.UnityHelpers.Utils`

- `Buffers<T>.List.Get(out List<T>)` — Returns `PooledResource<List<T>>`
- `Buffers<T>.HashSet.Get(out HashSet<T>)` — Returns `PooledResource<HashSet<T>>`
- `Buffers.StringBuilder.Get(out StringBuilder)` — Returns `PooledResource<StringBuilder>`
- `WallstopArrayPool<T>` — Exact-size array pooling (fixed sizes only)
- `SystemArrayPool<T>` — Variable-size array pooling

### Singletons

`RuntimeSingleton<T>`, `ScriptableObjectSingleton<T>`, `[AutoLoadSingleton]`

### DI Integrations

- **VContainer**: `builder.RegisterRelationalComponents()`
- **Zenject**: `RelationalComponentsInstaller`
- **Reflex**: `RelationalComponentsInstaller`

### Odin Inspector Integration

All inspector attributes work seamlessly with Odin Inspector when installed (`ODIN_INSPECTOR` define symbol). See [integrate-optional-dependency](./skills/integrate-optional-dependency.md).

### Editor Tools (Tools > Wallstop Studios > Unity Helpers)

- **Sprite Tools**: Cropper, Atlas Generator, Pivot Adjuster
- **Animation Tools**: Event Editor, Creator, Copier, Sheet Animation Creator
- **Texture Tools**: Blur, Resize, Settings Applier, Fit Texture Size
- **Validation**: Prefab Checker
- **Utilities**: ScriptableObject Singleton Creator

---

## Security & Configuration

- **Do not commit**: `Library/`, `obj/`, secrets, tokens
- **Do commit**: `.meta` files for all assets
- **Target Unity version**: 2021.3+
- **Verify `.asmdef` references** when adding new namespaces

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
