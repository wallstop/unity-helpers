# Skill: Use Serializable Types

<!-- trigger: dictionary, hashset, nullable, type, guid | Dictionaries, HashSets, Nullable, Type, Guid | Feature -->

**Trigger**: When you need Unity-serializable collections (dictionaries, hash sets), nullable value types, type references, or GUIDs that work in the Inspector, JSON, and Protobuf.

---

## When to Use This Skill

Use this skill when you need:

- Dictionary or hash set collections that serialize in Unity Inspector
- Nullable value types (`int?`, `float?`) that Unity can serialize
- Type references (`System.Type`) stored in assets
- GUIDs that survive Unity serialization

For common patterns and integration examples, see [Serializable Types Patterns](./use-serializable-types-patterns.md).
For serialization system details, see [Serialization](./use-serialization.md).

---

## Overview

Unity's serialization system has limitations - it cannot serialize dictionaries, hash sets, nullable value types, `System.Type` references, or `System.Guid` directly. This package provides serializable wrappers that work seamlessly with:

- Unity Inspector
- Unity serialization (ScriptableObjects, MonoBehaviours)
- JSON serialization via `Serializer.JsonSerialize()`
- Protobuf serialization via `Serializer.ProtoSerialize()`

| Type                                         | Purpose                                  | Namespace                                                  |
| -------------------------------------------- | ---------------------------------------- | ---------------------------------------------------------- |
| `SerializableDictionary<TKey, TValue>`       | Dictionary with Inspector support        | `WallstopStudios.UnityHelpers.Core.DataStructure.Adapters` |
| `SerializableSortedDictionary<TKey, TValue>` | Sorted dictionary with Inspector support | `WallstopStudios.UnityHelpers.Core.DataStructure.Adapters` |
| `SerializableHashSet<T>`                     | HashSet with Inspector support           | `WallstopStudios.UnityHelpers.Core.DataStructure.Adapters` |
| `SerializableNullable<T>`                    | Nullable value types                     | `WallstopStudios.UnityHelpers.Core.DataStructure.Adapters` |
| `SerializableType`                           | `System.Type` reference                  | `WallstopStudios.UnityHelpers.Core.DataStructure.Adapters` |
| `WGuid`                                      | Unity-serializable GUID                  | `WallstopStudios.UnityHelpers.Core.DataStructure.Adapters` |

---

## SerializableDictionary&lt;TKey, TValue&gt;

A Unity-serializable dictionary that displays in the Inspector and supports JSON/Protobuf.

### Basic Usage

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

public sealed class LootTable : MonoBehaviour
{
    [SerializeField]
    private SerializableDictionary<string, int> _dropWeights = new();

    private void Awake()
    {
        // Use like a regular dictionary
        _dropWeights["Common"] = 80;
        _dropWeights["Rare"] = 15;
        _dropWeights["Legendary"] = 5;

        // All standard dictionary operations work
        if (_dropWeights.TryGetValue("Rare", out int weight))
        {
            Debug.Log($"Rare drop weight: {weight}");
        }

        foreach (KeyValuePair<string, int> entry in _dropWeights)
        {
            Debug.Log($"{entry.Key}: {entry.Value}");
        }
    }
}
```

### With Complex Values (Cache Pattern)

For complex value types that need special serialization handling, use the three-parameter variant with a cache class:

```csharp
using System;
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

[Serializable]
public sealed class WeaponDefinition
{
    public string DisplayName;
    public int Damage;
    public float AttackSpeed;
}

[Serializable]
public sealed class WeaponCache : SerializableDictionary.Cache<WeaponDefinition>
{
}

public sealed class WeaponRegistry : MonoBehaviour
{
    [SerializeField]
    private SerializableDictionary<string, WeaponDefinition, WeaponCache> _weapons = new();

