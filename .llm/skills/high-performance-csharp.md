# Skill: High-Performance C

**Trigger**: When implementing ANY new feature, fixing bugs, or writing editor tooling. This applies to ALL code in this repository.

---

## Core Philosophy

**Every code path should be allocation-free in steady state.** This includes:

- Runtime gameplay code
- Editor tooling and inspectors (called every frame when visible)
- Bug fixes (must not regress performance)
- Test utilities (may run thousands of iterations)

### Why Zero-Allocation Matters in Unity

Unity uses the **Boehm-Demers-Weiser** garbage collector, which:

- **Scans the entire heap** on every collection (non-generational)
- **Does NOT compact memory** — leads to fragmentation
- **Causes frame stutters** — "stop the world" during collection

At 60 FPS with 1KB/frame allocation = **3.6 MB/minute** of garbage, triggering frequent GC pauses.

**Never duplicate code — build abstractions.** When you see repetitive patterns:

- Extract to lightweight, reusable abstractions
- Prefer `readonly struct` or `static` methods for zero allocation
- Apply SOLID principles — single responsibility, open/closed extension
- Use composition to build complex behavior from simple pieces

---

## Abstraction Guidelines

### When to Abstract

- **Two or more occurrences** — If you write similar code twice, extract it
- **Complex logic** — Encapsulate non-obvious algorithms behind clear interfaces
- **Cross-cutting concerns** — Logging, caching, validation patterns

### How to Abstract (Zero Allocation)

```csharp
// ✅ Value-type abstraction - no heap allocation
public readonly struct ValidationResult
{
    public readonly bool IsValid;
    public readonly string ErrorMessage;
    private readonly int _hash;

    public ValidationResult(bool isValid, string errorMessage = null)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
        _hash = Objects.HashCode(isValid, errorMessage);
    }
}

// ✅ Static utility methods - no allocation
public static class CollectionExtensions
{
    public static bool TryGetFirst<T>(this IList<T> list, out T result)
    {
        if (list.Count > 0)
        {
            result = list[0];
            return true;
        }
        result = default;
        return false;
    }
}

// ✅ Generic constraint-based abstraction
public static void ProcessAll<T>(IList<T> items) where T : IProcessable
{
    for (int i = 0; i < items.Count; i++)
    {
        items[i].Process();
    }
}
```

### Abstraction Anti-Patterns

````csharp
// ❌ Class when struct suffices - unnecessary allocation
public class ValidationResult { }

// ❌ Closure-capturing delegate factory
public Func<T> CreateGetter<T>(T value) => () => value;  // Allocates!

// ❌ Over-abstraction - adds complexity without value
public interface IStringProvider { string GetString(); }
public class ConstantStringProvider : IStringProvider { ... }  // Just use the string!

---

## Mandatory Patterns

### 1. Prefer Value Types

Use `readonly struct` for small data containers:

```csharp
// ✅ Value type with cached hash
public readonly struct FastVector2Int : IEquatable<FastVector2Int>
{
    public readonly int x;
    public readonly int y;
    private readonly int _hash;

    public FastVector2Int(int x, int y)
    {
        this.x = x;
        this.y = y;
        _hash = Objects.HashCode(x, y);  // Compute once
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => _hash;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(FastVector2Int other)
    {
        return _hash == other._hash && x == other.x && y == other.y;
    }
}
````

**When to use structs:**

- Data under ~16 bytes
- No inheritance needed
- Short-lived or frequently created
- Used as dictionary keys (cache the hash!)

### 2. No Closures

Closures allocate heap objects. Never use them in hot paths:

```csharp
// ❌ Captures searchId - allocates closure
Item found = list.Find(item => item.Id == searchId);

// ❌ Captures 'this' - allocates closure
items.RemoveAll(x => x.Owner == this);

