# Zenject Integration - Unity Helpers

## Why This Integration Matters

**Stop Writing GetComponent Boilerplate in Every Single Script**

When using dependency injection with Zenject, you've solved half the problem - your service dependencies get injected cleanly. But you're **still stuck** writing repetitive `GetComponent` boilerplate for hierarchy references in every. single. MonoBehaviour.

**The Painful Reality:**

1. **Dependencies** → ✅ Handled by Zenject (IHealthSystem, IAudioService, etc.)
2. **Hierarchy references** → ❌ Still manual hell (SpriteRenderer, Rigidbody2D, child colliders, etc.)

You're using a modern DI framework but still writing 2008-era Unity boilerplate. **Unity Helpers fixes this.**

**The Solution:** This integration automatically wires up relational component fields **right after** DI injection completes - giving you the best of both worlds with **literally zero extra code per component**.

### ⚡ Quick Example: Before vs After

**Before (Manual):**

```csharp
public class Enemy : MonoBehaviour
{
    [Inject] private IHealthSystem _healthSystem;
    private Animator _animator;
    private Rigidbody2D _rigidbody;
    private Collider2D[] _childColliders;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _childColliders = GetComponentsInChildren<Collider2D>();
        // 10+ more lines of GetComponent calls...

        if (_animator == null) Debug.LogError("Missing Animator!");
        if (_rigidbody == null) Debug.LogError("Missing Rigidbody2D!");
        // More validation...
    }
}
```

**After (With Integration):**

```csharp
public class Enemy : MonoBehaviour
{
    [Inject] private IHealthSystem _healthSystem;

    [SiblingComponent] private Animator _animator;
    [SiblingComponent] private Rigidbody2D _rigidbody;
    [ChildComponent] private Collider2D[] _childColliders;

    // That's it! No Awake() needed - both DI and relational fields are auto-wired
    // Automatic validation with helpful error messages included
}
```

**⏱️ Time Saved:** 10-20 lines of boilerplate per component × hundreds of components = **weeks** of development time.
**🧠 Mental Load Eliminated:** No more context-switching between DI patterns and Unity hierarchy patterns.
**🐛 Bugs Prevented:** Automatic validation catches missing references **before** they cause runtime errors.

---

## 🚀 Quick Setup (2 Minutes)

### Step 1: Add the Installer to Your SceneContext

