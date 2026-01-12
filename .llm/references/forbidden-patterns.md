# Forbidden Patterns Reference

This document consolidates all forbidden and recommended patterns from across the codebase. Use this as a single source of truth for pattern compliance.

---

## LINQ Patterns

LINQ methods allocate iterator objects and delegate objects on every call.

| Forbidden                                     | Use Instead                                   | Reason                              |
| --------------------------------------------- | --------------------------------------------- | ----------------------------------- |
| `.Where()`                                    | Explicit `for` loop with condition            | Allocates WhereIterator + delegate  |
| `.Select()`                                   | Explicit `for` loop with transform            | Allocates SelectIterator + delegate |
| `.Any()`                                      | Explicit `for` loop with `break`              | Allocates delegate                  |
| `.First()` / `.FirstOrDefault()`              | Explicit `for` loop with `break`              | Allocates delegate                  |
| `.ToList()` / `.ToArray()`                    | Use pooled collection                         | Creates new collection              |
| `.OrderBy()` / `.OrderByDescending()`         | `List.Sort()` with cached comparer            | Allocates buffer + comparer         |
| `.Count()` on IEnumerable                     | Track count manually or use `.Count` property | May allocate enumerator             |
| `.Sum()` / `.Average()` / `.Min()` / `.Max()` | Explicit loop with accumulator                | Allocates delegate                  |
| Chained LINQ (`.Where().Select().ToList()`)   | Single explicit loop                          | Multiple allocations compound       |

### LINQ vs Native Collection Methods

**Critical distinction**: Some methods that look like LINQ are actually native collection methods and do NOT allocate.

| Method Call                                   | Is LINQ? | Allocates?           | Notes                                        |
| --------------------------------------------- | -------- | -------------------- | -------------------------------------------- |
| `List<T>.ToArray()`                           | No       | Yes (new array)      | Native method, not System.Linq               |
| `IEnumerable<T>.ToArray()` (System.Linq)      | Yes      | Yes (array + buffer) | LINQ extension, avoid in hot paths           |
| `Array.Empty<T>()`                            | No       | No                   | Cached singleton, always safe                |
| `List<T>.Contains()`                          | No       | No                   | Native method                                |
| `IEnumerable<T>.Contains()` (System.Linq)     | Yes      | Maybe                | LINQ extension, may allocate enumerator      |
| `string.Concat(IEnumerable<string>)`          | No       | Yes (new string)     | Native BCL method, not LINQ                  |
| `List<T>.Exists(Predicate<T>)`                | No       | No                   | Native method, delegate passed directly      |
| `IEnumerable<T>.Any(Func<T,bool>)`            | Yes      | Yes (enumerator)     | LINQ extension, allocates                    |
| `Dictionary<K,V>.TryGetValue()`               | No       | No                   | Native method                                |
| `IEnumerable<T>.ToDictionary()` (System.Linq) | Yes      | Yes                  | LINQ extension, creates new dict + allocates |

**Rule of thumb**: If calling on a concrete type (`List<T>`, `Dictionary<K,V>`, `T[]`), check if it is a native method first. If calling on `IEnumerable<T>`, assume it is LINQ.

---

## Collection Building Patterns

| Forbidden                        | Use Instead                     | Reason                                    |
| -------------------------------- | ------------------------------- | ----------------------------------------- |
| `foreach` + `.Add()` on unknown  | `.AddRange()` when available    | `AddRange` pre-allocates and uses memcopy |
| `for` loop + `.Add()` repeatedly | Pre-size with capacity + `.Add` | Avoids resize/copy on every add           |
| Building without known capacity  | Pass capacity to constructor    | Avoids multiple internal resizes          |

### AddRange vs Foreach+Add

```csharp
// Forbidden - O(n) individual Add calls, potential resizes
foreach (var item in source)
{
    destination.Add(item);
}

// Preferred - Single operation, pre-allocates, uses Array.Copy
destination.AddRange(source);

// If source is IEnumerable<T> (not ICollection<T>), AddRange may still enumerate
// In that case, prefer explicit capacity + Add pattern:
destination.Capacity = destination.Count + expectedCount;
for (int i = 0; i < source.Length; i++)
{
    destination.Add(source[i]);
}
```

