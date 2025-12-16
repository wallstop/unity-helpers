# LLM Agent Instructions

This document provides comprehensive guidelines for AI assistants working with this repository.

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
    Attributes/            # Inspector & component attributes (WGroup, WButton, WShowIf, relational, etc.)
    DataStructure/         # Spatial trees, heaps, queues, tries, cyclic buffers
    Extension/             # Extension methods for Unity types, collections, strings, math
    Helper/                # Buffers, pooling, singletons, compression, logging
    Math/                  # Math utilities, ballistics, geometry, line helpers
    Model/                 # Serializable types (Dictionary, HashSet, Nullable, Type, Guid)
    OneOf/                 # Discriminated unions
    Random/                # 15+ PRNG implementations with IRandom interface
    Serialization/         # JSON/Protobuf serialization with Unity type converters
    Threading/             # Thread pools, main thread dispatcher, guards
  Utils/                   # Utility components (comparers, texture tools, etc.)
  Binaries/                # Binary dependencies (System.Text.Json, protobuf-net, etc.)
  Integrations/            # DI framework integrations (VContainer, Zenject, Reflex)
  Protobuf-Net/            # Protocol Buffers support
  Settings/                # Runtime settings
  Tags/                    # Effects/attribute system (AttributeEffect, TagHandler, Cosmetics)
  Visuals/                 # Visual components (EnhancedImage, LayeredImage)

Editor/                    # Editor-only tooling
  AssetProcessors/         # Asset import/export processors, change detection
  Core/                    # Core editor utilities
  CustomDrawers/           # Property drawers for all custom attributes
  CustomEditors/           # Custom inspectors (AttributeEffect, ScriptableObjects)
  Extensions/              # Editor extension methods
  Internal/                # Internal editor utilities
  Persistence/             # Editor persistence helpers
  Settings/                # Editor settings windows
  Sprites/                 # Sprite tools (cropper, atlas, pivot)
  Styles/                  # USS styles for UI Toolkit
  Tags/                    # Tag system editor tools
  Tools/                   # Editor windows (Animation Creator, Texture tools, etc.)
  Utils/                   # Editor utilities
  Visuals/                 # Visual editor components

Tests/
  Runtime/                 # PlayMode tests mirroring Runtime/ structure
  Editor/                  # EditMode tests mirroring Editor/ structure
  Core/                    # Shared test utilities and test doubles

Samples~/                  # Sample projects (imported via Package Manager)
  DI - VContainer/         # VContainer integration sample
  DI - Zenject/            # Zenject integration sample
  DI - Reflex/             # Reflex integration sample
  Relational Components - Basic/  # Basic auto-wiring sample
  Serialization - JSON/    # JSON serialization sample
  Random - PRNG/           # PRNG usage sample
  Logging - Tag Formatter/ # Logging extensions sample
  Spatial Structures - 2D and 3D/  # Spatial tree samples
  UI Toolkit - MultiFile Selector (Editor)/  # Editor UI sample
  UGUI - EnhancedImage/    # EnhancedImage component sample

Shaders/                   # Shader files
Styles/                    # Global USS styles
URP/                       # Universal Render Pipeline assets
docs/                      # Documentation
scripts/                   # Build/lint scripts
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

- **Always follow `.editorconfig`**: Adhere strictly to all rules defined in the repository's `.editorconfig` file(s)
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

1. **NO underscores in ANY method names** — This applies to ALL code (production AND test). This is a strict `.editorconfig` rule that must never be violated:
   - ✅ `GetInvocationStatusNoActiveInvocationReturnsZeroRunningCount`
   - ❌ `GetInvocationStatus_NoActiveInvocation_ReturnsZeroRunningCount`
   - ✅ `ValidateInputReturnsErrorWhenEmpty`
   - ❌ `ValidateInput_Returns_Error_When_Empty`
   - This includes test methods — always use PascalCase without underscores

2. **Avoid `var`**: Use expressive types everywhere

3. **NEVER use `#region`** — No regions anywhere in the codebase (production OR test code). This is an absolute rule with zero exceptions:
   - ❌ `#region Helper Methods`
   - ❌ `#endregion`
   - ❌ `#region Test Setup`
   - ❌ `#region Private Methods`
   - Organize code through class structure and file organization instead
   - If you see existing `#region` blocks, remove them

4. **No nullable reference types**: Don't use `string?`, `object?`, etc.

