# Skill: Profile and Debug Performance

**Trigger**: When investigating performance issues, optimizing hot paths, or verifying zero-allocation code.

---

## Profiling First Principle

**Never optimize without profiling.** Performance intuition is often wrong.

> "Profile first, optimize observed hot spots. Each optimization reduces flexibility."

However, for patterns that cost nothing to implement correctly from the start (caching, avoiding LINQ, pooling), use them by default.

---

## Unity Profiler

### Setup

1. **Window → Analysis → Profiler**
2. Enable **Deep Profile** for detailed allocation tracking (slower but comprehensive)
3. Connect to **target device** for accurate metrics (Editor != runtime)

### Key Modules

| Module    | What It Shows                                |
| --------- | -------------------------------------------- |
| CPU Usage | Frame time, method execution, GC allocations |
| GPU Usage | Rendering time, draw calls                   |
| Memory    | Heap size, allocations, native memory        |
| Rendering | Batches, set pass calls, triangles           |
| Physics   | Contacts, rigidbodies, colliders             |

### Finding Allocations

1. Select **CPU Usage** module
2. Enable **Deep Profile** or use **Call Stacks** for GC Alloc
3. Look at **GC.Alloc** column in hierarchy view
4. Click on frame spikes to investigate

### GC Allocation Markers

```csharp
// Add markers to isolate specific code sections
using Unity.Profiling;

public class MyComponent : MonoBehaviour
{
    private static readonly ProfilerMarker s_UpdateMarker =
        new ProfilerMarker("MyComponent.Update");

    private static readonly ProfilerMarker s_ProcessEnemiesMarker =
        new ProfilerMarker("MyComponent.ProcessEnemies");

    void Update()
    {
        using (s_UpdateMarker.Auto())
        {
            using (s_ProcessEnemiesMarker.Auto())
            {
                ProcessEnemies();
            }
        }
    }
}
```

---

## Memory Profiler

### Setup

Install via **Package Manager** → Unity Registry → Memory Profiler

### Key Features

- **Snapshot comparison** — Compare memory between two points in time
- **Object references** — See what's keeping objects alive
- **Native memory** — Track textures, meshes, audio
- **Fragmentation view** — Visualize heap fragmentation

### Taking Snapshots

1. Play in Editor or connect to device
2. Click **Capture** in Memory Profiler window
3. Wait for snapshot to complete
4. Compare snapshots to find leaks

---

## Frame Debugger

### Window → Analysis → Frame Debugger

Use to analyze:

- Draw call count and batching effectiveness
- Shader passes
- Render target switches
- What's being rendered (and shouldn't be)

---

## Allocation Testing in Code

### Verify Zero Allocation

```csharp
using NUnit.Framework;

[Test]
public void Method_ShouldNotAllocate_InSteadyState()
{
    // Warm up - first call may allocate (caches, etc.)
    _target.MyMethod();

    // Measure steady state
    long before = GC.GetAllocatedBytesForCurrentThread();

    for (int i = 0; i < 1000; i++)
    {
        _target.MyMethod();
    }

    long after = GC.GetAllocatedBytesForCurrentThread();
    long allocated = after - before;

    Assert.AreEqual(0, allocated, $"Method allocated {allocated} bytes over 1000 calls");
}
```

### Allocation Test with Warmup Pattern

```csharp
[Test]
public void ProcessItems_ZeroAllocationAfterWarmup()
{
    List<Item> items = CreateTestItems(100);

    // Warmup phase - allow one-time allocations
    for (int warmup = 0; warmup < 10; warmup++)
    {
        _processor.Process(items);
    }

    // Measurement phase
    long baseline = GC.GetAllocatedBytesForCurrentThread();

    for (int i = 0; i < 100; i++)
    {
        _processor.Process(items);
    }

    long allocated = GC.GetAllocatedBytesForCurrentThread() - baseline;
    Assert.AreEqual(0, allocated, $"Allocated {allocated} bytes in steady state");
}
```

---

## Identifying Hot Paths

### What Qualifies as a Hot Path

| Code Location             | Frequency      | Optimization Priority |
| ------------------------- | -------------- | --------------------- |
| Update()                  | Every frame    | ★★★★★ Critical        |
| FixedUpdate()             | 50x/second     | ★★★★★ Critical        |
| LateUpdate()              | Every frame    | ★★★★★ Critical        |
| OnGUI()                   | Multiple/frame | ★★★★★ Critical        |
| Coroutine loops           | Continuous     | ★★★★☆ High            |
| Event handlers (frequent) | Per-event      | ★★★☆☆ Medium          |
| One-time init             | Once           | ★☆☆☆☆ Low             |

### Profiler-Guided Optimization

1. **Profile** the actual game scenario
2. **Sort by Self Time** to find expensive methods
3. **Check GC.Alloc** for allocation hotspots
4. **Focus on top 10%** — usually 90% of the performance impact

---

## Common Performance Pitfalls

