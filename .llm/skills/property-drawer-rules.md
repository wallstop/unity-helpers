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

## Multi-Object Editing Pitfalls (CRITICAL)

Multi-object editing is notoriously bug-prone. These rules prevent common issues:

### 1. Never Modify Property During Render Phase

Only modify `SerializedProperty` values in callbacks (e.g., `GenericMenu` selection) or inside `BeginChangeCheck`/`EndChangeCheck` guards, NEVER unconditionally during `OnGUI` rendering:

```csharp
// WRONG - Modifying during render causes infinite repaint loops
public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
{
    int index = CalculateIndex(property);
    if (index < 0)
    {
        property.intValue = 0; // BUG: Writing during render!
    }
}

// WRONG - Direct assignment writes every frame even without user interaction
apply.boolValue = EditorGUI.ToggleLeft(r, label, apply.boolValue);
nameProp.stringValue = EditorGUI.TextField(r, "Name", nameProp.stringValue);

// CORRECT - Only modify in callbacks
public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
{
    if (EditorGUI.DropdownButton(fieldRect, content, FocusType.Keyboard))
    {
        GenericMenu menu = new();
        menu.AddItem(new GUIContent("Option"), false, () =>
        {
            property.intValue = 0; // Safe: callback context
            property.serializedObject.ApplyModifiedProperties();
        });
        menu.DropDown(fieldRect);
    }
}

// CORRECT - Guard with change detection
EditorGUI.BeginChangeCheck();
bool newValue = EditorGUI.ToggleLeft(r, label, apply.boolValue);
if (EditorGUI.EndChangeCheck())
{
    apply.boolValue = newValue;
}
```

**Note**: `EditorGUI.PropertyField()` handles change detection internally and does not need explicit guards.

### 2. Set showMixedValue BEFORE Index Calculations

Always check `hasMultipleDifferentValues` and set `EditorGUI.showMixedValue` BEFORE calculating display indices:

```csharp
// CORRECT - Check mixed state first
bool isMixed = property.hasMultipleDifferentValues;
EditorGUI.showMixedValue = isMixed;

// Now calculate index (may be invalid for some objects when mixed)
int currentIndex = GetCurrentIndex(property);
string displayText = isMixed ? "\u2014" : GetDisplayText(currentIndex);
```

### 3. Use Em Dash for Mixed Values

Display `"\u2014"` (em dash) when values differ across selected objects. This is the standard Unity convention:

```csharp
string GetDisplayValue(SerializedProperty property, string[] options)
{
    if (property.hasMultipleDifferentValues)
    {
        return "\u2014"; // Em dash for mixed values
    }
    int index = property.intValue;
    return IsValidIndex(index, options) ? options[index] : $"{index} (Invalid)";
}
```

### 4. Show "(Invalid)" for Out-of-Range Values

Never silently clamp invalid indices. Show the invalid state clearly:

```csharp
// WRONG - Silent clamping hides data corruption
int safeIndex = Mathf.Clamp(property.intValue, 0, options.Length - 1);

// CORRECT - Show invalid state
int index = property.intValue;
if (index < 0 || index >= options.Length)
{
    displayText = $"{index} (Invalid)";
}
```

### 5. Guard GenericMenu `isSelected` and Popup `SelectedIndex` for Mixed Values

When building `GenericMenu` items, guard the `isSelected` flag with `!property.hasMultipleDifferentValues` to prevent misleading checkmarks in multi-object editing mode:

```csharp
// CORRECT - No checkmark when values differ across selected objects
bool isSelected = i == currentIndex && !property.hasMultipleDifferentValues;
menu.AddItem(new GUIContent(label), isSelected, callback);

// WRONG - Shows a checkmark based on one object's value even when mixed
bool isSelected = i == currentIndex;
```

Similarly, popup window `SelectedIndex` should be set to `-1` when mixed:

```csharp
// CORRECT - Popup highlights nothing when mixed
SelectedIndex = property.hasMultipleDifferentValues ? -1 : currentIndex,

// WRONG - Highlights an index that only applies to one of the selected objects
SelectedIndex = currentIndex,
```

### 6. Undo.RecordObjects Pattern for Multi-Object

When modifying via reflection or direct field access, record undo for ALL targets:

```csharp
void ApplySelection(SerializedProperty property, object newValue)
{
    // Record undo for all selected objects
    Undo.RecordObjects(property.serializedObject.targetObjects, "Change Value");

    // Apply to all targets
    foreach (var target in property.serializedObject.targetObjects)
    {
        SetFieldValue(target, property.propertyPath, newValue);
        EditorUtility.SetDirty(target);
    }

    Undo.FlushUndoRecordObjects();
    property.serializedObject.Update();
}
```

### 7. Odin Drawer Mixed Value Detection

Odin drawers do NOT have `hasMultipleDifferentValues`. Manually check:

```csharp
// In OdinAttributeDrawer
bool HasMixedValues<T>(IPropertyValueEntry<T> valueEntry)
{
    if (valueEntry.ValueCount <= 1)
        return false;

    T firstValue = valueEntry.WeakValues[0] as T;
    for (int i = 1; i < valueEntry.ValueCount; i++)
    {
        T currentValue = valueEntry.WeakValues[i] as T;
        if (!EqualityComparer<T>.Default.Equals(firstValue, currentValue))
            return true;
    }
    return false;
}

// Usage
bool isMixed = HasMixedValues(Property.ValueEntry);
EditorGUI.showMixedValue = isMixed;
```

### 8. UI Toolkit Elements Need Same Handling

UI Toolkit `VisualElement`-based drawers require the same patterns:

```csharp
public override VisualElement CreatePropertyGUI(SerializedProperty property)
{
    var dropdown = new DropdownField();

    // Bind with mixed value support
    dropdown.RegisterCallback<AttachToPanelEvent>(evt =>
    {
        UpdateDropdownDisplay(dropdown, property);
    });

    return dropdown;
}

void UpdateDropdownDisplay(DropdownField dropdown, SerializedProperty property)
{
    if (property.hasMultipleDifferentValues)
    {
        dropdown.SetValueWithoutNotify("\u2014");
        return;
    }
    // Normal display logic
}
```

### 9. Default Field Values Must Not Collide with Sentinel Values

When a dropdown drawer uses a sentinel value (e.g., empty string for "Custom" mode), the data class field must NOT default to that sentinel. Otherwise, new entries will appear in the sentinel state (e.g., "Custom") instead of a sensible known option, and APIs that skip sentinel-valued entries will silently ignore new entries:

```csharp
// WRONG - Default collides with Custom sentinel
public string platformName = string.Empty; // Custom sentinel is also empty string
// New entries render as "Custom" and are skipped by the API

// CORRECT - Default is a known valid value; sentinel is distinct
public string platformName = TexturePlatformNameHelper.DefaultPlatformName;
// New entries render as "DefaultTexturePlatform" and are processed by the API
// Custom sentinel (empty string) is only set when user explicitly selects "Custom"
```

**Rule**: Define constants for sentinel values and ensure default field values use a known valid option. When the drawer maps `string.Empty` to "Custom", the field must default to a concrete platform or option name.

### 10. Reuse GUIContent in OnGUI — Never Allocate Per Frame

`OnGUI` runs every frame. Allocating `new GUIContent(...)` inside `OnGUI` or `DrawPropertyLayout` creates avoidable GC pressure. Reuse a static `GUIContent` instance and update its `.text`/`.tooltip` before each use:

```csharp
// WRONG - Allocates GUIContent every frame
public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
{
    GUIContent buttonContent = new(displayValue); // Allocation!
    EditorGUI.DropdownButton(fieldRect, buttonContent, FocusType.Keyboard);
}

// CORRECT - Reuse static instance
private static readonly GUIContent ReusableDropDownButtonContent = new();

public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
{
    ReusableDropDownButtonContent.text = displayValue;
    ReusableDropDownButtonContent.tooltip = string.Empty;
    EditorGUI.DropdownButton(fieldRect, ReusableDropDownButtonContent, FocusType.Keyboard);
}
```

**Exception**: `GenericMenu.AddItem(new GUIContent(...), ...)` allocations are acceptable because they only execute on user click (not every frame) and `GenericMenu` stores references internally, so a single instance cannot be reused across multiple `AddItem` calls.

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
- [defensive-editor-programming](./defensive-editor-programming.md) - Editor defensive coding patterns
- [defensive-programming](./defensive-programming.md) - General defensive coding practices
- [editor-multi-object-editing](./editor-multi-object-editing.md) - Multi-object editing patterns and undo support