---

## Collection Iteration

| Forbidden                      | Use Instead                            | Reason                      |
| ------------------------------ | -------------------------------------- | --------------------------- |
| `foreach` on `List<T>` (Mono)  | `for (int i = 0; i < list.Count; i++)` | Boxes enumerator (24 bytes) |
| `foreach` on `Dictionary<K,V>` | Use struct enumerator directly         | Boxes enumerator            |
| `foreach` on `HashSet<T>`      | Use struct enumerator directly         | Boxes enumerator            |
| `foreach` on arrays            | OK (optimized by compiler)             | No allocation               |

### Struct Enumerator Pattern

```csharp
// Instead of foreach on non-array collections:
var enumerator = collection.GetEnumerator();
while (enumerator.MoveNext())
{
    var element = enumerator.Current;
    // Process element
}
```

---

## Memory Allocation Traps

| Forbidden                               | Use Instead                         | Reason                   |
| --------------------------------------- | ----------------------------------- | ------------------------ |
| `new List<T>()` in hot path             | Use `Buffers<T>.List.Get()`         | Pool avoids allocation   |
| `new Dictionary<K,V>()` in hot path     | Use `Buffers<K,V>.Dictionary.Get()` | Pool avoids allocation   |
| `new StringBuilder()` in hot path       | Use `Buffers.StringBuilder.Get()`   | Pool avoids allocation   |
| String concatenation in loops           | Use pooled `StringBuilder`          | O(n²) allocations        |
| `$"interpolated {string}"` in hot paths | Use `StringBuilder.Append()` chain  | Hidden allocations       |
| `params` method calls                   | Chain 2-argument overloads          | Array allocated per call |
| Delegate assignment in loops            | Assign delegate once outside loop   | 52 bytes per iteration   |
| Closure capturing local variable        | Use explicit loop or static lambda  | Allocates closure class  |

---

## Boxing Traps

| Forbidden                        | Use Instead                       | Reason                |
| -------------------------------- | --------------------------------- | --------------------- |
| Struct in `Dictionary<TEnum, V>` | Custom `IEqualityComparer<TEnum>` | Boxing per lookup     |
| Struct without `IEquatable<T>`   | Implement `IEquatable<T>`         | Boxing per comparison |
| Value type to `object` parameter | Use generic method                | Boxing (12+ bytes)    |
| Interface boxing (non-generic)   | Use generic constraint            | Boxing (12+ bytes)    |

---

## Hash Code Patterns

**CRITICAL**: Hash code implementations must be deterministic across processes and Unity versions. The project uses `Objects.HashCode()` for all hash code generation.

| Forbidden                          | Use Instead          | Reason                                       |
| ---------------------------------- | -------------------- | -------------------------------------------- |
| `System.HashCode.Combine()`        | `Objects.HashCode()` | Non-deterministic between processes/restarts |
| `obj.GetHashCode()` for custom     | `Objects.HashCode()` | May be non-deterministic for Unity types     |
| `hash * 31 + field.GetHashCode()`  | `Objects.HashCode()` | Hand-rolled patterns are error-prone         |
| `hash ^ field.GetHashCode()`       | `Objects.HashCode()` | XOR patterns have poor distribution          |
| `hash * 397 ^ field.GetHashCode()` | `Objects.HashCode()` | ReSharper pattern, still non-deterministic   |
| `HashCode.Add()` builder pattern   | `Objects.HashCode()` | System.HashCode is non-deterministic         |

### Why System.HashCode is Forbidden

`System.HashCode.Combine()` uses per-process random seed initialization:

```csharp
// This is FORBIDDEN - hash value changes between process restarts
int hash = HashCode.Combine(name, value, type);
```

This causes problems for:

- Save files (hash stored, then different on reload)
- Network synchronization (different hash on different machines)
- Reproducible testing (tests may pass/fail non-deterministically)
- Caching (cache keys invalid after restart)

### Correct Pattern

Use `Objects.HashCode()` from this project, which provides deterministic hashing:

```csharp
// Correct - deterministic across processes and platforms
public override int GetHashCode()
{
    return Objects.HashCode(_name, _value, _type);
}

// For structs with IEquatable<T>
public readonly struct MyStruct : IEquatable<MyStruct>
{
    private readonly string _name;
    private readonly int _value;

    public override int GetHashCode() => Objects.HashCode(_name, _value);
    public bool Equals(MyStruct other) => _name == other._name && _value == other._value;
    public override bool Equals(object obj) => obj is MyStruct other && Equals(other);
}
```

See [ObjectsHashCodePattern.cs](../code-samples/patterns/ObjectsHashCodePattern.cs) for complete examples.

---

## Unity-Specific Patterns

### Component Access

| Forbidden                             | Use Instead                     | Reason                           |
| ------------------------------------- | ------------------------------- | -------------------------------- |
| `GetComponent<T>()` in `Update()`     | Cache in `Awake()`              | Expensive lookup every frame     |
| `Camera.main` in `Update()`           | Cache reference in `Awake()`    | Performs `FindGameObjectWithTag` |
| `FindObjectOfType<T>()` in `Update()` | Cache in `Awake()`              | Scans entire scene               |
| `transform` property repeatedly       | Cache `_transform` in `Awake()` | Property access overhead         |

### Array-Returning Properties

| Forbidden                   | Use Instead                                   | Reason                             |
| --------------------------- | --------------------------------------------- | ---------------------------------- |
| `mesh.vertices` repeatedly  | `mesh.GetVertices(list)`                      | Creates new array copy each access |
| `mesh.normals` repeatedly   | `mesh.GetNormals(list)`                       | Creates new array copy each access |
| `mesh.uv` repeatedly        | `mesh.GetUVs(channel, list)`                  | Creates new array copy each access |
| `mesh.triangles` repeatedly | `mesh.GetTriangles(list, submesh)`            | Creates new array copy each access |
| `Input.touches`             | `Input.touchCount` + `Input.GetTouch(i)`      | Creates new array each access      |
| `Animator.parameters`       | `Animator.parameterCount` + `GetParameter(i)` | Creates new array each access      |
| `Renderer.sharedMaterials`  | `Renderer.GetSharedMaterials(list)`           | Creates new array each access      |

### Physics

| Forbidden                      | Use Instead                               | Reason                             |
| ------------------------------ | ----------------------------------------- | ---------------------------------- |
| `Physics.RaycastAll()`         | `Physics.RaycastNonAlloc(buffer)`         | Allocates new array                |
| `Physics.OverlapSphere()`      | `Physics.OverlapSphereNonAlloc(buffer)`   | Allocates new array                |
| `Physics.OverlapBox()`         | `Physics.OverlapBoxNonAlloc(buffer)`      | Allocates new array                |
| `Physics2D.OverlapCircleAll()` | `Physics2D.OverlapCircleNonAlloc(buffer)` | Allocates new array                |
| Non-convex mesh colliders      | Compound primitive colliders              | Extremely slow collision detection |

### Tags, Names, and Strings

| Forbidden                               | Use Instead                    | Reason                       |
| --------------------------------------- | ------------------------------ | ---------------------------- |
| `gameObject.tag == "Tag"`               | `gameObject.CompareTag("Tag")` | `.tag` allocates new string  |
| `gameObject.name == "Name"`             | Cache name in `Awake()`        | `.name` allocates new string |
| String concatenation for UI every frame | Update only on value change    | Allocates every frame        |

### Messaging

| Forbidden            | Use Instead                         | Reason                          |
| -------------------- | ----------------------------------- | ------------------------------- |
| `SendMessage()`      | Direct interface call               | Up to 1000x slower (reflection) |
| `BroadcastMessage()` | Events/delegates or interface calls | Up to 1000x slower (reflection) |

### Materials

| Forbidden                                 | Use Instead                    | Reason                          |
| ----------------------------------------- | ------------------------------ | ------------------------------- |
| `renderer.material` for changes           | `MaterialPropertyBlock`        | `.material` clones the material |
| `Shader.PropertyToID("_Name")` repeatedly | Cache as `static readonly int` | String lookup overhead          |

### Coroutines

