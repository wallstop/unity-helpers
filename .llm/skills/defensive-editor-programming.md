# Skill: Defensive Editor Programming

<!-- trigger: editor defensive, serializedproperty, asset safety, editor validation | Editor code - handle Unity Editor edge cases | Core -->

**Trigger**: When writing Unity Editor code (custom inspectors, property drawers, editor windows, asset processors). Editor code requires additional defensive patterns beyond standard defensive programming.

---

## When to Use This Skill

Use this skill when:

- Creating custom PropertyDrawers or Editors
- Working with SerializedProperty and SerializedObject
- Loading or manipulating assets via AssetDatabase
- Caching editor state that may be invalidated by Unity reloads
- Handling serialization in editor contexts
- Writing event/callback handlers in editor tools

---

## Why Editor Code Needs Extra Defense

Editor code is especially vulnerable because:

- User can modify inspector values at any time
- SerializedProperty may reference destroyed objects
- Assets may be deleted mid-operation
- Unity reload (domain reload, assembly reload) may invalidate cached state
- Selection can change during multi-frame operations
- Undo/Redo can restore unexpected states

---

## Code Samples

Detailed code patterns are in dedicated sample files:

- [SerializedProperty Safety](./code-samples/editor-serialized-property-safety.md) - Safe property access, height calculation, nested property access
- [Asset Operations Safety](./code-samples/editor-asset-operations-safety.md) - Safe asset loading, iteration, and creation
- [Cache Invalidation Safety](./code-samples/editor-cache-invalidation-safety.md) - Safe caching with domain reload handling
- [Serialization Safety](./code-samples/editor-serialization-safety.md) - Defensive deserialization, EditorPrefs safety
- [Event/Callback Safety](./code-samples/editor-event-callback-safety.md) - Safe event invocation and callback registration

---

## Multi-Object Editing Pitfalls (CRITICAL)

Multi-object editing is a common source of bugs. These patterns prevent the most frequent issues:

### Never Modify Property During Render Phase

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

### Set showMixedValue BEFORE Calculations

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

### Display Conventions for Mixed/Invalid Values

| State                  | Display                     | Notes                     |
| ---------------------- | --------------------------- | ------------------------- |
| Mixed values           | `"\u2014"` (em dash)        | Standard Unity convention |
| Out-of-range index     | `"{index} (Invalid)"`       | Never silently clamp      |
| Null/missing reference | `"(None)"` or `"(Missing)"` | Context-dependent         |

### Undo Support for Multi-Object

```csharp
void ApplyValueToAllTargets(SerializedProperty property, object value)
{
    // Step 1: Record undo for ALL targets
    Undo.RecordObjects(property.serializedObject.targetObjects, "Change Value");

    // Step 2: Apply to each target
    foreach (var target in property.serializedObject.targetObjects)
    {
        SetFieldValue(target, property.propertyPath, value);
        EditorUtility.SetDirty(target);
    }

    // Step 3: Sync serialized state
    property.serializedObject.Update();
}
```

### Odin Inspector Mixed Value Detection

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

## Forbidden Editor APIs

### EditorGUI.Popup - NEVER USE

`EditorGUI.Popup` renders phantom empty rows on Linux when the selected index is -1. Always use `EditorGUI.DropdownButton` + `GenericMenu` instead. This applies to ALL drawers - standard PropertyDrawers and Odin OdinAttributeDrawers.

```csharp
// FORBIDDEN - Phantom rows on Linux with index -1
int newIndex = EditorGUI.Popup(position, label.text, currentIndex, displayOptions);

// CORRECT - GenericMenu-based dropdown
Rect fieldRect = EditorGUI.PrefixLabel(position, label);
if (EditorGUI.DropdownButton(fieldRect, buttonContent, FocusType.Keyboard))
{
    GenericMenu menu = new();
    for (int i = 0; i < options.Length; i++)
    {
        int captured = i;
        menu.AddItem(new GUIContent(options[i]), i == currentIndex,
            () => ApplySelection(captured));
    }
    menu.DropDown(fieldRect);
}
```

---

## Consistent Display Label Normalization

When editor UI renders a fallback label (e.g., `(Option N)` for empty strings), **every** code path that references that label must apply the same normalization - not just rendering, but also search, filter, suggestion, and selection. Centralize the fallback in a single helper (e.g., `GetNormalizedDisplayLabel`) and never use the raw label in comparison paths.

**Rule:** If any code path transforms a value for display, audit all other paths that read the same value and apply the identical transform. This covers rendering, search/filter, autocomplete/suggestion, selection/resolution, and keyboard navigation labels.

---

## IMGUI vs UI Toolkit Value Handling

