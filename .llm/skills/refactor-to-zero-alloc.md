# Skill: Refactor to Zero-Allocation Patterns

**Trigger**: When refactoring existing code that contains heap allocations to achieve zero-allocation in steady state. Use this skill when:

- Reviewing existing code for performance issues
- Migrating legacy code to high-performance patterns
- Fixing allocation hotspots identified in Unity Profiler
- Converting LINQ-heavy code to explicit loops

---

## Refactoring Process Overview

Follow these steps in order:

1. **Eliminate LINQ** - LINQ is forbidden; replace with explicit loops or visitor patterns
2. **Eliminate closures** - Remove lambda captures and delegate allocations
3. **Migrate to collection pooling** - Replace `new List<T>()` with `Buffers<T>`
4. **Apply StringBuilder patterns** - Replace string concatenation
5. **Use array pools** - Replace `new T[]` with appropriate pool
6. **Fix struct equality** - Implement `IEquatable<T>` on all structs
7. **Address foreach boxing** - Replace `foreach` on `List<T>` with `for` loops

---

## Struct vs Class Decision Matrix

Use this matrix to decide when to use structs vs classes:

| Factor               | Use Struct          | Use Class                               |
| -------------------- | ------------------- | --------------------------------------- |
| Size                 | < 16 bytes          | > 16 bytes                              |
| Copying frequency    | Low (pass by ref)   | High (need shared reference)            |
| Number of references | Few (1-2)           | Many (3+)                               |
| Inheritance needed   | No                  | Yes                                     |
| Mutability           | Immutable preferred | Mutable OK                              |
| Heap overhead        | N/A                 | 16 bytes header + 8 bytes per reference |

**Memory Calculation Example (56-byte data):**

```csharp
// Class: 56 + 16 (header) + 8×2 (refs) = 88 bytes
// Struct with 2 copies: 56 × 2 = 112 bytes (worse!)
// Struct with 1 copy: 56 bytes (better!)
```

**Rule**: If you'll have 3+ references to the same data, use a class.

---

## LINQ is FORBIDDEN — Use Zero-Allocation Visitor Patterns

**LINQ is NEVER acceptable in this codebase.** Every LINQ method allocates:

- **Iterator object** — The `IEnumerator<T>` state machine
- **Delegate allocation** — Every lambda/predicate passed
- **Closure objects** — When lambdas capture variables

Even "streaming" LINQ (`IEnumerable` without `ToList()`) still allocates the iterator and delegate on every call.

### Zero-Allocation Visitor Pattern

Instead of LINQ, use explicit loops with the visitor pattern:

```csharp
// ❌ FORBIDDEN: LINQ streaming (still allocates iterator + delegate!)
IEnumerable<Event> events = GetEventsLazy();
foreach (Event e in events.Where(e => e.IsRecent))
{
    ProcessEvent(e);
}

// ✅ REQUIRED: Zero-allocation visitor pattern
public void VisitRecentEvents(IReadOnlyList<Event> events, Action<Event> visitor)
{
    for (int i = 0; i < events.Count; i++)
    {
        Event e = events[i];
        if (e.IsRecent)
        {
            visitor(e);
        }
    }
}

// ✅ BETTER: Inline visitor logic to avoid delegate allocation
for (int i = 0; i < events.Count; i++)
{
    Event e = events[i];
    if (e.IsRecent)
    {
        ProcessEvent(e);
    }
}

// ✅ BEST: Generic visitor with ref struct for complex state
public ref struct EventVisitor
{
    public int ProcessedCount;
    public int TotalDamage;

    public void Visit(Event e)
    {
        if (e.IsRecent)
        {
            ProcessedCount++;
            TotalDamage += e.Damage;
        }
    }
}

EventVisitor visitor = new EventVisitor();
for (int i = 0; i < events.Count; i++)
{
    visitor.Visit(events[i]);
}
```

### When LINQ Might Be Acceptable (Extremely Rare)

LINQ is only acceptable when ALL of these conditions are met:

1. **Editor-only code** — Never called at runtime
2. **One-time initialization** — Called once during app startup, not per-frame
3. **Complexity significantly reduced** — The LINQ version is dramatically clearer
4. **Documented exception** — Comment explains why LINQ was chosen

