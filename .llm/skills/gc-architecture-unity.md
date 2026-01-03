# Skill: Unity Garbage Collection Architecture

<!-- trigger: gc, garbage, boehm, incremental, collect | Understanding Unity GC, incremental GC, manual GC | Performance -->

**Trigger**: When you need to understand how Unity's garbage collector differs from .NET, when to use incremental GC, or when to manually trigger garbage collection.

---

## Unity GC vs .NET GC Comparison

Unity uses the **Boehm-Demers-Weiser (BDW)** garbage collector, which differs significantly from .NET's generational GC:

| Aspect                  | Unity (Boehm GC)                | .NET CLR                                |
| ----------------------- | ------------------------------- | --------------------------------------- |
| **Algorithm**           | Conservative, non-generational  | Generational (Gen 0, 1, 2)              |
| **Generations**         | None — processes all objects    | 3 generations (short/medium/long-lived) |
| **Memory Compaction**   | ❌ No                           | ✅ Yes (except Large Object Heap)       |
| **Root Scanning**       | Conservative (less precise)     | Precise root scanning                   |
| **Fragmentation**       | Prone to fragmentation          | Reduced via compaction                  |
| **Short-lived Objects** | No optimization                 | Gen0 efficiently handles                |
| **Heap Shrinking**      | ❌ Never returns memory to OS   | ✅ Can release memory                   |
| **Stop-the-World**      | Full heap scan every collection | Per-generation (faster Gen0)            |

---

## Why This Matters

### The .NET Assumption That Fails in Unity

Standard .NET development assumes frequent small allocations are cheap because:

- Gen0 collections are fast (only scans newest objects)
- Short-lived objects are quickly collected
- Memory compaction prevents fragmentation

**In Unity, ALL of these assumptions are wrong:**

- Every collection scans the entire heap
- No optimization for short-lived objects
- Fragmentation accumulates over time
- Memory is never returned to the OS

### The Impact

```csharp
// In .NET: Acceptable pattern
// In Unity: Creates GC pressure every frame!
void Update()
{
    var enemies = new List<Enemy>();  // .NET: Gen0, cheap
                                       // Unity: Full heap allocation
}
```

---

## Unity's Incremental GC Mode

Unity 2019.1+ offers **Incremental Garbage Collection** to reduce frame spikes.

### How It Works

Instead of one long pause, incremental GC:

1. Distributes marking phase across multiple frames
2. Uses "write barriers" to track reference changes between frames
3. Reduces individual pause durations

### When Incremental GC Works Well

- Most object references remain stable between frames
- Objects don't frequently change relationships
- Steady-state gameplay with predictable allocation patterns

### When Incremental GC Fails

```csharp
// ❌ BAD: Too many reference changes per frame
void Update()
{
    // Each of these changes triggers write barrier overhead
    _target = FindNearestEnemy();
    _weapon.Target = _target;
    _ui.UpdateTarget(_target);
    // ...100 more reference changes
}
```

**Threshold Warning**: If reference changes exceed what incremental GC can process, Unity falls back to a **full stop-the-world collection** — potentially worse than non-incremental!

### Enabling Incremental GC

```text
Edit → Project Settings → Player → Other Settings → Use Incremental GC
```

**Default**: Enabled in Unity 2019.1+

---

## When to Manually Trigger GC

### Safe Times to Call GC.Collect()

```csharp
// ✅ During loading screens
public IEnumerator LoadLevel()
{
    ShowLoadingScreen();
    yield return LoadAssetsAsync();

    // Clean up before gameplay
    Resources.UnloadUnusedAssets();
    System.GC.Collect();

    HideLoadingScreen();
}

// ✅ During scene transitions
void OnSceneUnloaded(Scene scene)
{
    Resources.UnloadUnusedAssets();
    System.GC.Collect();
}

// ✅ During pause menus (if acceptable pause)
void OnGamePaused()
{
    System.GC.Collect();
}
```

### Never Call GC.Collect() During

- Active gameplay
- Animation playback
- Physics simulation
- Audio playback
- Any time frame timing matters

---

## Memory Lifecycle in Unity

### The Managed Heap Never Shrinks

```text
Start: [    Heap: 10 MB    ]
Allocate: [    Heap: 50 MB    ]
GC Run: [    Heap: 50 MB    ]  ← Still 50 MB!
              ↑ Free space exists but heap size unchanged
```

**Implication**: Memory high-water mark persists until application restart.

### Fragmentation Accumulates

Without compaction, freed memory creates "holes":

```text
Before: [A][B][C][D][E][F][G][H]
After:  [A][ ][C][ ][E][ ][G][ ]  ← Freed B, D, F, H
New:    [A][ ][C][ ][E][ ][G][ ]  ← Can't fit [IIII] (4 slots)
                                    Even though 4 slots are free!
```

**Solution**: Avoid the problem — don't allocate in the first place.

---

## Memory Types and GC Tracking

| Type              | Location       | GC Tracked | Notes                    |
| ----------------- | -------------- | ---------- | ------------------------ |
| Local value types | Stack          | No         | Automatic cleanup        |
| Reference types   | Managed heap   | Yes        | Subject to GC            |
| Boxed value types | Managed heap   | Yes        | Hidden allocations       |
| `NativeArray<T>`  | Unmanaged heap | No         | Manual disposal required |
| `stackalloc`      | Stack          | No         | Limited size, unsafe     |
| Static fields     | Managed heap   | Yes        | Persist until app exit   |

### Using Unmanaged Memory to Avoid GC

```csharp
using Unity.Collections;

// Allocates outside managed heap — GC ignores it
NativeArray<Vector3> positions = new NativeArray<Vector3>(
    128, Allocator.Persistent);

// CRITICAL: Must dispose manually or memory leaks!
void OnDestroy()
{
    if (positions.IsCreated)
    {
        positions.Dispose();
    }
}
```

---

## Platform Considerations

### Mobile

- Smaller heap limits
- GC pauses more noticeable
- Battery impact from GC work
- **Recommendation**: Even stricter zero-allocation policy

### WebGL

- Single-threaded, GC blocks everything
- No incremental GC available
- **Recommendation**: Pre-allocate everything at startup

### IL2CPP vs Mono

Both use the same Boehm GC behavior, but:

- IL2CPP generates better native code
- IL2CPP has AOT-only limitation (no runtime code generation)
- Same GC characteristics apply to both

---

## Quick Reference: GC-Safe Patterns

| ❌ Avoid                     | ✅ Use Instead                   |
| ---------------------------- | -------------------------------- |
| `new List<T>()` in methods   | `Buffers<T>.List.Get()`          |
| `new T[]` in methods         | Array pools                      |
| String concatenation         | `StringBuilder` pooling          |
| LINQ in Update/FixedUpdate   | Explicit `for` loops             |
| Boxing value types           | Generic methods                  |
| `foreach` on `List<T>`       | `for` with indexer               |
| Per-frame allocations        | Cache and reuse                  |
| Closures capturing variables | Static lambdas or explicit loops |

---

## Related Skills

- [high-performance-csharp](./high-performance-csharp.md) — Core zero-allocation patterns
- [unity-performance-patterns](./unity-performance-patterns.md) — Unity-specific optimizations
- [memory-allocation-traps](./memory-allocation-traps.md) — Hidden allocation sources
- [refactor-to-zero-alloc](./refactor-to-zero-alloc.md) — Migration patterns
- [use-pooling](./use-pooling.md) — Collection pooling
