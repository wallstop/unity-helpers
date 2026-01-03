# Skill: Use Threading Utilities

<!-- trigger: thread, async, main, dispatch, marshal | Main thread dispatch, thread safety | Feature -->

**Trigger**: When marshalling work between background threads/tasks and Unity's main thread, or when enforcing thread-safety for Unity API calls.

---

## UnityMainThreadDispatcher

Thread-safe singleton that enqueues work to run on Unity's main thread. Works in both Edit Mode and Play Mode.

### Basic Usage: RunOnMainThread

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

// From a background thread or Task
Task.Run(async () =>
{
    string data = await FetchDataAsync();

    // Marshal callback to main thread
    UnityMainThreadDispatcher.Instance.RunOnMainThread(() =>
    {
        // Safe to call Unity APIs here
        myText.text = data;
        Debug.Log("Data loaded!");
    });
});
```

### Async/Await Pattern: RunAsync

```csharp
UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance;

// Fire-and-forget with await
await dispatcher.RunAsync(() =>
{
    player.Health = 0;
    playerAnimator.Play("Die");
});

// With cancellation support
using CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
await dispatcher.RunAsync(async token =>
{
    await FadeCanvasGroupAsync(canvasGroup, 0f, token);
}, timeout.Token);
```

### TryRunOnMainThread (Silent Failures)

```csharp
// Use when overflow is expected and you want to silently drop work
bool queued = dispatcher.TryRunOnMainThread(() => UpdateTelemetryUI(status));
if (!queued)
{
    // Queue was full, action was dropped
    Debug.LogWarning("Telemetry update dropped");
}
```

---

## UnityMainThreadGuard

Enforces main thread access for Unity API calls. Throws `InvalidOperationException` when called from background threads.

### EnsureMainThread

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

public class MySingleton
{
    private static MySingleton _instance;

    public static MySingleton Instance
    {
        get
        {
            // Throws if not on main thread
            UnityMainThreadGuard.EnsureMainThread();
            return _instance;
        }
    }

    public void RefreshUI()
    {
        // With context for better error messages
        UnityMainThreadGuard.EnsureMainThread("Refreshing UI");

        // Safe to interact with Unity objects here
        canvas.enabled = true;
    }
}
```

### Check Without Throwing

```csharp
// Check thread without throwing
if (UnityMainThreadGuard.IsMainThread)
{
    // Direct Unity API call
    myTransform.position = newPos;
}
else
{
    // Marshal to main thread
    UnityMainThreadDispatcher.Instance.RunOnMainThread(() =>
    {
        myTransform.position = newPos;
    });
}
```

---

## SingleThreadedThreadPool

Dedicated single-threaded work queue for background processing. Useful for sequential async operations that shouldn't run on the thread pool.

```csharp
using WallstopStudios.UnityHelpers.Core.Threading;

// Create pool (runs in background by default)
SingleThreadedThreadPool pool = new SingleThreadedThreadPool();

// Enqueue synchronous work
pool.Enqueue(() => ProcessData(data));

// Enqueue async work
pool.Enqueue(async () => await SaveAsync(data));

// Check pending work count
int pending = pool.Count;

// Check for exceptions
while (pool.Exceptions.TryDequeue(out Exception ex))
{
    Debug.LogError($"Worker exception: {ex}");
}

// Cleanup
await pool.DisposeAsync();
```

---

## Pattern: Callback Marshalling

### From Native Plugins or External Libraries

```csharp
public class NativeCallbackHandler : MonoBehaviour
{
    // Called from native code on arbitrary thread
    [AOT.MonoPInvokeCallback(typeof(NativeCallback))]
    private static void OnNativeEvent(int eventId, string data)
    {
        // Marshal to Unity main thread
        UnityMainThreadDispatcher.Instance.RunOnMainThread(() =>
        {
            ProcessEvent(eventId, data);
        });
    }
}
```

### From Async Network Operations

```csharp
public async Task<Texture2D> LoadTextureAsync(string url)
{
    byte[] imageData = await httpClient.GetByteArrayAsync(url);

    Texture2D texture = null;

    // Texture creation must happen on main thread
    await UnityMainThreadDispatcher.Instance.RunAsync(() =>
    {
        texture = new Texture2D(2, 2);
        texture.LoadImage(imageData);
    });

    return texture;
}
```

---

## Pattern: Async Initialization

```csharp
public class GameManager : MonoBehaviour
{
    private async void Start()
    {
        // Heavy work on background thread
        GameData data = await Task.Run(() => LoadAndParseGameData());

        // Back on main thread (Unity's default synchronization context)
        InitializeGame(data);
    }

    // Alternative: explicit marshalling
    public async Task InitializeAsync()
    {
        GameData data = await Task.Run(() => LoadAndParseGameData());

        await UnityMainThreadDispatcher.Instance.RunAsync(() =>
        {
            // Guaranteed main thread even if caller's context changed
            InitializeGame(data);
        });
    }
}
```

---

## Pattern: Thread-Safe Property Access

```csharp
public class ThreadSafeComponent : MonoBehaviour
{
    private string _status;
    private readonly object _lock = new();

    // Thread-safe read
    public string Status
    {
        get
        {
            lock (_lock)
            {
                return _status;
            }
        }
    }

    // Write from any thread, updates UI on main thread
    public void SetStatus(string value)
    {
        lock (_lock)
        {
            _status = value;
        }

        UnityMainThreadDispatcher.Instance.RunOnMainThread(() =>
        {
            statusText.text = value;
        });
    }
}
```

---

## IL2CPP and WebGL Considerations

### WebGL: No Threading Support

