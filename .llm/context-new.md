# LLM Agent Instructions

This document provides guidelines for AI assistants working with this repository. Procedural skills are in the [skills/](skills/) directory.

---

## Repository Overview

**Package**: `com.wallstop-studios.unity-helpers`
**Version**: 2.2.0
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
3. **Production-ready** — 4,000+ tests, IL2CPP/WebGL compatible, shipped in commercial games

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
  CustomEditors/           # Custom inspectors
  Tools/                   # Editor windows (Animation Creator, Texture tools, etc.)

Tests/
  Runtime/                 # PlayMode tests mirroring Runtime/ structure
  Editor/                  # EditMode tests mirroring Editor/ structure
  Core/                    # Shared test utilities

Samples~/                  # Sample projects (imported via Package Manager)
```

---

## Skills Reference

Invoke these skills for specific tasks:

| Skill                                                        | When to Use                           |
| ------------------------------------------------------------ | ------------------------------------- |
| [create-csharp-file](skills/create-csharp-file.md)           | Creating any new `.cs` file           |
| [create-unity-meta](skills/create-unity-meta.md)             | After creating any new file or folder |
| [create-test](skills/create-test.md)                         | Writing or modifying test files       |
| [create-enum](skills/create-enum.md)                         | Creating a new `enum` type            |
| [format-code](skills/format-code.md)                         | After any C# file changes             |
| [search-codebase](skills/search-codebase.md)                 | Finding code, files, or patterns      |
| [performance-audit](skills/performance-audit.md)             | Reviewing performance-sensitive code  |
| [use-array-pool](skills/use-array-pool.md)                   | Working with temporary arrays         |
| [use-pooling](skills/use-pooling.md)                         | Working with temporary collections    |
| [use-prng](skills/use-prng.md)                               | Implementing randomization            |
| [use-spatial-structure](skills/use-spatial-structure.md)     | Spatial queries or proximity logic    |
| [use-serialization](skills/use-serialization.md)             | Save files, network, persistence      |
| [use-effects-system](skills/use-effects-system.md)           | Buffs, debuffs, stat modifications    |
| [add-inspector-attribute](skills/add-inspector-attribute.md) | Improving editor UX with attributes   |
| [debug-il2cpp](skills/debug-il2cpp.md)                       | IL2CPP build issues or AOT errors     |

---

## Build & Development Commands

### Setup

```bash
npm run hooks:install      # Install git hooks
dotnet tool restore        # Restore .NET tools (CSharpier, etc.)
```

### Formatting

```bash
dotnet tool run csharpier format .   # Format C#
```

### Linting

```bash
npm run lint:docs          # Lint documentation links
```

### Testing

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
- Line endings: CRLF
- Encoding: UTF-8 (no BOM)

---

## Critical Rules Summary

See [create-csharp-file](skills/create-csharp-file.md) for detailed rules. Key points:

1. `using` directives INSIDE namespace
2. NO underscores in method names (including tests)
3. Explicit types over `var`
4. NEVER use `#region`
5. NEVER use nullable reference types (`string?`)
6. One file per MonoBehaviour/ScriptableObject
7. NEVER use `?.`, `??`, `??=` on UnityEngine.Object types
8. Minimal comments (self-documenting code)

---

## Agent-Specific Rules

### Scope & Behavior

- Keep changes minimal and focused
- Strictly follow `.editorconfig` formatting rules
- Respect folder boundaries (Runtime vs Editor)
- Update docs and tests alongside code changes
- **NEVER pipe output to `/dev/null`**

### Git Operations

**NEVER use `git add` or `git commit` commands.** User handles all staging/committing.

✅ Allowed: `git status`, `git log`, `git diff`
❌ Forbidden: `git add`, `git commit`, `git push`, `git reset`

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

- **2D**: `QuadTree2D<T>`, `KDTree2D<T>`, `RTree2D<T>`, `SpatialHash2D<T>`
- **3D**: `OctTree3D<T>`, `KDTree3D<T>`, `RTree3D<T>`, `SpatialHash3D<T>`

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

- `Buffers<T>.List` / `Buffers<T>.HashSet` — Zero-allocation collection leases
- `WallstopArrayPool<T>` — Exact-size array pooling (fixed sizes only)
- `SystemArrayPool<T>` — Variable-size array pooling

### Singletons

`RuntimeSingleton<T>`, `ScriptableObjectSingleton<T>`, `[AutoLoadSingleton]`

### DI Integrations

- **VContainer**: `builder.RegisterRelationalComponents()`
- **Zenject**: `RelationalComponentsInstaller`
- **Reflex**: `RelationalComponentsInstaller`

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
