# Testing "Impossible" State Patterns

This guide documents patterns for testing states that "should never happen" but could occur in production. These tests catch edge cases that defensive programming must handle gracefully.

## Why Test "Impossible" States

Production code encounters situations that seem impossible during development:

- **Destroyed Unity Objects**: Objects destroyed by external code, scene unloading, or domain reloads
- **Null References**: References that "can't be null" become null due to serialization issues, race conditions, or user error
- **Invalid Enum Values**: Casting arbitrary integers to enums produces values not defined in the enum
- **Corrupted Serialization State**: Save files edited by users, version mismatches, or truncated data
- **Overflow Conditions**: Extreme values that exceed expected ranges

Testing these scenarios ensures code fails gracefully rather than crashing or corrupting data.

## Destroyed Unity Objects

Unity objects can be destroyed at any time by external systems. Code must handle the "fake null" state where an object reference is not `null` in C# terms but returns `true` for Unity's null check.

### Pattern: Test Behavior After DestroyImmediate

```csharp
[Test]
public void GetGameObjectHandlesDestroyedComponent()
{
    GameObject go = Track(new GameObject("Test", typeof(SpriteRenderer)));
    SpriteRenderer spriteRenderer = go.GetComponent<SpriteRenderer>();

    Object.DestroyImmediate(spriteRenderer); // UNH-SUPPRESS: Test verifies behavior after component destruction

    GameObject result = spriteRenderer.GetGameObject();

    Assert.IsTrue(result == null, "Should return null for destroyed component");
}
```

### Real Example: UnityExtensionsBasicTests.cs

From `/workspaces/com.wallstop-studios.unity-helpers/Tests/Runtime/Extensions/UnityExtensionsBasicTests.cs`:

```csharp
[Test]
public void GetCenterUsesCenterPointOffsetWhenAvailable()
{
    GameObject go = Track(new GameObject("CenterPointTest", typeof(CenterPointOffset)));

    go.transform.position = new Vector3(5f, 5f, 0f);
    CenterPointOffset offset = go.GetComponent<CenterPointOffset>();
    offset.offset = new Vector2(3f, 4f);

    Assert.AreEqual(offset.CenterPoint, go.GetCenter());

    Object.DestroyImmediate(offset); // UNH-SUPPRESS: Test verifies behavior after component destruction
    Assert.AreEqual((Vector2)go.transform.position, go.GetCenter());
}
```

This test verifies that `GetCenter()` falls back to the GameObject's transform position when the `CenterPointOffset` component is destroyed.

### Real Example: ObjectHelperTests.cs

From `/workspaces/com.wallstop-studios.unity-helpers/Tests/Runtime/Helper/ObjectHelperTests.cs`:

```csharp
[UnityTest]
public IEnumerator GetGameObject()
{
    GameObject go = Track(new GameObject("Test", typeof(SpriteRenderer)));
    SpriteRenderer spriteRenderer = go.GetComponent<SpriteRenderer>();

    GameObject result = go.GetGameObject();
    Assert.AreEqual(result, go);
    result = spriteRenderer.GetGameObject();
    Assert.AreEqual(result, go);

    Object.DestroyImmediate(spriteRenderer); // UNH-SUPPRESS: Test verifies behavior after component destruction
    result = spriteRenderer.GetGameObject();
    Assert.IsTrue(result == null);
    result = go.GetGameObject();
    Assert.AreEqual(result, go);

    Object.DestroyImmediate(go); // UNH-SUPPRESS: Test verifies behavior after GameObject destruction
    result = spriteRenderer.GetGameObject();
    Assert.IsTrue(result == null);
    result = go.GetGameObject();
    Assert.IsTrue(result == null);

    result = ((GameObject)null).GetGameObject();
    Assert.IsTrue(result == null);

    result = ((SpriteRenderer)null).GetGameObject();
    Assert.IsTrue(result == null);
    yield break;
}
```

This test verifies:

1. Normal operation with valid objects
2. Behavior after component destruction (object still valid)
3. Behavior after GameObject destruction (both references invalid)
4. Explicit null input handling

## Invalid Enum Values

