# Unity Helpers

[![Npm Publish](https://github.com/wallstop/unity-helpers/actions/workflows/npm-publish.yml/badge.svg)](https://github.com/wallstop/unity-helpers/actions/workflows/npm-publish.yml)

A comprehensive collection of high-performance utilities, data structures, and editor tools for Unity game development. Unity Helpers provides everything from blazing-fast random number generators and spatial trees to powerful editor wizards and component relationship management.

## Table of Contents

- [Why Unity Helpers?](#why-unity-helpers)
- [Key Features](#key-features)
- [Installation](#installation)
- [Compatibility](#compatibility)
- [Quick Start Guide](#quick-start-guide)
  - [Random Number Generation](#random-number-generation)
  - [Auto Component Discovery](#auto-component-discovery)
  - [Spatial Queries](#spatial-queries)
- [Core Features](#core-features)
  - [Random Number Generators](#random-number-generators)
  - [Spatial Trees](#spatial-trees)
  - [Effects, Attributes, and Tags](#effects-attributes-and-tags)
  - [Component Attributes](#component-attributes)
  - [Relational Components Guide](#relational-components-guide)
  - [Serialization](#serialization)
  - [Data Structures](#data-structures)
  - [Editor Tools](#editor-tools)
- [Use Cases & Examples](#use-cases--examples)
- [Performance](#performance)
- [Contributing](#contributing)
- [License](#license)
 - [Relational Components Guide](#relational-components-guide)

## Why Unity Helpers?

Unity Helpers was built to solve common game development challenges with **performance-first** solutions:

- 🚀 **10-15x faster** random number generation compared to Unity's built-in Random
- 🌳 **O(log n)** spatial queries instead of O(n) linear searches
- 🔧 **20+ editor tools** to streamline your workflow
- 📦 **Zero dependencies** - just import and use
- ✅ **Production-tested** in shipped games

## Key Features

### High-Performance Random Number Generators
- **12 different implementations** including PCG, XorShift, and more
- **Thread-safe** and **seedable** for deterministic gameplay
- **Rich API** with Gaussian distributions, noise maps, UUIDs, and more
- Up to **15x faster** than Unity.Random ([See benchmarks](#performance))

### Spatial Trees for Fast Queries
- **2D & 3D spatial trees** (QuadTree, OctTree, KDTree, RTree)
- Perfect for collision detection, AI, visibility culling
- **Massive performance gains** for games with many objects
- Immutable trees with O(log n) query performance

### Powerful Component Attributes
- `[ParentComponent]`, `[ChildComponent]`, `[SiblingComponent]` - Auto-wire components
- `[ValidateAssignment]` - Catch missing references at edit time
- `[DxReadOnly]` - Display calculated values in inspector
- `[WShowIf]` - Conditional inspector fields
 
 See the in-depth guide: `RELATIONAL_COMPONENTS.md`.

### 20+ Editor Tools
- **Sprite tools**: Cropper, Atlas Generator, Animation Editor
- **Texture tools**: Blur, Resize, Settings Applier
- **Validation**: Prefab Checker, Animation Event Editor
- **Automation**: ScriptableObject Singleton Creator
- [Full Editor Tools Documentation](EDITOR_TOOLS_GUIDE.md)

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

| Platform | Status |
| --- | --- |
| Unity 2021 | Likely, but untested |
| Unity 2022 | ✅ Supported |
| Unity 2023 | ✅ Supported |
| Unity 6 | ✅ Supported |
| URP | ✅ Compatible |
| HDRP | ✅ Compatible |

## Quick Start Guide

### Random Number Generation

Replace Unity's Random with high-performance alternatives:

```csharp
using WallstopStudios.UnityHelpers.Core.Random;

// Use the recommended default (currently PCG Random)
IRandom random = PRNG.Instance;

// Basic random values
float chance = random.NextFloat();           // 0.0f to 1.0f
int damage = random.Next(10, 20);            // 10 to 19
bool critical = random.NextBool();           // true or false

// Advanced features
Vector2 position = random.NextVector2();     // Random 2D position
Guid playerId = random.NextGuid();          // UUIDv4
float gaussian = random.NextGaussian();      // Normal distribution

// Random selection
string[] lootTable = { "Sword", "Shield", "Potion" };
string item = random.NextOf(lootTable);

// Weighted random
float[] weights = { 0.5f, 0.3f, 0.2f };
int index = random.NextWeightedIndex(weights);

// Noise generation
float[,] noiseMap = random.NextNoiseMap(256, 256, octaves: 4);
```

**Why use PRNG.Instance?**
- 10-15x faster than Unity.Random
- Seedable for deterministic gameplay
- Thread-safe for parallel operations
- Extensive API for common patterns

[📊 View Performance Benchmarks](RANDOM_PERFORMANCE.md)

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

    // Finds PlayerInput in parent hierarchy
    [ParentComponent]
    private PlayerInput input;

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

For a complete walkthrough with recipes, FAQs, and troubleshooting, see `RELATIONAL_COMPONENTS.md` (Troubleshooting: `RELATIONAL_COMPONENTS.md#troubleshooting`).

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

## Core Features

### Random Number Generators

Unity Helpers includes **12 high-quality random number generators**, all implementing a rich `IRandom` interface:

#### Available Generators

| Generator | Speed | Quality | Use Case |
|-----------|-------|---------|----------|
| **PcgRandom** ⭐ | Very Fast | Excellent | Default choice (via PRNG.Instance) |
| **RomuDuo** | Fastest | Good | Maximum performance needed |
| **LinearCongruentialGenerator** | Fastest | Fair | Simple, fast generation |
| **XorShiftRandom** | Very Fast | Good | General purpose |
| **XoroShiroRandom** | Very Fast | Good | General purpose |
| **SplitMix64** | Very Fast | Good | Initialization, hashing |
| **IllusionFlow** | Fast | Good | Balanced performance |
| **SquirrelRandom** | Moderate | Good | Hash-based generation |
| **WyRandom** | Moderate | Good | Hashing applications |
| **DotNetRandom** | Moderate | Good | .NET compatibility |
| **SystemRandom** | Slow | Good | Backward compatibility |
| **UnityRandom** | Very Slow | Good | Unity compatibility |

⭐ **Recommended**: Use `PRNG.Instance` which currently uses `PcgRandom`

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
IRandom seededRandom = new PcgRandom(seed: 12345);

// Same seed = same sequence
IRandom replay = new PcgRandom(seed: 12345);
// Both will generate identical values
```

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

- **OctTree3D** - Best general-purpose choice for 3D
- **KDTree3D** - Fast 3D nearest-neighbor queries
- **RTree3D** - Optimized for 3D bounding volumes

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

Further reading: see the full guide EFFECTS_SYSTEM.md.

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

Fast, compact serialization for save systems and networking.

```csharp
using WallstopStudios.UnityHelpers.Core.Serialization;

// JSON serialization with Unity type support
public class SaveData
{
    public Vector3 position;
    public Color playerColor;
    public List<GameObject> inventory;
}

SaveData data = new();
string json = JsonSerializer.Serialize(data);
SaveData loaded = JsonSerializer.Deserialize<SaveData>(json);

// Binary serialization with Protobuf
[Serializable]
public class NetworkMessage
{
    public int playerId;
    public Vector3 position;
}

byte[] bytes = ProtobufSerializer.Serialize(message);
NetworkMessage decoded = ProtobufSerializer.Deserialize<NetworkMessage>(bytes);
```

**Features:**
- Custom converters for Unity types (Vector2/3, Color, GameObject, etc.)
- Protobuf support for binary serialization
- LZMA compression utilities
- Type-safe serialization

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

## Performance

Unity Helpers is built with performance as a top priority. Here are some key metrics:

### Random Number Generation

Unity Helpers' random number generators are **10-15x faster** than Unity's built-in `UnityEngine.Random`:

- **UnityRandom**: 83M operations/sec
- **PcgRandom** (PRNG.Instance): 672M operations/sec (**8x faster**)
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

- `RELATIONAL_COMPONENTS.md`

Troubleshooting common issues (runtime-only assignment, filters, depth, inactive objects):

- `RELATIONAL_COMPONENTS.md#troubleshooting`

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
