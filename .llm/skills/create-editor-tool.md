# Skill: Create Editor Tool/Window

**Trigger**: When creating Unity Editor GUI tools, windows, custom inspectors, or property drawers in this repository.

---

## File Location Rules

All editor tools MUST go in the `Editor/` folder tree:

- Editor windows → `Editor/` or `Editor/Tools/`
- Property drawers → `Editor/CustomDrawers/`
- Custom inspectors → `Editor/CustomEditors/`
- Style loaders → `Editor/Styles/`

> ⚠️ Editor code cannot reference Runtime code unless via assembly definition references.

---

## EditorWindow Template

```csharp
namespace WallstopStudios.UnityHelpers.Editor.Tools
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public sealed class MyToolWindow : EditorWindow
    {
        private static bool SuppressUserPrompts { get; set; }

        static MyToolWindow()
        {
            try
            {
                if (Application.isBatchMode || IsInvokedByTestRunner())
                {
                    SuppressUserPrompts = true;
                }
            }
            catch { }
        }

        private static bool IsInvokedByTestRunner()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; ++i)
            {
                string a = args[i];
                if (
                    a.IndexOf("runTests", StringComparison.OrdinalIgnoreCase) >= 0
                    || a.IndexOf("testResults", StringComparison.OrdinalIgnoreCase) >= 0
                    || a.IndexOf("testPlatform", StringComparison.OrdinalIgnoreCase) >= 0
                )
                {
                    return true;
                }
            }
            return false;
        }

        [SerializeField]
        internal List<Object> _targetPaths = new();

        private Vector2 _scrollPosition;
        private SerializedObject _serializedObject;
        private SerializedProperty _targetPathsProperty;

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/My Tool", priority = -1)]
        public static void ShowWindow()
        {
            GetWindow<MyToolWindow>("My Tool");
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _targetPathsProperty = _serializedObject.FindProperty(nameof(_targetPaths));
        }

        private void OnGUI()
        {
            _serializedObject.Update();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            try
            {
                EditorGUILayout.LabelField("My Tool Settings", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(_targetPathsProperty, new GUIContent("Target Paths"));

                EditorGUILayout.Space();
                if (GUILayout.Button("Run", GUILayout.Height(30)))
                {
                    RunTool();
                }
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }

            _serializedObject.ApplyModifiedProperties();
        }

        private void RunTool()
        {
            // Implementation
        }
    }
#endif
}
```

### Key EditorWindow Patterns

1. **Test Runner Detection**: Include `SuppressUserPrompts` and `IsInvokedByTestRunner()` for batch mode compatibility
2. **SerializedObject for Window State**: Use `SerializedObject(this)` and `SerializedProperty` for persistent window state
3. **Scroll View**: Wrap content in `EditorGUILayout.BeginScrollView` / `EndScrollView`
4. **Menu Registration**: Use `[MenuItem("Tools/Wallstop Studios/Unity Helpers/...")]` with `priority = -1`

---

## PropertyDrawer Template

