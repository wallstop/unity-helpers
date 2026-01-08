# Skill: Data-Driven Testing

<!-- trigger: testcase, testcasesource, data-driven, parameterized | Data-driven testing with TestCase and TestCaseSource | Core -->

**Trigger**: When writing data-driven tests using `[TestCase]` or `[TestCaseSource]` attributes.

---

## When to Use

- Testing multiple input/output combinations for the same logic
- Covering edge cases, boundary values, and error conditions systematically
- Reducing code duplication across similar test scenarios
- Testing with computed or complex test data

---

## When NOT to Use

- Simple tests with a single scenario (use plain `[Test]` instead)
- Tests that require completely different setup/teardown per case
- When test logic differs significantly between cases

---

## Data-Driven Testing Overview

Data-driven tests using `[TestCase]` and `[TestCaseSource]` are **strongly preferred** in this repository. They provide comprehensive coverage with minimal code duplication.

---

## When to Use `[TestCase]`

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

### Best Practices for `[TestCase]`

| Practice                             | Example                                |
| ------------------------------------ | -------------------------------------- |
| Use dot notation in TestName         | `TestName = "Input.Null.ReturnsFalse"` |
| Group related cases together         | All null cases, then empty, then valid |
| Include expected result as parameter | `[TestCase("input", "expected")]`      |
| Keep inline data simple              | Primitives, strings, small arrays      |

---

## When to Use `[TestCaseSource]`

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

### Best Practices for `[TestCaseSource]`

| Practice                            | Example                                      |
| ----------------------------------- | -------------------------------------------- |
| Use `nameof()` for method reference | `[TestCaseSource(nameof(TestData))]`         |
| PascalCase method names             | `EdgeCaseTestData()`, NOT `edge_case_data()` |
| Use `.SetName()` with dot notation  | `.SetName("Input.Empty.ReturnsZero")`        |
| Return `IEnumerable<TestCaseData>`  | Allows `.SetName()` and other configuration  |

---

## Test Case Naming (CRITICAL)

All data-driven test names must use `.` (dot) separator or PascalCase—**NEVER underscores**.

### Correct Patterns

```csharp
// Dot-separated hierarchy (preferred for categorization)
[TestCase(null, false, TestName = "Input.Null.ReturnsFalse")]
[TestCase("", false, TestName = "Input.Empty.ReturnsFalse")]
[TestCase("valid", true, TestName = "Input.Valid.ReturnsTrue")]

// PascalCase descriptive (good for simple cases)
[TestCase(1, TestName = "SingleFolder")]
[TestCase(5, TestName = "MultipleFolders")]

// SetName with dot notation
yield return new TestCaseData(null).SetName("Input.Null.Throws");
yield return new TestCaseData(Array.Empty<int>()).SetName("Input.Empty.ReturnsZero");
```

### Anti-Patterns to Avoid

```csharp
// WRONG - Underscores in TestName
[TestCase(null, TestName = "Input_Null_Returns_False")]
[TestCase("", TestName = "Empty_String")]

// WRONG - Underscores in SetName()
yield return new TestCaseData(null).SetName("Null_Input_Throws");
yield return new TestCaseData(Array.Empty<int>()).SetName("Empty_Array");

// WRONG - Underscore-separated TestCaseSource method names
private static IEnumerable<TestCaseData> Edge_Case_Test_Data() { }
private static IEnumerable<TestCaseData> null_input_cases() { }
```

### Correct TestCaseSource Method Pattern

```csharp
// CORRECT - PascalCase method names, dot notation in SetName
private static IEnumerable<TestCaseData> EdgeCaseTestData()
{
    yield return new TestCaseData(null).SetName("Input.Null.Throws");
    yield return new TestCaseData(Array.Empty<int>()).SetName("Input.Empty.ReturnsZero");
}
```

---

## Organizing Test Case Categories

Structure your test cases to cover all required categories:

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

### Example: Comprehensive Test Case Coverage

```csharp
[Test]
[TestCaseSource(nameof(ProcessTestCases))]
public void ProcessHandlesAllCases(int[] input, int expected, string scenario)
{
    int result = MyProcessor.Process(input);

    Assert.AreEqual(expected, result, $"Failed for scenario: {scenario}");
}

private static IEnumerable<TestCaseData> ProcessTestCases()
{
    // Normal cases
    yield return new TestCaseData(new[] { 1, 2, 3 }, 6, "normal sum")
        .SetName("Input.Normal.SumsCorrectly");

    // Null/Empty cases
    yield return new TestCaseData(null, 0, "null input")
        .SetName("Input.Null.ReturnsZero");
    yield return new TestCaseData(Array.Empty<int>(), 0, "empty array")
        .SetName("Input.Empty.ReturnsZero");

    // Single element
    yield return new TestCaseData(new[] { 42 }, 42, "single element")
        .SetName("Input.Single.ReturnsElement");

    // Boundary values
    yield return new TestCaseData(new[] { 0 }, 0, "zero")
        .SetName("Input.Boundary.Zero");
    yield return new TestCaseData(new[] { int.MaxValue }, int.MaxValue, "max value")
        .SetName("Input.Boundary.MaxValue");
}
```

---

## Automated Enforcement

**MANDATORY:** After creating or modifying ANY test file, run the test linter:

```bash
pwsh -NoProfile -File scripts/lint-tests.ps1
```

The linter detects naming violations:

| Violation Type                          | Example                     |
| --------------------------------------- | --------------------------- |
| Underscores in test method names        | `Process_Null_Input_Throws` |
| Underscores in `TestName` values        | `TestName = "Null_Input"`   |
| Underscores in `SetName()` calls        | `.SetName("Empty_Array")`   |
| Non-PascalCase `TestCaseSource` methods | `edge_case_data()`          |

**Note:** Pre-commit hooks enforce these rules automatically, but running the linter manually during development catches issues before commit.

---

## Related Skills

- [create-test](./create-test.md) — Overall test creation guidance
- [test-naming-conventions](./test-naming-conventions.md) — Detailed naming rules and migration
- [test-unity-lifecycle](./test-unity-lifecycle.md) — Unity object lifecycle management
- [investigate-test-failures](./investigate-test-failures.md) — Root cause analysis for test failures