```csharp
// Editor-only, one-time initialization — LINQ acceptable with documentation
// LINQ Exception: Editor tool initialization, called once on domain reload
private static readonly Dictionary<Type, MethodInfo[]> CachedMethods =
    AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .ToDictionary(t => t, t => t.GetMethods());
```

**When in doubt: DO NOT use LINQ.** The explicit loop is always safe.

---

## Step 1: Identify Allocations

### Common Allocation Sources Checklist

| Pattern                | Allocation Type       | Search For                                                           |
| ---------------------- | --------------------- | -------------------------------------------------------------------- |
| LINQ methods           | Iterator + delegate   | `.Where(`, `.Select(`, `.Any(`, `.First(`, `.ToList()`, `.ToArray()` |
| Collection creation    | Heap allocation       | `new List<`, `new Dictionary<`, `new HashSet<`                       |
| Closures               | Closure object        | Lambdas that capture local variables or `this`                       |
| String operations      | String allocation     | `string.Format`, `$"..."`, `+` in loops                              |
| Array creation         | Heap allocation       | `new T[`, `new byte[`, `new int[`                                    |
| Boxing                 | Box allocation        | Struct assigned to `object`, non-generic interfaces                  |
| foreach on List        | Enumerator boxing     | `foreach` on `List<T>` (Mono compiler)                               |
| foreach on non-generic | Enumerator allocation | `foreach` on `IEnumerable` (not `IEnumerable<T>`)                    |
| params methods         | Array allocation      | Method calls with `params` parameters                                |
| Delegate in loop       | Boxing                | `Func<>` or `Action<>` assigned inside loops                         |
| Enum dictionary        | Boxing per lookup     | `Dictionary<MyEnum, T>` without custom comparer                      |

### How to Find Allocations

```csharp
// Search regex for common allocations:
// \.Where\(|\.Select\(|\.Any\(|\.First\(|\.ToList\(|\.ToArray\(
// new List<|new Dictionary<|new HashSet<
// => .*[^static]  (lambdas that might capture)
// foreach.*List<  (foreach on List)
```

---

## Step 2: LINQ → Zero-Allocation Loop/Visitor Conversion

**LINQ is forbidden.** Convert all LINQ to explicit loops or visitor patterns. The patterns below show common LINQ operations and their zero-allocation replacements.

### Pattern: Where + FirstOrDefault

```csharp
// ❌ BEFORE: Allocates iterator and delegate
Enemy target = enemies.Where(e => e.IsAlive && e.Team != myTeam)
                      .FirstOrDefault();

// ✅ AFTER: Zero allocation
Enemy target = null;
for (int i = 0; i < enemies.Count; i++)
{
    Enemy e = enemies[i];
    if (e.IsAlive && e.Team != myTeam)
    {
        target = e;
        break;
    }
}
```

### Pattern: Where + ToList

```csharp
// ❌ BEFORE: Allocates iterator, delegate, AND new list
List<Enemy> activeEnemies = enemies.Where(e => e.IsActive).ToList();

// ✅ AFTER: Zero allocation with pooling
using var lease = Buffers<Enemy>.List.Get(out List<Enemy> activeEnemies);
for (int i = 0; i < enemies.Count; i++)
{
    Enemy e = enemies[i];
    if (e.IsActive)
    {
        activeEnemies.Add(e);
    }
}
```

### Pattern: Select + ToArray

```csharp
// ❌ BEFORE: Allocates iterator, delegate, and array
string[] names = items.Select(x => x.Name).ToArray();

// ✅ AFTER: Zero allocation (if size known)
using PooledArray<string> pooled = SystemArrayPool<string>.Get(items.Count, out string[] names);
for (int i = 0; i < pooled.Length; i++)
{
    names[i] = items[i].Name;
}
```

### Pattern: Any

```csharp
// ❌ BEFORE: Allocates iterator and delegate
bool hasActive = enemies.Any(e => e.IsActive);

// ✅ AFTER: Zero allocation
bool hasActive = false;
for (int i = 0; i < enemies.Count; i++)
{
    if (enemies[i].IsActive)
    {
        hasActive = true;
        break;
    }
}
```

### Pattern: Count with predicate

```csharp
// ❌ BEFORE: Allocates iterator and delegate
int activeCount = enemies.Count(e => e.IsActive);

// ✅ AFTER: Zero allocation
int activeCount = 0;
for (int i = 0; i < enemies.Count; i++)
{
    if (enemies[i].IsActive)
    {
        activeCount++;
    }
}
```

