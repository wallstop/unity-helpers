# Skill: Integrate Optional Dependency

**Trigger**: When adding support for optional packages (Odin Inspector, VContainer, Zenject, Reflex, etc.) to this repository.

---

## Overview

This skill covers patterns for integrating with optional third-party packages that may or may not be installed in a project. The key principle is: **the package should work without any optional dependency, but enhance functionality when one is present**.

---

## Core Patterns

### 1. Conditional Compilation Structure

All optional dependency code should be wrapped in conditional compilation directives **inside** the namespace:

✅ **CORRECT**:

```csharp
namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using System;
    using Sirenix.OdinInspector.Editor;
    using UnityEngine;

    public sealed class MyOdinDrawer : OdinAttributeDrawer<MyAttribute>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            // Implementation
        }
    }
#endif
}
```

❌ **INCORRECT** (directive outside namespace):

```csharp
#if UNITY_EDITOR && ODIN_INSPECTOR
namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
    using System;

    public sealed class MyOdinDrawer : OdinAttributeDrawer<MyAttribute>
    {
        // Implementation
    }
}
#endif
```

### 2. File Organization

Each class integrating with an optional dependency should be in its own file:

```text
Editor/CustomDrawers/
├── Odin/                              # Odin-specific drawers
│   ├── IntDropDownOdinDrawer.cs
│   ├── WEnumToggleButtonsOdinDrawer.cs
│   └── WShowIfOdinDrawer.cs
├── IntDropDownDrawer.cs               # Standard Unity drawers
├── WEnumToggleButtonsDrawer.cs
└── WShowIfDrawer.cs
```

### 3. Shared Logic Extraction

When both standard Unity and optional dependency implementations share logic, extract it to a helper class:

✅ **CORRECT**:

```csharp
// WButtonOdinInspectorHelper.cs - Shared logic
namespace WallstopStudios.UnityHelpers.Editor.CustomEditors
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    internal static class WButtonOdinInspectorHelper
    {
        internal static void DrawInspectorGUI(Editor editor, /* params */)
        {
            // Shared implementation
        }
    }
#endif
}

// WButtonOdinMonoBehaviourInspector.cs - Thin wrapper
namespace WallstopStudios.UnityHelpers.Editor.CustomEditors
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    [CustomEditor(typeof(SerializedMonoBehaviour), true)]
    public sealed class WButtonOdinMonoBehaviourInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            WButtonOdinInspectorHelper.DrawInspectorGUI(this, /* params */);
        }
    }
#endif
}
```

❌ **INCORRECT** (duplicate code across classes):

```csharp
// DON'T copy-paste the same 200 lines into multiple inspector classes
```

### 4. Common Cache Extraction

Caches used across multiple drawers/inspectors should be centralized:

✅ **CORRECT**:

```csharp
// EditorCacheHelper.cs - Shared caching
public static class EditorCacheHelper
{
    private static readonly Dictionary<int, string> IntToStringCache = new();

    public static string GetCachedIntString(int value)
    {
        return IntToStringCache.GetOrAdd(value, v => v.ToString());
    }
}

// In drawers:
string display = EditorCacheHelper.GetCachedIntString(index);
```

❌ **INCORRECT** (duplicate caches):

```csharp
// Drawer1.cs
private static readonly Dictionary<int, string> IntToStringCache = new();

// Drawer2.cs
private static readonly Dictionary<int, string> IntToStringCache = new(); // Duplicate!

// Drawer3.cs
private static readonly Dictionary<int, string> IntToStringCache = new(); // Duplicate!
```

---

## Supported Optional Dependencies

| Package        | Define Symbol    | File Location                                         |
| -------------- | ---------------- | ----------------------------------------------------- |
| Odin Inspector | `ODIN_INSPECTOR` | `Editor/CustomDrawers/Odin/`, `Editor/CustomEditors/` |
| VContainer     | `VCONTAINER`     | `Runtime/Integrations/VContainer/`                    |
| Zenject        | `ZENJECT`        | `Runtime/Integrations/Zenject/`                       |
| Reflex         | `REFLEX`         | `Runtime/Integrations/Reflex/`                        |

