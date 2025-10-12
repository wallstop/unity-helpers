# Getting Started with Unity Helpers

Welcome! Unity Helpers is a comprehensive toolkit designed to make Unity development faster,
more reliable, and more enjoyable. This guide will help you get started based on your learning style.

## Choose Your Path

### 🎯 Path 1: "I Have a Specific Problem"

Jump directly to the solution you need:

**Performance Issues?**
- Slow random number generation → [Random Generators](#random-in-60-seconds)
- Too many objects to search → [Spatial Queries](#spatial-queries-in-60-seconds)
- Frame drops from allocations → [Buffering Pattern](README.md#buffering-pattern)

**Workflow Issues?**
- Writing too much GetComponent → [Auto Component Wiring](#component-wiring-in-60-seconds)
- Manual sprite animation setup → [Editor Tools](EDITOR_TOOLS_GUIDE.md)
- Prefab validation problems → [Prefab Checker](EDITOR_TOOLS_GUIDE.md#prefab-checker)

**Architecture Issues?**
- Need global settings → [Singletons](SINGLETONS.md)
- Need buff/debuff system → [Effects System](EFFECTS_SYSTEM.md)
- Need save/load system → [Serialization](SERIALIZATION.md)

### 📚 Path 2: "I Want to Understand Everything"

Comprehensive deep-dive (best for team leads and senior developers):

1. Read [Main Documentation](README.md) - Full feature overview
2. Review [Features Documentation](FEATURES.md) - Detailed API documentation
3. Explore category-specific guides as needed

### 💡 Path 3: "I Learn Best from Examples"

See it working first, understand the theory later:

1. Follow the [3 Quick Wins](#three-quick-wins-5-minutes) below
2. Clone relevant examples from [Use Cases](README.md#use-cases--examples)
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

**Learn More:** [Random Performance](RANDOM_PERFORMANCE.md)

---

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

**Learn More:** [Relational Components](RELATIONAL_COMPONENTS.md)

---

#### Using With DI Containers (VContainer/Zenject)

- If you use dependency injection, you can auto-populate relational fields right after DI injection.
- Quick setup:
  - VContainer: in `LifetimeScope.Configure`, call `builder.RegisterRelationalComponents()`.
  - Zenject: add `RelationalComponentsInstaller` to your `SceneContext` and (optionally) enable the scene scan on initialize.
- Full guide with scenarios and testing tips: RELATIONAL_COMPONENTS.md#dependency-injection-integrations

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
- [2D Spatial Trees Guide](SPATIAL_TREES_2D_GUIDE.md)
- [Performance Benchmarks](SPATIAL_TREE_2D_PERFORMANCE.md)

---

## What Should I Learn Next?

Based on your needs:

### For Gameplay Programmers

1. **Master the Effects System** - Data-driven buffs/debuffs
   - Start: [Effects System TL;DR](EFFECTS_SYSTEM.md#tldr--what-problem-this-solves)
   - Why: Build status effects without writing repetitive code

2. **Use Spatial Trees for AI** - Efficient awareness systems
   - Start: [Spatial Trees 2D Guide](SPATIAL_TREES_2D_GUIDE.md)
   - Why: Make enemy AI scale to hundreds of units

3. **Learn Serialization** - Save systems and networking
   - Start: [Serialization Guide](SERIALIZATION.md)
   - Why: Robust save/load with Unity types supported

### For Tools/Editor Programmers

1. **Explore Editor Tools** - Automate your asset pipeline
   - Start: [Editor Tools Guide](EDITOR_TOOLS_GUIDE.md)
   - Why: 20+ tools for sprites, animations, validation, and more

2. **Use ScriptableObject Singletons** - Global settings management
   - Start: [Singletons Guide](SINGLETONS.md)
   - Why: Auto-created, ODIN-compatible config assets

3. **Master Property Drawers** - Better inspector workflows
   - Start: [Property Drawers](EDITOR_TOOLS_GUIDE.md#property-drawers--attributes)
   - Why: Conditional fields, dropdowns, validation

### For Performance-Focused Developers

1. **Study Data Structures** - Choose the right container
   - Start: [Data Structures Guide](DATA_STRUCTURES.md)
   - Why: Heaps, tries, sparse sets, and more with clear trade-offs

2. **Use Advanced Math Helpers** - Avoid common pitfalls
   - Start: [Math & Extensions](MATH_AND_EXTENSIONS.md)
   - Why: Robust modulo, geometry, color averaging, and more

3. **Adopt the Buffering Pattern** - Zero-allocation queries
   - Start: [Buffering Pattern](README.md#buffering-pattern)
   - Why: Stable GC even under load

---

## Common Questions

### "Is this production-ready?"

Yes! Unity Helpers is:
- ✅ Used in shipped commercial games
- ✅ 4,000+ automated test cases
- ✅ Compatible with Unity 2022, 2023, and Unity 6
- ✅ Zero external dependencies

### "Will this conflict with my existing code?"

No! Unity Helpers:
- ✅ Uses namespaces (`WallstopStudios.UnityHelpers.*`)
- ✅ Doesn't modify Unity types or global state
- ✅ Opt-in for all features - use what you need

### "How do I get help?"

1. Check the [Troubleshooting section](RELATIONAL_COMPONENTS.md#troubleshooting)
   in the relevant guide
2. Search the [GitHub Issues](https://github.com/wallstop/unity-helpers/issues)
3. Open a new issue with code examples and error messages

### "Can I use this in commercial projects?"

Yes! Unity Helpers is released under the [MIT License](LICENSE.md) -
use it freely in commercial projects.

---

## Next Steps

Pick one feature that solves your immediate problem:

| Your Need | Start Here | Time to Learn |
|-----------|------------|---------------|
| Faster random numbers | [Random Performance](RANDOM_PERFORMANCE.md) | 5 min |
| Auto-wire components | [Relational Components](RELATIONAL_COMPONENTS.md) | 10 min |
| Spatial queries | [2D Spatial Trees](SPATIAL_TREES_2D_GUIDE.md) | 15 min |
| Buff/debuff system | [Effects System](EFFECTS_SYSTEM.md) | 20 min |
| Save/load data | [Serialization](SERIALIZATION.md) | 20 min |
| Editor automation | [Editor Tools](EDITOR_TOOLS_GUIDE.md) | 30 min |
| Global settings | [Singletons](SINGLETONS.md) | 10 min |

---

**Ready to dive deeper?** Return to the [main README](README.md) for the complete feature list.

**Building something cool?** We'd love to hear about it! Share your experience in
[GitHub Discussions](https://github.com/wallstop/unity-helpers/discussions).