Enums can hold any integer value their underlying type supports, not just defined members. This occurs when:

- Deserializing data from older/newer versions
- Casting user input or external data
- Data corruption

### Pattern: Cast Invalid Integer to Enum

```csharp
[Test]
public void DisplayNameWithInvalidEnumValue()
{
    TestEnum invalidValue = (TestEnum)999;

    string displayName = invalidValue.ToDisplayName();

    Assert.IsNotEmpty(displayName, "Should return some string, not crash");
}
```

### Pattern: Test All Enum Operations with Invalid Values

```csharp
[Test]
public void CachedNameWithInvalidEnumValue()
{
    TestEnum invalidValue = (TestEnum)999;

    string cachedName = invalidValue.ToCachedName();

    Assert.IsNotEmpty(cachedName);
}

[Test]
public void HasFlagNoAllocWithInvalidEnumValue()
{
    TestEnum invalidValue = (TestEnum)999;

    Assert.IsTrue(invalidValue.HasFlagNoAlloc(invalidValue));
    Assert.IsFalse(TestEnum.First.HasFlagNoAlloc(invalidValue));
}
```

### Real Example: EnumExtensionTests.cs

From `/workspaces/com.wallstop-studios.unity-helpers/Tests/Runtime/Extensions/EnumExtensionTests.cs`:

```csharp
[Test]
public void DisplayNameWithInvalidEnumValue()
{
    TestEnum invalidValue = (TestEnum)999;
    string displayName = invalidValue.ToDisplayName();
    Assert.IsNotEmpty(displayName);
}

[Test]
public void CachedNameWithInvalidEnumValue()
{
    TestEnum invalidValue = (TestEnum)999;
    string cachedName = invalidValue.ToCachedName();
    Assert.IsNotEmpty(cachedName);
}

[Test]
public void HasFlagNoAllocWithInvalidEnumValue()
{
    TestEnum invalidValue = (TestEnum)999;
    Assert.IsTrue(invalidValue.HasFlagNoAlloc(invalidValue));
    Assert.IsFalse(TestEnum.First.HasFlagNoAlloc(invalidValue));
}
```

### Pattern: Invalid SerializationType

```csharp
[Test]
public void GenericSerializeInvalidSerializationTypeThrowsException()
{
    TestMessage msg = new() { Id = 1, Name = "Test" };

    Assert.Throws<InvalidEnumArgumentException>(() =>
        Serializer.Serialize(msg, (SerializationType)999)
    );
}

[Test]
public void GenericDeserializeInvalidSerializationTypeThrowsException()
{
    byte[] data = { 1, 2, 3 };

    Assert.Throws<InvalidEnumArgumentException>(() =>
        Serializer.Deserialize<TestMessage>(data, (SerializationType)999)
    );
}
```

### Pattern: Flags Enum with All Bits Set

```csharp
[Test]
public void FlagsEnumShowsWhenAllFlagsSetAndExpectedIsSubset()
{
    OdinShowIfFlagsTarget target = CreateScriptableObject<OdinShowIfFlagsTarget>();
    target.flags = (TestFlagsEnum)(-1); // All bits set

    (bool success, bool shouldShow) = EvaluateCondition(
        target,
        nameof(OdinShowIfFlagsTarget.flags),
        new WShowIfAttribute(
            nameof(OdinShowIfFlagsTarget.flags),
            expectedValues: new object[] { TestFlagsEnum.FlagA | TestFlagsEnum.FlagB }
        )
    );

    Assert.That(success, Is.True);
    Assert.That(shouldShow, Is.True, "Field should show when all flags set and expected is subset");
}
```

## Overflow Conditions

Test behavior at the boundaries of numeric types to catch overflow, underflow, and precision issues.

### Pattern: Extreme Numeric Values

```csharp
[Test]
public void BinaryRoundTripComplexObjectAllFieldsCorrect()
{
    ComplexMessage msg = new()
    {
        Integer = int.MaxValue,
        Double = Math.PI,
        Text = "Complex test with unicode",
        Data = new byte[] { 1, 2, 3, 255, 0, 128 },
    };

    byte[] serialized = Serializer.BinarySerialize(msg);
    ComplexMessage deserialized = Serializer.BinaryDeserialize<ComplexMessage>(serialized);

    Assert.AreEqual(msg.Integer, deserialized.Integer);
    Assert.AreEqual(msg.Double, deserialized.Double);
}
```