PropertyDrawers behave differently depending on which rendering path is used. Understanding these differences prevents unexpected value modifications:

### OnGUI (IMGUI Path)

- **Display-only**: Invalid values are shown as "(Invalid)" but are NOT modified
- **No clamping**: Out-of-range values remain unchanged during render
- **Explicit user action required**: Values only change when user makes a selection
- **Rationale**: IMGUI renders every frame; modifying values during render causes infinite repaint loops

```csharp
// OnGUI renders but does NOT modify invalid values
public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
{
    int currentValue = property.intValue;
    int index = Array.IndexOf(options, currentValue);

    // Display "(Invalid)" for out-of-range, but DO NOT clamp
    string display = index >= 0 ? options[index].ToString() : $"{currentValue} (Invalid)";

    // Value only changes via user selection callback, never during render
}
```

### CreatePropertyGUI (UI Toolkit Path)

- **Clamps on initialization**: Invalid values are clamped to the first valid option
- **Binding-driven**: UI Toolkit uses bindings that sync values immediately
- **One-time setup**: CreatePropertyGUI runs once, not every frame
- **Rationale**: UI Toolkit binding model expects valid values for proper field state

```csharp
// CreatePropertyGUI clamps invalid values during initialization
public override VisualElement CreatePropertyGUI(SerializedProperty property)
{
    int currentValue = property.intValue;
    int index = Array.IndexOf(options, currentValue);

    // Clamp to first option if invalid
    if (index < 0 && options.Length > 0)
    {
        property.intValue = options[0];
        property.serializedObject.ApplyModifiedProperties();
    }

    return CreateDropdownField(property);
}
```

### Summary Table

| Aspect              | OnGUI (IMGUI)            | CreatePropertyGUI (UI Toolkit) |
| ------------------- | ------------------------ | ------------------------------ |
| Invalid value       | Display "(Invalid)"      | Clamp to first option          |
| Modification timing | Only on user selection   | On initialization              |
| Render frequency    | Every frame              | Once (binding-driven updates)  |
| Value preservation  | Preserves invalid values | Normalizes to valid values     |

---

## Quick Checklist for Editor Code

Before submitting editor code, verify:

- [ ] SerializedProperty null-checked before access ([SerializedProperty Safety](./code-samples/editor-serialized-property-safety.md))
- [ ] SerializedObject.targetObject validated ([SerializedProperty Safety](./code-samples/editor-serialized-property-safety.md))
- [ ] Asset paths validated before AssetDatabase operations ([Asset Operations Safety](./code-samples/editor-asset-operations-safety.md))
- [ ] Selection validated (may be empty or contain nulls) ([Asset Operations Safety](./code-samples/editor-asset-operations-safety.md))
- [ ] Cache invalidation handles domain reload ([Cache Invalidation Safety](./code-samples/editor-cache-invalidation-safety.md))
- [ ] Cache invalidation handles target object changes ([Cache Invalidation Safety](./code-samples/editor-cache-invalidation-safety.md))
- [ ] Event handlers wrapped in try-catch ([Event/Callback Safety](./code-samples/editor-event-callback-safety.md))
- [ ] Event subscriptions cleaned up in OnDisable ([Event/Callback Safety](./code-samples/editor-event-callback-safety.md))
- [ ] Deserialization handles corrupt/missing data ([Serialization Safety](./code-samples/editor-serialization-safety.md))
- [ ] EditorPrefs access handles missing keys ([Serialization Safety](./code-samples/editor-serialization-safety.md))
- [ ] Display label fallbacks applied consistently across all code paths
- [ ] No `EditorGUI.Popup` usage (use `GenericMenu` instead - Linux phantom rows)
- [ ] Multi-object editing supported (iterate `targetObjects`, use typed setters)
- [ ] No property modification during OnGUI render phase (only in callbacks)
- [ ] `EditorGUI.showMixedValue` set BEFORE index/value calculations
- [ ] Em dash (`\u2014`) displayed for mixed values
- [ ] "(Invalid)" suffix shown for out-of-range indices (no silent clamping)
- [ ] `Undo.RecordObjects(targetObjects, ...)` used for multi-object undo
- [ ] Odin drawers manually check mixed values via `ValueEntry.WeakValues`
- [ ] Standard and Odin drawer variants updated consistently

---

## Related Skills

- [defensive-programming](./defensive-programming.md) - Core defensive patterns for all code
- [create-editor-tool](./create-editor-tool.md) - Editor tool creation patterns
- [create-test](./create-test.md) - Test edge cases and error conditions
- [create-property-drawer](./create-property-drawer.md) - PropertyDrawer patterns including display label normalization
