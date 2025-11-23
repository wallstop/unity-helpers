# Unity Helpers

<p align="left">
  <a href="https://unity.com/releases/2021-lts"><img alt="Unity 2021.3+" src="https://img.shields.io/badge/Unity-2021.3%2B-000000?logo=unity&amp;logoColor=white" /></a><br>
  <a href="https://www.npmjs.com/package/com.wallstop-studios.unity-helpers"><img alt="npm version" src="https://img.shields.io/npm/v/com.wallstop-studios.unity-helpers" /></a><br>
  <a href="https://www.npmjs.com/package/com.wallstop-studios.unity-helpers"><img alt="npm downloads" src="https://img.shields.io/npm/dw/com.wallstop-studios.unity-helpers" /></a><br>
  <a href="https://github.com/wallstop/unity-helpers/releases"><img alt="GitHub release version" src="https://img.shields.io/github/v/release/wallstop/unity-helpers" /></a><br>
  <a href="https://github.com/wallstop/unity-helpers/releases"><img alt="GitHub downloads" src="https://img.shields.io/github/downloads/wallstop/unity-helpers/total" /></a><br>
  <a href="LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/License-MIT-blue.svg" /></a><br>
  <a href="https://github.com/wallstop/unity-helpers/actions/workflows/csharpier-autofix.yml"><img alt="CSharpier" src="https://github.com/wallstop/unity-helpers/actions/workflows/csharpier-autofix.yml/badge.svg?branch=main" /></a><br>
  <a href="https://github.com/wallstop/unity-helpers/actions/workflows/markdown-json.yml"><img alt="Markdown &amp; JSON Lint/Format" src="https://github.com/wallstop/unity-helpers/actions/workflows/markdown-json.yml/badge.svg?branch=main" /></a><br>
  <a href="https://github.com/wallstop/unity-helpers/actions/workflows/lint-doc-links.yml"><img alt="Lint Docs Links" src="https://github.com/wallstop/unity-helpers/actions/workflows/lint-doc-links.yml/badge.svg?branch=main" /></a><br>
  <a href="https://github.com/wallstop/unity-helpers/actions/workflows/npm-publish.yml"><img alt="Npm Publish" src="https://github.com/wallstop/unity-helpers/actions/workflows/npm-publish.yml/badge.svg?branch=main" /></a>
</p>

**Stop writing boilerplate. Start shipping features.**

Unity Helpers eliminates entire categories of repetitive work with production-ready utilities that are 10-100x faster than writing it yourself. From auto-wiring components to blazing-fast spatial queries, this is the toolkit that pays for itself in the first hour.

**What makes this different:**

- 🎨 **Professional inspector tooling** - Grouping, buttons, conditional display, toggle grids (rivals Odin Inspector, FREE)
- ⚡ **10-15x faster** random generation than Unity.Random
- 🔌 **Zero boilerplate** component wiring with attributes
- 🎮 **Designer-friendly** effects system (buffs/debuffs as ScriptableObjects)
- 🌳 **O(log n)** spatial queries instead of O(n) loops
- 🛠️ **20+ editor tools** that automate sprite/animation workflows
- ✅ **4,000+ tests** ensuring production quality

---

**🗺️ Roadmap Snapshot** — See the [Roadmap](docs/overview/roadmap.md) for prioritized details.

- Inspector tooling: inline nested editors, tabbed navigation, live instrumentation, disable-if/layer attributes
- Editor automation: Animation Creator and Sprite Sheet Animation Creator enhancements, timeline-ready Event Editor upgrades, and new automation dashboards
- Random/statistics: CI statistical harness, automated quality reports, scenario samplers, job-safe stream schedulers
- Spatial trees: graduate the 3D variants, add incremental updates, physics-shape parity, and streaming builders
- UI Toolkit: control pack (dockable panes, data grids), theming samples, and performance patterns
- Utilities: cross-system bridges plus new math/combinatorics and service-pattern helpers
- Performance: automated benchmarks, Burst/Jobs rewrites of hot paths, and allocation analyzers
- Attributes & tags: effect visualization tools, attribute graphs, and migration/versioning helpers
- Relational components: cached reflection, source generators, editor-time validation, and interface-based resolution

