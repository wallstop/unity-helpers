# Skill: Use Queue Structures

<!-- trigger: cyclic buffer, deque, ring buffer, undo redo | Rolling history, double-ended queues | Feature -->

**Trigger**: When implementing rolling history, position trails, undo/redo systems, BFS traversal, or any double-ended queue operations.

---

## When to Use This Skill

- Recording recent player inputs or positions
- Implementing undo/redo functionality
- Creating fixed-size rolling logs or telemetry
- BFS graph traversal algorithms
- Any scenario requiring efficient front/back insertion/removal

---

## Available Structures

| Structure         | Best For                                 |
| ----------------- | ---------------------------------------- |
| `CyclicBuffer<T>` | Fixed-size rolling history, ring buffers |
| `Deque<T>`        | Double-ended queue, BFS, undo/redo       |

### When to Use Each

- **CyclicBuffer<T>**: Fixed capacity that automatically overwrites oldest entries
- **Deque<T>**: Dynamic capacity with efficient operations at both ends

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

### Example: Input Buffer for Combo System

```csharp
public class ComboInputBuffer : MonoBehaviour
{
    private CyclicBuffer<InputAction> _inputBuffer;
    private const int BufferSize = 10;

    private void Awake()
    {
        _inputBuffer = new CyclicBuffer<InputAction>(BufferSize);
    }

    public void RecordInput(InputAction action)
    {
        _inputBuffer.Add(action);
    }

    public bool MatchesCombo(InputAction[] combo)
    {
        if (_inputBuffer.Count < combo.Length)
            return false;

        int bufferStart = _inputBuffer.Count - combo.Length;
        for (int i = 0; i < combo.Length; i++)
        {
            if (!_inputBuffer[bufferStart + i].Equals(combo[i]))
                return false;
        }
        return true;
    }

    public void ClearBuffer()
    {
        _inputBuffer.Clear();
    }
}
```

### Example: Rolling Average Calculator

```csharp
public class RollingAverage
{
    private readonly CyclicBuffer<float> _samples;

    public RollingAverage(int windowSize)
    {
        _samples = new CyclicBuffer<float>(windowSize);
    }

    public void AddSample(float value)
    {
        _samples.Add(value);
    }

    public float GetAverage()
    {
        if (_samples.Count == 0)
            return 0f;

        float sum = 0f;
        foreach (float sample in _samples)
        {
            sum += sample;
        }
        return sum / _samples.Count;
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

    public bool CanUndo => !_undoStack.IsEmpty;
    public bool CanRedo => !_redoStack.IsEmpty;
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

### Example: Sliding Window Maximum

```csharp
public class SlidingWindowMax
{
    private readonly Deque<(int index, int value)> _deque = new Deque<(int, int)>();
    private readonly int _windowSize;

    public SlidingWindowMax(int windowSize)
    {
        _windowSize = windowSize;
    }

    public int ProcessNext(int index, int value)
    {
        // Remove elements outside the window
        while (!_deque.IsEmpty && _deque.PeekFront().index <= index - _windowSize)
        {
            _deque.PopFront();
        }

        // Remove smaller elements from back
        while (!_deque.IsEmpty && _deque.PeekBack().value <= value)
        {
            _deque.PopBack();
        }

        _deque.PushBack((index, value));

        return _deque.PeekFront().value;
    }
}
```

### Example: Work Stealing Queue

```csharp
public class WorkStealingQueue<T>
{
    private readonly Deque<T> _tasks = new Deque<T>();
    private readonly object _lock = new object();

    // Owner pushes and pops from back
    public void Push(T task)
    {
        lock (_lock)
        {
            _tasks.PushBack(task);
        }
    }

    public bool TryPopOwn(out T task)
    {
        lock (_lock)
        {
            return _tasks.TryPopBack(out task);
        }
    }

    // Thieves steal from front
    public bool TrySteal(out T task)
    {
        lock (_lock)
        {
            return _tasks.TryPopFront(out task);
        }
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _tasks.Count;
            }
        }
    }
}
```

---

## Performance Tips

### Pre-size Collections

```csharp
// Avoid resizing by specifying initial capacity
Deque<Command> commandQueue = new Deque<Command>(initialCapacity: 64);
CyclicBuffer<Vector3> trail = new CyclicBuffer<Vector3>(32);
```

### Use Appropriate Capacity for CyclicBuffer

```csharp
// Capacity should match your actual needs
// Too small = losing important data
// Too large = wasted memory

// Position trail: ~1-2 seconds of positions
int trailCapacity = (int)(1.5f / Time.fixedDeltaTime);  // ~75 at 50fps
CyclicBuffer<Vector3> trail = new CyclicBuffer<Vector3>(trailCapacity);
```

### Allocation-Free Iteration

```csharp
// Both structures support allocation-free foreach
foreach (T item in buffer) { }
foreach (T item in deque) { }
```

---

## Complexity

| Operation     | CyclicBuffer | Deque  |
| ------------- | ------------ | ------ |
| Add/Push Back | O(1)         | O(1)\* |
| Push Front    | N/A          | O(1)\* |
| Pop Back      | N/A          | O(1)   |
| Pop Front     | N/A          | O(1)   |
| Remove        | O(n)         | O(n)   |
| Index Access  | O(1)         | O(1)   |
| Search        | O(n)         | O(n)   |

\* Amortized - occasional resize may occur

Memory:

- CyclicBuffer: O(capacity) - fixed
- Deque: O(n) - grows as needed

---

## Serialization Support

Both structures support ProtoBuf and Unity serialization:

```csharp
[Serializable]
public class SerializableTrail
{
    [SerializeField]
    private CyclicBuffer<Vector3> _positions;
}

[ProtoContract]
public class NetworkState
{
    [ProtoMember(1)]
    public Deque<InputFrame> InputHistory { get; set; }
}
```

---

## Related Skills

- [use-data-structures](./use-data-structures.md) - Overview of all data structures
- [use-priority-structures](./use-priority-structures.md) - Heap and PriorityQueue
- [use-algorithmic-structures](./use-algorithmic-structures.md) - DisjointSet, Trie, BitSet
- [use-pooling](./use-pooling.md) - Object pooling for reusable instances
