# Skill: Use Singleton

**Trigger**: When implementing global manager classes, service locators, or shared configuration objects in Unity.

---

## RuntimeSingleton&lt;T&gt; for MonoBehaviour Singletons

Use `RuntimeSingleton<T>` for MonoBehaviour-based singletons that need to exist in the scene.

### Basic Implementation

```csharp
using WallstopStudios.UnityHelpers.Utils;

public sealed class GameManager : RuntimeSingleton<GameManager>
{
    public int Score { get; set; }
    public bool IsPaused { get; set; }

    public void StartGame()
    {
        Score = 0;
        IsPaused = false;
    }
}

// Usage from anywhere
GameManager.Instance.StartGame();
Debug.Log($"Score: {GameManager.Instance.Score}");
```

### The `Preserve` Property

By default, `RuntimeSingleton<T>` persists across scene loads via `DontDestroyOnLoad`. Override `Preserve` to change this behavior:

```csharp
// ✅ Persists across scenes (default)
public sealed class AudioManager : RuntimeSingleton<AudioManager>
{
    // Preserve defaults to true - survives scene changes
}

// ✅ Scene-local singleton - destroyed on scene change
public sealed class LevelManager : RuntimeSingleton<LevelManager>
{
    protected override bool Preserve => false;  // Stay scene-local
}
```

### Lifecycle Methods

Override lifecycle methods as needed (always call base):

```csharp
public sealed class GameServices : RuntimeSingleton<GameServices>
{
    protected override void Awake()
    {
        base.Awake();  // Always call base first!
        InitializeServices();
    }

    protected override void Start()
    {
        base.Start();  // Always call base first!
        StartServices();
    }

    protected override void OnDestroy()
    {
        CleanupServices();
        base.OnDestroy();  // Call base last
    }
}
```

---

## ScriptableObjectSingleton&lt;T&gt; for Configuration

Use `ScriptableObjectSingleton<T>` for global configuration, settings, or data that should be edited in the Unity Editor.

### Basic Implementation

```csharp
using WallstopStudios.UnityHelpers.Utils;
using UnityEngine;

public sealed class GameSettings : ScriptableObjectSingleton<GameSettings>
{
    [SerializeField]
    private float _masterVolume = 1f;

    [SerializeField]
    private int _targetFrameRate = 60;

    public float MasterVolume => _masterVolume;
    public int TargetFrameRate => _targetFrameRate;
}

// Usage from anywhere
float volume = GameSettings.Instance.MasterVolume;
```

### Asset Creation

Create the ScriptableObject asset:

1. **Automatic**: Use the "ScriptableObject Singleton Creator" tool (Tools > WallstopStudios)
2. **Manual**: Right-click in Project > Create > [Your Type Name]

Assets are loaded from `Resources/` folder. Default lookup order:

1. Custom path specified via `[ScriptableSingletonPath]`
2. `Resources/<TypeName>/`
3. `Resources/<TypeName>` (exact name match)
4. Global Resources search

---

## Custom Asset Paths with [ScriptableSingletonPath]

Specify where the singleton asset should be loaded from:

```csharp
using WallstopStudios.UnityHelpers.Core.Attributes;
using WallstopStudios.UnityHelpers.Utils;

[ScriptableSingletonPath("Settings/Audio")]
public sealed class AudioSettings : ScriptableObjectSingleton<AudioSettings>
{
    // Asset should be at: Resources/Settings/Audio/AudioSettings.asset
}

[ScriptableSingletonPath("Config")]
public sealed class GameConfig : ScriptableObjectSingleton<GameConfig>
{
    // Asset should be at: Resources/Config/GameConfig.asset
}
```

---

## Automatic Instantiation with [AutoLoadSingleton]

Use `[AutoLoadSingleton]` to automatically instantiate singletons during Unity startup:

```csharp
using WallstopStudios.UnityHelpers.Core.Attributes;
using WallstopStudios.UnityHelpers.Utils;
using UnityEngine;

// Loads before splash screen (default)
[AutoLoadSingleton]
public sealed class BootstrapManager : RuntimeSingleton<BootstrapManager>
{
    protected override void Awake()
    {
        base.Awake();
        InitializeCore();
    }
}

// Loads at a specific phase
[AutoLoadSingleton(RuntimeInitializeLoadType.AfterSceneLoad)]
public sealed class PostSceneManager : RuntimeSingleton<PostSceneManager>
{
}
```

### Load Types

| Load Type               | When                        |
| ----------------------- | --------------------------- |
| `SubsystemRegistration` | Earliest, before subsystems |
| `AfterAssembliesLoaded` | After assemblies loaded     |
| `BeforeSplashScreen`    | Before splash (default)     |
| `BeforeSceneLoad`       | Before first scene          |
| `AfterSceneLoad`        | After first scene loaded    |

---

## Safe Instance Checking with HasInstance

Check if an instance exists without triggering creation:

```csharp
// ✅ Safe check - doesn't create instance
if (GameManager.HasInstance)
{
    GameManager.Instance.SaveProgress();
}

// ❌ Avoid - creates instance if it doesn't exist
if (GameManager.Instance != null)  // Instance property creates if missing!
{
    GameManager.Instance.SaveProgress();
}
```

### OnDestroy Pattern

```csharp
private void OnDestroy()
{
    // Safe cleanup during shutdown
    if (GameManager.HasInstance)
    {
        GameManager.Instance.UnregisterEntity(this);
    }
}
```

---

## Main Thread Requirements

Both singleton types require main thread access. Unity API calls must happen on the main thread.

```csharp
// ✅ Called from main thread (MonoBehaviour callbacks, coroutines)
void Update()
{
    GameManager.Instance.UpdateScore();  // OK
}

// ❌ Called from background thread
async Task ProcessAsync()
{
    await Task.Run(() =>
    {
        // This will throw!
        GameManager.Instance.UpdateScore();  // ERROR: Not on main thread
    });
}
```

The singletons use `UnityMainThreadGuard.EnsureMainThread()` internally to enforce this.

---

## Common Patterns

### Service Registry

```csharp
[AutoLoadSingleton]
public sealed class Services : RuntimeSingleton<Services>
{
    public IAudioService Audio { get; private set; }
    public IInputService Input { get; private set; }
    public ISaveService Save { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        Audio = new AudioService();
        Input = new InputService();
        Save = new SaveService();
    }
}

// Usage
Services.Instance.Audio.PlaySound("click");
```

### Configuration with Defaults

```csharp
[ScriptableSingletonPath("Config")]
public sealed class DifficultySettings : ScriptableObjectSingleton<DifficultySettings>
{
    [SerializeField]
    private float _enemyHealthMultiplier = 1f;

    [SerializeField]
    private float _playerDamageMultiplier = 1f;

    public float EnemyHealthMultiplier => _enemyHealthMultiplier;
    public float PlayerDamageMultiplier => _playerDamageMultiplier;
}
```

### Event Bus Singleton

```csharp
public sealed class EventBus : RuntimeSingleton<EventBus>
{
    public event Action<int> OnScoreChanged;
    public event Action OnGameOver;

    public void RaiseScoreChanged(int newScore) => OnScoreChanged?.Invoke(newScore);
    public void RaiseGameOver() => OnGameOver?.Invoke();
}

// Usage
EventBus.Instance.OnScoreChanged += HandleScoreChanged;
EventBus.Instance.RaiseScoreChanged(100);
```

---

## Common Mistakes

### ❌ Forgetting to Call Base Methods

```csharp
// ❌ Breaks singleton behavior
protected override void Awake()
{
    // Missing base.Awake()!
    Initialize();
}

// ✅ Always call base
protected override void Awake()
{
    base.Awake();  // Required!
    Initialize();
}
```

### ❌ Accessing Instance During OnDestroy

```csharp
// ❌ May create new instance during shutdown
private void OnDestroy()
{
    GameManager.Instance.Unregister(this);  // Dangerous!
}

// ✅ Check HasInstance first
private void OnDestroy()
{
    if (GameManager.HasInstance)
    {
        GameManager.Instance.Unregister(this);
    }
}
```

### ❌ Missing Asset for ScriptableObjectSingleton

```csharp
// ❌ No asset in Resources - returns null and logs warning
var settings = GameSettings.Instance;  // null if asset missing!

// ✅ Create asset first:
// 1. Tools > WallstopStudios > ScriptableObject Singleton Creator
// 2. Or manually create in Resources folder
```

### ❌ Scene-Local Singleton with [AutoLoadSingleton]

```csharp
// ❌ Conflicting: auto-loaded but won't persist
[AutoLoadSingleton]
public sealed class LevelManager : RuntimeSingleton<LevelManager>
{
    protected override bool Preserve => false;  // Destroyed on scene change!
}

// ✅ Either persist across scenes...
[AutoLoadSingleton]
public sealed class LevelManager : RuntimeSingleton<LevelManager>
{
    // Preserve defaults to true
}

// ✅ ...or don't auto-load scene-local singletons
public sealed class LevelManager : RuntimeSingleton<LevelManager>
{
    protected override bool Preserve => false;
}
```

---

## When to Use Each Singleton Type

| Use Case                 | Type                                           |
| ------------------------ | ---------------------------------------------- |
| Game managers, services  | `RuntimeSingleton<T>`                          |
| Audio/Input managers     | `RuntimeSingleton<T>`                          |
| Scene-specific managers  | `RuntimeSingleton<T>` with `Preserve => false` |
| Game settings/config     | `ScriptableObjectSingleton<T>`                 |
| Balance data             | `ScriptableObjectSingleton<T>`                 |
| Editor-configurable data | `ScriptableObjectSingleton<T>`                 |