5. **One file per MonoBehaviour/ScriptableObject** — Each `MonoBehaviour` or `ScriptableObject` class MUST have its own dedicated `.cs` file (production AND test code):
   - ✅ `MyTestComponent.cs` containing only `class MyTestComponent : MonoBehaviour`
   - ✅ `TestScriptableObject.cs` containing only `class TestScriptableObject : ScriptableObject`
   - ❌ Multiple MonoBehaviours in the same file
   - ❌ Test helper MonoBehaviours defined inside test class files
   - This is a Unity requirement for proper serialization and asset creation
   - **Enforced by pre-commit hook and CI/CD analyzer** — violations will block commits and fail builds

6. **Enum design requirements** — All enums must follow these rules to distinguish between "unset/default" and "explicitly set" values:
   - **Explicit values required**: Every enum member must have an explicitly assigned integer value
   - **First member must be `None` or `Unknown` with value `0`**: This represents the uninitialized/default state
   - **Mark the zero value as `[Obsolete]`**: Use a non-erroring obsolete attribute to warn users to select a meaningful value
   - The obsolete message should guide users to choose a specific value
   - Example:

     ```csharp
     public enum ItemType
     {
         [Obsolete("Use a specific ItemType value instead of None.")]
         None = 0,
         Weapon = 1,
         Armor = 2,
         Consumable = 3,
     }
     ```

   - This pattern ensures:
     - ✅ Default `ItemType` field values are distinguishable from intentionally set values
     - ✅ Serialization/deserialization can detect missing or unset fields
     - ✅ IDE warnings appear when using the default/unset value
     - ✅ Explicit ordering prevents value changes when members are reordered

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

**CRITICAL: Use PascalCase with NO underscores** — This rule is non-negotiable and must be followed strictly:

- ✅ `AsyncTaskInvocationCompletesAndRecordsHistory`
- ❌ `AsyncTask_Invocation_Completes_And_Records_History`
- ✅ `WhenInputIsEmptyReturnsDefaultValue`
- ❌ `When_Input_Is_Empty_Returns_Default_Value`
- ✅ `ShouldThrowArgumentExceptionForNullParameter`
- ❌ `Should_Throw_ArgumentException_For_Null_Parameter`

This aligns with the repository's `.editorconfig` naming conventions and Critical Rules.

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
- **Strictly follow `.editorconfig` formatting rules** — All code must adhere to the repository's `.editorconfig`; this includes naming conventions, indentation, and all other settings
- Respect folder boundaries (Runtime vs Editor)
- Update docs and tests alongside code changes

### Command-Line Tools

The dev container includes modern, optimized CLI tools. Prefer these over traditional alternatives:

| Modern Tool           | Replaces     | Why Better                                                        |
| --------------------- | ------------ | ----------------------------------------------------------------- |
| `rg` (ripgrep)        | `grep`       | 10x faster, respects `.gitignore`, better regex                   |
| `fd`                  | `find`       | Intuitive syntax, respects `.gitignore`, faster                   |
| `bat`                 | `cat`/`less` | Syntax highlighting, line numbers, git integration                |
| `eza`                 | `ls`         | Icons, git status, tree view, better defaults                     |
| `delta`               | `diff`       | Side-by-side diffs, syntax highlighting (auto-configured for git) |
| `fzf`                 | —            | Fuzzy finder for files, history, anything                         |
| `z` (zoxide)          | `cd`         | Learns your habits, jump to frequent directories                  |
| `jq`                  | —            | JSON processor and pretty-printer                                 |
| `yq`                  | —            | YAML processor (like jq for YAML)                                 |
| `duf`                 | `df`         | Better disk usage display                                         |
| `htop`                | `top`        | Interactive process viewer                                        |
| `ncdu`                | `du`         | Interactive disk usage analyzer                                   |
| `ag` (silversearcher) | `grep`       | Fast code search (alternative to rg)                              |
| `tldr`                | `man`        | Simplified man pages with examples                                |

**Usage guidelines:**

- Always prefer `rg` for searching file contents
- Use `fd` for finding files by name (e.g., `fd "\.cs$"` instead of `find . -name "*.cs"`)
- Use `bat` when you need syntax-highlighted file viewing
- The shell aliases `grep`, `find`, `cat`, `ls`, `cd`, and `df` to their modern equivalents

### Code Formatting

**CRITICAL: Always run CSharpier after making ANY C# code changes.**

After creating or modifying `.cs` files, run:

```bash
dotnet tool run csharpier format .
```

This ensures consistent formatting across the codebase. Do not skip this step.