// ✅ Explicit loop - zero allocation
Item found = null;
for (int i = 0; i < list.Count; i++)
{
    if (list[i].Id == searchId)
    {
        found = list[i];
        break;
    }
}
```

**For cached delegates, use `static` lambdas:**

```csharp
// ✅ Static lambda - no capture, single allocation at class load
private static readonly Func<Type, FieldInfo[]> FieldsFactory =
    static type => type.GetFields(BindingFlags.Instance | BindingFlags.Public);

// Usage in ConcurrentDictionary
fields = FieldCache.GetOrAdd(type, FieldsFactory);
```

### 2a. Delegate Assignment in Loops

Assigning a delegate in a loop allocates each iteration:

```csharp
// ❌ BAD: Allocates 52 bytes per iteration (13MB for 256K iterations!)
for (int i = 0; i < count; i++)
{
    Func<int> fn = MyFunction;  // Boxing each iteration
    result += fn();
}

// ✅ GOOD: Assign once outside loop
Func<int> fn = MyFunction;
for (int i = 0; i < count; i++)
{
    result += fn();
}
```

### 2b. Params Array Trap

Methods with `params` allocate an array every call:

```csharp
// ❌ BAD: Allocates 36 bytes per call
Mathf.Max(a, b, c);  // Calls Max(params int[] args)

// ✅ GOOD: Chain 2-argument overloads (zero allocation)
Mathf.Max(Mathf.Max(a, b), c);
```

### 3. Zero Dynamic Memory Allocation

Use pooled collections for ALL temporary allocations:

```csharp
// ❌ Allocates new list every call
public List<Enemy> GetActiveEnemies()
{
    List<Enemy> result = new List<Enemy>();
    // ...
    return result;
}

// ✅ Caller provides buffer, zero allocation
public void GetActiveEnemies(List<Enemy> result)
{
    result.Clear();
    for (int i = 0; i < enemies.Count; i++)
    {
        Enemy enemy = enemies[i];
        if (enemy.IsActive)
        {
            result.Add(enemy);
        }
    }
}

// ✅ Usage with pooling
using var lease = Buffers<Enemy>.List.Get(out List<Enemy> activeEnemies);
GetActiveEnemies(activeEnemies);
```

### 4. Use Buffer Patterns

**Collection Pooling:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

// List pooling
using var listLease = Buffers<T>.List.Get(out List<T> buffer);

// HashSet pooling
using var setLease = Buffers<T>.HashSet.Get(out HashSet<T> buffer);

// StringBuilder pooling
using var sbLease = Buffers.StringBuilder.Get(out StringBuilder sb);
```

**Array Pooling - Choose Correctly:**

| Pool                       | Use Case                                  | Returns            |
| -------------------------- | ----------------------------------------- | ------------------ |
| `WallstopArrayPool<T>`     | Fixed/constant sizes only                 | Exact size         |
| `WallstopFastArrayPool<T>` | Fixed sizes, unmanaged types, no clearing | Exact size         |
| `SystemArrayPool<T>`       | Variable/dynamic sizes                    | At least requested |

```csharp
// Fixed size (e.g., PRNG state buffers)
using PooledArray<ulong> pooled = WallstopArrayPool<ulong>.Get(4, out ulong[] state);

// Variable size (e.g., sorting buffers)
using PooledArray<T> pooled = SystemArrayPool<T>.Get(list.Count, out T[] temp);
for (int i = 0; i < pooled.Length; i++)  // Use pooled.Length, NOT buffer.Length!
{
    temp[i] = list[i];
}
```

### 5. Avoid Reflection — Use Direct Access

**Reflection is an ABSOLUTE LAST RESORT.** Always prefer architectures that don't require it.

#### Priority Order (Use First Available)

1. **Direct access** — Expose members directly or via interfaces
2. **`internal` + `[InternalsVisibleTo]`** — For cross-assembly access within the package
3. **Generic constraints** — Use `where T : IMyInterface` instead of runtime type checks
4. **Redesign** — If you need reflection, reconsider the architecture
5. **ReflectionHelpers** — Only when accessing external/Unity code you don't control

#### ✅ Prefer: Direct Access Patterns

