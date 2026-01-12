# Skill: Use Extension Methods

<!-- trigger: extension, utility, collection, string, color | Collection, string, color utilities | Feature -->

**Trigger**: When manipulating collections, strings, or colors and need convenient, performant utilities beyond built-in methods.

---

## Dictionary Extensions

### GetOrAdd - Add If Missing

```csharp
using WallstopStudios.UnityHelpers.Core.Extension;

// Get existing or create new value with factory
Dictionary<string, List<Item>> itemsByCategory = new();
List<Item> weapons = itemsByCategory.GetOrAdd("weapons", () => new List<Item>());
weapons.Add(sword);

// With key-based factory
Dictionary<int, PlayerData> playerCache = new();
PlayerData data = playerCache.GetOrAdd(playerId, id => LoadPlayerData(id));

// With parameterless constructor constraint
Dictionary<string, List<int>> scores = new();
List<int> playerScores = scores.GetOrAdd<string, List<int>>("player1");  // Creates new List<int>()
```

### GetOrElse - Default Without Modification

```csharp
// Get value or return default without modifying dictionary
IReadOnlyDictionary<string, int> config = GetConfig();
int maxPlayers = config.GetOrElse("maxPlayers", () => 4);
int timeout = config.GetOrElse("timeout", 30);  // Direct value overload
```

### TryRemove - Safe Removal With Value

```csharp
// Remove and get removed value in one operation
Dictionary<int, Enemy> enemies = new();
if (enemies.TryRemove(enemyId, out Enemy removed))
{
    removed.OnDespawn();
}
```

### AddOrUpdate - Upsert Pattern

```csharp
// Add new or update existing
Dictionary<string, int> scoreboard = new();
scoreboard.AddOrUpdate(
    playerName,
    key => 1,                           // Creator: first kill
    (key, existing) => existing + 1     // Updater: increment kills
);
```

### Merge - Combine Dictionaries

```csharp
// Merge two dictionaries (right overwrites left)
var defaults = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
var overrides = new Dictionary<string, int> { ["b"] = 5, ["c"] = 3 };
Dictionary<string, int> merged = defaults.Merge(overrides);
// Result: { ["a"] = 1, ["b"] = 5, ["c"] = 3 }
```

---

## List Extensions

### Shuffle - Fisher-Yates In-Place

```csharp
using WallstopStudios.UnityHelpers.Core.Extension;

List<Card> deck = GetAllCards();

// Shuffle using default PRNG
deck.Shuffle();

// Shuffle with custom random
deck.Shuffle(myRandom);
```

**Performance**: O(n), **Allocations**: None

### GetRandomElement - Random Selection

```csharp
List<Enemy> enemies = GetAllEnemies();
Enemy target = enemies.GetRandomElement();           // Uses PRNG.Instance
Enemy target2 = enemies.GetRandomElement(myRandom);  // Custom random
```

**Performance**: O(1), **Allocations**: None

### RemoveAtSwapBack - Fast Unordered Removal

```csharp
// ❌ Standard removal: O(n) - shifts all elements after index
enemies.RemoveAt(index);

// ✅ Swap-back removal: O(1) - swaps with last element, then removes last
enemies.RemoveAtSwapBack(index);  // Does not preserve order!
```

**Performance**: O(1), **Allocations**: None

### IndexOf / LastIndexOf with Predicate

```csharp
List<Enemy> enemies = GetEnemies();

// Find first enemy with low health
int index = enemies.IndexOf(e => e.Health < 10);

// Find last enemy that can attack
int lastIndex = enemies.LastIndexOf(e => e.CanAttack);
```

**Performance**: O(n), **Allocations**: None

### Shift - Rotate Elements

```csharp
List<int> numbers = new() { 1, 2, 3, 4, 5 };

numbers.Shift(2);   // Result: { 4, 5, 1, 2, 3 } (shift right)
numbers.Shift(-1);  // Result: { 2, 3, 4, 5, 1 } (shift left)
```

**Performance**: O(n), **Allocations**: None

### Reverse Range