### Testing Requirements for Bug Fixes and New Features

**When detecting and fixing issues or implementing new features, provide exhaustive, comprehensive tests.**

Tests must cover:

1. **Positive cases**: Verify the expected behavior works correctly under normal conditions
2. **Negative cases**: Verify proper handling of invalid inputs, error conditions, and failure scenarios
3. **Edge cases**: Test boundary conditions, empty inputs, null values, extreme values, and unusual but valid inputs
4. **Normal cases**: Test typical, everyday usage scenarios with representative real-world data

Examples of edge cases to consider:

- Empty collections, strings, or arrays
- Single-element collections
- Maximum/minimum values for numeric types
- Null inputs (where applicable)
- Whitespace-only strings
- Unicode and special characters
- Concurrent access (for thread-safe code)
- Boundary conditions (first/last elements, zero, negative numbers)
- Large inputs that stress performance

Examples of normal cases to consider:

- Typical collection sizes (5-20 elements)
- Common string formats and lengths
- Representative numeric values within expected ranges
- Standard workflows and method call sequences
- Realistic combinations of parameters

**Aim for thorough coverage** — it's better to have more tests than to miss important scenarios.

### When Tests Are Too Involved

If implementing comprehensive tests would be too time-consuming or complex for the current task:

1. **Create or update `PLAN.md`** with detailed test requirements
2. Add a new section or update an existing section with:
   - **Test file location**: Where the test file should be created (following the mirror structure in `Tests/`)
   - **Test class name**: Following the `*Tests.cs` naming convention
   - **Specific test cases to implement**: List each test method name (PascalCase, no underscores) with a description
   - **Edge cases to cover**: Enumerate specific boundary conditions and unusual inputs
   - **Setup requirements**: Any test fixtures, mocks, or helper classes needed
   - **Priority**: Mark as high/medium/low priority

Example PLAN.md entry:

```markdown
## Test Backlog

### SerializableDictionary Edge Case Tests

**Priority:** High
**File:** `Tests/Runtime/Core/Model/SerializableDictionaryEdgeCaseTests.cs`

| Test Method                                                    | Description                               |
| -------------------------------------------------------------- | ----------------------------------------- |
| `AddDuplicateKeyThrowsArgumentException`                       | Verify adding existing key throws         |
| `RemoveNonExistentKeyReturnsFalse`                             | Verify removing missing key returns false |
| `EnumerationDuringModificationThrowsInvalidOperationException` | Verify concurrent modification detection  |
| `SerializeDeserializePreservesOrderForLargeCollections`        | Test with 10,000+ entries                 |

**Setup needed:** None (uses existing test infrastructure)
```

**Always prefer implementing tests directly** — only defer to PLAN.md when the test implementation would significantly delay the primary task or requires substantial additional research.

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

| Assembly                                     | Purpose               |
| -------------------------------------------- | --------------------- |
| `WallstopStudios.UnityHelpers`               | Runtime code          |
| `WallstopStudios.UnityHelpers.Editor`        | Editor code           |
| `WallstopStudios.UnityHelpers.Tests.Runtime` | Runtime tests         |
| `WallstopStudios.UnityHelpers.Tests.Editor`  | Editor tests          |
| `WallstopStudios.UnityHelpers.Tests.Core`    | Shared test utilities |

---

## Key Features Reference

### Inspector Attributes

- **`[WGroup]`** / **`[WGroupEnd]`** — Boxed sections with collapsible headers, color themes, animations
- **`[WButton]`** — Method buttons with async support, cancellation, history
- **`[WShowIf]`** — Conditional visibility (9 comparison operators)
- **`[WEnumToggleButtons]`** — Flag enums as visual toggle grids
- **`[WInLineEditor]`** — Inline nested editor for object references
- **`[WValueDropDown]`** — Value dropdown selection
- **`[WSerializableCollectionFoldout]`** — Foldout for serializable collections
- **`[WReadOnly]`** — Read-only inspector display
- **`[WNotNull]`** — Validation for required fields (shows error in inspector)
- **`[ValidateAssignment]`** — Runtime assignment validation with logging
- **`[StringInList]`** — Dropdown selection from string lists or methods
- **`[IntDropDown]`** — Integer dropdown selection
- **`[EnumDisplayName]`** — Custom display names for enum values

### Relational Component Attributes

- **`[SiblingComponent]`** — Find components on same GameObject
- **`[ParentComponent]`** — Find components in parent hierarchy
- **`[ChildComponent]`** — Find components in children
- Options: `Optional`, `MaxDepth`, `OnlyAncestors`, `IncludeInactive`