---

## Testing Optional Dependencies

### Test File Organization

For each optional dependency integration, create tests that:

1. Use the same conditional compilation
2. Are located in `Tests/Editor/CustomDrawers/Odin/` or similar
3. Have test types in separate files under `Tests/Editor/TestTypes/Odin/`

### Test Type Extraction

Test helper MonoBehaviours and ScriptableObjects **MUST** be in separate files:

✅ **CORRECT**:

```text
Tests/Editor/
├── TestTypes/
│   └── Odin/
│       ├── OdinEnumToggleButtonsTarget.cs      # Each test SO/MB in own file
│       ├── OdinEnumToggleButtonsMonoBehaviour.cs
│       ├── OdinShowIfBoolTarget.cs
│       └── ...
├── CustomDrawers/
│   └── Odin/
│       ├── WEnumToggleButtonsOdinDrawerTests.cs  # Test class only
│       └── WShowIfOdinDrawerTests.cs
```

❌ **INCORRECT** (embedded test types):

```csharp
// WEnumToggleButtonsOdinDrawerTests.cs
[TestFixture]
public sealed class WEnumToggleButtonsOdinDrawerTests
{
    [Test]
    public void TestSomething() { }

    // ❌ WRONG - These should be in separate files!
    private sealed class OdinEnumToggleButtonsTarget : SerializedScriptableObject
    {
        public TestEnum testField;
    }

    private enum TestEnum { A, B, C }  // ❌ WRONG - Should be shared
}
```

### Shared Test Enums

Common enums used across multiple test files should be centralized:

```csharp
// Tests/Editor/TestTypes/SharedTestEnums.cs
namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    public enum TestModeEnum { ModeA, ModeB, ModeC }

    [Flags]
    public enum TestFlagsEnum { None = 0, Flag1 = 1, Flag2 = 2, Flag3 = 4 }

    public enum SmallTestEnum { One, Two, Three }
}
```

---

## Checklist for New Optional Dependency Integration

1. [ ] Create dedicated folder: `Editor/CustomDrawers/{DependencyName}/`
2. [ ] Place `#if` directives **inside** namespace
3. [ ] One class per file
4. [ ] Extract shared logic to helper classes
5. [ ] Extract shared caches to `EditorCacheHelper` or similar
6. [ ] Create test folder: `Tests/Editor/CustomDrawers/{DependencyName}/`
7. [ ] Create test types folder: `Tests/Editor/TestTypes/{DependencyName}/`
8. [ ] Extract all test MonoBehaviours/ScriptableObjects to separate files
9. [ ] Share test enums in `SharedTestEnums.cs`
10. [ ] Generate `.meta` files for all new files

---

## Anti-Patterns to Avoid

| Anti-Pattern              | Why It's Wrong                               | Correct Approach        |
| ------------------------- | -------------------------------------------- | ----------------------- |
| `#if` outside namespace   | Inconsistent with project style              | Put inside namespace    |
| Multiple classes per file | Hard to navigate, Unity serialization issues | One class per file      |
| Duplicate caches          | Memory waste, maintenance burden             | Centralize in helper    |
| Duplicate shared logic    | DRY violation, bug divergence                | Extract to helper class |
| Embedded test types       | Unity serialization errors                   | Separate files          |
| Duplicate test enums      | Maintenance burden                           | Shared enum file        |

---

## Odin Inspector Integration (Detailed)

### Odin Drawer Types

| Drawer Type            | Use Case                               | Base Class                                |
| ---------------------- | -------------------------------------- | ----------------------------------------- |
| Attribute Drawer       | Drawers triggered by custom attributes | `OdinAttributeDrawer<TAttribute>`         |
| Typed Attribute Drawer | Attribute + value type constraint      | `OdinAttributeDrawer<TAttribute, TValue>` |
| Value Drawer           | Drawers for specific types             | `OdinValueDrawer<TValue>`                 |

### Odin Drawer Lifecycle

