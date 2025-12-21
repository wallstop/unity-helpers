# Skill: High-Performance C

**Trigger**: When implementing ANY new feature, fixing bugs, or writing editor tooling. This applies to ALL code in this repository.

---

## Core Philosophy

**Every code path should be allocation-free in steady state.** This includes:

- Runtime gameplay code
- Editor tooling and inspectors (called every frame when visible)
- Bug fixes (must not regress performance)
- Test utilities (may run thousands of iterations)

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
```

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
| `foreach` on non-generic `IEnumerable` | Enumerator allocation          | `for` with indexer                       |
| Reflection                             | Slow, fragile, uncached        | Direct access, interfaces, generics      |
| Raw `GetField`/`GetMethod`             | Uncached, repeated lookups     | `ReflectionHelpers` (external APIs only) |

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

---

## Related Skills

- [use-pooling](use-pooling.md) - Detailed collection pooling patterns
- [use-array-pool](use-array-pool.md) - Array pool selection guide
- [performance-audit](performance-audit.md) - Performance review checklist
