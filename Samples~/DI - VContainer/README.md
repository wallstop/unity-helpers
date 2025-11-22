# VContainer Integration - Unity Helpers

## Why This Integration Matters

**Stop Writing GetComponent Boilerplate in Every Single Script**

When using dependency injection with VContainer, you've solved half the problem - your service dependencies get injected cleanly. But you're **still stuck** writing repetitive `GetComponent` boilerplate for hierarchy references in every. single. MonoBehaviour.

**The Painful Reality:**

1. **Dependencies** → ✅ Handled by VContainer (IHealthSystem, IAudioService, etc.)
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

### Step 1: Register the Integration

In your `LifetimeScope`, enable the integration (the sample exposes the three toggles below in the inspector so you can experiment without touching code):

```csharp
using UnityEngine;
using VContainer;
using VContainer.Unity;
using WallstopStudios.UnityHelpers.Integrations.VContainer;

public sealed class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private bool _includeInactiveSceneObjects = true;
    [SerializeField] private bool _useSinglePassScan = true;
    [SerializeField] private bool _listenForAdditiveScenes = true;

    protected override void Configure(IContainerBuilder builder)
    {
        // Your existing registrations...
        builder.Register<PlayerController>(Lifetime.Singleton);
        builder.Register<IHealthSystem, HealthSystem>(Lifetime.Scoped);

        RelationalSceneAssignmentOptions options = new RelationalSceneAssignmentOptions(
            _includeInactiveSceneObjects,
            _useSinglePassScan
        );

        // ✨ Scene scan + optional additive-scene listener
        builder.RegisterRelationalComponents(options, _listenForAdditiveScenes);
    }
}
```

**That's it!** All scene components with relational attributes are now automatically wired after DI injection, and additively loaded scenes can opt-in to the same treatment.

> 💡 **Beginner tip:** Not sure what these options do? Leave them all enabled (the defaults). You can always tune them later.
>
> - `includeInactiveSceneObjects` → Wires disabled GameObjects too (usually what you want)
> - `useSinglePassScan` → Faster scanning (always leave this on)
> - `listenForAdditiveScenes` → Auto-wires newly loaded scenes (great for multi-scene setups)

### Step 2: Use With Runtime Instantiation

When spawning prefabs at runtime, use the helpers that combine instantiation, DI, and relational assignment:

```csharp
using UnityEngine;
using VContainer;
using WallstopStudios.UnityHelpers.Integrations.VContainer;

public sealed class EnemySpawner : MonoBehaviour
{
    [Inject] private IObjectResolver _resolver;
    [SerializeField] private Enemy _enemyPrefab;
    [SerializeField] private GameObject _enemySquadPrefab;

    public Enemy SpawnEnemy(Transform parent)
    {
        return _resolver.InstantiateComponentWithRelations(_enemyPrefab, parent);
    }

    public GameObject SpawnEnemySquad(Transform parent)
    {
        return _resolver.InstantiateGameObjectWithRelations(
            _enemySquadPrefab,
            parent,
            includeInactiveChildren: true
        );
    }

    public void HydrateExisting(GameObject root)
    {
        _resolver.AssignRelationalHierarchy(root, includeInactiveChildren: true);
    }
}
```

---

## 📦 What's Included in This Sample

This sample provides a complete working example:

- **Scripts/GameLifetimeScope.cs** - LifetimeScope with inspector-driven options for include-inactive, scan strategy, and additive-scene listening
- **Scripts/Spawner.cs** - Demonstrates `InstantiateComponentWithRelations`, `InstantiateGameObjectWithRelations`, pooling helpers, and hydrating existing hierarchies
- **Scripts/RelationalConsumer.cs** - Component demonstrating relational attributes
- **Prefabs/RelationalConsumer.prefab** - Example prefab with relational fields
- **Prefabs/Spawner.prefab** - Spawner prefab wired to the helper methods above
- **Scenes/VContainer_Sample.unity** - Complete working scene ready to play

### How to Import This Sample

