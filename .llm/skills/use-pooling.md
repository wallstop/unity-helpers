# Skill: Use Pooling

**Trigger**: When working with frequently allocated collections to avoid GC pressure.

---

## Collection Pooling with Buffers&lt;T&gt;

### List Pooling

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

// Get a pooled list
using var lease = Buffers<Enemy>.List.Get(out List<Enemy> enemies);

// Use the list normally
foreach (Enemy e in GetAllEnemies())
{
    if (e.IsActive)
    {
        enemies.Add(e);
    }
}

ProcessEnemies(enemies);

// List is automatically returned to pool when lease is disposed
```

### HashSet Pooling

```csharp
using var lease = Buffers<int>.HashSet.Get(out HashSet<int> visited);

// Use the hashset normally
visited.Add(startNode);
while (queue.Count > 0)
{
    int node = queue.Dequeue();
    foreach (int neighbor in GetNeighbors(node))
    {
        if (visited.Add(neighbor))
        {
            queue.Enqueue(neighbor);
        }
    }
}

// HashSet is automatically returned to pool when lease is disposed
```

---

## Pattern: Zero-Allocation Method

### Before (Allocating)

```csharp
// ❌ Creates new list every call
public List<Enemy> GetEnemiesInRange(Vector2 center, float radius)
{
    List<Enemy> result = new List<Enemy>();
    foreach (Enemy enemy in allEnemies)
    {
        if (Vector2.Distance(center, enemy.Position) < radius)
        {
            result.Add(enemy);
        }
    }
    return result;
}
```

### After (Zero-Allocation)

```csharp
// ✅ Uses caller-provided list
public void GetEnemiesInRange(Vector2 center, float radius, List<Enemy> result)
{
    result.Clear();
    foreach (Enemy enemy in allEnemies)
    {
        if (Vector2.Distance(center, enemy.Position) < radius)
        {
            result.Add(enemy);
        }
    }
}

// Usage with pooling
using var lease = Buffers<Enemy>.List.Get(out List<Enemy> nearbyEnemies);
GetEnemiesInRange(playerPos, 10f, nearbyEnemies);
foreach (Enemy enemy in nearbyEnemies)
{
    // Process enemy
}
```

---

## Pattern: Temporary Processing

```csharp
public void ProcessItems()
{
    // Get pooled list for temporary use
    using var lease = Buffers<Item>.List.Get(out List<Item> validItems);

    // Filter items
    foreach (Item item in allItems)
    {
        if (item.IsValid)
        {
            validItems.Add(item);
        }
    }

    // Sort
    validItems.Sort((a, b) => a.Priority.CompareTo(b.Priority));

    // Process
    foreach (Item item in validItems)
    {
        ProcessItem(item);
    }

    // List automatically returned to pool
}
```

---

## Pattern: Nested Pooling

```csharp
public void ProcessGroups()
{
    using var groupLease = Buffers<ItemGroup>.List.Get(out List<ItemGroup> groups);

    foreach (ItemGroup group in GetGroups())
    {
        groups.Add(group);
    }

    foreach (ItemGroup group in groups)
    {
        // Nested pooled list
        using var itemLease = Buffers<Item>.List.Get(out List<Item> items);

        GetItemsInGroup(group, items);
        ProcessItems(items);

        // Inner list returned to pool
    }

    // Outer list returned to pool
}
```

---

## Pattern: Conditional Pooling

```csharp
public void MaybeProcessItems(bool shouldProcess)
{
    if (!shouldProcess)
    {
        return;
    }

    // Pool lease is only acquired when needed
    using var lease = Buffers<Item>.List.Get(out List<Item> items);

    // ... use items ...
}
```

---

## Pattern: Populating from IEnumerable

When materializing an `IEnumerable<T>` into a pooled list, **always use `AddRange`** instead of `foreach` + `Add`:

```csharp
// ❌ BAD: foreach allocates an enumerator, no capacity pre-allocation
using var lease = Buffers<T>.List.Get(out List<T> result);
foreach (T item in source)
{
    result.Add(item);  // May trigger multiple resizes
}

// ✅ GOOD: AddRange is optimized for performance
using var lease = Buffers<T>.List.Get(out List<T> result);
result.AddRange(source);
```

**Why AddRange is better:**

1. **Capacity pre-allocation**: If source is `ICollection<T>`, AddRange queries `Count` first
2. **Bulk copy**: For arrays and `List<T>`, uses `Array.Copy` which is much faster
3. **Potential zero-allocation**: May avoid enumerator allocation entirely
4. **Fewer resizes**: Pre-allocated capacity means fewer or no list resizes

### When to Use for Loop Instead

Use indexed `for` loops only when you must **transform** or **filter** each element:

```csharp
// ✅ TRANSFORMATION: Must use for loop
for (int i = 0; i < guids.Length; i++)
{
    paths.Add(ConvertGuidToPath(guids[i]));  // Transforming - can't use AddRange
}