### Pattern: Sum

```csharp
// ❌ BEFORE: Allocates delegate
int totalDamage = attacks.Sum(a => a.Damage);

// ✅ AFTER: Zero allocation
int totalDamage = 0;
for (int i = 0; i < attacks.Count; i++)
{
    totalDamage += attacks[i].Damage;
}
```

### Pattern: OrderBy + ToList

```csharp
// ❌ BEFORE: Multiple allocations
List<Enemy> sorted = enemies.OrderBy(e => e.Distance).ToList();

// ✅ AFTER: Zero allocation with pooling and in-place sort
using var lease = Buffers<Enemy>.List.Get(out List<Enemy> sorted);
for (int i = 0; i < enemies.Count; i++)
{
    sorted.Add(enemies[i]);
}
sorted.Sort((a, b) => a.Distance.CompareTo(b.Distance));
```

---

## Step 3: Closure Elimination Techniques

### Technique 1: Extract to Explicit Loop

```csharp
// ❌ BEFORE: Closure captures searchId
int searchId = GetTargetId();
Enemy found = enemies.Find(e => e.Id == searchId);

// ✅ AFTER: Explicit loop, no closure
int searchId = GetTargetId();
Enemy found = null;
for (int i = 0; i < enemies.Count; i++)
{
    if (enemies[i].Id == searchId)
    {
        found = enemies[i];
        break;
    }
}
```

### Technique 2: Static Lambda for Cached Delegates

```csharp
// ❌ BEFORE: Instance method creates closure each call
items.Sort((a, b) => a.Priority.CompareTo(b.Priority));

// ✅ AFTER: Static comparison, single allocation at class load
private static readonly Comparison<Item> PriorityComparison =
    static (a, b) => a.Priority.CompareTo(b.Priority);

items.Sort(PriorityComparison);
```

### Technique 3: Pass State via Parameter

```csharp
// ❌ BEFORE: Closure captures 'threshold'
float threshold = GetThreshold();
ProcessItems(items, item => item.Value > threshold);

// ✅ AFTER: Pass threshold as parameter
float threshold = GetThreshold();
ProcessItems(items, threshold, static (item, thresh) => item.Value > thresh);

// Or better, inline the logic:
for (int i = 0; i < items.Count; i++)
{
    if (items[i].Value > threshold)
    {
        ProcessItem(items[i]);
    }
}
```

### Technique 4: Remove 'this' Capture

```csharp
// ❌ BEFORE: Captures 'this' implicitly
items.RemoveAll(x => x.Owner == this);

// ✅ AFTER: Explicit loop
for (int i = items.Count - 1; i >= 0; i--)
{
    if (items[i].Owner == this)
    {
        items.RemoveAt(i);
    }
}
```

---

## Step 4: Collection Pooling Migration

### Pattern: Method Creating List

```csharp
// ❌ BEFORE: Allocates every call
public List<Enemy> GetEnemiesInRange(float range)
{
    List<Enemy> result = new List<Enemy>();
    foreach (Enemy e in allEnemies)
    {
        if (e.Distance < range)
        {
            result.Add(e);
        }
    }
    return result;
}

// ✅ AFTER: Caller provides buffer
public void GetEnemiesInRange(float range, List<Enemy> result)
{
    result.Clear();
    for (int i = 0; i < allEnemies.Count; i++)
    {
        Enemy e = allEnemies[i];
        if (e.Distance < range)
        {
            result.Add(e);
        }
    }
}

// ✅ CALLER:
using var lease = Buffers<Enemy>.List.Get(out List<Enemy> nearbyEnemies);
GetEnemiesInRange(10f, nearbyEnemies);
```

### Pattern: Temporary HashSet

```csharp
// ❌ BEFORE: Allocates HashSet
public bool HasDuplicates(List<int> ids)
{
    HashSet<int> seen = new HashSet<int>();
    foreach (int id in ids)
    {
        if (!seen.Add(id))
        {
            return true;
        }
    }
    return false;
}

// ✅ AFTER: Pooled HashSet
public bool HasDuplicates(List<int> ids)
{
    using var lease = Buffers<int>.HashSet.Get(out HashSet<int> seen);
    for (int i = 0; i < ids.Count; i++)
    {
        if (!seen.Add(ids[i]))
        {
            return true;
        }
    }
    return false;
}
```

