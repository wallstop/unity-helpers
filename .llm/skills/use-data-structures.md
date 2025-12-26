# Skill: Use Data Structures

**Trigger**: When implementing collections, caches, priority scheduling, connectivity checks, string prefix operations, or bit manipulation.

---

## Available Structures

| Structure          | Best For                                      |
| ------------------ | --------------------------------------------- |
| `CyclicBuffer<T>`  | Fixed-size rolling history, ring buffers      |
| `Heap<T>`          | Priority ordering, A\* open sets              |
| `PriorityQueue<T>` | Task scheduling, event systems                |
| `Deque<T>`         | Double-ended queue, BFS, undo/redo            |
| `DisjointSet`      | Union-find, connectivity, clustering          |
| `Trie`             | Prefix search, autocomplete, command matching |
| `TimedCache<T>`    | Expiring cached computations                  |
| `BitSet`           | Dense boolean flags, state masks, layer flags |

---

## Selection Guide

```text
What's your use case?
├─ Fixed-size history/trail → CyclicBuffer<T>
├─ Priority-based processing
│  ├─ Simple heap operations → Heap<T>
│  └─ Queue-like semantics → PriorityQueue<T>
├─ Insert/remove both ends → Deque<T>
├─ Connectivity/grouping → DisjointSet
├─ String prefix matching → Trie
├─ Expensive computation caching → TimedCache<T>
└─ Dense boolean flags → BitSet

Need serialization?
├─ YES → CyclicBuffer, Deque, DisjointSet, BitSet (all support ProtoBuf + Unity)
└─ NO → Any structure works
```

---

## CyclicBuffer\<T\>

Fixed-capacity ring buffer that overwrites old entries when full. Ideal for rolling logs, recent inputs, telemetry windows, and position trails.

### API

```csharp
CyclicBuffer<T> buffer = new CyclicBuffer<T>(capacity);

buffer.Add(item);           // Add item, overwrites oldest if full
buffer[index];              // Access by index (0 = oldest)
buffer.Count;               // Current number of items
buffer.Capacity;            // Maximum capacity
buffer.Remove(item);        // Remove specific item
buffer.Clear();             // Clear all items

// Allocation-free iteration
foreach (T item in buffer) { }
```

### Example: Position Trail

```csharp
public class TrailRenderer : MonoBehaviour
{
    private CyclicBuffer<Vector3> _positionHistory;

    private void Awake()
    {
        _positionHistory = new CyclicBuffer<Vector3>(32);
    }

    private void FixedUpdate()
    {
        _positionHistory.Add(transform.position);
    }

    private void OnDrawGizmos()
    {
        if (_positionHistory == null) return;

        Gizmos.color = Color.yellow;
        foreach (Vector3 pos in _positionHistory)
        {
            Gizmos.DrawSphere(pos, 0.1f);
        }
    }
}
```

---

## Heap\<T\>

Array-backed binary heap with min-heap or max-heap ordering. Optimized for priority-based element access with O(log n) push/pop.

### API

```csharp
// Min-heap (default)
Heap<T> heap = new Heap<T>();

// Custom ordering
Heap<T> heap = new Heap<T>(Comparer<T>.Create((a, b) => a.Priority.CompareTo(b.Priority)));

// From existing collection (O(n) heapify)
Heap<T> heap = new Heap<T>(items, comparer);

heap.Push(item);            // Add item
heap.Pop();                 // Remove and return top item
heap.Peek();                // View top item without removing
heap.TryPop(out T item);    // Safe removal
heap.TryPeek(out T item);   // Safe peek
heap.IsEmpty;               // Check if empty
heap.Count;                 // Number of items
heap.Clear();               // Remove all items
```

### Example: A\* Pathfinding Open Set

```csharp
public class PathFinder
{
    private readonly Heap<PathNode> _openSet;

    public PathFinder()
    {
        // Min-heap ordered by F cost
        _openSet = new Heap<PathNode>(
            Comparer<PathNode>.Create((a, b) => a.FCost.CompareTo(b.FCost))
        );
    }

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        _openSet.Clear();
        _openSet.Push(new PathNode(start, 0, Heuristic(start, goal)));

        while (!_openSet.IsEmpty && _openSet.TryPop(out PathNode current))
        {
            if (current.Position == goal)
                return ReconstructPath(current);

            foreach (PathNode neighbor in GetNeighbors(current))
            {
                _openSet.Push(neighbor);
            }
        }
        return null;
    }
}
```

---

## PriorityQueue\<T\>

Wrapper around `Heap<T>` with queue-like semantics. Clearer API for task scheduling, event systems, and AI decision making.

### API

```csharp
// Min-priority queue
PriorityQueue<T> queue = PriorityQueue<T>.CreateMin();

// Max-priority queue
PriorityQueue<T> queue = PriorityQueue<T>.CreateMax();

// Custom comparer
PriorityQueue<T> queue = new PriorityQueue<T>(comparer);

queue.Enqueue(item);           // Add item
queue.Dequeue();               // Remove and return highest priority
queue.Peek();                  // View highest priority without removing
queue.TryDequeue(out T item);  // Safe removal
queue.TryPeek(out T item);     // Safe peek
queue.IsEmpty;                 // Check if empty
queue.Count;                   // Number of items
queue.Clear();                 // Remove all items
```

### Example: Event Scheduler

