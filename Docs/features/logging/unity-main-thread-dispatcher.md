# Unity Main Thread Dispatcher & Guard

`UnityMainThreadDispatcher` and `UnityMainThreadGuard` provide thread-safe access to Unity's main thread from background workers, ensuring callbacks execute correctly and preventing common threading errors.

## UnityMainThreadDispatcher

The `UnityMainThreadDispatcher` is the package-wide bridge for marshaling callbacks from worker threads back onto Unity's main thread. The dispatcher is implemented as a `RuntimeSingleton<UnityMainThreadDispatcher>`, is marked `[ExecuteAlways]`, and runs both in Edit Mode and Play Mode so background logging, importers, and build scripts can all enqueue work safely.

## UnityMainThreadGuard

The `UnityMainThreadGuard` ensures critical code executes on Unity's main thread, automatically dispatching to it if called from a worker thread.

### Basic Usage

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Helper;

public class BackgroundProcessor
{
    public void ProcessOnWorkerThread()
    {
        // This might be called from a background thread
        UnityMainThreadGuard.EnsureMainThread(() =>
        {
            // This code ALWAYS executes on the main thread
            Debug.Log("Safe to call Unity APIs here");
            GameObject.FindObjectOfType<Player>().UpdateState();
        });
    }
}
```

### Why It Exists

**Problem:** Unity APIs must be called from the main thread, but async operations, Thread Pool work, and Task continuations often execute on background threads

**Solution:** `UnityMainThreadGuard` automatically detects the current thread and marshals execution to the main thread if needed

### API

```csharp
// Execute action on main thread (blocks if already on main thread)
UnityMainThreadGuard.EnsureMainThread(Action action);

// Check if currently on main thread
bool isMainThread = UnityMainThreadGuard.IsMainThread;
```

### Use Cases

```csharp
// Async/await with Unity API access
public async Task<GameObject> LoadAssetAsync(string path)
{
    var asset = await LoadFromDiskAsync(path);

    // Ensure instantiation happens on main thread
    GameObject result = null;
    UnityMainThreadGuard.EnsureMainThread(() =>
    {
        result = Object.Instantiate(asset);
    });

    return result;
}

// Thread Pool work
ThreadPool.QueueUserWorkItem(_ =>
{
    var data = ProcessHeavyComputation();

    UnityMainThreadGuard.EnsureMainThread(() =>
    {
        ApplyToScene(data);  // Safe Unity API calls
    });
});

// Task continuation
Task.Run(() => ComputeData())
    .ContinueWith(task =>
    {
        UnityMainThreadGuard.EnsureMainThread(() =>
        {
            DisplayResults(task.Result);
        });
    });
```

## Default Bootstrapping Flow

- A `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]` (`EnsureDispatcherBootstrap`) spins up the dispatcher before user assemblies receive their own `RuntimeInitializeOnLoadMethod` callbacks. This guarantees that worker threads can enqueue diagnostics as soon as your game loads.
- Inside the editor, `[InitializeOnLoadMethod]` (`EnsureDispatcherBootstrapInEditor`) mirrors the same behavior for Edit Mode tools. The bootstrap politely skips when play mode is already running, so it does not duplicate the instance scene-side.
- Both entry points run through `UnityMainThreadGuard.EnsureMainThread(...)` before touching Unity APIs, so the dispatcher is always created on the real main thread even when background code tries to force instantiation.

With no configuration, the dispatcher therefore always exists, enforces the queue bound exposed via `PendingActionLimit`, and is hidden from the hierarchy during Edit Mode by `HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.NotEditable`.

## Opting In/Out of Auto-Creation

Occasionally (especially in tests) you want to control exactly when the dispatcher is created so that scenes unload cleanly and Unity does not warn about hidden objects surviving domain reloads. Use the internal switch:

```csharp
UnityMainThreadDispatcher.SetAutoCreationEnabled(false);
// ... destroy any dispatcher instance if you want a completely clean slate ...
UnityMainThreadDispatcher.SetAutoCreationEnabled(true);
```

- The toggle is global to the current domain; disabling it prevents the runtime/editor bootstrap hooks from creating fresh GameObjects.
- When you re-enable auto-creation the very next call to `UnityMainThreadDispatcher.Instance` will recreate the singleton on the main thread (and Play Mode bootstrap will recreate it on the following domain reload).
- Always destroy the existing dispatcher GameObject (`UnityMainThreadDispatcher.DestroyExistingDispatcher` or the test helper) before turning the feature back on so that stale instances are removed from `Resources.FindObjectsOfTypeAll`.

### AutoCreationScope (scoped toggles)

`UnityMainThreadDispatcher.AutoCreationScope` wraps the toggle/cleanup pattern above in an `IDisposable` so you cannot forget to restore the previous state:

```csharp
using UnityMainThreadDispatcher.AutoCreationScope scope =
    UnityMainThreadDispatcher.AutoCreationScope.Disabled(
        destroyExistingInstanceOnEnter: true,
        destroyInstancesOnDispose: true,
        destroyImmediate: true   // prefer true in tests, false in runtime code
    );