1. Open Unity Package Manager
2. Find **Unity Helpers** in the package list
3. Expand the **Samples** section
4. Click **Import** next to "DI - VContainer"
5. Open `Scenes/VContainer_Sample.unity` and press Play

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

    // Everything wired automatically - no Awake() needed!

    void Update()
    {
        Vector2 input = _input.GetMovementInput();
        _rigidbody.velocity = input * moveSpeed;
        _animator.SetFloat("Speed", input.magnitude);
    }
}
```

### 🟡 Intermediate: "I'm spawning objects at runtime"

**Perfect for:** Enemy spawners, projectile systems, object pooling

**What you get:** One-line instantiation that handles DI injection + hierarchy wiring automatically

**Example:**

```csharp
public sealed class ProjectileSpawner : MonoBehaviour
{
    [Inject] private IObjectResolver _resolver;
    [SerializeField] private Projectile _projectilePrefab;

    public Projectile Fire(Vector3 position, Vector3 forward)
    {
        Projectile projectile = _resolver.InstantiateComponentWithRelations(_projectilePrefab);
        projectile.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
        projectile.Launch(forward);
        return projectile;
    }
}
```

### 🔴 Advanced: "I have complex hierarchies and custom workflows"

**Perfect for:** UI systems, vehicles with multiple parts, procedural generation, custom object pools

**What you get:** Full control over when and how wiring happens, with helpers for every scenario

**Example:**

```csharp
public sealed class VehicleFactory : MonoBehaviour
{
    [Inject] private IObjectResolver _resolver;
    [SerializeField] private GameObject _vehiclePrefab;

    public GameObject CreateVehicle(Transform parent)
    {
        return _resolver.InstantiateGameObjectWithRelations(
            _vehiclePrefab,
            parent,
            includeInactiveChildren: true
        );
    }
}
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

### Exclude Inactive GameObjects from Scene Scanning

By default, inactive GameObjects are included in the initial scene scan. To scan only active objects:

```csharp
protected override void Configure(IContainerBuilder builder)
{
    builder.RegisterRelationalComponents(
        new RelationalSceneAssignmentOptions(includeInactive: false),
        enableAdditiveSceneListener: false
    );
}
```

### Manual Wiring Helpers

If you need to hydrate instances that were created outside of the resolver:

```csharp
[Inject] private IObjectResolver _resolver;

void WireComponentOnly(MonoBehaviour component)
{
    // Only assigns relational component fields, skips DI injection
    _resolver.AssignRelationalComponents(component);
}

void WireHierarchy(GameObject root)
{
    _resolver.AssignRelationalHierarchy(root, includeInactiveChildren: true);
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
   - Use `_resolver.InstantiateComponentWithRelations(...)`, `_resolver.InstantiateGameObjectWithRelations(...)`, or `_resolver.AssignRelationalHierarchy(...)`
   - Regular `Instantiate()` on its own won't trigger relational wiring

4. **Check your filters:**
   - `TagFilter` must match an existing Unity tag exactly
   - `NameFilter` is case-sensitive

### Do I need to call AssignRelationalComponents() in Awake()?

**No!** The integration handles this automatically:

- **Scene objects:** Wired during scene initialization (after container builds)
- **Runtime objects:** Wired when you call any of the helper methods (`InstantiateComponentWithRelations`, `InstantiateGameObjectWithRelations`, `AssignRelationalHierarchy`, or the pooling `GetWithRelations` helpers)

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

- [Relational Components Guide](../../Docs/features/relational-components/relational-components.md) - Complete attribute reference and recipes
- [Getting Started](../../Docs/overview/getting-started.md) - Unity Helpers quick start guide
- [Main README](../../README.md) - Full feature overview

**VContainer Documentation:**

- [VContainer Official Docs](https://vcontainer.hadashikick.jp/) - Complete VContainer guide
- [VContainer GitHub](https://github.com/hadashiA/VContainer) - Source code and examples

**Troubleshooting:**

- [Relational Components Troubleshooting](../../Docs/features/relational-components/relational-components.md#troubleshooting) - Detailed solutions
- [DI Integration Testing Guide](../../Docs/features/relational-components/relational-components.md#di-integrations-testing-and-edge-cases) - Advanced scenarios

---

## 🎓 Next Steps

1. **Try the sample scene:** Open `VContainer_Sample.unity` and press Play
2. **Read the scripts:** See how `GameLifetimeScope` and `Spawner` work
3. **Add to your project:** Copy the pattern to your own LifetimeScope
4. **Explore attributes:** Check out the [Relational Components Guide](../../Docs/features/relational-components/relational-components.md) for all options

---

## Made with ❤️ by Wallstop Studios

*Unity Helpers is production-ready and actively maintained. [Star the repo](https://github.com/wallstop/unity-helpers) if you find it useful!*

