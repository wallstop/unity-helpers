# Unity Helpers

[![Npm Publish](https://github.com/wallstop/unity-helpers/actions/workflows/npm-publish.yml/badge.svg)](https://github.com/wallstop/unity-helpers/actions/workflows/npm-publish.yml)

A comprehensive collection of high-performance utilities, data structures, and editor tools for Unity game development. Unity Helpers provides everything from blazing-fast random number generators and spatial trees to powerful editor wizards and component relationship management.

---

**📚 New to Unity Helpers?** Start here: [Getting Started Guide](GETTING_STARTED.md)

**🔍 Looking for something specific?** Check the [Feature Index](INDEX.md)

**❓ Need a definition?** See the [Glossary](GLOSSARY.md)

---

## Quick Onramp

### Why Unity Helpers? The Killer Features

**⚡ Performance - Make Your Game Faster**
- **10-15x faster random** ([PRNG.Instance](RANDOM_PERFORMANCE.md)) vs UnityEngine.Random + seedable for determinism
- **Zero-allocation spatial queries** ([Buffering Pattern](#buffering-pattern)) → no GC spikes, stable 60fps
- **O(log n) spatial trees** ([Spatial Trees](SPATIAL_TREES_2D_GUIDE.md)) scale to millions of objects
- **IL-emitted reflection** ([ReflectionHelpers](#reflectionhelpers-blazing-fast-reflection)) → field/property access 10-100x faster than System.Reflection

**🚀 Productivity - Ship Features Faster**
- **Auto-wire components** ([Relational Components](RELATIONAL_COMPONENTS.md)) → eliminate GetComponent boilerplate
- **Data-driven effects** ([Effects System](EFFECTS_SYSTEM.md)) → designers create 100s of buffs/debuffs without programmer
- **20+ editor tools** ([Editor Tools](EDITOR_TOOLS_GUIDE.md)) → automate sprite cropping, animations, atlases
- **Predictive targeting** ([PredictCurrentTarget](#predictive-targeting-hit-moving-targets)) → perfect ballistics for turrets/missiles in 1 line
- **Professional pooling** ([Buffers](#professional-grade-object-pooling)) → zero-alloc patterns with automatic cleanup

**🛡️ Production-Ready - Never Break Player Saves**
- **Protobuf schema evolution** ([Serialization](SERIALIZATION.md#protobuf-schema-evolution-the-killer-feature)) → add/remove fields without breaking old saves
- **4,000+ test cases** → used in shipped commercial games
- **IL2CPP optimized** → works with Unity's aggressive compiler
- **SmartDestroy** ([Lifecycle Helpers](#lifecycle-helpers-no-more-destroyimmediate-bugs)) → editor/runtime safe destruction, never corrupt scenes again

---

TL;DR — Why use this?
- Ship faster with production‑ready utilities that are (much) faster than stock Unity options.
- Solve common problems: global settings/services, fast spatial queries, auto‑wiring components, robust serialization.
- 4,000+ tests and diagrams make behavior and trade‑offs clear.

Who is this for?
- Unity devs who want pragmatic, high‑quality building blocks without adopting a full framework.
- Teams that value performance, determinism, and predictable editor tooling.

Install in 60 seconds
```json
// Packages/manifest.json
{
  "dependencies": {
    "com.wallstop-studios.unity-helpers": "https://github.com/wallstop/unity-helpers.git"
  }
}
```

First 5 minutes: three quick wins
- Random: swap in a faster, seedable RNG
```csharp
using WallstopStudios.UnityHelpers.Core.Random;
IRandom rng = PRNG.Instance;
int damage = rng.Next(10, 20);
```

- Relational wiring: stop writing GetComponent
```csharp
using WallstopStudios.UnityHelpers.Core.Attributes;
public class Player : MonoBehaviour
{
  [SiblingComponent] SpriteRenderer sprite;
  void Awake() => this.AssignRelationalComponents();

> Need DI integration? Optional assemblies automatically light up when Zenject or VContainer is installed, exposing helpers like `RelationalComponentsInstaller` and `RegisterRelationalComponents()` so relational fields are assigned during container initialization.

}
```

- Spatial queries: O(log n) instead of O(n)
```csharp
using WallstopStudios.UnityHelpers.Core.DataStructure;
var tree = new QuadTree2D<Vector2>(points, p => p);
var results = new List<Vector2>();
tree.GetElementsInRange(playerPos, 10f, results);
```

Pick the right spatial structure (2D)
- Broad‑phase, many moving points: QuadTree2D
- Nearest neighbors on static points: KDTree2D (Balanced)
- Fast builds, good‑enough queries: KDTree2D (Unbalanced)
- Objects with size, bounds queries: RTree2D

Next steps
- Browse the Guides: Singletons, Relational Components, Spatial Trees 2D/3D, Serialization
- Skim the Performance pages for realistic expectations
- Use the Editor Tools to automate common art/content workflows

## Table of Contents

- [Quick Onramp](#quick-onramp)
- [Why Unity Helpers?](#why-unity-helpers)
- [Key Features](#key-features)
- [Installation](#installation)
- [Compatibility](#compatibility)
- [Quick Start Guide](#quick-start-guide)
  - [Random Number Generation](#random-number-generation)
  - [Auto Component Discovery](#auto-component-discovery)
  - [Spatial Queries](#spatial-queries)
  - [Effects in One Minute](#effects-in-one-minute)
  - [Serialization in One Minute](#serialization-in-one-minute)
- [Core Features](#core-features)
  - [Random Number Generators](#random-number-generators)
  - [Spatial Trees](#spatial-trees)
  - [Effects, Attributes, and Tags](#effects-attributes-and-tags)
  - [Component Attributes](#component-attributes)
  - [Relational Components Guide](#relational-components-guide)
  - [Serialization](#serialization)
  - [Serialization Guide (Full)](SERIALIZATION.md)
  - [Data Structures](#data-structures)
  - [Editor Tools](#editor-tools)
- [Use Cases & Examples](#use-cases--examples)
- [Performance](#performance)
- [Contributing](#contributing)
- [License](#license)
- [Relational Components Guide](#relational-components-guide)
- [API Index](#api-index)
 - [Buffering Pattern](#buffering-pattern)
 - [Docs Index](#docs-index)

## Why Unity Helpers?

Unity Helpers was built to solve common game development challenges with **performance-first** solutions:

- 🚀 **10-15x faster** random number generation compared to Unity's built-in Random
- 🌳 **O(log n)** spatial queries instead of O(n) linear searches
- 🔧 **20+ editor tools** to streamline your workflow
- 📦 **Zero dependencies** - just import and use
- ✅ **Production-tested** in shipped games
- 🧪 **5000+ test cases** cover most of the public API and run before each release to catch regressions and prevent bugs

## Key Features

### High-Performance Random Number Generators
- **12 different implementations** including PCG, XorShift, and more
- **Thread-safe access via `PRNG.Instance`** and **seedable** for deterministic gameplay
- **Rich API** with Gaussian distributions, noise maps, UUIDs, and more
- Up to **15x faster** than Unity.Random ([See benchmarks](#performance))

### Spatial Trees for Fast Queries
- **2D & 3D spatial trees** (QuadTree, OctTree, KDTree, RTree)
- Perfect for collision detection, AI, visibility culling
- **Massive performance gains** for games with many objects
- Immutable trees with O(log n) query performance
- Note on stability and semantics:
  - 2D: QuadTree2D and KdTree2D (balanced/unbalanced) return the same results for the same inputs/queries; they differ only in performance characteristics. RTree2D indexes rectangles (bounds), so results differ for sized objects.
  - 3D: KdTree3D (balanced/unbalanced) and OctTree3D can yield different results for the same inputs/queries due to boundary, tie‑breaking, and traversal semantics. RTree3D indexes 3D bounds and differs by design. See Spatial Tree Semantics for details.

### Powerful Component Attributes
- `[ParentComponent]`, `[ChildComponent]`, `[SiblingComponent]` - Auto-wire components
- `[ValidateAssignment]` - Catch missing references at edit time
- `[DxReadOnly]` - Display calculated values in inspector
- `[WShowIf]` - Conditional inspector fields
 
 See the in-depth guide: [Relational Components](RELATIONAL_COMPONENTS.md).

### 20+ Editor Tools
- **Sprite tools**: Cropper, Atlas Generator, Animation Editor, Animation Creator (one‑click bulk from naming patterns)
- **Texture tools**: Blur, Resize, Settings Applier
- **Validation**: Prefab Checker, Animation Event Editor
- **Automation**: ScriptableObject Singleton Creator
- [Full Editor Tools Documentation](EDITOR_TOOLS_GUIDE.md)

### Core Math & Extensions
- Numeric helpers, geometry primitives, Unity extensions, colors, collections, strings, directions.
 - See the guide: [Core Math & Extensions](MATH_AND_EXTENSIONS.md).

#### At a Glance
- `PositiveMod`, `WrappedAdd` — Safe cyclic arithmetic for indices/angles. See: [Numeric Helpers](MATH_AND_EXTENSIONS.md#numeric-helpers).
- `LineHelper.Simplify` — Reduce polyline vertices with Douglas–Peucker. See: [Geometry](MATH_AND_EXTENSIONS.md#geometry).
- `Line2D.Intersects` — Robust 2D segment intersection and closest-point helpers. See: [Geometry](MATH_AND_EXTENSIONS.md#geometry).
- `RectTransform.GetWorldRect` — Axis-aligned world bounds for rotated UI. See: [Unity Extensions](MATH_AND_EXTENSIONS.md#unity-extensions).
- `Camera.OrthographicBounds` — Compute visible world bounds for ortho cameras. See: [Unity Extensions](MATH_AND_EXTENSIONS.md#unity-extensions).
- `Color.GetAverageColor` — LAB/HSV/Weighted/Dominant color averaging. See: [Color Utilities](MATH_AND_EXTENSIONS.md#color-utilities).
- `IEnumerable.Infinite` — Cycle sequences without extra allocations. See: [Collections](MATH_AND_EXTENSIONS.md#collections).
- `StringExtensions.LevenshteinDistance` — Edit distance for fuzzy matching. See: [Strings](MATH_AND_EXTENSIONS.md#strings).

### Singleton Utilities (ODIN‑compatible)
- `RuntimeSingleton<T>` — Global component singleton with optional cross‑scene persistence. See the guide: [Singleton Utilities](SINGLETONS.md).
- `ScriptableObjectSingleton<T>` — Global settings/data singleton loaded from `Resources/`, auto‑created by the editor tool. See the guide: [Singleton Utilities](SINGLETONS.md) and the tool: [ScriptableObject Singleton Creator](EDITOR_TOOLS_GUIDE.md#scriptableobject-singleton-creator).

## Docs Index

**Start Here**
- 🚀 Getting Started — [Getting Started Guide](GETTING_STARTED.md)
- 🔍 Feature Index — [Complete A-Z Index](INDEX.md)
- 📖 Glossary — [Term Definitions](GLOSSARY.md)

**Core Guides**
- Serialization Guide — [Serialization](SERIALIZATION.md)
- Editor Tools Guide — [Editor Tools](EDITOR_TOOLS_GUIDE.md)
- Math & Extensions — [Core Math & Extensions](MATH_AND_EXTENSIONS.md)
- Singletons — [Singleton Utilities](SINGLETONS.md)
- Relational Components — [Relational Components](RELATIONAL_COMPONENTS.md)
- Effects System — [Effects System](EFFECTS_SYSTEM.md)
- Data Structures — [Data Structures](DATA_STRUCTURES.md)

**Spatial Trees**
- 2D Spatial Trees Guide — [2D Spatial Trees Guide](SPATIAL_TREES_2D_GUIDE.md)
- 3D Spatial Trees Guide — [3D Spatial Trees Guide](SPATIAL_TREES_3D_GUIDE.md)
- Spatial Tree Semantics — [Spatial Tree Semantics](SPATIAL_TREE_SEMANTICS.md)
- Spatial Tree 2D Performance — [Spatial Tree 2D Performance](SPATIAL_TREE_2D_PERFORMANCE.md)
- Spatial Tree 3D Performance — [Spatial Tree 3D Performance](SPATIAL_TREE_3D_PERFORMANCE.md)
- Hulls (Convex vs Concave) — [Hulls](HULLS.md)

**Performance & Reference**
- Random Performance — [Random Performance](RANDOM_PERFORMANCE.md)
- Reflection Helpers — [Reflection Helpers](REFLECTION_HELPERS.md)

**Project Info**
- Changelog — [Changelog](CHANGELOG.md)
- License — [License](LICENSE.md)
- Third‑Party Notices — [Third‑Party Notices](THIRD_PARTY_NOTICES.md)

## Installation

### As Unity Package (Recommended)

1. Open Unity Package Manager
2. *(Optional)* Enable **Pre-release packages** for cutting-edge builds
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
2. *(Optional)* Enable **Pre-release packages**
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

## Compatibility

| Unity Version | Built-In | URP | HDRP |
| --- | --- | --- | --- |
| 2021 | Likely, but untested | Likely, but untested | Likely, but untested |
| 2022 | ✅ Compatible | ✅ Compatible | ✅ Compatible |
| 2023 | ✅ Compatible | ✅ Compatible | ✅ Compatible |
| Unity 6 | ✅ Compatible | ✅ Compatible | ✅ Compatible |

## Serialization

- Formats: JSON (System.Text.Json), Protobuf (protobuf-net), SystemBinary (legacy)
- Unity-aware JSON converters (Vector2/3/4, Color, Matrix4x4, Type, GameObject)
- Pooled buffers to minimize GC; byte[] APIs for hot paths

JSON profiles

- Normal — robust defaults (case-insensitive, includes fields, comments/trailing commas allowed)
- Pretty — human-friendly, indented
- Fast — strict, minimal with Unity converters (case-sensitive, strict numbers, no comments/trailing commas, IncludeFields=false)
- FastPOCO — strict, minimal, no Unity converters; best for pure POCO graphs

Usage

```csharp
using WallstopStudios.UnityHelpers.Core.Serialization;

var normal   = Serializer.CreateNormalJsonOptions();
var pretty   = Serializer.CreatePrettyJsonOptions();
var fast     = Serializer.CreateFastJsonOptions();
var fastPOCO = Serializer.CreateFastPocoJsonOptions();

byte[] buf = null;
Serializer.JsonSerialize(model, fast, ref buf);                    // pooled, minimal allocs
Serializer.JsonSerialize(model, fast, sizeHint: 512*1024, ref buf); // preallocate for large outputs
var rt = Serializer.JsonDeserialize<MyType>(buf, null, fast);      // span-based; no string alloc
```

When to use what

- Save/configs: Normal or Pretty
- Hot loops/large arrays: Fast or FastPOCO (POCO-only graphs)
- Mixed graphs with Unity types: Fast

See the full guide for trade-offs, tips, and examples: [Serialization Guide](SERIALIZATION.md)
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

For a complete walkthrough with recipes, FAQs, and troubleshooting, see [Relational Components](RELATIONAL_COMPONENTS.md) (Troubleshooting: [Tips & Troubleshooting](RELATIONAL_COMPONENTS.md#troubleshooting)).

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

### Effects in One Minute

Author stackable buffs/debuffs as assets and apply/remove at runtime.

```csharp
using WallstopStudios.UnityHelpers.Core.Effects;

// ScriptableObject: AttributeEffect (create via menu)
// Contains: modifications (e.g., Speed x1.5), tags (e.g., "Haste"), duration

// Apply to a target GameObject
EffectHandle? handle = target.ApplyEffect(hasteEffect);

// Later: remove one stack or all
if (handle.HasValue) target.RemoveEffect(handle.Value);
target.RemoveAllEffectsWithTag("Haste");
```

Why use it
- Declarative authoring, automatic stacking/timing/tagging, clean removal.
- Cosmetic hooks for VFX/SFX via `CosmeticEffectData`.

### Serialization in One Minute

Serialize/deserialize with Unity‑aware JSON profiles; use pooled buffers for hot paths.

```csharp
using WallstopStudios.UnityHelpers.Core.Serialization;

var opts = Serializer.CreateFastJsonOptions(); // or Pretty/Normal

// Serialize into a pooled buffer
byte[] buf = null;
Serializer.JsonSerialize(model, opts, ref buf);

// Deserialize directly from bytes (no string alloc)
var model2 = Serializer.JsonDeserialize<MyType>(buf, null, opts);
```

Tips
- Pretty/Normal for configs; Fast for hot loops; FastPOCO for pure POCO graphs.
- Unity converters handle Vector/Color/Matrix/GameObject references.

### Choosing Spatial Structures

- QuadTree2D — Static or semi-static point data in 2D. Great for circular and rectangular queries, approximate kNN. Immutable (rebuild when positions change).
- KdTree2D/3D — Excellent nearest-neighbor performance for points. Balanced variant for uniform data; unbalanced for quicker builds. Immutable.
- RTree2D — For rectangular/sized objects (sprites, colliders). Great for bounds and radius intersection queries. Immutable.
- SpatialHash2D/3D — Many moving objects that are fairly uniformly distributed. Cheap updates; fast approximate neighborhood queries.

Rules of thumb:
- Frequent movement? Prefer SpatialHash. Static or batched rebuilds? Use QuadTree/KdTree/RTree.
- Query by area/rectangle? RTree2D excels. Nearest neighbors? KdTree. Broad-phase neighbor checks? SpatialHash.

## Core Features

### Random Number Generators

Unity Helpers includes **12 high-quality random number generators**, all implementing a rich `IRandom` interface:

#### Available Generators

| Generator | Speed | Quality | Use Case |
|-----------|-------|---------|----------|
| **IllusionFlow** ⭐ | Fast | Good | Default choice (via PRNG.Instance) |
| **PcgRandom** | Very Fast | Excellent | Deterministic gameplay; explicit seeding |
| **RomuDuo** | Fastest | Good | Maximum performance needed |
| **LinearCongruentialGenerator** | Fastest | Fair | Simple, fast generation |
| **XorShiftRandom** | Very Fast | Good | General purpose |
| **XoroShiroRandom** | Very Fast | Good | General purpose |
| **SplitMix64** | Very Fast | Good | Initialization, hashing |
| **SquirrelRandom** | Moderate | Good | Hash-based generation |
| **WyRandom** | Moderate | Good | Hashing applications |
| **DotNetRandom** | Moderate | Good | .NET compatibility |
| **SystemRandom** | Slow | Good | Backward compatibility |
| **UnityRandom** | Very Slow | Good | Unity compatibility |

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
float triangular = random.NextTriangular(min: 0f, max: 1f, mode: 0.5f);

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

Threading
- Do not share a single RNG instance across threads.
- Use `PRNG.Instance` for a thread-local default, or use each generator’s `TypeName.Instance` (e.g., `IllusionFlow.Instance`, `PcgRandom.Instance`).
- Alternatively, create one separate instance per thread.

[📊 Performance Comparison](RANDOM_PERFORMANCE.md)

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

### Effects, Attributes, and Tags

Create data-driven gameplay effects that modify stats, apply tags, and drive cosmetics.

Key pieces:

- `AttributeEffect` — ScriptableObject that bundles stat changes, tags, cosmetics, and duration.
- `EffectHandle` — Unique ID for one application instance; remove/refresh specific stacks.
- `AttributesComponent` — Base class for components that expose modifiable `Attribute` fields.
- `TagHandler` — Counts and queries string tags for gating gameplay (e.g., "Stunned").
- `CosmeticEffectData` — Prefab-like container of behaviors shown while an effect is active.

Why this helps:

- Decouples gameplay logic from presentation and from effect sources.
- Safe stacking and independent removal via handles and tag reference counts.
- Designer-friendly: author once in assets, reuse everywhere.

Quick start:

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

Details at a glance:

- `ModifierDurationType.Instant` — applies permanently; returns null handle.
- `ModifierDurationType.Duration` — temporary; expires automatically; reapply can reset if enabled.
- `ModifierDurationType.Infinite` — persists until `RemoveEffect(handle)` is called.
- `AttributeModification` order: Addition → Multiplication → Override.
- `CosmeticEffectData.RequiresInstancing` — instance per application or reuse shared presenters.

Tips:

- Use the Attribute Metadata Cache generator to power dropdowns and avoid typos in attribute names.
- Prefer `%`-style changes with Multiplication and small flat changes with Addition.
- Keep tag strings consistent; centralize in constants to avoid mistakes.

Further reading: see the full guide [Effects System](EFFECTS_SYSTEM.md).

### Component Attributes

Streamline component relationships and inspector validation.

#### Relational Component Attributes

```csharp
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

#### Validation Attributes

```csharp
public class PlayerController : MonoBehaviour
{
    // Validates at edit time, shows errors in inspector
    [ValidateAssignment]
    [SerializeField] private Rigidbody2D rigidbody;

    // Must be assigned in inspector
    [NotNull]
    [SerializeField] private PlayerData playerData;

    // Read-only display in inspector
    [DxReadOnly]
    public float currentHealth;

    // Conditional display based on enum
    public enum Mode { Simple, Advanced }
    public Mode currentMode;

    [WShowIf(nameof(currentMode), expectedValues = new object[] { Mode.Advanced })]
    public float advancedParameter;
}
```

### Serialization

 [Full guide: Serialization](SERIALIZATION.md)

Fast, compact serialization for save systems, config, and networking.

This package provides three serialization technologies:
- `Json` — Uses System.Text.Json with built‑in converters for Unity types.
- `Protobuf` — Uses protobuf-net for compact, fast, schema‑evolvable binary.
- `SystemBinary` — Uses .NET BinaryFormatter for legacy/ephemeral data only.

All are exposed via `WallstopStudios.UnityHelpers.Core.Serialization.Serializer`.

#### Formats Provided
- Json
  - Human‑readable; great for configs, save files you want to inspect or diff.
  - Includes converters for Unity types (Vector2/3/4, Color, Matrix4x4, GameObject, Type, enums as strings, cycles ignored, case‑insensitive, includes fields).
- Protobuf (protobuf‑net)
  - Small, fast, ideal for networking and large save payloads.
  - Forward/backward compatible when evolving messages (see tips below).
- SystemBinary (BinaryFormatter)
  - Only for legacy or trusted, same‑version, local data. Not recommended for long‑term persistence or untrusted input (security + versioning issues).

#### When To Use What
- Use Json for:
  - Player or tool settings, human‑readable saves, serverless workflows.
  - Interop with tooling, debugging, or versioning in Git.
- Use Protobuf for:
  - Network payloads, large save files, bandwidth/storage‑sensitive data.
  - Situations where you expect schema evolution across versions.
- Use SystemBinary only for:
  - Transient caches in trusted environments where data and code version match.
  - Never for untrusted data or long‑term persistence.

#### JSON Examples (Unity‑aware)
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

#### Protobuf Examples (Compact + Evolvable)
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

Notes:
- Protobuf‑net requires stable field numbers. Annotate with `[ProtoMember(n)]` and never reuse or renumber.
- Unity types supported via surrogates: Vector2/3, Vector2Int/3Int, Quaternion, Color/Color32, Rect/RectInt, Bounds/BoundsInt, Resolution.

#### Protobuf Compatibility Tips
- Add fields with new numbers; old clients ignore unknown fields, new clients default missing fields.
- Do not change field numbers or `oneof` layout; reserve removed numbers if needed.
- Avoid switching scalar types (e.g., `int32` → `string`) on the same number.
- Prefer optional/repeated over required; required breaks backward compatibility.
- Use sensible defaults to keep payloads minimal.

#### SystemBinary Examples (Legacy/Trusted Only)
```csharp
using WallstopStudios.UnityHelpers.Core.Serialization;

var obj = new SomeSerializableType();
byte[] bin = Serializer.BinarySerialize(obj);
SomeSerializableType roundtrip = Serializer.BinaryDeserialize<SomeSerializableType>(bin);

// Generic
byte[] bin2 = Serializer.Serialize(obj, SerializationType.SystemBinary);
var round2 = Serializer.Deserialize<SomeSerializableType>(bin2, SerializationType.SystemBinary);
```

Watch‑outs:
- BinaryFormatter is obsolete in modern .NET and not secure for untrusted input.
- Version changes often break binary round‑trips; use only for same‑version caches.

**Features:**
- Custom converters for Unity types (Vector2/3/4, Color, GameObject, Matrix4x4, Type)
- Protobuf (protobuf‑net) support for compact binary
- LZMA compression utilities (see `Runtime/Utils/LZMA.cs`)
- Type‑safe serialization and pooled buffers/writers to reduce GC

### Data Structures

Additional high-performance data structures:

| Structure | Use Case |
|-----------|----------|
| **CyclicBuffer<T>** | Ring buffer, sliding windows |
| **BitSet** | Compact boolean storage |
| **ImmutableBitSet** | Read-only bit flags |
| **Heap<T>** | Priority queue operations |
| **PriorityQueue<T>** | Event scheduling |
| **Deque<T>** | Double-ended queue |
| **DisjointSet** | Union-find operations |
| **Trie** | String prefix trees |
| **SparseSet** | Fast add/remove with iteration |
| **TimedCache<T>** | Auto-expiring cache |

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

### Helpers & Extensions

See also: [Buffering Pattern](#buffering-pattern)

High-level helpers and extension methods that streamline day-to-day Unity work.

Key picks:
- `Helpers.Find<T>(tag)` and `HasComponent<T>()` — Fewer `GetComponent` calls, cached lookups by tag.
- `GetOrAddComponent<T>()` — Idempotent component setup in initialization code.
- `DestroyAllChildren*` and `SmartDestroy()` — Safe destroy patterns across editor/runtime.
- `StartFunctionAsCoroutine(action, rate, useJitter)` — Simple polling/ticking utilities.
- `UpdateShapeToSprite()` — Sync `PolygonCollider2D` to `SpriteRenderer` at runtime.
- `GetAllLayerNames()` and `GetAllSpriteLabelNames()` — Editor integrations and tooling.
- `GetRandomPointInCircle/Sphere()` — Uniform random positions for spawn/FX.
- Unity Extensions: conversions (`Rect`⇄`Bounds`), camera `OrthographicBounds()`, physics `Rigidbody2D.Stop()`, input filtering `Vector2.IsNoise()`.

Examples:

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;
using WallstopStudios.UnityHelpers.Core.Extension;
using UnityEngine;

public class Setup : MonoBehaviour
{
    void Awake()
    {
        // Component orchestration
        var rb = gameObject.GetOrAddComponent<Rigidbody2D>();
        if (gameObject.HasComponent<SpriteRenderer>())
        {
            // Match collider to current sprite at runtime
            gameObject.UpdateShapeToSprite();
        }

        // Destroy patterns
        transform.parent.gameObject.DestroyAllChildrenGameObjects(); // runtime-safe

        // Lightweight polling
        this.StartFunctionAsCoroutine(() => Debug.Log("Tick"), 0.5f, useJitter: true);
    }
}

public class CameraUtils : MonoBehaviour
{
    void OnDrawGizmosSelected()
    {
        if (Camera.main)
        {
            // Compute world-space orthographic bounds for culling or UI logic
            Bounds view = Camera.main.OrthographicBounds();
            Gizmos.DrawWireCube(view.center, view.size);
        }
    }
}
```

When to use what:
- Prefer `SpatialHash2D` for many moving objects uniformly spread; prefer `QuadTree2D` for static or semi-static content with clustered queries.
- Use `Helpers.StartFunctionAsCoroutine` for simple, frame-safe polling; prefer `InvokeRepeating` or custom `Update` loops when you need fine-grained frame ordering.
- Use `SmartDestroy` when writing code that runs in both edit mode and play mode to avoid editor/runtime differences.

### Choosing Helpers

- Destroy patterns: Use `SmartDestroy` for editor/play safe teardown; `DestroyAllChildren*` to clear hierarchies quickly.
- Component wiring: Prefer relational attributes (`[SiblingComponent]`, etc.) + `AssignRelationalComponents()` over manual `GetComponent` calls.
  - To avoid first-use reflection overhead, prewarm caches at startup with `RelationalComponentInitializer.Initialize()` or enable “Prewarm Relational On Load” on the AttributeMetadataCache asset.
- Random placement: Use `Helpers.GetRandomPointInCircle/Sphere` or `RandomExtensions.NextVector2/3(InRange)` for uniform distributions.
- Asset/tooling: `GetAllLayerNames` and `GetAllSpriteLabelNames` power menu tooling and editor workflows.
- Math/geometry: `WallMath.PositiveMod/Wrapped*` for robust wrap-around; `LineHelper.Simplify*` to reduce polyline complexity; `Geometry.IsAPointLeftOfVectorOrOnTheLine` for sidedness tests.


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

<a id="use-cases--examples"></a>
<a id="use-cases-examples"></a>
## Use Cases & Examples

### Case Study: AI Behavior System

```csharp
public class AIController : MonoBehaviour
{
    [SiblingComponent] private NavMeshAgent agent;
    [SiblingComponent] private Animator animator;

    private IRandom random;
    private QuadTree2D<Enemy> enemyTree;

    void Start()
    {
        this.AssignRelationalComponents();

        // Deterministic AI with seeded random
        random = new PcgRandom(seed: GetInstanceID());

        // Build spatial tree for fast enemy queries
        enemyTree = new QuadTree2D<Enemy>(
            FindObjectsOfType<Enemy>(),
            e => e.transform.position
        );
    }

    void Update()
    {
        // Find nearby enemies efficiently
        List<Enemy> nearby = new();
        enemyTree.GetElementsInRange(transform.position, 20f, nearby);

        if (nearby.Count > 0)
        {
            // Pick random target with weighted selection
            float[] weights = nearby.Select(e => 1f / Vector2.Distance(
                transform.position, e.transform.position
            )).ToArray();

            int targetIndex = random.NextWeightedIndex(weights);
            Enemy target = nearby[targetIndex];

            agent.SetDestination(target.transform.position);
        }
    }
}
```

### Case Study: Procedural Level Generation

```csharp
public class LevelGenerator : MonoBehaviour
{
    private IRandom random;

    public void GenerateLevel(int seed)
    {
        random = new PcgRandom(seed); // Deterministic generation

        // Generate noise map for terrain
        float[,] heightMap = random.NextNoiseMap(
            width: 256,
            height: 256,
            octaves: 4,
            persistence: 0.5f,
            lacunarity: 2f
        );

        // Place objects based on height
        for (int x = 0; x < 256; x++)
        {
            for (int y = 0; y < 256; y++)
            {
                float height = heightMap[x, y];

                if (height > 0.7f) PlaceMountain(x, y);
                else if (height > 0.4f) PlaceTree(x, y);
                else if (height < 0.3f) PlaceWater(x, y);
            }
        }

        // Spawn enemies using Gaussian distribution for clustering
        int enemyCount = random.Next(10, 20);
        for (int i = 0; i < enemyCount; i++)
        {
            Vector2 clusterCenter = random.NextVector2() * 256f;

            // Cluster enemies around center point
            Vector2 offset = new Vector2(
                random.NextGaussian(mean: 0f, stdDev: 10f),
                random.NextGaussian(mean: 0f, stdDev: 10f)
            );

            SpawnEnemy(clusterCenter + offset);
        }
    }
}
```

### Case Study: Loot System

```csharp
public class LootTable
{
    private IRandom random = PRNG.Instance;

    [Serializable]
    public class LootEntry
    {
        public GameObject itemPrefab;
        public float weight;
        public int minCount;
        public int maxCount;
    }

    public List<LootEntry> lootEntries;

    public List<GameObject> RollLoot()
    {
        List<GameObject> rewards = new();

        // Roll each entry independently
        foreach (var entry in lootEntries)
        {
            // Weighted chance to get this item
            if (random.NextFloat() < entry.weight)
            {
                int count = random.Next(entry.minCount, entry.maxCount + 1);

                for (int i = 0; i < count; i++)
                {
                    rewards.Add(entry.itemPrefab);
                }
            }
        }

        // Shuffle for variety
        return random.Shuffle(rewards).ToList();
    }
}
```

### Case Study: Pooling with Spatial Awareness

```csharp
public class BulletManager : MonoBehaviour
{
    private List<Bullet> activeBullets = new();
    private QuadTree2D<Bullet> bulletTree;

    void LateUpdate()
    {
        // Rebuild tree each frame (bullets move constantly)
        if (activeBullets.Count > 0)
        {
            bulletTree = new QuadTree2D<Bullet>(
                activeBullets,
                b => b.transform.position
            );
        }
    }

    public List<Bullet> GetBulletsNear(Vector2 position, float radius)
    {
        List<Bullet> results = new();
        bulletTree?.GetElementsInRange(position, radius, results);
        return results;
    }

    // Efficiently check bullet collisions for a player
    public bool IsPlayerHit(Vector2 playerPos, float playerRadius)
    {
        List<Bullet> nearby = new();
        bulletTree?.GetElementsInRange(playerPos, playerRadius, nearby);

        foreach (var bullet in nearby)
        {
            if (Vector2.Distance(bullet.transform.position, playerPos) < playerRadius)
            {
                return true;
            }
        }

        return false;
    }
}
```

## Hidden Gems: Underrated Killer Features

These powerful utilities solve common game development problems but might not be immediately obvious from the feature list.

### Predictive Targeting: Hit Moving Targets

Perfect ballistics in one line. Calculates the intercept point for hitting a moving target with a projectile of known speed.

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

public class Turret : MonoBehaviour
{
    public float projectileSpeed = 25f;

    void Update()
    {
        GameObject target = FindTarget();
        if (target == null) return;

        // Estimate target velocity (or track it)
        Vector2 targetVelocity = EstimateVelocity(target);

        // Calculate perfect aim point accounting for projectile travel time
        Vector2 aimPoint = target.PredictCurrentTarget(
            launchLocation: transform.position,
            projectileSpeed: projectileSpeed,
            predictiveFiring: true,
            targetVelocity: targetVelocity
        );

        // Aim and fire
        transform.up = (aimPoint - (Vector2)transform.position).normalized;
        Fire();
    }
}
```

**Why this is a game-changer:**
- Solves quadratic intercept equation with robust fallbacks for edge cases
- Handles fast/slow projectiles, moving/stationary targets automatically
- Perfect for: turrets, homing missiles, AI prediction, physics-based games
- Eliminates need for iterative aiming or complex prediction logic

**Real-world use case:** Tower defense games where enemies move along paths - turrets lead the target perfectly without any tuning.

---

### ReflectionHelpers: Blazing-Fast Reflection

High-performance reflection using IL emission and expression compilation. 10-100x faster than System.Reflection for hot paths.

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;
using System.Reflection;

// ONE-TIME: Create cached delegates (do this at initialization)
FieldInfo healthField = typeof(Enemy).GetField("_health", BindingFlags.NonPublic | BindingFlags.Instance);
var getHealth = ReflectionHelpers.GetFieldGetter(healthField);      // Cached
var setHealth = ReflectionHelpers.GetFieldSetter(healthField);      // Cached

// HOT PATH: Use the delegates (10-100x faster than reflection)
void ProcessEnemies(List<object> enemies)
{
    foreach (object enemy in enemies)
    {
        float health = (float)getHealth(enemy);
        setHealth(enemy, health - 10f);
    }
}
```

**Advanced: Typed accessors for zero boxing**
```csharp
// For structs or when you need maximum performance
FieldInfo scoreField = typeof(Player).GetField("Score");
var getScore = ReflectionHelpers.GetFieldGetter<Player, int>(scoreField);
var setScore = ReflectionHelpers.GetFieldSetter<Player, int>(scoreField);

Player player = new Player();
setScore(ref player, 100);  // No boxing, direct struct mutation
int score = getScore(ref player);
```

**Why this is essential:**
- **Serialization systems**: Deserialize thousands of objects per frame
- **Data binding**: UI systems that update from model properties
- **Modding APIs**: Safe access to private fields without making everything public
- **ECS-style systems**: Generic component access without inheritance
- **IL2CPP safe**: Works with Unity's aggressive compilation

**Performance numbers:** GetField: ~2ns vs ~200ns (100x), SetField: ~3ns vs ~150ns (50x)

---

### Professional-Grade Object Pooling

Thread-safe pooling for Lists, Arrays, Dictionaries, and custom types with automatic cleanup via IDisposable pattern.

```csharp
using WallstopStudios.UnityHelpers.Utils;
using WallstopStudios.UnityHelpers.Core.DataStructure;

public class ParticleSystem : MonoBehaviour
{
    void Update()
    {
        // Get pooled list - automatically returned on scope exit
        using var lease = Buffers<Vector3>.List.Get(out List<Vector3> positions);

        // Use it freely
        CalculateParticlePositions(positions);

        // Do spatial query with pooled buffer
        using var enemiesLease = Buffers<Enemy>.List.Get(out List<Enemy> enemies);
        enemyTree.GetElementsInRange(transform.position, 10f, enemies);

        foreach (Enemy enemy in enemies)
        {
            enemy.TakeDamage(1f);
        }

        // Both lists automatically returned to pool here - zero cleanup code
    }
}
```

**Advanced: Pooled arrays for high-frequency operations**
```csharp
void ProcessFrame(int vertexCount)
{
    // Rent array from pool
    using var lease = WallstopArrayPool<Vector3>.Get(vertexCount, out Vector3[] vertices);

    // Use it for processing
    mesh.GetVertices(vertices);
    TransformVertices(vertices);
    mesh.SetVertices(vertices);

    // Array automatically returned and cleared
}
```

**Why this matters:**
- **Zero GC spikes**: Reuse allocations instead of creating garbage
- **Automatic cleanup**: IDisposable pattern ensures returns even on exceptions
- **Thread-safe**: ConcurrentStack backing for multi-threaded scenarios
- **Type-safe**: Generic pooling with full type safety
- **Customizable**: Create pools for your own types with custom lifecycle callbacks

**Perfect for:**
- AI systems querying neighbors every frame
- Particle systems with thousands of particles
- Physics raycasts returning hit arrays
- UI systems updating hundreds of elements
- Any system doing frequent spatial queries

---

### Lifecycle Helpers: No More DestroyImmediate Bugs

Safe object destruction that works correctly in both edit mode and play mode, preventing scene corruption.

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

public class DynamicUI : MonoBehaviour
{
    void RebuildUI()
    {
        // ❌ WRONG: Can corrupt scenes in edit mode
        // foreach (Transform child in transform)
        //     DestroyImmediate(child.gameObject);

        // ✅ RIGHT: Works safely in both modes
        transform.gameObject.DestroyAllChildrenGameObjects();
    }

    void CleanupEffect(GameObject effect)
    {
        // Automatically uses DestroyImmediate in editor, Destroy at runtime
        effect.SmartDestroy();

        // Or with delay (runtime only)
        effect.SmartDestroy(afterTime: 2f);
    }
}
```

**GetOrAddComponent: Idempotent Component Setup**
```csharp
public class PlayerSetup : MonoBehaviour
{
    void Initialize()
    {
        // Always safe - adds only if missing
        Rigidbody2D rb = gameObject.GetOrAddComponent<Rigidbody2D>();
        rb.gravityScale = 0;

        // Works with non-generic too
        Component collider = gameObject.GetOrAddComponent(typeof(CircleCollider2D));
    }
}
```

**Why these are essential:**
- `SmartDestroy`: Prevents "Destroying assets is not permitted" errors in editor
- `DestroyAllChildren*`: Cleans hierarchies without index shifting bugs
- `GetOrAddComponent`: Initialization code that's safe to run multiple times
- All methods handle null checks and edge cases
- Works correctly with prefab editing mode

**Common scenarios:**
- Editor tools that modify hierarchies
- Runtime UI builders
- Procedural content generation
- Testing/setup code that runs multiple times

---

### Convex & Concave Hull Generation

Production-ready implementations of hull generation algorithms for complex shapes from point clouds.

```csharp
using WallstopStudios.UnityHelpers.Core.Extension;
using System.Collections.Generic;

public class TerrainOutline : MonoBehaviour
{
    void GenerateOutline(List<Vector2> terrainPoints)
    {
        // Convex hull (fast, for simple outer bounds)
        List<Vector2> convexHull = terrainPoints.BuildConvexHull(
            algorithm: UnityExtensions.ConvexHullAlgorithm.MonotoneChain
        );

        // Concave hull (slower, but follows terrain shape closely)
        List<FastVector3Int> gridPositions = GetTerrainGridPositions();
        var options = new UnityExtensions.ConcaveHullOptions
        {
            Strategy = UnityExtensions.ConcaveHullStrategy.Knn,
            NearestNeighbors = 5  // Lower = tighter fit, higher = smoother
        };
        List<FastVector3Int> concaveHull = gridPositions.BuildConcaveHull(
            grid: GetComponent<Grid>(),
            options: options
        );

        // Use for collider generation, fog of war, territory borders, etc.
    }
}
```

**Why this is powerful:**
- **Convex hulls**: Perfect for collision bounds, fog of war outer limits, vision cones
- **Concave hulls**: Detailed territory borders, minimap fog, destructible terrain
- Multiple algorithms: MonotoneChain (fast), Jarvis (simple), Knn/EdgeSplit (concave)
- Grid-aware: Works with Unity Tilemap/Grid systems out of the box

**Real-world uses:**
- RTS territory visualization
- Fog of war boundaries
- Destructible terrain collision
- Minimap zone outlines
- Vision cone generation

---

### Coroutine Timing with Jitter

Production-ready timing utilities with staggered starts to prevent frame spikes.

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

public class HealthRegen : MonoBehaviour
{
    void Start()
    {
        // Poll every 0.5s with random initial delay (prevents all enemies syncing)
        this.StartFunctionAsCoroutine(
            action: RegenHealth,
            updateRate: 0.5f,
            useJitter: true  // Adds 0-0.5s random initial delay
        );
    }

    void RegenHealth()
    {
        health += regenPerTick;
    }
}
```

**Why jitter matters:**
- **Prevents frame spikes**: 100 enemies all polling at once = lag spike
- **Distributes load**: Staggers work across multiple frames
- **Simple API**: One parameter prevents performance issues

**Other timing helpers:**
```csharp
// Execute after delay
this.ExecuteFunctionAfterDelay(() => SpawnBoss(), delay: 3f);

// Execute next frame
this.ExecuteFunctionNextFrame(() => RefreshUI());

// Execute at end of frame (after rendering)
this.ExecuteFunctionAfterFrame(() => CaptureScreenshot());

// Execute N times over duration
StartCoroutine(Helpers.ExecuteOverTime(
    action: () => SpawnMinion(),
    totalCount: 10,
    duration: 5f,
    delay: true  // Space evenly over duration
));
```

---

### Cached Lookups: Find<T> with Tag Caching

Eliminates repeated GameObject.FindGameObjectWithTag calls with automatic caching.

```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

public class GameController : MonoBehaviour
{
    void Update()
    {
        // First call: searches scene and caches
        // Subsequent calls: instant lookup from cache
        AudioManager audio = Helpers.Find<AudioManager>("AudioManager");
        audio.PlaySound("Click");

        // From within a component (for logging context)
        PlayerData data = this.Find<PlayerData>("PlayerData");
    }
}
```

**Why this helps:**
- **Automatic caching**: First call populates cache, subsequent calls are O(1)
- **Fail-fast**: Logs warnings when tags are missing (can disable)
- **Memory efficient**: Only caches what you actually use
- **Invalidation safe**: Removes stale entries automatically

---

### UI Toolkit Progress Bars

Professional progress bar components with multiple visual styles, ready to use.

```csharp
// Available styles:
// - CircularProgressBar: Circular/radial progress
// - RegularProgressBar: Traditional horizontal/vertical
// - LiquidProgressBar: Liquid/wave effect
// - GlitchProgressBar: Glitch/cyberpunk effect
// - MarchingAntsProgressBar: Animated border
// - WigglyProgressBar: Wavy animation
// - ArcedProgressBar: Partial arc/gauge

// Use in UXML or code
var progressBar = new CircularProgressBar
{
    Progress = 0.75f,
    Radius = 50f,
    Thickness = 10f,
    Direction = CircularProgressBar.FillDirection.Clockwise,
    TrackColor = Color.gray,
    ProgressColor = Color.green
};
```

**Located in:** `Styles/Elements/Progress/`

---

## Performance

Unity Helpers is built with performance as a top priority. Here are some key metrics:

### Random Number Generation

Unity Helpers' random number generators are **10-15x faster** than Unity's built-in `UnityEngine.Random`:

- **UnityRandom**: 83M operations/sec
- **IllusionFlow** (PRNG.Instance): 609M operations/sec (**7x faster**)
- **RomuDuo** (fastest): 877M operations/sec (**10.5x faster**)

[📊 Full Random Performance Benchmarks](RANDOM_PERFORMANCE.md)

### Spatial Trees

Spatial queries are dramatically faster than linear searches:

| Objects | Linear Search | QuadTree2D | Speedup |
|---------|---------------|------------|---------|
| 1,000 | 1M ops/sec | 283M ops/sec | **283x** |
| 10,000 | 100K ops/sec | 233M ops/sec | **2,330x** |
| 100,000 | 10K ops/sec | 174M ops/sec | **17,400x** |
| 1,000,000 | 1K ops/sec | 141M ops/sec | **141,000x** |

*Measurements for small radius queries (1 unit)*

[📊 2D Spatial Tree Benchmarks](SPATIAL_TREE_2D_PERFORMANCE.md) | [📊 3D Spatial Tree Benchmarks](SPATIAL_TREE_3D_PERFORMANCE.md)

### Editor Performance

Editor tools use optimizations like:
- Parallel processing for image operations
- Cached reflection for attribute systems
- Batch asset database operations
- Progress bars for long operations

## Contributing

We welcome contributions! This project uses [CSharpier](https://csharpier.com/) for consistent code formatting.

### Before Contributing

1. Install [CSharpier](https://csharpier.com/) (via editor plugin or CLI)
2. Format changed files before committing
3. Consider installing the [pre-commit hook](https://pre-commit.com/#3-install-the-git-hook-scripts)

### How to Contribute

1. Fork the repository
2. Create a feature branch
3. Make your changes and format with CSharpier
4. Write/update tests if applicable
5. Submit a pull request

### Reporting Issues

Found a bug or have a feature request? [Open an issue](https://github.com/wallstop/unity-helpers/issues) on GitHub.

## License

[MIT License](LICENSE)

---

## Relational Components Guide

For a complete, user-friendly walkthrough of `[ParentComponent]`, `[ChildComponent]`, and `[SiblingComponent]` including examples, recipes, and FAQs, see:

- [Relational Components](RELATIONAL_COMPONENTS.md)

Troubleshooting common issues (runtime-only assignment, filters, depth, inactive objects):

- [Relational Components — Troubleshooting](RELATIONAL_COMPONENTS.md#troubleshooting)

---

## Additional Resources

- [Editor Tools Guide](EDITOR_TOOLS_GUIDE.md) - Complete documentation for all editor tools
- [Relational Components Guide](RELATIONAL_COMPONENTS.md) - Sibling/Parent/Child attributes with examples
- [Random Performance](RANDOM_PERFORMANCE.md) - Detailed RNG benchmarks
- [2D Spatial Trees](SPATIAL_TREE_2D_PERFORMANCE.md) - 2D spatial tree benchmarks
- [3D Spatial Trees](SPATIAL_TREE_3D_PERFORMANCE.md) - 3D spatial tree benchmarks
- [GitHub Repository](https://github.com/wallstop/unity-helpers)
- [Issue Tracker](https://github.com/wallstop/unity-helpers/issues)
- [NPM Package](https://www.npmjs.com/package/com.wallstop-studios.unity-helpers)

---

**Made with ❤️ by [Wallstop Studios](https://wallstopstudios.com)**

*Unity Helpers is production-ready and actively maintained. Star the repo if you find it useful!*

## API Index

- Namespaces
  - `WallstopStudios.UnityHelpers.Core.Helper`
    - `Helpers` — General utilities (layers, sprites, components, math, pooling)
    - `Objects` — Unity-aware null checks and deterministic hashing
    - `WallMath` — Positive modulo, wrapped add/increment, bounded floats/doubles
    - `LineHelper` — Douglas–Peucker polyline simplification
    - `Geometry` — Rect accumulation, sidedness tests
    - `PathHelper`/`FileHelper`/`DirectoryHelper` — File and path utilities (editor/runtime)
    - `SceneHelper` — Scene discovery and object retrieval (with disposal scope)
    - `SpriteHelpers` — Make textures readable (editor)
    - `UnityMainThreadDispatcher` — Enqueue work for main thread
    - [`ReflectionHelpers`](REFLECTION_HELPERS.md) — High-performance field/property/method/ctor access and type scanning
    - `FormattingHelpers` — Human-friendly sizes (e.g., bytes)
    - `IterationHelpers` — 2D/3D array index enumeration
    - `FuncBasedComparer`/`ReverseComparer` — Comparer utilities
    - `StringInList` — Inspector dropdown for strings
  - `WallstopStudios.UnityHelpers.Core.Extension`
    - `UnityExtensions` — Unity-centric extensions (Rect/Bounds, Camera, Rigidbody2D, vectors)
    - `RandomExtensions` — Random vectors/quaternions/colors and selections
    - `StringExtensions` — Case transforms, UTF-8, JSON, Levenshtein
    - `IEnumerableExtensions` — Collection conversions, infinite sequences, shuffle
    - `IListExtensions`/`IReadonlyListExtensions` — Shuffle/shift/sort/search utilities
    - `DictionaryExtensions` — GetOrAdd, GetOrElse helpers
    - `AnimatorExtensions` — ResetTriggers convenience
    - `AsyncOperationExtensions` — Await `AsyncOperation` (Task/ValueTask)
  - `WallstopStudios.UnityHelpers.Core.DataStructure`
    - Point/Bounds trees: `QuadTree2D<T>`, `KdTree2D<T>`, `KdTree3D<T>`, `RTree2D<T>`, `RTree3D<T>`
    - Spatial hashes: `SpatialHash2D<T>`, `SpatialHash3D<T>`
    - General: `Heap<T>`, `PriorityQueue<T>`, `Deque<T>`, `Trie`, `BitSet`, `CyclicBuffer<T>`, `SparseSet<T>`

### Quick Start: ReflectionHelpers

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using WallstopStudios.UnityHelpers.Core.Helper;

// 1) Fast field get/set (boxed)
public sealed class Player { public int Score; }
FieldInfo score = typeof(Player).GetField("Score");
var getScore = ReflectionHelpers.GetFieldGetter(score);   // object -> object
var setScore = ReflectionHelpers.GetFieldSetter(score);   // (object, object) -> void
var p = new Player();
setScore(p, 42);
UnityEngine.Debug.Log((int)getScore(p)); // 42

// 2) Struct note: prefer typed ref setter
public struct Stat { public int Value; }
FieldInfo valueField = typeof(Stat).GetField("Value");
var setValue = ReflectionHelpers.GetFieldSetter<Stat, int>(valueField);
Stat s = default;
setValue(ref s, 100); // s.Value == 100

// 3) Typed property getter
PropertyInfo prop = typeof(UnityEngine.Camera).GetProperty("orthographicSize");
var getSize = ReflectionHelpers.GetPropertyGetter<UnityEngine.Camera, float>(prop);
float size = getSize(UnityEngine.Camera.main);

// 4) Typed static method invoker (two params)
MethodInfo concat = typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) });
var concat2 = ReflectionHelpers.GetStaticMethodInvoker<string, string, string>(concat);
string joined = concat2("Hello ", "World");

// 5) Low-allocation constructors and collections
var newList = ReflectionHelpers.GetParameterlessConstructor<List<int>>();
List<int> list = newList();

var makeVec3Array = ReflectionHelpers.GetArrayCreator(typeof(UnityEngine.Vector3));
Array positions = makeVec3Array(256); // Vector3[256]

IList names = ReflectionHelpers.CreateList(typeof(string), 64); // List<string>

object intSet = ReflectionHelpers.CreateHashSet(typeof(int), 0); // HashSet<int>
var add = ReflectionHelpers.GetHashSetAdder(typeof(int));
add(intSet, 1);
add(intSet, 2);
```

Tip: Most collection-based APIs accept and fill buffers you provide (List<T>, arrays) to minimize allocations. Prefer passing a preallocated buffer for hot paths.

### Buffering Pattern

Many APIs accept a caller-provided buffer (e.g., `List<T>`) and clear it before writing results. Reuse these buffers to avoid per-frame allocations and reduce GC pressure.

Why it helps
- Prevents transient allocations in tight loops (AI queries, physics scans).
- Keeps GC stable in gameplay spikes (hundreds/thousands of queries).

Basics
- Create buffers once per system and reuse them.
- APIs that take a `List<T>` will clear it before use and return the same list for chaining.
- **Ergonomic benefit**: Because these APIs return the same list you pass in, you can use them directly in `foreach` loops for maximum convenience.
- Don't share a single buffer across concurrent operations; allocate one per caller or use pooling.

**Getting buffers easily:**
- Use `Buffers<T>.List.Get()` for pooled `List<T>` with automatic return via `Dispose`
- Use `WallstopArrayPool<T>.Get()` for pooled arrays with automatic return
- Use `WallstopFastArrayPool<T>.Get()` for frequently-used short-lived arrays
- See [Pooling utilities](#pooling-utilities) below for detailed examples

Examples

```csharp
// 2D tree query reuse
readonly List<Enemy> _enemiesBuffer = new(capacity: 256);

void Scan(QuadTree2D<Enemy> tree, Vector2 position, float radius)
{
    tree.GetElementsInRange(position, radius, _enemiesBuffer);
    for (int i = 0; i < _enemiesBuffer.Count; ++i)
    {
        Enemy e = _enemiesBuffer[i];
        // ... process
    }
}

// Ergonomic pattern: use the returned list directly in foreach
readonly List<Enemy> _nearbyBuffer = new(capacity: 128);

void ProcessNearbyEnemies(QuadTree2D<Enemy> tree, Vector2 position, float radius)
{
    // The API returns the same buffer, so you can chain it into foreach
    foreach (Enemy enemy in tree.GetElementsInRange(position, radius, _nearbyBuffer))
    {
        enemy.ApplyDamage(10f);
    }
}

// Spatial hash with distinct results, approximate distance
readonly List<Unit> _units = new(512);
hash.Query(center, 10f, _units, distinct: true, exactDistance: false);

// Components without allocations
readonly List<BoxCollider2D> _colliders = new(32);
gameObject.GetComponents(_colliders); // buffer is cleared by the API
```

Using the built‑in pool (advanced)

```csharp
using WallstopStudios.UnityHelpers.Utils;
using WallstopStudios.UnityHelpers.Core.DataStructure;

// Get a pooled List<T> and return it automatically via Dispose
using PooledResource<List<int>> lease = Buffers<int>.List.Get(out List<int> list);

// Use list here ...

// On dispose, list is cleared and returned to the pool
```

Pooling + Buffering combined

```csharp
using WallstopStudios.UnityHelpers.Utils;
using WallstopStudios.UnityHelpers.Core.DataStructure;

// Example: Use pooled buffer for spatial query
void FindNearbyEnemies(QuadTree2D<Enemy> tree, Vector2 position)
{
    // Get pooled list - automatically returned when scope exits
    using var lease = Buffers<Enemy>.List.Get(out List<Enemy> buffer);

    // Use it with spatial query - combines zero-alloc query + pooled buffer!
    foreach (Enemy enemy in tree.GetElementsInRange(position, 10f, buffer))
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

Do / Don’t
- Do reuse buffers per system or component.
- Do treat buffers as temporary scratch space (APIs clear them first).
- Don’t keep references to pooled lists beyond their lease lifetime.
- Don’t share the same buffer across overlapping async/coroutine work.

<a id="pooling-utilities"></a>
Pooling utilities

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

How pooling + buffering help APIs
- Spatial queries: pass a reusable `List<T>` to `GetElementsInRange/GetElementsInBounds` and iterate results without allocations.
- Component queries: `GetComponents(buffer)` clears and fills your buffer instead of allocating arrays.
- Editor utilities: temporary arrays/lists from pools keep import/scan tools snappy, especially inside loops.

## Dependency Injection Integrations

- Auto-detected packages
  - Zenject/Extenject: `com.extenject.zenject`, `com.modesttree.zenject`, `com.svermeulen.extenject`
  - VContainer: `jp.cysharp.vcontainer`, `jp.hadashikick.vcontainer`
- Manual or source imports (no UPM)
  - Add scripting defines in `Project Settings > Player > Other Settings > Scripting Define Symbols`:
    - `ZENJECT_PRESENT` when Zenject/Extenject is present
    - `VCONTAINER_PRESENT` when VContainer is present
  - Add the define per target platform (e.g., Standalone, Android, iOS).
- Notes
  - When the define is present, optional assemblies under `Runtime/Integrations/*` compile automatically and expose helpers like `RelationalComponentsInstaller` (Zenject) and `RegisterRelationalComponents()` (VContainer).
  - If you use UPM, no manual defines are required — the package IDs above trigger symbols via `versionDefines` in the asmdefs.
  - For test scenarios without LifetimeScope (VContainer) or SceneContext (Zenject), see “DI Integrations: Testing and Edge Cases” in RELATIONAL_COMPONENTS.md for step‑by‑step patterns.

- Quick start
  - VContainer: in your `LifetimeScope.Configure`, call `builder.RegisterRelationalComponents()`.
  - Zenject: add `RelationalComponentsInstaller` to your `SceneContext` (toggle scene scan if desired).

```csharp
// VContainer — LifetimeScope
using VContainer; using VContainer.Unity;
using WallstopStudios.UnityHelpers.Integrations.VContainer;
protected override void Configure(IContainerBuilder builder)
{
    builder.RegisterRelationalComponents();
}

// Zenject — prefab instantiation with DI + relations
using Zenject; using WallstopStudios.UnityHelpers.Integrations.Zenject;
var enemy = Container.InstantiateComponentWithRelations(enemyPrefab, parent);
```

See the full guide with scenarios, troubleshooting, and testing patterns: RELATIONAL_COMPONENTS.md
