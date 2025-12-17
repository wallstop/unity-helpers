# WEnumToggleButtons Attribute

## Why It Exists

Working with flag-based enums or small enumerated choice sets in Unity's inspector often involves the default dropdown or mask fields. These controls are serviceable but fall short when designers need to reason about combinations, visually scan state, or make rapid bulk changes. Inspired by Odin Inspector's `EnumToggleButtons`, the `WEnumToggleButtonsAttribute` serves as an in-house, dependency-free alternative that keeps the experience inside Unity's default tooling.

- **Clarity for composite selections:** Individual buttons communicate state far better than a numeric mask value or checked list inside a popup.
- **Fewer clicks for repetitive workflows:** Quick action buttons enable "All" or "None" operations without diving into menus.
- **Consistent styling with other helpers:** The attribute integrates with `IntDropdown`, `StringInList`, and `WValueDropDown`, letting teams standardize on a single visual language for curated options.

## Supported Targets

- `[Flags]` enums with discrete power-of-two members (plus the optional zero member).
- Standard enums for single-value selection.
- Fields decorated with `IntDropdown`, `StringInList`, or `WValueDropDown`, rendered as toggle groups instead of popups.

### Pagination Defaults

- Pagination automatically kicks in when the option count exceeds the project-wide page size defined in **Project Settings ▸ Unity Helpers** (`Editor/Settings/UnityHelpersSettings`).
- Override per-field with `PageSize` on the attribute, or disable entirely by setting `EnablePagination = false`.

## Usage

```csharp
[System.Flags]
public enum MovementCapabilities
{
    None = 0,
    Walk = 1 << 0,
    Jump = 1 << 1,
    Swim = 1 << 2,
    Fly = 1 << 3,
}

[WEnumToggleButtons(ButtonsPerRow = 3)]
public MovementCapabilities unlockedAbilities;
```

> **Visual Reference**
>
> ![WEnumToggleButtons showing flag enum as toggleable buttons](../../images/inspector/selection/wenum-toggle-buttons-flags-select.png)
> _Flag enum rendered as individual toggle buttons with Select All/None options_

```csharp

[WEnumToggleButtons(ShowSelectNone = false)]
[StringInList("Low", "Medium", "High")]
public string difficulty;

[WEnumToggleButtons]
[IntDropdown(30, 60, 90, 120)]
public int targetFrameRate;

[WEnumToggleButtons]
[WValueDropDown(typeof(AudioBusLibrary), nameof(AudioBusLibrary.GetBusNames))]
public string targetBus;

[WEnumToggleButtons(PageSize = 8)]
[IntDropdown(0, 1, 2, 3, 4, 5, 6, 7, 8, 9)]
public int presetIndex;

[WEnumToggleButtons(EnablePagination = false)]
[StringInList("North", "East", "South", "West", "Above", "Below")]
public string facing;
```

### Layout Controls

- `ButtonsPerRow`: forces a specific column count (set to zero for automatic sizing).
- `ShowSelectAll` / `ShowSelectNone`: toggles quick actions for flag enums.
- `EnablePagination`: set to `false` to always render every option on a single page.
- `PageSize`: tune the number of options shown per page (clamped to the Unity Helpers minimum and maximum page sizes).

## Editor Behaviour

- **Flags:** Each discrete flag is a button. "All" and "None" toggles appear when enabled.
- **Non-flags:** Buttons behave like radio controls; only one remains active.
- **Dropdown-backed fields:** Options mirror the ordering supplied by their source attribute.
- **Pagination:** Navigation controls appear above the buttons once the configured threshold is exceeded. Page state persists per object while the inspector is open.
- **Current Selection Badge:** When the active choice lives on a different page, a short read-only summary appears beneath the field label so you can see what is currently applied without hunting through pages.

### What About Composite Flags?

The drawer intentionally filters out composite flag values (e.g., `ReadWrite = Read | Write`). This keeps the UI focused on atomic toggles and avoids ambiguous interactions. If a composite convenience value is needed, the "All" and "None" buttons already cover the primary bulk operations.

## Editor Tests

The accompanying test suite exercises:

- Option generation for flag enums, normal enums, and dropdown-backed fields.
- State transitions when toggling individual buttons or executing the quick commands.
- Equality comparisons between serialized values and supplied dropdown entries, ensuring references, integers, floats, and strings behave consistently.
- Pagination state management and disable/override scenarios, including clamping behavior when the page size changes.

## Tips

- Keep option counts manageable. Toggle groups work best for short lists where a designer can see everything without scrolling.
- Prefer naming enum members descriptively so the automatic nicified labels remain readable.
- Combine with other inspectors (e.g., `WShowIf`) to build adaptive, context-aware authoring tools without sacrificing clarity.
