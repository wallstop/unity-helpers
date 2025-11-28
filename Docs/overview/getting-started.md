# Getting Started with Unity Helpers

**Welcome! You're about to save yourself weeks of repetitive work.**

Unity Helpers is a battle-tested toolkit that eliminates the boring, repetitive code you're tired of writing. This guide gets you productive in 5 minutes, whether you're a beginner or a senior engineer.

## What Makes This Worth Your Time?

**Three core principles that save you actual hours:**

### 1. 🎯 Zero Boilerplate

**APIs that handle the tedious stuff:**

- Random selection with weights? → `random.NextWeightedIndex(weights)`
- Auto-wire components? → `[SiblingComponent] private Animator animator;`
- Gaussian distribution? Perlin noise? → Built-in, one method call

**Self-documenting code:**

```csharp
[SiblingComponent] private Animator animator;                      // Clear intent
[ParentComponent(OnlyAncestors = true)] private Rigidbody2D rb;  // Explicit search
[ChildComponent(MaxDepth = 1)] private Collider2D[] colliders;   // Limited scope
```

**Helpful errors that save debugging time:**

- Missing components? → Full GameObject path + component type
- Invalid queries? → Explanation of what went wrong + how to fix it
- Schema issues? → Specific guidance for your serialization problem

### 2. ⚡ Performance-Proven

**Measurable speed improvements:**

- **10-15x faster** random generation (655M ops/sec vs 65M ops/sec)
- **100x faster** reflection (2ns vs 200ns field access)
- **O(log n)** spatial queries scale to millions of objects
- **Zero GC** with buffering pattern

**Real-world impact:**

- Stable 60 FPS with 1000+ AI agents querying neighbors
- No allocation spikes from pooled collections
- Deterministic replays with seedable RNG

### 3. ✅ Production-Ready

**Quality you can trust:**

- ✅ **4,000+ automated tests** - Edge cases covered before you hit them
- ✅ **Shipped in commercial games** - Battle-tested at scale
- ✅ **IL2CPP/WebGL compatible** - Works with aggressive compilers
- ✅ **Schema evolution** - Player saves never break from updates
- ✅ **SINGLE_THREADED optimized** - 10-20% faster on WebGL

**What this means for you:**

- Ship confidently knowing edge cases are handled
- No "works in editor but not in build" surprises
- Update your game without corrupting player data

---

## Choose Your Path

### 🎯 Path 1: "I Have a Specific Problem"

Jump directly to the solution you need:

**Performance Issues?**