```csharp
List<int> numbers = new() { 1, 2, 3, 4, 5 };
numbers.Reverse(1, 3);  // Result: { 1, 4, 3, 2, 5 }
```

**Performance**: O(n), **Allocations**: None

### Pooled Sorting

```csharp
using WallstopStudios.UnityHelpers.Core.Extension;

List<Enemy> enemies = GetEnemies();

// Sort with custom comparer and algorithm
enemies.Sort(
    Comparer<Enemy>.Create((a, b) => a.Distance.CompareTo(b.Distance)),
    SortAlgorithm.Grail  // Stable, allocation-free
);

// Available algorithms:
// - Grail (default): Stable, O(n log n), allocation-free
// - Tim: Stable, fast for partially sorted data
// - PatternDefeatingQuickSort: Fast unstable sort
// - Insertion: Best for small or nearly-sorted lists
// - Ghost, Meteor, Power, Ska, Ipn, Smooth, Block, Ips4o, Glide, Flux
```

---

## IEnumerable Extensions

### AsList - Avoid Unnecessary Allocation

```csharp
// ✅ Returns original if already IList, otherwise creates new List
IList<Item> items = someEnumerable.AsList();
```

### Shuffled - Returns New Shuffled Sequence

```csharp
// Returns lazy enumerable in random order
IEnumerable<Card> shuffled = cards.Shuffled();
IEnumerable<Card> shuffled2 = cards.Shuffled(myRandom);
```

**Note**: Allocates. For in-place shuffle, use `list.Shuffle()` instead.

### Infinite - Repeating Sequence

```csharp
// Create infinite repeating sequence
IEnumerable<Color> colors = new[] { Color.red, Color.green, Color.blue }.Infinite();

// Take first 10 from infinite cycle
var first10 = colors.Take(10).ToList();
```

### Ordered - Natural Ordering

```csharp
// Sort by natural IComparable order
IEnumerable<int> sorted = numbers.Ordered();
```

### ToLinkedList

```csharp
LinkedList<Item> linked = items.ToLinkedList();
```

---

## String Extensions

### Case Conversion

```csharp
using WallstopStudios.UnityHelpers.Core.Extension;

string input = "helloWorld";

input.ToCase(StringCase.PascalCase);    // "HelloWorld"
input.ToCase(StringCase.CamelCase);     // "helloWorld"
input.ToCase(StringCase.SnakeCase);     // "hello_world"
input.ToCase(StringCase.KebabCase);     // "hello-world"
input.ToCase(StringCase.TitleCase);     // "Hello World"
input.ToCase(StringCase.UpperCase);     // "HELLOWORLD"
input.ToCase(StringCase.LowerCase);     // "helloworld"
```

### Byte Conversion

```csharp
// String to UTF-8 bytes
byte[] bytes = "Hello".GetBytes();

// Bytes back to string
string text = bytes.GetString();
```

### JSON Serialization

```csharp
// Serialize any object to JSON
string json = myObject.ToJson();
```

### LevenshteinDistance - Fuzzy Matching

```csharp
// Calculate edit distance between strings
int distance = "kitten".LevenshteinDistance("sitting");  // Returns 3
```

**Performance**: O(n\*m), **Allocations**: Uses pooled arrays

### Center - Pad String

```csharp
string centered = "hi".Center(6);  // "  hi  "
```

---

## Color Extensions

### ToHex - Color to Hex String

```csharp
using WallstopStudios.UnityHelpers.Core.Extension;

Color color = new Color(1f, 0.5f, 0f, 1f);

string hexRGBA = color.ToHex();              // "#FF8000FF"
string hexRGB = color.ToHex(includeAlpha: false);  // "#FF8000"
```

**Performance**: O(1), **Allocations**: One string

### GetAverageColor - Sprite Color Analysis