---

**📚 New to Unity Helpers?** Start here: [Getting Started Guide](docs/overview/getting-started.md)

**🔍 Looking for something specific?** Check the [Feature Index](docs/overview/index.md)

**❓ Need a definition?** See the [Glossary](docs/overview/glossary.md)

---

## 👋 First Time Here?

**Pick your starting point based on your biggest pain point:**

| Your Problem                         | Your Solution                                                                           | Time to Value |
| ------------------------------------ | --------------------------------------------------------------------------------------- | ------------- |
| 🎨 Writing custom editors            | [**Inspector Tooling**](#1--professional-inspector-tooling) - Odin-level features, FREE | 2 minutes     |
| 🐌 Writing `GetComponent` everywhere | [**Relational Components**](#2--auto-wire-components) - Auto-wire with attributes       | 2 minutes     |
| 🎮 Need buffs/debuffs system         | [**Effects System**](#3--data-driven-effects) - Designer-friendly ScriptableObjects     | 5 minutes     |
| 🔍 Slow spatial searches             | [**Spatial Trees**](#spatial-trees) - O(log n) queries                                  | 5 minutes     |
| 🎲 Random is too slow/limited        | [**PRNG.Instance**](#random-number-generators) - 10-15x faster, extensive API           | 1 minute      |
| 💾 Need save/load system             | [**Serialization**](#4--unity-aware-serialization) - Unity types just work              | 10 minutes    |
| 🛠️ Manual sprite workflows           | [**Editor Tools**](#6-️-editor-tools-suite) - 20+ automation tools                      | 3 minutes     |

**Not sure where to start?** → [Getting Started Guide](docs/overview/getting-started.md) walks through the top 3 features in 5 minutes.

---

## ⚡ Top Time-Savers

These features eliminate entire categories of repetitive work. Pick one that solves your immediate pain:

### 1. 🎨 Professional Inspector Tooling

#### ⏱️ 5-10 min/script × 200 scripts = 20+ hours saved on custom editors

Stop writing custom PropertyDrawers and EditorGUI code. Get Odin Inspector-level features for free:

```csharp
// ❌ OLD WAY: 100+ lines of custom editor code
[CustomEditor(typeof(CharacterStats))]
public class CharacterStatsEditor : Editor {
    // ... SerializedProperty declarations ...
    // ... OnEnable setup ...
    // ... OnInspectorGUI with EditorGUI.BeginFoldoutHeaderGroup ...
    // ... Custom button rendering ...
    // ... Conditional field display logic ...
}

// ✅ NEW WAY: Declarative attributes, zero custom editors
public class CharacterStats : MonoBehaviour
{
    [WGroup("combat", "Combat Stats", colorKey: "Default-Dark", collapsible: true)]
    public float maxHealth = 100f;
    public float defense = 10f;
    [WGroupEnd("combat")]

    [WFoldoutGroup("abilities", "Abilities", startCollapsed: true)]
    [System.Flags] public enum Powers { None = 0, Fly = 1, Strength = 2, Speed = 4 }
    [WEnumToggleButtons(showSelectAll: true, buttonsPerRow: 3)]
    public Powers currentPowers;
    [WFoldoutGroupEnd("abilities")]

    public enum WeaponType { Melee, Ranged, Magic }
    public WeaponType weaponType;

    [WShowIf(nameof(weaponType), WShowIfComparison.Equal, WeaponType.Ranged)]
    public int ammoCapacity = 30;

    [WButton("Heal to Full", groupName: "Debug")]
    private void HealToFull() { maxHealth = 100f; }
}
```

**Features:**

- **[WGroup](docs/features/inspector/inspector-grouping-attributes.md)** / **[WFoldoutGroup](docs/features/inspector/inspector-grouping-attributes.md)** - Boxed sections with auto-inclusion, color themes, animations
- **[WButton](docs/features/inspector/inspector-button.md)** - Method buttons with history, async support, cancellation
- **[WShowIf](docs/features/inspector/inspector-conditional-display.md)** - Conditional visibility (9 comparison operators)
- **[WEnumToggleButtons](docs/features/inspector/inspector-selection-attributes.md)** - Flag enums as visual toggle grids
- **[SerializableDictionary](docs/features/serialization/serialization-types.md)**, **[SerializableSet](docs/features/serialization/serialization-types.md)**, **[WGuid](docs/features/serialization/serialization-types.md)**, **[SerializableType](docs/features/serialization/serialization-types.md)** - Collections Unity can't serialize

[📖 Complete Inspector Guide](docs/features/inspector/inspector-overview.md)

---

### 2. 🔌 Auto-Wire Components

#### ⏱️ 10-20 min/script × 100 scripts = 20+ hours saved

Stop writing GetComponent boilerplate forever. Replace 20+ lines with 3 attributes:

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

**Bonus:** Works with VContainer/Zenject/Reflex for automatic DI + relational wiring!

[📖 Learn More](docs/features/relational-components/relational-components.md) | [🎯 DI – VContainer](Samples~/DI%20-%20VContainer/README.md) | [🎯 DI – Zenject](Samples~/DI%20-%20Zenject/README.md) | [🎯 DI – Reflex](Samples~/DI%20-%20Reflex/README.md)

---

### 3. 🎮 Data-Driven Effects

#### ⏱️ 2-4 hours/effect × 50 effects = 150+ hours saved

Designers create buffs/debuffs as ScriptableObjects. Zero programmer time after 20-minute setup:

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

**Beyond buffs:** Tags become a powerful capability system for AI decisions, permission gates, state management, and complex gameplay interactions (invulnerability, stealth, elemental systems).

[📖 Full Guide](docs/features/effects/effects-system.md) | [🚀 5-Minute Tutorial](docs/features/effects/effects-system-tutorial.md)

---

### 4. 💾 Unity-Aware Serialization

#### ⏱️ 40+ hours on initial implementation + prevents player data loss

JSON/Protobuf that understands `Vector3`, `GameObject`, `Color` - no custom converters needed:

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

[📖 Serialization Guide](docs/features/serialization/serialization.md)

---

### 5. 🎱 Professional Pooling

#### ⏱️ Eliminates GC spikes = 5-10 FPS in complex scenes

Zero-allocation queries with automatic cleanup. Thread-safe pooling in one line:

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

### 6. 🛠️ Editor Tools Suite

#### ⏱️ 1-2 hours/operation × weekly use = 100+ hours/year

20+ tools that automate sprite cropping, animation creation, atlas generation, prefab validation:

**Common workflows:**

- **Sprite Cropper**: Add or remove transparent pixels from 500 sprites → 1 click (was: 30 minutes in Photoshop)
- **Animation Creator**: Bulk-create clips from naming patterns (`walk_0001.png`) → 1 minute (was: 20 minutes)
- **Prefab Checker**: Validate 200 prefabs for missing references → 1 click (was: manual QA)
- **Atlas Generator**: Create sprite atlases from regex/labels → automated (was: manual setup)

[📖 Editor Tools Guide](docs/features/editor-tools/editor-tools-guide.md)

---

## 🎁 Batteries-Included Extensions

**Stop Googling "Unity how to..." for the 100th time.**

Unity Helpers includes 200+ extension methods that handle the tedious stuff you're tired of writing:

### Hierarchy Traversal (Optimized)

```csharp
// Get all ancestors without allocating
transform.GetAncestors(buffer);  // 10x faster than recursive GetComponentInParent loops

// Find specific ancestor
Canvas canvas = transform.GetAncestor<Canvas>();  // Stops at first match

// Breadth-first child search with depth control
transform.GetDescendants(buffer, maxDepth: 2);  // Avoid traversing entire tree
```

**Why this matters:** The naive way allocates arrays on every call. These methods use buffering and early-exit for hot paths.

### Unity Type Extensions

```csharp
// Color averaging (4 methods: LAB, HSV, Weighted, Dominant)
Color teamColor = sprite.GetAverageColor(ColorAveragingMethod.LAB);  // Perceptually accurate

// Collider auto-fitting
polygonCollider.UpdateShapeToSprite();  // Instant sprite → collider sync

// Smooth rotation in one line
transform.rotation = transform.GetAngleWithSpeed(target, rotationSpeed, Time.deltaTime);

// Safe destruction (works in editor AND runtime)
gameObject.SmartDestroy();  // No more #if UNITY_EDITOR everywhere

// Camera world bounds
Bounds visibleArea = Camera.main.OrthographicBounds();  // Perfect for culling/spawning

// Predictive targeting (intercept moving targets)
if (Ballistics.TryGetInterceptVelocity(shooter, target, projectileSpeed, out Vector3 velocity)) {
    Instantiate(projectile, shooter, Quaternion.LookRotation(velocity));
}
```

### Math That Should Be Built-In

```csharp
// Positive modulo (no more negative results!)
int index = (-1).PositiveMod(array.Length);  // 4, not -1

// Wrapped add for ring buffers
index = index.WrappedAdd(2, capacity);  // Handles overflow correctly

// Approximate equality with tolerance
if (transform.position.x.Approximately(target.x, 0.01f)) { /* close enough */ }

// Polyline simplification (Douglas–Peucker)
List<Vector2> simplified = LineHelper.Simplify(path, epsilon: 0.5f);  // Reduce pathfinding waypoints
```

### Collection Utilities

```csharp
// Infinite iterator (no extra allocation)
foreach (var item in itemList.Infinite()) { /* cycles forever */ }

// Aggregate bounds from multiple renderers
Bounds? combined = renderers.Select(r => r.bounds).GetBounds();

// String similarity for fuzzy search
int distance = playerName.LevenshteinDistance("jon");  // "john" = 1, close match!

// Case conversions (6 styles: Pascal, Camel, Snake, Kebab, Title, Constant)
string apiKey = "user_name".ToPascalCase();  // "UserName"
```

**Full list:** [Math & Extensions Guide](docs/features/utilities/math-and-extensions.md) | [Reflection Helpers](docs/features/utilities/reflection-helpers.md)

---

## 💎 Hidden Gems Worth Discovering

These powerful utilities solve specific problems that waste hours if you implement them yourself:

| Feature                                                                                      | What It Does                                          | Time Saved                           |
| -------------------------------------------------------------------------------------------- | ----------------------------------------------------- | ------------------------------------ |
| **[Predictive Targeting](docs/features/utilities/math-and-extensions.md#predictive-target)** | Perfect ballistics for turrets/missiles in one call   | 2-3 hours per shooting system        |
| **[Coroutine Jitter](docs/features/utilities/math-and-extensions.md#unity-extensions)**      | Prevents 100 enemies polling on same frame            | Eliminates frame spikes              |
| **[IL-Emitted Reflection](docs/features/utilities/reflection-helpers.md)**                   | 100x faster than System.Reflection, IL2CPP safe       | Critical for serialization/modding   |
| **[SmartDestroy()](docs/features/utilities/math-and-extensions.md#lifecycle-helpers)**       | Editor/runtime safe destruction (no scene corruption) | Prevents countless debugging hours   |
| **[Convex/Concave Hulls](docs/features/spatial/hulls.md)**                                   | Generate territory borders from point clouds          | 4-6 hours per hull algorithm         |
| **[Logging Extensions](docs/features/logging/logging-extensions.md)**                        | Rich tags, thread-aware logs, per-object toggles      | Keeps consoles readable + actionable |

---

## Why Teams Choose Unity Helpers

**The Reality:** You're spending 30-40% of your time writing the same GetComponent boilerplate, spatial query loops, and save/load plumbing over and over. Unity Helpers gives you that time back.

**Built for Real Projects:**

- ✅ **Production-tested** in shipped commercial games
- ✅ **4,000+ automated tests** catch edge cases before you hit them
- ✅ **Zero dependencies** - drop it in any project
- ✅ **IL2CPP/WebGL ready** with optimized SINGLE_THREADED paths
- ✅ **MIT Licensed** - use freely in commercial projects

**Who This Is For:**

- **Indie devs** who need professional tools without enterprise overhead
- **Teams** who value performance and want their junior devs to use battle-tested code
- **Senior engineers** who are tired of re-implementing the same utilities every project

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

#### Solution: Use link.xml to preserve required types

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
- [Serialization Guide: IL2CPP Warning](docs/features/serialization/serialization.md#️-il2cpp-and-code-stripping-warning)
- [Reflection Helpers: IL2CPP Warning](docs/features/utilities/reflection-helpers.md#️-il2cpp-code-stripping-considerations)

---

## Quick Start Guide

> 💡 **First time?** Skip to section #2 ([Relational Components](#2--auto-wire-components)) - it has the biggest immediate impact.

Already read the [Top 5 Time-Savers](#-top-time-savers)? Jump directly to the [Core Features](#core-features) reference below, or check out the comprehensive [Getting Started Guide](docs/overview/getting-started.md).

---

## Core Features

### Random Number Generators

Unity Helpers includes **15 high-quality random number generators**, all implementing a rich `IRandom` interface:

#### Available Generators

> The tables below are auto-generated by the performance benchmark. Run  
> `RandomPerformanceTests.Benchmark` in the Unity Test Runner to refresh them.

<!-- RANDOM_BENCHMARKS_START -->

_No benchmark data available yet. Run `RandomPerformanceTests.Benchmark` to populate these tables._

<!-- RANDOM_BENCHMARKS_END -->

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

[📊 Performance Comparison](docs/performance/random-performance.md)

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

[📊 2D Benchmarks](docs/performance/spatial-tree-2d-performance.md) | [📊 3D Benchmarks](docs/performance/spatial-tree-3d-performance.md)

For behavior details and edge cases, see: [Spatial Tree Semantics](docs/features/spatial/spatial-tree-semantics.md)

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
- `[WInLineEditor]` - Inline inspector editing for object references
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

See the in-depth guide: [Relational Components](docs/features/relational-components/relational-components.md).
Performance snapshots: [Relational Component Performance Benchmarks](docs/performance/relational-components-performance.md).

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

**Power Pattern:** Tags aren't just for buffs—use them to build robust capability systems for invulnerability, AI decision-making, permission gates, state management, and elemental interactions. See [Advanced Scenarios](docs/features/effects/effects-system.md#advanced-scenarios-beyond-buffs-and-debuffs) for patterns.

Further reading: see the full guide [Effects System](docs/features/effects/effects-system.md).

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

[Full guide: Serialization](docs/features/serialization/serialization.md)

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

[Full guide: Data Structures](docs/features/utilities/data-structures.md)

---

### Core Math & Extensions

Numeric helpers, geometry primitives, Unity extensions, colors, collections, strings, directions.

See the guide: [Core Math & Extensions](docs/features/utilities/math-and-extensions.md).

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

See the guide: [Singleton Utilities](docs/features/utilities/singletons.md) and the tool: [ScriptableObject Singleton Creator](docs/features/editor-tools/editor-tools-guide.md#scriptableobject-singleton-creator).

---

### Editor Tools

Unity Helpers includes 20+ editor tools to streamline your workflow:

- **Sprite Tools**: Cropper, Atlas Generator, Animation Editor, Pivot Adjuster
- **Texture Tools**: Blur, Resize, Settings Applier, Fit Texture Size
- **Animation Tools**: Event Editor, Creator, Copier, Sheet Animation Creator
- **Validation**: Prefab Checker with comprehensive validation rules
- **Automation**: ScriptableObject Singleton Creator, Attribute Cache Generator

[📖 Complete Editor Tools Documentation](docs/features/editor-tools/editor-tools-guide.md)

**Quick Access:**

- Menu: `Tools > Wallstop Studios > Unity Helpers`
- Create Assets: `Assets > Create > Wallstop Studios > Unity Helpers`

---

## Buffering Pattern

### Professional-Grade Object Pooling

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
- Reflex: `com.gustavopsantos.reflex`

**Manual or source imports (no UPM):**

- Add scripting defines in `Project Settings > Player > Other Settings > Scripting Define Symbols`:
  - `ZENJECT_PRESENT` when Zenject/Extenject is present
  - `VCONTAINER_PRESENT` when VContainer is present
  - `REFLEX_PRESENT` when Reflex is present
- Add the define per target platform (e.g., Standalone, Android, iOS).

**Notes:**

- When the define is present, optional assemblies under `Runtime/Integrations/*` compile automatically and expose helpers like `RelationalComponentsInstaller` (Zenject/Reflex) and `RegisterRelationalComponents()` (VContainer).
- If you use UPM, no manual defines are required — the package IDs above trigger symbols via `versionDefines` in the asmdefs.
- For test scenarios without LifetimeScope (VContainer), SceneContext (Zenject), or SceneScope (Reflex), see [DI Integrations: Testing and Edge Cases](docs/features/relational-components/relational-components.md#di-integrations-testing-and-edge-cases) for step‑by‑step patterns.

**Quick start:**

- **VContainer**: in your `LifetimeScope.Configure`, call `builder.RegisterRelationalComponents()`.
- **Zenject/Extenject**: add `RelationalComponentsInstaller` to your `SceneContext` (toggle scene scan if desired).
- **Reflex**: place `RelationalComponentsInstaller` on the same GameObject as your `SceneScope` to bind the assigner, run the scene scan, and (optionally) listen for additive scenes. Use `container.InjectWithRelations(...)` / `InstantiateGameObjectWithRelations(...)` for DI-friendly hydration.

```csharp
// VContainer — LifetimeScope
using VContainer;
using VContainer.Unity;
using WallstopStudios.UnityHelpers.Integrations.VContainer;

protected override void Configure(IContainerBuilder builder)
{
    // Register assigner + one-time scene scan + additive listener (default)
    builder.RegisterRelationalComponents(
        RelationalSceneAssignmentOptions.Default,
        enableAdditiveSceneListener: true
    );
}

// Zenject — prefab instantiation with DI + relations
using Zenject;
using WallstopStudios.UnityHelpers.Integrations.Zenject;

var enemy = Container.InstantiateComponentWithRelations(enemyPrefab, parent);

// Reflex — prefab instantiation with DI + relations
using Reflex.Core;
using WallstopStudios.UnityHelpers.Integrations.Reflex;

var enemy = container.InstantiateComponentWithRelations(enemyPrefab, parent);
```

See the full guide with scenarios, troubleshooting, and testing patterns: [Relational Components Guide](docs/features/relational-components/relational-components.md)

### Additional Helpers

- VContainer:
  - `resolver.InjectWithRelations(component)` — inject + assign a single instance
  - `resolver.InstantiateComponentWithRelations(prefab, parent)` — instantiate + inject + assign
  - `resolver.InjectGameObjectWithRelations(root, includeInactiveChildren)` — inject hierarchy + assign
  - `resolver.InstantiateGameObjectWithRelations(prefab, parent)` — instantiate GO + inject + assign

- Zenject:
  - `container.InjectWithRelations(component)` — inject + assign a single instance
  - `container.InstantiateComponentWithRelations(prefab, parent)` — instantiate + assign
  - `container.InjectGameObjectWithRelations(root, includeInactiveChildren)` — inject hierarchy + assign
  - `container.InstantiateGameObjectWithRelations(prefab, parent)` — instantiate GO + inject + assign

- Reflex:
  - `container.InjectWithRelations(component)` — inject + assign a single instance
  - `container.InstantiateComponentWithRelations(prefab, parent)` — instantiate + inject + assign
  - `container.InjectGameObjectWithRelations(root, includeInactiveChildren)` — inject hierarchy + assign
  - `container.InstantiateGameObjectWithRelations(prefab, parent)` — instantiate GO + inject + assign

### Additive Scene Loads

- VContainer: `RegisterRelationalComponents(..., enableAdditiveSceneListener: true)` registers a listener that hydrates components in newly loaded scenes.
- Zenject: `RelationalComponentsInstaller` exposes a toggle "Listen For Additive Scenes" to register the same behavior.
- Reflex: `RelationalComponentsInstaller` exposes a toggle "Listen For Additive Scenes" to register the same behavior.
  - Only the newly loaded scene is processed; other loaded scenes are not re‑scanned.

### Performance Options

- One-time scene scan runs after container build; additive scenes are handled incrementally.
- Single-pass scan (default) reduces `FindObjectsOfType` calls by scanning once and checking type ancestry.
  - VContainer: `new RelationalSceneAssignmentOptions(includeInactive: true, useSinglePassScan: true)`
  - Zenject: `new RelationalSceneAssignmentOptions(includeInactive: true, useSinglePassScan: true)`
  - Reflex: `new RelationalSceneAssignmentOptions(includeInactive: true, useSinglePassScan: true)`
- Per-object paths (instantiate/inject helpers, pools) avoid global scans entirely for objects created via DI.

---

## Performance

Unity Helpers is built with performance as a top priority:

**Random Number Generation:**

- 10-15x faster than Unity.Random (655-885M ops/sec vs 65-85M ops/sec)
- Zero GC pressure with thread-local instances
- [📊 Full Random Performance Benchmarks](docs/performance/random-performance.md)

**Spatial Queries:**

- O(log n) tree queries vs O(n) linear search
- 100-1000x faster for large datasets
- QuadTree2D: 10,000 objects = ~13 checks vs 10,000 checks
- [📊 2D Performance Benchmarks](docs/performance/spatial-tree-2d-performance.md)
- [📊 3D Performance Benchmarks](docs/performance/spatial-tree-3d-performance.md)

**Memory Management:**

- Zero-allocation buffering pattern eliminates GC spikes
- Professional-grade pooling for List, HashSet, Stack, Queue, Arrays
- 5-10 FPS improvement in complex scenes from stable GC

**Reflection:**

- Cached delegates are 10-100x faster than raw `System.Reflection` (boxed scenarios improve the most)
- Safe for IL2CPP and AOT platforms; capability overrides (`ReflectionHelpers.OverrideReflectionCapabilities`) let tests force expression/IL fallbacks
- Run the benchmarks via **ReflectionPerformanceTests.Benchmark** (EditMode Test Runner) and commit the updated markdown section
- [📘 Reflection Helpers Guide](docs/features/utilities/reflection-helpers.md) and [📊 Benchmarks](docs/performance/reflection-performance.md)

**List Sorting:**

- Multiple adaptive algorithms (`Ghost`, `Meteor`, `Power`, `Grail`, `Pattern-Defeating QuickSort`, `Insertion`) tuned for `IList<T>`
- Deterministic datasets (sorted, nearly sorted, shuffled) across sizes from 100 to 1,000,000
- [📊 IList Sorting Performance Benchmarks](docs/performance/ilist-sorting-performance.md)

---

## Documentation Index

**Start Here:**

- 🚀 Getting Started — [Getting Started Guide](docs/overview/getting-started.md)
- 🔍 Feature Index — [Complete A-Z Index](docs/overview/index.md)
- 📖 Glossary — [Term Definitions](docs/overview/glossary.md)

**Core Guides:**

- Serialization Guide — [Serialization](docs/features/serialization/serialization.md)
- Editor Tools Guide — [Editor Tools](docs/features/editor-tools/editor-tools-guide.md)
- Math & Extensions — [Core Math & Extensions](docs/features/utilities/math-and-extensions.md)
- Singletons — [Singleton Utilities](docs/features/utilities/singletons.md)
- Relational Components — [Relational Components](docs/features/relational-components/relational-components.md)
- Effects System — [Effects System](docs/features/effects/effects-system.md)
- Data Structures — [Data Structures](docs/features/utilities/data-structures.md)

**Spatial Trees:**

- 2D Spatial Trees Guide — [2D Spatial Trees Guide](docs/features/spatial/spatial-trees-2d-guide.md)
- 3D Spatial Trees Guide — [3D Spatial Trees Guide](docs/features/spatial/spatial-trees-3d-guide.md)
- Spatial Tree Semantics — [Spatial Tree Semantics](docs/features/spatial/spatial-tree-semantics.md)
- Spatial Tree 2D Performance — [Spatial Tree 2D Performance](docs/performance/spatial-tree-2d-performance.md)
- Spatial Tree 3D Performance — [Spatial Tree 3D Performance](docs/performance/spatial-tree-3d-performance.md)
- Hulls (Convex vs Concave) — [Hulls](docs/features/spatial/hulls.md)

**Performance & Reference:**

- Reflection Performance Guide — [Reflection Benchmarks](docs/performance/reflection-performance.md)
- Reflection AOT/Burst Validation — [IL2CPP & Burst Validation](docs/features/utilities/reflection-helpers.md#il2cppwebgl-notes)
- Reflection Benchmark Workflow — [Benchmarking & Verification](docs/performance/reflection-performance.md#benchmarking--verification)
- Random Performance — [Random Performance](docs/performance/random-performance.md)
- Reflection Helpers — [Reflection Helpers](docs/features/utilities/reflection-helpers.md)
- IList Sorting Performance — [IList Sorting Performance](docs/performance/ilist-sorting-performance.md)

**Project Info:**

- Changelog — [Changelog](docs/project/changelog.md)
- License — [License](docs/project/license.md)
- Third‑Party Notices — [Third‑Party Notices](docs/project/third-party-notices.md)
- Contributing — [Contributing](docs/project/contributing.md)

---

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Formatting Assistance

- Dependabot PRs: Formatting fixes (CSharpier + Prettier + markdownlint) are applied automatically by CI.
- Contributor PRs: Opt-in formatting is available.
  - Comment on the PR with `/format` (aliases: `/autofix`, `/lint-fix`).
    - If the PR branch is in this repo, the bot pushes a commit with fixes.
    - If the PR is from a fork, the bot opens a formatting PR targeting the base branch.
    - The commenter must be the PR author or a maintainer/collaborator.
  - Or run the Actions workflow manually: Actions → "Opt-in Formatting" → Run workflow → enter the PR number.
- Not everything is auto-fixable: link checks and YAML linting may still require manual changes.

See more details in [CONTRIBUTING](docs/project/contributing.md).

---

## License

This project is licensed under the MIT License - see the [LICENSE](docs/project/license.md) file for details.

## 2.0 Release Notes (Highlights)

- BinaryFormatter deprecated, still functional for trusted/legacy data:
  - `SerializationType.SystemBinary` is `[Obsolete]`. Use `SerializationType.Json` (System.Text.Json + Unity converters) or `SerializationType.Protobuf` (protobuf-net) for new work. Keep BinaryFormatter for trusted, non‑portable data only.

- GameObject JSON converter outputs structured JSON:
  - `GameObjectConverter` now writes a JSON object with `name`, `type` (assembly-qualified), and `instanceId` rather than a stringified placeholder.

- Minor robustness improvements:
  - Guarded stray `UnityEditor` imports in runtime files to ensure clean player builds.

See [Serialization guide](docs/features/serialization/serialization.md) for AOT/IL2CPP guidance and Unity JSON options, and [Editor tools guide](docs/features/editor-tools/editor-tools-guide.md) for Editor tool usage details.