- Slow random number generation → [Random Generators](#random-in-60-seconds)
- Too many objects to search → [Spatial Queries](#spatial-queries-in-60-seconds)
- Frame drops from allocations → [Buffering Pattern](../../README.md#buffering-pattern)

**Workflow Issues?**

- Writing too much GetComponent → [Auto Component Wiring](#component-wiring-in-60-seconds)
- Manual sprite animation setup → [Editor Tools](../features/editor-tools/editor-tools-guide.md)
- Prefab validation problems → [Prefab Checker](../features/editor-tools/editor-tools-guide.md#prefab-checker)

**Architecture Issues?**

- Need global settings → [Singletons](../features/utilities/singletons.md)
- Need buff/debuff system → [Effects System](../features/effects/effects-system.md)
- Need save/load system → [Serialization](../features/serialization/serialization.md)

### 📚 Path 2: "I Want to Understand Everything"

Comprehensive deep-dive (best for team leads and senior developers):

1. Read [Main Documentation](../../README.md) - Full feature overview
2. Review [Features Documentation](index.md) - Detailed API documentation
3. Explore category-specific guides as needed

### 💡 Path 3: "I Learn Best from Examples"

See it working first, understand the theory later:

1. Follow the [3 Quick Wins](#three-quick-wins-5-minutes) below
2. Clone relevant examples from [Use Cases](../../README.md#use-cases--examples)
3. Modify examples for your specific needs
4. Read the detailed guides when you need to go deeper

---

## Installation (60 Seconds)

### Unity Package Manager (Recommended)

Add to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.wallstop-studios.unity-helpers": "https://github.com/wallstop/unity-helpers.git"
  }
}
```

Or in Unity:

1. Open **Window > Package Manager**
2. Click **+** → **Add package from git URL...**
3. Enter: `https://github.com/wallstop/unity-helpers.git`

### Verify Installation

Check that the package appears in Package Manager under "Custom". You should see:

- **Name:** Unity Helpers
- **Version:** (current version)
- **Author:** Wallstop Studios

---

## Three Quick Wins (5 Minutes)

<a id="random-in-60-seconds"></a>

### 1. Random in 60 Seconds 🟢 Beginner

**Problem:** Unity's `UnityEngine.Random` is slow and not seedable.

**Solution:**

```csharp
using WallstopStudios.UnityHelpers.Core.Random;
using WallstopStudios.UnityHelpers.Core.Extension;

public class LootDrop : MonoBehaviour
{
    void Start()
    {
        // 10-15x faster than UnityEngine.Random
        IRandom rng = PRNG.Instance;

        // Basic usage
        int damage = rng.Next(10, 20);
        float chance = rng.NextFloat();

        // Advanced: weighted random selection
        string[] loot = { "Common", "Rare", "Epic", "Legendary" };
        float[] weights = { 0.6f, 0.25f, 0.10f, 0.05f };
        int index = rng.NextWeightedIndex(weights);
        Debug.Log($"Dropped: {loot[index]}");
    }
}
```

> ⚠️ **Common Mistake:** Don't use `UnityEngine.Random` and `PRNG.Instance` together in the
> same class - pick one and stick with it for consistent results.

**Learn More:** [Random Performance](../performance/random-performance.md)

---

<a id="component-wiring-in-60-seconds"></a>

### 2. Component Wiring in 60 Seconds 🟢 Beginner

**Problem:** Writing `GetComponent` calls everywhere is tedious and error-prone.

**Solution:**

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class Player : MonoBehaviour
{
    // Auto-finds SpriteRenderer on same GameObject
    [SiblingComponent]
    private SpriteRenderer spriteRenderer;

    // Auto-finds Rigidbody2D in parent hierarchy
    [ParentComponent]
    private Rigidbody2D rigidbody;

    // Auto-finds all Collider2D in immediate children only
    [ChildComponent(OnlyDescendants = true, MaxDepth = 1)]
    private Collider2D[] childColliders;

    void Awake()
    {
        // One call wires everything!
        this.AssignRelationalComponents();

        // Now use them
        spriteRenderer.color = Color.red;
        rigidbody.velocity = Vector2.up * 5f;
        Debug.Log($"Found {childColliders.Length} child colliders");
    }
}
```

> ⚠️ **Common Mistake:** Don't call `AssignRelationalComponents()` in `Update()` -
> it should only run once during initialization (Awake/Start).

**Learn More:** [Relational Components](../features/relational-components/relational-components.md)

---

#### Using With DI Containers (VContainer/Zenject/Reflex)

- If you use dependency injection, you can auto-populate relational fields right after DI injection.
- Quick setup:
  - VContainer: in `LifetimeScope.Configure`, call `builder.RegisterRelationalComponents()`.
  - Zenject/Extenject: add `RelationalComponentsInstaller` to your `SceneContext` and (optionally) enable the scene scan on initialize.
  - Reflex: attach `RelationalComponentsInstaller` alongside your `SceneScope`. The installer binds the assigner, hydrates the active scene, and can listen for additive scenes. Use `ContainerRelationalExtensions` helpers (`InjectWithRelations`, `InstantiateGameObjectWithRelations`, etc.) when spawning objects through the container.
- Samples: [DI – VContainer](../../Samples~/DI%20-%20VContainer/README.md), [DI – Zenject](../../Samples~/DI%20-%20Zenject/README.md), [DI – Reflex](../../Samples~/DI%20-%20Reflex/README.md)
- Full guide with scenarios and testing tips: [Dependency Injection Integrations](../features/relational-components/relational-components.md#dependency-injection-integrations)

<a id="spatial-queries-in-60-seconds"></a>

### 3. Spatial Queries in 60 Seconds 🟡 Intermediate

**Problem:** Finding nearby objects with `FindObjectsOfType` and distance checks is O(n) and slow.

**Solution:**

```csharp
using WallstopStudios.UnityHelpers.Core.DataStructure;
using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    private QuadTree2D<Enemy> enemyTree;
    private List<Enemy> nearbyBuffer = new(64); // Reusable buffer

    void Start()
    {
        // Build tree once (O(n log n))
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        enemyTree = new QuadTree2D<Enemy>(enemies, e => e.transform.position);
    }

    public List<Enemy> GetEnemiesNearPlayer(Vector2 playerPos, float radius)
    {
        nearbyBuffer.Clear();

        // Fast query: O(log n) instead of O(n)
        enemyTree.GetElementsInRange(playerPos, radius, nearbyBuffer);

        return nearbyBuffer;
    }
}
```

> ⚠️ **Common Mistake:** Spatial trees are **immutable** - you must rebuild the tree
> when enemy positions change. For frequently moving objects, use `SpatialHash2D` instead.

**Learn More:**

- [2D Spatial Trees Guide](../features/spatial/spatial-trees-2d-guide.md)
- [Performance Benchmarks](../performance/spatial-tree-2d-performance.md)

---

## What Should I Learn Next?

Based on your needs:

### For Gameplay Programmers

1. **Master the Effects System** - Data-driven buffs/debuffs
   - Start: [Effects System TL;DR](../features/effects/effects-system.md#tldr--what-problem-this-solves)
   - Why: Build status effects without writing repetitive code

2. **Use Spatial Trees for AI** - Efficient awareness systems
   - Start: [Spatial Trees 2D Guide](../features/spatial/spatial-trees-2d-guide.md)
   - Why: Make enemy AI scale to hundreds of units

3. **Learn Serialization** - Save systems and networking
   - Start: [Serialization Guide](../features/serialization/serialization.md)
   - Why: Robust save/load with Unity types supported

### For Tools/Editor Programmers

1. **Explore Editor Tools** - Automate your asset pipeline
   - Start: [Editor Tools Guide](../features/editor-tools/editor-tools-guide.md)
   - Why: 20+ tools for sprites, animations, validation, and more

2. **Use ScriptableObject Singletons** - Global settings management
   - Start: [Singletons Guide](../features/utilities/singletons.md)
   - Why: Auto-created, ODIN-compatible config assets

3. **Master Property Drawers** - Better inspector workflows
   - Start: [Property Drawers](../features/editor-tools/editor-tools-guide.md#property-drawers--attributes)
   - Why: Conditional fields, dropdowns, validation

### For Performance-Focused Developers

1. **Study Data Structures** - Choose the right container
   - Start: [Data Structures Guide](../features/utilities/data-structures.md)
   - Why: Heaps, tries, sparse sets, and more with clear trade-offs

2. **Use Advanced Math Helpers** - Avoid common pitfalls
   - Start: [Math & Extensions](../features/utilities/math-and-extensions.md)
   - Why: Robust modulo, geometry, color averaging, and more

3. **Adopt the Buffering Pattern** - Zero-allocation queries
   - Start: [Buffering Pattern](../../README.md#buffering-pattern)
   - Why: Stable GC even under load

---

## Common Questions

### "Is this production-ready?"

Yes! Unity Helpers is:

- ✅ Used in shipped commercial games
- ✅ 4,000+ automated test cases
- ✅ Compatible with Unity 2022, 2023, and Unity 6
- ✅ Zero external dependencies
- ✅ **Fully WebGL/IL2CPP compatible** with optimized SINGLE_THREADED hot paths
- ✅ **Multiplatform support** - Desktop, Mobile, Web, and Consoles
- ⚠️ Requires .NET Standard 2.1

### "Will this conflict with my existing code?"

No! Unity Helpers:

- ✅ Uses namespaces (`WallstopStudios.UnityHelpers.*`)
- ✅ Doesn't modify Unity types or global state
- ✅ Opt-in for all features - use what you need

### "How do I get help?"

1. Check the [Troubleshooting section](../features/relational-components/relational-components.md#troubleshooting)
   in the relevant guide
2. Search the [GitHub Issues](https://github.com/wallstop/unity-helpers/issues)
3. Open a new issue with code examples and error messages

### "Can I use this in commercial projects?"

Yes! Unity Helpers is released under the [MIT License](../project/license.md) -
use it freely in commercial projects.

---

## Next Steps

Pick one feature that solves your immediate problem:

| Your Need             | Start Here                                                                          | Time to Learn |
| --------------------- | ----------------------------------------------------------------------------------- | ------------- |
| Faster random numbers | [Random Performance](../performance/random-performance.md)                          | 5 min         |
| Auto-wire components  | [Relational Components](../features/relational-components/relational-components.md) | 10 min        |
| Spatial queries       | [2D Spatial Trees](../features/spatial/spatial-trees-2d-guide.md)                   | 15 min        |
| Buff/debuff system    | [Effects System](../features/effects/effects-system.md)                             | 20 min        |
| Save/load data        | [Serialization](../features/serialization/serialization.md)                         | 20 min        |
| Editor automation     | [Editor Tools](../features/editor-tools/editor-tools-guide.md)                      | 30 min        |
| Global settings       | [Singletons](../features/utilities/singletons.md)                                   | 10 min        |

---

**Ready to dive deeper?** Return to the [main README](../../README.md) for the complete feature list.

**Building something cool?** We'd love to hear about it! Share your experience by opening an
[issue](https://github.com/wallstop/unity-helpers/issues).

---

## 📚 Related Documentation

**Core Guides:**

- [Main README](../../README.md) - Complete feature overview
- [Feature Index](index.md) - Alphabetical reference
- [Glossary](glossary.md) - Term definitions

**Deep Dives:**

- [Relational Components](../features/relational-components/relational-components.md) - Auto-wiring guide
- [Effects System](../features/effects/effects-system.md) - Buff/debuff system
- [Spatial Trees 2D](../features/spatial/spatial-trees-2d-guide.md) - Fast spatial queries
- [Serialization](../features/serialization/serialization.md) - Save systems and networking
- [Editor Tools](../features/editor-tools/editor-tools-guide.md) - Asset pipeline automation

**DI Integration:**

- [VContainer Sample](../../Samples~/DI%20-%20VContainer/README.md) - VContainer integration guide
- [Zenject Sample](../../Samples~/DI%20-%20Zenject/README.md) - Zenject integration guide

**Need help?** [Open an issue](https://github.com/wallstop/unity-helpers/issues) or check [Troubleshooting](../features/relational-components/relational-components.md#troubleshooting)