// ✅ FILTERING: Must use for loop
for (int i = 0; i < items.Length; i++)
{
    if (items[i].IsValid)
    {
        filtered.Add(items[i]);  // Filtering - can't use AddRange
    }
}
```

| Scenario                | Use                                             |
| ----------------------- | ----------------------------------------------- |
| Copy all elements as-is | `AddRange(source)`                              |
| Transform each element  | `for` loop + `Add(Transform(item))`             |
| Filter elements         | `for` loop + conditional `Add`                  |
| Transform AND filter    | `for` loop + conditional `Add(Transform(item))` |

---

## Common Mistakes

### ❌ Forgetting `using`

```csharp
// ❌ Memory leak - list never returned to pool
var lease = Buffers<Item>.List.Get(out List<Item> items);
// ... use items ...
// lease never disposed!

// ✅ Always use 'using'
using var lease = Buffers<Item>.List.Get(out List<Item> items);
```

### ❌ Using List After Dispose

```csharp
List<Item> storedItems;

void Bad()
{
    using var lease = Buffers<Item>.List.Get(out List<Item> items);
    items.Add(new Item());
    storedItems = items;  // ❌ Storing reference to pooled list!
}

void UseLater()
{
    foreach (Item item in storedItems)  // ❌ List may be in use elsewhere!
    {
        // Undefined behavior
    }
}
```

### ❌ Returning Pooled List

```csharp
// ❌ Returns list that will be returned to pool
public List<Item> GetItems()
{
    using var lease = Buffers<Item>.List.Get(out List<Item> items);
    // ... populate items ...
    return items;  // ❌ List will be pooled when method exits!
}

// ✅ Return a copy or use out parameter
public List<Item> GetItems()
{
    using var lease = Buffers<Item>.List.Get(out List<Item> items);
    // ... populate items ...
    return new List<Item>(items);  // Return copy
}

// ✅ Or use out parameter pattern
public void GetItems(List<Item> result)
{
    result.Clear();
    // ... populate result ...
}
```

---

## Array Pooling

For array pooling, see the [Array Pooling Guide](./use-array-pool.md).

Quick summary:

- **Fixed sizes** → `WallstopArrayPool<T>.Get()`
- **Variable sizes** → `SystemArrayPool<T>.Get()`

---

## Unity GameObject Pooling

For GameObjects (bullets, enemies, effects), use Unity's `ObjectPool<T>`:

```csharp
using UnityEngine.Pool;

private ObjectPool<GameObject> _bulletPool;

void Awake()
{
    _bulletPool = new ObjectPool<GameObject>(
        createFunc: () => Instantiate(_bulletPrefab),
        actionOnGet: obj => obj.SetActive(true),
        actionOnRelease: obj => obj.SetActive(false),
        actionOnDestroy: obj => Destroy(obj),
        defaultCapacity: 50,
        maxSize: 200
    );
}

public GameObject SpawnBullet() => _bulletPool.Get();
public void ReturnBullet(GameObject bullet) => _bulletPool.Release(bullet);
```

See [unity-performance-patterns](./unity-performance-patterns.md) for more Unity-specific pooling.

---

## Performance Comparison

```csharp
// ❌ Allocates new list (GC pressure)
void ProcessAllocating()
{
    List<Item> items = new List<Item>();  // Allocation!
    // ... use items ...
}

// ✅ Zero allocation (uses pool)
void ProcessPooled()
{
    using var lease = Buffers<Item>.List.Get(out List<Item> items);
    // ... use items ...
}
```

| Approach          | First Call | Subsequent Calls |
| ----------------- | ---------- | ---------------- |
| `new List<T>()`   | Allocates  | Allocates        |
| `Buffers<T>.List` | Allocates  | Zero allocation  |

---

## When to Use Pooling

✅ **Use pooling for:**

- Hot paths (Update, OnGUI, tight loops)
- Temporary processing collections
- Methods called frequently
- Collections with short lifetimes

❌ **Don't use pooling for:**

- Long-lived collections (class fields)
- Collections returned to callers (unless copied)
- Small, infrequent allocations

---

## Related Skills

- [high-performance-csharp](./high-performance-csharp.md) — Core performance patterns
- [unity-performance-patterns](./unity-performance-patterns.md) — Unity GameObject pooling
- [use-array-pool](./use-array-pool.md) — Array pooling guide
- [refactor-to-zero-alloc](./refactor-to-zero-alloc.md) — Migration patterns