### Pattern: Temporary Dictionary

```csharp
// ❌ BEFORE: Allocates Dictionary
public void GroupByType(List<Item> items)
{
    Dictionary<ItemType, List<Item>> groups = new Dictionary<ItemType, List<Item>>();
    // ...
}

// ✅ AFTER: Pooled Dictionary with pooled value lists
using var dictLease = Buffers<ItemType, List<Item>>.Dictionary.Get(
    out Dictionary<ItemType, List<Item>> groups);

// Note: Value lists also need careful management
```

---

## Step 5: StringBuilder Patterns

### Pattern: String Concatenation in Loop

```csharp
// ❌ BEFORE: New string allocation each iteration
string result = "";
for (int i = 0; i < items.Count; i++)
{
    result += items[i].Name;
    if (i < items.Count - 1)
    {
        result += ", ";
    }
}

// ✅ AFTER: Pooled StringBuilder
using var lease = Buffers.StringBuilder.Get(out StringBuilder sb);
for (int i = 0; i < items.Count; i++)
{
    if (i > 0)
    {
        sb.Append(", ");
    }
    sb.Append(items[i].Name);
}
string result = sb.ToString();
```

### Pattern: String Interpolation

```csharp
// ❌ BEFORE: Allocates formatted string
string message = $"Player {player.Name} scored {score} points!";

// ✅ AFTER: Pooled StringBuilder (for hot paths)
using var lease = Buffers.StringBuilder.Get(out StringBuilder sb);
sb.Append("Player ");
sb.Append(player.Name);
sb.Append(" scored ");
sb.Append(score);
sb.Append(" points!");
string message = sb.ToString();

// Note: For non-hot paths, interpolation is acceptable for readability
```

### Pattern: Multiple Format Calls

```csharp
// ❌ BEFORE: Multiple allocations
string line1 = string.Format("Name: {0}", name);
string line2 = string.Format("Score: {0}", score);
string output = line1 + "\n" + line2;

// ✅ AFTER: Single StringBuilder
using var lease = Buffers.StringBuilder.Get(out StringBuilder sb);
sb.Append("Name: ").Append(name).AppendLine();
sb.Append("Score: ").Append(score);
string output = sb.ToString();
```

---

## Step 6: Array Pool Usage

### Pattern: Temporary Array (Variable Size)

```csharp
// ❌ BEFORE: Allocates array
public void ProcessData(List<int> data)
{
    int[] temp = new int[data.Count];
    for (int i = 0; i < data.Count; i++)
    {
        temp[i] = data[i];
    }
    // ... process temp ...
}

// ✅ AFTER: SystemArrayPool (variable size)
public void ProcessData(List<int> data)
{
    using PooledArray<int> pooled = SystemArrayPool<int>.Get(data.Count, out int[] temp);
    for (int i = 0; i < pooled.Length; i++)  // Use pooled.Length!
    {
        temp[i] = data[i];
    }
    // ... process temp ...
}
```

### Pattern: Fixed-Size Buffer

```csharp
// ❌ BEFORE: Allocates fixed-size array
private void ComputeHash()
{
    byte[] buffer = new byte[64];
    // ... fill and use buffer ...
}

// ✅ AFTER: WallstopArrayPool (constant size)
private void ComputeHash()
{
    using PooledArray<byte> pooled = WallstopArrayPool<byte>.Get(64, out byte[] buffer);
    // ... fill and use buffer ...
}
```

### Critical: Choose the Right Pool

| Size Source                | Pool to Use            | Why                           |
| -------------------------- | ---------------------- | ----------------------------- |
| Literal constant (64, 256) | `WallstopArrayPool<T>` | Exact size, finite buckets    |
| `collection.Count`         | `SystemArrayPool<T>`   | Variable size, safe bucketing |
| User input                 | `SystemArrayPool<T>`   | Unpredictable sizes           |
| `width * height`           | `SystemArrayPool<T>`   | Computed at runtime           |

---

## Complete Refactoring Example

### Before: Multiple Allocation Issues