### Serializable Data Structures

- **`SerializableDictionary<TKey, TValue>`** — Dictionary with inspector support
- **`SerializableSortedDictionary<TKey, TValue>`** — Ordered dictionary
- **`SerializableHashSet<T>`** — HashSet with duplicate detection
- **`SerializableNullable<T>`** — Nullable value types in inspector
- **`SerializableType`** — Type references in inspector
- **`WGuid`** — Serializable GUID

### Spatial Data Structures

- **2D**: `QuadTree2D<T>`, `KDTree2D<T>`, `RTree2D<T>`, `SpatialHash2D<T>`
- **3D**: `OctTree3D<T>`, `KDTree3D<T>`, `RTree3D<T>`, `SpatialHash3D<T>`
- Common API: `GetElementsInRange()`, `GetElementsInBounds()`, `GetApproximateNearestNeighbors()`

### Other Data Structures

- **`CyclicBuffer<T>`** — Ring buffer
- **`Heap<T>`** / **`PriorityQueue<T>`** — Min/max heap
- **`Deque<T>`** — Double-ended queue
- **`DisjointSet`** — Union-find
- **`Trie`** — String prefix tree
- **`SparseSet`** — Fast add/remove with iteration
- **`BitSet`** / **`ImmutableBitSet`** — Compact boolean storage
- **`TimedCache<T>`** — Auto-expiring cache

### Random Number Generators

- **`PRNG.Instance`** — Thread-local default (IllusionFlow)
- **15+ implementations**: `IllusionFlow`, `PcgRandom`, `XorShiftRandom`, `XoroShiroRandom`, `SplitMix64`, `RomuDuo`, `FlurryBurstRandom`, `PhotonSpinRandom`, `WyRandom`, `SquirrelRandom`, `StormDropRandom`, `WaveSplatRandom`, `BlastCircuitRandom`, `LinearCongruentialGenerator`, `DotNetRandom`, `SystemRandom`, `UnityRandom`
- Rich `IRandom` interface: `NextFloat()`, `NextVector2/3()`, `NextGaussian()`, `NextWeightedIndex()`, `Shuffle()`, `NextGuid()`, `NextEnum<T>()`

### Effects System

- **`AttributeEffect`** — ScriptableObject defining stat modifications, tags, cosmetics, duration
- **`AttributesComponent`** — Base class exposing modifiable `Attribute` fields
- **`TagHandler`** — Reference-counted tag queries
- **`EffectHandle`** — Unique ID for tracking/removing specific effect instances
- **`CosmeticEffectData`** — VFX/SFX prefab data for effects

### Serialization

- **JSON**: `Serializer.JsonSerialize()` / `JsonDeserialize()` — System.Text.Json with Unity converters
- **Protobuf**: `Serializer.ProtoSerialize()` / `ProtoDeserialize()` — protobuf-net for compact binary
- Supported Unity types: Vector2/3/4, Color, Quaternion, Rect, Bounds, GameObject references

### Pooling & Buffering

- **`Buffers<T>.List`** / **`Buffers<T>.HashSet`** — Zero-allocation collection leases
- **`WallstopArrayPool<T>`** — Array pooling
- Pattern: `using var lease = Buffers<T>.List.Get(out List<T> buffer);`

### Singletons

- **`RuntimeSingleton<T>`** — Component singleton with cross-scene persistence
- **`ScriptableObjectSingleton<T>`** — Settings singleton from Resources
- **`[AutoLoadSingleton]`** — Automatic instantiation during Unity start-up

### DI Integrations

- **VContainer**: `builder.RegisterRelationalComponents()`
- **Zenject**: `RelationalComponentsInstaller` in SceneContext
- **Reflex**: `RelationalComponentsInstaller` in SceneScope
- Helper methods: `InjectWithRelations()`, `InstantiateComponentWithRelations()`, `InjectGameObjectWithRelations()`

### Editor Tools (Tools > Wallstop Studios > Unity Helpers)

- **Sprite Tools**: Cropper, Atlas Generator, Pivot Adjuster
- **Animation Tools**: Event Editor, Creator, Copier, Sheet Animation Creator
- **Texture Tools**: Blur, Resize, Settings Applier, Fit Texture Size
- **Validation**: Prefab Checker
- **Utilities**: ScriptableObject Singleton Creator, Request Script Compilation (Ctrl/Cmd+Alt+R)
