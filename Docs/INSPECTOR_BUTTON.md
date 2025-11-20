# Inspector Buttons (WButton)

**Execute methods from the inspector with one click.**

The `[WButton]` attribute exposes methods as clickable buttons in the Unity inspector, complete with result history, async support, cancellation, custom styling, and automatic grouping. Test gameplay features, debug systems, and prototype rapidly without writing custom editors.

---

## Table of Contents

- [Basic Usage](#basic-usage)
- [Parameters](#parameters)
- [Execution Types](#execution-types)
- [Result History](#result-history)
- [Draw Order & Positioning](#draw-order--positioning)
- [Grouping](#grouping)
- [Color Theming](#color-theming)
- [Configuration](#configuration)
- [Best Practices](#best-practices)
- [Examples](#examples)

---

## Basic Usage

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class PlayerController : MonoBehaviour
{
    public int health = 100;

    [WButton("Heal Player")]
    private void Heal()
    {
        health = 100;
        Debug.Log("Player healed!");
    }

    [WButton("Take Damage")]
    private void TakeDamage()
    {
        health -= 10;
        Debug.Log($"Player took damage! Health: {health}");
    }
}
```

![Image placeholder: Inspector showing two buttons labeled "Heal Player" and "Take Damage"]

---

## Parameters

```csharp
[WButton(
    string displayName = null,           // Button label (defaults to method name)
    int drawOrder = 0,                   // Position: -1+ above inspector, 0+ below
    int historyCapacity = UseGlobalHistory,  // Results to keep per method
    string colorKey = "Default",         // Color palette key
    string groupName = null,             // Group header for organization
    int priority = 0                     // Deprecated (use drawOrder instead)
)]
```

---

## Execution Types

WButton supports four method signatures:

### 1. Void Methods (Immediate)

```csharp
[WButton("Log Message")]
private void LogMessage()
{
    Debug.Log("Button clicked!");
}
```

**Behavior:** Executes immediately, no return value shown

---

### 2. Returning Values (With History)

```csharp
[WButton("Roll Dice")]
private int RollDice()
{
    return Random.Range(1, 7);
}

[WButton("Get Position")]
private Vector3 GetPlayerPosition()
{
    return transform.position;
}
```

![Image placeholder: Button with result history showing multiple dice rolls: 4, 2, 6, 1, 5]
![GIF placeholder: Clicking "Roll Dice" and seeing new result added to history]

**Behavior:** Shows return value in collapsible history panel

---

### 3. Coroutines (IEnumerator)

```csharp
[WButton("Fade Out")]
private IEnumerator FadeOut()
{
    SpriteRenderer sprite = GetComponent<SpriteRenderer>();
    Color color = sprite.color;

    for (float t = 1f; t >= 0f; t -= Time.deltaTime)
    {
        color.a = t;
        sprite.color = color;
        yield return null;
    }

    Debug.Log("Fade complete!");
}
```

![GIF placeholder: Button showing spinner while coroutine executes, then "Complete" status]

**Behavior:**

- Shows "Running..." status
- Spinner animation during execution
- "Complete" message when finished

---

### 4. Async Methods (Task / ValueTask)

```csharp
using System.Threading;
using System.Threading.Tasks;

[WButton("Load Data")]
private async Task<string> LoadDataAsync(CancellationToken ct)
{
    Debug.Log("Loading...");
    await Task.Delay(2000, ct);  // Simulate async work
    return "Data loaded successfully!";
}

[WButton("Download Asset")]
private async ValueTask<Texture2D> DownloadAssetAsync(CancellationToken ct)
{
    Debug.Log("Downloading...");
    await Task.Delay(1000, ct);
    // Simulate download
    return new Texture2D(256, 256);
}
```

![Image placeholder: Async button with "Running..." status and Cancel button]
![GIF placeholder: Clicking button, showing spinner, then result appearing]

**Behavior:**

- Automatic `CancellationToken` injection (optional parameter)
- "Cancel" button appears during execution
- Result shown in history when complete
- Exceptions logged to console

**Cancellation Example:**

```csharp
[WButton("Long Operation")]
private async Task LongOperationAsync(CancellationToken ct)
{
    for (int i = 0; i < 10; i++)
    {
        ct.ThrowIfCancellationRequested();  // Check cancellation
        Debug.Log($"Step {i + 1}/10");
        await Task.Delay(500, ct);
    }
    return Task.CompletedTask;
}
```

**Supported Signatures:**

- `Task` (void async)
- `Task<T>` (async with result)
- `ValueTask` (void async, no heap allocation)
- `ValueTask<T>` (async with result, no heap allocation)

---

## Result History

### Automatic History

```csharp
[WButton("Generate ID", historyCapacity: 10)]
private string GenerateId()
{
    return System.Guid.NewGuid().ToString().Substring(0, 8);
}
```

![Image placeholder: History panel showing last 10 generated IDs]

**Features:**

- Per-method, per-target buffering (history survives inspector refresh)
- Collapsible foldout for each method
- Chronological order (newest first)
- Pagination when history exceeds display threshold

---

### History Capacity Options

```csharp
// Use global setting (default: 5, configurable in UnityHelpersSettings)
[WButton("Use Global")]
private int UseGlobal() => Random.Range(1, 100);

// Custom capacity per method
[WButton("Keep 20 Results", historyCapacity: 20)]
private float KeepMany() => Random.value;

// Disable history (0 capacity)
[WButton("No History", historyCapacity: 0)]
private void NoHistory() => Debug.Log("No history stored");
```

**Global Setting:** `UnityHelpersSettings.WButtonHistorySize` (default: 5, range: 1-10)

---

## Draw Order & Positioning

Control where buttons appear in the inspector:

```csharp
public class ButtonPositioning : MonoBehaviour
{
    // Appears ABOVE default inspector (drawOrder >= -1)
    [WButton("Top Button", drawOrder: -1)]
    private void TopButton() => Debug.Log("Above inspector");

    // Default inspector fields appear here
    public int someField = 10;

    // Appears BELOW default inspector (drawOrder >= 0)
    [WButton("Bottom Button", drawOrder: 0)]
    private void BottomButton() => Debug.Log("Below inspector");

    [WButton("Another Bottom", drawOrder: 1)]
    private void AnotherBottom() => Debug.Log("Also below");
}
```

![Image placeholder: Inspector showing button above fields, then fields, then buttons below]

**Positioning Rules:**

- `drawOrder < -1`: Hidden (not drawn)
- `drawOrder == -1`: Drawn at top (before default inspector)
- `drawOrder >= 0`: Drawn at bottom (after default inspector)
- Higher `drawOrder` values appear later within their section

---

### Pagination by Draw Order

```csharp
[WButton("Action 1", drawOrder: 0)]
private void Action1() {}

[WButton("Action 2", drawOrder: 0)]
private void Action2() {}

// ... 10 more buttons with drawOrder: 0 ...

[WButton("Action 12", drawOrder: 0)]
private void Action12() {}
```

![Image placeholder: Button pagination controls showing "Page 1 of 2" with navigation buttons]

**Pagination Settings:**

- Page size controlled by `UnityHelpersSettings.WButtonPageSize` (default: 6)
- Pagination only applies within each draw order group
- Navigation: First, Previous, Next, Last buttons

---

## Grouping

Organize buttons into named sections:

```csharp
[WButton("Spawn Enemy", groupName: "Combat")]
private void SpawnEnemy() => Debug.Log("Enemy spawned");

[WButton("Clear Enemies", groupName: "Combat")]
private void ClearEnemies() => Debug.Log("Enemies cleared");

[WButton("Save Game", groupName: "Persistence")]
private void SaveGame() => Debug.Log("Game saved");

[WButton("Load Game", groupName: "Persistence")]
private void LoadGame() => Debug.Log("Game loaded");
```

![Image placeholder: Two button groups with headers "Combat" and "Persistence"]

**Grouping Behavior:**

- Groups are created automatically based on `groupName`
- Buttons within a group are organized by `drawOrder`
- Groups can be collapsible (controlled by `UnityHelpersSettings.WButtonFoldoutBehavior`)

---

### Foldout Behavior

**Global Setting:** `UnityHelpersSettings.WButtonFoldoutBehavior`

**Options:**

- `Always` - Always show group foldout triangles
- `StartExpanded` - Collapsible, starts open
- `StartCollapsed` - Collapsible, starts closed

**Animation:**

- Enable/disable via `UnityHelpersSettings.WButtonFoldoutTweenEnabled`
- Speed controlled by `UnityHelpersSettings.WButtonFoldoutSpeed` (default: 2.0, range: 2-12)

![GIF placeholder: Button group folding/unfolding with smooth animation]

---

## Color Theming

```csharp
[WButton("Dangerous Action", colorKey: "Default-Dark")]
private void DangerousAction() => Debug.LogWarning("Dangerous!");

[WButton("Safe Action", colorKey: "Default-Light")]
private void SafeAction() => Debug.Log("Safe operation");
```

![Image placeholder: Two buttons with different color themes (dark red and light green)]

**Built-in Color Keys:**

- `"Default"` - Theme-aware (adapts to Unity theme)
- `"Default-Dark"` - Dark theme colors
- `"Default-Light"` - Light theme colors
- `"WDefault"` - Legacy vibrant blue
- Custom keys defined in `UnityHelpersSettings.WButtonCustomColors`

**Define Custom Colors:**

1. Open `ProjectSettings/UnityHelpersSettings.asset`
2. Add entry to `WButtonCustomColors` dictionary
3. Set button background, text color, border

![Image placeholder: UnityHelpersSettings showing WButton custom color configuration]

---

## Configuration

### Global Settings

All buttons respect project-wide settings defined in `UnityHelpersSettings`:

**Location:** `ProjectSettings/UnityHelpersSettings.asset`

**Settings:**

- `WButtonHistorySize` (default: 5, range: 1-10) - Results to keep per method
- `WButtonPlacement` (Top or Bottom) - Default button position
- `WButtonFoldoutBehavior` (Always, StartExpanded, StartCollapsed) - Group collapsibility
- `WButtonFoldoutTweenEnabled` (bool) - Enable group animations
- `WButtonFoldoutSpeed` (default: 2.0, range: 2-12) - Animation speed
- `WButtonPageSize` (default: 6) - Buttons per page for pagination
- `WButtonCustomColors` - Custom color palette dictionary

![Image placeholder: UnityHelpersSettings showing all WButton configuration options]

---

## Best Practices

### 1. Clear Button Names

```csharp
// ✅ GOOD: Action-oriented, descriptive
[WButton("Heal to Full")]
private void HealToFull() { ... }

[WButton("Spawn 10 Enemies")]
private void SpawnEnemies() { ... }

// ❌ BAD: Vague or technical
[WButton("DoStuff")]
private void DoStuff() { ... }

[WButton]  // Defaults to method name "HandlePlayerDeath"
private void HandlePlayerDeath() { ... }
```

---

### 2. Group Related Actions

```csharp
// ✅ GOOD: Grouped by feature
[WButton("Spawn Enemy", groupName: "Combat Testing")]
private void SpawnEnemy() { ... }

[WButton("Kill All Enemies", groupName: "Combat Testing")]
private void KillAll() { ... }

[WButton("Save Progress", groupName: "Persistence")]
private void Save() { ... }

// ❌ BAD: No grouping, cluttered inspector
[WButton("Spawn Enemy")]
private void SpawnEnemy() { ... }

[WButton("Save Progress")]
private void Save() { ... }

[WButton("Kill All Enemies")]
private void KillAll() { ... }
```

---

### 3. Use History for Random/Variable Results

```csharp
// ✅ GOOD: History helps track random values
[WButton("Roll Loot", historyCapacity: 10)]
private string RollLoot()
{
    return lootTable[Random.Range(0, lootTable.Length)];
}

// ✅ GOOD: No history needed for fixed actions
[WButton("Reset Position", historyCapacity: 0)]
private void ResetPosition()
{
    transform.position = Vector3.zero;
}
```

---

### 4. Async Best Practices

```csharp
// ✅ GOOD: Accept CancellationToken, check it
[WButton("Long Task")]
private async Task LongTaskAsync(CancellationToken ct)
{
    for (int i = 0; i < 100; i++)
    {
        ct.ThrowIfCancellationRequested();
        await Task.Delay(100, ct);
    }
}

// ✅ GOOD: Handle exceptions gracefully
[WButton("Risky Operation")]
private async Task RiskyOperationAsync()
{
    try
    {
        await SomeRiskyApiCall();
    }
    catch (Exception ex)
    {
        Debug.LogError($"Operation failed: {ex.Message}");
    }
}

// ❌ BAD: Long operation with no cancellation support
[WButton("Infinite Loop")]
private async Task InfiniteLoopAsync()
{
    while (true)  // No way to stop this!
    {
        await Task.Delay(1000);
    }
}
```

---

### 5. Color Usage

```csharp
// ✅ GOOD: Use colors to indicate risk/importance
[WButton("Delete All Data", colorKey: "Default-Dark")]  // Dark = danger
private void DeleteAllData() { ... }

[WButton("Quick Save", colorKey: "Default-Light")]  // Light = safe
private void QuickSave() { ... }

// ❌ BAD: Random colors without meaning
[WButton("Log Message", colorKey: "CustomPurple")]  // Why purple?
private void LogMessage() { ... }
```

---

## Examples

### Example 1: Gameplay Testing

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class PlayerDebug : MonoBehaviour
{
    public int health = 100;
    public int gold = 0;

    [WButton("Heal", groupName: "Health", colorKey: "Default-Light")]
    private void Heal()
    {
        health = 100;
        Debug.Log("Player healed!");
    }

    [WButton("Take Damage", groupName: "Health")]
    private void TakeDamage()
    {
        health -= 25;
        Debug.Log($"Took damage! Health: {health}");
    }

    [WButton("Kill Player", groupName: "Health", colorKey: "Default-Dark")]
    private void Kill()
    {
        health = 0;
        Debug.LogWarning("Player died!");
    }

    [WButton("Add Gold", groupName: "Economy")]
    private void AddGold()
    {
        gold += 100;
        Debug.Log($"Gold: {gold}");
    }

    [WButton("Roll Reward", groupName: "Economy", historyCapacity: 10)]
    private int RollReward()
    {
        int amount = Random.Range(10, 100);
        gold += amount;
        return amount;
    }
}
```

![Image placeholder: PlayerDebug inspector with Health and Economy button groups]

---

### Example 2: Async Data Loading

```csharp
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class DataManager : MonoBehaviour
{
    [WButton("Load Player Data")]
    private async Task<string> LoadPlayerDataAsync(CancellationToken ct)
    {
        Debug.Log("Loading player data...");

        // Simulate network request
        await Task.Delay(2000, ct);

        string playerName = "TestPlayer_" + Random.Range(1000, 9999);
        Debug.Log($"Loaded: {playerName}");

        return playerName;
    }

    [WButton("Batch Load", historyCapacity: 5)]
    private async Task<int> BatchLoadAsync(CancellationToken ct)
    {
        int count = 0;
        for (int i = 0; i < 10; i++)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(200, ct);
            count++;
            Debug.Log($"Loaded item {count}/10");
        }
        return count;
    }
}
```

![GIF placeholder: Async button showing loading spinner, then result]

---

### Example 3: Procedural Generation Testing

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class LevelGenerator : MonoBehaviour
{
    [WButton("Generate Seed", historyCapacity: 20)]
    private int GenerateSeed()
    {
        return Random.Range(1000, 9999);
    }

    [WButton("Generate Level")]
    private void GenerateLevel()
    {
        int seed = Random.Range(1000, 9999);
        Random.InitState(seed);
        Debug.Log($"Generating level with seed: {seed}");
        // ... generation logic ...
    }

    [WButton("Clear Level", colorKey: "Default-Dark")]
    private void ClearLevel()
    {
        // ... cleanup logic ...
        Debug.Log("Level cleared");
    }

    [WButton("Generate with Seed")]
    private void GenerateWithSeed(int seed)
    {
        Random.InitState(seed);
        Debug.Log($"Generating with seed: {seed}");
        // ... generation logic ...
    }
}
```

**Note:** Parameters are not yet supported in WButton. Use separate fields for configuration.

---

### Example 4: Coroutine Animation Testing

```csharp
using UnityEngine;
using System.Collections;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class AnimationTester : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    [WButton("Fade Out", groupName: "Animations")]
    private IEnumerator FadeOutCoroutine()
    {
        Color color = spriteRenderer.color;
        while (color.a > 0f)
        {
            color.a -= Time.deltaTime;
            spriteRenderer.color = color;
            yield return null;
        }
        Debug.Log("Fade out complete");
    }

    [WButton("Fade In", groupName: "Animations")]
    private IEnumerator FadeInCoroutine()
    {
        Color color = spriteRenderer.color;
        while (color.a < 1f)
        {
            color.a += Time.deltaTime;
            spriteRenderer.color = color;
            yield return null;
        }
        Debug.Log("Fade in complete");
    }

    [WButton("Pulse", groupName: "Animations")]
    private IEnumerator PulseCoroutine()
    {
        Vector3 originalScale = transform.localScale;
        for (int i = 0; i < 3; i++)
        {
            transform.localScale = originalScale * 1.2f;
            yield return new WaitForSeconds(0.2f);
            transform.localScale = originalScale;
            yield return new WaitForSeconds(0.2f);
        }
        Debug.Log("Pulse complete");
    }
}
```

![GIF placeholder: Animation buttons triggering visual effects]

---

## Troubleshooting

### Button Not Appearing

**Problem:** Method has `[WButton]` but button doesn't show

**Solutions:**

1. Check `drawOrder` - values < -1 hide the button
2. Ensure method is `private` or `protected` (public methods may conflict)
3. Verify the component is enabled and active

---

### History Not Showing

**Problem:** Method returns a value but no history appears

**Solutions:**

1. Check `historyCapacity` - make sure it's > 0
2. Verify return type is serializable
3. Check `UnityHelpersSettings.WButtonHistorySize` if using global setting

---

### Async Method Not Cancelling

**Problem:** Cancel button doesn't stop async method

**Solutions:**

1. Ensure method accepts `CancellationToken` parameter
2. Check token periodically: `ct.ThrowIfCancellationRequested()`
3. Pass token to async operations: `await Task.Delay(1000, ct)`

```csharp
// ✅ CORRECT: Cancellable
[WButton("Long Task")]
private async Task LongTaskAsync(CancellationToken ct)
{
    for (int i = 0; i < 100; i++)
    {
        ct.ThrowIfCancellationRequested();  // Check token
        await Task.Delay(100, ct);  // Pass token
    }
}
```

---

## See Also

- **[Inspector Overview](INSPECTOR_OVERVIEW.md)** - Complete inspector features overview
- **[Inspector Grouping Attributes](INSPECTOR_GROUPING_ATTRIBUTES.md)** - WGroup and WFoldoutGroup
- **[Inspector Settings](INSPECTOR_SETTINGS.md)** - Configuration reference
- **[Editor Tools Guide](EDITOR_TOOLS_GUIDE.md)** - Other editor utilities

---

**Next Steps:**

- Add buttons to your components for quick testing
- Experiment with `groupName` to organize buttons
- Try async methods with `CancellationToken` support
- Customize colors in `UnityHelpersSettings.asset`
