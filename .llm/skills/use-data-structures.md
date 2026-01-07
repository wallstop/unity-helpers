# Skill: Use Data Structures

<!-- trigger: data structure, collection, container | Selecting appropriate data structures | Feature -->

**Trigger**: When implementing collections, caches, priority scheduling, connectivity checks, string prefix operations, or bit manipulation.

---

## When to Use This Skill

This is an overview skill for selecting the right data structure. Use this when:

- You need to choose between multiple data structure options
- You want a quick reference for available structures
- You need to compare complexity/performance characteristics

For detailed API documentation and examples, see the specialized skills linked below.

---

## Available Structures Overview

| Structure          | Best For                                      | Skill Reference                                               |
| ------------------ | --------------------------------------------- | ------------------------------------------------------------- |
| `CyclicBuffer<T>`  | Fixed-size rolling history, ring buffers      | [use-queue-structures](./use-queue-structures.md)             |
| `Heap<T>`          | Priority ordering, A\* open sets              | [use-priority-structures](./use-priority-structures.md)       |
| `PriorityQueue<T>` | Task scheduling, event systems                | [use-priority-structures](./use-priority-structures.md)       |
| `Deque<T>`         | Double-ended queue, BFS, undo/redo            | [use-queue-structures](./use-queue-structures.md)             |
| `DisjointSet`      | Union-find, connectivity, clustering          | [use-algorithmic-structures](./use-algorithmic-structures.md) |
| `Trie`             | Prefix search, autocomplete, command matching | [use-algorithmic-structures](./use-algorithmic-structures.md) |
| `TimedCache<T>`    | Expiring cached computations                  | [use-algorithmic-structures](./use-algorithmic-structures.md) |
| `BitSet`           | Dense boolean flags, state masks, layer flags | [use-algorithmic-structures](./use-algorithmic-structures.md) |
| `QuadTree2D<T>`    | 2D spatial queries, collision detection       | [use-spatial-structure](./use-spatial-structure.md)           |
| `OctTree3D<T>`     | 3D spatial queries, collision detection       | [use-spatial-structure](./use-spatial-structure.md)           |
| `KDTree<T>`        | Nearest neighbor queries                      | [use-spatial-structure](./use-spatial-structure.md)           |
| `SpatialHash<T>`   | Uniform distribution, fast insertion          | [use-spatial-structure](./use-spatial-structure.md)           |

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
├─ Dense boolean flags → BitSet
└─ Spatial queries → See use-spatial-structure skill

Need serialization?
├─ YES → CyclicBuffer, Deque, DisjointSet, BitSet (all support ProtoBuf + Unity)
└─ NO → Any structure works
```

---

## Quick API Reference

### CyclicBuffer\<T\> - Rolling History

```csharp
CyclicBuffer<T> buffer = new CyclicBuffer<T>(capacity);
buffer.Add(item);           // Add item, overwrites oldest if full
buffer[index];              // Access by index (0 = oldest)
buffer.Count;               // Current number of items
```

See [use-queue-structures](./use-queue-structures.md) for full API and examples.

### Heap\<T\> - Priority Access

```csharp
Heap<T> heap = new Heap<T>(comparer);
heap.Push(item);            // Add item
heap.Pop();                 // Remove and return top item
heap.TryPeek(out T item);   // Safe peek
```

See [use-priority-structures](./use-priority-structures.md) for full API and examples.

### PriorityQueue\<T\> - Task Scheduling

```csharp
PriorityQueue<T> queue = PriorityQueue<T>.CreateMin();
queue.Enqueue(item);        // Add item
queue.Dequeue();            // Remove highest priority
queue.TryPeek(out T item);  // Safe peek
```

See [use-priority-structures](./use-priority-structures.md) for full API and examples.

### Deque\<T\> - Double-Ended Queue

```csharp
Deque<T> deque = new Deque<T>();
deque.PushFront(item);      // Add to front
deque.PushBack(item);       // Add to back
deque.PopFront();           // Remove from front
deque.PopBack();            // Remove from back
```

See [use-queue-structures](./use-queue-structures.md) for full API and examples.

### DisjointSet - Connectivity

```csharp
DisjointSet set = new DisjointSet(elementCount);
set.TryUnion(x, y);                     // Merge two sets
set.TryIsConnected(x, y, out bool c);   // Check if same set
set.SetCount;                           // Number of distinct sets
```

See [use-algorithmic-structures](./use-algorithmic-structures.md) for full API and examples.

### Trie - Prefix Search

```csharp
Trie trie = new Trie(wordCollection);
trie.Contains(word);                              // Exact match
trie.GetWordsWithPrefix(prefix, results, max);    // Prefix search
```

See [use-algorithmic-structures](./use-algorithmic-structures.md) for full API and examples.

### TimedCache\<T\> - Expiring Cache

```csharp
TimedCache<T> cache = new TimedCache<T>(valueProducer, cacheTtl);
cache.Value;                // Get cached value, recomputes if expired
cache.Reset();              // Force recomputation
```

See [use-algorithmic-structures](./use-algorithmic-structures.md) for full API and examples.

### BitSet - Dense Flags

```csharp
BitSet bits = new BitSet(capacity);
bits.TrySet(index);                 // Set bit to 1
bits.TryClear(index);               // Set bit to 0
bits.TryGet(index, out bool value); // Read bit
bits.And(other);                    // Bitwise AND
bits.Or(other);                     // Bitwise OR
```

See [use-algorithmic-structures](./use-algorithmic-structures.md) for full API and examples.

---

## Performance Tips

### Use Pooled Collections with Data Structures

```csharp
// Combine with Buffers for temporary results
using var lease = Buffers<string>.List.Get(out List<string> suggestions);
trie.GetWordsWithPrefix("sp", suggestions, maxResults: 10);
ProcessSuggestions(suggestions);
```

### Pre-size Collections

```csharp
// Avoid resizing by specifying initial capacity
Heap<PathNode> openSet = new Heap<PathNode>(comparer, capacity: 256);
Deque<Command> commandQueue = new Deque<Command>(initialCapacity: 64);
BitSet flags = new BitSet(initialCapacity: 1024);
```

### Heapify for Bulk Insert

```csharp
// Slow - O(n log n)
Heap<int> heap = new Heap<int>();
foreach (int item in items)
{
    heap.Push(item);
}

// Fast - O(n) heapify
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
| DisjointSet   | -        | -        | -        | O(a(n)) | O(n)           |
| Trie          | O(k)     | -        | -        | O(k)    | O(total chars) |
| TimedCache    | -        | -        | O(1)\*\* | -       | O(1)           |
| BitSet        | O(1)     | O(1)     | O(1)     | O(1)    | O(n/64)        |

\* Amortized, front/back only
\*\* May trigger recomputation if TTL expired
k = string length, a(n) = inverse Ackermann function (effectively constant)

---

## Related Skills

- [use-priority-structures](./use-priority-structures.md) - Heap and PriorityQueue details
- [use-queue-structures](./use-queue-structures.md) - CyclicBuffer and Deque details
- [use-algorithmic-structures](./use-algorithmic-structures.md) - DisjointSet, Trie, TimedCache, BitSet details
- [use-spatial-structure](./use-spatial-structure.md) - QuadTree, OctTree, KDTree, SpatialHash
- [use-pooling](./use-pooling.md) - Object pooling and Buffers
