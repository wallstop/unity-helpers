# Skill: Create Test

**Trigger**: When creating or modifying test files in this repository.

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

### 9. Data-Driven Test Naming

Use `.` (dot) or no delimiter—**never underscores**:

- ✅ `[TestCase("Input.Valid")]`
- ✅ `[TestCase("InputValid")]`
- ❌ `[TestCase("Input_Valid")]`

---

## Test Coverage Requirements

### Positive Cases

Verify expected behavior under normal conditions.

### Negative Cases

Verify proper handling of:

- Invalid inputs
- Error conditions
- Failure scenarios

### Edge Cases

| Category        | Examples                                     |
| --------------- | -------------------------------------------- |
| Empty inputs    | Empty collections, strings, arrays           |
| Single elements | Single-element collections                   |
| Boundaries      | Max/min values, first/last elements, zero    |
| Null values     | Where applicable                             |
| Special strings | Whitespace-only, Unicode, special characters |
| Concurrency     | For thread-safe code                         |
| Large inputs    | Performance stress tests                     |

### Normal Cases

- Typical collection sizes (5-20 elements)
- Common string formats and lengths
- Representative numeric values
- Standard workflows

---

## Example: Comprehensive Test Class

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
        public void GetOrAddWithEmptyDictionaryAddsEntry()
        {
            Dictionary<int, List<string>> dictionary = new Dictionary<int, List<string>>();

            List<string> result = dictionary.GetOrAdd(1);

            Assert.AreEqual(1, dictionary.Count);
            Assert.IsTrue(result != null);
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