```csharp
// ❌ Reflection to access private field
FieldInfo field = type.GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance);
IList items = (IList)field.GetValue(target);

// ✅ Expose via internal + InternalsVisibleTo
internal IList Items => _items;

// In AssemblyInfo.cs or .asmdef:
[assembly: InternalsVisibleTo("WallstopStudios.UnityHelpers.Editor")]
[assembly: InternalsVisibleTo("WallstopStudios.UnityHelpers.Tests.Editor")]
```

```csharp
// ❌ Reflection to call method by name
MethodInfo method = type.GetMethod("Process");
method.Invoke(target, args);

// ✅ Use interface
public interface IProcessor
{
    void Process(ProcessArgs args);
}

// ✅ Or use delegate/action
public Action<ProcessArgs> OnProcess { get; set; }
```

```csharp
// ❌ Reflection to check type capabilities
if (type.GetMethod("Clone") != null) { ... }

// ✅ Use interface constraint
public void CloneItems<T>(IList<T> items) where T : ICloneable
{
    for (int i = 0; i < items.Count; i++)
    {
        T clone = (T)items[i].Clone();
    }
}
```

#### When Reflection Is Unavoidable

Only use reflection when accessing code you **cannot modify**:

- Unity's internal APIs (e.g., `SerializedProperty` internals)
- Third-party libraries without source access
- Runtime type discovery for plugin systems

**When you must use reflection, use ReflectionHelpers:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

// ✅ Cached field lookup (for Unity/external code only)
if (ReflectionHelpers.TryGetField(type, "fieldName", out FieldInfo field))
{
    object value = field.GetValue(target);
}

// ✅ Cached property lookup
if (ReflectionHelpers.TryGetProperty(type, "PropertyName", out PropertyInfo prop))
{
    object value = prop.GetValue(target);
}

// ✅ Cached type resolution
Type resolved = ReflectionHelpers.TryResolveType("MyNamespace.MyType");

// ✅ Get derived types (uses TypeCache in editor)
IEnumerable<Type> derived = ReflectionHelpers.GetTypesDerivedFrom<IMyInterface>();
```

#### Custom Reflection Caching (Last Resort)

If you must cache reflection for external APIs:

```csharp
#if SINGLE_THREADED
private static readonly Dictionary<Type, Func<object>> FactoryCache = new();
#else
private static readonly ConcurrentDictionary<Type, Func<object>> FactoryCache = new();
#endif

public static Func<object> GetFactory(Type type)
{
#if SINGLE_THREADED
    if (!FactoryCache.TryGetValue(type, out Func<object> factory))
    {
        factory = CreateFactory(type);
        FactoryCache[type] = factory;
    }
    return factory;
#else
    return FactoryCache.GetOrAdd(type, static t => CreateFactory(t));
#endif
}
```

### 6. Aggressive Inlining for Hot Paths

Mark frequently-called small methods:

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public override int GetHashCode() => _hash;

[MethodImpl(MethodImplOptions.AggressiveInlining)]
public bool Equals(FastVector2Int other)
{
    return _hash == other._hash && x == other.x && y == other.y;
}

[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static bool operator ==(FastVector2Int lhs, FastVector2Int rhs)
{
    return lhs.Equals(rhs);
}
```

### 7. Avoid foreach Boxing on Collections

Unity's old Mono compiler boxes enumerators for `List<T>` (24 bytes per loop). Arrays are optimized but Lists are not:

```csharp
// ❌ BAD: Allocates 24 bytes on List<T>
foreach (var item in myList) { }

// ✅ GOOD: Zero allocation
for (int i = 0; i < myList.Count; i++)
{
    var item = myList[i];
}

// Note: foreach on arrays is OK (Mono optimizes this)
foreach (var item in myArray) { }  // Zero allocation
```

**For non-indexable collections (HashSet, Dictionary):**

```csharp
// ✅ Use struct enumerator directly
var enumerator = hashSet.GetEnumerator();
while (enumerator.MoveNext())
{
    var element = enumerator.Current;
    // process
}
```

