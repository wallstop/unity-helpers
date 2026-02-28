# Skill: Property Drawer Rules

<!-- trigger: drawer rules, multi-object editing, drawer testing | PropertyDrawer critical rules and requirements | Core -->

**Trigger**: When reviewing or implementing PropertyDrawer requirements, multi-object editing, or testing.

---

## Multi-Object Editing (MANDATORY)

When modifying property values via callbacks (e.g., `GenericMenu` selection), always support multi-object editing:

1. **Use `Undo.RecordObjects(serializedObject.targetObjects, ...)`** to record undo for ALL selected objects
2. **Use typed `SerializedProperty` setters** (e.g., `property.vector2Value`, `property.colorValue`) instead of reflection — these automatically handle multi-object editing through `SerializedObject.ApplyModifiedProperties()`
3. **If reflection is unavoidable**, iterate over ALL `serializedObject.targetObjects` and apply the change to each one individually

### Typed Setters vs Reflection

```csharp
// CORRECT - Uses SerializedProperty typed setters (multi-object safe)
case SerializedPropertyType.Vector2:
    if (selectedOption is Vector2 v2) property.vector2Value = v2;
    break;
case SerializedPropertyType.Color:
    if (selectedOption is Color c) property.colorValue = c;
    break;

// WRONG - Reflection on single targetObject (breaks multi-object editing)
UnityEngine.Object target = property.serializedObject.targetObject;
SetFieldValue(target, property.propertyPath, selectedOption);

// CORRECT - Reflection iterating ALL targetObjects
UnityEngine.Object[] targets = property.serializedObject.targetObjects;
for (int i = 0; i < targets.Length; i++)
{
    SetFieldValue(targets[i], property.propertyPath, selectedOption);
    EditorUtility.SetDirty(targets[i]);
}
```

---

## Standard and Odin Drawer Consistency (MANDATORY)

When this package has BOTH a standard `PropertyDrawer` AND an Odin `OdinAttributeDrawer` for the same attribute:

1. **Both drawers MUST have the same rendering behavior** — if one uses `GenericMenu`, the other must too
2. **Both drawers MUST use the same dropdown implementation** — never mix `EditorGUI.Popup` in one and `GenericMenu` in another
3. **CHANGELOG entries MUST accurately reflect which drawers were changed** — never claim all variants were updated if only some were
4. **When fixing a rendering bug in one variant, fix ALL variants** — standard drawer, Odin drawer, and their popup/inline code paths

---

## Critical Rules

### 1. `#if UNITY_EDITOR` Wrapping

Wrap all editor code after namespace declaration:

```csharp
namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    // All code here
#endif
}
```

### 2. `using` Directives INSIDE Namespace

```csharp
namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using UnityEditor;
    using UnityEngine;
    // ...
#endif
}
```

### 3. Qualify `Object` References

```csharp
using Object = UnityEngine.Object;
```

### 4. Unity Object Null Checks

```csharp
// CORRECT
if (targetObject != null)
{
    // Use targetObject
}

// INCORRECT
if (targetObject?.name != null) // Bypasses Unity null check
```

### 5. Sealed Classes

PropertyDrawers should be `sealed` unless designed for inheritance:

```csharp
public sealed class MyAttributePropertyDrawer : PropertyDrawer { }
```

---

## Post-Creation Steps (MANDATORY)

1. **Generate meta file** (required - do not skip):

   ```bash
   ./scripts/generate-meta.sh <path-to-file.cs>
   ```

   > See [create-unity-meta](./create-unity-meta.md) for full details.

2. **Format code**:

   ```bash
   dotnet tool run csharpier format .
   ```

3. **Verify no errors**:
   - Check IDE for compilation errors
   - Ensure `WallstopStudios.UnityHelpers.Editor.asmdef` reference is correct

---

## File Naming Conventions

| Type           | Naming                 | Example                     |
| -------------- | ---------------------- | --------------------------- |
| PropertyDrawer | `{Attribute}Drawer.cs` | `WNotNullPropertyDrawer.cs` |

---

## Testing PropertyDrawers (MANDATORY)

**All PropertyDrawers MUST have exhaustive tests.** Create tests in `Tests/Editor/` mirroring the source structure.

See [create-test](./create-test.md) for full testing guidelines.

### Required Test Coverage

| Category           | Test Scenarios                                      |
| ------------------ | --------------------------------------------------- |
| **Normal Cases**   | Typical usage, valid inputs, expected workflows     |
| **Negative Cases** | Invalid inputs, null values, missing dependencies   |
| **Edge Cases**     | Empty data, boundary values, unusual configurations |
| **Property Types** | All supported `SerializedPropertyType` values       |
| **Null Targets**   | Null `SerializedProperty`, null `SerializedObject`  |
| **Multi-Object**   | Multiple selected objects with different values     |

---

## Related Skills

- [create-property-drawer](./create-property-drawer.md) - Main PropertyDrawer creation guide
- [property-drawer-examples](./property-drawer-examples.md) - Dropdown and foldout examples
- [create-test](./create-test.md) - Test creation guidelines
- [test-odin-drawers](./test-odin-drawers.md) - Odin Inspector drawer testing
- [defensive-programming](./defensive-programming.md) - General defensive coding practices
