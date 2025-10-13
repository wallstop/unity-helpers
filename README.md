# Unity Helpers

[![Unity 2021.3+](https://img.shields.io/badge/Unity-2021.3%2B-000000?logo=unity&logoColor=white)](https://unity.com/releases/2021-lts)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![CSharpier](https://github.com/wallstop/unity-helpers/actions/workflows/csharpier.yml/badge.svg?branch=main)](https://github.com/wallstop/unity-helpers/actions/workflows/csharpier.yml)
[![Markdown & JSON Lint/Format](https://github.com/wallstop/unity-helpers/actions/workflows/markdown-json.yml/badge.svg?branch=main)](https://github.com/wallstop/unity-helpers/actions/workflows/markdown-json.yml)
[![Lint Docs Links](https://github.com/wallstop/unity-helpers/actions/workflows/lint-doc-links.yml/badge.svg?branch=main)](https://github.com/wallstop/unity-helpers/actions/workflows/lint-doc-links.yml)
[![Npm Publish](https://github.com/wallstop/unity-helpers/actions/workflows/npm-publish.yml/badge.svg?branch=main)](https://github.com/wallstop/unity-helpers/actions/workflows/npm-publish.yml)

A comprehensive collection of high-performance utilities, data structures, and editor tools for Unity game development. Unity Helpers provides everything from blazing-fast random number generators and spatial trees to powerful editor wizards and component relationship management.

---

**📚 New to Unity Helpers?** Start here: [Getting Started Guide](GETTING_STARTED.md)

**🔍 Looking for something specific?** Check the [Feature Index](INDEX.md)

**❓ Need a definition?** See the [Glossary](GLOSSARY.md)

---

## 👋 First Time Here? Choose Your Path

Unity Helpers provides tools for different roles and needs. Pick your path to get started quickly:

### 🎮 For Gameplay Programmers

**You want:** Faster iteration on game features without sacrificing performance

<!-- markdownlint-disable MD036 -->

**Your quick wins:**

1. **[Random Number Generators](#random-number-generators)** - 10-15x faster with extensive API

   - Weighted selection, Gaussian distributions, noise maps - all built-in
   - Seedable for deterministic gameplay (replays, networking)

2. **[Relational Components](#relational-components)** - Stop writing GetComponent boilerplate

   - `[SiblingComponent]`, `[ParentComponent]`, `[ChildComponent]` - that's it
   - Works with DI containers (VContainer/Zenject)

3. **[Effects System](#effects-attributes-and-tags)** - Data-driven buffs/debuffs
   - Designers create effects as ScriptableObjects
   - Automatic stacking, timing, and tag management

**Start here:** [Quick Start Guide](#quick-start-guide)

---

### 🔧 For Tools & Editor Developers

**You want:** Automate asset pipelines and validation workflows

**Your quick wins:**

1. **[Editor Tools](#editor-tools)** - 20+ tools for sprites, animations, validation

   - Sprite cropper, atlas generator, animation creator
   - Prefab checker with comprehensive validation rules

2. **[ScriptableObject Singletons](#singleton-utilities-odin-compatible)** - Global settings management

   - Auto-created from Resources/ folder
   - ODIN Inspector compatible

3. **[Reflection Helpers](REFLECTION_HELPERS.md)** - 100x faster than System.Reflection
   - IL-emitted delegates for field/property access
   - Safe for IL2CPP and AOT platforms

**Start here:** [Editor Tools Guide](EDITOR_TOOLS_GUIDE.md)

---

### ⚡ For Performance Engineers

**You want:** Optimize hotspots and eliminate GC pressure

**Your quick wins:**

1. **[Spatial Trees](#spatial-trees)** - O(log n) queries vs O(n) loops

   - QuadTree2D, KDTree2D/3D, RTree2D/3D
   - Scale to millions of objects

2. **[Buffering Pattern](#buffering-pattern)** - Zero-allocation queries

   - Reusable collections eliminate GC spikes
   - Professional-grade pooling with automatic cleanup

3. **[Data Structures](#data-structures)** - Production-ready containers
   - Heaps, tries, sparse sets with clear trade-offs
   - Performance benchmarks for informed decisions

**Start here:** [Spatial Trees 2D Guide](SPATIAL_TREES_2D_GUIDE.md) + [Performance Benchmarks](#performance)

---

### 🏗️ For Architects & Tech Leads

**You want:** Understand integration points and architectural patterns

**Your quick wins:**

1. **[DI Integration](#dependency-injection-integrations)** - VContainer & Zenject support

   - Automatic relational component wiring after DI injection
   - Scene and runtime instantiation patterns

2. **[Serialization](#serialization)** - JSON/Protobuf with Unity type support

   - Schema evolution for save files that never break
   - Pooled buffers for hot paths

3. **[Feature Index](INDEX.md)** - Complete feature catalog
   - Alphabetical reference with links
   - Quick navigation to any feature

**Start here:** [DI Integration Samples](#dependency-injection-integrations) or [Architecture Overview](#why-unity-helpers)

---

## ⚡ Top 5 Features That Will Save You Weeks

Unity Helpers isn't just about performance - it's about **eliminating entire categories of repetitive work**. Here are the five features that deliver the biggest time savings:

### 1. 🔌 Auto-Wire Components (Relational Components)

**Time saved: 10-20 minutes per script × 100+ scripts = 20+ hours**

Stop writing GetComponent boilerplate forever. Replace 20+ lines of repetitive code with 3 attributes.

```csharp
// ❌ OLD WAY: 20+ lines per script
void Awake() {
    sprite = GetComponent<SpriteRenderer>();
    if (sprite == null) Debug.LogError("Missing SpriteRenderer!");

    rigidbody = GetComponentInParent<Rigidbody2D>();
    if (rigidbody == null) Debug.LogError("Missing Rigidbody2D!");

    colliders = GetComponentsInChildren<Collider2D>();
    // 15 more lines...
}

// ✅ NEW WAY: 4 lines total
[SiblingComponent] private SpriteRenderer sprite;
[ParentComponent] private Rigidbody2D rigidbody;
[ChildComponent] private Collider2D[] colliders;
void Awake() => this.AssignRelationalComponents();
```

**Bonus:** Works with VContainer/Zenject for automatic DI + relational wiring!

[📖 Learn More](RELATIONAL_COMPONENTS.md) | [🎯 DI Integration](Samples~/DI%20-%20VContainer/README.md)

---

### 2. 🎮 Data-Driven Effects System

**Time saved: 2-4 hours per effect × 50 effects = 150+ hours**

Designers create buffs/debuffs without touching code. Zero programmer time after initial setup.

```csharp
// Create once (ScriptableObject in editor):
// - HasteEffect: Speed × 1.5, duration 5s, tag "Haste", particle effect

// Use everywhere (zero boilerplate):
player.ApplyEffect(hasteEffect);           // Apply buff
if (player.HasTag("Stunned")) return;      // Query state
player.RemoveAllEffectsWithTag("Haste");   // Batch removal
```

**What you get:**

- Automatic stacking & duration management
- Reference-counted tags for gameplay queries
- Cosmetic VFX/SFX that spawn/despawn automatically
- Designer-friendly iteration without code changes

[📖 Full Guide](EFFECTS_SYSTEM.md) | [🚀 5-Minute Tutorial](EFFECTS_SYSTEM_TUTORIAL.md)

---

### 3. 💾 Unity-Aware Serialization

**Time saved: 40+ hours on initial save system + preventing player data loss**

JSON/Protobuf that understands `Vector3`, `GameObject`, `Color` - no custom converters needed.

```csharp
// Vector3, Color, GameObject references just work:
var saveData = new SaveData {
    playerPosition = new Vector3(1, 2, 3),
    playerColor = Color.cyan,
    inventory = new List<GameObject>()
};

// One line to save:
byte[] data = Serializer.JsonSerialize(saveData);

// Schema evolution = never break old saves:
[ProtoMember(1)] public int gold;
[ProtoMember(2)] public Vector3 position;
// Adding new field? Old saves still load!
[ProtoMember(3)] public int level;  // Safe to add
```

**Real-world impact:** Ship updates without worrying about corrupting player saves.

[📖 Serialization Guide](SERIALIZATION.md)

---

### 4. 🎱 Professional Pooling (Buffers\<T\>)

**Time saved: Eliminates GC spikes = 5-10 FPS improvement in complex scenes**

Zero-allocation queries with automatic cleanup. Thread-safe, production-grade pooling in one line.

```csharp
// Get pooled buffer - automatically returned on scope exit
void ProcessEnemies(QuadTree2D<Enemy> enemyTree) {
    using var lease = Buffers<Enemy>.List.Get(out List<Enemy> buffer);

    // Use it for spatial query - zero allocations!
    enemyTree.GetElementsInRange(playerPos, 10f, buffer);

    foreach (Enemy enemy in buffer) {
        enemy.TakeDamage(5f);
    }

    // Buffer automatically returned to pool here - no cleanup needed
}
```

**Why this matters:**

- Stable 60 FPS under load (no GC spikes)
- AI systems querying hundreds of neighbors per frame
- Particle systems with thousands of particles
- Works for List, HashSet, Stack, Queue, and Arrays

[📖 Buffering Pattern](#buffering-pattern)

---

### 5. 🛠️ Editor Tools Suite

**Time saved: 1-2 hours per batch operation × weekly usage = hundreds of hours/year**

20+ tools that automate sprite cropping, animation creation, atlas generation, prefab validation.

**Common workflows:**

- **Sprite Cropper**: Add or remove transparent pixels from 500 sprites → 1 click (was: 30 minutes in Photoshop)
- **Animation Creator**: Bulk-create clips from naming patterns (`walk_0001.png`) → 1 minute (was: 20 minutes)
- **Prefab Checker**: Validate 200 prefabs for missing references → 1 click (was: manual QA)
- **Atlas Generator**: Create sprite atlases from regex/labels → automated (was: manual setup)

[📖 Editor Tools Guide](EDITOR_TOOLS_GUIDE.md)

---

## 💎 Hidden Gems Worth Discovering

These powerful utilities solve common problems but might not be obvious from feature names:

| Feature                                                              | What It Does                                          | Time Saved                           |
| -------------------------------------------------------------------- | ----------------------------------------------------- | ------------------------------------ |
| **[Predictive Targeting](MATH_AND_EXTENSIONS.md#predictive-target)** | Perfect ballistics for turrets/missiles in one call   | 2-3 hours per shooting system        |
| **[UpdateShapeToSprite()](MATH_AND_EXTENSIONS.md#unity-extensions)** | Collider instantly matches sprite changes at runtime  | 30 minutes per dynamic sprite system |
| **[Coroutine Jitter](MATH_AND_EXTENSIONS.md#unity-extensions)**      | Prevents 100 enemies polling on same frame            | Eliminates frame spikes              |
| **[GetAngleWithSpeed()](MATH_AND_EXTENSIONS.md#unity-extensions)**   | Smooth rotation toward target in one line             | 15 minutes per rotating entity       |
| **[IL-Emitted Reflection](REFLECTION_HELPERS.md)**                   | 100x faster than System.Reflection, IL2CPP safe       | Critical for serialization/modding   |
| **[SmartDestroy()](MATH_AND_EXTENSIONS.md#lifecycle-helpers)**       | Editor/runtime safe destruction (no scene corruption) | Prevents countless debugging hours   |
| **[Convex/Concave Hulls](HULLS.md)**                                 | Generate territory borders from point clouds          | 4-6 hours per hull algorithm         |

---

## Why Unity Helpers?

Unity Helpers was built to solve common game development challenges with **performance-first** solutions:

- 🚀 **10-15x faster** random number generation compared to Unity's built-in Random
- 🌳 **O(log n)** spatial queries instead of O(n) linear searches
- 🔧 **20+ editor tools** to streamline your workflow
- 📦 **Zero dependencies** - just import and use
- ✅ **Production-tested** in shipped games
- 🧪 **5000+ test cases** cover most of the public API and run before each release

**TL;DR — Why use this?**

- Ship faster with production‑ready utilities that are (much) faster than stock Unity options.
- Solve common problems: global settings/services, fast spatial queries, auto‑wiring components, robust serialization.
- 4,000+ tests and diagrams make behavior and trade‑offs clear.

**Who is this for?**

- Unity devs who want pragmatic, high‑quality building blocks without adopting a full framework.
- Teams that value performance, determinism, and predictable editor tooling.

---

## Installation

### As Unity Package (Recommended)

1. Open Unity Package Manager
2. _(Optional)_ Enable **Pre-release packages** for cutting-edge builds
3. Click the **+** dropdown → **Add package from git URL...**
4. Enter: `https://github.com/wallstop/unity-helpers.git`

**OR** add to your `manifest.json`:

```json
{
  "dependencies": {
    "com.wallstop-studios.unity-helpers": "https://github.com/wallstop/unity-helpers.git"
  }
}
```

### From NPM Registry

1. Open Unity Package Manager
2. _(Optional)_ Enable **Pre-release packages**
3. Open **Advanced Package Settings** (gear icon)
4. Add a new **Scoped Registry**:
   - **Name**: `NPM`
   - **URL**: `https://registry.npmjs.org`
   - **Scope(s)**: `com.wallstop-studios.unity-helpers`
5. Search for and install `com.wallstop-studios.unity-helpers`

### From Source

1. [Download the latest release](https://github.com/wallstop/unity-helpers/releases) or clone this repository
2. Copy the contents to your project's `Assets` folder
3. Unity will automatically import the package

---

## Compatibility

| Unity Version | Built-In             | URP                  | HDRP                 |
| ------------- | -------------------- | -------------------- | -------------------- |
| 2021          | Likely, but untested | Likely, but untested | Likely, but untested |
| 2022          | ✅ Compatible        | ✅ Compatible        | ✅ Compatible        |
| 2023          | ✅ Compatible        | ✅ Compatible        | ✅ Compatible        |
| Unity 6       | ✅ Compatible        | ✅ Compatible        | ✅ Compatible        |

### Platform Support

Unity Helpers is **fully multiplatform compatible** including:

- ✅ **WebGL** - Full support with optimized SINGLE_THREADED hot paths
- ✅ **IL2CPP** - Tested and compatible with ahead-of-time compilation
- ✅ **Mobile** (iOS, Android) - Production-ready with IL2CPP
- ✅ **Desktop** (Windows, macOS, Linux) - Full threading support
- ✅ **Consoles** - IL2CPP compatible

**Requirements:**

- **.NET Standard 2.1** - Required for core library features

### WebGL and Single-Threaded Optimization

Unity Helpers includes a `SINGLE_THREADED` scripting define symbol for WebGL and other single-threaded environments. When enabled, the library automatically uses optimized code paths that eliminate threading overhead:

**Optimized systems with SINGLE_THREADED:**

- **Buffers & Pooling** - Uses `Stack<T>` and `Dictionary<T>` instead of `ConcurrentBag<T>` and `ConcurrentDictionary<T>`
- **Random Number Generation** - Static instances instead of `ThreadLocal<T>`
- **Reflection Caches** - Non-concurrent dictionaries for faster lookups
- **Thread Pools** - SingleThreadedThreadPool disabled (not needed on WebGL)

**How to enable:**

Unity automatically defines `UNITY_WEBGL` for WebGL builds. To enable SINGLE_THREADED optimization:

1. Go to **Project Settings > Player > Other Settings > Scripting Define Symbols**
2. Add `SINGLE_THREADED` for WebGL platform
3. Or use in your `csc.rsp` file: `-define:SINGLE_THREADED`

**Performance impact:** 10-20% faster hot path operations on single-threaded platforms by avoiding unnecessary synchronization overhead.

### IL2CPP and Code Stripping Considerations

⚠️ **Important for IL2CPP builds (WebGL, Mobile, Consoles):**

Some features in Unity Helpers use reflection internally (particularly **Protobuf serialization** and **ReflectionHelpers**). IL2CPP's managed code stripping may remove types/members that are only accessed via reflection, causing runtime errors.

**Symptoms of stripping issues:**

- `NullReferenceException` or `TypeLoadException` during deserialization
- Missing fields after Protobuf deserialization
- Reflection helpers failing to find types at runtime

**Solution: Use link.xml to preserve required types**

Create a `link.xml` file in your `Assets` folder to prevent stripping:

```xml
<linker>
  <!-- Preserve your serialized types -->
  <assembly fullname="Assembly-CSharp">
    <type fullname="MyNamespace.PlayerSave" preserve="all"/>
    <type fullname="MyNamespace.InventoryData" preserve="all"/>
    <!-- Add all Protobuf-serialized types here -->
  </assembly>

  <!-- Preserve Unity Helpers if needed -->
  <assembly fullname="WallstopStudios.UnityHelpers.Runtime" preserve="all"/>
</linker>
```

**Best practices:**

- ✅ **Always test IL2CPP builds** - Development builds don't use stripping, so bugs only appear in release builds
- ✅ **Test on target platform** - WebGL stripping behaves differently than iOS/Android
- ✅ **Use link.xml for all Protobuf types** - Any type with `[ProtoContract]` should be preserved
- ✅ **Verify after every schema change** - Adding new serialized types requires updating link.xml
- ✅ **Check logs for stripping warnings** - Unity logs which types are stripped during build

**When you don't need link.xml:**

- JSON serialization (uses source-generated converters, not reflection)
- Spatial trees and data structures (no reflection used)
- Most helper methods (compiled ahead-of-time)

**Related documentation:**

- [Unity Manual: Managed Code Stripping](https://docs.unity3d.com/Manual/ManagedCodeStripping.html)
- [Protobuf-net and IL2CPP](https://github.com/protobuf-net/protobuf-net#il2cpp)
- [Serialization Guide: IL2CPP Warning](SERIALIZATION.md#️-il2cpp-and-code-stripping-warning)
- [Reflection Helpers: IL2CPP Warning](REFLECTION_HELPERS.md#️-il2cpp-code-stripping-considerations)

---

## Quick Start Guide

### Random Number Generation

Replace Unity's Random with high-performance alternatives:

```csharp
using System;
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Random;
using WallstopStudios.UnityHelpers.Core.Extension; // extension APIs like NextVector2(), NextWeightedIndex()

// Use the recommended default (currently IllusionFlow)
IRandom random = PRNG.Instance;

// Basic random values
float chance = random.NextFloat();           // 0.0f to 1.0f
int damage = random.Next(10, 20);            // 10 to 19
bool critical = random.NextBool();           // true or false

// Advanced features
Vector2 position = random.NextVector2();     // Random 2D position (extension method)
Guid playerId = random.NextGuid();           // UUIDv4
float gaussian = random.NextGaussian();      // Normal distribution

// Random selection
string[] lootTable = { "Sword", "Shield", "Potion" };
string item = random.NextOf(lootTable);

// Weighted bool
float probability = 0.7f;
bool lucky = random.NextBool(probability);

// Noise generation
float[,] noiseMap = new float[256, 256];
random.NextNoiseMap(noiseMap, octaves: 4);
```

**Why use PRNG.Instance?**

- 10-15x faster than Unity.Random
- Seedable for deterministic gameplay
- Thread-safe access (uses a thread-local instance)
- Extensive API for common patterns

[📊 View Random Performance Benchmarks](RANDOM_PERFORMANCE.md)

---

### Auto Component Discovery

Stop writing GetComponent calls everywhere:

```csharp
using WallstopStudios.UnityHelpers.Core.Attributes;
using UnityEngine;

public class Player : MonoBehaviour
{
    // Automatically finds SpriteRenderer on same GameObject
    [SiblingComponent]
    private SpriteRenderer spriteRenderer;

    // Finds Rigidbody2D on same GameObject, but it's optional
    [SiblingComponent(Optional = true)]
    private Rigidbody2D rigidbody;

    // Finds Camera in parent hierarchy
    [ParentComponent]
    private Camera parentCamera;

    // Only search ancestors, not siblings
    [ParentComponent(OnlyAncestors = true)]
    private Transform[] parentTransforms;

    // Finds all PolygonCollider2D in children
    [ChildComponent]
    private List<PolygonCollider2D> childColliders;

    // Only search descendants
    [ChildComponent(OnlyDescendants = true)]
    private EdgeCollider2D edgeCollider;

    private void Awake()
    {
        // One call wires up everything!
        this.AssignRelationalComponents();

        // All fields are now assigned (or logged errors if missing)
        spriteRenderer.color = Color.red;
    }
}
```

**Benefits:**

- Cleaner, more declarative code
- Safer defaults (required by default; opt-in `Optional = true`)
- Filters by tag/name, limit results, control depth, support interfaces
- Works with single fields, arrays, `List<T>`, and `HashSet<T>`
- Descriptive error logging for missing required components
- Honors `IncludeInactive` (include disabled/inactive when true)

For a complete walkthrough with recipes, FAQs, and troubleshooting, see [Relational Components](RELATIONAL_COMPONENTS.md).

---

### Spatial Queries

Fast spatial lookups for AI, collision detection, and more:

```csharp
using WallstopStudios.UnityHelpers.Core.DataStructure;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    private QuadTree2D<Enemy> enemyTree;

    void Start()
    {
        // Build tree from all enemies
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        enemyTree = new QuadTree2D<Enemy>(enemies, e => e.transform.position);
    }

    // Find all enemies in radius (O(log n) instead of O(n))
    public List<Enemy> GetEnemiesInRange(Vector2 position, float radius)
    {
        List<Enemy> results = new();
        enemyTree.GetElementsInRange(position, radius, results);
        return results;
    }

    // Find enemies in a rectangular area
    public List<Enemy> GetEnemiesInArea(Bounds area)
    {
        List<Enemy> results = new();
        enemyTree.GetElementsInBounds(area, results);
        return results;
    }

    // Find nearest enemies fast
    public List<Enemy> GetNearestEnemies(Vector2 position, int count)
    {
        List<Enemy> results = new();
        enemyTree.GetApproximateNearestNeighbors(position, count, results);
        return results;
    }
}
```

**Important:** Spatial trees are **immutable** - rebuild them when positions change.

[📊 View 2D Performance Benchmarks](SPATIAL_TREE_2D_PERFORMANCE.md) | [📊 View 3D Performance Benchmarks](SPATIAL_TREE_3D_PERFORMANCE.md)

For zero‑alloc queries and stable GC, see the [Buffering Pattern](#buffering-pattern).

---

## Core Features

### Random Number Generators

Unity Helpers includes **12 high-quality random number generators**, all implementing a rich `IRandom` interface:

#### Available Generators

| Generator                       | Speed     | Quality   | Use Case                                 |
| ------------------------------- | --------- | --------- | ---------------------------------------- |
| **IllusionFlow** ⭐             | Fast      | Good      | Default choice (via PRNG.Instance)       |
| **PcgRandom**                   | Very Fast | Excellent | Deterministic gameplay; explicit seeding |
| **RomuDuo**                     | Fastest   | Good      | Maximum performance needed               |
| **LinearCongruentialGenerator** | Fastest   | Fair      | Simple, fast generation                  |
| **XorShiftRandom**              | Very Fast | Good      | General purpose                          |
| **XoroShiroRandom**             | Very Fast | Good      | General purpose                          |
| **SplitMix64**                  | Very Fast | Good      | Initialization, hashing                  |
| **SquirrelRandom**              | Moderate  | Good      | Hash-based generation                    |
| **WyRandom**                    | Moderate  | Good      | Hashing applications                     |
| **DotNetRandom**                | Moderate  | Good      | .NET compatibility                       |
| **SystemRandom**                | Slow      | Good      | Backward compatibility                   |
| **UnityRandom**                 | Very Slow | Good      | Unity compatibility                      |

⭐ **Recommended**: Use `PRNG.Instance` (currently IllusionFlow)

#### Rich API

All generators implement `IRandom` with extensive functionality:

```csharp
IRandom random = PRNG.Instance;

// Basic types
int i = random.Next();                  // int in [0, int.MaxValue]
int range = random.Next(10, 20);        // int in [10, 20)
uint ui = random.NextUint();            // uint in [0, uint.MaxValue]
float f = random.NextFloat();           // float in [0.0f, 1.0f]
double d = random.NextDouble();         // double in [0.0d, 1.0d]
bool b = random.NextBool();             // true or false

// Unity types
Vector2 v2 = random.NextVector2();      // Random 2D vector
Vector3 v3 = random.NextVector3();      // Random 3D vector
Color color = random.NextColor();       // Random color
Quaternion rot = random.NextRotation(); // Random rotation

// Distributions
float gaussian = random.NextGaussian(mean: 0f, stdDev: 1f);

// Collections
T item = random.NextOf(collection);     // Random element
T[] shuffled = random.Shuffle(array);   // Fisher-Yates shuffle
int weightedIndex = random.NextWeightedIndex(weights);

// Special
Guid uuid = random.NextGuid();          // UUIDv4
T enumValue = random.NextEnum<T>();     // Random enum value
float[,] noise = random.NextNoiseMap(width, height); // Perlin noise
```

#### Deterministic Gameplay

All generators are **seedable** for replay systems:

```csharp
// Create seeded generator for deterministic behavior
IRandom seededRandom = new IllusionFlow(seed: 12345);

// Same seed = same sequence
IRandom replay = new IllusionFlow(seed: 12345);
// Both will generate identical values
```

**Threading:**

- Do not share a single RNG instance across threads.
- Use `PRNG.Instance` for a thread-local default, or use each generator's `TypeName.Instance` (e.g., `IllusionFlow.Instance`, `PcgRandom.Instance`).
- Alternatively, create one separate instance per thread.

[📊 Performance Comparison](RANDOM_PERFORMANCE.md)

---

### Spatial Trees

Efficient spatial data structures for 2D and 3D games.

#### 2D Spatial Trees

- **QuadTree2D** - Best general-purpose choice
- **KDTree2D** - Fast nearest-neighbor queries
- **RTree2D** - Optimized for bounding boxes

```csharp
using WallstopStudios.UnityHelpers.Core.DataStructure;

// Create from collection
GameObject[] objects = FindObjectsOfType<GameObject>();
QuadTree2D<GameObject> tree = new(objects, go => go.transform.position);

// Query by radius
List<GameObject> nearby = new();
tree.GetElementsInRange(playerPos, radius: 10f, nearby);

// Query by bounds
Bounds searchArea = new(center, size);
tree.GetElementsInBounds(searchArea, nearby);

// Find nearest neighbors (approximate, but fast)
tree.GetApproximateNearestNeighbors(playerPos, count: 5, nearby);
```

#### 3D Spatial Trees

Note: KdTree3D, OctTree3D, and RTree3D are under active development. SpatialHash3D is stable and production‑ready.

- **OctTree3D** - Best general-purpose choice for 3D
- **KDTree3D** - Fast 3D nearest-neighbor queries
- **RTree3D** - Optimized for 3D bounding volumes
- **SpatialHash3D** - Efficient for uniformly distributed moving objects (stable)

```csharp
// Same API as 2D, but with Vector3
Vector3[] positions = GetAllPositions();
OctTree3D<Vector3> tree = new(positions, p => p);

List<Vector3> results = new();
tree.GetElementsInRange(center, radius: 50f, results);
```

#### When to Use Spatial Trees

✅ **Good for:**

- Many objects (100+)
- Frequent spatial queries
- Static or slowly changing data
- AI awareness systems
- Visibility culling
- Collision detection optimization

❌ **Not ideal for:**

- Few objects (<50)
- Constantly moving objects
- Single queries
- Already using Unity's physics system

[📊 2D Benchmarks](SPATIAL_TREE_2D_PERFORMANCE.md) | [📊 3D Benchmarks](SPATIAL_TREE_3D_PERFORMANCE.md)

For behavior details and edge cases, see: [Spatial Tree Semantics](SPATIAL_TREE_SEMANTICS.md)

---

### Relational Components

Stop writing GetComponent boilerplate. Auto-wire components using attributes.

**Key attributes:**

- `[SiblingComponent]` - Find components on same GameObject
- `[ParentComponent]` - Find components in parent hierarchy
- `[ChildComponent]` - Find components in children
- `[ValidateAssignment]` - Validate at edit time, show errors in inspector
- `[NotNull]` - Must be assigned in inspector
- `[DxReadOnly]` - Read-only display in inspector
- `[WShowIf]` - Conditional display based on field values

**Quick example:**

```csharp
using WallstopStudios.UnityHelpers.Core.Attributes;

public class Enemy : MonoBehaviour
{
    // Find on same GameObject
    [SiblingComponent]
    private Animator animator;

    // Find in parent
    [ParentComponent]
    private EnemySpawner spawner;

    // Find in children
    [ChildComponent]
    private List<Weapon> weapons;

    // Optional component (no error if missing)
    [SiblingComponent(Optional = true)]
    private AudioSource audioSource;

    // Only search direct children/parents
    [ParentComponent(OnlyAncestors = true)]
    private Transform[] parentHierarchy;

    // Include inactive components
    [ChildComponent(IncludeInactive = true)]
    private ParticleSystem[] effects;

    private void Awake()
    {
        this.AssignRelationalComponents();
    }
}
```

See the in-depth guide: [Relational Components](RELATIONAL_COMPONENTS.md).

---

### Effects, Attributes, and Tags

Create data-driven gameplay effects that modify stats, apply tags, and drive cosmetics.

**Key pieces:**

- `AttributeEffect` — ScriptableObject that bundles stat changes, tags, cosmetics, and duration.
- `EffectHandle` — Unique ID for one application instance; remove/refresh specific stacks.
- `AttributesComponent` — Base class for components that expose modifiable `Attribute` fields.
- `TagHandler` — Counts and queries string tags for gating gameplay (e.g., "Stunned").
- `CosmeticEffectData` — Prefab-like container of behaviors shown while an effect is active.

**Quick example:**

```csharp
using WallstopStudios.UnityHelpers.Tags;

// 1) Define stats on a component
public class CharacterStats : AttributesComponent
{
    public Attribute Health = 100f;
    public Attribute Speed = 5f;
}

// 2) Author an AttributeEffect (ScriptableObject) in the editor
//    - modifications: [ { attribute: "Speed", action: Multiplication, value: 1.5f } ]
//    - durationType: Duration, duration: 5
//    - effectTags: [ "Haste" ]
//    - cosmeticEffects: [ a prefab with CosmeticEffectData + Particle/Audio components ]

// 3) Apply and later remove
GameObject player = ...;
AttributeEffect haste = ...; // ScriptableObject reference
EffectHandle? handle = player.ApplyEffect(haste);
if (handle.HasValue)
{
    // Remove early if needed
    player.RemoveEffect(handle.Value);
}

// Query tags anywhere
if (player.HasTag("Stunned")) { /* disable input */ }
```

**Details at a glance:**

- `ModifierDurationType.Instant` — applies permanently; returns null handle.
- `ModifierDurationType.Duration` — temporary; expires automatically; reapply can reset if enabled.
- `ModifierDurationType.Infinite` — persists until `RemoveEffect(handle)` is called.
- `AttributeModification` order: Addition → Multiplication → Override.
- `CosmeticEffectData.RequiresInstancing` — instance per application or reuse shared presenters.

Further reading: see the full guide [Effects System](EFFECTS_SYSTEM.md).

---

### Serialization

Fast, compact serialization for save systems, config, and networking.

This package provides three serialization technologies:

- `Json` — Uses System.Text.Json with built‑in converters for Unity types.
- `Protobuf` — Uses protobuf-net for compact, fast, schema‑evolvable binary.
- `SystemBinary` — Uses .NET BinaryFormatter for legacy/ephemeral data only.

All are exposed via `WallstopStudios.UnityHelpers.Core.Serialization.Serializer`.

#### JSON Profiles

- **Normal** — robust defaults (case-insensitive, includes fields, comments/trailing commas allowed)
- **Pretty** — human-friendly, indented
- **Fast** — strict, minimal with Unity converters (case-sensitive, strict numbers, no comments/trailing commas, IncludeFields=false)
- **FastPOCO** — strict, minimal, no Unity converters; best for pure POCO graphs

#### When To Use What

- Use **Json** for:
  - Player or tool settings, human‑readable saves, serverless workflows.
  - Interop with tooling, debugging, or versioning in Git.
- Use **Protobuf** for:
  - Network payloads, large save files, bandwidth/storage‑sensitive data.
  - Situations where you expect schema evolution across versions.
- Use **SystemBinary** only for:
  - Transient caches in trusted environments where data and code version match.
  - Never for untrusted data or long‑term persistence.

#### JSON Example

```csharp
using System.Collections.Generic;
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Serialization;

public class SaveData
{
    public Vector3 position;
    public Color playerColor;
    public List<GameObject> inventory;
}

var data = new SaveData
{
    position = new Vector3(1, 2, 3),
    playerColor = Color.cyan,
    inventory = new List<GameObject>()
};

// Serialize to UTF‑8 JSON bytes (Unity types supported via built‑in converters)
byte[] jsonBytes = Serializer.JsonSerialize(data);

// Deserialize from string
string jsonText = Serializer.JsonStringify(data, pretty: true);
SaveData fromText = Serializer.JsonDeserialize<SaveData>(jsonText);

// File helpers
Serializer.WriteToJsonFile(data, path: "save.json", pretty: true);
SaveData fromFile = Serializer.ReadFromJsonFile<SaveData>("save.json");

// Generic entry points (choose format at runtime)
byte[] bytes = Serializer.Serialize(data, SerializationType.Json);
SaveData loaded = Serializer.Deserialize<SaveData>(bytes, SerializationType.Json);
```

#### Protobuf Example

```csharp
using ProtoBuf; // protobuf-net
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Serialization;

[ProtoContract]
public class NetworkMessage
{
    [ProtoMember(1)] public int playerId;
    [ProtoMember(2)] public Vector3 position; // Vector3 works in Protobuf via built-in surrogates
}

var message = new NetworkMessage { playerId = 7, position = new Vector3(5, 0, -2) };

// Protobuf bytes (small + fast)
byte[] bytes = Serializer.ProtoSerialize(message);
NetworkMessage decoded = Serializer.ProtoDeserialize<NetworkMessage>(bytes);

// Generic entry points
byte[] bytes2 = Serializer.Serialize(message, SerializationType.Protobuf);
NetworkMessage decoded2 = Serializer.Deserialize<NetworkMessage>(bytes2, SerializationType.Protobuf);

// Buffer reuse (reduce GC for hot paths)
byte[] buffer = null;
int len = Serializer.Serialize(message, SerializationType.Protobuf, ref buffer);
NetworkMessage again = Serializer.Deserialize<NetworkMessage>(buffer.AsSpan(0, len).ToArray(), SerializationType.Protobuf);
```

**Notes:**

- Protobuf‑net requires stable field numbers. Annotate with `[ProtoMember(n)]` and never reuse or renumber.
- Unity types supported via surrogates: Vector2/3, Vector2Int/3Int, Quaternion, Color/Color32, Rect/RectInt, Bounds/BoundsInt, Resolution.

**Features:**

- Custom converters for Unity types (Vector2/3/4, Color, GameObject, Matrix4x4, Type)
- Protobuf (protobuf‑net) support for compact binary
- LZMA compression utilities (see `Runtime/Utils/LZMA.cs`)
- Type‑safe serialization and pooled buffers/writers to reduce GC

[Full guide: Serialization](SERIALIZATION.md)

---

### Data Structures

Additional high-performance data structures:

| Structure            | Use Case                       |
| -------------------- | ------------------------------ |
| **CyclicBuffer<T>**  | Ring buffer, sliding windows   |
| **BitSet**           | Compact boolean storage        |
| **ImmutableBitSet**  | Read-only bit flags            |
| **Heap<T>**          | Priority queue operations      |
| **PriorityQueue<T>** | Event scheduling               |
| **Deque<T>**         | Double-ended queue             |
| **DisjointSet**      | Union-find operations          |
| **Trie**             | String prefix trees            |
| **SparseSet**        | Fast add/remove with iteration |
| **TimedCache<T>**    | Auto-expiring cache            |

```csharp
// Cyclic buffer for damage history
CyclicBuffer<float> damageHistory = new(capacity: 10);
damageHistory.Add(25f);
damageHistory.Add(30f);
float avgDamage = damageHistory.Average();

// Priority queue for event scheduling
PriorityQueue<GameEvent> eventQueue = new();
eventQueue.Enqueue(spawnEvent, priority: 1);
eventQueue.Enqueue(bossEvent, priority: 10);
GameEvent next = eventQueue.Dequeue(); // Highest priority

// Trie for autocomplete
Trie commandTrie = new();
commandTrie.Insert("teleport");
commandTrie.Insert("tell");
commandTrie.Insert("terrain");
List<string> matches = commandTrie.GetWordsWithPrefix("tel");
// Returns: ["teleport", "tell"]
```

[Full guide: Data Structures](DATA_STRUCTURES.md)

---

### Core Math & Extensions

Numeric helpers, geometry primitives, Unity extensions, colors, collections, strings, directions.

See the guide: [Core Math & Extensions](MATH_AND_EXTENSIONS.md).

#### At a Glance

- `PositiveMod`, `WrappedAdd` — Safe cyclic arithmetic for indices/angles.
- `LineHelper.Simplify` — Reduce polyline vertices with Douglas–Peucker.
- `Line2D.Intersects` — Robust 2D segment intersection and closest-point helpers.
- `RectTransform.GetWorldRect` — Axis-aligned world bounds for rotated UI.
- `Camera.OrthographicBounds` — Compute visible world bounds for ortho cameras.
- `Color.GetAverageColor` — LAB/HSV/Weighted/Dominant color averaging.
- `IEnumerable.Infinite` — Cycle sequences without extra allocations.
- `StringExtensions.LevenshteinDistance` — Edit distance for fuzzy matching.

---

<a id="singleton-utilities-odin-compatible"></a>

### Singleton Utilities (ODIN‑compatible)

- `RuntimeSingleton<T>` — Global component singleton with optional cross‑scene persistence.
- `ScriptableObjectSingleton<T>` — Global settings/data singleton loaded from `Resources/`, auto‑created by the editor tool.

See the guide: [Singleton Utilities](SINGLETONS.md) and the tool: [ScriptableObject Singleton Creator](EDITOR_TOOLS_GUIDE.md#scriptableobject-singleton-creator).

---

### Editor Tools

Unity Helpers includes 20+ editor tools to streamline your workflow:

- **Sprite Tools**: Cropper, Atlas Generator, Animation Editor, Pivot Adjuster
- **Texture Tools**: Blur, Resize, Settings Applier, Fit Texture Size
- **Animation Tools**: Event Editor, Creator, Copier, Sheet Animation Creator
- **Validation**: Prefab Checker with comprehensive validation rules
- **Automation**: ScriptableObject Singleton Creator, Attribute Cache Generator

[📖 Complete Editor Tools Documentation](EDITOR_TOOLS_GUIDE.md)

**Quick Access:**

- Menu: `Tools > Wallstop Studios > Unity Helpers`
- Create Assets: `Assets > Create > Wallstop Studios > Unity Helpers`

---

## Buffering Pattern

**Professional-Grade Object Pooling**

Zero-allocation queries with automatic cleanup and thread-safe pooling.

```csharp
using WallstopStudios.UnityHelpers.Utils;
using WallstopStudios.UnityHelpers.Core.DataStructure;

// Example: Use pooled buffer for spatial query
void FindNearbyEnemies(QuadTree2D<Enemy> tree, Vector2 position)
{
    // Get pooled list - automatically returned when scope exits
    using var lease = Buffers<Enemy>.List.Get(out List<Enemy> buffer);

    // Use it with spatial query - combines zero-alloc query + pooled buffer!
    tree.GetElementsInRange(position, 10f, buffer);

    foreach (Enemy enemy in buffer)
    {
        enemy.TakeDamage(5f);
    }
    // buffer automatically returned to pool here
}

// Array pooling example
void ProcessLargeDataset(int size)
{
    using var lease = WallstopArrayPool<float>.Get(size, out float[] buffer);

    // Use buffer for temporary processing
    for (int i = 0; i < size; i++)
    {
        buffer[i] = ComputeValue(i);
    }

    // buffer automatically returned to pool here
}
```

**Do / Don'ts:**

- Do reuse buffers per system or component.
- Do treat buffers as temporary scratch space (APIs clear them first).
- Don't keep references to pooled lists beyond their lease lifetime.
- Don't share the same buffer across overlapping async/coroutine work.

<a id="pooling-utilities"></a>

**Pooling utilities:**

- `Buffers<T>` — pooled collections (List/Stack/Queue/HashSet) with `PooledResource` leases.

  - Lists: `using var lease = Buffers<Foo>.List.Get(out List<Foo> list);`
  - Stacks: `using var lease = Buffers<Foo>.Stack.Get(out Stack<Foo> stack);`
  - HashSets: `using var lease = Buffers<Foo>.HashSet.Get(out HashSet<Foo> set);`
  - Pattern: acquire → use → Dispose (returns to pool, clears collection).

- `WallstopArrayPool<T>` — rent arrays by length with automatic return on dispose.

  - Example: `using var lease = WallstopArrayPool<int>.Get(1024, out int[] buffer);`
  - Use for temporary processing buffers, sorting, or interop with APIs that require arrays.

- `WallstopFastArrayPool<T>` — fast array pool specialized for frequent short‑lived arrays.
  - Example: `using var lease = WallstopFastArrayPool<string>.Get(count, out string[] buffer);`
  - Used throughout Helpers for high‑frequency editor/runtime operations (e.g., asset searches).

**How pooling + buffering help APIs:**

- Spatial queries: pass a reusable `List<T>` to `GetElementsInRange/GetElementsInBounds` and iterate results without allocations.
- Component queries: `GetComponents(buffer)` clears and fills your buffer instead of allocating arrays.
- Editor utilities: temporary arrays/lists from pools keep import/scan tools snappy, especially inside loops.

---

## Dependency Injection Integrations

**Auto-detected packages:**

- Zenject/Extenject: `com.extenject.zenject`, `com.modesttree.zenject`, `com.svermeulen.extenject`
- VContainer: `jp.cysharp.vcontainer`, `jp.hadashikick.vcontainer`

**Manual or source imports (no UPM):**

- Add scripting defines in `Project Settings > Player > Other Settings > Scripting Define Symbols`:
  - `ZENJECT_PRESENT` when Zenject/Extenject is present
  - `VCONTAINER_PRESENT` when VContainer is present
- Add the define per target platform (e.g., Standalone, Android, iOS).

**Notes:**

- When the define is present, optional assemblies under `Runtime/Integrations/*` compile automatically and expose helpers like `RelationalComponentsInstaller` (Zenject) and `RegisterRelationalComponents()` (VContainer).
- If you use UPM, no manual defines are required — the package IDs above trigger symbols via `versionDefines` in the asmdefs.
- For test scenarios without LifetimeScope (VContainer) or SceneContext (Zenject), see [DI Integrations: Testing and Edge Cases](RELATIONAL_COMPONENTS.md#di-integrations-testing-and-edge-cases) for step‑by‑step patterns.

**Quick start:**

- **VContainer**: in your `LifetimeScope.Configure`, call `builder.RegisterRelationalComponents()`.
- **Zenject**: add `RelationalComponentsInstaller` to your `SceneContext` (toggle scene scan if desired).

```csharp
// VContainer — LifetimeScope
using VContainer;
using VContainer.Unity;
using WallstopStudios.UnityHelpers.Integrations.VContainer;

protected override void Configure(IContainerBuilder builder)
{
    builder.RegisterRelationalComponents();
}

// Zenject — prefab instantiation with DI + relations
using Zenject;
using WallstopStudios.UnityHelpers.Integrations.Zenject;

var enemy = Container.InstantiateComponentWithRelations(enemyPrefab, parent);
```

See the full guide with scenarios, troubleshooting, and testing patterns: [Relational Components Guide](RELATIONAL_COMPONENTS.md)

---

## Performance

Unity Helpers is built with performance as a top priority:

**Random Number Generation:**

- 10-15x faster than Unity.Random (655-885M ops/sec vs 65-85M ops/sec)
- Zero GC pressure with thread-local instances
- [📊 Full Random Performance Benchmarks](RANDOM_PERFORMANCE.md)

**Spatial Queries:**

- O(log n) tree queries vs O(n) linear search
- 100-1000x faster for large datasets
- QuadTree2D: 10,000 objects = ~13 checks vs 10,000 checks
- [📊 2D Performance Benchmarks](SPATIAL_TREE_2D_PERFORMANCE.md)
- [📊 3D Performance Benchmarks](SPATIAL_TREE_3D_PERFORMANCE.md)

**Memory Management:**

- Zero-allocation buffering pattern eliminates GC spikes
- Professional-grade pooling for List, HashSet, Stack, Queue, Arrays
- 5-10 FPS improvement in complex scenes from stable GC

**Reflection:**

- IL-emitted delegates 10-100x faster than System.Reflection
- Safe for IL2CPP and AOT platforms
- [📊 Reflection Performance](REFLECTION_HELPERS.md)

---

## Documentation Index

**Start Here:**

- 🚀 Getting Started — [Getting Started Guide](GETTING_STARTED.md)
- 🔍 Feature Index — [Complete A-Z Index](INDEX.md)
- 📖 Glossary — [Term Definitions](GLOSSARY.md)

**Core Guides:**

- Serialization Guide — [Serialization](SERIALIZATION.md)
- Editor Tools Guide — [Editor Tools](EDITOR_TOOLS_GUIDE.md)
- Math & Extensions — [Core Math & Extensions](MATH_AND_EXTENSIONS.md)
- Singletons — [Singleton Utilities](SINGLETONS.md)
- Relational Components — [Relational Components](RELATIONAL_COMPONENTS.md)
- Effects System — [Effects System](EFFECTS_SYSTEM.md)
- Data Structures — [Data Structures](DATA_STRUCTURES.md)

**Spatial Trees:**

- 2D Spatial Trees Guide — [2D Spatial Trees Guide](SPATIAL_TREES_2D_GUIDE.md)
- 3D Spatial Trees Guide — [3D Spatial Trees Guide](SPATIAL_TREES_3D_GUIDE.md)
- Spatial Tree Semantics — [Spatial Tree Semantics](SPATIAL_TREE_SEMANTICS.md)
- Spatial Tree 2D Performance — [Spatial Tree 2D Performance](SPATIAL_TREE_2D_PERFORMANCE.md)
- Spatial Tree 3D Performance — [Spatial Tree 3D Performance](SPATIAL_TREE_3D_PERFORMANCE.md)
- Hulls (Convex vs Concave) — [Hulls](HULLS.md)

**Performance & Reference:**

- Random Performance — [Random Performance](RANDOM_PERFORMANCE.md)
- Reflection Helpers — [Reflection Helpers](REFLECTION_HELPERS.md)

**Project Info:**

- Changelog — [Changelog](CHANGELOG.md)
- License — [License](LICENSE.md)
- Third‑Party Notices — [Third‑Party Notices](THIRD_PARTY_NOTICES.md)

---

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE.md) file for details.
