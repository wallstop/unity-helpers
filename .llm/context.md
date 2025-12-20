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
- **No nullable reference types**: Do not use `?` nullable annotations (see Unity/Mono/IL2CPP Compatibility below)
- **No regions**: Never use `#region` anywhere
- **Unity/Mono/IL2CPP compatible**: All code must work with Unity's runtime constraints (see dedicated section below)

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

1. **`using` directives MUST be inside the namespace** — All `using` directives must be placed INSIDE the namespace declaration, NEVER at the top of the file outside the namespace. This is a strict `.editorconfig` rule:
   - ✅ Correct:

     ```csharp
     namespace WallstopStudios.UnityHelpers.Core
     {
         using System;
         using System.Collections.Generic;
         using UnityEngine;

         public sealed class MyClass
         {
             // ...
         }
     }
     ```

   - ❌ Incorrect:

     ```csharp
     using System;
     using System.Collections.Generic;
     using UnityEngine;

     namespace WallstopStudios.UnityHelpers.Core
     {
         public sealed class MyClass
         {
             // ...
         }
     }
     ```

   - This applies to ALL C# files (production AND test code)
   - The only exception is `global using` directives if ever used (but prefer avoiding them)

2. **NO underscores in ANY method names** — This applies to ALL code (production AND test). This is a strict `.editorconfig` rule that must never be violated:
   - ✅ `GetInvocationStatusNoActiveInvocationReturnsZeroRunningCount`
   - ❌ `GetInvocationStatus_NoActiveInvocation_ReturnsZeroRunningCount`
   - ✅ `ValidateInputReturnsErrorWhenEmpty`
   - ❌ `ValidateInput_Returns_Error_When_Empty`
   - This includes test methods — always use PascalCase without underscores

3. **Avoid `var`**: Use expressive types everywhere

4. **NEVER use `#region`** — No regions anywhere in the codebase (production OR test code). This is an absolute rule with zero exceptions:
   - ❌ `#region Helper Methods`
   - ❌ `#endregion`
   - ❌ `#region Test Setup`
   - ❌ `#region Private Methods`
   - Organize code through class structure and file organization instead
   - If you see existing `#region` blocks, remove them

5. **NEVER use nullable reference types** — This is a strict, non-negotiable rule. Nullable reference types (NRTs) are NOT compatible with Unity/Mono/IL2CPP:
   - ❌ `string?` — Never use nullable string
   - ❌ `object?` — Never use nullable object
   - ❌ `List<string>?` — Never use nullable collections
   - ❌ `MyClass?` — Never use nullable class types
   - ❌ `T?` where T is a reference type — Never use nullable generic reference types
   - ❌ `#nullable enable` — Never enable nullable context
   - ❌ Null-forgiving operator `!` (e.g., `value!`) — Never use this operator
   - ✅ `string` — Use non-nullable reference types and handle null with runtime checks
   - ✅ `int?`, `float?`, `bool?` — Nullable VALUE types are OK (these are `Nullable<T>` structs)
   - **Why**: Unity uses Mono/IL2CPP which targets older .NET Standard. NRTs cause compilation failures, IL2CPP build errors, and runtime issues on platforms like WebGL, iOS, and Android.

6. **One file per MonoBehaviour/ScriptableObject** — Each `MonoBehaviour` or `ScriptableObject` class MUST have its own dedicated `.cs` file (production AND test code):
   - ✅ `MyTestComponent.cs` containing only `class MyTestComponent : MonoBehaviour`
   - ✅ `TestScriptableObject.cs` containing only `class TestScriptableObject : ScriptableObject`
   - ❌ Multiple MonoBehaviours in the same file
   - ❌ Test helper MonoBehaviours defined inside test class files
   - This is a Unity requirement for proper serialization and asset creation
   - **Enforced by pre-commit hook and CI/CD analyzer** — violations will block commits and fail builds

7. **Enum design requirements** — All enums must follow these rules to distinguish between "unset/default" and "explicitly set" values:
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

