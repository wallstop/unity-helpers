namespace WallstopStudios.UnityHelpers.Editor.Utils.WGroup
{
#if UNITY_EDITOR
    using System.Runtime.CompilerServices;
    using UnityEngine;

    /// <summary>
    /// Provides color derivation algorithms for WGroup collection styling.
    /// Derives appropriate row colors, selection colors, and borders from a palette's BackgroundColor
    /// when explicit values are not provided.
    /// </summary>
    internal static class WGroupColorDerivation
    {
        /// <summary>
        /// The luminance threshold that distinguishes light from dark backgrounds.
        /// Backgrounds with luminance above this value are considered "light".
        /// </summary>
        private const float LuminanceThreshold = 0.5f;

        /// <summary>
        /// Darkening amount for row color on light backgrounds (3%).
        /// </summary>
        private const float LightRowDarkenAmount = 0.03f;

        /// <summary>
        /// Darkening amount for alternate row color on light backgrounds (6%).
        /// </summary>
        private const float LightAlternateRowDarkenAmount = 0.06f;

        /// <summary>
        /// Darkening amount for border color on light backgrounds (15%).
        /// </summary>
        private const float LightBorderDarkenAmount = 0.15f;

        /// <summary>
        /// Lightening amount for row color on dark backgrounds (5%).
        /// </summary>
        private const float DarkRowLightenAmount = 0.05f;

        /// <summary>
        /// Lightening amount for alternate row color on dark backgrounds (10%).
        /// </summary>
        private const float DarkAlternateRowLightenAmount = 0.10f;

        /// <summary>
        /// Lightening amount for border color on dark backgrounds (20%).
        /// </summary>
        private const float DarkBorderLightenAmount = 0.20f;

        /// <summary>
        /// Darkening amount for pending background on light themes (8%).
        /// </summary>
        private const float LightPendingDarkenAmount = 0.08f;

        /// <summary>
        /// Lightening amount for pending background on dark themes (12%).
        /// </summary>
        private const float DarkPendingLightenAmount = 0.12f;

        /// <summary>
        /// Selection color for light backgrounds (blue accent with moderate opacity).
        /// </summary>
        private static readonly Color LightSelectionColor = new Color(0.33f, 0.62f, 0.95f, 0.65f);

        /// <summary>
        /// Selection color for dark backgrounds (blue accent with slightly higher opacity).
        /// </summary>
        private static readonly Color DarkSelectionColor = new Color(0.2f, 0.45f, 0.85f, 0.7f);

        /// <summary>
        /// Alpha value for derived row colors.
        /// </summary>
        private const float RowColorAlpha = 1f;

        /// <summary>
        /// Alpha value for derived border colors.
        /// </summary>
        private const float BorderColorAlpha = 1f;

        /// <summary>
        /// Subtle yellow tint factor for pending background.
        /// </summary>
        private const float PendingYellowTintFactor = 0.02f;

        /// <summary>
        /// Subtle green tint factor for pending background.
        /// </summary>
        private const float PendingGreenTintFactor = 0.01f;

        /// <summary>
        /// Calculates the perceived luminance of a color using the standard formula.
        /// </summary>
        /// <param name="color">The color to evaluate.</param>
        /// <returns>A value between 0 (black) and 1 (white) representing perceived brightness.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetLuminance(Color color)
        {
            return (0.299f * color.r) + (0.587f * color.g) + (0.114f * color.b);
        }

        /// <summary>
        /// Determines whether a color is considered "light" based on its luminance.
        /// </summary>
        /// <param name="color">The color to evaluate.</param>
        /// <returns>True if the color has luminance above the threshold; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLightColor(Color color)
        {
            return GetLuminance(color) > LuminanceThreshold;
        }

        /// <summary>
        /// Lightens a color by the specified amount.
        /// </summary>
        /// <param name="color">The color to lighten.</param>
        /// <param name="amount">The amount to lighten (0 = no change, 1 = full white).</param>
        /// <returns>A new color lightened by the specified amount, preserving alpha.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color Lighten(Color color, float amount)
        {
            float clampedAmount = Mathf.Clamp01(amount);
            Color result = new Color(
                Mathf.Lerp(color.r, 1f, clampedAmount),
                Mathf.Lerp(color.g, 1f, clampedAmount),
                Mathf.Lerp(color.b, 1f, clampedAmount),
                color.a
            );
            return result;
        }

        /// <summary>
        /// Darkens a color by the specified amount.
        /// </summary>
        /// <param name="color">The color to darken.</param>
        /// <param name="amount">The amount to darken (0 = no change, 1 = full black).</param>
        /// <returns>A new color darkened by the specified amount, preserving alpha.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color Darken(Color color, float amount)
        {
            float clampedAmount = Mathf.Clamp01(amount);
            Color result = new Color(
                Mathf.Lerp(color.r, 0f, clampedAmount),
                Mathf.Lerp(color.g, 0f, clampedAmount),
                Mathf.Lerp(color.b, 0f, clampedAmount),
                color.a
            );
            return result;
        }

        /// <summary>
        /// Derives the base row color from a background color.
        /// For light backgrounds, darkens slightly for contrast.
        /// For dark backgrounds, lightens slightly to avoid dark-on-dark issues.
        /// </summary>
        /// <param name="backgroundColor">The background color to derive from.</param>
        /// <returns>A derived row color with appropriate contrast.</returns>
        public static Color DeriveRowColor(Color backgroundColor)
        {
            Color derivedColor;
            if (IsLightColor(backgroundColor))
            {
                derivedColor = Darken(backgroundColor, LightRowDarkenAmount);
            }
            else
            {
                derivedColor = Lighten(backgroundColor, DarkRowLightenAmount);
            }
            derivedColor.a = RowColorAlpha;
            return derivedColor;
        }

        /// <summary>
        /// Derives the alternate row color from a background color.
        /// Provides slightly more contrast than the base row color for visual distinction.
        /// </summary>
        /// <param name="backgroundColor">The background color to derive from.</param>
        /// <returns>A derived alternate row color.</returns>
        public static Color DeriveAlternateRowColor(Color backgroundColor)
        {
            Color derivedColor;
            if (IsLightColor(backgroundColor))
            {
                derivedColor = Darken(backgroundColor, LightAlternateRowDarkenAmount);
            }
            else
            {
                derivedColor = Lighten(backgroundColor, DarkAlternateRowLightenAmount);
            }
            derivedColor.a = RowColorAlpha;
            return derivedColor;
        }

        /// <summary>
        /// Derives the selection/hover color from a background color.
        /// Uses a blue accent color appropriate for the background luminance.
        /// </summary>
        /// <param name="backgroundColor">The background color to derive from.</param>
        /// <returns>A derived selection color with appropriate opacity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color DeriveSelectionColor(Color backgroundColor)
        {
            return IsLightColor(backgroundColor) ? LightSelectionColor : DarkSelectionColor;
        }

        /// <summary>
        /// Derives the border color from a background color.
        /// Creates a subtle border that defines edges without being harsh.
        /// </summary>
        /// <param name="backgroundColor">The background color to derive from.</param>
        /// <returns>A derived border color.</returns>
        public static Color DeriveBorderColor(Color backgroundColor)
        {
            Color derivedColor;
            if (IsLightColor(backgroundColor))
            {
                derivedColor = Darken(backgroundColor, LightBorderDarkenAmount);
            }
            else
            {
                derivedColor = Lighten(backgroundColor, DarkBorderLightenAmount);
            }
            derivedColor.a = BorderColorAlpha;
            return derivedColor;
        }

        /// <summary>
        /// Derives the pending/new entry background color from a background color.
        /// Applies a subtle yellow/green tint to indicate a pending state.
        /// </summary>
        /// <param name="backgroundColor">The background color to derive from.</param>
        /// <returns>A derived pending background color with subtle tinting.</returns>
        public static Color DerivePendingBackgroundColor(Color backgroundColor)
        {
            Color derivedColor;
            if (IsLightColor(backgroundColor))
            {
                derivedColor = Darken(backgroundColor, LightPendingDarkenAmount);
            }
            else
            {
                derivedColor = Lighten(backgroundColor, DarkPendingLightenAmount);
            }

            // Apply subtle yellow/green tint for pending state indication
            derivedColor.r = Mathf.Clamp01(derivedColor.r + PendingYellowTintFactor);
            derivedColor.g = Mathf.Clamp01(
                derivedColor.g + PendingYellowTintFactor + PendingGreenTintFactor
            );

            derivedColor.a = RowColorAlpha;
            return derivedColor;
        }

        /// <summary>
        /// Gets the effective row color, using the provided explicit value if available,
        /// otherwise deriving from the background color.
        /// </summary>
        /// <param name="backgroundColor">The background color for derivation.</param>
        /// <param name="explicitRowColor">Optional explicit row color to use if provided.</param>
        /// <returns>The effective row color.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color GetEffectiveRowColor(Color backgroundColor, Color? explicitRowColor)
        {
            return explicitRowColor ?? DeriveRowColor(backgroundColor);
        }

        /// <summary>
        /// Gets the effective alternate row color, using the provided explicit value if available,
        /// otherwise deriving from the background color.
        /// </summary>
        /// <param name="backgroundColor">The background color for derivation.</param>
        /// <param name="explicitAlternateRowColor">Optional explicit alternate row color to use if provided.</param>
        /// <returns>The effective alternate row color.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color GetEffectiveAlternateRowColor(
            Color backgroundColor,
            Color? explicitAlternateRowColor
        )
        {
            return explicitAlternateRowColor ?? DeriveAlternateRowColor(backgroundColor);
        }

        /// <summary>
        /// Gets the effective selection color, using the provided explicit value if available,
        /// otherwise deriving from the background color.
        /// </summary>
        /// <param name="backgroundColor">The background color for derivation.</param>
        /// <param name="explicitSelectionColor">Optional explicit selection color to use if provided.</param>
        /// <returns>The effective selection color.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color GetEffectiveSelectionColor(
            Color backgroundColor,
            Color? explicitSelectionColor
        )
        {
            return explicitSelectionColor ?? DeriveSelectionColor(backgroundColor);
        }

        /// <summary>
        /// Gets the effective border color, using the provided explicit value if available,
        /// otherwise deriving from the background color.
        /// </summary>
        /// <param name="backgroundColor">The background color for derivation.</param>
        /// <param name="explicitBorderColor">Optional explicit border color to use if provided.</param>
        /// <returns>The effective border color.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color GetEffectiveBorderColor(
            Color backgroundColor,
            Color? explicitBorderColor
        )
        {
            return explicitBorderColor ?? DeriveBorderColor(backgroundColor);
        }

        /// <summary>
        /// Gets the effective pending background color, using the provided explicit value if available,
        /// otherwise deriving from the background color.
        /// </summary>
        /// <param name="backgroundColor">The background color for derivation.</param>
        /// <param name="explicitPendingBackgroundColor">Optional explicit pending background color to use if provided.</param>
        /// <returns>The effective pending background color.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color GetEffectivePendingBackgroundColor(
            Color backgroundColor,
            Color? explicitPendingBackgroundColor
        )
        {
            return explicitPendingBackgroundColor ?? DerivePendingBackgroundColor(backgroundColor);
        }
    }
#endif
}
