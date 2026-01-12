# Skill: Use Priority Structures

<!-- trigger: heap, priority queue, scheduling | Priority ordering or task scheduling | Feature -->

**Trigger**: When implementing priority-based element access, A\* pathfinding open sets, task scheduling, or event systems.

---

## When to Use This Skill

- Implementing A\* or Dijkstra pathfinding algorithms
- Building event schedulers that process events by time
- Creating AI decision systems with weighted priorities
- Managing task queues where order matters
- Any scenario requiring efficient access to min/max elements

---

## Available Structures

| Structure          | Best For                         |
| ------------------ | -------------------------------- |
| `Heap<T>`          | Priority ordering, A\* open sets |
| `PriorityQueue<T>` | Task scheduling, event systems   |

### When to Use Each

- **Heap<T>**: Lower-level control, direct heap operations, custom bulk initialization
- **PriorityQueue<T>**: Queue-like semantics, clearer API for scheduling scenarios

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

// With initial capacity
Heap<T> heap = new Heap<T>(comparer, capacity: 256);

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

### Example: Top-K Selection

```csharp
public class TopKSelector<T>
{
    private readonly Heap<T> _maxHeap;
    private readonly int _k;

    public TopKSelector(int k, IComparer<T> comparer)
    {
        _k = k;
        // Use max-heap to efficiently maintain top-k smallest
        _maxHeap = new Heap<T>(Comparer<T>.Create((a, b) => comparer.Compare(b, a)));
    }

    public void Add(T item)
    {
        if (_maxHeap.Count < _k)
        {
            _maxHeap.Push(item);
        }
        else if (_maxHeap.TryPeek(out T max) && Comparer<T>.Default.Compare(item, max) < 0)
        {
            _maxHeap.Pop();
            _maxHeap.Push(item);
        }
    }

    public IEnumerable<T> GetTopK()
    {
        List<T> results = new List<T>(_maxHeap.Count);
        while (!_maxHeap.IsEmpty)
        {
            results.Add(_maxHeap.Pop());
        }
        results.Reverse();
        return results;
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

public readonly struct ScheduledEvent
{
    public readonly Action Action;
    public readonly float ExecuteTime;

    public ScheduledEvent(Action action, float executeTime)
    {
        Action = action;
        ExecuteTime = executeTime;
    }
}
```

### Example: AI Priority System

```csharp
public class AIPrioritySystem : MonoBehaviour
{
    private readonly PriorityQueue<AITask> _taskQueue;

    public AIPrioritySystem()
    {
        // Higher priority value = processed first
        _taskQueue = PriorityQueue<AITask>.CreateMax();
    }

    public void AddTask(AITask task)
    {
        _taskQueue.Enqueue(task);
    }

    public AITask GetNextTask()
    {
        return _taskQueue.TryDequeue(out AITask task) ? task : null;
    }

    public void ProcessTasks(int maxTasks)
    {
        int processed = 0;
        while (processed < maxTasks && _taskQueue.TryDequeue(out AITask task))
        {
            task.Execute();
            processed++;
        }
    }
}
```

---

## Performance Tips

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

### Pre-size Collections

```csharp
// Avoid resizing by specifying initial capacity
Heap<PathNode> openSet = new Heap<PathNode>(comparer, capacity: 256);
```

### Reuse Instances

```csharp
// Clear and reuse instead of creating new
_openSet.Clear();
// ... use the heap
```

### Use Pooled Results with Queries

```csharp
// Combine with Buffers for temporary processing
using var lease = Buffers<PathNode>.List.Get(out List<PathNode> results);
while (!_openSet.IsEmpty)
{
    results.Add(_openSet.Pop());
}
ProcessResults(results);
```

---

## Complexity

| Operation | Heap     | PriorityQueue |
| --------- | -------- | ------------- |
| Insert    | O(log n) | O(log n)      |
| Remove    | O(log n) | O(log n)      |
| Peek      | O(1)     | O(1)          |
| Search    | O(n)     | O(n)          |
| Heapify   | O(n)     | O(n)          |

Memory: O(n) for both structures

---

## Related Skills

- [use-data-structures](./use-data-structures.md) - Overview of all data structures
- [use-queue-structures](./use-queue-structures.md) - CyclicBuffer and Deque
- [use-algorithmic-structures](./use-algorithmic-structures.md) - DisjointSet, Trie, BitSet
- [use-spatial-structure](./use-spatial-structure.md) - Spatial trees for proximity queries