```csharp
public class EnemyManager
{
    private List<Enemy> _enemies = new List<Enemy>();

    // ❌ Multiple allocations per call
    public string GetStatusReport()
    {
        // LINQ allocations
        var activeEnemies = _enemies.Where(e => e.IsActive).ToList();
        var totalHealth = activeEnemies.Sum(e => e.Health);

        // String allocation
        string report = $"Active: {activeEnemies.Count}, Total HP: {totalHealth}";

        // More LINQ
        var lowHealth = activeEnemies.Where(e => e.Health < 50).ToList();
        foreach (var enemy in lowHealth)
        {
            report += $"\n  Warning: {enemy.Name} at {enemy.Health} HP";
        }

        return report;
    }
}
```

### After: Zero-Allocation

```csharp
public class EnemyManager
{
    private List<Enemy> _enemies = new List<Enemy>();

    // ✅ Zero allocation in steady state
    public string GetStatusReport()
    {
        using var activeLease = Buffers<Enemy>.List.Get(out List<Enemy> activeEnemies);
        using var sbLease = Buffers.StringBuilder.Get(out StringBuilder sb);

        // Explicit loop instead of LINQ
        int totalHealth = 0;
        for (int i = 0; i < _enemies.Count; i++)
        {
            Enemy e = _enemies[i];
            if (e.IsActive)
            {
                activeEnemies.Add(e);
                totalHealth += e.Health;
            }
        }

        // StringBuilder instead of interpolation
        sb.Append("Active: ");
        sb.Append(activeEnemies.Count);
        sb.Append(", Total HP: ");
        sb.Append(totalHealth);

        // Single pass for low health warnings
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            Enemy e = activeEnemies[i];
            if (e.Health < 50)
            {
                sb.AppendLine();
                sb.Append("  Warning: ");
                sb.Append(e.Name);
                sb.Append(" at ");
                sb.Append(e.Health);
                sb.Append(" HP");
            }
        }

        return sb.ToString();
    }
}
```

---

## Verification: Confirming Zero Allocations

### Unity Profiler Method

1. Open **Window → Analysis → Profiler**
2. Enable **Deep Profile** for detailed allocation tracking
3. Select **CPU Usage** module
4. Look for **GC.Alloc** column in the hierarchy
5. Run your code path and check for allocations

### Profiler Markers

```csharp
using Unity.Profiling;

private static readonly ProfilerMarker s_MyMethodMarker =
    new ProfilerMarker("MyClass.MyMethod");

public void MyMethod()
{
    using (s_MyMethodMarker.Auto())
    {
        // Code to profile
    }
}
```

### Allocation Test Pattern

```csharp
[Test]
public void Method_ShouldNotAllocate_InSteadyState()
{
    // Warm up - first call may allocate
    myObject.Method();

    // Measure steady state
    long before = GC.GetAllocatedBytesForCurrentThread();

    for (int i = 0; i < 1000; i++)
    {
        myObject.Method();
    }

    long after = GC.GetAllocatedBytesForCurrentThread();
    long allocated = after - before;

    Assert.AreEqual(0, allocated, $"Method allocated {allocated} bytes");
}
```

---

## Quick Refactoring Checklist

When refactoring a method to zero-allocation:

- [ ] **Eliminate ALL LINQ** → Convert to explicit `for` loops or visitor patterns
- [ ] Consider visitor pattern for complex iteration logic
- [ ] Find all closures → Use static lambdas or inline logic
- [ ] Find `new List<T>()` → Use `Buffers<T>.List.Get()`
- [ ] Find `new HashSet<T>()` → Use `Buffers<T>.HashSet.Get()`
- [ ] Find `new T[]` with variable size → Use `SystemArrayPool<T>.Get()`
- [ ] Find `new T[]` with constant size → Use `WallstopArrayPool<T>.Get()`
- [ ] Find string concatenation in loops → Use `Buffers.StringBuilder.Get()`
- [ ] Find string interpolation in hot paths → Use StringBuilder
- [ ] Verify all `using` statements are present for pooled resources
- [ ] Check method signatures - prefer out parameters over return values
- [ ] Run Unity Profiler to verify zero allocations

---

## Related Skills

- [high-performance-csharp](./high-performance-csharp.md) - Core performance philosophy
- [unity-performance-patterns](./unity-performance-patterns.md) - Unity-specific patterns
- [profile-debug-performance](./profile-debug-performance.md) - Profiling guide
- [use-pooling](./use-pooling.md) - Detailed collection pooling patterns
- [use-array-pool](./use-array-pool.md) - Array pool selection guide
- [performance-audit](./performance-audit.md) - Performance review checklist