| Forbidden                          | Use Instead                         | Reason                    |
| ---------------------------------- | ----------------------------------- | ------------------------- |
| `new WaitForSeconds()` in loop     | Cache `WaitForSeconds` instance     | Allocates every iteration |
| `new WaitForEndOfFrame()` in loop  | Cache `WaitForEndOfFrame` instance  | Allocates every iteration |
| `new WaitForFixedUpdate()` in loop | Cache `WaitForFixedUpdate` instance | Allocates every iteration |

### Debug and Lifecycle

| Forbidden                                           | Use Instead                           | Reason                           |
| --------------------------------------------------- | ------------------------------------- | -------------------------------- |
| `Debug.Log()` in production builds                  | `#if UNITY_EDITOR` or `[Conditional]` | String allocation even in builds |
| Empty `Update()` / `FixedUpdate()` / `LateUpdate()` | Remove entirely                       | Managed/native boundary overhead |
| `Instantiate`/`Destroy` spam                        | Object pooling                        | GC spikes and fragmentation      |

---

## Reflection Patterns

| Forbidden                                         | Use Instead                        | Reason                       |
| ------------------------------------------------- | ---------------------------------- | ---------------------------- |
| `Type.GetField()` on our code                     | Make field `internal`              | Slow, no compile-time safety |
| `Type.GetProperty()` on our code                  | Make property `internal`           | Slow, no compile-time safety |
| `Type.GetMethod()` on our code                    | Make method `internal`             | Slow, no compile-time safety |
| `FieldInfo.GetValue()`/`SetValue()`               | Direct field access via `internal` | Slow, no compile-time safety |
| `MethodInfo.Invoke()`                             | Direct method call via `internal`  | Slow, no compile-time safety |
| `Activator.CreateInstance()` with non-public ctor | Make constructor `internal`        | Slow, no compile-time safety |

### Acceptable Reflection

- Accessing Unity internal members (unavoidable)
- Accessing third-party library internals (document why)
- Testing reflection utilities themselves

---

## Magic String Patterns

| Forbidden                                 | Use Instead                               | Reason                 |
| ----------------------------------------- | ----------------------------------------- | ---------------------- |
| `"fieldName"` for our field names         | `nameof(fieldName)`                       | No compile-time safety |
| `"PropertyName"` for our properties       | `nameof(PropertyName)`                    | No compile-time safety |
| `"MethodName"` for our methods            | `nameof(MethodName)`                      | No compile-time safety |
| `"ClassName"` for our types               | `nameof(ClassName)` or `typeof().Name`    | No compile-time safety |
| `"Namespace.ClassName"` for full names    | `typeof(ClassName).FullName`              | No compile-time safety |
| `GetProperty("PropertyName")`             | `nameof()` + internal visibility          | No compile-time safety |
| `serializedObject.FindProperty("_field")` | `nameof(_field)` (field must be internal) | No compile-time safety |

### Acceptable Magic Strings

- Unity internal properties (`m_Script`, `m_LocalPosition`, etc.)
- Third-party library internals (document why)
- User-facing display strings
- Configuration/data keys (JSON properties, PlayerPrefs, etc.)
- File paths and resource names

---

## Update Method Anti-Patterns

| Anti-Pattern                        | Solution                   | Reason                               |
| ----------------------------------- | -------------------------- | ------------------------------------ |
| Physics operations in `Update()`    | Use `FixedUpdate()`        | Inconsistent at different framerates |
| Input handling in `FixedUpdate()`   | Use `Update()`             | May miss input events                |
| Heavy logic every frame             | Spread work across frames  | Frame rate drops                     |
| Many MonoBehaviours with `Update()` | Centralized update manager | Managed/native boundary overhead     |

---

## Related Documentation

- [high-performance-csharp](../skills/high-performance-csharp.md) - Core performance patterns
- [unity-performance-patterns](../skills/unity-performance-patterns.md) - Unity-specific patterns
- [memory-allocation-traps](../skills/memory-allocation-traps.md) - Hidden allocation sources
- [avoid-reflection](../skills/avoid-reflection.md) - Reflection avoidance
- [avoid-magic-strings](../skills/avoid-magic-strings.md) - Magic string avoidance
