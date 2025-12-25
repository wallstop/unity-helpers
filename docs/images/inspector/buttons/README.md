# WButton Inspector Images - Additional Requirements

This directory contains images for the WButton inspector documentation. The following additional images are needed for the new `groupPriority` and `groupPlacement` features.

## Required New Images

### Static Screenshots (PNG)

| Filename                               | Description                                                                 |
| -------------------------------------- | --------------------------------------------------------------------------- |
| `inspector-button-group-priority.png`  | Shows groups ordered by priority (Primary first, Debug second, Misc last)   |
| `inspector-button-group-placement.png` | Shows Setup group at top, properties in middle, Maintenance group at bottom |

### Animated GIFs

| Filename                               | Description                                                                    |
| -------------------------------------- | ------------------------------------------------------------------------------ |
| `inspector-button-advanced-layout.gif` | Advanced layout showing: Authoring → Validation at top, IO → Network at bottom |

## Image Specifications

- **Screenshots**: 800-1200px wide, PNG format
- **GIFs**: 600-900px wide, optimized for file size
- **Theme**: Use Unity's default dark theme for consistency

## Code Examples for Screenshots

### inspector-button-group-priority.png

```csharp
public class ActionPanel : MonoBehaviour
{
    // This group renders FIRST (priority 0)
    [WButton("Quick Save", groupName: "Primary", groupPriority: 0)]
    private void QuickSave() { }

    [WButton("Quick Load", groupName: "Primary", groupPriority: 0)]
    private void QuickLoad() { }

    // This group renders SECOND (priority 10)
    [WButton("Debug Info", groupName: "Debug", groupPriority: 10)]
    private void ShowDebugInfo() { }

    // This group renders LAST (no explicit priority)
    [WButton("Reset", groupName: "Misc")]
    private void Reset() { }
}
```

### inspector-button-group-placement.png

```csharp
public class MixedPlacementExample : MonoBehaviour
{
    public int health = 100;
    public float speed = 5f;

    // This group ALWAYS renders at the top
    [WButton("Initialize", groupName: "Setup", groupPlacement: WButtonGroupPlacement.Top)]
    private void Initialize() { }

    [WButton("Validate", groupName: "Setup", groupPlacement: WButtonGroupPlacement.Top)]
    private void Validate() { }

    // This group ALWAYS renders at the bottom
    [WButton("Cleanup", groupName: "Maintenance", groupPlacement: WButtonGroupPlacement.Bottom)]
    private void Cleanup() { }

    [WButton("Reset All", groupName: "Maintenance", groupPlacement: WButtonGroupPlacement.Bottom)]
    private void ResetAll() { }
}
```

### inspector-button-advanced-layout.gif

```csharp
public class AdvancedButtonLayout : ScriptableObject
{
    // TOP SECTION - ordered by priority
    [WButton("Generate IDs", groupName: "Authoring", groupPriority: 0, groupPlacement: WButtonGroupPlacement.Top)]
    private void GenerateIds() { }

    [WButton("Validate Data", groupName: "Validation", groupPriority: 1, groupPlacement: WButtonGroupPlacement.Top)]
    private void ValidateData() { }

    // BOTTOM SECTION - ordered by priority
    [WButton("Export", groupName: "IO", groupPriority: 0, groupPlacement: WButtonGroupPlacement.Bottom)]
    private void Export() { }

    [WButton("Import", groupName: "IO", groupPriority: 0, groupPlacement: WButtonGroupPlacement.Bottom)]
    private void Import() { }

    [WButton("Submit to Server", groupName: "Network", groupPriority: 10, groupPlacement: WButtonGroupPlacement.Bottom)]
    private void Submit() { }
}
```
