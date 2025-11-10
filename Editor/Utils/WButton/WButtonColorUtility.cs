namespace WallstopStudios.UnityHelpers.Editor.Utils.WButton
{
#if UNITY_EDITOR
    using UnityEngine;

    internal static class WButtonColorUtility
    {
        private const float GoldenRatio = 0.61803398875f;
        private const float DefaultSaturation = 0.65f;
        private const float DefaultValue = 0.9f;
        private const float ActiveDarkenAmount = 0.12f;
        private const float HoverDarkenAmount = 0.06f;

        internal static Color SuggestPaletteColor(int index)
        {
            float hue = Mathf.Repeat(0.05f + (index * GoldenRatio), 1f);
            Color color = Color.HSVToRGB(hue, DefaultSaturation, DefaultValue);
            color.a = 1f;
            return color;
        }

        internal static Color GetReadableTextColor(Color background)
        {
            float luminance =
                (0.299f * background.r) + (0.587f * background.g) + (0.114f * background.b);
            return luminance > 0.55f ? Color.black : Color.white;
        }

        internal static Color GetHoverColor(Color baseColor)
        {
            return AdjustValue(baseColor, -HoverDarkenAmount);
        }

        internal static Color GetActiveColor(Color baseColor)
        {
            return AdjustValue(baseColor, -ActiveDarkenAmount);
        }

        private static Color AdjustValue(Color color, float delta)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            v = Mathf.Clamp01(v + delta);
            Color adjusted = Color.HSVToRGB(h, s, v);
            adjusted.a = color.a;
            return adjusted;
        }
    }
#endif
}
