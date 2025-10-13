# VContainer Integration - Unity Helpers

## Why This Integration Matters

**The Problem:** When using dependency injection with VContainer, you often need to wire up both:

1. **Dependencies** (injected via constructor/properties)
2. **Hierarchy references** (SpriteRenderer, Rigidbody2D, child colliders, etc.)

Doing this manually means writing boilerplate in every component.

**The Solution:** Unity Helpers' VContainer integration automatically wires up relational component fields **right after** DI injection completes - giving you the best of both worlds with zero extra code.

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

### Step 1: Register the Integration

In your `LifetimeScope`, add one line:

```csharp
using VContainer;
using VContainer.Unity;
using WallstopStudios.UnityHelpers.Integrations.VContainer;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // Your existing registrations...
        builder.Register<PlayerController>(Lifetime.Singleton);
        builder.Register<IHealthSystem, HealthSystem>(Lifetime.Scoped);

        // ✨ Add this line to enable relational components with DI
        builder.RegisterRelationalComponents();
    }
}
```

**That's it!** All scene components with relational attributes are now automatically wired after DI injection.

### Step 2: Use With Runtime Instantiation

When spawning prefabs at runtime, use `BuildUpWithRelations` instead of just `Instantiate`:

```csharp
using UnityEngine;
using VContainer;
using WallstopStudios.UnityHelpers.Integrations.VContainer;

public class EnemySpawner : MonoBehaviour
{
    [Inject] private IObjectResolver _resolver;
    [SerializeField] private Enemy _enemyPrefab;

    public void SpawnEnemy(Vector3 position)
    {
        Enemy enemy = Instantiate(_enemyPrefab, position, Quaternion.identity);

        // ✨ Performs both DI injection AND relational component wiring
        _resolver.BuildUpWithRelations(enemy);

        // enemy._healthSystem is injected
        // enemy._animator, enemy._rigidbody are auto-wired
        // Ready to use immediately!
    }
}
```

---

## 📦 What's Included in This Sample

This sample provides a complete working example:

- **Scripts/GameLifetimeScope.cs** - Example LifetimeScope with integration registered
- **Scripts/Spawner.cs** - Runtime instantiation using `BuildUpWithRelations()`
- **Scripts/RelationalConsumer.cs** - Component demonstrating relational attributes
- **Prefabs/RelationalConsumer.prefab** - Example prefab with relational fields
- **Prefabs/Spawner.prefab** - Spawner prefab showing runtime usage
- **Scenes/VContainer_Sample.unity** - Complete working scene ready to play

### How to Import This Sample

1. Open Unity Package Manager
2. Find **Unity Helpers** in the package list
3. Expand the **Samples** section
4. Click **Import** next to "DI - VContainer"
5. Open `Scenes/VContainer_Sample.unity` and press Play

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

    // Everything wired automatically - no Awake() needed!

    void Update()
    {
        Vector2 input = _input.GetMovementInput();
        _rigidbody.velocity = input * moveSpeed;
        _animator.SetFloat("Speed", input.magnitude);
    }
}
```

### Runtime-Spawned Prefabs

For enemies, projectiles, and dynamic objects:

```csharp
public class ProjectileSpawner : MonoBehaviour
{
    [Inject] private IObjectResolver _resolver;
    [SerializeField] private Projectile _projectilePrefab;

    public void Fire(Vector3 position, Vector3 direction)
    {
        Projectile projectile = Instantiate(_projectilePrefab, position, Quaternion.identity);

        // Both DI injection and relational component wiring happen here
        _resolver.BuildUpWithRelations(projectile);

        projectile.Launch(direction);
    }
}
```

### Complex Prefab Hierarchies

For UI panels, vehicles, or multi-part systems:

```csharp
public class VehicleFactory : MonoBehaviour
{
    [Inject] private IObjectResolver _resolver;
    [SerializeField] private GameObject _vehiclePrefab;

    public GameObject CreateVehicle()
    {
        GameObject vehicle = Instantiate(_vehiclePrefab);

        // Wire up entire hierarchy - all nested components get DI + relational wiring
        _resolver.AssignRelationalHierarchy(vehicle, includeInactiveChildren: true);

        return vehicle;
    }
}
```

---

## 🔧 Advanced Configuration

### Exclude Inactive GameObjects from Scene Scanning

By default, inactive GameObjects are included in the initial scene scan. To scan only active objects:

```csharp
protected override void Configure(IContainerBuilder builder)
{
    builder.RegisterRelationalComponents(
        new RelationalSceneAssignmentOptions(includeInactive: false)
    );
}
```

### Manual Component Wiring (Without BuildUp)

If you need to wire relational components without DI injection:

```csharp
[Inject] private IObjectResolver _resolver;

void WireComponentOnly(MonoBehaviour component)
{
    // Only assigns relational component fields, skips DI injection
    _resolver.AssignRelationalComponents(component);
}
```

### Performance: Prewarming Reflection Caches

For large projects, prewarm reflection caches during loading to avoid first-use stalls:

```csharp
using WallstopStudios.UnityHelpers.Core.Attributes;