```csharp
namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector.Editor;
    using UnityEngine;

    public sealed class MyOdinDrawer : OdinAttributeDrawer<MyAttribute>
    {
        /// <summary>
        /// Called once when the drawer is created. Use for one-time setup.
        /// </summary>
        protected override void Initialize()
        {
            // Cache expensive computations here
        }

        /// <summary>
        /// Main draw method. Called every frame while inspector is visible.
        /// </summary>
        protected override void DrawPropertyLayout(GUIContent label)
        {
            // Access property value: this.ValueEntry.SmartValue
            // Access property info: this.Property
            // Call next drawer: this.CallNextDrawer(label)
        }

        /// <summary>
        /// Optional: Override to filter which properties this drawer handles.
        /// </summary>
        protected override bool CanDrawProperty(InspectorProperty property)
        {
            return base.CanDrawProperty(property);
        }
    }
#endif
}
```

### Property Tree Navigation

```csharp
// Access current property's value
object value = this.Property.ValueEntry.WeakSmartValue;
T typedValue = this.ValueEntry.SmartValue;

// Access parent property
InspectorProperty parent = this.Property.Parent;
object parentValue = parent?.ValueEntry?.WeakSmartValue;

// Access child properties
foreach (InspectorProperty child in this.Property.Children)
{
    child.Draw(child.Label);
}

// Find sibling by name
InspectorProperty sibling = this.Property.Parent?.Children[memberName];
```

### Common Sirenix Namespace Imports

```csharp
using Sirenix.OdinInspector;                // Attributes
using Sirenix.OdinInspector.Editor;         // Drawers, editors
using Sirenix.Serialization;                // Serialization
using Sirenix.Utilities;                    // Utilities
using Sirenix.Utilities.Editor;             // Editor utilities
```

### Odin vs Unity Property Access Comparison

| Operation      | Unity PropertyDrawer                | Odin AttributeDrawer              |
| -------------- | ----------------------------------- | --------------------------------- |
| Get value      | `property.objectReferenceValue`     | `this.ValueEntry.SmartValue`      |
| Set value      | `property.objectReferenceValue = x` | `this.ValueEntry.SmartValue = x`  |
| Get parent     | Reflection required                 | `this.Property.Parent`            |
| Get field info | `fieldInfo` parameter               | `this.Property.Info.MemberInfo`   |
| Disable GUI    | `EditorGUI.BeginDisabledGroup()`    | `GUIHelper.PushGUIEnabled(false)` |
| Draw property  | `EditorGUI.PropertyField()`         | `this.CallNextDrawer(label)`      |
| Get attribute  | `attribute` field                   | `this.Attribute`                  |

### Advanced Property Tree Navigation

```csharp
// Navigate to parent's value (e.g., to access sibling members for conditions)
InspectorProperty parent = this.Property.Parent;
object parentValue = parent?.ValueEntry?.WeakSmartValue;

// Access sibling property by name (useful for WShowIf, conditions)
string conditionMember = this.Attribute.conditionMember;
InspectorProperty sibling = parent?.Children.Get(conditionMember);
object conditionValue = sibling?.ValueEntry?.WeakSmartValue;

// Resolve member via reflection fallback when property tree fails
Type parentType = parentValue?.GetType();
MemberInfo memberInfo = parentType?.GetField(conditionMember, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

// Draw children (for inline/nested editors)
foreach (InspectorProperty child in this.Property.Children)
{
    child.Draw(child.Label);
}

// Check if property has Odin drawer chain
bool hasNextDrawer = this.Property.GetNextDrawer() != null;
```

### Common Sirenix Namespace Imports Reference

```csharp
// Core attributes (runtime)
using Sirenix.OdinInspector;                // [Button], [ShowIf], [Required], etc.

// Drawer development (editor only)
using Sirenix.OdinInspector.Editor;         // OdinAttributeDrawer, InspectorProperty
using Sirenix.OdinInspector.Editor.ValueResolvers;  // ValueResolver for member evaluation
using Sirenix.Utilities;                    // TypeExtensions, MemberFinder
using Sirenix.Utilities.Editor;             // GUIHelper, SirenixEditorGUI

// Serialization (for SerializedMonoBehaviour/SerializedScriptableObject)
using Sirenix.Serialization;                // OdinSerializeAttribute, ISerializationCallbackReceiver
```