### 8. Implement IEquatable<T> to Avoid Boxing

Without `IEquatable<T>`, struct comparisons in collections cause boxing:

```csharp
// ❌ BAD: Allocates 4MB for 128K Contains calls!
public struct BadStruct
{
    public int X, Y;
}

var list = new List<BadStruct>();
list.Contains(someStruct);  // Boxes twice per call!

// ✅ GOOD: Zero allocation
public struct GoodStruct : IEquatable<GoodStruct>
{
    public int X, Y;

    public bool Equals(GoodStruct other) => X == other.X && Y == other.Y;

    public override bool Equals(object obj) =>
        obj is GoodStruct other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public static bool operator ==(GoodStruct a, GoodStruct b) => a.Equals(b);
    public static bool operator !=(GoodStruct a, GoodStruct b) => !a.Equals(b);
}
```

### 9. Enum Dictionary Keys Cause Boxing

Enum keys box on every lookup unless you provide a custom comparer:

```csharp
// ❌ BAD: Allocates 4.5MB for 128K lookups!
Dictionary<MyEnum, string> dict = new Dictionary<MyEnum, string>();
var value = dict[MyEnum.SomeValue];  // Boxing per lookup!

// ✅ GOOD: Custom comparer (zero allocation)
public struct MyEnumComparer : IEqualityComparer<MyEnum>
{
    public bool Equals(MyEnum x, MyEnum y) => x == y;
    public int GetHashCode(MyEnum obj) => (int)obj;
}

var dict = new Dictionary<MyEnum, string>(new MyEnumComparer());

// ✅ ALTERNATIVE: Cast to int
Dictionary<int, string> dict = new Dictionary<int, string>();
dict[(int)MyEnum.SomeValue] = "value";
```

---

## Editor Tooling Requirements

Editor code runs every frame when inspectors are visible. Apply ALL performance patterns:

```csharp
// ❌ Allocates every OnGUI call
public override void OnInspectorGUI()
{
    List<string> options = GetOptions().ToList();  // Allocation!
    int selected = EditorGUILayout.Popup("Option", current, options.ToArray());  // More allocations!
}

// ✅ Cache everything, pool temporaries
private static readonly GUIContent TitleContent = new GUIContent("Option");
private string[] _cachedOptions;
private int _cachedOptionsHash;

public override void OnInspectorGUI()
{
    int currentHash = ComputeOptionsHash();
    if (_cachedOptions == null || _cachedOptionsHash != currentHash)
    {
        using var lease = Buffers<string>.List.Get(out List<string> options);
        GetOptions(options);
        _cachedOptions = options.ToArray();  // Only allocate when data changes
        _cachedOptionsHash = currentHash;
    }

    int selected = EditorGUILayout.Popup(TitleContent, current, _cachedOptions);
}
```

**Cache GUIContent, GUIStyle, and computed values:**

```csharp
private static readonly GUIContent Label = new GUIContent("Label", "Tooltip");
private static readonly GUIStyle BoxStyle = new GUIStyle("box");
private static readonly Color HighlightColor = new Color(0.3f, 0.6f, 1f);
```

---

## Bug Fix Requirements

When fixing bugs, you MUST:

1. **Not regress performance** - If existing code is allocation-free, the fix must be too
2. **Improve if possible** - If fixing allocating code, make it allocation-free
3. **Verify with patterns** - Apply all patterns from this document

```csharp
// Bug: NullReferenceException when list is empty
// ❌ Bad fix - introduces allocation
public Item GetFirst()
{
    return items.FirstOrDefault();  // LINQ allocates!
}

// ✅ Good fix - maintains zero allocation
public Item GetFirst()
{
    return items.Count > 0 ? items[0] : null;
}
```

---

## Forbidden Patterns

### Never Use in Hot Paths