    public WeaponDefinition GetWeapon(string id)
    {
        return _weapons.TryGetValue(id, out WeaponDefinition weapon) ? weapon : null;
    }
}
```

### Initialize From Existing Dictionary

```csharp
// Copy from standard dictionary
Dictionary<string, int> source = new()
{
    { "Gold", 100 },
    { "Silver", 50 }
};
SerializableDictionary<string, int> serializable = new(source);
```

---

## SerializableSortedDictionary&lt;TKey, TValue&gt;

A sorted dictionary that maintains key ordering. Keys must implement `IComparable<TKey>`.

### Basic Usage

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

public sealed class Leaderboard : MonoBehaviour
{
    [SerializeField]
    private SerializableSortedDictionary<string, int> _scores = new();

    private void Start()
    {
        _scores["Alice"] = 1200;
        _scores["Bob"] = 900;
        _scores["Charlie"] = 1500;

        // Iterates in sorted key order (Alice, Bob, Charlie)
        foreach (KeyValuePair<string, int> entry in _scores)
        {
            Debug.Log($"{entry.Key}: {entry.Value}");
        }
    }
}
```

### With Cache Pattern

```csharp
[Serializable]
public sealed class QuestDefinition
{
    public string Title;
    public int RequiredLevel;
}

[Serializable]
public sealed class QuestCache : SerializableDictionary.Cache<QuestDefinition>
{
}

// Keys sorted by quest ID
[Serializable]
public sealed class QuestDictionary
    : SerializableSortedDictionary<int, QuestDefinition, QuestCache>
{
}
```

---

## SerializableHashSet&lt;T&gt;

A Unity-serializable hash set for storing unique elements.

### Basic Usage

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

public sealed class AchievementTracker : MonoBehaviour
{
    [SerializeField]
    private SerializableHashSet<string> _unlockedAchievements = new();

    public void Unlock(string achievementId)
    {
        if (_unlockedAchievements.Add(achievementId))
        {
            Debug.Log($"Achievement unlocked: {achievementId}");
        }
    }

    public bool IsUnlocked(string achievementId)
    {
        return _unlockedAchievements.Contains(achievementId);
    }
}
```

### With Custom Comparer

```csharp
// Case-insensitive string set
SerializableHashSet<string> tags = new(StringComparer.OrdinalIgnoreCase);
tags.Add("Player");
tags.Contains("PLAYER");  // true

// From existing collection
string[] initialTags = { "Enemy", "Boss", "Flying" };
SerializableHashSet<string> enemies = new(initialTags);
```

### Set Operations

```csharp
SerializableHashSet<string> setA = new() { "A", "B", "C" };
SerializableHashSet<string> setB = new() { "B", "C", "D" };

// Union
setA.UnionWith(setB);  // A, B, C, D

// Intersection
setA.IntersectWith(setB);  // B, C

// Difference
setA.ExceptWith(setB);  // A

// Convert to standard HashSet
HashSet<string> copy = setA.ToHashSet();
```

---

## SerializableNullable&lt;T&gt;

A Unity-serializable alternative to `Nullable<T>` for value types. Shows a checkbox in the Inspector to toggle whether a value is set.

### Basic Usage

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

public sealed class SpawnSettings : MonoBehaviour
{
    [SerializeField]
    private SerializableNullable<float> _respawnDelay = new(5f);  // Has value

    [SerializeField]
    private SerializableNullable<int> _maxSpawns = new();  // No value (null)

    private void Start()
    {
        // Check if value exists
        if (_respawnDelay.HasValue)
        {
            Debug.Log($"Respawn delay: {_respawnDelay.Value}");
        }

        // TryGetValue pattern
        if (_maxSpawns.TryGetValue(out int max))
        {
            Debug.Log($"Max spawns: {max}");
        }

        // Get with default
        float delay = _respawnDelay.GetValueOrDefault(3f);
        int spawns = _maxSpawns.GetValueOrDefault(10);
    }
}
```

### Value Management

```csharp
SerializableNullable<int> score = new();

// Set value
score.SetValue(100);
Debug.Log(score.HasValue);  // true
Debug.Log(score.Value);     // 100

// Clear value
score.Clear();
Debug.Log(score.HasValue);  // false

// Implicit conversions
SerializableNullable<int> fromValue = 42;
SerializableNullable<int> fromNullable = (int?)null;
int? toNullable = score;  // Works both ways
```

### Use Cases

- Optional configuration values
- Fields that may or may not be set in Inspector
- Nullable value types in save data
- Optional parameters with clear "not set" state

