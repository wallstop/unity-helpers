# Skill: Create Test

<!-- trigger: test, testing, nunit, testcase, coverage | Writing or modifying test files | Core -->

**Trigger**: When creating or modifying test files in this repository.

---

## When to Use This Skill

- **After ANY new feature** (MANDATORY)
- **After ANY bug fix** (MANDATORY)
- When adding new public API
- When modifying existing behavior
- When refactoring production code
- When fixing a flaky test

For Odin Inspector drawer testing, see [test-odin-drawers](./test-odin-drawers.md).
For Unity object lifecycle management in tests, see [test-unity-lifecycle](./test-unity-lifecycle.md).

---

## Zero-Flaky Test Policy

**This repository enforces a strict zero-flaky test policy.** Every test failure indicates a real bug—either in production code OR in the test itself.

When any test fails, you MUST:

1. **Investigate the root cause** before making any code changes
2. **Classify the bug** as production bug or test bug
3. **Implement a comprehensive fix** that addresses the actual problem
4. **Verify the fix is correct** by running the full test suite

See [investigate-test-failures](./investigate-test-failures.md) for detailed investigation procedures.

---

## MANDATORY: Exhaustive Testing for All Production Code

**Every new production feature MUST have exhaustive test coverage.** This is NON-NEGOTIABLE.

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

- `AsyncTaskInvocationCompletesAndRecordsHistory`
- `WhenInputIsEmptyReturnsDefaultValue`
- ~~`AsyncTask_Invocation_Completes_And_Records_History`~~
- ~~`When_Input_Is_Empty_Returns_Default_Value`~~

### 2. NEVER Use `#region`

### 3. One File Per MonoBehaviour/ScriptableObject

Any class deriving from `MonoBehaviour` or `ScriptableObject` MUST be in its own dedicated `.cs` file—even in tests. Place test helper types in `Tests/Editor/TestTypes/`.

### 4. No `async Task` Test Methods

Unity Test Runner doesn't support them. Use `IEnumerator` with `[UnityTest]` attribute.

### 5. No `Assert.ThrowsAsync`

It doesn't exist in Unity's NUnit version.

### 6. Unity Object Null Checks

```csharp
// ✅ CORRECT
Assert.IsTrue(gameObject != null);
Assert.IsFalse(component == null);

// ❌ NEVER USE - bypasses Unity's null check
Assert.IsNull(gameObject);
Assert.IsNotNull(component);
Assert.That(obj, Is.Null);
```

### 7. Self-Documenting Tests (No Comments)

Tests should be self-documenting through descriptive names:

- ~~`// Arrange`, `// Act`, `// Assert`~~
- ~~`// Create the object`~~
- Let test method names describe the scenario
- Use descriptive variable names

### 8. No `[Description]` Annotations

### 9. No File Paths in Docstrings

```csharp
// ❌ BAD
/// Tests serialization (Tests/Runtime/Utils/SerializerTests.cs)

// ✅ GOOD
/// Verifies that SerializableDictionary serializes correctly
```

### 10. Data-Driven Test Naming (CRITICAL)

All data-driven test names must use `.` (dot) separator or PascalCase—**NEVER underscores**:

```csharp
// ✅ CORRECT - Dot-separated hierarchy
[TestCase(null, false, TestName = "Input.Null.ReturnsFalse")]
[TestCase("", false, TestName = "Input.Empty.ReturnsFalse")]

// ✅ CORRECT - PascalCase descriptive
[TestCase(1, TestName = "SingleFolder")]

// ❌ WRONG - Underscores
[TestCase(null, TestName = "Input_Null")]
```

---

## Data-Driven Testing (PREFERRED)

Data-driven tests using `[TestCase]` and `[TestCaseSource]` are **strongly preferred**.

### When to Use `[TestCase]`

Use for simple inline test cases with primitive values:

```csharp
[Test]
[TestCase(null, false, TestName = "Input.Null.ReturnsFalse")]
[TestCase("", false, TestName = "Input.Empty.ReturnsFalse")]
[TestCase("valid", true, TestName = "Input.Valid.ReturnsTrue")]
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
    yield return new TestCaseData(Array.Empty<int>(), 0)
        .SetName("Input.Empty.ReturnsZero");
    yield return new TestCaseData(new[] { 42 }, 42)
        .SetName("Input.SingleElement.ReturnsElement");
    yield return new TestCaseData(new[] { int.MaxValue }, int.MaxValue)
        .SetName("Input.MaxValue.HandlesCorrectly");
}
```

---

## Test Quality Requirements (Prevent Flaky Tests)

### Determinism

| Anti-Pattern                      | Required Pattern                              |
| --------------------------------- | --------------------------------------------- |
| `DateTime.Now` in assertions      | Inject time provider or use fixed values      |
| `Random` without seed             | Use seeded PRNG: `new PcgRandom(fixedSeed)`   |
| Depending on dictionary/set order | Sort before comparing or use ordered types    |
| Floating-point exact equality     | Use tolerance: `Assert.AreEqual(a, b, 0.001)` |

### Isolation

| Anti-Pattern                       | Required Pattern                            |
| ---------------------------------- | ------------------------------------------- |
| Static mutable state between tests | Reset in `[TearDown]` or use instance state |
| Shared fixtures without reset      | `[SetUp]` creates fresh state each test     |
| Tests affecting each other         | Each test must be completely independent    |

