# Skill: Create Enum

<!-- trigger: enum, enumeration, create, type | Creating a new enum type | Core -->

**Trigger**: When creating any new `enum` type in this repository.

---

## MANDATORY Requirements (NON-NEGOTIABLE)

| Rule                                | Requirement                                                            |
| ----------------------------------- | ---------------------------------------------------------------------- |
| **Explicit integer assignment**     | EVERY enum member MUST have an explicit integer value (`= N`)          |
| **Zero value first**                | First member MUST be `None`, `Unknown`, or `Unset` with value `= 0`    |
| **`[Obsolete]` on zero value**      | Zero member MUST have `[Obsolete]` attribute (non-error, warning only) |
| **Sequential or meaningful values** | Use sequential integers (1, 2, 3) or powers of 2 for `[Flags]`         |

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

### 1. Explicit Values for Every Member (MANDATORY)

**EVERY enum member MUST have an explicitly assigned integer value.** No implicit values allowed.

```csharp
// ✅ CORRECT - explicit values
public enum Status
{
    [Obsolete("Use a specific Status value instead of Unknown.")]
    Unknown = 0,
    Active = 1,
    Inactive = 2,
    Pending = 3,
}

// ❌ INCORRECT - implicit values (FORBIDDEN)
public enum Status
{
    Unknown,
    Active,
    Inactive,
    Pending,
}

// ❌ INCORRECT - partial explicit values (FORBIDDEN)
public enum Status
{
    Unknown = 0,
    Active,      // Implicit!
    Inactive,    // Implicit!
    Pending,     // Implicit!
}
```

### 2. First Member Must Be `None`/`Unknown`/`Unset` with Value `0` (MANDATORY)

The first member represents the uninitialized/default state and MUST be value `0`:

```csharp
// ✅ CORRECT - None as first member with value 0
public enum Direction
{
    [Obsolete("Use a specific Direction value instead of None.")]
    None = 0,
    North = 1,
    East = 2,
    South = 3,
    West = 4,
}

// ✅ CORRECT - Unknown as first member with value 0
public enum ProcessState
{
    [Obsolete("Use a specific ProcessState value instead of Unknown.")]
    Unknown = 0,
    Starting = 1,
    Running = 2,
    Stopped = 3,
}

// ❌ INCORRECT - no zero/default member (FORBIDDEN)
public enum Direction
{
    North = 1,
    East = 2,
    South = 3,
    West = 4,
}

// ❌ INCORRECT - first member not zero (FORBIDDEN)
public enum Direction
{
    North = 1,
    East = 2,
    South = 3,
    West = 4,
    None = 0,    // Should be first!
}
```

### 3. Mark Zero Value as `[Obsolete]` (MANDATORY when possible)

Use a **non-erroring** obsolete attribute (warning only, does NOT fail compilation):

```csharp
// ✅ CORRECT - warning-only obsolete (no error parameter or error=false)
[Obsolete("Use a specific {EnumName} value instead of {ZeroMemberName}.")]
None = 0,

// ❌ INCORRECT - error-causing obsolete (FORBIDDEN)
[Obsolete("Don't use", true)]  // true = compilation error!
None = 0,

// ❌ INCORRECT - no obsolete attribute
None = 0,  // Missing [Obsolete]!
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

❌ **Missing explicit values** (FORBIDDEN):

```csharp
public enum State { None, Active, Inactive }
```

❌ **Partial explicit values** (FORBIDDEN):

```csharp
public enum State { None = 0, Active, Inactive }  // Active & Inactive are implicit!
```

❌ **Zero value without `[Obsolete]`**:

```csharp
public enum State { None = 0, Active = 1 }  // Missing [Obsolete] on None!
```

❌ **Non-zero first value**:

```csharp
public enum State { Active = 1, Inactive = 2 }  // No zero/default value!
```

❌ **Generic obsolete message**:

```csharp
[Obsolete("Don't use")]  // Not helpful - specify the enum name!
None = 0,
```

❌ **Error-causing obsolete**:

```csharp
[Obsolete("Use a specific value", true)]  // true = compilation error!
None = 0,
```

---

## Exceptions (When `[Obsolete]` May Be Omitted)

In rare cases, the `[Obsolete]` attribute on the zero value may be omitted:

| Exception                    | Example                                           | Reason                                    |
| ---------------------------- | ------------------------------------------------- | ----------------------------------------- |
| `None` is semantically valid | `[Flags]` where `None` means "no flags set"       | `None` is a legitimate, intentional value |
| Boolean-like enum            | `TriState { Unknown = 0, False = 1, True = 2 }`   | `Unknown` may be a valid state            |
| External API compatibility   | Matching external library or protocol enum        | Must match external definition            |
| Zero is the expected default | `Priority { Normal = 0, High = 1, Critical = 2 }` | `Normal` is intentionally the default     |

**When omitting `[Obsolete]`:**

1. Document WHY in a code comment
2. Ensure zero value has a meaningful name (not just `None` or `Unknown`)
3. Still use explicit integer values for ALL members

```csharp
// Priority.Normal is intentionally the default - most operations are normal priority
public enum Priority
{
    Normal = 0,    // Valid default - not obsolete
    High = 1,
    Critical = 2,
    Urgent = 3,
}
```