```csharp
// WebGL runs single-threaded - no background threads available
#if UNITY_WEBGL && !UNITY_EDITOR
// Use coroutines instead of threads
StartCoroutine(ProcessDataCoroutine(data));
#else
// Use threading on other platforms
Task.Run(() => ProcessData(data));
#endif
```

### IL2CPP Thread Safety

```csharp
// IL2CPP requires careful thread synchronization
// Use Interlocked for atomic operations
private int _counter;

public void IncrementSafe()
{
    Interlocked.Increment(ref _counter);
}

// Avoid lock-free patterns that rely on memory barriers
// IL2CPP's memory model may differ from Mono
```

---

## Test Scope Management

```csharp
using NUnit.Framework;

[TestFixture]
public class DispatcherTests
{
    private UnityMainThreadDispatcher.AutoCreationScope _scope;

    [SetUp]
    public void SetUp()
    {
        // Disable auto-creation, destroy existing, re-enable for test
        _scope = UnityMainThreadDispatcher.CreateTestScope(destroyImmediate: true);
    }

    [TearDown]
    public void TearDown()
    {
        _scope?.Dispose();
        _scope = null;
    }

    [Test]
    public void TestDispatcher()
    {
        UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance;
        Assert.IsNotNull(dispatcher);
        // Test logic...
    }
}
```

---

## Common Mistakes

### ❌ Deadlock: Blocking on Main Thread

```csharp
// ❌ DEADLOCK - main thread blocks waiting for itself
void Update()
{
    Task task = UnityMainThreadDispatcher.Instance.RunAsync(() => DoWork());
    task.Wait(); // Blocks main thread forever!
}

// ✅ Use async/await instead
async void Update()
{
    await UnityMainThreadDispatcher.Instance.RunAsync(() => DoWork());
}
```

### ❌ Using Destroyed Dispatcher

```csharp
// ❌ Dispatcher may be destroyed on scene change
UnityMainThreadDispatcher dispatcher = UnityMainThreadDispatcher.Instance;
// ... later, after scene change ...
dispatcher.RunOnMainThread(() => DoWork()); // May throw!

// ✅ Always get fresh reference or check
if (UnityMainThreadDispatcher.TryGetInstance(out var dispatcher))
{
    dispatcher.RunOnMainThread(() => DoWork());
}

// Or simply get Instance each time (auto-creates if needed)
UnityMainThreadDispatcher.Instance.RunOnMainThread(() => DoWork());
```

### ❌ Capturing Unity Objects in Background Tasks

```csharp
// ❌ Accessing Transform from background thread
Transform myTransform = transform;
Task.Run(() =>
{
    Vector3 pos = myTransform.position; // CRASH! Unity API on background thread
});

// ✅ Capture values, not Unity objects
Vector3 currentPos = transform.position;
Task.Run(() =>
{
    Vector3 newPos = CalculateNewPosition(currentPos);
    UnityMainThreadDispatcher.Instance.RunOnMainThread(() =>
    {
        transform.position = newPos;
    });
});
```

### ❌ Ignoring Queue Overflow

```csharp
// ❌ Flooding the dispatcher queue
for (int i = 0; i < 100000; i++)
{
    dispatcher.RunOnMainThread(() => UpdateSomething(i));
}

// ✅ Batch work or check queue status
dispatcher.PendingActionLimit = 1000; // Set reasonable limit

// Or batch updates
List<int> batch = new List<int>();
for (int i = 0; i < 100000; i++)
{
    batch.Add(i);
    if (batch.Count >= 100)
    {
        List<int> captured = new List<int>(batch);
        dispatcher.RunOnMainThread(() => UpdateBatch(captured));
        batch.Clear();
    }
}
```

### ❌ Edit Mode Assumptions

```csharp
// ❌ Assuming Play Mode behavior in Edit Mode
void OnValidate()
{
    // RunOnMainThread works, but Update timing differs in Edit Mode
    UnityMainThreadDispatcher.Instance.RunOnMainThread(() =>
    {
        // May execute later than expected in Edit Mode
        RefreshEditor();
    });
}

// ✅ Use EditorApplication.delayCall for Edit Mode
#if UNITY_EDITOR
void OnValidate()
{
    if (!Application.isPlaying)
    {
        UnityEditor.EditorApplication.delayCall += RefreshEditor;
        return;
    }

    UnityMainThreadDispatcher.Instance.RunOnMainThread(RefreshEditor);
}
#endif
```

---

## API Reference

| Class                       | Method                                                       | Description                                  |
| --------------------------- | ------------------------------------------------------------ | -------------------------------------------- |
| `UnityMainThreadDispatcher` | `Instance`                                                   | Singleton accessor (auto-creates if enabled) |
|                             | `RunOnMainThread(Action)`                                    | Enqueue action for main thread execution     |
|                             | `TryRunOnMainThread(Action)`                                 | Try enqueue without logging overflow         |
|                             | `RunAsync(Action)`                                           | Enqueue and return awaitable Task            |
|                             | `RunAsync(Func<CancellationToken, Task>, CancellationToken)` | Enqueue async delegate with cancellation     |
|                             | `PendingActionCount`                                         | Number of queued actions                     |
|                             | `PendingActionLimit`                                         | Max queue size (0 = unlimited)               |
| `UnityMainThreadGuard`      | `EnsureMainThread(string)`                                   | Throw if not on main thread                  |
|                             | `IsMainThread`                                               | Check without throwing                       |
| `SingleThreadedThreadPool`  | `Enqueue(Action)`                                            | Queue synchronous work                       |
|                             | `Enqueue(Func<Task>)`                                        | Queue async work                             |
|                             | `Count`                                                      | Pending work items                           |
|                             | `Exceptions`                                                 | Queue of caught exceptions                   |
