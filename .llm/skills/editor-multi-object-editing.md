# Skill: Editor Multi-Object Editing

<!-- trigger: multi-object editing, mixed values, undo support, targetObjects | Multi-object editing patterns and undo support for editor code | Core -->

**Trigger**: When implementing multi-object editing support in PropertyDrawers, custom Editors, or editor tools that modify SerializedProperty values.

---

## When to Use

- Implementing PropertyDrawers or custom Editors that modify values
- Adding GenericMenu callbacks that change property values
- Supporting undo/redo in multi-object editing scenarios
- Handling mixed values display in the Inspector

## When NOT to Use

- Read-only editor UI that never modifies properties
- Editor tools that operate on a single asset (not multi-select)

---

## Never Modify Property During Render Phase

Property modification during `OnGUI` causes infinite repaint loops and corrupted state:

```csharp
// FORBIDDEN - Writing to property during render
public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
{
    if (property.intValue < 0)
    {
        property.intValue = 0; // BUG: Causes repaint loop!
    }
    // ... render code
}

// CORRECT - Only modify in event callbacks
public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
{
    // ... render code (read-only)

    if (EditorGUI.DropdownButton(rect, content, FocusType.Keyboard))
    {
        ShowMenu(property); // Menu callback will modify property
    }
}
```

---

## Guard ALL OnGUI Value Reads with BeginChangeCheck/EndChangeCheck

Even seemingly harmless assignments like `property.boolValue = EditorGUI.ToggleLeft(...)` or `property.stringValue = EditorGUI.TextField(...)` write to the property EVERY frame, dirtying the serialized object and potentially corrupting undo history. Always guard with change detection:

```csharp
// FORBIDDEN - Direct assignment every frame
apply.boolValue = EditorGUI.ToggleLeft(r, label, apply.boolValue);
nameProp.stringValue = EditorGUI.TextField(r, "Name", nameProp.stringValue);

// CORRECT - Only write on actual change
EditorGUI.BeginChangeCheck();
bool newValue = EditorGUI.ToggleLeft(r, label, apply.boolValue);
if (EditorGUI.EndChangeCheck())
{
    apply.boolValue = newValue;
}

EditorGUI.BeginChangeCheck();
string newName = EditorGUI.TextField(r, "Name", nameProp.stringValue);
if (EditorGUI.EndChangeCheck())
{
    nameProp.stringValue = newName;
}
```

**Note**: `EditorGUI.PropertyField()` handles change detection internally and is safe without explicit guards.

---

## GenericMenu Callbacks Must Record Undo and Handle All Options

GenericMenu callbacks execute asynchronously. They MUST:

1. Call `Undo.RecordObjects(serializedObject.targetObjects, ...)` before mutation
2. Handle ALL menu options, including special options like "Custom" or "None"
3. Call `serializedObject.ApplyModifiedProperties()` after mutation

```csharp
// FORBIDDEN - Missing Undo, special option not handled
menu.AddItem(new GUIContent(choices[i]), isSelected, () =>
{
    serializedObject.Update();
    SerializedProperty prop = serializedObject.FindProperty(propertyPath);
    if (value != CustomOptionLabel)  // BUG: "Custom" does nothing!
    {
        prop.stringValue = value;
    }
    serializedObject.ApplyModifiedProperties();
});

// CORRECT - Undo recorded, all options handled
menu.AddItem(new GUIContent(choices[i]), isSelected, () =>
{
    Undo.RecordObjects(serializedObject.targetObjects, "Change Selection");
    serializedObject.Update();
    SerializedProperty prop = serializedObject.FindProperty(propertyPath);
    if (prop == null) return;

    if (value == CustomOptionLabel)
    {
        prop.stringValue = string.Empty;  // Triggers custom mode
    }
    else
    {
        prop.stringValue = value;
    }
    serializedObject.ApplyModifiedProperties();
});
```

---

## Never Assign Computed Display Values Back to Properties

When rendering a dropdown that computes a display label from the property value, never assign the display label back to the property during render. This is redundant, dirties the object every frame, and can overwrite values set by GenericMenu callbacks:

```csharp
// FORBIDDEN - Redundant render-phase writeback
string currentDisplay = choices[GetSelectedIndex(nameProp.stringValue, choices)];
// ... render dropdown ...
nameProp.stringValue = currentDisplay; // BUG: Overwrites every frame!

// CORRECT - Property is read-only during render; only callbacks modify it
string currentDisplay = choices[GetSelectedIndex(nameProp.stringValue, choices)];
// ... render dropdown using currentDisplay for display only ...
```

---

## Set showMixedValue BEFORE Calculations