8. **NEVER use `?.`, `??`, `??=`, or `ReferenceEquals` on UnityEngine.Object types** — Unity overrides `==` and `!=` operators for lifecycle-aware null checks. The standard C# null operators bypass this override and produce incorrect results:
   - ❌ `gameObject?.SetActive(true)` — Bypasses Unity's null check
   - ❌ `component ?? fallback` — Bypasses Unity's null check
   - ❌ `_cached ??= GetComponent<T>()` — Bypasses Unity's null check
   - ❌ `ReferenceEquals(gameObject, null)` — Bypasses Unity's null check
   - ❌ `object.ReferenceEquals(transform, null)` — Bypasses Unity's null check
   - ✅ `if (gameObject != null) gameObject.SetActive(true)` — Uses Unity's overridden operator
   - ✅ `component != null ? component : fallback` — Uses Unity's overridden operator
   - ✅ `if (_cached == null) _cached = GetComponent<T>()` — Uses Unity's overridden operator
   - **Why**: `UnityEngine.Object` overrides `==` and `!=` to return `true` for destroyed objects (fake null). The `?.`, `??`, and `ReferenceEquals` operators check reference equality only, so destroyed objects appear non-null and cause `MissingReferenceException` at runtime.
   - **This applies to ALL UnityEngine.Object-derived types**: `GameObject`, `Component`, `MonoBehaviour`, `ScriptableObject`, `Transform`, `Rigidbody`, `Collider`, `Renderer`, `Material`, `Texture`, `AudioClip`, `AnimationClip`, `Sprite`, etc.
   - **Enforced in both production AND test code** — no exceptions

9. **Use Unity Helpers collection/dictionary extensions** — Prefer the built-in extension methods from `WallstopStudios.UnityHelpers.Core.Extension` to simplify code and reduce boilerplate:
   - **Dictionary extensions** (`DictionaryExtensions`):
     - ✅ `dictionary.GetOrAdd(key)` — Gets or creates a new instance (requires `where V : new()`)
     - ✅ `dictionary.GetOrAdd(key, () => new Value())` — Gets or creates with factory
     - ✅ `dictionary.GetOrAdd(key, k => CreateFrom(k))` — Gets or creates with key-based factory
     - ✅ `dictionary.GetOrElse(key, defaultValue)` — Gets or returns default (read-only)
     - ✅ `dictionary.AddOrUpdate(key, creator, updater)` — Atomic add or update
     - ✅ `dictionary.TryRemove(key, out value)` — Remove with out parameter
     - ✅ `lhs.Merge(rhs)` — Combine two dictionaries
     - ✅ `lhs.Difference(rhs)` — Find changed entries
     - ✅ `dictionary.Reversed()` — Swap keys and values
   - **List extensions** (`IListExtensions`):
     - ✅ `list.Shuffle()` / `list.Shuffle(random)` — Fisher-Yates shuffle
     - ✅ `list.Sort(SortAlgorithm.Tim)` — 17 sorting algorithms available
     - ✅ `list.BinarySearch()` variants — Efficient searching
   - **IEnumerable extensions** (`IEnumerableExtensions`):
     - ✅ `enumerable.WhereNotNull()` — Filter null elements
     - ✅ `enumerable.ToHashSet()` — Convert to HashSet
   - **Examples**:

     ```csharp
     // ❌ Verbose pattern
     if (!dictionary.TryGetValue(key, out List<Item> items))
     {
         items = new List<Item>();
         dictionary[key] = items;
     }
     items.Add(newItem);

     // ✅ Simplified with GetOrAdd
     dictionary.GetOrAdd(key).Add(newItem);

     // ✅ With factory for complex initialization
     dictionary.GetOrAdd(key, () => new List<Item>(capacity)).Add(newItem);
     ```

   - These extensions are optimized for `ConcurrentDictionary` when applicable
   - Located in `Runtime/Core/Extension/` — ensure proper `using` directive

10. **Minimize allocations and maximize performance** — All production and editor code should aim for minimal GC allocations and high performance. This is critical for both runtime code (frame rate, GC spikes) and editor tooling (responsive UI, large project scalability):

- **Avoid LINQ in hot paths**: LINQ methods allocate iterators and delegates
  - ❌ `items.Where(x => x.IsValid).ToList()` — Allocates iterator, delegate, and list
  - ❌ `items.Any(x => x.Id == targetId)` — Allocates delegate
  - ❌ `items.Select(x => x.Name).FirstOrDefault()` — Allocates iterator and delegate
  - ✅ Use `for`/`foreach` loops with explicit logic instead
  - ✅ Use `Buffers<T>.List` for temporary collections (zero-allocation pooling)
- **Avoid closures that capture variables**: Closures allocate heap objects
  - ❌ `list.Find(item => item.Id == searchId)` — Captures `searchId`, allocates closure
  - ❌ `items.RemoveAll(x => x.Owner == this)` — Captures `this`, allocates closure
  - ✅ Use explicit loops or pass state via parameters/fields
