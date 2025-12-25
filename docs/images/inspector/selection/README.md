# Selection Attribute Inspector Images

This directory contains images for the [Inspector Selection Attributes](../../../features/inspector/inspector-selection-attributes.md) documentation. This includes images for:

- **WEnumToggleButtons** - Visual toggle buttons for enums and flag enums
- **WValueDropDown** - Generic dropdown for any type with fixed values or provider methods
- **IntDropdown** - Integer selection from predefined values
- **StringInList** - String selection with search and pagination

---

## Required Images

### WEnumToggleButtons Images

| Filename                                | Description                                                     |
| --------------------------------------- | --------------------------------------------------------------- |
| `wenum-toggle-buttons-basic.png`        | Basic flag enum permissions rendered as toggle buttons          |
| `wenum-toggle-buttons-flags-select.png` | Flag enum with Select All and Select None quick action buttons  |
| `wenum-toggle-buttons-radio.png`        | Radio-style toggle buttons for standard enums (only one active) |
| `wenum-toggle-buttons-pagination.gif`   | Paginated toggle buttons with navigation controls               |
| `wenum-toggle-buttons-layouts.png`      | Three different layouts showing 1, 2, and auto columns          |
| `wenum-toggle-buttons-themes.png`       | Toggle buttons with different color themes (dark/light)         |

### WValueDropDown Images

| Filename                          | Description                                                              |
| --------------------------------- | ------------------------------------------------------------------------ |
| `wvalue-dropdown-basic.png`       | Basic dropdown showing predefined primitive values (integers, floats)    |
| `wvalue-dropdown-strings.png`     | Dropdown showing string options (e.g., difficulty levels)                |
| `wvalue-dropdown-provider.png`    | Dropdown populated from a provider method (e.g., PowerUpDefinition)      |
| `wvalue-dropdown-custom-type.png` | Dropdown showing custom type selection (e.g., Preset class)              |
| `wvalue-dropdown-primitives.png`  | Multiple dropdowns showing all primitive type support (bool, char, etc.) |
| `wvalue-dropdown-instance.png`    | Dropdown using instance provider method for context-aware options        |

### IntDropdown Images

| Filename                    | Description                                                 |
| --------------------------- | ----------------------------------------------------------- |
| `int-dropdown-basic.png`    | Integer dropdown showing predefined values (0, 30, 60, 120) |
| `int-dropdown-fallback.png` | IntDropdown falling back to IntField when value not in list |

### StringInList Images

| Filename                        | Description                                                           |
| ------------------------------- | --------------------------------------------------------------------- |
| `string-in-list-basic.png`      | String dropdown showing popup with search bar and predefined results  |
| `string-in-list-search.gif`     | Animated search filtering in StringInList popup                       |
| `string-in-list-pagination.gif` | Paginated StringInList with navigation controls                       |
| `string-in-list-instance.png`   | StringInList using instance provider method for context-aware options |

---

## Image Specifications

- **Screenshots (PNG)**: 800-1200px wide, PNG format with transparency where appropriate
- **Animated GIFs**: 600-900px wide, optimized for file size (under 2MB)
- **Theme**: Use Unity's default dark theme for consistency with other documentation

---

## Code Examples for Screenshots

### wenum-toggle-buttons-basic.png

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
    }

    [WEnumToggleButtons]
    public Permissions currentPermissions = Permissions.Move | Permissions.Attack;
}
```

### wvalue-dropdown-basic.png

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class StaminaConfig : MonoBehaviour
{
    [WValueDropDown(0, 25, 50, 100)]
    public int staminaThreshold = 50;

    [WValueDropDown(0.5f, 1.0f, 1.5f, 2.0f)]
    public float damageMultiplier = 1.0f;
}
```

### wvalue-dropdown-strings.png

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class GameSettings : MonoBehaviour
{
    [WValueDropDown("Easy", "Normal", "Hard", "Insane")]
    public string difficulty = "Normal";
}
```

### wvalue-dropdown-provider.png

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;
using System.Collections.Generic;

public class PowerUpConfig : MonoBehaviour
{
    [WValueDropDown(typeof(PowerUpLibrary), nameof(PowerUpLibrary.GetAvailablePowerUps))]
    public PowerUpDefinition selectedPowerUp;
}

public static class PowerUpLibrary
{
    public static IEnumerable<PowerUpDefinition> GetAvailablePowerUps()
    {
        return Resources.LoadAll<PowerUpDefinition>("PowerUps");
    }
}
```