1. Add a `SceneContext` to your scene (if you don't have one already)
2. Add the `RelationalComponentsInstaller` component to the same GameObject
3. Enable **"Assign Scene On Initialize"** to automatically wire all scene components after the container builds (recommended)

> 💡 **Beginner tip:** Enable both checkboxes in the inspector:
>
> - ✅ **Assign Scene On Initialize** → Auto-wires all scene objects (saves you from calling it manually)
> - ✅ **Listen For Additive Scenes** → Auto-wires newly loaded scenes (great for multi-scene setups)

### Step 2: Use With Prefab Instantiation

When spawning prefabs at runtime, use the helpers that combine instantiation, DI, and relational assignment:

```csharp
using UnityEngine;
using Zenject;
using WallstopStudios.UnityHelpers.Integrations.Zenject;

public sealed class EnemySpawner : MonoBehaviour
{
    [Inject] private DiContainer _container;
    [SerializeField] private Enemy _enemyPrefab;
    [SerializeField] private GameObject _enemySquadPrefab;

    public Enemy SpawnEnemy(Transform parent)
    {
        return _container.InstantiateComponentWithRelations(_enemyPrefab, parent);
    }

    public GameObject SpawnEnemySquad(Transform parent)
    {
        return _container.InstantiateGameObjectWithRelations(
            _enemySquadPrefab,
            parent,
            includeInactiveChildren: true
        );
    }

    public void HydrateExisting(GameObject root)
    {
        _container.AssignRelationalHierarchy(root, includeInactiveChildren: true);
    }
}
```

**That's it!** Both DI injection and relational component wiring happen automatically.

---

## 📦 What's Included in This Sample

This sample provides a complete working example:

- **Scripts/SpawnerZenject.cs** - Demonstrates `InstantiateComponentWithRelations`, `InstantiateGameObjectWithRelations`, optional pooling, and hierarchy hydration
- **Scripts/RelationalConsumerPool.cs** - Minimal `RelationalMemoryPool` implementation for use with Zenject memory pools
- **Scripts/RelationalConsumer.cs** - Component demonstrating relational attributes
- **Prefabs/RelationalConsumer.prefab** - Example prefab with relational fields
- **Prefabs/SpawnerZenject.prefab** - Spawner prefab showing runtime usage
- **Scenes/Zenject_Sample.unity** - Complete working scene with SceneContext

### How to Import This Sample

1. Open Unity Package Manager
2. Find **Unity Helpers** in the package list
3. Expand the **Samples** section
4. Click **Import** next to "DI - Zenject"
5. Open `Scenes/Zenject_Sample.unity` and press Play

---

## 🎯 Common Use Cases (By Experience Level)

### 🟢 Beginner: "I just want my components to work"

**Perfect for:** Player controllers, enemy AI, simple gameplay scripts

**What you get:** No more `GetComponent` calls, no more null reference exceptions from missing components

**Example:**

```csharp
public class PlayerController : MonoBehaviour
{
    // Injected dependencies
    [Inject] private IInputService _input;
    [Inject] private IAudioService _audio;

    // Hierarchy references (auto-wired)
    [SiblingComponent] private Animator _animator;
    [SiblingComponent] private Rigidbody2D _rigidbody;
    [ChildComponent(TagFilter = "Weapon")] private Weapon _weapon;

    // Everything wired automatically when scene loads!

    void Update()
    {
        Vector2 input = _input.GetMovementInput();
        _rigidbody.velocity = input * moveSpeed;
        _animator.SetFloat("Speed", input.magnitude);
    }
}
```

**Important:** Enable **"Assign Scene On Initialize"** in the `RelationalComponentsInstaller` for automatic scene wiring.

### 🟡 Intermediate: "I'm spawning objects at runtime"

**Perfect for:** Enemy spawners, projectile systems, object pooling

**What you get:** One-line instantiation that handles DI injection + hierarchy wiring automatically

**Example:**

```csharp
public sealed class ProjectileSpawner : MonoBehaviour
{
    [Inject] private DiContainer _container;
    [SerializeField] private Projectile _projectilePrefab;

    public Projectile Fire(Vector3 position, Vector3 direction)
    {
        Projectile projectile = _container.InstantiateComponentWithRelations(_projectilePrefab);
        projectile.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
        projectile.Launch(direction);
        return projectile;
    }
}
```

### 🔴 Advanced: "I have complex hierarchies and custom workflows"

**Perfect for:** UI systems, vehicles with multiple parts, procedural generation, custom factories

**What you get:** Full control over when and how wiring happens, with helpers for every scenario

**Example - Complex Prefabs:**

```csharp
public sealed class VehicleFactory : MonoBehaviour
{
    [Inject] private DiContainer _container;
    [SerializeField] private GameObject _vehiclePrefab;

    public GameObject CreateVehicle()
    {
        return _container.InstantiateGameObjectWithRelations(
            _vehiclePrefab,
            parent: null,
            includeInactiveChildren: true
        );
    }
}
```

**Example - Custom Factories:**

```csharp
public class EnemyFactory : PlaceholderFactory<Enemy>
{
    [Inject] private DiContainer _container;

    public override Enemy Create()
    {
        Enemy enemy = base.Create();
        _container.AssignRelationalComponents(enemy);
        return enemy;
    }
}

// In your installer:
Container.BindFactory<Enemy, EnemyFactory>()
    .FromComponentInNewPrefab(enemyPrefab);
```

---

## 💡 Real-World Impact: A Day in the Life

### Without This Integration

**Morning:** You start work on a new enemy type.

```csharp
public class FlyingEnemy : MonoBehaviour
{
    [Inject] private IHealthSystem _health;
    [Inject] private IAudioService _audio;

    private Animator _animator;
    private Rigidbody2D _rigidbody;
    private SpriteRenderer _sprite;
    private Collider2D[] _hitboxes;
    private Transform _weaponMount;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null) Debug.LogError("Missing Animator on FlyingEnemy!");

        _rigidbody = GetComponent<Rigidbody2D>();
        if (_rigidbody == null) Debug.LogError("Missing Rigidbody2D on FlyingEnemy!");

        _sprite = GetComponent<SpriteRenderer>();
        if (_sprite == null) Debug.LogError("Missing SpriteRenderer on FlyingEnemy!");

        _hitboxes = GetComponentsInChildren<Collider2D>();
        if (_hitboxes.Length == 0) Debug.LogWarning("No hitboxes found on FlyingEnemy!");

        _weaponMount = transform.Find("WeaponMount");
        if (_weaponMount == null) Debug.LogError("Missing WeaponMount on FlyingEnemy!");

        // Finally, actual game logic can start...
    }
}
```

**10 minutes later:** You've written 20+ lines of boilerplate before writing any actual game logic.

**30 minutes later:** Null reference exception in the build! You forgot to add the SpriteRenderer to the prefab.

**60 minutes later:** You're manually wiring up the 8th enemy variant of the day...

### With This Integration

**Morning:** You start work on a new enemy type.

```csharp
public class FlyingEnemy : MonoBehaviour
{
    [Inject] private IHealthSystem _health;
    [Inject] private IAudioService _audio;

    [SiblingComponent] private Animator _animator;
    [SiblingComponent] private Rigidbody2D _rigidbody;
    [SiblingComponent] private SpriteRenderer _sprite;
    [ChildComponent] private Collider2D[] _hitboxes;
    [ChildComponent(NameFilter = "WeaponMount")] private Transform _weaponMount;

    // Start writing game logic immediately
    void Start() => _animator.Play("Idle");
}
```

**2 minutes later:** You're done with wiring and writing game logic.

**10 minutes later:** You've shipped 5 enemy variants with zero boilerplate.

**Never:** You never see "Missing component" runtime errors because validation happens automatically with helpful messages.

---

## 🔧 Advanced Configuration

### RelationalComponentsInstaller Options

The installer component provides these settings:

**Assign Scene On Initialize** *(default: true)*

- When enabled, automatically wires all scene components with relational attributes after the container builds
- Disable if you want to manually control when scene wiring happens

```csharp
// Manual scene wiring (if you disabled auto-assign)
[Inject] private DiContainer _container;

void Start()
{
    _container.AssignRelationalComponents(this);
}
```

### Manual Hierarchy Wiring

For dynamic hierarchies or pooled objects:

```csharp
[Inject] private DiContainer _container;

void SetupComplexHierarchy(GameObject root)
{
    // Wire all components in hierarchy
    _container.AssignRelationalHierarchy(root, includeInactiveChildren: false);
}
```

### Performance: Prewarming Reflection Caches

For large projects, prewarm reflection caches during loading to avoid first-use stalls:

```csharp
using WallstopStudios.UnityHelpers.Core.Attributes;

public class GameBootstrap : IInitializable
{
    public void Initialize()
    {
        // Call once during bootstrap/loading screen
        RelationalComponentInitializer.Initialize();
    }
}

// In your installer:
Container.BindInterfacesTo<GameBootstrap>().AsSingle();
```

Or enable auto-prewarm on the `AttributeMetadataCache` asset:

1. Create: `Assets > Create > Wallstop Studios > Unity Helpers > Attribute Metadata Cache`
2. Enable **"Prewarm Relational On Load"** in the Inspector

---

## 🧰 Additional Helpers & Recipes

### One-liners for DI + Relational Wiring

```csharp
// Inject + assign a single component
Container.InjectWithRelations(component);

// Instantiate a component prefab + assign
var comp = Container.InstantiateComponentWithRelations(prefabComp, parent);

// Inject + assign a whole hierarchy
Container.InjectGameObjectWithRelations(root, includeInactiveChildren: true);

// Instantiate a GameObject prefab + inject + assign hierarchy
var go = Container.InstantiateGameObjectWithRelations(prefabGo, parent);
```

### Additive Scenes & Options

In the `RelationalComponentsInstaller`, enable “Assign Scene On Initialize” and “Listen For Additive Scenes”. You can also control scanning behavior via options:

```csharp
public sealed class GameInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Bind assigner (done by installer automatically if used)
        // Container.Bind<IRelationalComponentAssigner>().To<RelationalComponentAssigner>().AsSingle();

        // Configure scan options used by the initializer/listener
        Container.BindInstance(new RelationalSceneAssignmentOptions(
            includeInactive: true,
            useSinglePassScan: true
        ));

        // Register initializer + additive scene listener (installer toggles also available)
        Container.BindInterfacesTo<RelationalComponentSceneInitializer>().AsSingle();
        Container.BindInterfacesTo<RelationalSceneLoadListener>().AsSingle();
    }
}
```

### Pools

Use DI-aware Zenject memory pools to assign on spawn automatically:

```csharp
public class EnemyPool : RelationalMemoryPool<Enemy> {}

// Or with a spawn parameter
public class BulletPool : RelationalMemoryPool<Vector3, Bullet> {}
```

---

## ❓ Troubleshooting

### My relational fields are null even with the integration

**Check these common issues:**

1. **Did you add the installer?**
   - Ensure `RelationalComponentsInstaller` is on your `SceneContext` GameObject
   - Check that it's enabled in the Inspector

2. **Scene components not wired?**
   - Enable **"Assign Scene On Initialize"** in the `RelationalComponentsInstaller`
   - Or manually call `_container.AssignRelationalHierarchy(gameObject, includeInactiveChildren: true)` at bootstrap time

3. **Are you using the right attributes?**
   - Fields need `[SiblingComponent]`, `[ParentComponent]`, or `[ChildComponent]` attributes
   - These are different from `[Inject]` - you can use both on the same component

4. **Runtime instantiation not working?**
   - Use `_container.InstantiateComponentWithRelations(...)`, `_container.InstantiateGameObjectWithRelations(...)`, or `_container.AssignRelationalHierarchy(...)`
   - Regular `InstantiatePrefab()`/`InstantiatePrefabForComponent()` won't trigger relational wiring without these helpers

5. **Check your filters:**
   - `TagFilter` must match an existing Unity tag exactly
   - `NameFilter` is case-sensitive

### Do I need to call AssignRelationalComponents() in Awake()?

**No!** The integration handles this automatically:

- **Scene objects:** Wired when you enable "Assign Scene On Initialize" (recommended)
- **Runtime objects:** Wired when you call any of the helper methods (`InstantiateComponentWithRelations`, `InstantiateGameObjectWithRelations`, `AssignRelationalHierarchy`, or the pooling helpers built on `RelationalMemoryPool`)

Only call `AssignRelationalComponents()` manually if you need fine-grained control.

### Does this work without Zenject?

**Yes!** The integration gracefully falls back to standard Unity Helpers behavior if Zenject isn't detected. You can:

- Adopt incrementally without breaking existing code
- Use in projects that mix DI and non-DI components
- Remove Zenject later without refactoring all your components

### Performance impact?

**Minimal:** Relational component assignment happens once per component at initialization time. After that, there's zero runtime overhead - the references are just regular fields.

**Optimization tips:**

- Use `MaxDepth` to limit hierarchy traversal
- Use `TagFilter` or `NameFilter` to narrow searches
- Use `OnlyDescendants`/`OnlyAncestors` to exclude self when appropriate

### Zenject vs Extenject?

This integration works with **all** Zenject variants:

- **Zenject** (original)
- **Extenject** (community fork)
- **Zenject (Modesttree)** (updated original)

Unity Helpers automatically detects which one you're using.

---

## 📚 Learn More

**Unity Helpers Documentation:**

- [Relational Components Guide](../../Docs/features/relational-components/relational-components.md) - Complete attribute reference and recipes
- [Getting Started](../../Docs/overview/getting-started.md) - Unity Helpers quick start guide
- [Main README](../../README.md) - Full feature overview

**Zenject Documentation:**

- [Zenject GitHub](https://github.com/modesttree/Zenject) - Official Zenject documentation
- [Extenject GitHub](https://github.com/svermeulen/Extenject) - Community fork with updates

**Troubleshooting:**

- [Relational Components Troubleshooting](../../Docs/features/relational-components/relational-components.md#troubleshooting) - Detailed solutions
- [DI Integration Testing Guide](../../Docs/features/relational-components/relational-components.md#di-integrations-testing-and-edge-cases) - Advanced scenarios

---

## 🎓 Next Steps

1. **Try the sample scene:** Open `Zenject_Sample.unity` and press Play
2. **Read the scripts:** See how `SpawnerZenject` and `RelationalConsumer` work
3. **Add to your project:** Add `RelationalComponentsInstaller` to your `SceneContext`
4. **Explore attributes:** Check out the [Relational Components Guide](../../Docs/features/relational-components/relational-components.md) for all options

---

## 🔄 Comparison: Zenject vs VContainer Integration

If you're choosing between Zenject and VContainer, here's how the integrations differ:

| Feature | Zenject | VContainer |
|---------|---------|------------|
| Setup | Add installer to SceneContext | Call in LifetimeScope.Configure() |
| Scene wiring | Toggle on installer | Automatic |
| Runtime instantiation | `InstantiateComponentWithRelations()`, `InstantiateGameObjectWithRelations()`, `RelationalMemoryPool` helpers | `InstantiateComponentWithRelations()`, `InstantiateGameObjectWithRelations()`, `BuildUpWithRelations()`, `RelationalObjectPools` helpers |
| Performance | Good | Slightly faster |
| Maintenance | Community-maintained | Actively developed |

Both integrations provide the same relational component features - choose based on your DI framework preference.

---

## Made with ❤️ by Wallstop Studios

*Unity Helpers is production-ready and actively maintained. [Star the repo](https://github.com/wallstop/unity-helpers) if you find it useful!*

