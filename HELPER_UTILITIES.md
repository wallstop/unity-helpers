# Helper Utilities Guide

## TL;DR — Why Use These

Static helper classes and utilities that solve common programming problems without needing components on GameObjects. Use these for predictive aiming, path utilities, threading, hashing, formatting, and more.

---

## Contents

- [Gameplay Helpers](#gameplay-helpers) — Predictive aiming, spatial sampling, rotation
- [GameObject & Component Helpers](#gameobject--component-helpers) — Component discovery, hierarchy manipulation
- [Transform Helpers](#transform-helpers) — Hierarchy traversal
- [Threading](#threading) — Main thread dispatcher
- [Path & File Helpers](#path--file-helpers) — Path resolution, file operations
- [Scene Helpers](#scene-helpers) — Scene queries and loading
- [Advanced Utilities](#advanced-utilities) — Null checks, hashing, formatting

---

<a id="gameplay-helpers"></a>

## Gameplay Helpers

### Predictive Aiming

**What it does:** Calculates where to aim when shooting at a moving target, accounting for projectile travel time.

**Problem it solves:** Shooting a bullet at where an enemy _is_ misses if they're moving. You need to aim at where they _will be_.

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

Vector2 enemyPos = enemy.transform.position;
Vector2 enemyVelocity = enemy.GetComponent<Rigidbody2D>().velocity;
Vector2 turretPos = turret.transform.position;
float bulletSpeed = 20f;

Vector2? aimPosition = Helpers.PredictCurrentTarget(
    enemyPos,
    enemyVelocity,
    turretPos,
    bulletSpeed
);

if (aimPosition.HasValue)
{
    // Aim at aimPosition to hit the moving target
    Vector2 aimDirection = (aimPosition.Value - turretPos).normalized;
    FireProjectile(aimDirection, bulletSpeed);
}
else
{
    // Target is too fast, can't hit
}
```

**When to use:**

- Turrets shooting at moving enemies
- AI aiming at moving players
- Predictive targeting systems
- Guided missiles

**When NOT to use:**

- Homing projectiles (use steering behaviors)
- Instant-hit weapons (use raycasts)
- Slow-moving or stationary targets (just aim directly)

---

### Spatial Sampling

**Get random points in circles/spheres:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

// Random point inside circle (uniform distribution)
Vector2 spawnPoint = Helpers.GetRandomPointInCircle(center, radius);

// Random point inside sphere (uniform distribution)
Vector3 explosionPoint = Helpers.GetRandomPointInSphere(center, radius);
```

**Use for:**

- Spawn points (enemies, pickups, particles)
- Explosion damage distribution
- Random movement destinations
- Scatter patterns

---

### Smooth Rotation Helpers

**Get rotation speed for smooth turning:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

// Calculate how much to rotate this frame toward target
float currentAngle = transform.eulerAngles.z;
float targetAngle = GetTargetAngle();
float maxDegreesPerSecond = 180f;

float newAngle = Helpers.GetAngleWithSpeed(
    currentAngle,
    targetAngle,
    maxDegreesPerSecond,
    Time.deltaTime
);

transform.eulerAngles = new Vector3(0, 0, newAngle);
```

**Handles:**

- Frame-rate independence
- Shortest rotation path (doesn't spin 270° when 90° is shorter)
- Angle wrapping (0-360°)

---

### Delayed Execution

**Execute code after delay or next frame:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

// Execute after 2 seconds
Helpers.ExecuteFunctionAfterDelay(
    monoBehaviour,
    () => Debug.Log("Delayed!"),
    delayInSeconds: 2f
);

// Execute next frame
Helpers.ExecuteFunctionNextFrame(
    monoBehaviour,
    () => Debug.Log("Next frame!")
);
```

Uses coroutines under the hood.

---

### Repeating Execution with Jitter

**Run function repeatedly with random timing variance:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

// Spawn enemy every 5-8 seconds
Helpers.StartFunctionAsCoroutine(
    gameManager,
    SpawnEnemy,
    baseInterval: 5f,
    intervalJitter: 3f  // Random ±3 seconds
);

void SpawnEnemy()
{
    Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
}
```

**Use for:**

- Enemy spawning with variability
- Random event triggers
- Staggered updates to spread CPU load
- Natural-feeling timing

---

### Layer & Label Queries

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

// Get all layer names (cached after first call)
string[] allLayers = Helpers.GetAllLayerNames();

// Get all sprite label names (editor only, cached)
string[] labels = Helpers.GetAllSpriteLabelNames();
```

**Use for:**

- Populating dropdowns in editor tools
- Runtime layer/label validation
- Configuration systems

---

### Collider Syncing

**Update PolygonCollider2D to match sprite:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

SpriteRenderer renderer = GetComponent<SpriteRenderer>();
PolygonCollider2D collider = GetComponent<PolygonCollider2D>();

Helpers.UpdateShapeToSprite(renderer, collider);
// Collider now matches sprite's physics shape
```

---

<a id="gameobject--component-helpers"></a>

## GameObject & Component Helpers

### Cached Component Lookup

**Fast tag-based component finding with caching:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

// First call searches scene, subsequent calls use cache
Player player = Helpers.Find<Player>("Player");

// Clear cache manually if needed
Helpers.ClearInstance<Player>();

// Set cache manually (for dependency injection scenarios)
Helpers.SetInstance(playerInstance);
```

**Performance:** First call = GameObject.FindWithTag, subsequent calls = O(1) dictionary lookup.

---

### Component Existence Checks

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

// Check if component exists without allocating
bool hasRigidbody = Helpers.HasComponent<Rigidbody2D>(gameObject);

// Better than:
bool hasRigidbody = GetComponent<Rigidbody2D>() != null; // Allocates
```

---

### Get-or-Add Pattern

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

// Get existing component or add if missing
Rigidbody2D rb = Helpers.GetOrAddComponent<Rigidbody2D>(gameObject);
```

---

### Hierarchical Enable/Disable

**Recursively enable/disable components:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

// Enable all Collider2D components in children
Helpers.EnableRecursively<Collider2D>(rootObject, enable: true);

// Disable all renderers in hierarchy
Helpers.EnableRendererRecursively<SpriteRenderer>(rootObject, enable: false);
```

**Use for:**

- Toggling collision for entire character rigs
- Hiding/showing complex prefabs
- Debug visualization toggles

---

### Bulk Child Destruction

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

// Destroy all children (useful for clearing containers)
Helpers.DestroyAllChildrenGameObjects(parentTransform);
```

**Use for:**

- Clearing inventory UI
- Resetting spawn containers
- Cleanup before repopulating

---

### Smart Destruction

**Editor/runtime aware destruction:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

// Uses DestroyImmediate in editor, Destroy in play mode
Helpers.SmartDestroy(gameObject);

// Also handles assets correctly (won't destroy project assets)
```

**Use in editor tools** to avoid "Destroying assets is not permitted" errors.

---

### Prefab Utilities

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

// Check if GameObject is a prefab asset or instance
bool isPrefab = Helpers.IsPrefab(gameObject);

// Safely modify prefab (editor only)
#if UNITY_EDITOR
Helpers.ModifyAndSavePrefab(prefabAssetPath, prefab =>
{
    // Modify prefab here
    var component = prefab.AddComponent<MyComponent>();
    component.value = 42;
    // Changes saved automatically
});
#endif
```

---

<a id="transform-helpers"></a>

## Transform Helpers

### Hierarchy Traversal (Depth-First)

**Visit all children recursively:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

// Depth-first traversal (visits deepest children first)
Helpers.IterateOverAllChildrenRecursively<SpriteRenderer>(rootTransform, renderer =>
{
    renderer.color = Color.red;
});

// Buffered version (zero allocation)
using (var buffer = Buffers<Transform>.List.Get())
{
    Helpers.IterateOverAllChildrenRecursively(rootTransform, buffer.Value);
    foreach (Transform child in buffer.Value)
    {
        // Process children
    }
}
```

---

### Hierarchy Traversal (Breadth-First)

**Visit by depth level:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

// Breadth-first traversal with depth limit
Helpers.IterateOverAllChildrenRecursivelyBreadthFirst(
    rootTransform,
    transform => Debug.Log(transform.name),
    maxDepth: 3  // Only visit 3 levels deep
);
```

**Use for:**

- Finding immediate area (not entire tree)
- Level-based operations
- Performance-sensitive searches

---

### Parent Traversal

**Walk up the hierarchy:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

// Find component in parents
Helpers.IterateOverAllParentComponentsRecursively<Canvas>(transform, canvas =>
{
    Debug.Log($"Found canvas: {canvas.name}");
});

// Get all parents (no component filter)
using (var buffer = Buffers<Transform>.List.Get())
{
    Helpers.IterateOverAllParents(transform, buffer.Value);
    // buffer contains all parent transforms up to root
}
```

**Use for:**

- Finding UI Canvas parents
- Inheritance checking (is this under X?)
- Walking to root of hierarchy

---

### Direct Children/Parents

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

// Get immediate children (non-recursive)
using (var buffer = Buffers<Transform>.List.Get())
{
    Helpers.IterateOverAllChildren(transform, buffer.Value);
    // Only direct children, no grandchildren
}
```

---

<a id="threading"></a>

## Threading

### UnityMainThreadDispatcher

**Execute code on Unity's main thread from background threads:**

**Problem it solves:** Unity APIs can only be called from the main thread. Background Tasks/threads can't directly manipulate GameObjects. This marshals callbacks back to the main thread.

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;
using System.Threading.Tasks;

async Task LoadDataInBackground()
{
    // Background thread work
    await Task.Run(() =>
    {
        // Expensive computation
        var data = LoadFromDatabase();

        // Need to update UI - marshal back to main thread
        UnityMainThreadDispatcher.Instance.RunOnMainThread(() =>
        {
            // Safe to call Unity APIs here
            uiText.text = data.ToString();
        });
    });
}
```

**Async version with result:**

```csharp
async Task<string> GetTextFromMainThread()
{
    // Called from background thread, executes on main thread
    string text = await UnityMainThreadDispatcher.Instance.Post(() =>
    {
        return uiText.text; // Safe to access Unity objects
    });

    return text;
}
```

**Fire-and-forget on main thread:**

```csharp
// From background thread
UnityMainThreadDispatcher.Instance.RunOnMainThread(() =>
{
    Instantiate(prefab, position, rotation);
});
```

**When to use:**

- Async file loading callbacks
- Network request callbacks
- Database query results
- Background computation results that update UI

**Important:**

- Works in both edit mode and play mode
- Actions queued during edit mode execute in next editor update
- Don't block the main thread with long operations

---

<a id="path--file-helpers"></a>

## Path & File Helpers

### Path Sanitization

**Normalize path separators:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

string windowsPath = @"Assets\Sprites\Player.png";
string unityPath = PathHelper.Sanitize(windowsPath);
// Result: "Assets/Sprites/Player.png"
```

Unity prefers forward slashes. Use this for cross-platform paths.

---

### Directory Utilities

**Create directories safely:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

#if UNITY_EDITOR
// Creates directory and updates AssetDatabase
DirectoryHelper.EnsureDirectoryExists("Assets/Generated/Data");
#endif
```

**Find package root:**

```csharp
// Walk hierarchy to find package.json
string packageRoot = DirectoryHelper.FindPackageRootPath();
// Returns path to package containing calling script
```

**Use for:**

- Editor tools generating assets
- Finding package-relative paths
- Build scripts creating folders

---

### Path Conversion

**Convert between absolute and Unity-relative paths:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

string absolute = "C:/Projects/MyGame/Assets/Textures/player.png";
string relative = DirectoryHelper.AbsoluteToUnityRelativePath(absolute);
// Result: "Assets/Textures/player.png"
```

**Get calling script's directory:**

```csharp
// Uses [CallerFilePath] magic
string scriptDir = DirectoryHelper.GetCallerScriptDirectory();
// Returns directory containing the calling .cs file
```

---

### File Operations

**Initialize file if missing:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

// Create config.json with default contents if it doesn't exist
FileHelper.InitializePath(
    "Assets/config.json",
    "{ \"version\": 1 }"
);
```

**Async file copy:**

```csharp
using System.Threading;

CancellationTokenSource cts = new CancellationTokenSource();

await FileHelper.CopyFileAsync(
    "source.txt",
    "destination.txt",
    bufferSize: 81920,  // 80KB buffer
    cts.Token
);
```

**Use for:**

- Large file operations without blocking
- Cancellable copy operations
- Streaming file operations

---

<a id="scene-helpers"></a>

## Scene Helpers

### Scene Queries

**Check if scene is loaded:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

bool loaded = SceneHelper.IsSceneLoaded("GameLevel");
// Checks by scene name or path
```

**Get all scene paths (editor):**

```csharp
#if UNITY_EDITOR
string[] allScenes = SceneHelper.GetAllScenePaths();
// Returns all .unity files in project

string[] buildScenes = SceneHelper.GetScenesInBuild();
// Returns only scenes in Build Settings
#endif
```

---

### Temporary Scene Loading

**Load scene, extract data, auto-unload:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

// RAII pattern - scene unloaded when disposed
using (var scope = SceneHelper.GetObjectOfTypeInScene<LevelConfig>("Scenes/LevelData"))
{
    if (scope.HasObject)
    {
        LevelConfig config = scope.Object;
        // Use config data
    }
    // Scene automatically unloaded here
}
```

**Use for:**

- Extracting data from data-only scenes
- Editor tools reading scene contents
- Validation scripts
- Testing scene contents

---

<a id="advanced-utilities"></a>

## Advanced Utilities

### Unity-Aware Null Checks

**The problem:** Unity's `==` operator overload can be slow, and destroyed UnityEngine.Objects return `true` for `== null` but `false` for `is null`.

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

GameObject obj = GetMaybeDestroyedObject();

// Proper Unity null check
bool isNull = Objects.Null(obj);
bool notNull = Objects.NotNull(obj);
```

Handles:

- Destroyed UnityEngine.Objects
- Actual null references
- Optimized checks for non-Unity types

---

### Deterministic Hashing

**Combine hash codes correctly:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

public class CompositeKey
{
    public string Name;
    public int Level;
    public Vector2 Position;

    public override int GetHashCode()
    {
        // FNV-1a based hash combination
        return Objects.HashCode(Name, Level, Position);
    }
}
```

Supports up to 11 parameters. Uses FNV-1a algorithm for good distribution.

**Hash entire collections:**

```csharp
List<int> numbers = new List<int> { 1, 2, 3, 4, 5 };
int hash = Objects.EnumerableHashCode(numbers);
```

**Use for:**

- Custom GetHashCode implementations
- Dictionary keys with multiple fields
- Networking determinism
- Save file hashing

---

### Formatting

**Human-readable byte counts:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

long bytes = 1536000;
string formatted = FormattingHelpers.FormatBytes(bytes);
// Result: "1.46 MB"
```

Auto-scales to B, KB, MB, GB, TB.

**Use for:**

- File size displays
- Memory usage UI
- Profiling output
- Download progress

---

### Multi-Dimensional Array Iteration

**Enumerate 2D/3D array indices:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

int[,] grid = new int[10, 10];

// Get all indices as tuples
foreach (var (x, y) in IterationHelpers.IndexOver(grid))
{
    grid[x, y] = x + y;
}

// Buffered (zero allocation)
using (var buffer = Buffers<(int, int)>.List.Get())
{
    IterationHelpers.IndexOver(grid, buffer.Value);
    foreach (var (x, y) in buffer.Value)
    {
        // Process
    }
}
```

Also supports 3D arrays with `(int, int, int)` tuples.

---

### Binary Array Conversion

**Fast marshalling between int[] and byte[]:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

int[] ints = { 1, 2, 3, 4, 5 };

// Convert to bytes (uses Buffer.BlockCopy)
byte[] bytes = ArrayConverter.IntArrayToByteArrayBlockCopy(ints);

// Convert back
int[] restored = ArrayConverter.ByteArrayToIntArrayBlockCopy(bytes);
```

**Use for:**

- Network serialization
- Binary file formats
- Save game data
- High-performance data conversion

**Performance:** O(n) native memory copy, much faster than element-by-element loops.

---

### Custom Comparers

**Create IComparer from lambda:**

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

var enemies = new List<Enemy>();

// Sort by health descending
enemies.Sort(new FuncBasedComparer<Enemy>((a, b) =>
    b.health.CompareTo(a.health) // Descending
));
```

**Reverse any comparer:**

```csharp
var comparer = Comparer<int>.Default;
var reversed = new ReverseComparer<int>(comparer);

// Now sorts descending
list.Sort(reversed);
```

---

## Best Practices

### Performance

- **Cache lookups**: `Helpers.Find<T>()` caches, but don't call every frame anyway
- **Use buffered variants**: `IterateOverAllChildrenRecursively` with buffers for hot paths
- **Main thread dispatch**: Don't send hundreds of tiny tasks, batch work
- **Hierarchy traversal**: Use breadth-first with depth limits for large hierarchies

### Threading

- **Main thread rule**: Only Unity APIs need main thread, pure C# can stay on background threads
- **Avoid blocking**: Don't wait for main thread results in tight loops
- **CancellationToken**: Always support cancellation for long operations

### Architecture

- **Component vs Helper**: Components (MonoBehaviours) for per-object state, Helpers for stateless operations
- **Static method smell**: If you need instance state, use a component instead
- **Editor/Runtime split**: Use `#if UNITY_EDITOR` guards for editor-only helpers

### Code Organization

- **Namespace imports**: Use `using WallstopStudios.UnityHelpers.Core.Helper;` at top of file
- **Don't extend helpers**: These are sealed utility classes, not inheritance hierarchies
- **Prefer composition**: Use helpers from components, don't try to combine them

---

## Related Documentation

- [Math & Extensions](MATH_AND_EXTENSIONS.md) - Extension methods on built-in types
- [Utility Components](UTILITY_COMPONENTS.md) - MonoBehaviour-based utilities
- [Reflection Helpers](REFLECTION_HELPERS.md) - High-performance reflection utilities
- [Singletons](SINGLETONS.md) - RuntimeSingleton and ScriptableObjectSingleton