---

## Assertion Best Practices

### Prefer Specific Assertions

```csharp
// ✅ GOOD - Specific, clear failure message
Assert.AreEqual(42, result);
Assert.IsTrue(collection.Contains("key"));
Assert.Throws<ArgumentNullException>(() => Method(null));

// ❌ AVOID - Generic, unclear failure
Assert.That(result == 42);
```

### Collection Assertions

```csharp
// ✅ GOOD - Collection-specific assertions
Assert.AreEqual(5, list.Count);
CollectionAssert.AreEqual(expected, actual);
CollectionAssert.AreEquivalent(expected, actual); // Order-independent
CollectionAssert.IsEmpty(collection);
```

---

## Concurrent Test Patterns

When testing thread-safety, use controlled parallelism to verify concurrent access behavior. These patterns help identify race conditions and ensure data structures handle parallel operations correctly.

```csharp
[Test]
[TestCase(4, TestName = "ThreadCount.Four")]
[TestCase(8, TestName = "ThreadCount.Eight")]
public void ConcurrentInsertIsThreadSafe(int threadCount)
{
    var cache = new Cache<int, string>(100);
    var tasks = new Task[threadCount];

    for (int i = 0; i < threadCount; i++)
    {
        int captured = i; // Capture loop variable to avoid closure issues
        tasks[i] = Task.Run(() =>
        {
            for (int j = 0; j < 100; j++)
            {
                cache.Set(captured * 100 + j, $"value_{captured}_{j}");
            }
        });
    }

    Task.WaitAll(tasks);
    Assert.AreEqual(threadCount * 100, cache.Count);
}
```

Key points for concurrent tests:

- **Capture loop variables** — Always capture `i` to a local variable (`int captured = i`) before using in `Task.Run`
- **Use `Task.WaitAll`** — Ensures all parallel operations complete before assertions
- **Parameterize thread counts** — Use `[TestCase]` to test with different parallelism levels
- **Verify final state** — Assert on aggregate results after all tasks complete
- **Consider contention** — Test scenarios where multiple threads access the same keys

---

## Diagnostic Output for Debugging

Use `TestContext.WriteLine` to capture diagnostic information without modifying assertions. This output appears in test results, helping diagnose failures without changing test behavior.

```csharp
[Test]
public void CacheEvictionWorks()
{
    var cache = new Cache<int, string>(2);
    cache.Set(1, "a");
    cache.Set(2, "b");
    cache.Set(3, "c"); // Should evict oldest

    // Capture state for diagnostics before assertion
    TestContext.WriteLine($"Cache count: {cache.Count}");
    TestContext.WriteLine($"Contains key 1: {cache.TryGet(1, out _)}");
    TestContext.WriteLine($"Contains key 2: {cache.TryGet(2, out _)}");
    TestContext.WriteLine($"Contains key 3: {cache.TryGet(3, out _)}");

    Assert.IsFalse(cache.TryGet(1, out _), "Key 1 should have been evicted");
}
```

Guidelines for diagnostic output:

- **Never modify assertions** — Diagnostics are for observation only
- **Capture state before assertions** — Log values that will help understand failures
- **Output appears on failure** — In Unity Test Framework, diagnostic output is shown when tests fail
- **Remove after debugging** — Once the issue is resolved, consider removing verbose diagnostics

---

## Test Independence

Every test MUST be completely independent:

1. **No shared state** — Each test creates its own instances
2. **No execution order dependency** — Tests can run in any order
3. **No side effects** — Tests don't modify global state
4. **Clean teardown** — Dispose all resources created during test

---

## Test Creation Checklist

### Coverage

- [ ] Normal cases covered (typical usage scenarios)
- [ ] Negative/error cases covered (invalid inputs, exceptions)
- [ ] Edge cases covered (empty, single, boundary values)
- [ ] Extreme cases covered (large inputs, max/min values)
- [ ] Unexpected situations covered (null, disposed, concurrent)

### Structure

- [ ] Data-driven tests used where appropriate
- [ ] Tests are completely independent
- [ ] Test method names follow convention (PascalCase, no underscores)
- [ ] TestCaseSource names use dot notation
- [ ] No comments in test code (self-documenting)

### Quality

- [ ] No flaky tests (deterministic, isolated)
- [ ] Meaningful assertion failure messages
- [ ] Proper cleanup in TearDown
- [ ] Fast execution (<100ms per test)

### Technical

- [ ] Unity null checks use `== null` / `!= null`
- [ ] No `async Task` test methods
- [ ] No `#region` blocks
- [ ] MonoBehaviour/ScriptableObject helpers in separate files

---

## Post-Creation Steps

1. Generate meta file:

   ```bash
   ./scripts/generate-meta.sh <test-file-path>
   ```

2. Format code:

   ```bash
   dotnet tool run csharpier format <test-file-path>
   ```

3. Run test lifecycle linter:

   ```bash
   pwsh -NoProfile -File scripts/lint-tests.ps1
   ```

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

---

## Related Skills

- [test-odin-drawers](./test-odin-drawers.md) — Odin Inspector drawer testing patterns
- [test-unity-lifecycle](./test-unity-lifecycle.md) — Track(), DestroyImmediate, object management
- [investigate-test-failures](./investigate-test-failures.md) — Root cause analysis for test failures
- [validate-before-commit](./validate-before-commit.md) — Pre-commit validation workflow
