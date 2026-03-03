# Editor Code Sample: SerializedProperty Safety

<!-- parent: defensive-editor-programming.md -->

Code patterns for safely working with SerializedProperty in Unity Editor code.

---

## Safe Property Access

```csharp
// Safe property access in PropertyDrawer.OnGUI
public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
{
    if (property == null)
    {
        return;
    }

    if (property.serializedObject == null || property.serializedObject.targetObject == null)
    {
        EditorGUI.LabelField(position, label, new GUIContent("(Missing Object)"));
        return;
    }

    EditorGUI.BeginProperty(position, label, property);
    try
    {
        // Draw property...
    }
    finally
    {
        EditorGUI.EndProperty();
    }
}
```

---

## Safe Property Height Calculation

```csharp
public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
{
    if (property == null || property.serializedObject == null)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    // Calculate actual height...
    return EditorGUIUtility.singleLineHeight;
}
```

---

## Safe Nested Property Access

```csharp
// Safe nested property access
private SerializedProperty GetNestedProperty(SerializedProperty parent, string path)
{
    if (parent == null || string.IsNullOrEmpty(path))
    {
        return null;
    }

    SerializedProperty nested = parent.FindPropertyRelative(path);
    // FindPropertyRelative returns null if not found - no exception
    return nested;
}
```

---

## Safe Property Iteration

```csharp
// Safe sibling property iteration with NextVisible
SerializedProperty iterator = serializedObject.GetIterator();
bool enterChildren = true;
while (iterator.NextVisible(enterChildren))
{
    if (iterator.propertyPath == "m_Script") continue; // Skip script field
    enterChildren = false;
    // Process property safely...
}
```

```csharp
// Safe iteration with Next() - includes hidden properties
SerializedProperty iterator = serializedObject.GetIterator();
bool enterChildren = true;
while (iterator.Next(enterChildren))
{
    enterChildren = false;

    // Validate property is still valid during iteration
    if (iterator.serializedObject == null || iterator.serializedObject.targetObject == null)
    {
        break; // Object was destroyed during iteration
    }

    // Process property...
}
```

```csharp
// Safe iteration with depth tracking to avoid infinite loops
SerializedProperty iterator = property.Copy(); // Always copy to avoid modifying original
int startDepth = iterator.depth;
bool enterChildren = true;

while (iterator.NextVisible(enterChildren))
{
    enterChildren = false;

    // Stop when we've returned to the starting depth or higher (exited children)
    if (iterator.depth <= startDepth)
    {
        break;
    }

    // Process child properties...
}
```

```csharp
// Safe iteration with undo/redo awareness
private void DrawAllProperties(SerializedObject serializedObject)
{
    // Always refresh before iterating - handles undo/redo
    serializedObject.Update();

    SerializedProperty iterator = serializedObject.GetIterator();
    bool enterChildren = true;

    EditorGUI.BeginChangeCheck();
    while (iterator.NextVisible(enterChildren))
    {
        if (iterator.propertyPath == "m_Script") continue;
        enterChildren = false;

        EditorGUILayout.PropertyField(iterator, true);
    }

    if (EditorGUI.EndChangeCheck())
    {
        serializedObject.ApplyModifiedProperties();
    }
}
```

### Handling Edge Cases

| Edge Case                             | Solution                                                        |
| ------------------------------------- | --------------------------------------------------------------- |
| Last property                         | `Next()`/`NextVisible()` returns false - loop exits naturally   |
| Object destroyed during iteration     | Check `serializedObject.targetObject != null` before processing |
| Undo/redo during iteration            | Call `serializedObject.Update()` before starting iteration      |
| Modifying properties during iteration | Use `Copy()` for the iterator and apply changes after iteration |
| Nested iteration                      | Always use `Copy()` to create independent iterators             |

---

## Key Points

- Always null-check `property` before any access
- Validate `property.serializedObject` and `property.serializedObject.targetObject`
- Use `EditorGUI.BeginProperty`/`EndProperty` with try-finally
- `FindPropertyRelative` returns null safely (no exception)
- Return sensible defaults (single line height) when property is invalid
- Use `Copy()` when iterating to avoid modifying the original property
- Call `serializedObject.Update()` before iteration to handle undo/redo
- Track depth when iterating children to know when to stop
- `NextVisible(true)` enters children, `NextVisible(false)` iterates siblings

---

## See Also

- [Serialization Safety](./editor-serialization-safety.md) - Safe deserialization and EditorPrefs
- [Asset Operations Safety](./editor-asset-operations-safety.md) - Safe asset loading and creation
- [Cache Invalidation Safety](./editor-cache-invalidation-safety.md) - Proper cache management
- [Event and Callback Safety](./editor-event-callback-safety.md) - Safe event handling