void Start()
{
    // Call once during bootstrap/loading screen
    RelationalComponentInitializer.Initialize();
}
```

Or enable auto-prewarm on the `AttributeMetadataCache` asset:

1. Find the asset: `Assets > Create > Wallstop Studios > Unity Helpers > Attribute Metadata Cache`
2. Enable **"Prewarm Relational On Load"** in the Inspector

---

## 🧰 Additional Helpers & Recipes

### One-liners for DI + Relational Wiring

```csharp
// Inject + assign a single component
resolver.InjectWithRelations(component);

// Instantiate a component prefab + inject + assign
var comp = resolver.InstantiateComponentWithRelations(prefabComp, parent);

// Inject + assign a whole hierarchy
resolver.InjectGameObjectWithRelations(root, includeInactiveChildren: true);

// Instantiate a GameObject prefab + inject + assign hierarchy
var go = resolver.InstantiateGameObjectWithRelations(prefabGo, parent);
```

### Additive Scenes & Options

The registration can enable an additive-scene listener that hydrates relational fields in newly loaded scenes, and you can customize scan behavior:

```csharp
protected override void Configure(IContainerBuilder builder)
{
    var options = new RelationalSceneAssignmentOptions(
        includeInactive: true,
        useSinglePassScan: true
    );
    builder.RegisterRelationalComponents(options, enableAdditiveSceneListener: true);
}
```

### Pools

Use DI-aware pools to auto-inject and assign on rent:

```csharp
// Component pool
var pool = RelationalObjectPools.CreatePoolWithRelations(
    resolver,
    createFunc: () => Instantiate(componentPrefab)
);
var item = pool.GetWithRelations(resolver);

// GameObject pool
var goPool = RelationalObjectPools.CreateGameObjectPoolWithRelations(prefab);
var instance = goPool.GetWithRelations(resolver);
```

---

## ❓ Troubleshooting

### My relational fields are null even with the integration

**Check these common issues:**

1. **Did you register the integration?**
   - Ensure `builder.RegisterRelationalComponents()` is called in your `LifetimeScope.Configure()`

2. **Are you using the right attributes?**
   - Fields need `[SiblingComponent]`, `[ParentComponent]`, or `[ChildComponent]` attributes
   - These are different from `[Inject]` - you can use both on the same component

3. **Runtime instantiation not working?**
   - Use `_resolver.BuildUpWithRelations(component)` instead of just `Instantiate()`
   - Regular `Instantiate()` won't trigger the integration

4. **Check your filters:**
   - `TagFilter` must match an existing Unity tag exactly
   - `NameFilter` is case-sensitive

### Do I need to call AssignRelationalComponents() in Awake()?

**No!** The integration handles this automatically:

- **Scene objects:** Wired during scene initialization (after container builds)
- **Runtime objects:** Wired when you call `BuildUpWithRelations()`

Only call `AssignRelationalComponents()` manually if you're not using the DI integration.

### Does this work without VContainer?

**Yes!** The integration gracefully falls back to standard Unity Helpers behavior if VContainer isn't detected. You can:

- Adopt incrementally without breaking existing code
- Use in projects that mix DI and non-DI components
- Remove VContainer later without refactoring all your components

### Performance impact?

**Minimal:** Relational component assignment happens once per component at initialization time. After that, there's zero runtime overhead - the references are just regular fields.

**Optimization tips:**

- Use `MaxDepth` to limit hierarchy traversal
- Use `TagFilter` or `NameFilter` to narrow searches
- Use `OnlyDescendants`/`OnlyAncestors` to exclude self when appropriate

---

## 📚 Learn More

**Unity Helpers Documentation:**

- [Relational Components Guide](../../RELATIONAL_COMPONENTS.md) - Complete attribute reference and recipes
- [Getting Started](../../GETTING_STARTED.md) - Unity Helpers quick start guide
- [Main README](../../README.md) - Full feature overview

**VContainer Documentation:**

- [VContainer Official Docs](https://vcontainer.hadashikick.jp/) - Complete VContainer guide
- [VContainer GitHub](https://github.com/hadashiA/VContainer) - Source code and examples

**Troubleshooting:**

- [Relational Components Troubleshooting](../../RELATIONAL_COMPONENTS.md#troubleshooting) - Detailed solutions
- [DI Integration Testing Guide](../../RELATIONAL_COMPONENTS.md#di-integrations-testing-and-edge-cases) - Advanced scenarios

---

## 🎓 Next Steps

1. **Try the sample scene:** Open `VContainer_Sample.unity` and press Play
2. **Read the scripts:** See how `GameLifetimeScope` and `Spawner` work
3. **Add to your project:** Copy the pattern to your own LifetimeScope
4. **Explore attributes:** Check out the [Relational Components Guide](../../RELATIONAL_COMPONENTS.md) for all options

---

## Made with ❤️ by Wallstop Studios

*Unity Helpers is production-ready and actively maintained. [Star the repo](https://github.com/wallstop/unity-helpers) if you find it useful!*
