# Inspector Grouping Attributes

**Organize your inspector without writing custom editors.**

Unity Helpers provides powerful grouping attributes that create boxed sections and collapsible foldouts with zero boilerplate. These attributes rival commercial tools like Odin Inspector while offering unique features like auto-inclusion and project-wide color theming.

---

## Table of Contents

- [WGroup & WGroupEnd](#wgroup--wgroupend)
- [Common Features](#common-features)
- [Configuration](#configuration)
- [Best Practices](#best-practices)
- [Examples](#examples)

---

## WGroup & WGroupEnd

Creates boxed inspector sections with optional collapsible headers and automatic field inclusion.

### Basic Usage

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class CharacterStats : MonoBehaviour
{
    // Simple box with 4 fields
    [WGroup("combat", "Combat Stats")]
    public float maxHealth = 100f;
    public float defense = 10f;
    public float attackPower = 25f;
    public float criticalChance = 0.15f;
    [WGroupEnd("combat")]

    public string characterName; // Not in group
}
```

![Image placeholder: WGroup showing boxed combat stats with header]

### Parameters

```csharp
[WGroup(
    string groupName,                    // Required: Unique identifier
    string displayName = null,           // Optional: Header text (defaults to groupName)
    int autoIncludeCount = UseGlobalAutoInclude,  // Auto-include N fields (or use global setting)
    bool collapsible = false,            // Enable foldout behavior
    bool startCollapsed = false,         // Initial collapsed state
    string colorKey = "Default",         // Color palette key
    bool hideHeader = false              // Draw body without header bar
)]
```

> 💡 Use the optional `CollapseBehavior` named argument (or `startCollapsed: true`) to override the project-wide default configured under **Project Settings ▸ Wallstop Studios ▸ Unity Helpers ▸ Start WGroups Collapsed**. Example:
>
> ```csharp
> [WGroup(
>     "advanced",
>     collapsible: true,
>     CollapseBehavior = WGroupAttribute.WGroupCollapseBehavior.ForceExpanded
> )]
> ```

`CollapseBehavior` options:

- `UseProjectSetting` (default) – defers to the Unity Helpers project setting.
- `ForceExpanded` – always starts expanded.
- `ForceCollapsed` – always starts collapsed (equivalent to `startCollapsed: true`).

---

### Auto-Inclusion Modes

#### 1. Explicit Count

```csharp
[WGroup("items", "Inventory", autoIncludeCount: 3)]
public GameObject weapon;
public GameObject armor;
public GameObject accessory;
[WGroupEnd("items")]  // Terminates auto-inclusion

public int gold;  // Not included
```

#### 2. Infinite Auto-Include

```csharp
[WGroup("settings", "Settings", autoIncludeCount: WGroupAttribute.InfiniteAutoInclude)]
public bool enableSound;
public bool enableMusic;
public float volume;
// ... 20 more fields ...
[WGroupEnd("settings")]  // Required to terminate
```

#### 3. Global Default

```csharp
// Uses WGroupAutoIncludeRowCount from ProjectSettings/UnityHelpersSettings.asset (default: 4)
[WGroup("stats", "Stats")]  // autoIncludeCount defaults to UseGlobalAutoInclude
public int strength;
public int intelligence;
public int agility;
public int luck;
[WGroupEnd("stats")]  // Optional if count matches setting
```

---

### Collapsible Groups

```csharp
[WGroup("advanced", "Advanced Options", collapsible: true, startCollapsed: true)]
public float raycastDistance = 100f;
public LayerMask collisionMask;
public bool debugDraw = false;
[WGroupEnd("advanced")]
```

![GIF placeholder: WGroup being collapsed and expanded with smooth animation]

**Animation Settings:**

- Speed controlled by `UnityHelpersSettings.WGroupTweenSpeed` (default: 2.0)
- Enable/disable via `UnityHelpersSettings.WGroupTweenEnabled`

---

### Color Theming

```csharp
[WGroup("health", "Health", colorKey: "Default-Dark")]
public float currentHealth;
public float maxHealth;
[WGroupEnd("health")]

[WGroup("mana", "Mana", colorKey: "Default-Light")]
public float currentMana;
public float maxMana;
[WGroupEnd("mana")]
```

![Image placeholder: Two groups with different color themes (dark blue and light blue)]

**Built-in Color Keys:**

- `"Default"` - Theme-aware (light theme = light colors, dark theme = dark colors)
- `"Default-Dark"` - Dark theme palette
- `"Default-Light"` - Light theme palette
- `"WDefault"` - Legacy vibrant blue
- Custom keys defined in `UnityHelpersSettings.WGroupCustomColors`

**Define Custom Colors:**

1. Open `ProjectSettings/UnityHelpersSettings.asset`
2. Add entry to `WGroupCustomColors` dictionary
3. Set `Header Background`, `Border Color`, `Body Background`

![Image placeholder: UnityHelpersSettings showing custom color palette configuration]

---

### Hiding Headers

```csharp
[WGroup("stealth", "", hideHeader: true, colorKey: "Default-Light")]
public float opacity = 1f;
public bool isVisible = true;
[WGroupEnd("stealth")]
```

![Image placeholder: WGroup with just border and body, no header]

**Use Cases:**

- Visual separation without labels
- Nested grouping styles
- Minimalist inspector layouts

---

### Nested Groups

```csharp
[WGroup("outer", "Character")]
public string characterName;

    [WGroup("inner", "Stats", colorKey: "Default-Light")]
    public int level;
    public int experience;
    [WGroupEnd("inner")]

public string faction;
[WGroupEnd("outer")]
```

![Image placeholder: Nested WGroup showing outer and inner boxes with different colors]

---

### WGroupEnd Variants

#### 1. End Specific Group

```csharp
[WGroupEnd("combat")]  // Closes only the "combat" group
```

#### 2. Include Element in Group

```csharp
[WGroupEnd("stats", includeElement: true)]
public int totalPoints;  // Included in "stats" group, then closes it
```

#### 3. Close All Active Groups

```csharp
[WGroupEnd]  // Closes all active groups (no group name specified)
```

---

## Common Features

### Auto-Include Constants

```csharp
public class WGroupAttribute
{
    public const int UseGlobalAutoInclude = -1;   // Default: use project setting
    public const int InfiniteAutoInclude = -2;    // Include until WGroupEnd
}
```

### Shared Group Names

```csharp
[WGroup("settings", "Game Settings", autoIncludeCount: 2)]
public float masterVolume;
public float musicVolume;
[WGroupEnd("settings")]

// Later in the same script...
[WGroup("settings", autoIncludeCount: 1)]  // Reuses "Game Settings" header
public float sfxVolume;
[WGroupEnd("settings")]
```

![Image placeholder: Two separate WGroup sections with same header styling]

**Note:** Shared groups are visually separate but use the same display name and color settings.

---

## Configuration

### Global Settings

All grouping attributes respect project-wide settings defined in `UnityHelpersSettings`:

**Location:** `ProjectSettings/UnityHelpersSettings.asset`

**Settings:**

- `WGroupAutoIncludeRowCount` (default: 4) - Default fields to auto-include
- `WGroupCustomColors` - Custom color palette dictionary
- `WGroupTweenEnabled` - Enable animations
- `WGroupTweenSpeed` - Animation speed (2-12)

![Image placeholder: UnityHelpersSettings asset showing WGroup configuration section]

---

## Best Practices

### 1. Consistent Naming

```csharp
// ✅ GOOD: Clear, descriptive group names
[WGroup("combat", "Combat Stats")]
[WGroup("movement", "Movement Settings")]
[WGroup("visuals", "Visual Effects")]

// ❌ BAD: Vague or inconsistent
[WGroup("group1", "Stuff")]
[WGroup("misc", "Things")]
```

### 2. Auto-Inclusion Strategy

```csharp
// ✅ GOOD: Explicit count for small groups
[WGroup("position", "Position", autoIncludeCount: 3)]
public Vector3 position;
public Quaternion rotation;
public Vector3 scale;
[WGroupEnd("position")]

// ✅ GOOD: Infinite for dynamic/long lists
[WGroup("inventory", "Items", autoIncludeCount: WGroupAttribute.InfiniteAutoInclude)]
public List<GameObject> weapons;
public List<GameObject> consumables;
// ... many more fields ...
[WGroupEnd("inventory")]

// ❌ BAD: Infinite without WGroupEnd (includes everything below!)
[WGroup("bad", autoIncludeCount: WGroupAttribute.InfiniteAutoInclude)]
public int field1;
public int field2;
// Oops, forgot [WGroupEnd]!
public string unrelatedField;  // Also included!
```

### 3. Color Usage

```csharp
// ✅ GOOD: Use colors to differentiate categories
[WGroup("health", "Health", colorKey: "Default-Dark")]
// ... health fields ...

[WGroup("mana", "Mana", colorKey: "Default-Light")]
// ... mana fields ...

// ❌ BAD: Random colors without meaning
[WGroup("stats", colorKey: "CustomRed")]  // Why red?
```

### 4. Collapsible vs Always-Open

```csharp
// ✅ GOOD: Always-visible for frequently accessed data
[WGroup("core", "Core Stats", collapsible: false)]
public float health;
public float energy;
[WGroupEnd("core")]

// ✅ GOOD: Collapsible for optional/advanced features
[WGroup("advanced", "Advanced", collapsible: true, startCollapsed: true)]
public float debugParameter;
public bool experimentalFeature;
[WGroupEnd("advanced")]

// ❌ BAD: Everything collapsible (hides important data)
[WGroup("important", "Critical Settings", collapsible: true, startCollapsed: true)]
public float maxHealth;  // Why hide this?
[WGroupEnd("important")]
```

---

## Examples

### Example 1: RPG Character Stats

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class RPGCharacter : MonoBehaviour
{
    [WGroup("identity", "Identity", colorKey: "Default-Dark")]
    public string characterName;
    public Sprite portrait;
    public string className;
    [WGroupEnd("identity")]

    [WGroup("attributes", "Base Attributes", collapsible: true)]
    public int strength = 10;
    public int agility = 10;
    public int intelligence = 10;
    public int vitality = 10;
    [WGroupEnd("attributes")]

    [WGroup("combat", "Combat Stats", colorKey: "Default-Light")]
    public float maxHealth = 100f;
    public float attackPower = 25f;
    public float defense = 15f;
    [WGroupEnd("combat")]

    [WGroup("skills", "Skills", collapsible: true, startCollapsed: true)]
    public List<string> learnedSkills;
    public int skillPoints = 0;
    [WGroupEnd("skills")]
}
```

![Image placeholder: RPGCharacter inspector showing all groups with different colors]

---

### Example 2: Weapon Configuration

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class WeaponConfig : MonoBehaviour
{
    [WGroup("basic", "Basic Info", autoIncludeCount: 2)]
    public string weaponName;
    public Sprite icon;
    [WGroupEnd("basic")]

    [WGroup("damage", "Damage", collapsible: true, colorKey: "Default-Dark")]
    public float baseDamage = 10f;
    public float criticalMultiplier = 2f;
    public DamageType damageType;
    [WGroupEnd("damage")]

    [WGroup("effects", "Special Effects", collapsible: true, startCollapsed: true)]
    public ParticleSystem hitEffect;
    public AudioClip hitSound;
    public float effectDuration = 1f;
    [WGroupEnd("effects")]

    [WGroup("advanced", "Advanced Settings", collapsible: true, startCollapsed: true)]
    public float projectileSpeed = 20f;
    public LayerMask targetLayers;
    public bool debugMode = false;
    [WGroupEnd("advanced")]
}
```

![Image placeholder: WeaponConfig inspector with mixed open/closed groups]

---

### Example 3: Dynamic Form with Many Fields

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class LevelSettings : MonoBehaviour
{
    [WGroup("general", "General", autoIncludeCount: 3)]
    public string levelName;
    public Sprite thumbnail;
    public string description;
    [WGroupEnd("general")]

    [WGroup("environment", "Environment", collapsible: true, startCollapsed: true,
            autoIncludeCount: WGroupAttribute.InfiniteAutoInclude)]
    public Color skyColor;
    public Color fogColor;
    public float fogDensity;
    public Light directionalLight;
    public Cubemap skybox;
    public float ambientIntensity;
    public float sunIntensity;
    [WGroupEnd("environment")]

    [WGroup("gameplay", "Gameplay Rules", collapsible: true, startCollapsed: false)]
    public int enemyCount = 10;
    public float difficultyMultiplier = 1f;
    public bool allowRespawns = true;
    [WGroupEnd("gameplay")]

    [WGroup("debug", "Debug Options", collapsible: true, startCollapsed: true)]
    public bool godMode = false;
    public bool unlimitedAmmo = false;
    public bool showHitboxes = false;
    [WGroupEnd("debug")]
}
```

![GIF placeholder: LevelSettings with multiple collapsible groups being toggled]

---

### Example 4: Nested Configuration

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class AIController : MonoBehaviour
{
    [WGroup("outer", "AI Configuration")]

        [WGroup("detection", "Detection", colorKey: "Default-Light")]
        public float sightRange = 10f;
        public float hearingRange = 5f;
        [WGroupEnd("detection")]

        [WGroup("behavior", "Behavior", colorKey: "Default-Dark")]
        public float aggressionLevel = 0.5f;
        public float retreatThreshold = 0.2f;
        [WGroupEnd("behavior")]

    [WGroupEnd("outer")]
}
```

![Image placeholder: Nested groups showing visual hierarchy]

---

## Troubleshooting

### Group Not Appearing

**Problem:** Fields not showing in a group

**Solutions:**

1. Check `autoIncludeCount` - make sure it includes all desired fields
2. Verify `WGroupEnd` placement - fields after `WGroupEnd` won't be included
3. Ensure group names match between `WGroup` and `WGroupEnd`

```csharp
// ❌ WRONG: Count too low
[WGroup("stats", autoIncludeCount: 2)]
public int strength;
public int agility;
public int intelligence;  // Not included! (count is 2)
[WGroupEnd("stats")]

// ✅ CORRECT: Increase count
[WGroup("stats", autoIncludeCount: 3)]
public int strength;
public int agility;
public int intelligence;
[WGroupEnd("stats")]
```

---

### Animation Not Working

**Problem:** Groups don't animate when collapsed/expanded

**Solutions:**

1. Check `UnityHelpersSettings.WGroupTweenEnabled`
2. Ensure `collapsible: true` is set for WGroup
3. Verify animation speed isn't set too low in settings

---

### Color Not Applied

**Problem:** Custom color key doesn't work

**Solutions:**

1. Verify the color key exists in `UnityHelpersSettings.WGroupCustomColors`
2. Check spelling - color keys are case-sensitive
3. Ensure the settings asset is saved

---

## See Also

- **[Inspector Overview](inspector-overview.md)** - Complete inspector features overview
- **[Inspector Buttons](inspector-button.md)** - WButton for method invocation
- **[Inspector Settings](inspector-settings.md)** - Configuration reference
- **[Editor Tools Guide](../editor-tools/editor-tools-guide.md)** - Other editor utilities

---

**Next Steps:**

- Try grouping your existing scripts with `[WGroup]`
- Customize colors in `UnityHelpersSettings.asset`
- Explore `[WButton]` to add method buttons to your groups
