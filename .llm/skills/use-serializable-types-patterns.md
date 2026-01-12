# Skill: Use Serializable Types - Patterns and Integration

<!-- trigger: serializable patterns, dictionary patterns, hashset patterns, collection patterns | Common patterns for serializable collections | Feature -->

**Trigger**: When implementing common patterns with Unity-serializable collections, integrating with JSON/Protobuf, or needing advanced usage examples.

---

## When to Use This Skill

Use this skill when you need:

- Common patterns for configuration, registries, and tracking systems
- Integration with JSON and Protobuf serialization
- Advanced usage examples for serializable collections
- Best practices for working with SerializableDictionary, SerializableHashSet, and related types

For basic type definitions and API reference, see [Serializable Types](./use-serializable-types.md).
For serialization system details, see [Serialization](./use-serialization.md).

---

## JSON/Protobuf Compatibility

All serializable types work automatically with the package's serialization system.

### JSON Example

```csharp
using WallstopStudios.UnityHelpers.Core.Serialization;

[Serializable]
public class GameState
{
    public SerializableDictionary<string, int> Scores = new();
    public SerializableHashSet<string> UnlockedLevels = new();
    public SerializableNullable<float> BestTime = new();
    public WGuid PlayerId = WGuid.NewGuid();
}

// Serialize to JSON
GameState state = new();
state.Scores["Level1"] = 1500;
state.UnlockedLevels.Add("Level1");
state.BestTime.SetValue(45.3f);

string json = Serializer.JsonSerialize(state, prettyPrint: true);

// Deserialize from JSON
GameState loaded = Serializer.JsonDeserialize<GameState>(json);
```

### Protobuf Example

```csharp
using ProtoBuf;
using WallstopStudios.UnityHelpers.Core.Serialization;

[ProtoContract]
public class SaveData
{
    [ProtoMember(1)]
    public SerializableDictionary<string, int> Inventory { get; set; } = new();

    [ProtoMember(2)]
    public SerializableHashSet<string> Achievements { get; set; } = new();

    [ProtoMember(3)]
    public SerializableNullable<int> HighScore { get; set; } = new();

    [ProtoMember(4)]
    public WGuid SessionId { get; set; } = WGuid.NewGuid();
}

// Serialize to bytes
SaveData data = new();
byte[] bytes = Serializer.ProtoSerialize(data);

// Deserialize from bytes
SaveData loaded = Serializer.ProtoDeserialize<SaveData>(bytes);
```

---

## Common Patterns

### Configuration with Defaults

```csharp
public sealed class EnemyConfig : MonoBehaviour
{
    [SerializeField]
    private SerializableDictionary<string, float> _statMultipliers = new()
    {
        { "Health", 1.0f },
        { "Damage", 1.0f },
        { "Speed", 1.0f }
    };

    [SerializeField]
    private SerializableNullable<float> _bossMultiplier = new();

    public float GetMultiplier(string stat)
    {
        float baseMultiplier = _statMultipliers.GetValueOrDefault(stat, 1.0f);
        float bossBonus = _bossMultiplier.GetValueOrDefault(1.0f);
        return baseMultiplier * bossBonus;
    }
}
```

### Entity Registry with GUIDs

```csharp
public sealed class EntityRegistry : MonoBehaviour
{
    [SerializeField]
    private SerializableDictionary<WGuid, string> _entityNames = new();

    public void Register(WGuid id, string name)
    {
        _entityNames[id] = name;
    }

    public string GetName(WGuid id)
    {
        return _entityNames.GetValueOrDefault(id, "Unknown");
    }
}
```

### Type-Based Factory

```csharp
public sealed class EnemyFactory : MonoBehaviour
{
    [SerializeField]
    private SerializableDictionary<string, SerializableType> _enemyTypes = new();

    public Component SpawnEnemy(string enemyId, GameObject prefab)
    {
        if (_enemyTypes.TryGetValue(enemyId, out SerializableType typeRef))
        {
            Type type = typeRef.Value;
            if (type != null)
            {
                return prefab.AddComponent(type);
            }
        }
        return null;
    }
}
```

### Unlockables Tracking

```csharp
public sealed class ProgressTracker : MonoBehaviour
{
    [SerializeField]
    private SerializableHashSet<string> _unlockedItems = new();

    [SerializeField]
    private SerializableSortedDictionary<string, int> _itemCounts = new();

    public void UnlockItem(string itemId)
    {
        _unlockedItems.Add(itemId);
    }

    public void AddItem(string itemId, int count)
    {
        if (_itemCounts.TryGetValue(itemId, out int current))
        {
            _itemCounts[itemId] = current + count;
        }
        else
        {
            _itemCounts[itemId] = count;
        }
    }
}
```

### Weighted Random Selection

```csharp
public sealed class LootDropper : MonoBehaviour
{
    [SerializeField]
    private SerializableDictionary<string, int> _dropWeights = new()
    {
        { "Common", 70 },
        { "Uncommon", 20 },
        { "Rare", 8 },
        { "Epic", 2 }
    };

    public string GetRandomDrop(IRandom random)
    {
        int totalWeight = 0;
        foreach (KeyValuePair<string, int> entry in _dropWeights)
        {
            totalWeight += entry.Value;
        }

        int roll = random.Next(totalWeight);
        int cumulative = 0;

        foreach (KeyValuePair<string, int> entry in _dropWeights)
        {
            cumulative += entry.Value;
            if (roll < cumulative)
            {
                return entry.Key;
            }
        }

        return "Common";
    }
}
```

