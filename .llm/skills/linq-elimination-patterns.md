# Skill: LINQ Elimination Patterns

<!-- trigger: linq, where, select, any, first, iterator | Converting LINQ to zero-allocation loops | Performance -->

**Trigger**: When eliminating LINQ from code to achieve zero-allocation patterns. Use this skill when:

- Converting existing LINQ code to explicit loops
- Understanding why LINQ is forbidden in this codebase
- Implementing zero-allocation visitor patterns
- Refactoring code for hot path performance

---

## When to Use This Skill

Use this skill when you encounter LINQ methods in code that needs to be allocation-free. LINQ is forbidden in runtime code because every LINQ method allocates:

- **Iterator object** - The `IEnumerator<T>` state machine
- **Delegate allocation** - Every lambda/predicate passed
- **Closure objects** - When lambdas capture variables

Even "streaming" LINQ (`IEnumerable` without `ToList()`) still allocates the iterator and delegate on every call.

---

## LINQ is FORBIDDEN in Runtime Code

**LINQ is NEVER acceptable in runtime code.** All LINQ operations must be converted to explicit loops or visitor patterns.

### LINQ Allocation Breakdown

| Method       | Allocations                          |
| ------------ | ------------------------------------ |
| `.Where()`   | WhereIterator + delegate             |
| `.Select()`  | SelectIterator + delegate            |
| `.Any()`     | Delegate (no iterator if early exit) |
| `.First()`   | Delegate                             |
| `.ToList()`  | New `List<T>`                        |
| `.ToArray()` | New `T[]`                            |
| `.Count()`   | Enumeration (may allocate)           |
| `.Sum()`     | Delegate                             |
| `.OrderBy()` | Buffer + comparer                    |

---

## Zero-Allocation Visitor Pattern

Instead of LINQ, use explicit loops with the visitor pattern:

```csharp
// FORBIDDEN: LINQ streaming (still allocates iterator + delegate!)
IEnumerable<Event> events = GetEventsLazy();
foreach (Event e in events.Where(e => e.IsRecent))
{
    ProcessEvent(e);
}

// REQUIRED: Zero-allocation visitor pattern
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

// BETTER: Inline visitor logic to avoid delegate allocation
for (int i = 0; i < events.Count; i++)
{
    Event e = events[i];
    if (e.IsRecent)
    {
        ProcessEvent(e);
    }
}

// BEST: Generic visitor with ref struct for complex state
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

---

## Pattern: Where + FirstOrDefault

```csharp
// BEFORE: Allocates iterator and delegate
Enemy target = enemies.Where(e => e.IsAlive && e.Team != myTeam)
                      .FirstOrDefault();

// AFTER: Zero allocation
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

---

## Pattern: Where + ToList

```csharp
// BEFORE: Allocates iterator, delegate, AND new list
List<Enemy> activeEnemies = enemies.Where(e => e.IsActive).ToList();

// AFTER: Zero allocation with pooling
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

---

## Pattern: Select + ToArray

```csharp
// BEFORE: Allocates iterator, delegate, and array
string[] names = items.Select(x => x.Name).ToArray();

// AFTER: Zero allocation (if size known)
using PooledArray<string> pooled = SystemArrayPool<string>.Get(items.Count, out string[] names);
for (int i = 0; i < pooled.Length; i++)
{
    names[i] = items[i].Name;
}
```

---

## Pattern: Any

```csharp
// BEFORE: Allocates iterator and delegate
bool hasActive = enemies.Any(e => e.IsActive);

// AFTER: Zero allocation
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

---

## Pattern: Count with Predicate

```csharp
// BEFORE: Allocates iterator and delegate
int activeCount = enemies.Count(e => e.IsActive);

// AFTER: Zero allocation
int activeCount = 0;
for (int i = 0; i < enemies.Count; i++)
{
    if (enemies[i].IsActive)
    {
        activeCount++;
    }
}
```

---

## Pattern: Sum

```csharp
// BEFORE: Allocates delegate
int totalDamage = attacks.Sum(a => a.Damage);

// AFTER: Zero allocation
int totalDamage = 0;
for (int i = 0; i < attacks.Count; i++)
{
    totalDamage += attacks[i].Damage;
}
```

---

## Pattern: OrderBy + ToList

```csharp
// BEFORE: Multiple allocations
List<Enemy> sorted = enemies.OrderBy(e => e.Distance).ToList();

// AFTER: Zero allocation with pooling and in-place sort
using var lease = Buffers<Enemy>.List.Get(out List<Enemy> sorted);
for (int i = 0; i < enemies.Count; i++)
{
    sorted.Add(enemies[i]);
}
sorted.Sort((a, b) => a.Distance.CompareTo(b.Distance));
```

---

## Pattern: GroupBy