- **Prefer structs over classes for small, short-lived data**:
  - ✅ Use `struct` for data containers under ~16 bytes that don't need inheritance
  - ✅ Use `readonly struct` for immutable value types
  - ✅ Use `ref struct` for stack-only types when appropriate
  - ❌ Avoid boxing structs (passing to `object`, non-generic collections)
- **Pool and reuse collections**:
  - ✅ `using var lease = Buffers<T>.List.Get(out List<T> buffer);` — Returns to pool automatically
  - ✅ `using var lease = Buffers<T>.HashSet.Get(out HashSet<T> buffer);`
  - ✅ Clear and reuse collections instead of allocating new ones
  - ❌ `new List<T>()` in frequently-called methods
- **Choose the right array pool**:

  > **⚠️ CRITICAL: Array Pool Selection Directly Impacts Memory Usage**
  >
  > Using the wrong array pool for your use case **will cause memory leaks**. Read this section carefully before choosing a pool.
  - **`WallstopArrayPool<T>`** / **`WallstopFastArrayPool<T>`** — Use **ONLY** for **constant or tightly bounded sizes**. Returns arrays of EXACT requested size. Pools arrays by exact size in a dictionary/list keyed by size.

    **⚠️ MEMORY LEAK WARNING**: These pools create a separate pool bucket for EVERY unique size requested. If you pass variable sizes (user input, collection.Count, dynamic values), each unique size creates a new bucket that persists forever, causing unbounded memory growth.
    - ✅ **SAFE**: Compile-time constants (`Get(16)`, `Get(64)`, `Get(256)`)
    - ✅ **SAFE**: Algorithm-bounded sizes with small fixed upper limits (bucket counts capped at 32)
    - ✅ **SAFE**: PRNG internal state buffers (fixed sizes like 16, 32, 64 bytes)
    - ✅ **SAFE**: Sizes from a small, known set of values (e.g., enum-based sizes)
    - ❌ **MEMORY LEAK**: `Get(userInput)` — Every unique user value creates a permanent bucket
    - ❌ **MEMORY LEAK**: `Get(collection.Count)` — Every unique collection size leaks memory
    - ❌ **MEMORY LEAK**: `Get(random.Next(1, 1000))` — Creates up to 1000 permanent buckets
    - ❌ **MEMORY LEAK**: `Get(dynamicCalculation)` — Unbounded sizes = unbounded memory

    **Rule of thumb**: If you cannot enumerate ALL possible sizes at compile time, use `SystemArrayPool<T>` instead.

    **`WallstopFastArrayPool<T>`** is identical but does NOT clear arrays on return. Use for `unmanaged` types where you'll overwrite all values before reading.

  - **`SystemArrayPool<T>`** — Use for **variable or unpredictable sizes**. Returns arrays of AT LEAST requested size (may be larger due to power-of-2 bucketing). Wraps .NET's `ArrayPool<T>.Shared` which efficiently handles varied sizes.
    - ✅ Sorting algorithm buffers (scale with input: `count / 2`, `count`)
    - ✅ Collection copies of unknown size
    - ✅ Any size derived from user input or external data
    - ✅ Sizes computed at runtime based on data
    - ✅ Large arrays (1KB+) where exact sizing doesn't matter

  - **CRITICAL for `SystemArrayPool`**: The returned array may be LARGER than requested. Always use `pooledArray.Length` (the originally requested size), NOT `array.Length`:

    ```csharp
    // ✅ Correct - use pooledArray.Length
    using PooledArray<int> pooled = SystemArrayPool<int>.Get(count, out int[] buffer);
    for (int i = 0; i < pooled.Length; i++)  // Use pooled.Length, not buffer.Length
    {
        buffer[i] = ProcessItem(i);
    }

    // ❌ Wrong - buffer.Length may be larger than requested
    for (int i = 0; i < buffer.Length; i++)  // May iterate past valid data!
    {
        ...
    }
    ```

  - **Decision flowchart**:

    ```text
    Is the size a compile-time constant or from a small fixed set?
    ├─ YES → Use WallstopArrayPool<T> (or WallstopFastArrayPool<T> for unmanaged types)
    └─ NO  → Is the size derived from user input, collection sizes, or runtime calculations?
             ├─ YES → Use SystemArrayPool<T>
             └─ NO  → When in doubt, use SystemArrayPool<T> (safer default)
    ```

- **Use stack allocation where appropriate**:
  - ✅ `stackalloc` for small fixed-size arrays in performance-critical code
  - ✅ Value tuples `(int x, int y)` instead of `Tuple<int, int>`
