# Skill: Test Naming Conventions

<!-- trigger: test-name, naming, underscore, migration, legacy | Test method and TestName naming rules | Core -->

**Trigger**: When naming test methods, TestName values, or migrating legacy tests with naming violations.

---

## When to Use

- Naming new test methods
- Adding `TestName` values to `[TestCase]` attributes
- Using `.SetName()` in `[TestCaseSource]` methods
- Migrating legacy tests with underscore naming
- Fixing lint errors related to test naming (UNH004)

---

## When NOT to Use

- Naming production code (use C# conventions)
- Naming non-test files or classes

---

## Naming Rules Summary

| Element                  | Rule                       | Example                           |
| ------------------------ | -------------------------- | --------------------------------- |
| Test method names        | PascalCase, NO underscores | `ProcessNullInputThrows`          |
| `TestName` values        | Dot notation or PascalCase | `"Input.Null.Throws"`             |
| `.SetName()` values      | Dot notation or PascalCase | `.SetName("Input.Empty.Returns")` |
| `TestCaseSource` methods | PascalCase                 | `EdgeCaseTestData()`              |

---

## Test Method Naming

### Correct Pattern

Use PascalCase without any underscores:

```csharp
// CORRECT
public void AsyncTaskInvocationCompletesAndRecordsHistory() { }
public void WhenInputIsEmptyReturnsDefaultValue() { }
public void ProcessThrowsArgumentNullExceptionForNullInput() { }
public void CalculateReturnsSumOfAllElements() { }
```

### Anti-Patterns

```csharp
// WRONG - Snake_Case
public void AsyncTask_Invocation_Completes_And_Records_History() { }
public void When_Input_Is_Empty_Returns_Default_Value() { }

// WRONG - Mixed underscores
public void Process_NullInput_Throws() { }
public void Calculate_Returns_Sum() { }
```

### Recommended Naming Patterns

| Pattern                     | Example                                 |
| --------------------------- | --------------------------------------- |
| `MethodReturnsXWhenY`       | `ProcessReturnsNullWhenInputIsEmpty`    |
| `MethodThrowsExceptionForX` | `ParseThrowsFormatExceptionForInvalid`  |
| `MethodHandlesXCorrectly`   | `CacheHandlesConcurrentAccessCorrectly` |
| `WhenXThenY`                | `WhenInputIsNullReturnsDefault`         |
| `XIsYWhenZ`                 | `ResultIsEmptyWhenNoMatchesFound`       |

---

## TestName and SetName Values

### Dot Notation (Preferred)

Use hierarchical dot notation for categorization:

```csharp
// TestCase attribute
[TestCase(null, false, TestName = "Input.Null.ReturnsFalse")]
[TestCase("", false, TestName = "Input.Empty.ReturnsFalse")]
[TestCase("valid", true, TestName = "Input.Valid.ReturnsTrue")]

// SetName in TestCaseSource
yield return new TestCaseData(null).SetName("Input.Null.Throws");
yield return new TestCaseData(Array.Empty<int>()).SetName("Input.Empty.ReturnsZero");
yield return new TestCaseData(new[] { 42 }).SetName("Input.Single.ReturnsElement");
```

### PascalCase (Alternative)

For simple cases without hierarchy:

```csharp
[TestCase(1, TestName = "SingleFolder")]
[TestCase(5, TestName = "MultipleFolders")]
[TestCase(100, TestName = "ManyFolders")]
```

### Common Dot Notation Patterns

| Pattern                 | Use Case                          |
| ----------------------- | --------------------------------- |
| `Input.Null.X`          | Null input scenarios              |
| `Input.Empty.X`         | Empty collection/string scenarios |
| `Input.Single.X`        | Single element scenarios          |
| `Input.Large.X`         | Large input scenarios             |
| `Boundary.Zero.X`       | Zero value edge cases             |
| `Boundary.MaxValue.X`   | Maximum value edge cases          |
| `Error.InvalidFormat.X` | Error condition scenarios         |
| `ThreadCount.Four`      | Parameterized thread counts       |

---

## TestCaseSource Method Naming

### Correct Pattern

```csharp
// CORRECT - PascalCase
private static IEnumerable<TestCaseData> EdgeCaseTestData() { }
private static IEnumerable<TestCaseData> NullInputCases() { }
private static IEnumerable<TestCaseData> BoundaryValueTestData() { }
```

### Anti-Patterns

```csharp
// WRONG - Underscore-separated
private static IEnumerable<TestCaseData> Edge_Case_Test_Data() { }
private static IEnumerable<TestCaseData> null_input_cases() { }

// WRONG - lowercase
private static IEnumerable<TestCaseData> edgecasetestdata() { }
```

---

## Pre-commit Hook Enforcement

The pre-commit hook runs `scripts/lint-tests.ps1` and **rejects commits** containing:

- Underscores in test method names
- Underscores in `TestName` values
- Underscores in `SetName()` calls

Commits will fail with specific error messages indicating which files and lines violate the naming convention.

---

## Migrating Legacy Tests

If you encounter existing tests with underscores, follow this migration process:

### Step 1: Identify Violations

Run the linter to find all violations:

```bash
pwsh -NoProfile -File scripts/lint-tests.ps1
```

### Step 2: Rename Test Methods

Convert from Snake_Case to PascalCase:

| Before                             | After                          |
| ---------------------------------- | ------------------------------ |
| `Process_Null_Input_Throws`        | `ProcessNullInputThrows`       |
| `Calculate_Returns_Sum_When_Valid` | `CalculateReturnsSumWhenValid` |
| `When_Input_Is_Empty_Returns_Zero` | `WhenInputIsEmptyReturnsZero`  |

### Step 3: Update TestName Values

Convert to dot notation:

| Before                            | After                                     |
| --------------------------------- | ----------------------------------------- |
| `TestName = "Null_Input_Throws"`  | `TestName = "Input.Null.Throws"`          |
| `TestName = "Empty_String"`       | `TestName = "Input.Empty.String"`         |
| `TestName = "Max_Value_Overflow"` | `TestName = "Boundary.MaxValue.Overflow"` |

### Step 4: Update SetName() Calls

```csharp
// Before
yield return new TestCaseData(null).SetName("Null_Input_Throws");
yield return new TestCaseData(Array.Empty<int>()).SetName("Empty_Array");

// After
yield return new TestCaseData(null).SetName("Input.Null.Throws");
yield return new TestCaseData(Array.Empty<int>()).SetName("Input.Empty.Array");
```

### Step 5: Verify and Test

```bash
# Re-run linter to verify all violations are fixed
pwsh -NoProfile -File scripts/lint-tests.ps1

# Run affected tests to ensure they still pass after renaming
# Use Unity Test Framework or dotnet test
```

### Migration Tips

- **Incremental commits**: When migrating many tests, make incremental commits by file or test fixture to keep changes reviewable
- **Preserve test behavior**: Only change names, not test logic
- **Update any test references**: If test names are referenced elsewhere (CI, documentation), update those too

---

## Validation Checklist

Before committing test changes:

- [ ] Test method names use PascalCase (no underscores)
- [ ] `TestName` values use dot notation or PascalCase
- [ ] `.SetName()` values use dot notation or PascalCase
- [ ] `TestCaseSource` method names use PascalCase
- [ ] Ran `pwsh -NoProfile -File scripts/lint-tests.ps1` with no errors

---

## Related Skills

- [create-test](./create-test.md) — Overall test creation guidance
- [test-data-driven](./test-data-driven.md) — Data-driven testing patterns
- [investigate-test-failures](./investigate-test-failures.md) — Root cause analysis for test failures
- [validate-before-commit](./validate-before-commit.md) — Pre-commit validation workflow
