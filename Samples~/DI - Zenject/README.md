# Zenject Integration - Unity Helpers

## Why This Integration Matters

**The Problem:** When using dependency injection with Zenject, you often need to wire up both:

1. **Dependencies** (injected via constructor/properties/fields)
2. **Hierarchy references** (SpriteRenderer, Rigidbody2D, child colliders, etc.)

Doing this manually means writing boilerplate in every component.

**The Solution:** Unity Helpers' Zenject integration automatically wires up relational component fields **right after** DI injection completes - giving you the best of both worlds with zero extra code.

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

**Time Saved:** 10-20 lines of boilerplate per component × hundreds of components = **weeks** of development time.

---

## 🚀 Quick Setup (2 Minutes)

### Step 1: Add the Installer to Your SceneContext

1. Add a `SceneContext` to your scene (if you don't have one already)
2. Add the `RelationalComponentsInstaller` component to the same GameObject
3. *(Optional)* Toggle **"Assign Scene On Initialize"** to automatically wire all scene components after the container builds

![SceneContext Setup](../../Docs/Images/zenject_setup.png)

### Step 2: Use With Prefab Instantiation

When spawning prefabs at runtime, use `InstantiateComponentWithRelations` instead of regular Zenject instantiation:

```csharp
using UnityEngine;
using Zenject;
using WallstopStudios.UnityHelpers.Integrations.Zenject;

public class EnemySpawner : MonoBehaviour
{
    [Inject] private DiContainer _container;
    [SerializeField] private Enemy _enemyPrefab;

    public void SpawnEnemy(Vector3 position)
    {
        // ✨ Performs DI injection AND relational component wiring
        Enemy enemy = _container.InstantiateComponentWithRelations(
            _enemyPrefab,
            position,
            Quaternion.identity,
            parentTransform: null
        );

        // enemy._healthSystem is injected
        // enemy._animator, enemy._rigidbody are auto-wired
        // Ready to use immediately!
    }
}
```

**That's it!** Both DI injection and relational component wiring happen automatically.

---

## 📦 What's Included in This Sample

This sample provides a complete working example:

- **Scripts/SpawnerZenject.cs** - Runtime instantiation using `InstantiateComponentWithRelations()`
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

## 🎯 Common Use Cases

### Scene Objects with Both DI and Hierarchy References

Perfect for player controllers, managers, and gameplay systems:

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

### Runtime-Spawned Prefabs

For enemies, projectiles, and dynamic objects:

```csharp
public class ProjectileSpawner : MonoBehaviour
{
    [Inject] private DiContainer _container;
    [SerializeField] private Projectile _projectilePrefab;

    public void Fire(Vector3 position, Vector3 direction)
    {
        // Both DI injection and relational component wiring happen here
        Projectile projectile = _container.InstantiateComponentWithRelations(
            _projectilePrefab,
            position,
            Quaternion.LookRotation(direction)
        );

        projectile.Launch(direction);
    }
}
```

### Complex Prefab Hierarchies

For UI panels, vehicles, or multi-part systems:

```csharp
public class VehicleFactory : MonoBehaviour
{
    [Inject] private DiContainer _container;
    [SerializeField] private GameObject _vehiclePrefab;

    public GameObject CreateVehicle()
    {
        // Instantiate with DI
        GameObject vehicle = _container.InstantiatePrefab(_vehiclePrefab);

        // Wire up entire hierarchy - all nested components get relational wiring
        _container.AssignRelationalHierarchy(vehicle, includeInactiveChildren: true);

        return vehicle;
    }
}
```

### Factory Pattern with Relational Components

Combine Zenject factories with relational wiring:

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
   - Or manually call `_container.AssignRelationalComponents(this)` in your component

3. **Are you using the right attributes?**
   - Fields need `[SiblingComponent]`, `[ParentComponent]`, or `[ChildComponent]` attributes
   - These are different from `[Inject]` - you can use both on the same component

4. **Runtime instantiation not working?**
   - Use `_container.InstantiateComponentWithRelations()` instead of regular Zenject methods
   - Regular `InstantiatePrefab()` won't trigger relational wiring

5. **Check your filters:**
   - `TagFilter` must match an existing Unity tag exactly
   - `NameFilter` is case-sensitive

### Do I need to call AssignRelationalComponents() in Awake()?

**No!** The integration handles this automatically:

- **Scene objects:** Wired when you enable "Assign Scene On Initialize" (recommended)
- **Runtime objects:** Wired when you call `InstantiateComponentWithRelations()`

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

- [Relational Components Guide](../../RELATIONAL_COMPONENTS.md) - Complete attribute reference and recipes
- [Getting Started](../../GETTING_STARTED.md) - Unity Helpers quick start guide
- [Main README](../../README.md) - Full feature overview

**Zenject Documentation:**

- [Zenject GitHub](https://github.com/modesttree/Zenject) - Official Zenject documentation
- [Extenject GitHub](https://github.com/svermeulen/Extenject) - Community fork with updates

**Troubleshooting:**

- [Relational Components Troubleshooting](../../RELATIONAL_COMPONENTS.md#troubleshooting) - Detailed solutions
- [DI Integration Testing Guide](../../RELATIONAL_COMPONENTS.md#di-integrations-testing-and-edge-cases) - Advanced scenarios

---

## 🎓 Next Steps

1. **Try the sample scene:** Open `Zenject_Sample.unity` and press Play
2. **Read the scripts:** See how `SpawnerZenject` and `RelationalConsumer` work
3. **Add to your project:** Add `RelationalComponentsInstaller` to your `SceneContext`
4. **Explore attributes:** Check out the [Relational Components Guide](../../RELATIONAL_COMPONENTS.md) for all options

---

## 🔄 Comparison: Zenject vs VContainer Integration

If you're choosing between Zenject and VContainer, here's how the integrations differ:

| Feature | Zenject | VContainer |
|---------|---------|------------|
| Setup | Add installer to SceneContext | Call in LifetimeScope.Configure() |
| Scene wiring | Toggle on installer | Automatic |
| Runtime instantiation | `InstantiateComponentWithRelations()` | `BuildUpWithRelations()` |
| Performance | Good | Slightly faster |
| Maintenance | Community-maintained | Actively developed |

Both integrations provide the same relational component features - choose based on your DI framework preference.

---

## Made with ❤️ by Wallstop Studios

*Unity Helpers is production-ready and actively maintained. [Star the repo](https://github.com/wallstop/unity-helpers) if you find it useful!*
