# Skill: Create Enum

**Trigger**: When creating any new `enum` type in this repository.

---

## Enum Template

```csharp
public enum ItemType
{
    [Obsolete("Use a specific ItemType value instead of None.")]
    None = 0,
    Weapon = 1,
    Armor = 2,
    Consumable = 3,
}
```

---

## Required Rules

### 1. Explicit Values for Every Member

Every enum member MUST have an explicitly assigned integer value:

```csharp
// ✅ CORRECT
public enum Status
{
    [Obsolete("Use a specific Status value instead of Unknown.")]
    Unknown = 0,
    Active = 1,
    Inactive = 2,
    Pending = 3,
}

// ❌ INCORRECT - implicit values
public enum Status
{
    Unknown,
    Active,
    Inactive,
    Pending,
}
```

### 2. First Member Must Be `None` or `Unknown` with Value `0`

This represents the uninitialized/default state:

```csharp
// ✅ CORRECT
public enum Direction
{
    [Obsolete("Use a specific Direction value instead of None.")]
    None = 0,
    North = 1,
    East = 2,
    South = 3,
    West = 4,
}
```

### 3. Mark Zero Value as `[Obsolete]`

Use a non-erroring obsolete attribute to warn users:

```csharp
[Obsolete("Use a specific {EnumName} value instead of {ZeroMemberName}.")]
```

The message should guide users to choose a specific value.

---

## Why This Pattern?

| Benefit                  | Explanation                                                                       |
| ------------------------ | --------------------------------------------------------------------------------- |
| Distinguishable defaults | Default `ItemType` field values are distinguishable from intentionally set values |
| Serialization safety     | Serialization/deserialization can detect missing or unset fields                  |
| IDE warnings             | Warnings appear when using the default/unset value                                |
| Stable ordering          | Explicit values prevent changes when members are reordered                        |

---

## Flags Enum Template

For `[Flags]` enums, use powers of 2:

```csharp
[Flags]
public enum Permissions
{
    [Obsolete("Use specific Permissions flags instead of None.")]
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4,
    Delete = 8,
    All = Read | Write | Execute | Delete,
}
```

---

## Examples

### Simple Enum

```csharp
public enum LogLevel
{
    [Obsolete("Use a specific LogLevel value instead of Unset.")]
    Unset = 0,
    Debug = 1,
    Info = 2,
    Warning = 3,
    Error = 4,
    Fatal = 5,
}
```

### Enum with Custom Display Names

```csharp
public enum DamageType
{
    [Obsolete("Use a specific DamageType value instead of None.")]
    None = 0,

    [EnumDisplayName("Physical Damage")]
    Physical = 1,

    [EnumDisplayName("Fire Damage")]
    Fire = 2,

    [EnumDisplayName("Ice Damage")]
    Ice = 3,

    [EnumDisplayName("Lightning Damage")]
    Lightning = 4,
}
```

---

## Common Mistakes

❌ **Missing explicit values**:

```csharp
public enum State { None, Active, Inactive }
```

❌ **Zero value without `[Obsolete]`**:

```csharp
public enum State { None = 0, Active = 1 }
```

❌ **Non-zero first value**:

```csharp
public enum State { Active = 1, Inactive = 2 }
```

❌ **Generic obsolete message**:

```csharp
[Obsolete("Don't use")]  // Not helpful
None = 0,
```
