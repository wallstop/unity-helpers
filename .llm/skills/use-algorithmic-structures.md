# Skill: Use Algorithmic Structures

<!-- trigger: disjoint set, union find, trie, prefix tree, bitset, cache | Connectivity, prefix search, bit manipulation, caching | Feature -->

**Trigger**: When implementing connectivity queries, string prefix operations, dense boolean flags, or time-based caching.

---

## When to Use This Skill

- Checking connectivity between elements (islands, clusters, networks)
- Implementing autocomplete or command prefix matching
- Managing dense boolean state flags efficiently
- Caching expensive computations with expiration

---

## Available Structures

| Structure             | Best For                                      |
| --------------------- | --------------------------------------------- |
| `DisjointSet`         | Union-find, connectivity, clustering          |
| `Trie`                | Prefix search, autocomplete, command matching |
| `TimedCache<T>`       | Expiring cached computations                  |
| `BitSet`              | Dense boolean flags, state masks, layer flags |
| `ImmutableBitSet`     | Read-only bit operations                      |

---

## DisjointSet

Union-find data structure with path compression and union by rank. Near-constant time O(alpha(n)) operations for connectivity queries.

### API

```csharp
DisjointSet set = new DisjointSet(elementCount);

set.TryFind(x, out int root);           // Find set representative
set.TryUnion(x, y);                     // Merge two sets
set.TryIsConnected(x, y, out bool c);   // Check if same set
set.Count;                              // Total elements
set.SetCount;                           // Number of distinct sets
set.GetSetSize(x, out int size);        // Size of set containing x
set.GetAllSets();                       // Get all sets as lists
```

### Example: Procedural Island Detection

```csharp
public class IslandDetector
{
    public int CountIslands(bool[,] grid)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);
        DisjointSet islands = new DisjointSet(width * height);

        int ToIndex(int x, int y) => y * width + x;

        // Connect adjacent land cells
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!grid[x, y]) continue;

                int current = ToIndex(x, y);

                // Connect to right neighbor
                if (x + 1 < width && grid[x + 1, y])
                    islands.TryUnion(current, ToIndex(x + 1, y));

                // Connect to bottom neighbor
                if (y + 1 < height && grid[x, y + 1])
                    islands.TryUnion(current, ToIndex(x, y + 1));
            }
        }

        // Count unique land regions
        HashSet<int> uniqueRoots = new HashSet<int>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y] && islands.TryFind(ToIndex(x, y), out int root))
                {
                    uniqueRoots.Add(root);
                }
            }
        }

        return uniqueRoots.Count;
    }
}
```

### Example: Dynamic Connectivity

```csharp
public class NetworkConnectivity
{
    private readonly DisjointSet _nodes;

    public NetworkConnectivity(int nodeCount)
    {
        _nodes = new DisjointSet(nodeCount);
    }

    public void Connect(int nodeA, int nodeB)
    {
        _nodes.TryUnion(nodeA, nodeB);
    }

    public bool AreConnected(int nodeA, int nodeB)
    {
        return _nodes.TryIsConnected(nodeA, nodeB, out bool connected) && connected;
    }

    public int GetNetworkCount() => _nodes.SetCount;

    public int GetNetworkSize(int node)
    {
        return _nodes.GetSetSize(node, out int size) ? size : 0;
    }
}
```

---

## Trie

Array-backed prefix tree for fast string operations. Optimized for prefix search and exact word lookup with minimal allocations.

### API

```csharp
Trie trie = new Trie(wordCollection);

trie.Contains(word);                              // Exact match
trie.GetWordsWithPrefix(prefix, results, max);    // Prefix search
trie.Count;                                       // Number of words

// Iteration
foreach (string word in trie) { }
```

### Example: Command Autocomplete