```csharp
namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
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
            System.StringComparer.Ordinal
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

### Key PropertyDrawer Patterns

1. **Caching**: Use static `Dictionary` caches for height calculations—drawers run every frame
2. **Reusable GUIContent**: Create static `GUIContent` instances to reduce allocations
3. **BeginProperty/EndProperty**: Always wrap drawing in `EditorGUI.BeginProperty` / `EndProperty`
4. **Height Calculation**: Override `GetPropertyHeight` if adding custom elements

---

## Custom Inspector (Editor) Template

```csharp
namespace WallstopStudios.UnityHelpers.Editor.CustomEditors
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Utils;

    [CustomEditor(typeof(MyComponent))]
    [CanEditMultipleObjects]
    public sealed class MyComponentEditor : Editor
    {
        private static readonly WallstopGenericPool<
            Dictionary<string, SerializedProperty>
        > PropertyLookupPool = new(
            () => new Dictionary<string, SerializedProperty>(16, StringComparer.Ordinal),
            onRelease: d => d.Clear()
        );

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            // Draw default script field (disabled)
            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            if (scriptProperty != null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(scriptProperty, true);
                }
                EditorGUILayout.Space();
            }

            // Draw custom properties
            SerializedProperty myProperty = serializedObject.FindProperty("_myField");
            if (myProperty != null)
            {
                EditorGUILayout.PropertyField(myProperty);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
```

### Key Custom Inspector Patterns

1. **Multi-Object Editing**: Include `[CanEditMultipleObjects]` for selection support
2. **Script Field**: Draw `m_Script` property disabled at top
3. **Property Pooling**: Use `WallstopGenericPool` for dictionary caches in performance-sensitive inspectors
4. **Update/Apply Cycle**: Always call `UpdateIfRequiredOrScript()` and `ApplyModifiedProperties()`

---

## Caching Requirements (CRITICAL)

Inspectors and drawers run **every frame**. Minimize allocations:

```csharp
// ✅ CORRECT - Static caches
private static readonly Dictionary<string, float> HeightCache = new(StringComparer.Ordinal);
private static readonly GUIContent ReusableContent = new();

// ✅ CORRECT - Pool expensive objects
private static readonly WallstopGenericPool<List<string>> ListPool = new(
    () => new List<string>(),
    onRelease: list => list.Clear()
);

// ❌ INCORRECT - Allocates every frame
public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
{
    GUIContent content = new GUIContent("Label"); // Allocation!
    List<string> items = new List<string>();       // Allocation!
}
```

### Shared Caches (DRY Principle)

When multiple drawers/inspectors need the same cache, use `EditorCacheHelper`:

```csharp
// ✅ CORRECT - Use shared cache helper
using WallstopStudios.UnityHelpers.Editor.Core.Helper;

// In any drawer:
string display = EditorCacheHelper.GetCachedIntString(index);
string pageLabel = EditorCacheHelper.GetPaginationLabel(page, total);
Texture2D solidTex = EditorCacheHelper.GetSolidTexture(color);

// ❌ INCORRECT - Duplicating caches across multiple files
// Drawer1.cs
private static readonly Dictionary<int, string> IntToStringCache = new();

// Drawer2.cs (duplicate!)
private static readonly Dictionary<int, string> IntToStringCache = new();
```

Available shared caches in `EditorCacheHelper`:

- `GetCachedIntString(int)` — Cached integer-to-string conversion
- `GetPaginationLabel(int page, int total)` — Cached "Page X / Y" strings
- `GetSolidTexture(Color)` — Cached 1x1 solid color textures

---

## USS/UXML Styling

This repository uses USS stylesheets for UI Toolkit elements. Load styles via `WDropDownStyleLoader` pattern:

```csharp
namespace WallstopStudios.UnityHelpers.Editor.Styles
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Core.Helper;

    public static class MyStyleLoader
    {
        private const string StylesRelativePath = "Editor/Styles/MyComponent/";
        private const string StylesFileName = "MyStyles.uss";

        private static StyleSheet _stylesStyleSheet;
        private static bool _initialized;

        public static StyleSheet Styles
        {
            get
            {
                EnsureInitialized();
                return _stylesStyleSheet;
            }
        }

        public static bool IsProSkin => EditorGUIUtility.isProSkin;

        public static void ApplyStyles(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            EnsureInitialized();

            if (_stylesStyleSheet != null)
            {
                element.styleSheets.Add(_stylesStyleSheet);
            }
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            string stylesPath = DirectoryHelper.GetPackagePath(StylesRelativePath + StylesFileName);
            _stylesStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(stylesPath);
        }
    }
#endif
}
```

USS files go in `Editor/Styles/` with `.uss` extension.

---

## SerializedProperty and SerializedObject

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

## Common Patterns

### Progress Bar for Long Operations

```csharp
private void ProcessAssets(List<string> assetPaths)
{
    int total = assetPaths.Count;
    try
    {
        for (int i = 0; i < total; i++)
        {
            string path = assetPaths[i];

            if (EditorUtility.DisplayCancelableProgressBar(
                "Processing Assets",
                $"Processing {Path.GetFileName(path)} ({i + 1}/{total})",
                (float)i / total))
            {
                break; // User cancelled
            }

            ProcessSingleAsset(path);
        }
    }
    finally
    {
        EditorUtility.ClearProgressBar();
    }
}
```

### Undo Support

```csharp
// Record object before modification
Undo.RecordObject(targetObject, "Descriptive Undo Name");
targetObject.someField = newValue;

// For multiple objects
Undo.RecordObjects(serializedObject.targetObjects, "Change Multiple");

// Group multiple operations
Undo.SetCurrentGroupName("Complex Operation");
int undoGroup = Undo.GetCurrentGroup();
// ... multiple changes ...
Undo.CollapseUndoOperations(undoGroup);

// Register newly created objects
GameObject newObj = new GameObject("Created Object");
Undo.RegisterCreatedObjectUndo(newObj, "Create Object");
```

### AssetDatabase Refresh

```csharp
// After modifying assets on disk
AssetDatabase.Refresh();

// With import options
AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

// Import specific asset
AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

// Save all assets
AssetDatabase.SaveAssets();
```

### Drag and Drop

```csharp
private void HandleDragAndDrop(Rect dropArea)
{
    Event evt = Event.current;

    switch (evt.type)
    {
        case EventType.DragUpdated:
        case EventType.DragPerform:
        {
            if (!dropArea.Contains(evt.mousePosition))
            {
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                foreach (Object draggedObject in DragAndDrop.objectReferences)
                {
                    string path = AssetDatabase.GetAssetPath(draggedObject);
                    // Process dropped object
                }
            }

            evt.Use();
            break;
        }
    }
}
```

### User Dialogs (with Suppression)

```csharp
private bool ConfirmAction(string message)
{
    if (SuppressUserPrompts)
    {
        return true; // Auto-confirm in batch/test mode
    }

    return EditorUtility.DisplayDialog(
        "Confirm Action",
        message,
        "Yes",
        "No"
    );
}

