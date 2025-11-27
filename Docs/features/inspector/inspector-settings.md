# Inspector Settings Reference

**Centralized configuration for all inspector features.**

The `UnityHelpersSettings` asset provides project-wide configuration for pagination, colors, animations, and history capacity across all inspector attributes and custom drawers. Configure once, apply everywhere.

---

## Table of Contents

- [Accessing Settings](#accessing-settings)
- [Pagination Settings](#pagination-settings)
- [WButton Settings](#wbutton-settings)
- [WGroup Settings](#wgroup-settings)
- [Color Palettes](#color-palettes)
- [WEnumToggleButtons Settings](#wenumtogglebuttons-settings)
- [Creating the Settings Asset](#creating-the-settings-asset)

---

## Accessing Settings

**Location:** `ProjectSettings/UnityHelpersSettings.asset`

**Access in Unity:**

1. Open Project Settings window (`Edit > Project Settings`)
2. Scroll to "Unity Helpers" section (if available)
3. Or navigate to `ProjectSettings/UnityHelpersSettings.asset` directly

**Note:** The asset is created automatically on first use. If missing, any inspector feature will generate it.

![Image placeholder: Project Settings window showing Unity Helpers settings]

---

## Pagination Settings

Controls how many items are shown per page in various UI elements.

### StringInListPageSize

**Default:** 25
**Range:** 5 - 500
**Applies to:** `[StringInList]` attribute

**Description:** Number of string options shown per page in the dropdown.

**Usage:**

```csharp
[StringInList(typeof(SceneLibrary), nameof(SceneLibrary.GetAllSceneNames))]
public string sceneName;  // Uses StringInListPageSize
```

---

### SerializableSetPageSize

**Default:** 15
**Range:** 5 - 500
**Applies to:** `SerializableHashSet<T>`, `SerializableSortedSet<T>`

**Description:** Number of set elements shown per page in the inspector.

**Usage:**

```csharp
public SerializableHashSet<string> items;  // Uses SerializableSetPageSize
```

---

### SerializableDictionaryPageSize

**Default:** 15
**Range:** 5 - 250
**Applies to:** `SerializableDictionary<TKey, TValue>`, `SerializableSortedDictionary<TKey, TValue>`

**Description:** Number of dictionary entries shown per page in the inspector.

**Usage:**

```csharp
public SerializableDictionary<string, GameObject> prefabs;  // Uses SerializableDictionaryPageSize
```

---

### SerializableSetFoldoutTweenEnabled

**Default:** On  
**Applies to:** `SerializableHashSet<T>`

**Description:** Controls whether the manual entry foldout in SerializableSet inspectors animates when expanding or collapsing.

---

### SerializableSetFoldoutSpeed

**Default:** 2  
**Range:** 2 - 12  
**Applies to:** `SerializableHashSet<T>`

**Description:** Animation speed for the SerializableSet manual entry foldout when `SerializableSetFoldoutTweenEnabled` is enabled.

---

### SerializableSortedSetFoldoutTweenEnabled

**Default:** On  
**Applies to:** `SerializableSortedSet<T>`

**Description:** Controls whether the manual entry foldout in SerializableSortedSet inspectors animates when expanding or collapsing.

---

### SerializableSortedSetFoldoutSpeed

**Default:** 2  
**Range:** 2 - 12  
**Applies to:** `SerializableSortedSet<T>`

**Description:** Animation speed for the SerializableSortedSet manual entry foldout when `SerializableSortedSetFoldoutTweenEnabled` is enabled.

---

### EnumToggleButtonsPageSize

**Default:** 15
**Range:** 5 - 50
**Applies to:** `[WEnumToggleButtons]` attribute (when pagination enabled)

**Description:** Number of toggle buttons shown per page for enums with many values.

**Usage:**

```csharp
[WEnumToggleButtons(enablePagination: true)]
public ManyOptionsEnum options;  // Uses EnumToggleButtonsPageSize
```

---

### WButtonPageSize

**Default:** 6
**Range:** 1 - 20
**Applies to:** `[WButton]` attribute (grouped by draw order)

**Description:** Number of button actions shown per page.

**Usage:**

```csharp
[WButton("Action 1", drawOrder: 0)]
private void Action1() { }

// ... 10 more buttons with drawOrder: 0 ...
// Pagination kicks in after WButtonPageSize buttons
```

---

## WButton Settings

### WButtonHistorySize

**Default:** 5
**Range:** 1 - 10
**Applies to:** `[WButton]` methods with return values

**Description:** Number of recent results to keep per method per target.

**Usage:**

```csharp
[WButton("Roll Dice")]  // Uses WButtonHistorySize
private int RollDice() => Random.Range(1, 7);

[WButton("Custom History", historyCapacity: 20)]  // Overrides global setting
private int CustomHistory() => Random.Range(1, 100);
```

---

### WButtonPlacement

**Default:** Bottom
**Options:** Top, Bottom
**Applies to:** `[WButton]` buttons with default `drawOrder`

**Description:** Default placement of buttons in the inspector.

- **Top:** Buttons appear before default inspector fields
- **Bottom:** Buttons appear after default inspector fields

**Note:** `drawOrder` parameter overrides this setting:

- `drawOrder >= -1` → Top (above inspector)
- `drawOrder >= 0` → Bottom (below inspector)

---

### WButtonFoldoutBehavior

**Default:** StartExpanded
**Options:** Always, StartExpanded, StartCollapsed
**Applies to:** `[WButton]` grouped buttons

**Description:** Controls foldout behavior for button groups.

- **Always:** Groups always show foldout triangles
- **StartExpanded:** Groups start open (can be collapsed)
- **StartCollapsed:** Groups start closed (can be expanded)

---

### WButtonFoldoutTweenEnabled

**Default:** true
**Applies to:** `[WButton]` grouped buttons

**Description:** Enable smooth animation when expanding/collapsing button groups.

---

### WButtonFoldoutSpeed

**Default:** 2.0
**Range:** 2.0 - 12.0
**Applies to:** `[WButton]` grouped buttons (when tween enabled)

**Description:** Animation speed for button group fold/unfold.

- Lower values = slower animation
- Higher values = faster animation

---

## WGroup Settings

### WGroupAutoIncludeRowCount

**Default:** 4
**Range:** 0 - 32
**Applies to:** `[WGroup]` attributes using `UseGlobalAutoInclude`

**Description:** Default number of fields to auto-include in a WGroup.

**Usage:**

```csharp
// Uses WGroupAutoIncludeRowCount (default: 4)
[WGroup("stats", "Stats")]
public int strength;
public int agility;
public int intelligence;
public int luck;
[WGroupEnd("stats")]

// Explicit override
[WGroup("combat", "Combat", autoIncludeCount: 2)]
public float health;
public float mana;
[WGroupEnd("combat")]
```

---

### WGroupTweenEnabled

**Default:** true
**Applies to:** `[WGroup]` with `collapsible: true`

**Description:** Enable smooth animation when expanding/collapsing groups.

---

### WGroupTweenSpeed

**Default:** 2.0
**Range:** 2.0 - 12.0
**Applies to:** `[WGroup]` with `collapsible: true` (when tween enabled)

**Description:** Animation speed for group fold/unfold.

---

## Color Palettes

Palette keys keep WButton, WGroup, and WEnumToggleButtons visuals consistent across the project. Open the **Color Palettes** foldout inside `UnityHelpersSettings` to add or edit entries. Each key is matched at draw time against the `ColorKey`/`priority` parameter on the corresponding attribute; unknown keys fall back to theme-aware defaults.

### WButtonCustomColors

**Applies to:** `[WButton]` via the `priority` (alias `ColorKey`) parameter  
**Reserved keys:** `Default`, `Default-Light`, `Default-Dark`, `WDefault` (legacy)  
**Description:** Each entry stores a button color and a readable text color. Reserved keys auto-sync to the current editor skin and cannot be deleted. Custom keys are ideal for highlighting dangerous or primary actions across multiple inspectors.  
**Usage:**

1. Expand **Color Palettes → WButton Custom Colors**.
2. Add an entry (e.g., `Highlight`) and pick button/text colors.
3. Reference the key from your button:

```csharp
[WButton("Submit", priority: "Highlight")]
private void Submit() { }
```

### WGroupCustomColors

**Applies to:** `[WGroup]` via the `colorKey` constructor argument  
**Reserved keys:** `Default`, `Default-Light`, `Default-Dark`, `WDefault`  
**Description:** Entries store background and header text colors for boxed groups. Reserved keys map to light/dark editor themes, while custom keys let you brand different sections (e.g., `Stats`, `Networking`, `Danger`). Unknown keys are created automatically but editing them here ensures consistent palettes across scenes.  
**Usage:** Add a key under **WGroup Custom Colors** and reference it: `[WGroup("stats", colorKey: "StatsAccent")]`.

### WEnumToggleButtonsCustomColors

**Applies to:** `[WEnumToggleButtons]` via the `ColorKey` property  
**Reserved keys:** `Default`, `Default-Light`, `Default-Dark`  
**Description:** Each entry defines four colors (selected background/text and inactive background/text). Use this dictionary to align enum toggle palettes with the rest of your UI or to clearly separate different tool contexts.  
**Usage:** Add a key under **WEnumToggleButtons Custom Colors**, then assign it per field:

```csharp
[WEnumToggleButtons(ColorKey = "Difficulty")]
public DifficultyLevel difficulty;
```

---

## WEnumToggleButtons Settings

### EnumToggleButtonsPageSize

See [Pagination Settings](#enumtogglebuttonspagesize).

---

## Creating the Settings Asset

The `UnityHelpersSettings` asset is automatically created on first use, but you can create it manually:

### Method 1: Automatic Creation

1. Use any inspector attribute (WGroup, WButton, etc.)
2. Open the inspector
3. Asset is created at `ProjectSettings/UnityHelpersSettings.asset`

### Method 2: Force Creation

1. Open any script with an inspector attribute
2. Select the GameObject/asset in the inspector
3. Settings asset is generated automatically

### Method 3: Via API (Editor Script)

```csharp
#if UNITY_EDITOR
using WallstopStudios.UnityHelpers.Editor.Settings;

UnityHelpersSettings settings = UnityHelpersSettings.Instance;
// Settings asset is now created
#endif
```

---

## Example Configurations

### High-Density UI (More items per page)

```text
StringInListPageSize: 50
SerializableSetPageSize: 30
EnumToggleButtonsPageSize: 25
WButtonPageSize: 12
```

**Use case:** Large monitors, prefer scrolling over pagination

---

### Low-Density UI (Fewer items per page)

```text
StringInListPageSize: 10
SerializableSetPageSize: 5
EnumToggleButtonsPageSize: 8
WButtonPageSize: 4
```

**Use case:** Laptop screens, prefer focused views

---

### Performance-Focused (Disable animations)

```text
WButtonFoldoutTweenEnabled: false
WGroupTweenEnabled: false
```

**Use case:** Slower machines, prefer instant feedback

---

### Smooth Animations (Fast tweens)

```text
WButtonFoldoutSpeed: 8.0
WGroupTweenSpeed: 8.0
```

**Use case:** Snappy UI feel

---

### Extensive History (More button results)

```text
WButtonHistorySize: 10
```

**Use case:** Heavy testing/debugging workflows

---

## Troubleshooting

### Settings Asset Missing

**Problem:** Can't find `UnityHelpersSettings.asset`

**Solution:**

1. Use any inspector attribute in a script
2. Select object in inspector
3. Asset is created automatically
4. Check `ProjectSettings/UnityHelpersSettings.asset`

---

### Changes Not Applied

**Problem:** Modified settings but inspector doesn't update

**Solution:**

1. Ensure settings asset is saved (`Ctrl+S` or `Cmd+S`)
2. Refresh inspector (click away and back)
3. Reimport scripts if needed (`Assets > Reimport All`)

---

### Color Palette Not Working

**Problem:** Custom color key doesn't apply

**Solution:**

1. Check color key spelling (case-sensitive)
2. Verify entry exists in appropriate dictionary (WButtonCustomColors, WGroupCustomColors, etc.)
3. Ensure colors are set (not transparent/default)
4. Save settings asset

---

## See Also

- **[Inspector Overview](inspector-overview.md)** - Complete inspector features overview
- **[Inspector Grouping Attributes](inspector-grouping-attributes.md)** - WGroup layouts
- **[Inspector Buttons](inspector-button.md)** - WButton
- **[Inspector Selection Attributes](inspector-selection-attributes.md)** - WEnumToggleButtons

---

**Next Steps:**

- Customize pagination sizes for your workflow
- Create custom color palettes for project theming
- Adjust animation speeds to your preference
- Configure default button history capacity
