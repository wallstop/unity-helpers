# Skill: Create Property Drawer

<!-- trigger: drawer, property drawer, attribute drawer | Creating PropertyDrawers for custom attributes | Core -->

**Trigger**: When creating Unity PropertyDrawers for custom attributes or field types in this repository.

---

## File Location

All PropertyDrawers MUST go in `Editor/CustomDrawers/`.

> Editor code cannot reference Runtime code unless via assembly definition references.

---

## PropertyDrawer Template

```csharp
namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [CustomPropertyDrawer(typeof(MyAttribute))]
    public sealed class MyAttributePropertyDrawer : PropertyDrawer
    {
        private const float HelpBoxPadding = 2f;

        private static readonly Dictionary<string, float> HeightCache = new(
            StringComparer.Ordinal
        );
        private static readonly GUIContent ReusableContent = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float baseHeight = EditorGUI.GetPropertyHeight(property, label, true);
            // Add additional height for custom elements
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

        // Optional: UI Toolkit support
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement container = new();
            container.style.flexDirection = FlexDirection.Column;

            PropertyField propertyField = new(property);
            propertyField.label = property.displayName;

            container.Add(propertyField);
            return container;
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

## Drawer with Dropdown Selection

For drawers that display a dropdown of options:

```csharp
[CustomPropertyDrawer(typeof(MySelectableAttribute))]
public sealed class MySelectablePropertyDrawer : PropertyDrawer
{
    private static readonly string[] Options = { "Option A", "Option B", "Option C" };
    private static readonly GUIContent ReusableContent = new();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property == null || property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        EditorGUI.BeginProperty(position, label, property);
        try
        {
            int currentIndex = Array.IndexOf(Options, property.stringValue);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, Options);
            if (newIndex != currentIndex && newIndex >= 0 && newIndex < Options.Length)
            {
                property.stringValue = Options[newIndex];
            }
        }
        finally
        {
            EditorGUI.EndProperty();
        }
    }
}
```

---

## Drawer with Foldout Section

For complex drawers with expandable sections:

```csharp
[CustomPropertyDrawer(typeof(MyComplexType))]
public sealed class MyComplexTypeDrawer : PropertyDrawer
{
    private const float LineHeight = 18f;
    private const float Spacing = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
        {
            return LineHeight;
        }

        int lineCount = 1; // Foldout
        lineCount += 3;    // Three child properties
        return lineCount * (LineHeight + Spacing);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        try
        {
            Rect foldoutRect = new(position.x, position.y, position.width, LineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                float y = position.y + LineHeight + Spacing;
                DrawChildProperty(ref y, position.width, property, "childField1");
                DrawChildProperty(ref y, position.width, property, "childField2");
                DrawChildProperty(ref y, position.width, property, "childField3");

                EditorGUI.indentLevel--;
            }
        }
        finally
        {
            EditorGUI.EndProperty();
        }
    }

    private void DrawChildProperty(ref float y, float width, SerializedProperty parent, string childName)
    {
        SerializedProperty child = parent.FindPropertyRelative(childName);
        if (child != null)
        {
            Rect rect = new(0, y, width, LineHeight);
            rect = EditorGUI.IndentedRect(rect);
            EditorGUI.PropertyField(rect, child);
            y += LineHeight + Spacing;
        }
    }
}
```

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

- [create-editor-tool](./create-editor-tool.md) - EditorWindows and Custom Inspectors
- [editor-caching-patterns](./editor-caching-patterns.md) - Editor caching and common patterns
- [defensive-programming](./defensive-programming.md) - General defensive coding practices
- [create-test](./create-test.md) - Test creation guidelines
- [test-odin-drawers](./test-odin-drawers.md) - Odin Inspector drawer testing
