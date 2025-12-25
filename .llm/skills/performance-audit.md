# Skill: Performance Audit

**Trigger**: When reviewing or writing performance-sensitive code, or when asked to optimize.

---

## Allocation Checklist

### ❌ LINQ in Hot Paths

LINQ methods allocate iterators and delegates:

```csharp
// ❌ Allocates: iterator, delegate, list
List<Enemy> active = enemies.Where(e => e.Health > 0).ToList();

// ❌ Allocates delegate
bool hasTarget = items.Any(x => x.Id == targetId);

// ❌ Allocates iterator and delegate
string name = items.Select(x => x.Name).FirstOrDefault();
```

**Fix**: Use explicit loops:

```csharp
// ✅ Zero allocations with pooled buffer
using var lease = Buffers<Enemy>.List.Get(out List<Enemy> active);
for (int i = 0; i < enemies.Count; i++)
{
    Enemy enemy = enemies[i];
    if (enemy.Health > 0)
    {
        active.Add(enemy);
    }
}
```

### ❌ Closures That Capture Variables

Closures allocate heap objects:

```csharp
// ❌ Captures searchId, allocates closure
Item found = list.Find(item => item.Id == searchId);

// ❌ Captures this, allocates closure
items.RemoveAll(x => x.Owner == this);
```

**Fix**: Use explicit loops or pass state via parameters:

```csharp
// ✅ No allocation
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

### ❌ String Operations in Loops

```csharp
// ❌ Creates new string each iteration
string result = "";
for (int i = 0; i < items.Count; i++)
{
    result += items[i].Name;
}

// ❌ String.Format allocates
string msg = string.Format("Value: {0}", value);
```

**Fix**: Use StringBuilder or cache strings:

```csharp
// ✅ Single allocation
StringBuilder sb = new StringBuilder();
for (int i = 0; i < items.Count; i++)
{
    sb.Append(items[i].Name);
}
string result = sb.ToString();
```

### ❌ Frequent Collection Allocations

```csharp
// ❌ New allocation every call
void ProcessItems()
{
    List<Item> temp = new List<Item>();
    // ...
}
```

**Fix**: Use pooled collections:

```csharp
// ✅ Zero allocation (pooled)
void ProcessItems()
{
    using var lease = Buffers<Item>.List.Get(out List<Item> temp);
    // ...
}
```

---

## Pooling Patterns

### Collection Pooling

```csharp
// List pooling
using var listLease = Buffers<T>.List.Get(out List<T> buffer);

// HashSet pooling
using var setLease = Buffers<T>.HashSet.Get(out HashSet<T> buffer);
```

### Array Pooling

See the [Array Pooling Guide](use-array-pool.md) for detailed guidance.

Quick reference:

- **Fixed sizes** → `WallstopArrayPool<T>`
- **Variable sizes** → `SystemArrayPool<T>`

---

## Struct vs Class

### Use Structs When

- Data container under ~16 bytes
- No inheritance needed
- Short-lived, frequently created
- Value semantics desired

```csharp
// ✅ Good struct candidate
public readonly struct Vector2Int
{
    public readonly int X;
    public readonly int Y;
}
```

### Avoid Boxing

```csharp
// ❌ Boxing occurs
object boxed = myStruct;
IComparable comparable = myStruct;

// ❌ Non-generic collections box
ArrayList list = new ArrayList();
list.Add(myStruct);  // Boxing

// ✅ Use generic collections
List<MyStruct> list = new List<MyStruct>();
```

---

## Stack Allocation

```csharp
// ✅ Stack-allocated array for small fixed sizes
Span<int> buffer = stackalloc int[16];

// ✅ Value tuples instead of Tuple<>
(int x, int y) point = (10, 20);
```

---

## Editor Code Is NOT Exempt

Editor tools must also be performant:

| Context              | Concern                          |
| -------------------- | -------------------------------- |
| Inspector drawing    | Called every frame when visible  |
| Asset processing     | May handle thousands of assets   |
| Scene view callbacks | Called frequently during editing |

---

## Profiling Checklist

1. **Identify hot paths**: Code called frequently (Update, OnGUI, loops)
2. **Check allocations**: Use Unity Profiler's GC Alloc column
3. **Look for boxing**: Value types passed to `object` parameters
4. **Find LINQ usage**: Search for `.Where`, `.Select`, `.Any`, `.First`
5. **Check string operations**: `+` operator, `Format`, interpolation in loops
6. **Review collection creation**: `new List<>`, `new Dictionary<>` in methods

---

## Quick Wins

| Pattern                          | Replacement                      |
| -------------------------------- | -------------------------------- |
| `items.Where(x => ...).ToList()` | `for` loop with pooled list      |
| `items.Any(x => ...)`            | `for` loop with early return     |
| `items.FirstOrDefault(x => ...)` | `for` loop with `break`          |
| `new List<T>()` in method        | `Buffers<T>.List.Get()`          |
| `string.Format()` in loop        | `StringBuilder`                  |
| `items.Count()` on IEnumerable   | Cache count or use `ICollection` |

---

## Example Transformation

### Before (Allocating)

```csharp
public List<Enemy> GetActiveEnemiesInRange(float range)
{
    return enemies
        .Where(e => e.Health > 0 && e.Distance < range)
        .ToList();
}
```

### After (Zero-Allocation)

```csharp
public void GetActiveEnemiesInRange(float range, List<Enemy> results)
{
    results.Clear();
    for (int i = 0; i < enemies.Count; i++)
    {
        Enemy enemy = enemies[i];
        if (enemy.Health > 0 && enemy.Distance < range)
        {
            results.Add(enemy);
        }
    }
}

// Usage with pooling
using var lease = Buffers<Enemy>.List.Get(out List<Enemy> activeEnemies);
GetActiveEnemiesInRange(10f, activeEnemies);
```