| Pattern                                | Problem                        | Alternative                              |
| -------------------------------------- | ------------------------------ | ---------------------------------------- |
| LINQ (`.Where`, `.Select`, `.Any`)     | Iterator + delegate allocation | `for` loop                               |
| `string.Format()` / interpolation      | String allocation              | `StringBuilder` or cache                 |
| `new List<T>()`                        | Heap allocation                | `Buffers<T>.List.Get()`                  |
| Lambda capturing locals                | Closure allocation             | Static lambda or explicit loop           |
| Boxing (`object x = struct`)           | Heap allocation                | Generic methods                          |
| `foreach` on `List<T>` (Mono)          | Enumerator boxing (24 bytes)   | `for` with indexer                       |
| `foreach` on non-generic `IEnumerable` | Enumerator allocation          | `for` with indexer                       |
| `params` methods                       | Array allocation per call      | Chain 2-arg overloads                    |
| Delegate assignment in loops           | Boxing each iteration          | Assign once outside loop                 |
| Enum dictionary keys                   | Boxing per lookup              | Custom `IEqualityComparer` or cast       |
| Struct without `IEquatable<T>`         | Boxing in collections          | Implement `IEquatable<T>`                |
| Reflection                             | Slow, fragile, uncached        | Direct access, interfaces, generics      |
| Raw `GetField`/`GetMethod`             | Uncached, repeated lookups     | `ReflectionHelpers` (external APIs only) |
| Duplicated code patterns               | Maintenance burden             | Extract to value-type abstraction        |

### Avoid These Allocations

```csharp
// ❌ String concatenation in loops
string result = "";
for (int i = 0; i < items.Count; i++)
{
    result += items[i].Name;  // New string each iteration!
}

// ✅ StringBuilder
using var lease = Buffers.StringBuilder.Get(out StringBuilder sb);
for (int i = 0; i < items.Count; i++)
{
    sb.Append(items[i].Name);
}
string result = sb.ToString();
```

---

## Modern C# Zero-Allocation Patterns

### Span<T> for Zero-Allocation Slicing

`Span<T>` provides allocation-free views into contiguous memory:

```csharp
// ✅ Zero-allocation array slicing
byte[] buffer = GetBuffer();
Span<byte> header = buffer.AsSpan(0, 8);      // No allocation
Span<byte> payload = buffer.AsSpan(8);         // No allocation

// ✅ Zero-allocation string parsing
public bool TryParseCoordinates(ReadOnlySpan<char> input, out int x, out int y)
{
    x = y = 0;
    int commaIndex = input.IndexOf(',');
    if (commaIndex < 0) return false;

    ReadOnlySpan<char> xPart = input.Slice(0, commaIndex);
    ReadOnlySpan<char> yPart = input.Slice(commaIndex + 1);

    return int.TryParse(xPart, out x) && int.TryParse(yPart, out y);
}

// Usage
string coord = "123,456";
if (TryParseCoordinates(coord.AsSpan(), out int x, out int y)) { }
```

### stackalloc for Small Buffers

```csharp
// ✅ Stack allocation for small, fixed-size buffers
public void ProcessSmallData(int count)
{
    // Stack allocate if small, heap allocate if large
    Span<int> buffer = count <= 64
        ? stackalloc int[count]
        : new int[count];

    // Use buffer...
}
```

**Note**: `Span<T>` is a `ref struct` — cannot be used in async methods or stored in fields.

### Value Tuples Over Classes

```csharp
// ❌ BAD: Allocates on heap
public Tuple<int, int> FindMinMax(int[] input) { }

// ✅ GOOD: Value type, stack allocated
public (int min, int max) FindMinMax(int[] input)
{
    int min = int.MaxValue;
    int max = int.MinValue;
    for (int i = 0; i < input.Length; i++)
    {
        if (input[i] < min) min = input[i];
        if (input[i] > max) max = input[i];
    }
    return (min, max);
}

// Usage with deconstruction
var (minimum, maximum) = FindMinMax(values);
```

### Pass Large Structs by Reference