- **String operations**:
  - ❌ String concatenation in loops (`str += value`)
  - ❌ `string.Format()` or interpolation in hot paths
  - ✅ `StringBuilder` for building strings (pooled if possible)
  - ✅ Cache formatted strings when values don't change
- **Editor code is NOT exempt**: Editor tools must also be performant, especially for:
  - Inspector drawing (called every frame when visible)
  - Asset processing (may handle thousands of assets)
  - Scene view rendering callbacks
- **Example transformation**:

  ```csharp
  // ❌ Allocates: iterator, delegate, closure, list
  List<Enemy> activeEnemies = enemies.Where(e => e.Health > 0 && e.Distance < range).ToList();

  // ✅ Zero allocations with pooled buffer
  using var lease = Buffers<Enemy>.List.Get(out List<Enemy> activeEnemies);
  for (int i = 0; i < enemies.Count; i++)
  {
      Enemy enemy = enemies[i];
      if (enemy.Health > 0 && enemy.Distance < range)
      {
          activeEnemies.Add(enemy);
      }
  }
  ```

---

## Unity/Mono/IL2CPP Compatibility

**CRITICAL**: All code in this repository MUST be compatible with Unity's Mono and IL2CPP scripting backends. This package targets Unity 2021.3+ LTS and must work across all platforms including WebGL, iOS, Android, and consoles.
**CRITICAL**: Ensure all `Object` access is specified via something like `using Object = UnityEngine.Object;`, or fully qualifying the `Object` reference, correctly.

### Forbidden C# Features

The following modern C# features are NOT compatible with Unity/Mono/IL2CPP and must NEVER be used:

| Feature                           | Example                          | Why Forbidden                               |
| --------------------------------- | -------------------------------- | ------------------------------------------- |
| Nullable reference types          | `string?`, `object?`, `T?`       | IL2CPP compilation failures, runtime issues |
| Null-forgiving operator           | `value!`                         | Requires nullable context                   |
| `#nullable` directives            | `#nullable enable`               | Not supported by Unity's compiler           |
| `required` modifier               | `required string Name`           | C# 11 feature, not available                |
| `init` accessors                  | `{ get; init; }`                 | C# 9 feature, limited support               |
| File-scoped types                 | `file class Helper`              | C# 11 feature, not available                |
| Raw string literals               | `"""text"""`                     | C# 11 feature, not available                |
| Generic attributes                | `[Attr<T>]`                      | C# 11 feature, not available                |
| Static abstract interface members | `static abstract void Method();` | Limited IL2CPP support                      |
| `Span<T>` in some contexts        | Stack-allocated spans            | Limited support, avoid in hot paths         |

### Safe C# Features

These features ARE safe to use with Unity 2021.3+:

- Pattern matching (`is`, `switch` expressions)
- Null-coalescing operators (`??`, `??=`) — **ONLY for non-UnityEngine.Object types** (see Critical Rule #7)
- Null-conditional operators (`?.`, `?[]`) — **ONLY for non-UnityEngine.Object types** (see Critical Rule #7)
- Nullable VALUE types (`int?`, `float?`, `bool?`) — These are `Nullable<T>` structs, not NRTs
- Expression-bodied members (`=>`)
- Local functions
- Tuples and deconstruction
- `nameof()` operator
- String interpolation (`$"{value}"`)
- `async`/`await` (with Unity's limitations)
- Default interface implementations (Unity 2021.2+)
- Records (Unity 2021.2+, but prefer classes for serialization)

### IL2CPP-Specific Considerations

1. **Avoid heavy reflection**: IL2CPP strips unused code; reflection targets may be removed
2. **Use `[Preserve]` attribute**: Mark types/members accessed only via reflection
3. **Avoid `System.Reflection.Emit`**: Not supported on IL2CPP
4. **Generic virtual methods**: Can cause issues; prefer non-generic alternatives
5. **Test on IL2CPP**: Some code works in Editor (Mono) but fails in IL2CPP builds

### Platform-Specific Notes

- **WebGL**: No threading support, no `System.IO.File` operations
- **iOS**: AOT compilation means no runtime code generation
- **Android**: 64-bit requirements, potential JNI limitations

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
6. **Unity Object null checks**: For any `UnityEngine.Object`-derived type (`GameObject`, `Component`, `MonoBehaviour`, `ScriptableObject`, etc.):
   - ✅ Use `Assert.IsTrue(thing != null)` or `Assert.IsFalse(thing == null)` — Uses Unity's overridden `==`
   - ✅ Use `thing != null` / `thing == null` directly in test logic
   - ❌ **NEVER use `Assert.IsNull(unityObject)`** — Bypasses Unity's null check, fails on destroyed objects
   - ❌ **NEVER use `Assert.IsNotNull(unityObject)`** — Bypasses Unity's null check, passes on destroyed objects
   - ❌ **NEVER use `Assert.That(unityObject, Is.Null)`** — Same problem
   - ❌ **NEVER use `Assert.That(unityObject, Is.Not.Null)`** — Same problem
   - ❌ **NEVER use `?.`, `??`, `??=` on Unity objects** — See Critical Rule #7
   - ❌ **NEVER use `ReferenceEquals(unityObject, null)`** — Bypasses Unity's null check
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
- **NEVER pipe output to `/dev/null`** — Do not use `>/dev/null`, `2>/dev/null`, `&>/dev/null`, or any similar redirection to discard output. Command output (including errors) should be visible for debugging and transparency. If a command produces unwanted output, find a proper flag to suppress it or accept the output. The only exception is when a tool absolutely requires it and there is no alternative.

### Command-Line Tools

The dev container includes modern, high-performance CLI tools. **Always use these tools instead of their traditional counterparts** — they are faster, more intuitive, and provide better output.

#### Tool Reference

| Modern Tool           | Replaces     | Why Better                                                                           |
| --------------------- | ------------ | ------------------------------------------------------------------------------------ |
| `rg` (ripgrep)        | `grep`       | 10-100x faster, respects `.gitignore`, better regex, colored output                  |
| `fd`                  | `find`       | 5x faster, intuitive syntax, respects `.gitignore`, colored output                   |
| `bat`                 | `cat`/`less` | Syntax highlighting, line numbers, git integration (use `--paging=never` in scripts) |
| `eza`                 | `ls`         | Icons, git status, tree view, better defaults                                        |
| `delta`               | `diff`       | Side-by-side diffs, syntax highlighting (auto-configured for git)                    |
| `sd`                  | `sed`        | Intuitive syntax, regex support, no escaping headaches                               |
| `dust`                | `du`         | Visual directory size with percentages, sorted output                                |
| `procs`               | `ps`         | Colored output, tree view, searchable, better defaults                               |
| `tokei`               | `cloc`       | Fast code statistics by language, accurate line counts                               |
| `fzf`                 | —            | Fuzzy finder for files, history, anything                                            |
| `z` (zoxide)          | `cd`         | Learns your habits, jump to frequent directories                                     |
| `jq`                  | —            | JSON processor and pretty-printer                                                    |
| `yq`                  | —            | YAML processor (like jq for YAML)                                                    |
| `duf`                 | `df`         | Better disk usage display                                                            |
| `htop`                | `top`        | Interactive process viewer                                                           |
| `ncdu`                | `du`         | Interactive disk usage analyzer                                                      |
| `ag` (silversearcher) | `grep`       | Fast code search (alternative to rg)                                                 |
| `tldr`                | `man`        | Simplified man pages with examples                                                   |

#### Shell Aliases (Pre-configured)

The following aliases are configured in the dev container, so traditional commands automatically use modern tools:

```bash
grep → rg        # ripgrep
find → fd        # fd-find
cat  → bat       # bat with syntax highlighting (use --paging=never)
ls   → eza       # eza with icons
cd   → z         # zoxide smart navigation
df   → duf       # disk usage
sed  → sd        # intuitive find-and-replace
du   → dust      # visual disk usage
ps   → procs     # modern process viewer
```

#### Mandatory Tool Usage Rules

**ALWAYS use `rg` (ripgrep) instead of `grep`:**

```bash
# ✅ CORRECT - Use ripgrep
rg "function_name" --type cs                    # Search C# files
rg "TODO|FIXME" -g "*.cs"                       # Search with glob pattern
rg "pattern" -C 3                               # Show 3 lines of context
rg "class \w+" --pcre2                          # Use PCRE2 regex
rg "error" -i                                   # Case-insensitive search
rg "pattern" -l                                 # List matching files only
rg "pattern" --no-ignore                        # Include gitignored files
rg "pattern" -A 5 -B 2                          # 5 lines after, 2 before

# ❌ NEVER use grep
grep -r "pattern" .                             # Slow, no syntax highlighting
grep -rn "pattern" --include="*.cs" .           # Verbose, slower
```

**ALWAYS use `fd` instead of `find`:**

```bash
# ✅ CORRECT - Use fd
fd "\.cs$"                                      # Find all C# files
fd "Tests" --type d                             # Find directories named Tests
fd "\.meta$" Runtime/                           # Find .meta files in Runtime/
fd -e cs -e json                                # Find files with extensions
fd "pattern" --hidden                           # Include hidden files
fd "pattern" --no-ignore                        # Include gitignored files
fd "pattern" -x echo {}                         # Execute command on results
fd "^test" --type f -X bat --paging=never       # Open all matching files in bat

# ❌ NEVER use find
find . -name "*.cs"                             # Slow, verbose syntax
find . -type d -name "Tests"                    # More typing, slower
```

**ALWAYS use `bat` instead of `cat` (with `--paging=never`):**

```bash
# ✅ CORRECT - Use bat with --paging=never
bat --paging=never file.cs                      # View with syntax highlighting
bat --paging=never -n file.cs                   # Show line numbers only
bat --paging=never -r 10:20 file.cs             # Show lines 10-20
bat --paging=never -p file.cs                   # Plain output (no decorations)
bat --paging=never -l cs file.txt               # Force C# syntax highlighting
bat --paging=never --style=plain file.cs        # No line numbers or decorations

# ✅ CORRECT - Combining with other tools
head -n 50 file.cs | bat --paging=never -l cs   # First 50 lines with highlighting
tail -n 50 file.cs | bat --paging=never -l cs   # Last 50 lines with highlighting
rg "pattern" -C 3 | bat --paging=never -l cs    # Search results with highlighting

# ❌ NEVER use bare bat without --paging=never - it will block
bat file.cs                                     # BLOCKS waiting for pager input!
bat -n file.cs                                  # BLOCKS!
bat -r 10:20 file.cs                            # BLOCKS!

# ❌ NEVER use cat - no syntax highlighting
cat file.cs                                     # No highlighting, harder to read
cat -n file.cs                                  # No highlighting
```

**ALWAYS use `eza` instead of `ls`:**

```bash
# ✅ CORRECT - Use eza
eza -la                                         # List all with details
eza --tree                                      # Tree view
eza --tree --level=2                            # Tree with depth limit
eza -la --git                                   # Show git status
eza --icons                                     # Show file type icons

# ❌ NEVER use ls
ls -la                                          # No icons, no git status
```

**ALWAYS use `duf` instead of `df`:**

```bash
# ✅ CORRECT - Use duf
duf                                             # Show disk usage beautifully

# ❌ NEVER use df
df -h                                           # Hard to read output
```

**ALWAYS use `sd` instead of `sed` for find-and-replace:**

```bash
# ✅ CORRECT - Use sd
sd 'oldPattern' 'newPattern' file.cs            # Simple replacement
sd 'foo(\d+)' 'bar$1' file.cs                   # Regex with capture groups
sd -F 'literal.string' 'replacement' file.cs    # Fixed string (no regex)
echo "hello world" | sd 'world' 'universe'      # Pipe support
fd -e cs | xargs sd 'OldClass' 'NewClass'       # Bulk replace in files

# ❌ NEVER use sed
sed -i 's/old/new/g' file.cs                    # Escape nightmare
sed -E 's/foo([0-9]+)/bar\1/g' file.cs          # Confusing syntax
```

**ALWAYS use `dust` instead of `du` for disk usage:**

```bash
# ✅ CORRECT - Use dust
dust                                            # Visual size breakdown of current dir
dust -r                                         # Reverse order (largest last)
dust -d 2                                       # Limit depth to 2 levels
dust Runtime/                                   # Analyze specific directory
dust -n 20                                      # Show top 20 entries

# ❌ NEVER use du
du -sh *                                        # No visual breakdown, unsorted
du -h --max-depth=2                             # Harder to read output
```

**ALWAYS use `procs` instead of `ps` for process viewing:**

```bash
# ✅ CORRECT - Use procs
procs                                           # List all processes (colored, sorted)
procs --tree                                    # Show process tree
procs dotnet                                    # Filter by name
procs --sortd cpu                               # Sort by CPU descending
procs --watch                                   # Watch mode (auto-refresh)

# ❌ NEVER use ps
ps aux                                          # Hard to read, no colors
ps aux | grep dotnet                            # Awkward filtering
```

**Use `tokei` for code statistics:**

```bash
# ✅ CORRECT - Use tokei
tokei                                           # Statistics for current project
tokei Runtime/                                  # Statistics for specific directory
tokei -e Tests                                  # Exclude directory
tokei -t "C#"                                   # Only count C# files
tokei --sort code                               # Sort by lines of code
```

#### Common Workflow Patterns

**Finding and searching code:**

```bash
# Find all C# files containing a pattern
rg "SerializableDictionary" --type cs

# Find test files
fd "Tests\.cs$"

# Search in specific directory
rg "IRandom" Runtime/Core/Random/

# Find files modified recently and search them
fd -e cs --changed-within 1d -x rg "pattern" {}

# Count occurrences
rg "TODO" --type cs -c

# Search and replace preview (find what would change)
rg "OldName" --type cs -l | xargs -I {} bat {} -r 1:5
```

**Viewing and comparing files:**

```bash
# View file with context
bat --paging=never -r 50:100 Runtime/Core/Helper/Buffers.cs

# Compare files
delta file1.cs file2.cs

# View git diff with syntax highlighting (automatic via delta)
git diff

# Pretty-print JSON
jq '.' package.json

# Pretty-print YAML
yq '.' some-file.yaml
```

**Interactive navigation:**

```bash
# Fuzzy find and open file
fd -e cs | fzf | xargs bat

# Search history interactively
history | fzf

# Jump to frequently used directory
z Runtime                                       # Jump to most-used Runtime path
zi                                              # Interactive directory selection
```

#### Performance Comparison

| Task                         | Traditional    | Modern Tool | Speedup              |
| ---------------------------- | -------------- | ----------- | -------------------- |
| Search 10k files for pattern | `grep -r`      | `rg`        | 10-100x              |
| Find files by extension      | `find -name`   | `fd -e`     | 5-10x                |
| List directory with details  | `ls -la`       | `eza -la`   | Similar + features   |
| View file with highlighting  | `cat` + manual | `bat`       | Instant highlighting |

**Bottom line:** Modern tools are not just faster — they have better defaults, respect `.gitignore` automatically, provide colored output, and have more intuitive syntax. There is no reason to use the traditional tools in this dev container.

#### Using Modern Tools with xargs and Shell Subcommands

**CRITICAL**: When using `xargs`, `sh -c`, or any shell subcommand, you MUST still use modern tools. The shell aliases are NOT available inside subshells spawned by `xargs -I {} sh -c '...'` or similar constructs.

**Always use the actual tool names (`rg`, `fd`, `bat`, etc.) in xargs commands:**

```bash
# ✅ CORRECT - Use modern tools explicitly in xargs
fd -e cs | xargs -I {} sh -c 'rg "pattern" "{}" && echo "Found in: {}"'
fd -e cs | xargs -I {} sh -c 'rg -q "^namespace" "{}" || echo "No namespace: {}"'
fd -e cs -x rg "using" {}                       # fd's native -x flag (preferred)
fd -e cs -X bat --paging=never                  # fd's -X for batch execution

# ❌ NEVER use traditional tools in xargs (aliases don't work in subshells)
fd -e cs | xargs -I {} sh -c 'grep "pattern" "{}"'     # grep is slow
find . -name "*.cs" | xargs grep "pattern"             # Both find AND grep are wrong
fd -e cs | xargs -I {} sh -c 'cat "{}"'                # cat has no highlighting, use bat --paging=never
```

**Prefer `fd`'s native execution over xargs when possible:**

```bash
# ✅ BEST - Use fd's -x (per-file) or -X (batch) flags
fd -e cs -x rg "TODO" {}                        # Run rg on each file
fd -e cs -X rg "TODO"                           # Run rg once with all files as args
fd -e cs -x bat --paging=never -r 1:10 {}       # Show first 10 lines of each file

# ✅ GOOD - Use xargs with modern tools when fd flags aren't enough
fd -e cs | xargs -I {} sh -c 'rg -q "^using" "{}" && rg -q "^namespace" "{}" && echo "OK: {}"'

# ❌ AVOID - Unnecessary xargs when fd can do it
fd -e cs | xargs rg "pattern"                   # fd -e cs -X rg "pattern" is cleaner
```

**Complex filtering with modern tools:**

```bash
# ✅ CORRECT - Check files for conditions using rg
fd -e cs | while read -r f; do
    if rg -q "^using" "$f" && ! rg -q "^namespace" "$f"; then
        echo "Violation: $f"
    fi
done

# ✅ CORRECT - Using fd with rg for multi-step analysis
fd -e cs -x sh -c 'rg -l "^using" "$1" | xargs -I {} rg -L "^namespace" {}' _ {}

# ❌ NEVER mix traditional and modern tools
fd -e cs | xargs grep "pattern"                 # Use rg, not grep
find . -name "*.cs" -exec rg "pattern" {} \;   # Use fd, not find
```

### Code Formatting

**CRITICAL: Always run CSharpier after making ANY C# code changes.**

After creating or modifying `.cs` files, run:

```bash
dotnet tool run csharpier format .
```

This ensures consistent formatting across the codebase. Do not skip this step.

### Unity Meta File Generation

**CRITICAL: Always generate `.meta` files for every new file or folder created in the Unity package.**

Unity requires a corresponding `.meta` file for every asset. Missing `.meta` files will cause Unity to generate new ones with different GUIDs, breaking references.

#### Using the Meta File Generator

After creating any new file or folder, run:

```bash
./scripts/generate-meta.sh <path-to-file-or-folder>
```

Examples:

```bash
# For a new C# script
./scripts/generate-meta.sh Runtime/Core/NewFeature/MyNewClass.cs

# For a new folder
./scripts/generate-meta.sh Runtime/Core/NewFeature

# For documentation
./scripts/generate-meta.sh docs/features/new-feature.md

# For assembly definitions
./scripts/generate-meta.sh Runtime/NewAssembly.asmdef
```

#### When to Generate Meta Files

Generate a `.meta` file whenever you create:

- **C# source files** (`.cs`) — Uses `MonoImporter`
- **Assembly definitions** (`.asmdef`, `.asmref`) — Uses appropriate importer
- **Documentation** (`.md`, `.txt`, `.json`, `.xml`, `.yaml`) — Uses `TextScriptImporter`
- **Shaders** (`.shader`, `.compute`, `.hlsl`, `.cginc`) — Uses appropriate shader importer
- **UI files** (`.uss`, `.uxml`) — Uses UI Toolkit importers
- **Folders/directories** — Uses `DefaultImporter` with `folderAsset: yes`
- **Any other asset file** — The script handles most common Unity file types

#### Important Rules

1. **Never skip meta file generation** — Every file and folder needs a `.meta` file
2. **Generate meta files in creation order** — Create parent folders' meta files before children
3. **Use the script, don't manually create meta files** — The script generates proper GUIDs and importer settings
4. **Don't modify existing meta files** — Changing GUIDs breaks asset references
5. **Generate after file creation** — The file must exist before generating its meta file

#### Supported File Types

The script automatically selects the correct importer for:

| Extension(s)                      | Importer Type                       |
| --------------------------------- | ----------------------------------- |
| `.cs`                             | MonoImporter                        |
| `.asmdef`                         | AssemblyDefinitionImporter          |
| `.asmref`                         | AssemblyDefinitionReferenceImporter |
| `.shader`                         | ShaderImporter                      |
| `.compute`                        | ComputeShaderImporter               |
| `.shadergraph`, `.shadersubgraph` | ScriptedImporter                    |
| `.uss`, `.uxml`                   | Simple format (timeCreated)         |
| `.mat`                            | NativeFormatImporter                |
| `.asset`                          | NativeFormatImporter                |
| `.prefab`                         | PrefabImporter                      |
| `.unity`                          | DefaultImporter                     |
| `.png`, `.jpg`, `.tga`, etc.      | TextureImporter                     |
| `.wav`, `.mp3`, `.ogg`, etc.      | AudioImporter                       |
| `.fbx`, `.obj`, `.dae`, etc.      | ModelImporter                       |
| `.ttf`, `.otf`                    | TrueTypeFontImporter                |
| `.md`, `.txt`, `.json`, `.xml`    | TextScriptImporter                  |
| `package.json`                    | PackageManifestImporter             |
| directories                       | DefaultImporter (folderAsset)       |
| other                             | DefaultImporter                     |

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
- **`WallstopArrayPool<T>`** — Exact-size array pooling (use for fixed/predictable sizes)
- **`SystemArrayPool<T>`** — Variable-size array pooling (wraps `System.Buffers.ArrayPool<T>.Shared`)
- **`WallstopFastArrayPool<T>`** — Exact-size array pooling without clearing (unmanaged types)
- Pattern: `using var lease = Buffers<T>.List.Get(out List<T> buffer);`
- Pattern: `using PooledArray<T> pooled = SystemArrayPool<T>.Get(count, out T[] buffer);`
- **⚠️ WARNING**: `WallstopArrayPool<T>` and `WallstopFastArrayPool<T>` leak memory if used with variable sizes — see "Choose the right array pool" in Coding Style for details

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
