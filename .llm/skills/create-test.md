# Skill: Create Test

**Trigger**: When creating or modifying test files in this repository.

---

## MANDATORY: Exhaustive Testing for All Production Code

**Every new production feature MUST have exhaustive test coverage.** This includes:

- **Runtime code** — New classes, methods, extension methods, data structures
- **Editor tooling** — Property drawers, custom inspectors, editor windows
- **Inspector attributes** — All attribute behaviors must be tested

### Test Requirements

| Category           | Requirement                                             |
| ------------------ | ------------------------------------------------------- |
| **Normal Cases**   | Cover typical/expected usage scenarios                  |
| **Negative Cases** | Invalid inputs, error conditions, null values           |
| **Edge Cases**     | Empty, single-element, boundary values, max/min         |
| **Data-Driven**    | Prefer `[TestCase]` / `[TestCaseSource]` for variations |

---

## Test File Location

Mirror the source structure:

- `Runtime/Core/Helper/Buffers.cs` → `Tests/Runtime/Core/Helper/BuffersTests.cs`
- `Editor/Tools/SpriteCropper.cs` → `Tests/Editor/Tools/SpriteCropperTests.cs`

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

### 7. NO Comments in Tests

- ❌ `// Arrange`, `// Act`, `// Assert`
- ❌ `// Create the object`, `// Call the method`
- ❌ Line-end comments explaining obvious code
- ✅ Let test method names describe the scenario
- ✅ Let variable names explain what values represent

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
        // === NORMAL CASES ===

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

        // === NEGATIVE CASES ===

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

        // === EDGE CASES ===

        [Test]
        public void GetOrAddWithEmptyDictionaryAddsEntry()
        {
            Dictionary<int, List<string>> dictionary = new Dictionary<int, List<string>>();

            List<string> result = dictionary.GetOrAdd(1);

            Assert.AreEqual(1, dictionary.Count);
            Assert.IsTrue(result != null);
        }

        // === DATA-DRIVEN TESTS ===

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

## Post-Creation Steps

1. Generate meta file for test file
2. Format code with CSharpier
3. Ask user to run tests and provide output (do not run Unity CLI yourself)
