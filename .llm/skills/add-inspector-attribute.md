# Skill: Add Inspector Attribute

**Trigger**: When adding or configuring Unity Helpers inspector attributes to improve editor UX.

---

## Available Attributes

| Attribute                          | Purpose                                 |
| ---------------------------------- | --------------------------------------- |
| `[WGroup]` / `[WGroupEnd]`         | Boxed sections with collapsible headers |
| `[WButton]`                        | Method buttons with async support       |
| `[WShowIf]`                        | Conditional visibility                  |
| `[WEnumToggleButtons]`             | Flag enums as toggle grids              |
| `[WInLineEditor]`                  | Inline nested editor                    |
| `[WValueDropDown]`                 | Value dropdown selection                |
| `[WSerializableCollectionFoldout]` | Foldout for collections                 |
| `[WReadOnly]`                      | Read-only display                       |
| `[WNotNull]`                       | Required field validation               |
| `[ValidateAssignment]`             | Runtime assignment validation           |
| `[StringInList]`                   | Dropdown from string list               |
| `[IntDropDown]`                    | Integer dropdown                        |
| `[EnumDisplayName]`                | Custom enum display names               |

---

## Grouping: `[WGroup]` / `[WGroupEnd]`

Create boxed, collapsible sections:

```csharp
[WGroup("Movement Settings", WGroupColor.Blue)]
[SerializeField] private float moveSpeed = 5f;
[SerializeField] private float jumpHeight = 2f;
[SerializeField] private float gravity = -9.81f;
[WGroupEnd]

[WGroup("Combat Settings", WGroupColor.Red)]
[SerializeField] private int damage = 10;
[SerializeField] private float attackRange = 2f;
[WGroupEnd]
```

### Group Colors

`WGroupColor.Blue`, `WGroupColor.Red`, `WGroupColor.Green`, `WGroupColor.Yellow`, `WGroupColor.Purple`, `WGroupColor.Orange`, `WGroupColor.Gray`

---

## Buttons: `[WButton]`

Add clickable method buttons:

```csharp
[WButton("Reset to Defaults")]
private void ResetDefaults()
{
    moveSpeed = 5f;
    jumpHeight = 2f;
}

[WButton("Spawn Enemy")]
private void SpawnEnemy()
{
    // Spawning logic
}

// Async support with cancellation
[WButton("Long Operation")]
private async Task LongOperation(CancellationToken token)
{
    await Task.Delay(1000, token);
}
```

---

## Conditional Display: `[WShowIf]`

Show/hide fields based on conditions:

```csharp
[SerializeField] private bool useCustomSettings;

[WShowIf(nameof(useCustomSettings))]
[SerializeField] private float customValue;

// Comparison operators
[WShowIf(nameof(healthPercent), WShowIfOperator.LessThan, 0.5f)]
[SerializeField] private GameObject lowHealthWarning;

// Multiple conditions
[WShowIf(nameof(isEnabled))]
[WShowIf(nameof(level), WShowIfOperator.GreaterOrEqual, 5)]
[SerializeField] private string advancedOption;
```

### Operators

`Equal`, `NotEqual`, `GreaterThan`, `LessThan`, `GreaterOrEqual`, `LessOrEqual`, `And`, `Or`, `Not`

---

## Enum Toggle Buttons: `[WEnumToggleButtons]`

Display flag enums as visual toggle grids:

```csharp
[Flags]
public enum DamageTypes
{
    None = 0,
    Physical = 1,
    Fire = 2,
    Ice = 4,
    Lightning = 8,
}

[WEnumToggleButtons]
[SerializeField] private DamageTypes resistances;
```

---

## Inline Editor: `[WInLineEditor]`

Edit referenced ScriptableObjects inline:

```csharp
[WInLineEditor]
[SerializeField] private WeaponData weaponData;

[WInLineEditor]
[SerializeField] private List<BuffEffect> buffs;
```

---

## Dropdowns: `[WValueDropDown]` / `[StringInList]` / `[IntDropDown]`

### Value Dropdown

```csharp
[WValueDropDown(nameof(GetAvailableOptions))]
[SerializeField] private string selectedOption;

private IEnumerable<string> GetAvailableOptions()
{
    return new[] { "Option A", "Option B", "Option C" };
}
```

### String in List

```csharp
[StringInList("Easy", "Medium", "Hard")]
[SerializeField] private string difficulty;

// Or from method
[StringInList(nameof(GetDifficultyOptions))]
[SerializeField] private string difficulty;
```

### Int Dropdown

```csharp
[IntDropDown(1, 5, 10, 25, 50, 100)]
[SerializeField] private int spawnCount;
```

---

## Validation: `[WNotNull]` / `[ValidateAssignment]`

### Required Fields

```csharp
[WNotNull]
[SerializeField] private Transform spawnPoint;  // Shows error if null

[WNotNull("Player reference is required!")]
[SerializeField] private PlayerController player;
```

### Runtime Validation

```csharp
[ValidateAssignment]
[SerializeField] private GameObject prefab;  // Logs warning if invalid assignment
```

---

## Read-Only Display: `[WReadOnly]`

```csharp
[WReadOnly]
[SerializeField] private int currentHealth;

[WReadOnly]
[SerializeField] private string generatedId;
```

---

## Collection Foldout: `[WSerializableCollectionFoldout]`

```csharp
[WSerializableCollectionFoldout]
[SerializeField] private List<Item> inventory;

[WSerializableCollectionFoldout("Equipped Items")]
[SerializeField] private SerializableDictionary<EquipSlot, Item> equipped;
```

---

## Custom Enum Names: `[EnumDisplayName]`

```csharp
public enum ItemRarity
{
    [EnumDisplayName("Common (Gray)")]
    Common = 1,

    [EnumDisplayName("Uncommon (Green)")]
    Uncommon = 2,

    [EnumDisplayName("Rare (Blue)")]
    Rare = 3,

    [EnumDisplayName("Legendary (Orange)")]
    Legendary = 4,
}
```

---

## Complete Example

```csharp
public class EnemyController : MonoBehaviour
{
    [WGroup("Identity", WGroupColor.Blue)]
    [WNotNull]
    [SerializeField] private string enemyId;

    [WReadOnly]
    [SerializeField] private string displayName;
    [WGroupEnd]

    [WGroup("Stats", WGroupColor.Green)]
    [SerializeField] private int maxHealth = 100;

    [WReadOnly]
    [SerializeField] private int currentHealth;

    [WEnumToggleButtons]
    [SerializeField] private DamageTypes weaknesses;
    [WGroupEnd]

    [WGroup("Behavior", WGroupColor.Purple)]
    [SerializeField] private bool isAggressive;

    [WShowIf(nameof(isAggressive))]
    [SerializeField] private float aggroRange = 10f;

    [WShowIf(nameof(isAggressive))]
    [WInLineEditor]
    [SerializeField] private AttackPattern attackPattern;
    [WGroupEnd]

    [WButton("Reset Health")]
    private void ResetHealth()
    {
        currentHealth = maxHealth;
    }

    [WButton("Test Attack")]
    private void TestAttack()
    {
        Debug.Log($"{displayName} attacks!");
    }
}
```