### wvalue-dropdown-custom-type.png

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;
using System.Collections.Generic;

[System.Serializable]
public class Preset
{
    public string name;
    public float value;

    public override string ToString() => name;
}

public class Config : MonoBehaviour
{
    [WValueDropDown(typeof(Config), nameof(GetPresets))]
    public Preset selectedPreset;

    public static IEnumerable<Preset> GetPresets()
    {
        return new[]
        {
            new Preset { name = "Low", value = 0.5f },
            new Preset { name = "Medium", value = 1.0f },
            new Preset { name = "High", value = 2.0f },
        };
    }
}
```

### wvalue-dropdown-primitives.png

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class AllPrimitives : MonoBehaviour
{
    [WValueDropDown(true, false)]
    public bool boolValue;

    [WValueDropDown('A', 'B', 'C')]
    public char charValue;

    [WValueDropDown((byte)1, (byte)2, (byte)3)]
    public byte byteValue;

    [WValueDropDown(100, 200, 300)]
    public int intValue;

    [WValueDropDown(0.1f, 0.5f, 1.0f)]
    public float floatValue;

    [WValueDropDown(0.1, 0.5, 1.0)]
    public double doubleValue;
}
```

### wvalue-dropdown-instance.png

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;
using System.Collections.Generic;

public class DynamicOptions : MonoBehaviour
{
    public string prefix = "Option";
    public int optionCount = 5;

    // Instance method - uses object state to build options
    [WValueDropDown(nameof(GetAvailableOptions), typeof(string))]
    public string selectedOption;

    private IEnumerable<string> GetAvailableOptions()
    {
        for (int i = 1; i <= optionCount; i++)
        {
            yield return $"{prefix}_{i}";
        }
    }
}
```

### int-dropdown-basic.png

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class FrameRateConfig : MonoBehaviour
{
    [IntDropdown(0, 30, 60, 120, 240)]
    public int refreshRate = 60;

    [IntDropdown(1, 2, 4, 8, 16, 32)]
    public int threadCount = 4;
}
```

### string-in-list-basic.png

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class DifficultySelector : MonoBehaviour
{
    [StringInList("Easy", "Normal", "Hard", "Nightmare")]
    public string difficulty = "Normal";

    [StringInList("Red", "Green", "Blue", "Yellow", "Purple")]
    public string teamColor = "Red";
}
```

### string-in-list-instance.png

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;
using System.Collections.Generic;

public class StateMachine : MonoBehaviour
{
    [StringInList(nameof(BuildAvailableStates))]
    public string currentState;

    private IEnumerable<string> BuildAvailableStates()
    {
        yield return $"{gameObject.name}_Idle";
        yield return $"{gameObject.name}_Run";
        yield return $"{gameObject.name}_Jump";
    }
}
```

---

## Capture Guidelines

1. **Focus on the inspector panel** - Crop to show relevant inspector area only
2. **Show realistic data** - Use meaningful values, not placeholder text
3. **Highlight the attribute effect** - Make sure the custom drawer is clearly visible
4. **Include tooltips when relevant** - Hover states can be captured in GIFs
5. **Consistent component naming** - Use descriptive component names visible in inspector header

---

## Existing References

The following image references exist in the [Inspector Selection Attributes](../../../features/inspector/inspector-selection-attributes.md) documentation:

```markdown
> ![WEnumToggleButtons with flag enum](../../images/inspector/wenum-toggle-buttons-basic.png)
> ![WEnumToggleButtons with Select All/None buttons](../../images/inspector/wenum-toggle-buttons-flags-select.png)
```

Once images are added to this folder, update references in the documentation to use the `selection/` subdirectory path:

```markdown
> ![WEnumToggleButtons with flag enum](../../images/inspector/selection/wenum-toggle-buttons-basic.png)
```
