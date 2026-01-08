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

---

## When NOT to Use

- For Odin Inspector drawer testing, see [test-odin-drawers](./test-odin-drawers.md)
- For Unity object lifecycle management in tests, see [test-unity-lifecycle](./test-unity-lifecycle.md)
- For data-driven test patterns, see [test-data-driven](./test-data-driven.md)
- For naming conventions and migration, see [test-naming-conventions](./test-naming-conventions.md)

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

| Category                  | Requirement                                                                              |
| ------------------------- | ---------------------------------------------------------------------------------------- |
| **Normal Cases**          | Cover typical/expected usage scenarios (5-20 elements, common inputs)                    |
| **Negative Cases**        | Invalid inputs, error conditions, exceptions, invalid state transitions                  |
| **Edge Cases**            | Empty collections, single elements, boundary values (0, -1, int.MaxValue)                |
| **Extreme Cases**         | Very large inputs (10K+ elements), maximum values, near-overflow                         |
| **Unexpected Situations** | Null inputs, disposed objects, concurrent access, missing dependencies                   |
| **"The Impossible"**      | Cases that "should never happen" but might (corrupted state, invalid enum values)        |
| **Data-Driven**           | PREFER `[TestCase]` / `[TestCaseSource]` — see [test-data-driven](./test-data-driven.md) |

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

## Critical Rules Summary

For complete naming rules, see [test-naming-conventions](./test-naming-conventions.md).

### Naming

1. **NO underscores in test method names** — Use PascalCase only
2. **Data-driven test names use dot notation** — `TestName = "Input.Null.ReturnsFalse"`

### Code Structure

1. **NEVER use `#region`**
2. **One file per MonoBehaviour/ScriptableObject** — Even in tests
3. **No `async Task` test methods** — Use `IEnumerator` with `[UnityTest]`
4. **No `Assert.ThrowsAsync`** — Not available in Unity's NUnit

### Unity-Specific

1. **Unity object null checks** — Use `== null` / `!= null`, never `Assert.IsNull`

```csharp
// CORRECT
Assert.IsTrue(gameObject != null);
Assert.IsFalse(component == null);

// NEVER USE - bypasses Unity's null check
Assert.IsNull(gameObject);
Assert.IsNotNull(component);
```

### Documentation

1. **Self-documenting tests** — No `// Arrange`, `// Act`, `// Assert` comments
2. **No `[Description]` annotations**
3. **No file paths in docstrings**

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
// GOOD - Specific, clear failure message
Assert.AreEqual(42, result);
Assert.IsTrue(collection.Contains("key"));
Assert.Throws<ArgumentNullException>(() => Method(null));

// AVOID - Generic, unclear failure
Assert.That(result == 42);
```

### Collection Assertions

```csharp
// GOOD - Collection-specific assertions
Assert.AreEqual(5, list.Count);
CollectionAssert.AreEqual(expected, actual);
CollectionAssert.AreEquivalent(expected, actual); // Order-independent
CollectionAssert.IsEmpty(collection);
```

---

## Concurrent Test Patterns

When testing thread-safety, use controlled parallelism:

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
        int captured = i; // Capture loop variable
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

Key points:

- **Capture loop variables** — Always capture `i` to a local variable before using in `Task.Run`
- **Use `Task.WaitAll`** — Ensures all operations complete before assertions
- **Parameterize thread counts** — Use `[TestCase]` for different parallelism levels

---

## Diagnostic Output for Debugging

Use `TestContext.WriteLine` to capture diagnostic information:

```csharp
[Test]
public void CacheEvictionWorks()
{
    var cache = new Cache<int, string>(2);
    cache.Set(1, "a");
    cache.Set(2, "b");
    cache.Set(3, "c");

    TestContext.WriteLine($"Cache count: {cache.Count}");
    TestContext.WriteLine($"Contains key 1: {cache.TryGet(1, out _)}");

    Assert.IsFalse(cache.TryGet(1, out _), "Key 1 should have been evicted");
}
```

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

- [ ] Data-driven tests used where appropriate — see [test-data-driven](./test-data-driven.md)
- [ ] Tests are completely independent
- [ ] Naming follows conventions — see [test-naming-conventions](./test-naming-conventions.md)
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
- [ ] Ran `pwsh -NoProfile -File scripts/lint-tests.ps1` and fixed any issues

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

- [test-data-driven](./test-data-driven.md) — Data-driven testing with TestCase and TestCaseSource
- [test-naming-conventions](./test-naming-conventions.md) — Naming rules and legacy test migration
- [test-odin-drawers](./test-odin-drawers.md) — Odin Inspector drawer testing patterns
- [test-unity-lifecycle](./test-unity-lifecycle.md) — Track(), DestroyImmediate, object management
- [investigate-test-failures](./investigate-test-failures.md) — Root cause analysis for test failures
- [validate-before-commit](./validate-before-commit.md) — Pre-commit validation workflow