private string SelectFolder()
{
    if (SuppressUserPrompts)
    {
        return null;
    }

    return EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
}
```

---

## Critical Rules

### 1. `#if UNITY_EDITOR` Wrapping

Wrap all editor code after namespace declaration:

```csharp
namespace WallstopStudios.UnityHelpers.Editor.Tools
{
#if UNITY_EDITOR
    // All code here
#endif
}
```

### 2. `using` Directives INSIDE Namespace

```csharp
namespace WallstopStudios.UnityHelpers.Editor.Tools
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
// ✅ CORRECT
if (targetObject != null)
{
    // Use targetObject
}

// ❌ INCORRECT
if (targetObject?.name != null) // Bypasses Unity null check
```

### 5. Sealed Classes

Editor tools should be `sealed` unless designed for inheritance:

```csharp
public sealed class MyToolWindow : EditorWindow { }
```

---

## Defensive Programming (MANDATORY)

Editor code is especially vulnerable to unexpected states. ALL editor code MUST follow [defensive-programming](defensive-programming.md).

### Editor-Specific Defensive Patterns

```csharp
// ✅ Safe SerializedProperty access
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

// ✅ Safe asset operations
public static T LoadAssetSafe<T>(string path) where T : Object
{
    if (string.IsNullOrEmpty(path))
    {
        return null;
    }
    return AssetDatabase.LoadAssetAtPath<T>(path);
}

// ✅ Cache invalidation on target change
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

### Never Throw From Editor Code

- Return early for null/invalid inputs
- Use `TryXxx` patterns for failable operations
- Log warnings for debugging, don't crash the inspector
- Handle destroyed objects gracefully

---

## Post-Creation Steps (MANDATORY)

1. **Generate meta file** (required — do not skip):

   ```bash
   ./scripts/generate-meta.sh <path-to-file.cs>
   ```

   > ⚠️ See [create-unity-meta](create-unity-meta.md) for full details.

2. **Format code**:

   ```bash
   dotnet tool run csharpier format .
   ```

3. **Verify no errors**:
   - Check IDE for compilation errors
   - Ensure `WallstopStudios.UnityHelpers.Editor.asmdef` reference is correct

---

## File Naming Conventions

| Type           | Naming                      | Example                          |
| -------------- | --------------------------- | -------------------------------- |
| EditorWindow   | `{Name}Window.cs`           | `FitTextureSizeWindow.cs`        |
| Tool Window    | `{Name}Tool.cs`             | `ImageBlurTool.cs`               |
| PropertyDrawer | `{Attribute}Drawer.cs`      | `WNotNullPropertyDrawer.cs`      |
| Custom Editor  | `{Component}Editor.cs`      | `MatchColliderToSpriteEditor.cs` |
| Style Loader   | `{Component}StyleLoader.cs` | `WDropDownStyleLoader.cs`        |

---

## Testing Editor Tools (MANDATORY)

**All editor tools, property drawers, and custom inspectors MUST have exhaustive tests.** Create tests in `Tests/Editor/` mirroring the source structure.

See [create-test](create-test.md) for full testing guidelines.

### Required Test Coverage

| Category           | Test Scenarios                                      |
| ------------------ | --------------------------------------------------- |
| **Normal Cases**   | Typical usage, valid inputs, expected workflows     |
| **Negative Cases** | Invalid inputs, null values, missing dependencies   |
| **Edge Cases**     | Empty data, boundary values, unusual configurations |
| **Property Types** | All supported `SerializedPropertyType` values       |
| **Null Targets**   | Null `SerializedProperty`, null `SerializedObject`  |
| **Multi-Object**   | Multiple selected objects with different values     |

### Example: Data-Driven Editor Tests

```csharp
namespace WallstopStudios.UnityHelpers.Tests.Editor.Tools
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEditor;
    using WallstopStudios.UnityHelpers.Editor.Tools;

    [TestFixture]
    public sealed class MyToolWindowTests
    {
        [Test]
        public void ShowWindowCreatesWindowInstance()
        {
            MyToolWindow window = EditorWindow.GetWindow<MyToolWindow>();

            Assert.IsTrue(window != null);

            window.Close();
        }

        private static IEnumerable<TestCaseData> InvalidInputTestCases()
        {
            yield return new TestCaseData(null).SetName("Input.Null.Handled");
            yield return new TestCaseData("").SetName("Input.Empty.Handled");
            yield return new TestCaseData("   ").SetName("Input.Whitespace.Handled");
        }

        [Test]
        [TestCaseSource(nameof(InvalidInputTestCases))]
        public void ProcessInputHandlesInvalidValues(string input)
        {
            MyToolWindow window = EditorWindow.GetWindow<MyToolWindow>();

            bool result = window.ProcessInput(input);

            Assert.IsFalse(result);
            window.Close();
        }
    }
}
```