```csharp
// BEFORE: Allocates grouping objects, iterator, delegate
var groups = items.GroupBy(x => x.Category);

// AFTER: Manual grouping with pooled dictionary
using var dictLease = Buffers<Category, List<Item>>.Dictionary.Get(
    out Dictionary<Category, List<Item>> groups);

for (int i = 0; i < items.Count; i++)
{
    Item item = items[i];
    if (!groups.TryGetValue(item.Category, out List<Item> group))
    {
        // Note: Inner lists need careful lifecycle management
        group = new List<Item>();
        groups[item.Category] = group;
    }
    group.Add(item);
}
```

---

## Pattern: Distinct

```csharp
// BEFORE: Allocates iterator and HashSet internally
var unique = items.Distinct().ToList();

// AFTER: Zero allocation with pooled HashSet
using var hashLease = Buffers<Item>.HashSet.Get(out HashSet<Item> seen);
using var listLease = Buffers<Item>.List.Get(out List<Item> unique);

for (int i = 0; i < items.Count; i++)
{
    if (seen.Add(items[i]))
    {
        unique.Add(items[i]);
    }
}
```

---

## Pattern: Aggregate/Reduce

```csharp
// BEFORE: Allocates delegate
int product = numbers.Aggregate(1, (acc, n) => acc * n);

// AFTER: Zero allocation
int product = 1;
for (int i = 0; i < numbers.Count; i++)
{
    product *= numbers[i];
}
```

---

## Pattern: All

```csharp
// BEFORE: Allocates delegate
bool allValid = items.All(x => x.IsValid);

// AFTER: Zero allocation
bool allValid = true;
for (int i = 0; i < items.Count; i++)
{
    if (!items[i].IsValid)
    {
        allValid = false;
        break;
    }
}
```

---

## Pattern: Take/Skip

```csharp
// BEFORE: Allocates iterators
var firstFive = items.Skip(10).Take(5).ToList();

// AFTER: Zero allocation with bounds checking
using var lease = Buffers<Item>.List.Get(out List<Item> result);
int start = Math.Min(10, items.Count);
int end = Math.Min(start + 5, items.Count);
for (int i = start; i < end; i++)
{
    result.Add(items[i]);
}
```

---

## When LINQ Might Be Acceptable (Extremely Rare)

LINQ is only acceptable when ALL of these conditions are met:

1. **Editor-only code** - Never called at runtime
2. **One-time initialization** - Called once during app startup, not per-frame
3. **Complexity significantly reduced** - The LINQ version is dramatically clearer
4. **Documented exception** - Comment explains why LINQ was chosen

```csharp
// Editor-only, one-time initialization - LINQ acceptable with documentation
// LINQ Exception: Editor tool initialization, called once on domain reload
private static readonly Dictionary<Type, MethodInfo[]> CachedMethods =
    AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .ToDictionary(t => t, t => t.GetMethods());
```

**When in doubt: DO NOT use LINQ.** The explicit loop is always safe.

---

## Quick Conversion Reference

| LINQ Pattern                  | Zero-Allocation Replacement            |
| ----------------------------- | -------------------------------------- |
| `.Where(predicate)`           | `for` loop with `if` check             |
| `.Select(transform)`          | `for` loop building result             |
| `.FirstOrDefault(predicate)`  | `for` loop with early `break`          |
| `.Any(predicate)`             | `for` loop returning `bool`            |
| `.All(predicate)`             | `for` loop with early `return false`   |
| `.Count(predicate)`           | `for` loop incrementing counter        |
| `.Sum(selector)`              | `for` loop accumulating                |
| `.OrderBy(key).ToList()`      | Copy to pooled list, then `Sort()`     |
| `.GroupBy(key)`               | Pooled dictionary with manual grouping |
| `.Distinct()`                 | Pooled HashSet for deduplication       |
| `.Take(n)` / `.Skip(n)`       | `for` loop with index bounds           |
| `.ToList()` / `.ToArray()`    | Pooled collection with explicit copy   |
| `.Aggregate(seed, func)`      | `for` loop with accumulator variable   |
| `.SelectMany(collection)`     | Nested `for` loops                     |
| `.Concat(other)`              | Two sequential `for` loops             |
| `.Zip(other, resultSelector)` | Single `for` loop with dual indexing   |

---

## Related Skills

- [refactor-to-zero-alloc](./refactor-to-zero-alloc.md) - Complete refactoring workflow
- [high-performance-csharp](./high-performance-csharp.md) - Core performance philosophy
- [avoid-allocations](./avoid-allocations.md) - Closure and boxing avoidance
- [use-pooling](./use-pooling.md) - Collection pooling patterns
- [memory-allocation-traps](./memory-allocation-traps.md) - Hidden allocation sources