### Pattern: Edge Case Values via TestCaseSource

```csharp
private static IEnumerable<TestCaseData> EdgeCaseTestData()
{
    yield return new TestCaseData(new[] { int.MaxValue }, int.MaxValue)
        .SetName("Input.MaxValue.HandlesCorrectly");
    yield return new TestCaseData(new[] { int.MinValue }, int.MinValue)
        .SetName("Input.MinValue.HandlesCorrectly");
    yield return new TestCaseData(new[] { 0 }, 0)
        .SetName("Input.Zero.ReturnsZero");
    yield return new TestCaseData(new[] { -1 }, -1)
        .SetName("Input.Negative.HandlesCorrectly");
}

[Test]
[TestCaseSource(nameof(EdgeCaseTestData))]
public void ProcessHandlesEdgeCases(int[] input, int expected)
{
    int result = MyProcessor.Process(input);

    Assert.AreEqual(expected, result);
}
```

### Real Example: SerializerAdditionalTests.cs

From `/workspaces/com.wallstop-studios.unity-helpers/Tests/Runtime/Serialization/SerializerAdditionalTests.cs`:

```csharp
[Test]
public void GenericSerializeWithAllTypesEdgeCaseData()
{
    ComplexMessage msg = new()
    {
        Integer = int.MinValue,
        Double = double.MaxValue,
        Text = string.Empty,
        Data = new byte[] { 0, 255 },
        StringList = new List<string> { "", "test" },
        Dictionary = new Dictionary<string, int> { [""] = 0, ["test"] = -1 },
    };

    foreach (SerializationType type in new[]
    {
        SerializationType.SystemBinary,
        SerializationType.Protobuf,
    })
    {
        byte[] serialized = Serializer.Serialize(msg, type);
        ComplexMessage deserialized = Serializer.Deserialize<ComplexMessage>(serialized, type);

        Assert.AreEqual(msg.Integer, deserialized.Integer, $"Failed for {type}");
        Assert.AreEqual(msg.Double, deserialized.Double, $"Failed for {type}");
    }
}
```

## Corrupted Serialization State

Test handling of malformed, truncated, or invalid serialized data.

### Pattern: Empty Data

```csharp
[Test]
public void BinaryDeserializeEmptyArrayThrowsException()
{
    byte[] emptyData = Array.Empty<byte>();

    Assert.Throws<SerializationException>(() =>
        Serializer.BinaryDeserialize<TestMessage>(emptyData)
    );
}

[Test]
public void ProtoDeserializeEmptyArrayReturnsDefaultInstance()
{
    byte[] emptyData = Array.Empty<byte>();

    TestMessage result = Serializer.ProtoDeserialize<TestMessage>(emptyData);

    Assert.NotNull(result);
    Assert.AreEqual(0, result.Id);
}
```

### Pattern: Corrupted Data

```csharp
[Test]
public void BinaryDeserializeCorruptedDataThrowsException()
{
    byte[] corruptedData = { 0xFF, 0xFF, 0xFF, 0xFF };

    Assert.Throws<SerializationException>(() =>
        Serializer.BinaryDeserialize<TestMessage>(corruptedData)
    );
}

[Test]
public void FileIOReadFromInvalidJsonThrowsException()
{
    string filePath = Path.Combine(_tempDirectory, "invalid.json");
    File.WriteAllText(filePath, "{ invalid json content }");

    Assert.Throws<JsonException>(() =>
        Serializer.ReadFromJsonFile<TestMessage>(filePath)
    );
}
```

### Pattern: Null Data

```csharp
[Test]
public void ProtoDeserializeNullDataThrowsException()
{
    Assert.Throws<ProtoException>(() =>
        Serializer.ProtoDeserialize<TestMessage>(null)
    );
}

[Test]
public void ProtoDeserializeWithTypeNullDataThrowsException()
{
    Assert.Throws<ArgumentException>(() =>
        Serializer.ProtoDeserialize<object>(null, typeof(TestMessage))
    );
}
```