Always check and set mixed state before any index or value calculations:

```csharp
// CORRECT order
EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
int currentIndex = CalculateDisplayIndex(property); // May be invalid when mixed
string display = property.hasMultipleDifferentValues ? "\u2014" : GetLabel(currentIndex);

// WRONG order - may use invalid index before checking mixed state
int currentIndex = CalculateDisplayIndex(property); // Bug: Invalid for some targets
EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
```

---

## Display Conventions for Mixed/Invalid Values

| State                  | Display                     | Notes                     |
| ---------------------- | --------------------------- | ------------------------- |
| Mixed values           | `"\u2014"` (em dash)        | Standard Unity convention |
| Out-of-range index     | `"{index} (Invalid)"`       | Never silently clamp      |
| Null/missing reference | `"(None)"` or `"(Missing)"` | Context-dependent         |

---

## Undo Support for Multi-Object

There are two valid approaches for undo support. Choose based on whether you can use the SerializedProperty API or need direct object mutation.

### Approach A: SerializedProperty API (Preferred)

When you can express changes through SerializedProperty, this is the simplest and safest approach:

```csharp
void ReorderElements(SerializedProperty arrayProperty, int fromIndex, int toIndex)
{
    Undo.RecordObjects(arrayProperty.serializedObject.targetObjects, "Reorder Elements");
    arrayProperty.MoveArrayElement(fromIndex, toIndex);
    arrayProperty.serializedObject.ApplyModifiedProperties(); // Writes changes AND integrates with undo
}
```

`ApplyModifiedProperties()` handles both writing the changes and finalizing the undo record.

### Approach B: Direct Object Mutation

When you must modify the underlying C# objects directly (e.g., calling methods on the target, complex mutations):

```csharp
void ApplyValueToAllTargets(SerializedProperty property, object value)
{
    // Step 1: Record undo for ALL targets (captures "before" snapshot)
    Undo.RecordObjects(property.serializedObject.targetObjects, "Change Value");

    // Step 2: Apply to each target
    foreach (var target in property.serializedObject.targetObjects)
    {
        SetFieldValue(target, property.propertyPath, value);
        EditorUtility.SetDirty(target);
    }

    // Step 3: CRITICAL - Flush undo records to finalize the diff
    Undo.FlushUndoRecordObjects();

    // Step 4: Sync serialized state
    property.serializedObject.Update();
}
```

**Common bug**: Forgetting `Undo.FlushUndoRecordObjects()` in Approach B causes undo to silently fail. The undo record is never finalized, so `Undo.PerformUndo()` has nothing to revert. This is especially hard to catch because:

- It works in normal Editor usage (end-of-frame processing handles the flush automatically)
- It fails in tests (no frame loop to trigger automatic flush)
- It fails when multiple operations happen in sequence (each `RecordObjects` may overwrite the previous unflushed snapshot)

**Rule**: If you call `Undo.RecordObjects()` and then mutate the objects directly (not via `SerializedProperty` + `ApplyModifiedProperties()`), you MUST call `Undo.FlushUndoRecordObjects()` before the next `RecordObjects` call or before the method returns.

---

## Default Field Values Must Not Collide with Sentinel Values

When a dropdown uses a sentinel value (e.g., empty string for "Custom" mode), ensure the data class field defaults to a known valid option, not the sentinel:

```csharp
// WRONG - New entries appear as "Custom" and are silently skipped by APIs
public string platformName = string.Empty;

// CORRECT - New entries default to a valid platform
public string platformName = TexturePlatformNameHelper.DefaultPlatformName;
```

**Rule**: Sentinel values (empty string, `-1`, `null`) must only be assigned through explicit user action (e.g., selecting "Custom" from a dropdown), never as the default field value.

---

## Odin Inspector Mixed Value Detection

Odin `OdinAttributeDrawer` lacks `hasMultipleDifferentValues`. Check manually:

```csharp
bool IsMixedValue<T>(InspectorProperty property)
{
    var entry = property.ValueEntry;
    if (entry.ValueCount <= 1) return false;

    var first = entry.WeakValues[0];
    for (int i = 1; i < entry.ValueCount; i++)
    {
        if (!Equals(first, entry.WeakValues[i]))
            return true;
    }
    return false;
}
```

---

## Related Skills

- [defensive-editor-programming](./defensive-editor-programming.md) - Overview of all defensive editor patterns
- [property-drawer-rules](./property-drawer-rules.md) - PropertyDrawer critical rules and requirements
- [create-property-drawer](./create-property-drawer.md) - PropertyDrawer creation patterns
- [editor-api-rules](./editor-api-rules.md) - Forbidden APIs and value handling rules
