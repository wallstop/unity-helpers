# Skill: Create C# File

**Trigger**: When creating any new `.cs` file in this repository.

---

## Pre-Creation Checklist

1. **Determine file location**:
   - Runtime code → `Runtime/` folder tree
   - Editor-only code → `Editor/` folder tree
   - Tests → `Tests/Runtime/` or `Tests/Editor/` (mirror source structure)

2. **One file per MonoBehaviour/ScriptableObject**:
   - Each `MonoBehaviour` or `ScriptableObject` MUST have its own dedicated `.cs` file
   - ❌ Multiple MonoBehaviours in the same file
   - ❌ Test helper MonoBehaviours defined inside test class files
   - Enforced by pre-commit hook and CI/CD analyzer

---

## File Template

```csharp
namespace WallstopStudios.UnityHelpers.{Subsystem}
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public sealed class MyClass
    {
        // Implementation
    }
}
```

---

## Critical Rules

### 1. `using` Directives INSIDE Namespace

✅ **CORRECT**:

```csharp
namespace WallstopStudios.UnityHelpers.Core
{
    using System;
    using UnityEngine;

    public sealed class MyClass { }
}
```

❌ **INCORRECT**:

```csharp
using System;
using UnityEngine;

namespace WallstopStudios.UnityHelpers.Core
{
    public sealed class MyClass { }
}
```

### 2. NO Underscores in Method Names

- ✅ `GetValueWhenInputIsEmpty`
- ❌ `GetValue_When_Input_Is_Empty`
- Applies to ALL methods including tests

### 3. Explicit Types Over `var`

- ✅ `List<string> items = new List<string>();`
- ❌ `var items = new List<string>();`

### 4. Braces Required for All Control Structures

```csharp
// ✅ CORRECT
if (condition)
{
    DoSomething();
}

// ❌ INCORRECT
if (condition)
    DoSomething();
```

### 5. NEVER Use `#region`

- ❌ `#region Helper Methods`
- ❌ `#endregion`
- Organize code through class structure and file organization instead

### 6. NEVER Use Nullable Reference Types

- ❌ `string?`, `object?`, `List<string>?`, `MyClass?`
- ❌ `#nullable enable`
- ❌ Null-forgiving operator `!` (e.g., `value!`)
- ✅ `int?`, `float?`, `bool?` — Nullable VALUE types are OK

### 7. Unity Object Null Checks

For `UnityEngine.Object`-derived types (`GameObject`, `Component`, `MonoBehaviour`, etc.):

- ❌ `gameObject?.SetActive(true)` — Bypasses Unity's null check
- ❌ `component ?? fallback` — Bypasses Unity's null check
- ❌ `_cached ??= GetComponent<T>()` — Bypasses Unity's null check
- ❌ `ReferenceEquals(gameObject, null)` — Bypasses Unity's null check
- ✅ `if (gameObject != null) gameObject.SetActive(true)`
- ✅ `component != null ? component : fallback`
- ✅ `if (_cached == null) _cached = GetComponent<T>()`

### 8. Qualify `Object` References

```csharp
// ✅ CORRECT - Add using alias or fully qualify
using Object = UnityEngine.Object;

// or
UnityEngine.Object obj = ...;
```

### 9. Minimal Comments

- ✅ Comments explaining **why** a non-obvious algorithm is used
- ✅ Comments documenting Unity quirks or platform-specific behavior
- ❌ Comments describing **what** readable code does
- ❌ Commented-out code (use version control)
- ❌ TODO/FIXME without associated issue tracking
- ❌ Section dividers like `// ========= METHODS =========`

---

## Post-Creation Steps

1. **Generate meta file**:

   ```bash
   ./scripts/generate-meta.sh <path-to-file.cs>
   ```

2. **Format code**:

   ```bash
   dotnet tool run csharpier format .
   ```

3. **Verify no errors**:
   - Check IDE for compilation errors
   - Ensure `.asmdef` references are correct if adding new namespaces

---

## Naming Conventions Quick Reference

| Element               | Convention  | Example                     |
| --------------------- | ----------- | --------------------------- |
| Types, public members | PascalCase  | `SerializableDictionary`    |
| Fields, locals        | camelCase   | `keyValue`, `itemCount`     |
| Interfaces            | `I` prefix  | `IResolver`, `ISpatialTree` |
| Type parameters       | `T` prefix  | `TKey`, `TValue`            |
| Events                | `On` prefix | `OnValueChanged`            |
| Constants (public)    | PascalCase  | `DefaultCapacity`           |
