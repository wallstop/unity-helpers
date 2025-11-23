# Inspector & Serialization Features Overview

**Stop writing boilerplate. Start designing in the inspector.**

Unity Helpers includes a powerful suite of inspector attributes and serialization types that transform how you author components and data. These features eliminate repetitive code, provide designer-friendly workflows, and make your inspector experiences rival commercial tools like Odin Inspector.

---

## Why Use These Features?

**Time Savings:**

- **Grouping & Organization**: Replace 50+ lines of custom editor code with a single `[WGroup]` attribute
- **Method Buttons**: Expose test methods in the inspector without writing custom editors
- **Conditional Display**: Show/hide fields based on values without PropertyDrawer boilerplate
- **Selection Controls**: Turn enums into toggle buttons, primitives into dropdowns - all declaratively
- **Serialization**: Store GUIDs, dictionaries, sets, and types with built-in Unity support

**Professional Quality:**

- Designer-friendly interfaces reduce programmer bottlenecks
- Project-wide settings ensure consistent styling and behavior
- Fully customizable color palettes for theming
- Pagination, animation, and polish built-in

---

## Feature Categories

### 1. Layout & Organization

Control how fields are grouped and organized in the inspector:

- **[WGroup & WGroupEnd](inspector-grouping-attributes.md#wgroup--wgroupend)** - Boxed sections with optional collapse, color themes, auto-inclusion
- **[WFoldoutGroup & WFoldoutGroupEnd](inspector-grouping-attributes.md#wfoldoutgroup--wfoldoutgroupend)** - Collapsible foldout sections for long forms

![Image placeholder: WGroup example showing boxed fields with colored header]
![Image placeholder: WFoldoutGroup collapsed and expanded states]
![Image placeholder: Color palette customization in settings]

**[→ Full Guide: Inspector Grouping Attributes](inspector-grouping-attributes.md)**

---

### 2. Method Invocation

Expose methods as clickable buttons in the inspector:

- **[WButton](inspector-button.md)** - One-click method execution with result history, async support, custom styling, grouping

![Image placeholder: WButton examples showing void, async, and history]
![Image placeholder: WButton grouped by draw order with pagination]
![GIF placeholder: WButton executing async method with spinner and result display]

**[→ Full Guide: Inspector Buttons](inspector-button.md)**

---

### 3. Conditional Display

Show or hide fields based on runtime values:

- **[WShowIf](inspector-conditional-display.md)** - Visibility rules with comparison operators (Equal, GreaterThan, IsNull, etc.), inversion, stacking

![Image placeholder: WShowIf showing field appearing based on bool toggle]
![Image placeholder: WShowIf with numeric comparison]
![GIF placeholder: WShowIf dynamic visibility based on enum selection]

**[→ Full Guide: Inspector Conditional Display](inspector-conditional-display.md)**

---

### 4. Selection & Dropdowns

Provide designer-friendly selection controls:

- **[WEnumToggleButtons](inspector-selection-attributes.md#wenumtogglebuttons)** - Visual toggle buttons for enums and flag enums
- **[WValueDropDown](inspector-selection-attributes.md#wvaluedropdown)** - Generic dropdown for any type
- **[IntDropdown](inspector-selection-attributes.md#intdropdown)** - Integer selection from predefined values
- **[StringInList](inspector-selection-attributes.md#stringinlist)** - String selection with search and pagination

![Image placeholder: WEnumToggleButtons for flag enum with Select All/None]
![Image placeholder: WValueDropDown showing type-safe dropdown]
![Image placeholder: StringInList with search and pagination]

**[→ Full Guide: Inspector Selection Attributes](inspector-selection-attributes.md)**

---

### 5. Serialization Types

Unity-friendly wrappers for complex data:

- **[WGuid](../serialization/serialization-types.md#wguid)** - Immutable GUID using two longs (faster than System.Guid for Unity)
- **[SerializableDictionary](../serialization/serialization-types.md#serializabledictionary)** - Key/value pairs with custom drawer
- **[SerializableSet](../serialization/serialization-types.md#serializableset)** - HashSet and SortedSet with duplicate detection, pagination, reordering
- **[SerializableType](../serialization/serialization-types.md#serializabletype)** - Type references that survive refactoring
- **[SerializableNullable](../serialization/serialization-types.md#serializablenullable)** - Nullable value types

![Image placeholder: WGuid drawer with Generate button]
![Image placeholder: SerializableDictionary with key/value editor]
![Image placeholder: SerializableSet with pagination and duplicate highlighting]
![Image placeholder: SerializableType with search and type browser]

**[→ Full Guide: Serialization Types](../serialization/serialization-types.md)**

---

### 6. Project Settings

Centralized configuration for all inspector features:

- **[UnityHelpersSettings](inspector-settings.md)** - Global settings for pagination, colors, animations, history

**Location:** `ProjectSettings/UnityHelpersSettings.asset`

**Settings:**

- Pagination sizes (buttons, sets, dropdowns)
- Button placement and history capacity
- Color palettes for themes (light/dark/custom)
- Animation speeds for foldouts and groups
- Auto-include defaults

![Image placeholder: UnityHelpersSettings inspector showing all configuration options]
![GIF placeholder: Changing color palette and seeing instant update in inspector]

**[→ Full Guide: Inspector Settings](inspector-settings.md)**

---

## Quick Start Examples

### Example 1: Organized Inspector with Groups

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class CharacterStats : MonoBehaviour
{
    [WGroup("Combat", "Combat Stats", colorKey: "Default-Dark", collapsible: true)]
    public float maxHealth = 100f;
    public float defense = 10f;
    public float attackPower = 25f;
    [WGroupEnd("Combat")]

    [WFoldoutGroup("Visual", "Visual Settings", startCollapsed: true)]
    public Color primaryColor = Color.white;
    public Material skinMaterial;
    public Sprite portrait;
    [WFoldoutGroupEnd("Visual")]

    [WButton("Test Damage", colorKey: "Default-Dark")]
    private void TestTakeDamage()
    {
        Debug.Log($"Took 10 damage! Health: {maxHealth - 10}");
    }
}
```

![Image placeholder: Result of above code showing grouped inspector]

---

### Example 2: Dynamic UI with Conditional Fields

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class WeaponConfig : MonoBehaviour
{
    public enum WeaponType { Melee, Ranged, Magic }

    public WeaponType weaponType;

    [WShowIf(nameof(weaponType), WShowIfComparison.Equal, WeaponType.Melee)]
    public float meleeRange = 2f;

    [WShowIf(nameof(weaponType), WShowIfComparison.Equal, WeaponType.Ranged)]
    public int ammoCapacity = 30;

    [WShowIf(nameof(weaponType), WShowIfComparison.Equal, WeaponType.Magic)]
    public float manaCost = 15f;
}
```

![GIF placeholder: Changing weapon type and fields appearing/disappearing]

---

### Example 3: Flag Enum as Toggle Buttons

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class EntityPermissions : MonoBehaviour
{
    [System.Flags]
    public enum Permissions
    {
        None = 0,
        Move = 1 << 0,
        Attack = 1 << 1,
        UseItems = 1 << 2,
        CastSpells = 1 << 3,
        Interact = 1 << 4,
    }

    [WEnumToggleButtons(showSelectAll: true, showSelectNone: true, buttonsPerRow: 3)]
    public Permissions currentPermissions = Permissions.Move | Permissions.Attack;
}
```

![Image placeholder: Permission flags as toggle button grid]

---

### Example 4: Serializable Collections

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

public class GameDatabase : MonoBehaviour
{
    // Dictionary with custom drawer
    public SerializableDictionary<string, GameObject> prefabRegistry;

    // HashSet with duplicate detection
    public SerializableHashSet<string> uniqueItemIds;

    // SortedSet with automatic ordering
    public SerializableSortedSet<int> scoreThresholds;

    // Type reference that survives refactoring
    [StringInList(typeof(TypeHelper), nameof(TypeHelper.GetAllMonoBehaviours))]
    public SerializableType behaviorType;

    // Nullable primitive
    public SerializableNullable<float> optionalBonus;

    // GUID generation
    public WGuid entityId = WGuid.NewGuid();
}
```

![Image placeholder: SerializableDictionary editor with add/remove buttons]
![Image placeholder: SerializableSet with pagination]

---

## Feature Comparison

| Feature                 | Unity Default | Odin Inspector        | Unity Helpers                 |
| ----------------------- | ------------- | --------------------- | ----------------------------- |
| **Grouping/Boxes**      | Custom Editor | `[BoxGroup]`          | `[WGroup]`                    |
| **Foldouts**            | Custom Editor | `[FoldoutGroup]`      | `[WFoldoutGroup]`             |
| **Method Buttons**      | Custom Editor | `[Button]`            | `[WButton]`                   |
| **Conditional Display** | Custom Drawer | `[ShowIf]`            | `[WShowIf]`                   |
| **Enum Toggles**        | Custom Drawer | `[EnumToggleButtons]` | `[WEnumToggleButtons]`        |
| **Dictionaries**        | Not Supported | `[ShowInInspector]`   | `SerializableDictionary<K,V>` |
| **Sets**                | Not Supported | Custom                | `SerializableHashSet<T>`      |
| **Type References**     | Not Supported | Custom                | `SerializableType`            |
| **Nullable Values**     | Not Supported | Custom                | `SerializableNullable<T>`     |
| **Color Themes**        | Not Supported | Built-in              | Project Settings              |
| **Cost**                | Free          | $55-$95               | Free (MIT)                    |

---

## Design Philosophy

**Declarative Over Imperative:**

- Attributes describe _what_, not _how_
- No custom PropertyDrawers for common patterns
- Configuration over code

**Designer-Friendly:**

- Visual controls for visual people
- Reduce programmer bottlenecks
- Iteration without recompiling

**Performance-Conscious:**

- Cached reflection delegates
- Pooled buffers for UI rendering
- Minimal GC allocations

**Project-Consistent:**

- Centralized settings asset
- Color palettes for theming
- Predictable behavior across all inspectors

---

## Getting Started

1. **Install Unity Helpers** - See [Installation Guide](../../../README.md#installation)

2. **Explore Examples** - Check the guides linked above

3. **Configure Settings** - Open `ProjectSettings/UnityHelpersSettings.asset` to customize pagination, colors, and animations

4. **Add Attributes** - Start with `[WGroup]` and `[WButton]` for immediate impact

5. **Use Serialization Types** - Replace custom wrappers with `SerializableDictionary`, `SerializableSet`, etc.

---

## Detailed Documentation

### Inspector Attributes

- **[Inspector Grouping Attributes](inspector-grouping-attributes.md)** - WGroup, WFoldoutGroup, layout control
- **[Inspector Buttons](inspector-button.md)** - WButton for method invocation
- **[Inspector Conditional Display](inspector-conditional-display.md)** - WShowIf for dynamic visibility
- **[Inspector Selection Attributes](inspector-selection-attributes.md)** - WEnumToggleButtons, dropdowns

### Serialization

- **[Serialization Types](../serialization/serialization-types.md)** - WGuid, SerializableDictionary, SerializableSet, SerializableType, SerializableNullable

### Configuration

- **[Inspector Settings](inspector-settings.md)** - UnityHelpersSettings asset reference

---

## See Also

- **[Editor Tools Guide](../editor-tools/editor-tools-guide.md)** - 20+ automation tools for sprites, animations, validation
- **[Relational Components](../relational-components/relational-components.md)** - Auto-wire components with attributes
- **[Effects System](../effects/effects-system.md)** - Data-driven buffs/debuffs
- **[Main Documentation](../../../README.md)** - Complete feature list

---

**Next Steps:**

Choose a guide based on what you want to learn first:

- Want organized inspectors? → [Inspector Grouping Attributes](inspector-grouping-attributes.md)
- Want method buttons? → [Inspector Buttons](inspector-button.md)
- Want conditional fields? → [Inspector Conditional Display](inspector-conditional-display.md)
- Want better selection controls? → [Inspector Selection Attributes](inspector-selection-attributes.md)
- Want to serialize complex data? → [Serialization Types](../serialization/serialization-types.md)
