# Skill: Odin Inspector Undo Safety

<!-- trigger: Odin undo, WeakTargets, Undo.RecordObjects, Odin drawer undo, Property.Tree | Safe undo recording patterns for Odin Inspector drawers | Core -->

**Trigger**: When implementing undo support in Odin Inspector `OdinAttributeDrawer<T>` classes that need to record undo operations.

---

## When to Use

- Implementing `ApplySelection`, `ApplyValue`, or similar methods in Odin drawers
- Recording undo for multi-object editing in Odin contexts
- Accessing `Property.Tree.WeakTargets` for any purpose involving `Undo`

## When NOT to Use

- Standard PropertyDrawers (use `serializedObject.targetObjects` which is always safe)
- Read-only Odin drawers that never modify values
- Odin drawers that only set `Property.ValueEntry.WeakSmartValue` without undo

---

## The Problem

In Odin Inspector, `Property.Tree.WeakTargets` returns an `IList` that may contain:

- Non-`UnityEngine.Object` instances (e.g., plain C# objects in serialized contexts)
- Destroyed Unity objects that evaluate to `null` in Unity's equality system
- Actual `null` entries

Passing an array with null entries to `Undo.RecordObjects()` causes `ArgumentNullException` at runtime.

### Unsafe Pattern (NEVER DO THIS)

```csharp
// FORBIDDEN - Can produce null entries in the targets array
IList weakTargets = Property.Tree.WeakTargets;
UnityEngine.Object[] targets = new UnityEngine.Object[weakTargets.Count];
for (int i = 0; i < weakTargets.Count; i++)
{
    targets[i] = weakTargets[i] as UnityEngine.Object;  // May be null!
}
Undo.RecordObjects(targets, "Change Selection");  // THROWS if any null
```

### Safe Pattern (ALWAYS USE THIS)

```csharp
// CORRECT - Filters out non-Unity and destroyed targets
IList weakTargets = Property.Tree.WeakTargets;
List<UnityEngine.Object> validTargets = new(weakTargets.Count);
for (int i = 0; i < weakTargets.Count; i++)
{
    if (weakTargets[i] is UnityEngine.Object unityObject && unityObject != null)
    {
        validTargets.Add(unityObject);
    }
}

if (validTargets.Count > 0)
{
    Undo.RecordObjects(validTargets.ToArray(), "Change Selection");
}
```

### Key Points

1. **Use pattern matching** (`is UnityEngine.Object obj`) instead of `as` cast — when combined with `&& obj != null`, it handles both non-Unity types and destroyed objects
2. **Use `List<T>`** to collect only valid targets — don't pre-allocate an array that may have holes
3. **Guard `Undo.RecordObjects`** with `validTargets.Count > 0` — calling with an empty array is a no-op but wasteful
4. **Always set the value** regardless of undo success — `Property.ValueEntry.WeakSmartValue = value` should execute even if no undo targets exist

## Contrast with PropertyDrawers

Standard `PropertyDrawer` and `Editor` classes use `serializedObject.targetObjects`, which is a Unity-managed `UnityEngine.Object[]` guaranteed to contain valid objects. No filtering needed:

```csharp
// SAFE in PropertyDrawer/Editor context - targetObjects is always valid
Undo.RecordObjects(serializedObject.targetObjects, "Change Value");
```

## Lint Rule

The `lint-odin-undo-safety.ps1` script (UNH006) enforces this pattern by detecting:

- `as UnityEngine.Object` casts on `WeakTargets` elements without null filtering
- Direct casts of `WeakTargets` to `UnityEngine.Object[]`
- `Undo.RecordObjects` with unsafely-cast `WeakTargets` arrays

## Related Skills

- [editor-multi-object-editing](./editor-multi-object-editing.md) - Multi-object undo patterns
- [defensive-editor-programming](./defensive-editor-programming.md) - Overview of all defensive editor patterns
- [test-odin-drawers](./test-odin-drawers.md) - Testing Odin drawers