```csharp
// ❌ BAD: Copies entire struct
public void ProcessData(LargeStruct data) { }

// ✅ GOOD: Read-only reference, no copy
public void ProcessData(in LargeStruct data) { }

// ✅ GOOD: Modifiable reference
public void UpdateData(ref LargeStruct data) { }

// ✅ GOOD: Return by reference
public ref readonly LargeStruct GetCachedData() => ref _cachedData;
```

### Implement IEquatable<T> to Avoid Boxing

```csharp
// ❌ BAD: GetHashCode and Equals cause boxing if not overridden
public struct BadStruct { public int Value; }

// ✅ GOOD: Implement IEquatable<T> to avoid boxing
public readonly struct GoodStruct : IEquatable<GoodStruct>
{
    public readonly int Value;
    private readonly int _hash;

    public GoodStruct(int value)
    {
        Value = value;
        _hash = value.GetHashCode();
    }

    public bool Equals(GoodStruct other) => Value == other.Value;
    public override bool Equals(object obj) => obj is GoodStruct other && Equals(other);
    public override int GetHashCode() => _hash;
}
```

---

## List Pre-allocation

Lists grow by **doubling capacity**, causing allocations and copies:

```csharp
// ❌ BAD: Multiple reallocations as list grows
List<Enemy> enemies = new List<Enemy>();
for (int i = 0; i < 10000; i++)
{
    enemies.Add(GetEnemy(i));  // Reallocates at 4, 8, 16, 32...
}

// ✅ GOOD: Pre-allocate when size is known
List<Enemy> enemies = new List<Enemy>(10000);
for (int i = 0; i < 10000; i++)
{
    enemies.Add(GetEnemy(i));  // No reallocations
}
```

**Memory impact**: Without pre-allocation, lists average **33% wasted capacity** and during resize temporarily use **3x memory**.

---

## Thread Safety Patterns

Use conditional compilation for thread-safe vs single-threaded builds:

```csharp
#if SINGLE_THREADED
private static readonly Dictionary<Type, object> Cache = new();
#else
using System.Collections.Concurrent;
private static readonly ConcurrentDictionary<Type, object> Cache = new();
#endif
```

For primitives, use `Volatile` or `Interlocked`:

```csharp
private static int _counter;

// Thread-safe increment
int newValue = Interlocked.Increment(ref _counter);

// Thread-safe read/write
int current = Volatile.Read(ref _counter);
Volatile.Write(ref _counter, newValue);
```

---

## Quick Checklist

Before submitting any code, verify:

- [ ] No LINQ in hot paths
- [ ] No closures capturing variables
- [ ] All temporary collections use `Buffers<T>`
- [ ] All temporary arrays use appropriate pool
- [ ] No reflection on code we control (use `internal` + `[InternalsVisibleTo]`)
- [ ] Reflection on external APIs uses `ReflectionHelpers`
- [ ] Value types used where appropriate
- [ ] Hash codes cached for dictionary keys
- [ ] Editor code caches GUIContent/GUIStyle
- [ ] `[MethodImpl(MethodImplOptions.AggressiveInlining)]` on hot paths
- [ ] Thread safety uses conditional compilation pattern
- [ ] No duplicated code — extract common patterns to abstractions
- [ ] Abstractions are lightweight (prefer `readonly struct` or `static`)

---

## Related Skills

- [defensive-programming](./defensive-programming.md) — Error handling patterns (MANDATORY companion skill)
- [unity-performance-patterns](./unity-performance-patterns.md) — Unity-specific optimizations (MANDATORY for Unity code)
- [profile-debug-performance](./profile-debug-performance.md) — Profiling and debugging performance
- [use-pooling](./use-pooling.md) — Detailed collection pooling patterns
- [use-array-pool](./use-array-pool.md) — Array pool selection guide
- [refactor-to-zero-alloc](./refactor-to-zero-alloc.md) — Migration guide for existing code
- [performance-audit](./performance-audit.md) — Performance review checklist
- [create-editor-tool](./create-editor-tool.md) — Editor-specific patterns
