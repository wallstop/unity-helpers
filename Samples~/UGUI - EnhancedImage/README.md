UGUI – EnhancedImage

Shows programmatic setup and usage of `EnhancedImage` with HDR tinting and shape masks.

How to use

- Add `EnhancedImageDemo` to an empty scene and press Play.
- Optionally assign a `materialTemplate` in the inspector to use a shader that exposes `_Color` and optional `_ShapeMask`.

What it shows

- Creating a `Canvas` and an `EnhancedImage` at runtime.
- Setting an HDR tint via `HdrColor` (values > 1).
- Using shape masks for custom masking effects.

Features

HDR Color Support:

- `HdrColor` property accepts values > 1 for intensity effects
- Automatically creates per-instance materials to prevent shared state
- Falls back to standard `Graphic.color` when HDR values aren't needed

Shape Mask Support:

- Assign a texture to the `_shapeMask` field for shader-driven masking
- Works without additional UI Mask components
- Requires a shader with a `_ShapeMask` texture slot (falls back to `Hidden/Wallstop/EnhancedImageSupport` if needed)
- Useful for custom wipe effects, reveals, and stylized outlines

Example: HDR Tinting

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Visuals.UGUI;

public sealed class AbilityIconPresenter : MonoBehaviour
{
    [SerializeField] private EnhancedImage icon;

    public void SetCharge(float normalizedCharge)
    {
        // Values > 1 create HDR glow effects
        float intensity = Mathf.Lerp(1f, 2.5f, normalizedCharge);
        icon.HdrColor = new Color(intensity, 1f, 0.4f, 1f);
    }
}
```

When to Use

Reach for `EnhancedImage` when you need:

- Per-control HDR highlights or glow effects
- Shader-driven reveal effects or wipes
- Custom masking without UI Mask components

Prefer the stock `Image` when shared materials and low overhead matter more than these features.