### Symptoms and Causes

| Symptom                       | Likely Cause                | Investigation                     |
| ----------------------------- | --------------------------- | --------------------------------- |
| Frame rate drops periodically | GC collection               | Check GC.Alloc in Profiler        |
| Stuttering every few seconds  | Large GC collection         | Memory Profiler snapshots         |
| Slow first frame              | Cold caches, initialization | Profile startup                   |
| Mobile overheating            | CPU/GPU overwork            | Check both CPU and GPU time       |
| Memory grows over time        | Memory leak                 | Compare Memory Profiler snapshots |

### GC Spike Diagnosis

1. In CPU Profiler, look for `GC.Collect` spikes
2. Check frames before the spike for high `GC.Alloc`
3. Use **Call Stacks** to trace allocation source
4. Common culprits:
   - LINQ in Update
   - String concatenation
   - `new List<T>()` in loops
   - Closures/lambdas

---

## Build-Time Profiling

### Development Builds

Enable for accurate profiling:

- **Development Build** checkbox
- **Autoconnect Profiler**
- **Deep Profiling Support** (optional, adds overhead)

### IL2CPP Considerations

- IL2CPP builds behave differently than Mono
- Always profile IL2CPP builds for release targets
- Some allocations present in Mono may be eliminated in IL2CPP

---

## Benchmark Patterns

### Micro-Benchmark Pattern

```csharp
[Test]
public void CompareImplementations()
{
    const int iterations = 100000;
    List<int> testData = CreateTestData(1000);

    // Warmup
    for (int i = 0; i < 100; i++)
    {
        ImplementationA(testData);
        ImplementationB(testData);
    }

    // Measure A
    Stopwatch swA = Stopwatch.StartNew();
    for (int i = 0; i < iterations; i++)
    {
        ImplementationA(testData);
    }
    swA.Stop();

    // Measure B
    Stopwatch swB = Stopwatch.StartNew();
    for (int i = 0; i < iterations; i++)
    {
        ImplementationB(testData);
    }
    swB.Stop();

    Debug.Log($"A: {swA.ElapsedMilliseconds}ms, B: {swB.ElapsedMilliseconds}ms");
    Debug.Log($"B is {(float)swA.ElapsedMilliseconds / swB.ElapsedMilliseconds:F2}x faster");
}
```

### Loop Comparison Reference

Typical performance for 16M element array:

| Pattern              | Time  | Allocations    |
| -------------------- | ----- | -------------- |
| `for` over array     | 35ms  | 0B             |
| `for` over List      | 62ms  | 0B             |
| `foreach` over array | 35ms  | 0B             |
| `foreach` over List  | 120ms | 24B (old Mono) |
| LINQ `.Sum()`        | 271ms | 24B+           |

---

## Platform-Specific Profiling

### Mobile

- Profile on actual devices, not Editor
- Check thermal throttling (sustained performance)
- Monitor battery usage
- Use platform-specific tools (Xcode Instruments, Android Studio Profiler)

### Android Commands

```bash
# Memory info for specific app
adb shell dumpsys meminfo com.company.game

# Process memory usage
adb shell procrank

# System memory overview
adb shell cat /proc/meminfo
```

### Console

- Use platform SDK profilers
- Check certification requirements for performance
- Test with final build configuration

---

## Automated Performance Regression Tests

### Performance Test Framework

```csharp
using NUnit.Framework;
using Unity.PerformanceTesting;

public class PerformanceTests
{
    [Test, Performance]
    public void MeasureMethodPerformance()
    {
        Measure.Method(() =>
        {
            MyExpensiveMethod();
        })
        .WarmupCount(10)
        .MeasurementCount(100)
        .Run();
    }
}
```

### CI Performance Gates

```csharp
[Test]
public void ProcessItems_UnderBudget()
{
    Stopwatch sw = Stopwatch.StartNew();

    for (int i = 0; i < 1000; i++)
    {
        _processor.Process(_testItems);
    }

    sw.Stop();
    double msPerCall = sw.ElapsedMilliseconds / 1000.0;

    Assert.Less(msPerCall, 0.5, $"Process took {msPerCall}ms, budget is 0.5ms");
}
```

---

## Optimization Checklist

### Before Optimizing

- [ ] Have you profiled on target platform?
- [ ] Is this actually a hot path?
- [ ] What's the current allocation/time cost?
- [ ] What's the acceptable budget?

### After Optimizing

- [ ] Did you verify improvement with profiler?
- [ ] Did you add regression tests?
- [ ] Did you document the optimization reason?
- [ ] Is the code still maintainable?

---

## Related Skills

- [high-performance-csharp](./high-performance-csharp.md) — Performance patterns
- [unity-performance-patterns](./unity-performance-patterns.md) — Unity-specific patterns
- [performance-audit](./performance-audit.md) — Code review checklist
- [refactor-to-zero-alloc](./refactor-to-zero-alloc.md) — Migration patterns
