# Skill: Create Editor Tool/Window

<!-- trigger: editor, window, inspector, tool | Creating Editor windows and inspectors | Core -->

**Trigger**: When creating Unity Editor GUI tools, windows, or custom inspectors in this repository.

> For PropertyDrawers, see [create-property-drawer](./create-property-drawer.md).
> For caching patterns, see [editor-caching-patterns](./editor-caching-patterns.md).

---

## File Location Rules

All editor tools MUST go in the `Editor/` folder tree:

- Editor windows: `Editor/` or `Editor/Tools/`
- Property drawers: `Editor/CustomDrawers/`
- Custom inspectors: `Editor/CustomEditors/`
- Style loaders: `Editor/Styles/`

> Editor code cannot reference Runtime code unless via assembly definition references.

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

Inspectors run **every frame**. See [editor-caching-patterns](./editor-caching-patterns.md) for complete guidance.

Quick reference:

```csharp
// CORRECT - Static caches
private static readonly Dictionary<string, float> HeightCache = new(StringComparer.Ordinal);
private static readonly GUIContent ReusableContent = new();

// CORRECT - Use shared cache helper
using WallstopStudios.UnityHelpers.Editor.Core.Helper;
string display = EditorCacheHelper.GetCachedIntString(index);

// INCORRECT - Allocates every frame
public override void OnInspectorGUI()
{
    GUIContent content = new GUIContent("Label"); // Allocation!
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
// CORRECT
if (targetObject != null)
{
    // Use targetObject
}

// INCORRECT
if (targetObject?.name != null) // Bypasses Unity null check
```

### 5. Sealed Classes

Editor tools should be `sealed` unless designed for inheritance:

```csharp
public sealed class MyToolWindow : EditorWindow { }
```

---

## Defensive Programming (MANDATORY)

Editor code is especially vulnerable to unexpected states. ALL editor code MUST follow [defensive-programming](./defensive-programming.md).

### Editor-Specific Defensive Patterns

```csharp
// Safe SerializedProperty access
public override void OnInspectorGUI()
{
    if (serializedObject == null || serializedObject.targetObject == null)
    {
        EditorGUILayout.HelpBox("Target object is missing.", MessageType.Warning);
        return;
    }

    serializedObject.UpdateIfRequiredOrScript();

    SerializedProperty prop = serializedObject.FindProperty("_myField");
    if (prop != null)
    {
        EditorGUILayout.PropertyField(prop);
    }

    serializedObject.ApplyModifiedProperties();
}

// Safe asset operations
public static T LoadAssetSafe<T>(string path) where T : Object
{
    if (string.IsNullOrEmpty(path))
    {
        return null;
    }
    return AssetDatabase.LoadAssetAtPath<T>(path);
}
```

### Never Throw From Editor Code

- Return early for null/invalid inputs
- Use `TryXxx` patterns for failable operations
- Log warnings for debugging, don't crash the inspector
- Handle destroyed objects gracefully

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

4. **Update documentation** (MANDATORY for user-facing tools):
   - Add CHANGELOG entry in `### Added` section
   - Document the tool in `docs/features/editor-tools/`
   - Add XML documentation (`///`) on public API
   - Include screenshots for UI-based tools
   - See [update-documentation](./update-documentation.md) for standards

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

**All editor tools and custom inspectors MUST have exhaustive tests.** Create tests in `Tests/Editor/` mirroring the source structure.

See [create-test](./create-test.md) for full testing guidelines.

### Required Test Coverage

| Category           | Test Scenarios                                      |
| ------------------ | --------------------------------------------------- |
| **Normal Cases**   | Typical usage, valid inputs, expected workflows     |
| **Negative Cases** | Invalid inputs, null values, missing dependencies   |
| **Edge Cases**     | Empty data, boundary values, unusual configurations |
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

---

## Related Skills

- [update-documentation](./update-documentation.md) — **MANDATORY** after creating user-facing tools
- [create-property-drawer](./create-property-drawer.md) — PropertyDrawer creation guide
- [editor-caching-patterns](./editor-caching-patterns.md) — Caching and common editor patterns
- [defensive-programming](./defensive-programming.md) — General defensive coding practices
- [create-test](./create-test.md) — Test creation guidelines
- [test-odin-drawers](./test-odin-drawers.md) — Odin Inspector drawer testing
