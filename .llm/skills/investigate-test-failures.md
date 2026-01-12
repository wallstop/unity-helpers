# Skill: Investigate Test Failures

<!-- trigger: test, fail, failure, timeout, flaky | ANY test failure - investigate before fixing | Core -->

**Trigger**: When ANY test fails, times out, or behaves inconsistently.

---

## Zero-Flaky Test Policy (MANDATORY)

**This repository enforces a strict zero-flaky test policy.** Every test failure is treated as a real bug that requires comprehensive investigation and resolution.

### Core Principle

> **A test failure ALWAYS indicates a bug—either in production code OR in the test itself. Both require full investigation and proper fixes.**

### What This Means

| Forbidden Action                               | Required Action                                      |
| ---------------------------------------------- | ---------------------------------------------------- |
| "Make the test pass" without understanding why | Investigate root cause before any code changes       |
| Ignore intermittent failures                   | Treat as highest priority—flaky tests mask real bugs |
| Disable or skip failing tests                  | Fix the underlying issue in production or test code  |
| Add retry logic to hide flakiness              | Eliminate the source of non-determinism              |
| Assume "it works on my machine"                | Reproduce and fix environment-specific issues        |
| Blame external factors (timing, resources)     | Design tests to be deterministic and isolated        |

---

## Investigation Process

### Step 1: Reproduce and Understand

Before making ANY code changes:

1. **Read the full error message** — Stack traces, assertion messages, expected vs actual values
2. **Understand the test's intent** — What behavior is being verified?
3. **Identify the failure pattern** — Consistent failure? Intermittent? Environment-specific?
4. **Check recent changes** — Did a recent commit introduce this failure?

### Adding Diagnostic Logging

When investigating failures, add diagnostic output to understand state WITHOUT modifying assertions. This is critical for preserving test intent while gathering information.

```csharp
// WRONG: Modifying assertion while investigating
Assert.AreEqual(5, result); // Changed from 10 to 5 to make test pass

// CORRECT: Add logging without changing assertion
TestContext.WriteLine($"Input values: {string.Join(", ", inputs)}");
TestContext.WriteLine($"Intermediate state: {processor.State}");
TestContext.WriteLine($"Actual result: {result}");
Assert.AreEqual(10, result); // Keep original assertion unchanged
```

**Diagnostic Logging Rules:**

1. **NEVER modify assertions while investigating** — The original assertion defines expected behavior
2. **Add `TestContext.WriteLine` to capture state** — Log values at the failure point
3. **Include all relevant context** — Collection contents, input values, timing info, intermediate state
4. **Remove diagnostic logging after fix** — Once root cause is identified and fixed, clean up verbose output

Example of comprehensive diagnostic logging:

```csharp
[Test]
public void CacheEvictionFollowsLruPolicy()
{
    var cache = new Cache<int, string>(maxSize: 3);
    cache.Set(1, "a");
    cache.Set(2, "b");
    cache.Set(3, "c");
    _ = cache.Get(1); // Access key 1 to make it recently used
    cache.Set(4, "d"); // Should evict key 2 (least recently used)

    // Diagnostic logging for investigation
    TestContext.WriteLine($"Cache count: {cache.Count}");
    TestContext.WriteLine($"Keys present: {string.Join(", ", cache.Keys)}");
    TestContext.WriteLine($"Key 1 present: {cache.ContainsKey(1)}");
    TestContext.WriteLine($"Key 2 present: {cache.ContainsKey(2)}");

    Assert.IsFalse(cache.ContainsKey(2), "Key 2 should have been evicted as LRU");
}
```

### Step 2: Classify the Bug

Every test failure falls into one of two categories:

#### Production Bug

The test correctly identifies broken behavior in production code.

**Signs:**

- Test assertion accurately describes expected behavior
- Production code doesn't match documented/intended behavior
- Edge case not handled in production code
- Regression from recent changes

**Resolution:** Fix the production code, keep the test unchanged.

#### Test Bug

The test itself is flawed—either in its assertions, setup, or design.

**Signs:**

- Test makes incorrect assumptions about expected behavior
- Test has race conditions or timing dependencies
- Test doesn't properly isolate from external state
- Test setup is incomplete or incorrect
- Test assertions are too strict or too loose

**Resolution:** Fix the test to correctly verify intended behavior.

### Step 3: Implement Proper Fix

#### For Production Bugs

1. Understand the intended behavior from documentation, interfaces, or design
2. Write the minimal fix that corrects the behavior
3. Verify the fix addresses the root cause, not just symptoms
4. Consider if additional tests are needed for related edge cases

#### For Test Bugs

1. Understand what behavior the test SHOULD verify
2. Fix the test to correctly verify that behavior
3. Ensure the test is deterministic and isolated
4. Verify the test fails when production code is broken (test the test)

---

## Common Test Bug Categories

### Non-Deterministic Tests

**Problem:** Tests pass/fail unpredictably.

| Anti-Pattern                  | Correct Pattern                               |
| ----------------------------- | --------------------------------------------- |
| `DateTime.Now` in assertions  | Inject `ITimeProvider` or use fixed values    |
| `Random` without seed         | Use seeded PRNG: `new PcgRandom(fixedSeed)`   |
| Depending on dictionary order | Use ordered collections or sort before assert |
| Timing-dependent assertions   | Use synchronization primitives or callbacks   |
| Floating-point exact equality | Use tolerance: `Assert.AreEqual(a, b, 0.001)` |

### State Leakage

**Problem:** Tests affect each other.