```csharp
public class CommandSystem
{
    private readonly Trie _commands;
    private readonly Dictionary<string, Action> _handlers;

    public CommandSystem()
    {
        string[] validCommands = { "spawn", "speed", "spectate", "save", "settings" };
        _commands = new Trie(validCommands);
        _handlers = new Dictionary<string, Action>();
    }

    public List<string> GetSuggestions(string input, int maxResults = 5)
    {
        List<string> results = new List<string>();
        _commands.GetWordsWithPrefix(input, results, maxResults);
        return results;
    }

    public bool TryExecute(string command)
    {
        if (_commands.Contains(command) && _handlers.TryGetValue(command, out Action handler))
        {
            handler?.Invoke();
            return true;
        }
        return false;
    }
}
```

### Example: Word Validation

```csharp
public class WordValidator
{
    private readonly Trie _dictionary;

    public WordValidator(IEnumerable<string> validWords)
    {
        _dictionary = new Trie(validWords);
    }

    public bool IsValidWord(string word)
    {
        return _dictionary.Contains(word);
    }

    public bool HasWordsStartingWith(string prefix)
    {
        List<string> results = new List<string>();
        _dictionary.GetWordsWithPrefix(prefix, results, maxResults: 1);
        return results.Count > 0;
    }
}
```

---

## TimedCache\<T\>

Lightweight time-based cache that recomputes values after a TTL expires. Optional jitter prevents thundering herd.

### API

```csharp
TimedCache<T> cache = new TimedCache<T>(
    valueProducer,      // Factory function
    cacheTtl,           // Time to live in seconds
    useJitter,          // Optional: spread refreshes
    timeProvider,       // Optional: custom time source
    jitterOverride      // Optional: custom jitter amount
);

cache.Value;            // Get cached value, recomputes if expired
cache.Reset();          // Force recomputation on next access
```

### Example: Expensive Query Cache

```csharp
public class EnemyRadar : MonoBehaviour
{
    private TimedCache<int> _nearbyEnemyCount;
    private TimedCache<Enemy> _closestEnemy;
    private Collider[] _colliders = new Collider[32];
    private int enemyLayer;

    private void Awake()
    {
        // Recompute enemy count every 0.5 seconds with jitter
        _nearbyEnemyCount = new TimedCache<int>(
            () => Physics.OverlapSphereNonAlloc(transform.position, 50f, _colliders, enemyLayer),
            cacheTtl: 0.5f,
            useJitter: true
        );

        // Cache closest enemy for 0.25 seconds
        _closestEnemy = new TimedCache<Enemy>(
            () => FindClosestEnemy(),
            cacheTtl: 0.25f
        );
    }

    public int NearbyEnemyCount => _nearbyEnemyCount.Value;
    public Enemy ClosestEnemy => _closestEnemy.Value;

    private Enemy FindClosestEnemy() { /* ... */ return null; }
}
```

### Example: Configuration Cache

```csharp
public class ConfigCache
{
    private readonly TimedCache<GameConfig> _config;

    public ConfigCache()
    {
        // Reload config every 60 seconds
        _config = new TimedCache<GameConfig>(
            () => LoadConfigFromFile(),
            cacheTtl: 60f,
            useJitter: true  // Spread reloads across instances
        );
    }

    public GameConfig Config => _config.Value;

    public void ForceReload() => _config.Reset();

    private GameConfig LoadConfigFromFile() { /* ... */ return null; }
}
```

---

## BitSet / ImmutableBitSet

Compact bit storage using a single bit per boolean flag. Ideal for entity state masks, collision layers, and dense flag arrays.

### API

```csharp
BitSet bits = new BitSet(initialCapacity);

bits.TrySet(index);                 // Set bit to 1
bits.TryClear(index);               // Set bit to 0
bits.TryGet(index, out bool value); // Read bit
bits[index];                        // Indexer (get/set)
bits.Capacity;                      // Current capacity
bits.SetAll();                      // Set all bits to 1
bits.ClearAll();                    // Set all bits to 0
bits.And(other);                    // Bitwise AND
bits.Or(other);                     // Bitwise OR
bits.Xor(other);                    // Bitwise XOR
bits.Not();                         // Bitwise NOT

// ImmutableBitSet for read-only scenarios
ImmutableBitSet immutable = new ImmutableBitSet(bits);
ImmutableBitSet immutable = new ImmutableBitSet(trueIndices);
```