### Testing `OdinAttributeDrawer` Implementations

```csharp
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using NUnit.Framework;
    using Sirenix.OdinInspector;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.MyFeature;

    [TestFixture]
    public sealed class MyOdinDrawerTests : CommonTestBase
    {
        [Test]
        public void DrawerRegistrationCreatesEditorForScriptableObject()
        {
            MyOdinTestTarget target = CreateScriptableObject<MyOdinTestTarget>();
            Editor editor = Editor.CreateEditor(target);
            Track(editor);

            Assert.IsTrue(editor != null);
        }

        [Test]
        public void DrawerRegistrationCreatesEditorForMonoBehaviour()
        {
            MyOdinTestMonoBehaviour target = NewGameObject("Test")
                .AddComponent<MyOdinTestMonoBehaviour>();
            Editor editor = Editor.CreateEditor(target);
            Track(editor);

            Assert.IsTrue(editor != null);
        }

        [Test]
        public void OnInspectorGuiDoesNotThrowForValidTarget()
        {
            MyOdinTestTarget target = CreateScriptableObject<MyOdinTestTarget>();
            Editor editor = Editor.CreateEditor(target);
            Track(editor);

            Assert.DoesNotThrow(() => editor.OnInspectorGUI());
        }

        [Test]
        public void OnInspectorGuiHandlesDefaultValues()
        {
            MyOdinTestTarget target = CreateScriptableObject<MyOdinTestTarget>();
            Editor editor = Editor.CreateEditor(target);
            Track(editor);

            Assert.DoesNotThrow(() =>
            {
                editor.OnInspectorGUI();
                editor.OnInspectorGUI();  // Multiple calls to test caching
            });
        }

        [Test]
        public void DrawerHandlesMultipleFieldsOnSameTarget()
        {
            MyOdinMultipleFieldsTarget target = CreateScriptableObject<MyOdinMultipleFieldsTarget>();
            Editor editor = Editor.CreateEditor(target);
            Track(editor);

            Assert.DoesNotThrow(() => editor.OnInspectorGUI());
        }
    }
#endif
}
```

### Consolidating Odin and Non-Odin Drawer Logic

When both implementations share significant logic, create utility classes:

✅ **CORRECT** (shared utility):

```csharp
// Editor/CustomDrawers/Utils/ShowIfConditionEvaluator.cs
namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers.Utils
{
    /// <summary>
    /// Shared condition evaluation logic for WShowIf drawers.
    /// Used by both Unity PropertyDrawer and Odin AttributeDrawer.
    /// </summary>
    public static class ShowIfConditionEvaluator
    {
        public static bool EvaluateCondition(
            object conditionValue,
            object[] expectedValues,
            WShowIfOperator op,
            bool inverse)
        {
            // Shared implementation
        }
    }
}

// Editor/CustomDrawers/Odin/WShowIfOdinDrawer.cs
protected override void DrawPropertyLayout(GUIContent label)
{
    object conditionValue = GetConditionValue(); // Odin-specific
    bool show = ShowIfConditionEvaluator.EvaluateCondition(
        conditionValue, Attribute.expectedValues, Attribute.op, Attribute.inverse);
    if (show) CallNextDrawer(label);
}

// Editor/CustomDrawers/WShowIfPropertyDrawer.cs
public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
{
    object conditionValue = GetConditionValue(property); // Unity-specific
    bool show = ShowIfConditionEvaluator.EvaluateCondition(
        conditionValue, attribute.expectedValues, attribute.op, attribute.inverse);
    if (show) EditorGUI.PropertyField(position, property, label);
}
```

❌ **INCORRECT** (duplicated logic):

```csharp
// DON'T copy 200 lines of condition evaluation into both drawers!
```

### Testing Odin Drawers