---

## SerializableType

Stores a `System.Type` reference that survives serialization. Displays a searchable dropdown in the Inspector.

### Basic Usage

```csharp
using System;
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

public sealed class SpawnRule : MonoBehaviour
{
    [SerializeField]
    private SerializableType _behaviourType = new(typeof(EnemyController));

    private void SpawnEnemy(GameObject prefab)
    {
        // Resolve the stored type
        Type type = _behaviourType.Value;
        if (type != null)
        {
            prefab.AddComponent(type);
        }
    }
}
```

### Type Resolution

```csharp
SerializableType enemyType = new(typeof(EnemyController));

// Check if type is set
if (!enemyType.IsEmpty)
{
    // Get resolved type (null if type no longer exists)
    Type type = enemyType.Value;

    // Try pattern for safer access
    if (enemyType.TryGetValue(out Type resolved))
    {
        Debug.Log($"Type: {resolved.Name}");
    }

    // Display name for UI
    string displayName = enemyType.DisplayName;
}

// Create from type
SerializableType fromType = SerializableType.FromType(typeof(PlayerController));

// Assignment
enemyType.SetType(typeof(BossController));
```

### Inspector Features

- Searchable dropdown showing all available types
- Grouped by namespace
- Shows friendly display names
- Handles type renames/refactors gracefully

---

## WGuid

A Unity-serializable wrapper for `System.Guid`. Stores as two longs for efficient serialization.

### Basic Usage

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

public sealed class Entity : MonoBehaviour
{
    [SerializeField]
    private WGuid _id = WGuid.NewGuid();

    public WGuid Id => _id;

    private void Awake()
    {
        if (_id == WGuid.Empty)
        {
            _id = WGuid.NewGuid();
        }
        Debug.Log($"Entity ID: {_id}");
    }
}
```

### Conversion and Parsing

```csharp
// Generate new GUID
WGuid newId = WGuid.NewGuid();

// Convert to/from System.Guid (implicit)
Guid systemGuid = newId;
WGuid fromSystem = systemGuid;

// Parse from string
WGuid parsed = WGuid.Parse("2f3a9b4c-8d1f-4cba-8df7-2af00f5c6c1e");

// Safe parsing
if (WGuid.TryParse(userInput, out WGuid guid))
{
    Debug.Log($"Valid GUID: {guid}");
}

// From byte array
byte[] bytes = Guid.NewGuid().ToByteArray();
WGuid fromBytes = new WGuid(bytes);

// String output
string str = newId.ToString();
```

### Comparison and Collections

```csharp
WGuid id1 = WGuid.NewGuid();
WGuid id2 = WGuid.NewGuid();

// Equality
bool same = id1 == id2;
bool different = id1 != id2;

// Empty check
bool isEmpty = id1 == WGuid.Empty;

// Use in collections
HashSet<WGuid> pending = new();
pending.Add(id1);

Dictionary<WGuid, string> names = new();
names[id1] = "Player";
```

---

## Quick Reference

| Type                      | Create                  | Check Value        | Get Value                      |
| ------------------------- | ----------------------- | ------------------ | ------------------------------ |
| `SerializableDictionary`  | `new()`                 | `ContainsKey(key)` | `TryGetValue(key, out value)`  |
| `SerializableSortedDict`  | `new()`                 | `ContainsKey(key)` | `TryGetValue(key, out value)`  |
| `SerializableHashSet`     | `new()`                 | `Contains(item)`   | N/A (set membership)           |
| `SerializableNullable<T>` | `new()` or `new(value)` | `HasValue`         | `Value` or `GetValueOrDefault` |
| `SerializableType`        | `new(typeof(T))`        | `!IsEmpty`         | `Value` or `TryGetValue`       |
| `WGuid`                   | `WGuid.NewGuid()`       | `!= WGuid.Empty`   | Implicit conversion to `Guid`  |

---

## Related Skills

- [Serializable Types Patterns](./use-serializable-types-patterns.md) - Common patterns and integration examples
- [Serialization](./use-serialization.md) - JSON and Protobuf serialization details
- [Data Structures](./use-data-structures.md) - Other available data structures