```csharp
// Get average color from sprite
Color avg = sprite.GetAverageColor();

// With specific averaging method
Color avgLAB = sprite.GetAverageColor(ColorAveragingMethod.LAB);      // Perceptually accurate
Color avgHSV = sprite.GetAverageColor(ColorAveragingMethod.HSV);      // Preserves vibrancy
Color avgWeighted = sprite.GetAverageColor(ColorAveragingMethod.Weighted);
Color dominant = sprite.GetAverageColor(ColorAveragingMethod.Dominant);  // Most common color

// From multiple sprites
Color combined = sprites.GetAverageColor();
```

**Warning**: NOT thread-safe, modifies texture import settings.

### GetComplement - Complementary Color

```csharp
// Get complementary color (180° hue rotation)
Color complement = color.GetComplement();

// With randomization for variety
Color varied = color.GetComplement(PRNG.Instance, variance: 0.1f);
```

**Performance**: O(1), **Allocations**: None

---

## Performance Summary

### Zero-Allocation Methods ✅

| Extension                | Type       | Notes                        |
| ------------------------ | ---------- | ---------------------------- |
| `Shuffle()`              | IList      | Fisher-Yates in-place        |
| `GetRandomElement()`     | IList      | O(1)                         |
| `RemoveAtSwapBack()`     | IList      | O(1), unordered              |
| `IndexOf(predicate)`     | IList      | O(n)                         |
| `LastIndexOf(predicate)` | IList      | O(n)                         |
| `Shift()`                | IList      | Three reversals              |
| `Reverse(start, end)`    | IList      | In-place                     |
| `Sort()`                 | IList      | Pooled sorting algorithms    |
| `TryRemove()`            | Dictionary | -                            |
| `GetOrElse()`            | Dictionary | Read-only                    |
| `ToHex()`                | Color      | Allocates result string only |
| `GetComplement()`        | Color      | -                            |

### Allocating Methods ⚠️

| Extension           | Type        | Allocation                |
| ------------------- | ----------- | ------------------------- |
| `GetOrAdd()`        | Dictionary  | Value if created          |
| `Merge()`           | Dictionary  | New dictionary            |
| `Shuffled()`        | IEnumerable | LINQ structures           |
| `AsList()`          | IEnumerable | List if not already IList |
| `ToLinkedList()`    | IEnumerable | New LinkedList            |
| `FindAll()`         | IList       | New List                  |
| `GetAverageColor()` | Sprite      | Pixel array               |

---

## Common Patterns

### Pattern: Safe Dictionary Access

```csharp
// ❌ Verbose null checking
if (!cache.TryGetValue(key, out var value))
{
    value = CreateValue();
    cache[key] = value;
}

// ✅ Concise with GetOrAdd
var value = cache.GetOrAdd(key, () => CreateValue());
```

### Pattern: Fast Collection Processing

```csharp
// ❌ LINQ with allocations
var randomEnemy = enemies.OrderBy(_ => random.Next()).First();

// ✅ Zero-allocation random selection
var randomEnemy = enemies.GetRandomElement();
```

### Pattern: Unordered Fast Removal

```csharp
// ❌ O(n) removal in hot path
for (int i = activeProjectiles.Count - 1; i >= 0; i--)
{
    if (activeProjectiles[i].IsExpired)
    {
        activeProjectiles.RemoveAt(i);  // Shifts all elements!
    }
}

// ✅ O(1) removal when order doesn't matter
for (int i = activeProjectiles.Count - 1; i >= 0; i--)
{
    if (activeProjectiles[i].IsExpired)
    {
        activeProjectiles.RemoveAtSwapBack(i);  // Just swaps with last
    }
}
```

### Pattern: Thread-Safe Dictionary Operations

```csharp
// Works with ConcurrentDictionary automatically
ConcurrentDictionary<int, Player> players = new();

// These use ConcurrentDictionary's native thread-safe methods
players.GetOrAdd(playerId, id => new Player(id));
players.TryRemove(playerId, out var removed);
players.AddOrUpdate(playerId, _ => new Player(playerId), (_, p) => p.Update());
```

---

## Namespace

```csharp
using WallstopStudios.UnityHelpers.Core.Extension;
```

All extension methods are in this namespace and available on their respective types once imported.
