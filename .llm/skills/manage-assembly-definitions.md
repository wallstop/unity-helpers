# Skill: Manage Assembly Definitions

<!-- trigger: asmdef, assembly, split, precompiled, references | Assembly definition creation, splitting, and reference management | Feature -->

**Trigger**: When creating, modifying, or splitting Unity assembly definition (`.asmdef`) files.

---

## When to Use This Skill

Use this skill when:

- Creating a new `.asmdef` file for a new directory
- Splitting an existing assembly into smaller child assemblies
- Adding or modifying `precompiledReferences` in an `.asmdef`
- Debugging compilation errors related to missing assembly references (CS0012, CS0311)
- Moving test files between assembly boundaries

---

## Mandatory Rule: Always Run the Linter

After creating or modifying ANY `.asmdef` file, you **MUST** run:

```bash
pwsh -NoProfile -File scripts/lint-asmdef.ps1
```

This validates assembly references AND checks that `Sirenix.Serialization.dll` is present in test assemblies that need it. Skipping this step risks introducing CS0012/CS0311 compilation errors that only surface when Odin Inspector is installed.

---

## Critical Rule: Transitive Precompiled References

**When `overrideReferences` is `true`, each assembly must independently list ALL precompiled DLLs it needs — including transitive dependencies from referenced assemblies.**

Unity's `overrideReferences: true` means the assembly can ONLY see the DLLs explicitly listed in its `precompiledReferences`. Unlike managed assembly references (which provide type visibility into referenced assemblies), precompiled references do NOT propagate transitively. If Assembly A references Assembly B, and Assembly B uses types from `Sirenix.Serialization.dll`, then Assembly A ALSO needs `Sirenix.Serialization.dll` in its own `precompiledReferences` to resolve those types.

### The Sirenix/Odin Inheritance Chain Problem

This project's `ScriptableObjectSingleton<T>` conditionally inherits from `SerializedScriptableObject` (from `Sirenix.Serialization.dll`) when `ODIN_INSPECTOR` is defined:

```csharp
public abstract class ScriptableObjectSingleton<T> :
#if ODIN_INSPECTOR
    SerializedScriptableObject  // From Sirenix.Serialization.dll
#else
    ScriptableObject            // From UnityEngine
#endif
    where T : ScriptableObjectSingleton<T>
```

**Any assembly that uses types derived from `ScriptableObjectSingleton<T>` MUST include `Sirenix.Serialization.dll` in its `precompiledReferences`**, or it will fail with CS0012 errors when `ODIN_INSPECTOR` is globally defined.

Types that inherit from `ScriptableObjectSingleton<T>` include:

- `AttributeMetadataCache`
- All test singletons (`TestSingleton`, `CustomPathSingleton`, etc.)

Note: `ScriptableObjectSingletonMetadata` inherits from `ScriptableObject` directly (not `ScriptableObjectSingleton<T>`), so it does NOT trigger this issue.

Similarly, `RuntimeSingleton<T>` has the same conditional inheritance pattern with `SerializedMonoBehaviour`.

### Latent Risk: Future Test Additions

Many test assemblies (e.g., `Tests.Editor.WButton`, `Tests.Editor.Extensions`) currently do NOT use `ScriptableObjectSingleton<T>`-derived types and therefore do not need `Sirenix.Serialization.dll`. However, if a developer later adds a test to one of these assemblies that references `AttributeMetadataCache` or any singleton type, the same CS0012 error will occur. The standard template below includes `Sirenix.Serialization.dll` by default to prevent this.

---

## Splitting Assemblies: Step-by-Step Checklist

When splitting a parent assembly into child assemblies:

1. **Audit the parent's `precompiledReferences`** — every DLL listed there is a candidate for child assemblies
2. **For each new child asmdef**, determine which precompiled DLLs it needs:
   - Search the child's source files for usage of types from each DLL
   - Check for **indirect** usage: if a source file uses `AttributeMetadataCache`, it transitively needs `Sirenix.Serialization.dll` (because the base class chain includes `SerializedScriptableObject`)
