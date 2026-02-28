# Skill: Create Property Drawer

<!-- trigger: drawer, property drawer, attribute drawer | Creating PropertyDrawers for custom attributes | Core -->

**Trigger**: When creating Unity PropertyDrawers for custom attributes or field types in this repository.

---

## File Location

All PropertyDrawers MUST go in `Editor/CustomDrawers/`.

> Editor code cannot reference Runtime code unless via assembly definition references.

---

## PropertyDrawer Template

See full template: [code-samples/property-drawer-template.cs](./code-samples/property-drawer-template.cs)

> **Note**: The inline template below shows IMGUI-only implementation. The full template file adds:
>
> - `UnityEditor.UIElements` and `UnityEngine.UIElements` imports
> - `CreatePropertyGUI(SerializedProperty)` method for UI Toolkit support

```csharp
namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [CustomPropertyDrawer(typeof(MyAttribute))]
    public sealed class MyAttributePropertyDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, float> HeightCache = new(StringComparer.Ordinal);
        private static readonly GUIContent ReusableContent = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float baseHeight = EditorGUI.GetPropertyHeight(property, label, true);
            return baseHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            try
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
            finally
            {
                EditorGUI.EndProperty();
            }
        }
    }
#endif
}
```

---

## Key PropertyDrawer Patterns

1. **Caching**: Use static `Dictionary` caches for height calculations - drawers run every frame
2. **Reusable GUIContent**: Create static `GUIContent` instances to reduce allocations
3. **BeginProperty/EndProperty**: Always wrap drawing in `EditorGUI.BeginProperty` / `EndProperty`
4. **Height Calculation**: Override `GetPropertyHeight` if adding custom elements

---

## Caching Requirements (CRITICAL)

PropertyDrawers run **every frame**. Minimize allocations:

```csharp
// CORRECT - Static caches
private static readonly Dictionary<string, float> HeightCache = new(StringComparer.Ordinal);
private static readonly GUIContent ReusableContent = new();

// CORRECT - Pool expensive objects
private static readonly WallstopGenericPool<List<string>> ListPool = new(
    () => new List<string>(),
    onRelease: list => list.Clear()
);

// INCORRECT - Allocates every frame
public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
{
    GUIContent content = new GUIContent("Label"); // Allocation!
    List<string> items = new List<string>();       // Allocation!
}
```

### Shared Caches (DRY Principle)

When multiple drawers need the same cache, use `EditorCacheHelper`:

```csharp
// CORRECT - Use shared cache helper
using WallstopStudios.UnityHelpers.Editor.Core.Helper;

// In any drawer:
string display = EditorCacheHelper.GetCachedIntString(index);
string pageLabel = EditorCacheHelper.GetPaginationLabel(page, total);
Texture2D solidTex = EditorCacheHelper.GetSolidTexture(color);

// INCORRECT - Duplicating caches across multiple files
// Drawer1.cs
private static readonly Dictionary<int, string> IntToStringCache = new();

// Drawer2.cs (duplicate!)
private static readonly Dictionary<int, string> IntToStringCache = new();
```

See [editor-caching-patterns](./editor-caching-patterns.md) for complete caching guidance.

---

## SerializedProperty Patterns

### Common Property Operations

```csharp
// Get property
SerializedProperty property = serializedObject.FindProperty("_fieldName");

// Array operations
SerializedProperty arrayProp = serializedObject.FindProperty("_items");
int arraySize = arrayProp.arraySize;
SerializedProperty element = arrayProp.GetArrayElementAtIndex(0);
arrayProp.InsertArrayElementAtIndex(arraySize);
arrayProp.DeleteArrayElementAtIndex(0);

// Nested properties
SerializedProperty nested = property.FindPropertyRelative("nestedField");

// Iterate children
SerializedProperty iterator = property.Copy();
SerializedProperty endProperty = iterator.GetEndProperty();
while (iterator.NextVisible(true) && !SerializedProperty.EqualContents(iterator, endProperty))
{
    // Process each visible property
}

// Apply changes
serializedObject.ApplyModifiedProperties();
```

### Property Types

```csharp
switch (property.propertyType)
{
    case SerializedPropertyType.Integer:
        int intValue = property.intValue;
        break;
    case SerializedPropertyType.Float:
        float floatValue = property.floatValue;
        break;
    case SerializedPropertyType.String:
        string stringValue = property.stringValue;
        break;
    case SerializedPropertyType.Boolean:
        bool boolValue = property.boolValue;
        break;
    case SerializedPropertyType.ObjectReference:
        Object objValue = property.objectReferenceValue;
        break;
    case SerializedPropertyType.Enum:
        int enumIndex = property.enumValueIndex;
        break;
}
```

---

## Defensive Programming (MANDATORY)

PropertyDrawers are especially vulnerable to unexpected states. ALL drawer code MUST follow [defensive-programming](./defensive-programming.md).

### Safe SerializedProperty Access

```csharp
// Safe OnGUI implementation
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
        // Draw property safely...
    }
    finally
    {
        EditorGUI.EndProperty();
    }
}
```

### Cache Invalidation on Target Change

```csharp
private Object _lastTarget;
private SerializedProperty _cachedProperty;

private SerializedProperty GetProperty(SerializedObject so)
{
    if (so == null || so.targetObject == null)
    {
        _cachedProperty = null;
        _lastTarget = null;
        return null;
    }

    if (_lastTarget != so.targetObject)
    {
        _cachedProperty = null;
        _lastTarget = so.targetObject;
    }

    if (_cachedProperty == null)
    {
        _cachedProperty = so.FindProperty("_fieldName");
    }

    return _cachedProperty;
}
```

### Never Throw From Drawer Code

- Return early for null/invalid inputs
- Use `TryXxx` patterns for failable operations
- Log warnings for debugging, don't crash the inspector
- Handle destroyed objects gracefully

---

## Related Skills

- [property-drawer-examples](./property-drawer-examples.md) - Dropdown and foldout drawer examples
- [property-drawer-rules](./property-drawer-rules.md) - Critical rules, multi-object editing, testing
- [create-editor-tool](./create-editor-tool.md) - EditorWindows and Custom Inspectors
- [editor-caching-patterns](./editor-caching-patterns.md) - Editor caching and common patterns
- [defensive-programming](./defensive-programming.md) - General defensive coding practices
- [create-test](./create-test.md) - Test creation guidelines
- [test-odin-drawers](./test-odin-drawers.md) - Odin Inspector drawer testing
