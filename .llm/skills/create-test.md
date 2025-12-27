# Skill: Create Test

**Trigger**: When creating or modifying test files in this repository.

---

## When to Use This Skill

- **After ANY new feature** (MANDATORY)
- **After ANY bug fix** (MANDATORY)
- When adding new public API
- When modifying existing behavior
- When refactoring production code
- When fixing a flaky test

---

## Zero-Flaky Test Policy

**This repository enforces a strict zero-flaky test policy.** Every test failure indicates a real bug—either in production code OR in the test itself. Never "just make tests pass" or ignore failing tests.

When any test fails, you MUST:

1. **Investigate the root cause** before making any code changes
2. **Classify the bug** as production bug or test bug
3. **Implement a comprehensive fix** that addresses the actual problem
4. **Verify the fix is correct** by running the full test suite

See [investigate-test-failures](investigate-test-failures.md) for detailed investigation procedures.

---

## MANDATORY: Exhaustive Testing for All Production Code

**Every new production feature MUST have exhaustive test coverage.** This is NON-NEGOTIABLE. This includes:

- **Runtime code** — New classes, methods, extension methods, data structures
- **Editor tooling** — Property drawers, custom inspectors, editor windows
- **Inspector attributes** — All attribute behaviors must be tested

### Test Coverage Categories (ALL Required)

| Category                  | Requirement                                                                             |
| ------------------------- | --------------------------------------------------------------------------------------- |
| **Normal Cases**          | Cover typical/expected usage scenarios (5-20 elements, common inputs)                   |
| **Negative Cases**        | Invalid inputs, error conditions, exceptions, invalid state transitions                 |
| **Edge Cases**            | Empty collections, single elements, boundary values (0, -1, int.MaxValue)               |
| **Extreme Cases**         | Very large inputs (10K+ elements), maximum values, near-overflow                        |
| **Unexpected Situations** | Null inputs, disposed objects, concurrent access, missing dependencies                  |
| **"The Impossible"**      | Cases that "should never happen" but might (corrupted state, invalid enum values)       |
| **Data-Driven**           | PREFER `[TestCase]` / `[TestCaseSource]` for comprehensive variations with minimal code |

---

## Test File Location

Mirror the source structure:

- `Runtime/Core/Helper/Buffers.cs` → `Tests/Runtime/Core/Helper/BuffersTests.cs`
- `Editor/Tools/SpriteCropper.cs` → `Tests/Editor/Tools/SpriteCropperTests.cs`

### EditMode vs PlayMode Tests

| Test Type    | Location         | Use When                                                                   |
| ------------ | ---------------- | -------------------------------------------------------------------------- |
| **EditMode** | `Tests/Editor/`  | Testing Editor tools, property drawers, inspectors, non-MonoBehaviour code |
| **PlayMode** | `Tests/Runtime/` | Testing MonoBehaviour lifecycle, coroutines, Update loops, physics         |

---

## Test File Template

```csharp
namespace WallstopStudios.UnityHelpers.Tests.{Subsystem}
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core;

    [TestFixture]
    public sealed class MyClassTests
    {
        [Test]
        public void MethodNameReturnsExpectedResultWhenCondition()
        {
            MyClass sut = new MyClass();

            string result = sut.MethodName("input");

            Assert.AreEqual("expected", result);
        }
    }
}
```

---

## Critical Rules

### 1. NO Underscores in Test Method Names

- ✅ `AsyncTaskInvocationCompletesAndRecordsHistory`
- ❌ `AsyncTask_Invocation_Completes_And_Records_History`
- ✅ `WhenInputIsEmptyReturnsDefaultValue`
- ❌ `When_Input_Is_Empty_Returns_Default_Value`

### 2. NEVER Use `#region`

- ❌ `#region Test Setup`
- ❌ `#region Helper Methods`

### 3. One File Per MonoBehaviour/ScriptableObject

Any class deriving from `MonoBehaviour` or `ScriptableObject` MUST be in its own dedicated `.cs` file—even in tests:

- ✅ `TestHelperComponent.cs` containing only `class TestHelperComponent : MonoBehaviour`
- ✅ `TestScriptableObject.cs` containing only `class TestScriptableObject : ScriptableObject`
- ❌ Test helper MonoBehaviours defined inside test class files
- ❌ Test helper ScriptableObjects defined inside test class files
- ❌ Nested classes deriving from MonoBehaviour/ScriptableObject within test classes

**Why**: Unity's serialization and asset system requires these types to be in files matching their class name. Embedded definitions cause editor errors and serialization failures.

#### Test Type File Location

Place test helper types in a dedicated `TestTypes/` folder:

```text
Tests/Editor/
├── TestTypes/                          # Dedicated folder for test helpers
│   ├── SharedTestEnums.cs              # Shared enums used across tests
│   ├── Odin/                           # Optional dependency-specific types
│   │   ├── OdinEnumToggleButtonsTarget.cs
│   │   └── OdinShowIfBoolTarget.cs
│   └── WButton/
│       ├── WButtonTestMonoBehaviour.cs
│       └── WButtonTestScriptableObject.cs
├── CustomDrawers/
│   └── Odin/
│       └── WEnumToggleButtonsOdinDrawerTests.cs  # Test class only
```

#### Shared Test Enums

Extract common test enums to a shared file instead of duplicating:

```csharp
// Tests/Editor/TestTypes/SharedTestEnums.cs
namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    public enum TestModeEnum { ModeA, ModeB, ModeC }

    [Flags]
    public enum TestFlagsEnum { None = 0, Flag1 = 1, Flag2 = 2, All = Flag1 | Flag2 }
}
```

### 4. No `async Task` Test Methods

Unity Test Runner doesn't support them:

- ❌ `public async Task MyAsyncTest()`
- ✅ Use `IEnumerator` with `[UnityTest]` attribute

### 5. No `Assert.ThrowsAsync`

It doesn't exist in Unity's NUnit version.

### 6. Unity Object Null Checks

For `UnityEngine.Object`-derived types:

✅ **CORRECT**:

```csharp
Assert.IsTrue(gameObject != null);
Assert.IsFalse(component == null);
```

❌ **NEVER USE**:

```csharp
Assert.IsNull(gameObject);        // Bypasses Unity's null check
Assert.IsNotNull(component);      // Bypasses Unity's null check
Assert.That(obj, Is.Null);        // Bypasses Unity's null check
Assert.That(obj, Is.Not.Null);    // Bypasses Unity's null check
```

### 7. Self-Documenting Tests (No Comments)

Tests should be self-documenting through descriptive names. Let code speak for itself:

- ❌ `// Arrange`, `// Act`, `// Assert`
- ❌ `// Create the object`, `// Call the method`
- ❌ Line-end comments explaining obvious code
- ❌ Comments describing what the test does (that's what the method name is for)
- ✅ Let test method names describe the scenario
- ✅ Let variable names explain what values represent
- ✅ Use descriptive constant/variable names instead of magic values with comments

```csharp
// ❌ BAD - Comments stating the obvious
[Test]
public void TestMethod()
{
    // Create instance
    MyClass sut = new MyClass();
    int input = 5; // Input value

    // Call method and verify
    int result = sut.Calculate(input);
    Assert.AreEqual(10, result); // Should double the input
}

// ✅ GOOD - Self-documenting
[Test]
public void CalculateReturnsDoubleOfInput()
{
    MyClass sut = new MyClass();
    int inputValue = 5;
    int expectedDoubledValue = 10;

    int result = sut.Calculate(inputValue);

    Assert.AreEqual(expectedDoubledValue, result);
}
```

### 8. No `[Description]` Annotations

Don't use Description attributes on tests.

### 9. Data-Driven Test Naming (CRITICAL)

All data-driven test names must use `.` (dot) separator or PascalCase—**NEVER underscores**:

#### TestCase with TestName

```csharp
// ✅ CORRECT - Dot-separated hierarchy
[TestCase(null, false, TestName = "Input.Null.ReturnsFalse")]
[TestCase("", false, TestName = "Input.Empty.ReturnsFalse")]
[TestCase("valid", true, TestName = "Input.Valid.ReturnsTrue")]

// ✅ CORRECT - PascalCase descriptive
[TestCase(1, TestName = "SingleFolder")]
[TestCase(5, TestName = "MultipleFolders")]

// ❌ WRONG - Underscores
[TestCase(null, TestName = "Input_Null")]
[TestCase("", TestName = "Empty_String_Path")]
```

#### TestCaseSource with SetName

```csharp
// ✅ CORRECT - Dot-separated hierarchy for categorization
private static IEnumerable<TestCaseData> EdgeCasePaths()
{
    yield return new TestCaseData(null, false)
        .SetName("EdgeCase.NullPath.Rejected");
    yield return new TestCaseData("", false)
        .SetName("EdgeCase.EmptyPath.Rejected");
    yield return new TestCaseData("   ", false)
        .SetName("EdgeCase.WhitespacePath.Rejected");
}

// ✅ CORRECT - Category.Scenario.Expected pattern
yield return new TestCaseData("GroupA", "Custom Display A", 3)
    .SetName("DisplayName.FirstFieldHasCustomName.Preserved");
yield return new TestCaseData("GroupD", "GroupD", 2)
    .SetName("DisplayName.NoExplicitName.UsesGroupName");

// ❌ WRONG - Underscores in SetName
yield return new TestCaseData(null).SetName("Null_Path");
yield return new TestCaseData("").SetName("Empty_String");
```

#### Naming Patterns for TestCaseSource

| Pattern                      | Example                                 | When to Use                 |
| ---------------------------- | --------------------------------------- | --------------------------- |
| `Category.Scenario.Expected` | `DisplayName.NoExplicit.UsesDefault`    | Testing behavior variations |
| `Input.Type.Result`          | `Input.Null.ReturnsFalse`               | Input validation tests      |
| `Feature.Condition.Outcome`  | `Serialization.EmptyList.PreservesType` | Feature behavior tests      |
| `PascalCaseDescriptive`      | `SingleElement`                         | Simple enumerations         |

---

## Data-Driven Testing (PREFERRED)

Data-driven tests using `[TestCase]` and `[TestCaseSource]` are **strongly preferred** over multiple individual test methods. Benefits:

- **DRY** — Consolidate test logic, avoid repetition
- **Comprehensive Coverage** — Easy to add new test cases
- **Clear Naming** — SetName makes test intent obvious
- **Maintainability** — Single test method to update

### When to Use `[TestCase]`

Use for simple inline test cases with primitive values:

```csharp
[Test]
[TestCase(null, false, TestName = "Input.Null.ReturnsFalse")]
[TestCase("", false, TestName = "Input.Empty.ReturnsFalse")]
[TestCase("   ", false, TestName = "Input.Whitespace.ReturnsFalse")]
[TestCase("valid", true, TestName = "Input.Valid.ReturnsTrue")]
[TestCase("VALID", true, TestName = "Input.UpperCase.ReturnsTrue")]
[TestCase("a", true, TestName = "Input.SingleChar.ReturnsTrue")]
public void IsValidReturnsExpectedResult(string input, bool expected)
{
    bool result = MyValidator.IsValid(input);

    Assert.AreEqual(expected, result);
}
```

### When to Use `[TestCaseSource]`

Use for complex test data, computed values, or many test cases:

```csharp
[Test]
[TestCaseSource(nameof(EdgeCaseTestData))]
public void ProcessHandlesEdgeCases(int[] input, int expected)
{
    int result = MyProcessor.Process(input);

    Assert.AreEqual(expected, result);
}

private static IEnumerable<TestCaseData> EdgeCaseTestData()
{
    // Empty
    yield return new TestCaseData(Array.Empty<int>(), 0)
        .SetName("Input.Empty.ReturnsZero");

    // Single element
    yield return new TestCaseData(new[] { 42 }, 42)
        .SetName("Input.SingleElement.ReturnsElement");

    // Boundary values
    yield return new TestCaseData(new[] { int.MaxValue }, int.MaxValue)
        .SetName("Input.MaxValue.HandlesCorrectly");
    yield return new TestCaseData(new[] { int.MinValue }, int.MinValue)
        .SetName("Input.MinValue.HandlesCorrectly");
    yield return new TestCaseData(new[] { 0 }, 0)
        .SetName("Input.Zero.ReturnsZero");
    yield return new TestCaseData(new[] { -1 }, -1)
        .SetName("Input.Negative.HandlesCorrectly");

    // Multiple elements
    yield return new TestCaseData(new[] { 1, 2, 3, 4, 5 }, 15)
        .SetName("Input.MultipleElements.SumsCorrectly");

    // Large collection
    yield return new TestCaseData(Enumerable.Range(1, 10000).ToArray(), 50005000)
        .SetName("Input.LargeCollection.HandlesScale");

    // Duplicates
    yield return new TestCaseData(new[] { 5, 5, 5, 5 }, 20)
        .SetName("Input.AllDuplicates.SumsCorrectly");
}
```

### Comprehensive Test Case Template

When creating TestCaseSource data, include ALL these categories:

```csharp
private static IEnumerable<TestCaseData> ComprehensiveTestData()
{
    // === NORMAL CASES ===
    yield return new TestCaseData("normal input", "expected output")
        .SetName("Normal.TypicalInput.ProducesExpected");
    yield return new TestCaseData("another normal", "another expected")
        .SetName("Normal.AlternateInput.ProducesExpected");

    // === EDGE CASES ===
    // Empty
    yield return new TestCaseData("", "default")
        .SetName("Edge.EmptyString.ReturnsDefault");

    // Single element
    yield return new TestCaseData("a", "a")
        .SetName("Edge.SingleChar.Preserved");

    // Boundaries
    yield return new TestCaseData(new string('x', 1), "x")
        .SetName("Edge.MinLength.Handled");
    yield return new TestCaseData(new string('x', 10000), "truncated")
        .SetName("Edge.MaxLength.Truncated");

    // === NEGATIVE CASES ===
    yield return new TestCaseData(null, null)
        .SetName("Negative.NullInput.ReturnsNull");
    yield return new TestCaseData("invalid@#$", "sanitized")
        .SetName("Negative.SpecialChars.Sanitized");

    // === EXTREME CASES ===
    yield return new TestCaseData(new string('a', 100000), "handled")
        .SetName("Extreme.VeryLongInput.Handled");

    // === UNEXPECTED SITUATIONS ===
    yield return new TestCaseData("\0\0\0", "handled")
        .SetName("Unexpected.NullChars.Handled");
    yield return new TestCaseData("   \t\n\r   ", "handled")
        .SetName("Unexpected.OnlyWhitespace.Handled");
}
```

---

## Test Coverage Requirements (MANDATORY)

All production code **MUST** have tests covering the following categories.

### Normal Cases (Required)

Verify expected behavior under typical conditions:

- Standard collection sizes (5-20 elements)
- Common string formats and lengths
- Representative numeric values within normal ranges
- Standard user workflows
- Typical parameter combinations

### Negative Cases (Required)

Verify proper handling of invalid/error conditions:

- Invalid inputs (wrong types, out-of-range values)
- Error conditions (missing dependencies, failed operations)
- Failure scenarios (exceptions, null returns)
- Invalid state transitions
- Missing or misconfigured dependencies

### Edge Cases (Required)

| Category        | Test Scenarios                                              |
| --------------- | ----------------------------------------------------------- |
| Empty inputs    | Empty collections `[]`, empty strings `""`, empty arrays    |
| Single elements | Single-element collections, single character strings        |
| Boundaries      | `int.MaxValue`, `int.MinValue`, `0`, `-1`, first/last index |
| Null values     | Null parameters, null collection elements, null returns     |
| Special strings | Whitespace-only `"   "`, Unicode characters, special chars  |
| Concurrency     | Thread-safe code: parallel access, race conditions          |
| Large inputs    | Stress tests: 10K+ elements, deep nesting                   |
| Type variations | Different generic type parameters, inheritance hierarchies  |

### Extreme Cases (Required)

Test behavior at the limits:

| Category          | Test Scenarios                                            |
| ----------------- | --------------------------------------------------------- |
| Maximum values    | `int.MaxValue`, `float.MaxValue`, `long.MaxValue`         |
| Minimum values    | `int.MinValue`, `float.MinValue`, `long.MinValue`         |
| Near overflow     | `int.MaxValue - 1`, values that cause arithmetic overflow |
| Large collections | 10,000+ elements, deeply nested structures                |
| Long strings      | 100K+ character strings, strings near `string.MaxLength`  |
| Deep recursion    | Deeply nested objects, hierarchies near stack limit       |

### Unexpected Situations (Required)

Test "impossible" scenarios that might still occur:

| Category          | Test Scenarios                                          |
| ----------------- | ------------------------------------------------------- |
| Null inputs       | `null` for every parameter that accepts reference types |
| Disposed objects  | Operations on disposed streams, destroyed Unity objects |
| Invalid state     | Methods called before initialization, after cleanup     |
| Corrupted data    | Malformed JSON, invalid byte sequences, truncated files |
| Concurrent access | Thread contention, race conditions, deadlock potential  |
| Invalid enums     | Cast invalid integers to enum types: `(MyEnum)999`      |

### Inspector/Drawer-Specific Tests (Required for Editor Code)

When testing property drawers, custom inspectors, or editor tools:

| Category             | Test Scenarios                                     |
| -------------------- | -------------------------------------------------- |
| Property types       | All supported `SerializedPropertyType` values      |
| Null targets         | Null `SerializedProperty`, null `SerializedObject` |
| Missing attributes   | Fields without the target attribute                |
| Multi-object editing | Multiple selected objects with different values    |
| Nested properties    | Properties inside arrays, lists, nested classes    |
| Undo/Redo            | State preservation across undo operations          |
| Layout calculations  | Height calculations for varying content            |

### Odin Drawer Testing (When ODIN_INSPECTOR is defined)

When testing Odin `OdinAttributeDrawer` implementations, follow these patterns:

#### Test Target Structure

Test targets must be in separate files under `Tests/Editor/TestTypes/Odin/{Feature}/`:

```text
Tests/Editor/
├── TestTypes/
│   ├── SharedEnums/                        # Shared test enums
│   │   ├── SimpleTestEnum.cs
│   │   ├── TestFlagsEnum.cs
│   │   └── TestModeEnum.cs
│   └── Odin/
│       ├── EnumToggleButtons/              # Per-feature subfolders
│       │   ├── OdinEnumToggleButtonsRegularTarget.cs
│       │   ├── OdinEnumToggleButtonsFlagsTarget.cs
│       │   ├── OdinEnumToggleButtonsMonoBehaviour.cs
│       │   └── OdinEnumToggleButtonsPaginated.cs
│       ├── ShowIf/
│       │   ├── OdinShowIfBoolTarget.cs
│       │   └── OdinShowIfEnumTarget.cs
│       └── InLineEditor/
│           └── OdinInLineEditorTarget.cs
├── CustomDrawers/
│   └── Odin/
│       ├── WEnumToggleButtonsOdinDrawerTests.cs
│       └── WShowIfOdinDrawerTests.cs
```

#### Test Target Template (SerializedScriptableObject)

```csharp
namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.MyFeature
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.SharedEnums;

    /// <summary>
    /// Test target for MyAttribute with SerializedScriptableObject.
    /// </summary>
    internal sealed class OdinMyFeatureTarget : SerializedScriptableObject
    {
        [MyAttribute]
        public SimpleTestEnum myField;
    }
#endif
}
```

#### Test Target Template (SerializedMonoBehaviour)

```csharp
namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.MyFeature
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.SharedEnums;

    /// <summary>
    /// Test target for MyAttribute with SerializedMonoBehaviour.
    /// </summary>
    internal sealed class OdinMyFeatureMonoBehaviour : SerializedMonoBehaviour
    {
        [MyAttribute]
        public SimpleTestEnum myField;
    }
#endif
}
```

#### Odin Drawer Test Class Template

```csharp
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using Sirenix.OdinInspector;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.MyFeature;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.SharedEnums;

    /// <summary>
    /// Tests for MyOdinDrawer ensuring MyAttribute works correctly
    /// with Odin Inspector for SerializedMonoBehaviour/SerializedScriptableObject.
    /// </summary>
    [TestFixture]
    public sealed class MyOdinDrawerTests : CommonTestBase
    {
        [Test]
        public void DrawerRegistrationCreatesEditorForScriptableObject()
        {
            OdinMyFeatureTarget target = CreateScriptableObject<OdinMyFeatureTarget>();
            Editor editor = Editor.CreateEditor(target);
            Track(editor);

            Assert.That(editor, Is.Not.Null, "Editor should be created");
        }

        [Test]
        public void DrawerRegistrationCreatesEditorForMonoBehaviour()
        {
            OdinMyFeatureMonoBehaviour target = NewGameObject("TestMB")
                .AddComponent<OdinMyFeatureMonoBehaviour>();
            Editor editor = Editor.CreateEditor(target);
            Track(editor);

            Assert.That(editor, Is.Not.Null, "Editor should be created for MB");
        }

        [Test]
        public void OnInspectorGuiDoesNotThrowForScriptableObject()
        {
            OdinMyFeatureTarget target = CreateScriptableObject<OdinMyFeatureTarget>();
            Editor editor = Editor.CreateEditor(target);
            Track(editor);

            Assert.DoesNotThrow(() => editor.OnInspectorGUI());
        }

        [Test]
        public void OnInspectorGuiDoesNotThrowForMonoBehaviour()
        {
            OdinMyFeatureMonoBehaviour target = NewGameObject("TestMB")
                .AddComponent<OdinMyFeatureMonoBehaviour>();
            Editor editor = Editor.CreateEditor(target);
            Track(editor);

            Assert.DoesNotThrow(() => editor.OnInspectorGUI());
        }

        [Test]
        public void OnInspectorGuiHandlesMultipleCalls()
        {
            OdinMyFeatureTarget target = CreateScriptableObject<OdinMyFeatureTarget>();
            Editor editor = Editor.CreateEditor(target);
            Track(editor);

            Assert.DoesNotThrow(() =>
            {
                editor.OnInspectorGUI();
                editor.OnInspectorGUI();
                editor.OnInspectorGUI();
            });
        }

        [Test]
        [TestCaseSource(nameof(FieldValueTestCases))]
        public void OnInspectorGuiHandlesVariousFieldValues(SimpleTestEnum value)
        {
            OdinMyFeatureTarget target = CreateScriptableObject<OdinMyFeatureTarget>();
            target.myField = value;
            Editor editor = Editor.CreateEditor(target);
            Track(editor);

            Assert.DoesNotThrow(() => editor.OnInspectorGUI());
        }

        private static IEnumerable<TestCaseData> FieldValueTestCases()
        {
            yield return new TestCaseData(SimpleTestEnum.One)
                .SetName("Value.FirstEnumMember");
            yield return new TestCaseData(SimpleTestEnum.Two)
                .SetName("Value.SecondEnumMember");
            yield return new TestCaseData(SimpleTestEnum.Three)
                .SetName("Value.ThirdEnumMember");
            yield return new TestCaseData((SimpleTestEnum)999)
                .SetName("Value.InvalidEnumValue");
        }
    }
#endif
}
```

#### Required Test Categories for Odin Drawers

| Category                 | Test Scenarios                                          |
| ------------------------ | ------------------------------------------------------- |
| Editor creation          | `Editor.CreateEditor(target)` returns non-null          |
| ScriptableObject targets | `SerializedScriptableObject` base class works correctly |
| MonoBehaviour targets    | `SerializedMonoBehaviour` base class works correctly    |
| No-throw on GUI          | `OnInspectorGUI()` doesn't throw for valid targets      |
| Multiple GUI calls       | Repeated `OnInspectorGUI()` calls don't cause issues    |
| Various field values     | Different enum values, null references, edge cases      |
| Multiple fields          | Multiple attributes on same target work together        |
| Attribute configurations | Different attribute constructor parameters              |
| Caching behavior         | Multiple instances share caches correctly               |
| Editor cleanup           | `DestroyImmediate(editor)` in finally blocks            |

#### Shared Test Enums

Extract common test enums to `Tests/Editor/TestTypes/SharedEnums/`:

```csharp
// Tests/Editor/TestTypes/SharedEnums/SimpleTestEnum.cs
namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.SharedEnums
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    public enum SimpleTestEnum { One, Two, Three }
#endif
}

// Tests/Editor/TestTypes/SharedEnums/TestFlagsEnum.cs
namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.SharedEnums
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using System;

    [Flags]
    public enum TestFlagsEnum
    {
        None = 0,
        Flag1 = 1,
        Flag2 = 2,
        Flag3 = 4,
        All = Flag1 | Flag2 | Flag3
    }
#endif
}
```

#### CommonTestBase Usage for Odin Tests

Always inherit from `CommonTestBase` for automatic cleanup:

```csharp
public sealed class MyOdinDrawerTests : CommonTestBase
{
    // CreateScriptableObject<T>() - Creates and tracks SO for cleanup
    // NewGameObject(name) - Creates and tracks GO for cleanup
    // Track(obj) - Manually track any Unity object for cleanup
}
```

---

## Example: Comprehensive Data-Driven Test Class

```csharp
namespace WallstopStudios.UnityHelpers.Tests.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Extension;

    [TestFixture]
    public sealed class DictionaryExtensionsTests
    {
        [Test]
        public void GetOrAddReturnsExistingValueWhenKeyExists()
        {
            Dictionary<string, int> dictionary = new Dictionary<string, int> { { "key", 42 } };

            int result = dictionary.GetOrAdd("key", () => 999);

            Assert.AreEqual(42, result);
        }

        [Test]
        public void GetOrAddCreatesNewValueWhenKeyMissing()
        {
            Dictionary<string, int> dictionary = new Dictionary<string, int>();

            int result = dictionary.GetOrAdd("key", () => 42);

            Assert.AreEqual(42, result);
            Assert.IsTrue(dictionary.ContainsKey("key"));
        }

        [Test]
        public void GetOrAddWithNullKeyThrowsArgumentNullException()
        {
            Dictionary<string, int> dictionary = new Dictionary<string, int>();

            Assert.Throws<ArgumentNullException>(() => dictionary.GetOrAdd(null, () => 42));
        }

        [Test]
        public void GetOrAddWithNullFactoryThrowsArgumentNullException()
        {
            Dictionary<string, int> dictionary = new Dictionary<string, int>();

            Assert.Throws<ArgumentNullException>(() => dictionary.GetOrAdd("key", null));
        }

        [Test]
        public void GetOrAddWithEmptyDictionaryAddsEntry()
        {
            Dictionary<int, List<string>> dictionary = new Dictionary<int, List<string>>();

            List<string> result = dictionary.GetOrAdd(1);

            Assert.AreEqual(1, dictionary.Count);
            Assert.IsTrue(result != null);
        }

        private static IEnumerable<TestCaseData> KeyValueTestCases()
        {
            yield return new TestCaseData("", 0)
                .SetName("Key.EmptyString.AcceptsValue");
            yield return new TestCaseData("normal", 42)
                .SetName("Key.NormalString.AcceptsValue");
            yield return new TestCaseData("   ", 100)
                .SetName("Key.WhitespaceString.AcceptsValue");
            yield return new TestCaseData("a", int.MaxValue)
                .SetName("Value.MaxInt.Stored");
            yield return new TestCaseData("b", int.MinValue)
                .SetName("Value.MinInt.Stored");
        }

        [Test]
        [TestCaseSource(nameof(KeyValueTestCases))]
        public void GetOrAddStoresCorrectValue(string key, int value)
        {
            Dictionary<string, int> dictionary = new Dictionary<string, int>();

            int result = dictionary.GetOrAdd(key, () => value);

            Assert.AreEqual(value, result);
            Assert.AreEqual(value, dictionary[key]);
        }

        private static IEnumerable<TestCaseData> CollectionSizeTestCases()
        {
            yield return new TestCaseData(0).SetName("Size.Empty");
            yield return new TestCaseData(1).SetName("Size.Single");
            yield return new TestCaseData(10).SetName("Size.Small");
            yield return new TestCaseData(100).SetName("Size.Medium");
            yield return new TestCaseData(10000).SetName("Size.Large");
        }

        [Test]
        [TestCaseSource(nameof(CollectionSizeTestCases))]
        public void GetOrAddWorksWithVariousDictionarySizes(int existingCount)
        {
            Dictionary<int, int> dictionary = new Dictionary<int, int>();
            for (int i = 0; i < existingCount; i++)
            {
                dictionary[i] = i * 2;
            }

            int newKey = existingCount;
            int result = dictionary.GetOrAdd(newKey, () => 999);

            Assert.AreEqual(999, result);
            Assert.AreEqual(existingCount + 1, dictionary.Count);
        }
    }
}
```

---

## Timeouts

For long-running tests, use timeouts defined in `Tests/Runtime/RuntimeTestTimeouts.cs`.

---

## Test Quality Requirements (Prevent Flaky Tests)

All tests MUST be deterministic and isolated. Follow these requirements to prevent flaky tests:

### Determinism

| Anti-Pattern                      | Required Pattern                              |
| --------------------------------- | --------------------------------------------- |
| `DateTime.Now` in assertions      | Inject time provider or use fixed values      |
| `Random` without seed             | Use seeded PRNG: `new PcgRandom(fixedSeed)`   |
| Depending on dictionary/set order | Sort before comparing or use ordered types    |
| Floating-point exact equality     | Use tolerance: `Assert.AreEqual(a, b, 0.001)` |
| Timing-dependent assertions       | Use synchronization or callbacks              |

### Isolation

| Anti-Pattern                       | Required Pattern                            |
| ---------------------------------- | ------------------------------------------- |
| Static mutable state between tests | Reset in `[TearDown]` or use instance state |
| Shared fixtures without reset      | `[SetUp]` creates fresh state each test     |
| Tests affecting each other         | Each test must be completely independent    |
| External system dependencies       | Mock external systems or use test doubles   |

### Unity-Specific

| Anti-Pattern                        | Required Pattern                          |
| ----------------------------------- | ----------------------------------------- |
| Depending on editor selection state | Create test GameObjects in code           |
| Assuming coroutine timing           | Use `WaitUntil` with completion callbacks |
| Frame-count assumptions             | Wait for actual completion signals        |
| Scene-dependent tests               | Create all needed objects in test setup   |

### Cleanup

Always clean up created objects:

```csharp
[TearDown]
public void TearDown()
{
    if (_testGameObject != null)
    {
        Object.DestroyImmediate(_testGameObject);
    }
}
```

---

## Assertion Best Practices

### Prefer Specific Assertions

```csharp
// ✅ GOOD - Specific, clear failure message
Assert.AreEqual(42, result);
Assert.IsTrue(collection.Contains("key"));
Assert.IsInstanceOf<MyException>(ex);
Assert.Throws<ArgumentNullException>(() => Method(null));

// ❌ AVOID - Generic, unclear failure
Assert.That(result == 42);
Assert.IsTrue(result.Equals(42));
```

### Use Meaningful Failure Messages

```csharp
// ✅ GOOD - Helpful failure message
Assert.AreEqual(expected, actual, $"Expected {expected} but got {actual} for input '{input}'");
Assert.IsTrue(isValid, $"Validation failed for input: {input}");

// ❌ AVOID - No context on failure
Assert.AreEqual(expected, actual);
Assert.IsTrue(isValid);
```

### Collection Assertions

```csharp
// ✅ GOOD - Collection-specific assertions
Assert.AreEqual(5, list.Count);
Assert.Contains("expected", list);
CollectionAssert.AreEqual(expected, actual);
CollectionAssert.AreEquivalent(expected, actual); // Order-independent
CollectionAssert.IsEmpty(collection);
CollectionAssert.IsNotEmpty(collection);

// ❌ AVOID - Manual iteration
Assert.IsTrue(list.Count == 5);
Assert.IsTrue(list.Any(x => x == "expected"));
```

### Assert.That with Constraints (For Complex Assertions)

```csharp
// Use for complex or compound assertions
Assert.That(result, Is.GreaterThan(0).And.LessThan(100));
Assert.That(text, Does.StartWith("Hello").And.EndWith("World"));
Assert.That(collection, Has.Count.EqualTo(5));
Assert.That(dictionary, Contains.Key("myKey"));
```

---

## Test Anti-Patterns to AVOID

### ❌ Testing Implementation Details

```csharp
// ❌ WRONG - Tests private method call order
Assert.That(mock.ReceivedCalls().Count, Is.EqualTo(3));

// ✅ CORRECT - Tests observable behavior
Assert.AreEqual("expected output", result);
```

### ❌ Flaky Tests

```csharp
// ❌ WRONG - Timing-dependent
await Task.Delay(100);
Assert.IsTrue(operationCompleted);

// ✅ CORRECT - Explicit synchronization
await taskCompletionSource.Task;
Assert.IsTrue(operationCompleted);
```

### ❌ Tests Depending on External State

```csharp
// ❌ WRONG - Depends on file system
string content = File.ReadAllText("/some/fixed/path");

// ✅ CORRECT - Uses test-controlled data
string content = CreateTestFile("test content");
```

### ❌ Overly Complex Test Setup

```csharp
// ❌ WRONG - 50 lines of setup
[SetUp]
public void SetUp()
{
    // ... 50 lines of complex setup ...
}

// ✅ CORRECT - Factory methods, minimal setup
[Test]
public void TestMethod()
{
    MyClass sut = CreateDefaultSut();
    // test...
}
```

### ❌ Missing Edge Cases

```csharp
// ❌ WRONG - Only tests happy path
[Test]
public void ProcessReturnsResult()
{
    var result = Process("valid input");
    Assert.IsNotNull(result);
}

// ✅ CORRECT - Comprehensive coverage
[Test]
[TestCase(null, TestName = "Input.Null")]
[TestCase("", TestName = "Input.Empty")]
[TestCase("   ", TestName = "Input.Whitespace")]
[TestCase("valid", TestName = "Input.Valid")]
[TestCase("very long string...", TestName = "Input.VeryLong")]
public void ProcessHandlesAllInputTypes(string input) { ... }
```

### ❌ Magic Numbers Without Context

```csharp
// ❌ WRONG - What does 42 mean?
Assert.AreEqual(42, result);

// ✅ CORRECT - Named constant explains intent
const int ExpectedUserCount = 42;
Assert.AreEqual(ExpectedUserCount, result);
```

---

## Test Independence

Every test MUST be completely independent:

1. **No shared state** — Each test creates its own instances
2. **No execution order dependency** — Tests can run in any order
3. **No side effects** — Tests don't modify global state
4. **Clean teardown** — Dispose all resources created during test

```csharp
[TestFixture]
public sealed class IndependentTests
{
    private MyClass _sut;
    private GameObject _testObject;

    [SetUp]
    public void SetUp()
    {
        _sut = new MyClass();
        _testObject = new GameObject("Test");
    }

    [TearDown]
    public void TearDown()
    {
        _sut?.Dispose();
        if (_testObject != null)
        {
            Object.DestroyImmediate(_testObject);
        }
    }
}
```

---

## Performance Considerations

- Tests should run **quickly** (<100ms per test ideally)
- **Mock expensive dependencies** (file I/O, network, databases)
- Use appropriate **timeouts** for async tests
- Avoid unnecessary **allocations** in hot test paths
- Group related tests to share expensive setup via `[OneTimeSetUp]`

```csharp
[TestFixture]
public sealed class PerformanceAwareTests
{
    private static ExpensiveResource _sharedResource;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Expensive setup done once for all tests in class
        _sharedResource = new ExpensiveResource();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _sharedResource?.Dispose();
    }
}
```

---

## Test Creation Checklist

Before submitting tests, verify ALL items:

### Coverage

- [ ] ✅ Normal cases covered (typical usage scenarios)
- [ ] ✅ Negative/error cases covered (invalid inputs, exceptions)
- [ ] ✅ Edge cases covered (empty, single, boundary values)
- [ ] ✅ Extreme cases covered (large inputs, max/min values)
- [ ] ✅ Unexpected situations covered (null, disposed, concurrent)
- [ ] ✅ "The impossible" covered (corrupted state, invalid enums)

### Structure

- [ ] ✅ Data-driven tests used where appropriate (`[TestCase]`/`[TestCaseSource]`)
- [ ] ✅ Tests are completely independent
- [ ] ✅ Test method names follow convention (PascalCase, no underscores)
- [ ] ✅ TestCaseSource names use dot notation (e.g., `Input.Null.ReturnsFalse`)
- [ ] ✅ No comments in test code (self-documenting)

### Quality

- [ ] ✅ No flaky tests (deterministic, isolated)
- [ ] ✅ Meaningful assertion failure messages
- [ ] ✅ Proper cleanup in TearDown
- [ ] ✅ Fast execution (<100ms per test)
- [ ] ✅ No external dependencies

### Technical

- [ ] ✅ Unity null checks use `== null` / `!= null` (not `Is.Null`)
- [ ] ✅ No `async Task` test methods (use `IEnumerator` + `[UnityTest]`)
- [ ] ✅ No `#region` blocks
- [ ] ✅ No `[Description]` attributes
- [ ] ✅ MonoBehaviour/ScriptableObject test helpers in separate files

---

## Post-Creation Steps

1. Generate meta file for test file:

   ```bash
   /workspaces/com.wallstop-studios.unity-helpers/scripts/generate-meta.sh <test-file-path>
   ```

2. Format code with CSharpier:

   ```bash
   dotnet tool run csharpier format <test-file-path>
   ```

3. Ask user to run tests and provide output (do not run Unity CLI yourself)

---

## Quick Reference: Test Case Categories

| Category       | Examples                   | Why Test                       |
| -------------- | -------------------------- | ------------------------------ |
| **Normal**     | `"hello"`, `[1,2,3]`, `42` | Verify basic functionality     |
| **Null**       | `null`, `default`          | Prevent NullReferenceException |
| **Empty**      | `""`, `[]`, `{}`           | Handle degenerate cases        |
| **Single**     | `"a"`, `[1]`               | Off-by-one errors              |
| **Boundary**   | `0`, `-1`, `int.MaxValue`  | Overflow, underflow            |
| **Large**      | 10K+ elements              | Performance, memory            |
| **Invalid**    | `"@#$%"`, `(MyEnum)999`    | Graceful error handling        |
| **Concurrent** | Parallel access            | Thread safety                  |