```csharp
public class EventScheduler : MonoBehaviour
{
    private readonly PriorityQueue<ScheduledEvent> _events;

    public EventScheduler()
    {
        // Events ordered by execution time (earliest first)
        _events = new PriorityQueue<ScheduledEvent>(
            Comparer<ScheduledEvent>.Create((a, b) => a.ExecuteTime.CompareTo(b.ExecuteTime))
        );
    }

    public void Schedule(Action action, float delay)
    {
        _events.Enqueue(new ScheduledEvent(action, Time.time + delay));
    }

    private void Update()
    {
        while (!_events.IsEmpty && _events.TryPeek(out var evt) && evt.ExecuteTime <= Time.time)
        {
            _events.Dequeue();
            evt.Action?.Invoke();
        }
    }
}
```

---

## Deque\<T\>

Double-ended queue with O(1) insertion and removal from both front and back. Implemented as a circular array.

### API

```csharp
Deque<T> deque = new Deque<T>();
Deque<T> deque = new Deque<T>(initialCapacity);

deque.PushFront(item);      // Add to front
deque.PushBack(item);       // Add to back
deque.PopFront();           // Remove from front
deque.PopBack();            // Remove from back
deque.PeekFront();          // View front without removing
deque.PeekBack();           // View back without removing
deque.TryPopFront(out T);   // Safe front removal
deque.TryPopBack(out T);    // Safe back removal
deque[index];               // Random access
deque.Count;                // Number of items
deque.IsEmpty;              // Check if empty
deque.Clear();              // Remove all items
```

### Example: Undo/Redo System

```csharp
public class UndoRedoManager<T>
{
    private readonly Deque<T> _undoStack = new Deque<T>();
    private readonly Deque<T> _redoStack = new Deque<T>();
    private readonly int _maxHistory;

    public UndoRedoManager(int maxHistory = 50)
    {
        _maxHistory = maxHistory;
    }

    public void RecordState(T state)
    {
        _undoStack.PushBack(state);
        _redoStack.Clear();

        // Limit history size
        while (_undoStack.Count > _maxHistory)
        {
            _undoStack.PopFront();
        }
    }

    public bool TryUndo(out T previousState)
    {
        if (_undoStack.TryPopBack(out previousState))
        {
            _redoStack.PushBack(previousState);
            return true;
        }
        return false;
    }

    public bool TryRedo(out T nextState)
    {
        if (_redoStack.TryPopBack(out nextState))
        {
            _undoStack.PushBack(nextState);
            return true;
        }
        return false;
    }
}
```

### Example: BFS Traversal

```csharp
public static IEnumerable<T> BreadthFirstSearch<T>(T start, Func<T, IEnumerable<T>> getNeighbors)
{
    HashSet<T> visited = new HashSet<T>();
    Deque<T> queue = new Deque<T>();

    queue.PushBack(start);
    visited.Add(start);

    while (!queue.IsEmpty && queue.TryPopFront(out T current))
    {
        yield return current;

        foreach (T neighbor in getNeighbors(current))
        {
            if (visited.Add(neighbor))
            {
                queue.PushBack(neighbor);
            }
        }
    }
}
```

---

## DisjointSet

Union-find data structure with path compression and union by rank. Near-constant time O(α(n)) operations for connectivity queries.

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
}
```

---

## Performance Tips

### Use Pooled Collections with Data Structures

```csharp
// ✅ Combine with Buffers for temporary results
using var lease = Buffers<string>.List.Get(out List<string> suggestions);
trie.GetWordsWithPrefix("sp", suggestions, maxResults: 10);
ProcessSuggestions(suggestions);
```

### Pre-size Collections

```csharp
// ✅ Avoid resizing by specifying initial capacity
Heap<PathNode> openSet = new Heap<PathNode>(comparer, capacity: 256);
Deque<Command> commandQueue = new Deque<Command>(initialCapacity: 64);
BitSet flags = new BitSet(initialCapacity: 1024);
```

### Heapify for Bulk Insert

```csharp
// ❌ Slow - O(n log n)
Heap<int> heap = new Heap<int>();
foreach (int item in items)
{
    heap.Push(item);
}

// ✅ Fast - O(n) heapify
Heap<int> heap = new Heap<int>(items);
```

---

## Complexity Comparison

| Structure     | Insert   | Remove   | Peek     | Search  | Memory         |
| ------------- | -------- | -------- | -------- | ------- | -------------- |
| CyclicBuffer  | O(1)     | O(n)     | O(1)     | O(n)    | O(capacity)    |
| Heap          | O(log n) | O(log n) | O(1)     | O(n)    | O(n)           |
| PriorityQueue | O(log n) | O(log n) | O(1)     | O(n)    | O(n)           |
| Deque         | O(1)\*   | O(1)\*   | O(1)     | O(n)    | O(n)           |
| DisjointSet   | -        | -        | -        | O(α(n)) | O(n)           |
| Trie          | O(k)     | -        | -        | O(k)    | O(total chars) |
| TimedCache    | -        | -        | O(1)\*\* | -       | O(1)           |
| BitSet        | O(1)     | O(1)     | O(1)     | O(1)    | O(n/64)        |

\* Amortized, front/back only  
\*\* May trigger recomputation if TTL expired  
k = string length, α = inverse Ackermann function (effectively constant)