```csharp
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using NUnit.Framework;
    using Sirenix.OdinInspector;
    using UnityEditor;
    using UnityEngine;

    [TestFixture]
    public sealed class MyOdinDrawerTests : CommonTestBase
    {
        [Test]
        public void Inspector_OnInspectorGUI_DoesNotThrow()
        {
            // Create test target (from separate file)
            OdinTestTarget target = ScriptableObject.CreateInstance<OdinTestTarget>();
            TrackObject(target);

            // Create editor
            Editor editor = Editor.CreateEditor(target);
            TrackObject(editor);

            // Test doesn't throw
            Assert.DoesNotThrow(() => editor.OnInspectorGUI());
        }

        [Test]
        public void Inspector_SerializedMonoBehaviour_CreatesCorrectEditorType()
        {
            GameObject go = new GameObject("Test");
            TrackObject(go);
            OdinMonoBehaviourTarget component = go.AddComponent<OdinMonoBehaviourTarget>();

            Editor editor = Editor.CreateEditor(component);
            TrackObject(editor);

            // Verify Odin inspector is used
            Assert.That(editor.GetType().Name, Does.Contain("Odin"));
        }
    }
#endif
}
```

### Shared Utility Folder Structure

Based on the completed Odin consolidation (Sessions 22-26), the actual structure is:

```text
Editor/CustomDrawers/
├── Utils/                               # Shared utilities (ALWAYS #if UNITY_EDITOR)
│   ├── EnumToggleButtonsShared.cs       # Enum button logic (952 lines)
│   ├── ShowIfConditionEvaluator.cs      # Condition evaluation (586 lines)
│   ├── InLineEditorShared.cs            # Inline editor state (587 lines)
│   ├── DropDownShared.cs                # Dropdown rendering (643 lines)
│   └── ValidationShared.cs              # WNotNull/ValidateAssignment helpers (542 lines)
Editor/Core/Helper/
│   └── EditorCacheHelper.cs             # Centralized style/string/texture caching
├── Odin/                                # Odin-specific drawers (#if ODIN_INSPECTOR)
│   ├── WEnumToggleButtonsOdinDrawer.cs
│   ├── WShowIfOdinDrawer.cs
│   ├── WInLineEditorOdinDrawer.cs
│   ├── WValueDropDownOdinDrawer.cs
│   ├── IntDropDownOdinDrawer.cs
│   ├── StringInListOdinDrawer.cs
│   ├── WNotNullOdinDrawer.cs
│   ├── ValidateAssignmentOdinDrawer.cs
│   ├── WReadOnlyOdinDrawer.cs
│   └── ...
├── WEnumToggleButtonsPropertyDrawer.cs  # Standard Unity drawers
├── WShowIfPropertyDrawer.cs
└── ...
```

### Shared Utility Design Principles

1. **Pure Logic Only**: Shared utilities contain only data structures, constants, and pure helper methods
2. **No GUI Drawing**: Actual drawing stays in drawer classes (different APIs between Unity/Odin)
3. **State Management**: State dictionaries (foldouts, scroll positions, pagination) are centralized
4. **Type Aliases**: Odin drawers use type aliases for cleaner imports:

```csharp
using EnumShared = WallstopStudios.UnityHelpers.Editor.CustomDrawers.Utils.EnumToggleButtonsShared;
using CacheHelper = WallstopStudios.UnityHelpers.Editor.Core.Helper.EditorCacheHelper;
```

### Example: EnumToggleButtonsShared Usage

```csharp
// In Odin drawer (WEnumToggleButtonsOdinDrawer.cs)
protected override void DrawPropertyLayout(GUIContent label)
{
    EnumShared.ToggleOption[] options = EnumShared.BuildToggleOptions(valueType);
    ulong currentMask = EnumShared.ConvertToUInt64(this.ValueEntry.SmartValue);
    EnumShared.SelectionSummary summary = EnumShared.BuildSelectionSummary(
        options, currentMask, isFlags, startIndex, visibleCount, usePagination
    );
    // Odin-specific rendering...
}

// In Unity drawer (WEnumToggleButtonsPropertyDrawer.cs)
public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
{
    EnumShared.ToggleOption[] options = EnumShared.BuildToggleOptions(enumType);
    ulong currentMask = EnumShared.ConvertToUInt64(property.enumValueFlag);
    EnumShared.SelectionSummary summary = EnumShared.BuildSelectionSummary(
        options, currentMask, isFlags, startIndex, visibleCount, usePagination
    );
    // Unity-specific rendering...
}
```