### State Machine Transitions

```csharp
public sealed class StateMachine : MonoBehaviour
{
    [SerializeField]
    private SerializableDictionary<string, SerializableHashSet<string>> _validTransitions = new();

    private string _currentState = "Idle";

    public bool TryTransition(string targetState)
    {
        if (!_validTransitions.TryGetValue(_currentState, out SerializableHashSet<string> allowed))
        {
            return false;
        }

        if (!allowed.Contains(targetState))
        {
            return false;
        }

        _currentState = targetState;
        return true;
    }
}
```

### Save/Load with Versioning

```csharp
[ProtoContract]
public class VersionedSaveData
{
    [ProtoMember(1)]
    public int Version { get; set; } = 2;

    [ProtoMember(2)]
    public SerializableDictionary<string, int> PlayerStats { get; set; } = new();

    [ProtoMember(3)]
    public SerializableHashSet<WGuid> CompletedQuests { get; set; } = new();

    [ProtoMember(4)]
    public SerializableNullable<DateTime> LastPlayed { get; set; } = new();

    public void Migrate()
    {
        // Handle version migrations
        if (Version < 2)
        {
            // Migration logic from v1 to v2
            Version = 2;
        }
    }
}
```

### Caching with Nullable

```csharp
public sealed class ExpensiveCalculation : MonoBehaviour
{
    [SerializeField]
    private SerializableNullable<float> _cachedResult = new();

    [SerializeField]
    private SerializableDictionary<string, float> _parameterCache = new();

    public float Calculate(string parameter)
    {
        if (_parameterCache.TryGetValue(parameter, out float cached))
        {
            return cached;
        }

        float result = PerformExpensiveCalculation(parameter);
        _parameterCache[parameter] = result;
        return result;
    }

    public void InvalidateCache()
    {
        _cachedResult.Clear();
        _parameterCache.Clear();
    }

    private float PerformExpensiveCalculation(string parameter)
    {
        // Expensive work here
        return 0f;
    }
}
```

---

## Common Pitfalls

### Using Standard Dictionary

```csharp
// Won't serialize in Unity Inspector or save files
[SerializeField]
private Dictionary<string, int> scores;  // Not serializable!
```

### Use SerializableDictionary

```csharp
[SerializeField]
private SerializableDictionary<string, int> scores = new();  // Works!
```

### Forgetting to Initialize

```csharp
[SerializeField]
private SerializableDictionary<string, int> _scores;  // Might be null!

void Start()
{
    _scores["test"] = 1;  // NullReferenceException
}
```

### Always Initialize

```csharp
[SerializeField]
private SerializableDictionary<string, int> _scores = new();  // Safe

void Start()
{
    _scores["test"] = 1;  // Works
}
```

### Nullable in SerializeField

```csharp
[SerializeField]
private int? _optionalValue;  // Unity ignores this!
```

### Use SerializableNullable

```csharp
[SerializeField]
private SerializableNullable<int> _optionalValue = new();  // Works!
```

### Modifying During Iteration

```csharp
// ConcurrentModificationException risk
foreach (var key in _dictionary.Keys)
{
    if (ShouldRemove(key))
    {
        _dictionary.Remove(key);  // Dangerous!
    }
}
```

### Collect Keys First

```csharp
// Safe iteration with modification
List<string> toRemove = new();
foreach (var key in _dictionary.Keys)
{
    if (ShouldRemove(key))
    {
        toRemove.Add(key);
    }
}
foreach (string key in toRemove)
{
    _dictionary.Remove(key);
}
```

---

## Performance Tips

### Pre-size Collections When Possible

```csharp
// If you know approximate size
SerializableDictionary<string, int> dict = new(expectedCount);
SerializableHashSet<string> set = new(expectedCount);
```

### Use TryGetValue

```csharp
// Avoid double lookup
if (_dictionary.TryGetValue(key, out var value))
{
    // Use value directly
}

// Instead of
if (_dictionary.ContainsKey(key))
{
    var value = _dictionary[key];  // Second lookup
}
```

### Cache Frequently Accessed Values

```csharp
// If accessing same key repeatedly in a frame
private string _cachedKey;
private int _cachedValue;

public int GetValue(string key)
{
    if (key == _cachedKey)
    {
        return _cachedValue;
    }

    if (_dictionary.TryGetValue(key, out int value))
    {
        _cachedKey = key;
        _cachedValue = value;
        return value;
    }

    return 0;
}
```

---

## Related Skills

- [Serializable Types](./use-serializable-types.md) - Type definitions and API reference
- [Serialization](./use-serialization.md) - JSON and Protobuf serialization details
- [Data Structures](./use-data-structures.md) - Other available data structures
- [GC Architecture](./gc-architecture-unity.md) - Memory management considerations
