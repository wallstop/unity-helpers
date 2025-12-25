# Inline Editor Inspector Images

This directory contains images for the inspector inline editor documentation (`inspector-inline-editor.md`).

---

## Required Images

| Filename                      | Description                                                                   |
| ----------------------------- | ----------------------------------------------------------------------------- |
| `winlineeditor-basic.gif`     | Basic WInLineEditor showing embedded ScriptableObject inspector with foldout  |
| `winlineeditor-modes.png`     | Comparison of AlwaysExpanded, FoldoutExpanded, and FoldoutCollapsed modes     |
| `winlineeditor-options.png`   | Different configuration combinations showing compact vs full-featured layouts |
| `winlineeditor-animation.gif` | Smooth expand/collapse animation with configurable speed                      |

---

## Image Specifications

- **Screenshots (PNG)**: 800-1200px wide, PNG format with transparency where appropriate
- **Animated GIFs**: 600-900px wide, optimized for file size (under 2MB)
- **Theme**: Use Unity's default dark theme for consistency with other documentation

---

## Code Examples for Screenshots

### winlineeditor-basic.gif

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class AbilityConfig : ScriptableObject
{
    public string displayName;
    public float cooldown;
    public Sprite icon;
}

public class Character : MonoBehaviour
{
    [WInLineEditor]
    public AbilityConfig primaryAbility;
}
```

### winlineeditor-modes.png

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class ModesExample : MonoBehaviour
{
    [WInLineEditor(WInLineEditorMode.AlwaysExpanded)]
    public AbilityConfig alwaysVisible;

    [WInLineEditor(WInLineEditorMode.FoldoutExpanded)]
    public AbilityConfig expandedByDefault;

    [WInLineEditor(WInLineEditorMode.FoldoutCollapsed)]
    public AbilityConfig collapsedByDefault;
}
```

### winlineeditor-options.png

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class OptionsExample : MonoBehaviour
{
    // Compact: no header, no object field
    [WInLineEditor(
        WInLineEditorMode.AlwaysExpanded,
        inspectorHeight: 160f,
        drawObjectField: false,
        drawHeader: false)]
    public AbilityConfig compactView;

    // Full featured: preview, scrolling, header
    [WInLineEditor(
        WInLineEditorMode.FoldoutExpanded,
        inspectorHeight: 300f,
        drawPreview: true,
        previewHeight: 80f)]
    public Texture2D fullFeatured;
}
```

### winlineeditor-animation.gif

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class AnimationExample : MonoBehaviour
{
    [WInLineEditor(WInLineEditorMode.FoldoutCollapsed)]
    public AbilityConfig animatedFoldout;
}
```

Capture: Click the foldout to show smooth expand/collapse animation.

---

## Capture Guidelines

1. **Focus on the inspector panel** - Crop to show relevant inspector area only
2. **Show realistic data** - Use meaningful values in the ScriptableObjects
3. **Highlight the attribute effect** - Make sure inline editors are clearly visible
4. **Include transitions in GIFs** - Show expand/collapse for foldout modes
5. **Consistent component naming** - Use descriptive component names visible in inspector header
