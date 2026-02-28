# Skill: Property Drawer Examples

<!-- trigger: drawer example, dropdown drawer, foldout drawer | Property drawer code examples | Core -->

**Trigger**: When implementing specific PropertyDrawer patterns like dropdowns or foldouts.

---

## Drawer with Dropdown Selection

**CRITICAL**: Never use `EditorGUI.Popup` for dropdowns. It renders phantom empty rows on Linux when the selected index is -1. Always use `GenericMenu` or `EditorGUI.DropdownButton` + `GenericMenu` instead.

**Full code**: [code-samples/dropdown-drawer-example.cs](./code-samples/dropdown-drawer-example.cs)

**Key pattern summary**:

- Use `EditorGUI.DropdownButton` + `GenericMenu` (not `EditorGUI.Popup`)
- Capture `serializedObject` and `propertyPath` before lambda (closures)
- Use `Undo.RecordObjects` for multi-object editing support
- Call `serializedObject.Update()` and `ApplyModifiedProperties()` in menu callback

---

## Drawer with Foldout Section

**Full code**: [code-samples/foldout-drawer-example.cs](./code-samples/foldout-drawer-example.cs)

**Key pattern summary**:

- Use `property.isExpanded` to track foldout state (Unity persists this)
- Override `GetPropertyHeight` to return different heights based on expanded state
- Use `EditorGUI.Foldout` with `toggleOnLabelClick: true` for better UX
- Manage `EditorGUI.indentLevel` for nested content
- Use helper method like `DrawChildProperty` to reduce code duplication

---

## Display Label Normalization (Dropdown Drawers)

When a dropdown renders a fallback for null/empty labels (e.g., `(Option N)`), **every** code path — rendering, search, filter, suggestion, selection — MUST use the same normalized label. Use a single `GetNormalizedDisplayLabel(int)` helper backed by `DropDownShared.GetFallbackOptionLabel(int)`. Never call raw `GetDisplayLabel` in search/filter/suggestion paths.

See also: [defensive-editor-programming - Consistent Display Label Normalization](./defensive-editor-programming.md)

---

## Related Skills

- [create-property-drawer](./create-property-drawer.md) - Main PropertyDrawer creation guide
- [property-drawer-rules](./property-drawer-rules.md) - Critical rules and requirements
- [editor-caching-patterns](./editor-caching-patterns.md) - Editor caching and common patterns
