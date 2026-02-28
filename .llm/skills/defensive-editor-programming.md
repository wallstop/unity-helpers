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
- [ ] Standard and Odin drawer variants updated consistently

---

## Related Skills

- [defensive-programming](./defensive-programming.md) - Core defensive patterns for all code
- [create-editor-tool](./create-editor-tool.md) - Editor tool creation patterns
- [create-test](./create-test.md) - Test edge cases and error conditions
- [create-property-drawer](./create-property-drawer.md) - PropertyDrawer patterns including display label normalization