### Example: Entity State Flags

```csharp
public class EntityStateManager
{
    private enum StateFlag { Active = 0, Visible = 1, Damaged = 2, Invincible = 3 }

    private readonly BitSet _entityStates;

    public EntityStateManager(int maxEntities)
    {
        // 4 flags per entity
        _entityStates = new BitSet(maxEntities * 4);
    }

    private int GetFlagIndex(int entityId, StateFlag flag) => entityId * 4 + (int)flag;

    public void SetFlag(int entityId, StateFlag flag)
    {
        _entityStates.TrySet(GetFlagIndex(entityId, flag));
    }

    public void ClearFlag(int entityId, StateFlag flag)
    {
        _entityStates.TryClear(GetFlagIndex(entityId, flag));
    }

    public bool HasFlag(int entityId, StateFlag flag)
    {
        return _entityStates.TryGet(GetFlagIndex(entityId, flag), out bool value) && value;
    }
}
```

### Example: Layer Mask Operations

```csharp
public class LayerMaskHelper
{
    public static BitSet FromUnityLayerMask(LayerMask mask)
    {
        BitSet bits = new BitSet(32);
        int maskValue = mask.value;
        for (int i = 0; i < 32; i++)
        {
            if ((maskValue & (1 << i)) != 0)
            {
                bits.TrySet(i);
            }
        }
        return bits;
    }

    public static BitSet CombineMasks(BitSet a, BitSet b)
    {
        BitSet result = new BitSet(a.Capacity);
        result.Or(a);
        result.Or(b);
        return result;
    }

    public static BitSet IntersectMasks(BitSet a, BitSet b)
    {
        BitSet result = new BitSet(a.Capacity);
        result.Or(a);
        result.And(b);
        return result;
    }
}
```

### Example: Visibility Culling

```csharp
public class VisibilityCuller
{
    private readonly BitSet _visibleObjects;

    public VisibilityCuller(int maxObjects)
    {
        _visibleObjects = new BitSet(maxObjects);
    }

    public void SetVisible(int objectId) => _visibleObjects.TrySet(objectId);
    public void SetHidden(int objectId) => _visibleObjects.TryClear(objectId);
    public bool IsVisible(int objectId) =>
        _visibleObjects.TryGet(objectId, out bool v) && v;

    public void ClearAll() => _visibleObjects.ClearAll();
}
```

---

## Complexity Comparison

| Structure   | Insert   | Remove | Search  | Memory         |
| ----------- | -------- | ------ | ------- | -------------- |
| DisjointSet | -        | -      | O(alpha(n)) | O(n)           |
| Trie        | O(k)     | -      | O(k)    | O(total chars) |
| TimedCache  | -        | -      | O(1)*   | O(1)           |
| BitSet      | O(1)     | O(1)   | O(1)    | O(n/64)        |

k = string length, alpha = inverse Ackermann function (effectively constant)
\* May trigger recomputation if TTL expired

---

## Serialization Support

These structures support ProtoBuf and Unity serialization:

```csharp
[ProtoContract]
public class SaveData
{
    [ProtoMember(1)]
    public DisjointSet Connectivity { get; set; }

    [ProtoMember(2)]
    public BitSet UnlockedFeatures { get; set; }
}
```

---

## Related Skills

- [use-data-structures](./use-data-structures.md) - Overview of all data structures
- [use-priority-structures](./use-priority-structures.md) - Heap and PriorityQueue
- [use-queue-structures](./use-queue-structures.md) - CyclicBuffer and Deque
- [use-spatial-structure](./use-spatial-structure.md) - Spatial trees for proximity queries