3. **Copy required DLLs** from the parent's `precompiledReferences` to each child
4. **Always include `Sirenix.Serialization.dll`** if the child uses ANY type derived from `ScriptableObjectSingleton<T>` or `RuntimeSingleton<T>`
5. **Verify compilation** in a context where `ODIN_INSPECTOR` is defined

### Quick Reference: Which DLLs to Include

| If the child assembly uses...                             | Include in `precompiledReferences` |
| --------------------------------------------------------- | ---------------------------------- |
| `ScriptableObjectSingleton<T>` or its subclasses          | `Sirenix.Serialization.dll`        |
| `RuntimeSingleton<T>` or its subclasses                   | `Sirenix.Serialization.dll`        |
| `AttributeMetadataCache`                                  | `Sirenix.Serialization.dll`        |
| `SerializedScriptableObject` or `SerializedMonoBehaviour` | `Sirenix.Serialization.dll`        |
| Odin editor APIs (`OdinAttributeDrawer`, etc.)            | `Sirenix.OdinInspector.Editor.dll` |
| JSON serialization APIs                                   | `System.Text.Json.dll`             |
| NUnit test framework                                      | `nunit.framework.dll`              |

---

## Standard Test Assembly Template

When creating a new test assembly that uses types from the Runtime assembly:

```json
{
  "name": "WallstopStudios.UnityHelpers.Tests.Editor.{Feature}",
  "rootNamespace": "WallstopStudios.UnityHelpers.Tests.{Feature}",
  "references": [
    "UnityEditor.TestRunner",
    "UnityEngine.TestRunner",
    "WallstopStudios.UnityHelpers",
    "WallstopStudios.UnityHelpers.Editor",
    "WallstopStudios.UnityHelpers.Tests.Core",
    "WallstopStudios.UnityHelpers.Tests.Editor"
  ],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll", "Sirenix.Serialization.dll"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "versionDefines": [],
  "noEngineReferences": false
}
```

**Note**: `Sirenix.Serialization.dll` is included by default because most test assemblies interact with `ScriptableObjectSingleton<T>`-derived types. If the DLL is not present at compile time (Odin not installed), Unity silently ignores the reference — there is no downside to including it preemptively.

---

## Diagnosing CS0012 / CS0311 Errors

When you see errors like:

```text
error CS0012: The type 'SerializedScriptableObject' is defined in an assembly that is not referenced.
You must add a reference to assembly 'Sirenix.Serialization, Version=1.0.0.0, ...'
```

or:

```text
error CS0311: The type 'X' cannot be used as type parameter 'T' in the generic type or method 'Y'.
There is no implicit reference conversion from 'X' to 'UnityEngine.ScriptableObject'.
```

**Root cause**: The consuming assembly has `overrideReferences: true` but is missing `Sirenix.Serialization.dll` in its `precompiledReferences`. The compiler can see that the type inherits from `SerializedScriptableObject` but cannot resolve that type.

**Fix**: Add `"Sirenix.Serialization.dll"` to the assembly's `precompiledReferences` array.

---

## Anti-Patterns

| Anti-Pattern                                                          | Why It's Wrong                               | Correct Approach                                   |
| --------------------------------------------------------------------- | -------------------------------------------- | -------------------------------------------------- |
| Creating child asmdef without checking parent's precompiledReferences | Missing transitive dependencies cause CS0012 | Audit parent DLLs and propagate needed ones        |
| Assuming assembly references propagate precompiled DLLs               | Unity does not propagate precompiled refs    | Each assembly lists its own precompiled DLLs       |
| Only testing without Odin installed                                   | Misses conditional compilation path bugs     | Test with ODIN_INSPECTOR defined                   |
| Omitting Sirenix.Serialization.dll from test asmdefs                  | Breaks when Odin is installed in project     | Include it preemptively (safely ignored if absent) |

---

## Related Skills

- [integrate-optional-dependency](./integrate-optional-dependency.md) - Optional dependency patterns
- [integrate-odin-inspector](./integrate-odin-inspector.md) - Odin Inspector integration
- [create-csharp-file](./create-csharp-file.md) - C# file creation patterns
- [create-test](./create-test.md) - Test creation patterns