// Auto-creation is disabled inside the scope; manually enable or create instances as needed.
```

- On enter, the scope captures the previous `AutoCreationEnabled` value, switches to the desired state, and optionally destroys any existing dispatcher instances.
- On dispose, the scope restores the original toggle and (when requested) destroys any dispatcher that may have been created during the scoped work, ensuring follow-up tests inherit a clean slate.

## Test Bootstrap Workflow

Two dedicated bootstraps live under `Tests/*/TestUtils` and keep dispatcher lifetimes predictable during automated suites:

### Runtime / PlayMode (`Tests/Runtime/TestUtils/UnityMainThreadDispatcherTestBootstrap.cs`)

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void DisableAutoCreation()
{
    UnityMainThreadDispatcher.SetAutoCreationEnabled(false);
    UnityMainThreadDispatcherTestHelper.DestroyDispatcherIfExists(immediate: true);
}
```

- Runs before every play-mode domain reload.
- Forces any hidden dispatcher left over from a prior scene to be `DestroyImmediate`'d so play-mode tests can opt into dispatcher usage explicitly.
- Guarantees a clean slate for suites that create and destroy scenes repeatedly (see `CommonTestBase.BaseSetUp`).

### Editor / EditMode (`Tests/Editor/TestUtils/UnityMainThreadDispatcherEditorTestBootstrap.cs`)

```csharp
[InitializeOnLoad]
internal static class UnityMainThreadDispatcherEditorTestBootstrap
{
    static UnityMainThreadDispatcherEditorTestBootstrap()
    {
        UnityMainThreadDispatcher.SetAutoCreationEnabled(false);
        // DestroyImmediate the hidden dispatcher object, if any.
    }
}
```

- Executes once per editor domain reload (before EditMode tests run).
- Ensures the hidden dispatcher object is removed from the hierarchy so Unity's test runner does not flag leaked objects between assemblies.

<a id="testing-patterns"></a>

### Re-enabling per Test

The runtime and editor `CommonTestBase` fixtures demonstrate the intended per-test lifecycle via `UnityMainThreadDispatcher.AutoCreationScope`:

1. At `[SetUp]` it grabs `UnityMainThreadDispatcher.CreateTestScope(destroyImmediate: true)` which internally disables auto-creation, destroys stragglers, and then re-enables auto-creation so the test can access `Instance` normally.
2. Production code can create/destroy the dispatcher freely; the scope tracks everything automatically.
3. During every teardown stage it disposes the scope, restoring the previous auto-creation flag and destroying any dispatcher created while the test runs — no manual try/finally blocks required.

Downstream packages can copy the exact pattern:

1. Add a `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` that disables auto-creation and destroys lingering instances.
2. Add an `[InitializeOnLoad]` editor bootstrap that mirrors the same behavior for Edit Mode.
3. When a single test needs to temporarily disable the dispatcher, wrap the custom logic in `using var scope = UnityMainThreadDispatcher.AutoCreationScope.Disabled(...)` so cleanup always runs.

Following this workflow keeps Unity from warning about hidden GameObjects during test teardown, prevents worker threads from instantiating the dispatcher off-thread, and documents exactly when auto-creation is in effect for your package.