## Best Practices

### Identifying "Impossible" States to Test

1. **Review defensive code paths**: Any `if (x == null)` or `try-catch` suggests a potential "impossible" state
2. **Examine switch statements**: Missing `default` cases indicate unhandled enum values
3. **Check serialization boundaries**: Data crossing process/version boundaries can be corrupted
4. **Consider Unity lifecycle**: Objects can be destroyed at any frame
5. **Look for race conditions**: Multi-threaded code has timing-dependent states

### Test Structure

Always include these categories in your tests:

| Category             | Examples                                         |
| -------------------- | ------------------------------------------------ |
| Normal cases         | Typical usage, common inputs                     |
| Edge cases           | Empty, single element, boundary values           |
| Negative cases       | Invalid inputs, error conditions                 |
| Extreme cases        | Maximum values, large collections                |
| **"The Impossible"** | Destroyed objects, invalid enums, corrupted data |

### UNH-SUPPRESS Usage

When testing destroyed object behavior, use the `// UNH-SUPPRESS` comment:

```csharp
// UNH-SUPPRESS tells the test linter this DestroyImmediate is intentional
Object.DestroyImmediate(target); // UNH-SUPPRESS: Test verifies behavior after destruction
```

Only use this for intentional destruction testing, not for cleanup. Use `Track()` for normal test cleanup.

### Assertions for "Impossible" States

Choose assertions based on expected behavior:

```csharp
// When graceful handling is expected
Assert.DoesNotThrow(() => Process(invalidInput));
Assert.IsNotEmpty(invalidValue.ToDisplayName());
Assert.IsTrue(result == null);

// When exceptions are expected
Assert.Throws<InvalidEnumArgumentException>(() => Serialize(msg, (SerializationType)999));
Assert.Throws<SerializationException>(() => Deserialize(corruptedData));

// When default values are expected
Assert.AreEqual(default(T), result);
Assert.AreEqual(0, deserializedFromEmpty.Id);
```

## Data-Driven Testing for Edge Cases

Use `[TestCaseSource]` to systematically cover impossible states:

```csharp
private static IEnumerable<TestCaseData> ImpossibleStateTestCases()
{
    // Destroyed references
    yield return new TestCaseData(CreateDestroyedObject())
        .SetName("State.DestroyedObject.HandledGracefully");

    // Invalid enums
    yield return new TestCaseData((MyEnum)(-1))
        .SetName("State.NegativeEnumValue.HandledGracefully");
    yield return new TestCaseData((MyEnum)999)
        .SetName("State.LargeEnumValue.HandledGracefully");
    yield return new TestCaseData((MyEnum)int.MaxValue)
        .SetName("State.MaxIntEnumValue.HandledGracefully");

    // Overflow values
    yield return new TestCaseData(int.MaxValue)
        .SetName("State.IntMaxValue.HandledGracefully");
    yield return new TestCaseData(int.MinValue)
        .SetName("State.IntMinValue.HandledGracefully");

    // Corrupted strings
    yield return new TestCaseData("\0\0\0")
        .SetName("State.NullChars.HandledGracefully");
    yield return new TestCaseData(new string('\uD800', 1000))
        .SetName("State.InvalidSurrogates.HandledGracefully");
}

[Test]
[TestCaseSource(nameof(ImpossibleStateTestCases))]
public void ProcessHandlesImpossibleStates(object input)
{
    Assert.DoesNotThrow(() => Process(input));
}
```

## Summary

Testing "impossible" states is essential for robust production code. These tests:

1. **Catch silent failures** before they reach users
2. **Document expected behavior** for edge cases
3. **Prevent regressions** when code is refactored
4. **Build confidence** that defensive code works

When adding new features, always ask: "What happens if this input is destroyed, null, invalid, or corrupted?" Then write tests to answer that question.

For more information on contributing to Unity Helpers, see the [Contributing guide](../project/contributing.md).