| Anti-Pattern                       | Correct Pattern                             |
| ---------------------------------- | ------------------------------------------- |
| Static mutable state               | Reset in `[TearDown]` or use instance state |
| Shared test fixtures without reset | `[SetUp]` creates fresh state each test     |
| Global singletons                  | Use test-specific instances                 |
| File system side effects           | Use temp directories, clean up in teardown  |

### Brittle Assertions

**Problem:** Tests break from unrelated changes.

| Anti-Pattern                                         | Correct Pattern                              |
| ---------------------------------------------------- | -------------------------------------------- |
| Asserting on `ToString()` format                     | Assert on semantic properties                |
| Exact string matching                                | Contains/Regex for format-independent checks |
| Asserting collection order when order doesn't matter | Use `CollectionAssert.AreEquivalent`         |
| Over-specifying mock interactions                    | Verify only essential interactions           |

### Missing Isolation

**Problem:** Tests depend on external systems.

| Anti-Pattern             | Correct Pattern                        |
| ------------------------ | -------------------------------------- |
| Real file system access  | Mock file operations or use temp files |
| Network calls            | Mock HTTP clients                      |
| Database dependencies    | In-memory test databases or mocks      |
| Unity scene dependencies | Create test GameObjects in code        |

---

## Unity-Specific Test Issues

### Editor State

```csharp
// ❌ Test depends on editor selection state
[Test]
public void BrokenTest()
{
    // Fails if nothing selected in editor
    GameObject selected = Selection.activeGameObject;
}

// ✅ Test creates its own controlled state
[Test]
public void CorrectTest()
{
    GameObject testObject = new GameObject("TestObject");
    try
    {
        // Test with controlled object
    }
    finally
    {
        Object.DestroyImmediate(testObject);
    }
}
```

### Async Operations

```csharp
// ❌ Race condition—coroutine may not complete
[UnityTest]
public IEnumerator BrokenAsyncTest()
{
    StartSomeCoroutine();
    // Immediate assertion without waiting
    Assert.IsTrue(operationComplete);
}

// ✅ Properly wait for completion
[UnityTest]
public IEnumerator CorrectAsyncTest()
{
    bool completed = false;
    StartCoroutine(DoOperation(() => completed = true));

    yield return new WaitUntil(() => completed);

    Assert.IsTrue(operationComplete);
}
```

### Frame-Dependent Logic

```csharp
// ❌ Assumes operation completes in one frame
[UnityTest]
public IEnumerator BrokenFrameTest()
{
    TriggerAnimation();
    yield return null; // Only one frame
    Assert.IsTrue(animationComplete); // May still be running
}

// ✅ Wait for actual completion signal
[UnityTest]
public IEnumerator CorrectFrameTest()
{
    bool animationDone = false;
    TriggerAnimation(onComplete: () => animationDone = true);

    yield return new WaitUntil(() => animationDone);

    Assert.IsTrue(animationComplete);
}
```

---

## Investigation Checklist

Use this checklist for every test failure:

```markdown
### Test Failure Investigation: [TestName]

- [ ] Read complete error message and stack trace
- [ ] Understand what behavior the test verifies
- [ ] Reproduce failure consistently (or identify intermittent pattern)
- [ ] Classify: Production bug or Test bug?

#### If Production Bug:

- [ ] Identify the incorrect production behavior
- [ ] Determine root cause (not just symptoms)
- [ ] Implement minimal fix to production code
- [ ] Verify test passes with fix
- [ ] Consider additional edge case tests

#### If Test Bug:

- [ ] Identify the test defect (assertion, setup, isolation, determinism)
- [ ] Fix test to correctly verify intended behavior
- [ ] Verify test fails when it should (mutation testing)
- [ ] Ensure test is deterministic across runs

#### Final Verification:

- [ ] Run full test suite to check for regressions
- [ ] Confirm no new flakiness introduced
```

---

## Red Flags Requiring Deep Investigation

These patterns indicate systemic issues requiring thorough analysis:

| Red Flag                                 | Indicates                                    |
| ---------------------------------------- | -------------------------------------------- |
| Test passes locally, fails in CI         | Environment dependency or race condition     |
| Test fails on first run, passes on retry | Static state leakage or initialization order |
| Multiple unrelated tests fail together   | Shared state corruption                      |
| Test fails only with other tests         | Test isolation violation                     |
| Test fails near resource limits          | Memory leak or resource exhaustion           |
| Test fails at specific times             | Time-dependent logic or timezone issues      |

---

## Documentation Requirements

When fixing test failures, document:

1. **Root cause** — What was actually broken (production or test)?
2. **Fix approach** — Why this fix addresses the root cause
3. **Prevention** — How similar issues can be avoided

For significant fixes, update relevant documentation or add code comments explaining non-obvious design decisions.

---

## Summary

**Never "just make tests pass."** Every test failure is a signal that requires:

1. Full investigation to understand root cause
2. Classification as production bug or test bug
3. Comprehensive fix addressing the actual problem
4. Verification that the fix is correct and complete

Tests are production code. Treat them with the same rigor.

---

## Related Skills

- [create-test](./create-test.md) — General test creation guidelines
- [test-data-driven](./test-data-driven.md) — Data-driven testing with TestCase and TestCaseSource
- [test-naming-conventions](./test-naming-conventions.md) — Naming rules and legacy test migration
- [test-unity-lifecycle](./test-unity-lifecycle.md) — Track(), DestroyImmediate, object management
- [validate-before-commit](./validate-before-commit.md) — Pre-commit validation workflow
