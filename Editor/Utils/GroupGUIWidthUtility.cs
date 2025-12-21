namespace WallstopStudios.UnityHelpers.Editor.Utils
{
#if UNITY_EDITOR
    using System;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Settings;

    internal static class GroupGUIWidthUtility
    {
        private sealed class WidthPaddingScope : IDisposable
        {
            private readonly float _padding;
            private readonly float _leftPadding;
            private readonly float _rightPadding;
            private readonly bool _trackScopeDepth;
            private bool _disposed;

            internal WidthPaddingScope(float horizontalPadding)
            {
                float resolved = Mathf.Max(0f, horizontalPadding);
                float split = resolved * 0.5f;
                _padding = resolved;
                _leftPadding = split;
                _rightPadding = resolved - split;

                // Only track scope depth if there's actual padding to apply
                // Zero padding should not increase scope depth as it has no visual effect
                _trackScopeDepth = _padding > 0f;

                if (_trackScopeDepth)
                {
                    _scopeDepth++;
                    _totalPadding += _padding;
                    _totalLeftPadding += _leftPadding;
                    _totalRightPadding += _rightPadding;
                }
            }

            internal WidthPaddingScope(
                float horizontalPadding,
                float leftPadding,
                float rightPadding
            )
            {
                _leftPadding = Mathf.Max(0f, leftPadding);
                _rightPadding = Mathf.Max(0f, rightPadding);
                float combined = Mathf.Max(0f, horizontalPadding);
                if (combined <= 0f)
                {
                    combined = _leftPadding + _rightPadding;
                }

                // Only track scope depth if there's actual padding to apply
                // Zero padding should not increase scope depth as it has no visual effect
                if (combined <= 0f)
                {
                    _padding = 0f;
                    _leftPadding = 0f;
                    _rightPadding = 0f;
                    _trackScopeDepth = false;
                    return;
                }

                _padding = combined;
                float resolvedLeft = _leftPadding;
                float resolvedRight = _rightPadding;
                if (resolvedLeft <= 0f && resolvedRight <= 0f)
                {
                    float split = combined * 0.5f;
                    resolvedLeft = split;
                    resolvedRight = combined - split;
                }

                _leftPadding = resolvedLeft;
                _rightPadding = resolvedRight;
                _trackScopeDepth = true;

                _scopeDepth++;
                _totalPadding += _padding;
                _totalLeftPadding += _leftPadding;
                _totalRightPadding += _rightPadding;
            }

            public void Dispose()
            {
                if (_disposed || !_trackScopeDepth)
                {
                    return;
                }

                _disposed = true;

                _totalPadding = Mathf.Max(0f, _totalPadding - _padding);
                _totalLeftPadding = Mathf.Max(0f, _totalLeftPadding - _leftPadding);
                _totalRightPadding = Mathf.Max(0f, _totalRightPadding - _rightPadding);
                _scopeDepth = Mathf.Max(0, _scopeDepth - 1);
            }
        }

        private static float _totalPadding;
        private static float _totalLeftPadding;
        private static float _totalRightPadding;
        private static int _scopeDepth;
        private static bool _isInsideWGroupPropertyDraw;
        private static UnityHelpersSettings.WGroupPaletteEntry? _currentPalette;

        internal static float CurrentHorizontalPadding => _totalPadding;
        internal static float CurrentLeftPadding => _totalLeftPadding;
        internal static float CurrentRightPadding => _totalRightPadding;
        internal static int CurrentScopeDepth => _scopeDepth;

        internal static bool IsInsideWGroupPropertyDraw => _isInsideWGroupPropertyDraw;

        /// <summary>
        /// Gets whether code is currently executing inside any WGroup scope.
        /// This is true whenever there is an active WGroup palette on the stack.
        /// </summary>
        internal static bool IsInsideWGroup => _currentPalette != null;

        /// <summary>
        /// Gets the current WGroup palette entry if drawing inside a WGroup, or null otherwise.
        /// Use this to ensure child elements (lists, dictionaries, etc.) use consistent theming.
        /// </summary>
        internal static UnityHelpersSettings.WGroupPaletteEntry? CurrentPalette => _currentPalette;

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        internal static void ResetForTests()
        {
            _totalPadding = 0f;
            _totalLeftPadding = 0f;
            _totalRightPadding = 0f;
            _scopeDepth = 0;
            _isInsideWGroupPropertyDraw = false;
            _currentPalette = null;
        }

        internal static IDisposable PushWGroupPropertyContext()
        {
            return new WGroupPropertyContextScope();
        }

        /// <summary>
        /// Pushes a WGroup palette onto the stack so child drawers can access it.
        /// </summary>
        internal static IDisposable PushWGroupPalette(
            UnityHelpersSettings.WGroupPaletteEntry palette
        )
        {
            return new WGroupPaletteScope(palette);
        }

        private sealed class WGroupPaletteScope : IDisposable
        {
            private readonly UnityHelpersSettings.WGroupPaletteEntry? _previousPalette;
            private bool _disposed;

            internal WGroupPaletteScope(UnityHelpersSettings.WGroupPaletteEntry palette)
            {
                _previousPalette = _currentPalette;
                _currentPalette = palette;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _currentPalette = _previousPalette;
            }
        }

        private sealed class WGroupPropertyContextScope : IDisposable
        {
            private readonly bool _previousValue;
            private bool _disposed;

            internal WGroupPropertyContextScope()
            {
                _previousValue = _isInsideWGroupPropertyDraw;
                _isInsideWGroupPropertyDraw = true;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _isInsideWGroupPropertyDraw = _previousValue;
            }
        }

        internal static IDisposable PushContentPadding(float horizontalPadding)
        {
            return new WidthPaddingScope(horizontalPadding);
        }

        internal static IDisposable PushContentPadding(
            float horizontalPadding,
            float leftPadding,
            float rightPadding
        )
        {
            return new WidthPaddingScope(horizontalPadding, leftPadding, rightPadding);
        }

        internal static Rect ApplyCurrentPadding(Rect rect)
        {
            float leftPadding = _totalLeftPadding;
            float rightPadding = _totalRightPadding;
            if (leftPadding <= 0f && rightPadding <= 0f)
            {
                return rect;
            }

            Rect adjusted = rect;
            adjusted.xMin += leftPadding;
            adjusted.xMax -= rightPadding;
            if (adjusted.width < 0f || float.IsNaN(adjusted.width))
            {
                adjusted.width = 0f;
            }

            return adjusted;
        }

        internal static float CalculateHorizontalPadding(GUIStyle containerStyle)
        {
            return CalculateHorizontalPadding(containerStyle, out _, out _);
        }

        internal static float CalculateHorizontalPadding(
            GUIStyle containerStyle,
            out float leftPadding,
            out float rightPadding
        )
        {
            leftPadding = 0f;
            rightPadding = 0f;
            if (containerStyle == null)
            {
                return 0f;
            }

            RectOffset padding = containerStyle.padding;
            if (padding == null)
            {
                return 0f;
            }

            leftPadding = Mathf.Max(0f, padding.left);
            rightPadding = Mathf.Max(0f, padding.right);
            int total = padding.left + padding.right;
            return Mathf.Max(0f, total);
        }

        /// <summary>
        /// Returns true if there is an active WGroup palette with a light background
        /// (luminance > 0.5), indicating child elements should use dark/light theming
        /// opposite to the Unity editor skin.
        /// </summary>
        internal static bool IsInsideLightPaletteWGroup()
        {
            if (_currentPalette == null)
            {
                return false;
            }

            Color bg = _currentPalette.Value.BackgroundColor;
            float luminance = 0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b;
            return luminance > 0.5f;
        }

        /// <summary>
        /// Returns true if there is an active WGroup palette with a dark background
        /// (luminance <= 0.5), indicating child elements should use dark theming.
        /// </summary>
        internal static bool IsInsideDarkPaletteWGroup()
        {
            if (_currentPalette == null)
            {
                return false;
            }

            Color bg = _currentPalette.Value.BackgroundColor;
            float luminance = 0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b;
            return luminance <= 0.5f;
        }

        /// <summary>
        /// Gets the appropriate row background color considering the current WGroup palette.
        /// When inside a WGroup, the color is derived from the palette's background luminance.
        /// Otherwise, uses Unity's editor skin (Pro = dark, Personal = light).
        /// </summary>
        internal static Color GetThemedRowColor(Color lightRowColor, Color darkRowColor)
        {
            if (_currentPalette == null)
            {
                return EditorGUIUtility.isProSkin ? darkRowColor : lightRowColor;
            }

            Color bg = _currentPalette.Value.BackgroundColor;
            float luminance = 0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b;

            // Light palette background -> use light row colors
            // Dark palette background -> use dark row colors
            return luminance > 0.5f ? lightRowColor : darkRowColor;
        }

        /// <summary>
        /// Gets the appropriate selection/highlight color considering the current WGroup palette.
        /// </summary>
        internal static Color GetThemedSelectionColor(
            Color lightSelectionColor,
            Color darkSelectionColor
        )
        {
            if (_currentPalette == null)
            {
                return EditorGUIUtility.isProSkin ? darkSelectionColor : lightSelectionColor;
            }

            Color bg = _currentPalette.Value.BackgroundColor;
            float luminance = 0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b;
            return luminance > 0.5f ? lightSelectionColor : darkSelectionColor;
        }

        /// <summary>
        /// Gets the appropriate border color considering the current WGroup palette.
        /// </summary>
        internal static Color GetThemedBorderColor(Color lightBorderColor, Color darkBorderColor)
        {
            if (_currentPalette == null)
            {
                return EditorGUIUtility.isProSkin ? darkBorderColor : lightBorderColor;
            }

            Color bg = _currentPalette.Value.BackgroundColor;
            float luminance = 0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b;
            return luminance > 0.5f ? lightBorderColor : darkBorderColor;
        }

        /// <summary>
        /// Gets the appropriate background color for pending entry sections
        /// considering the current WGroup palette.
        /// </summary>
        internal static Color GetThemedPendingBackgroundColor(
            Color lightBackgroundColor,
            Color darkBackgroundColor
        )
        {
            if (_currentPalette == null)
            {
                return EditorGUIUtility.isProSkin ? darkBackgroundColor : lightBackgroundColor;
            }

            Color bg = _currentPalette.Value.BackgroundColor;
            float luminance = 0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b;
            return luminance > 0.5f ? lightBackgroundColor : darkBackgroundColor;
        }

        /// <summary>
        /// Gets a palette-derived row background color for alternating rows in collection drawers.
        /// When inside a WGroup, this creates a subtle variation of the palette background color.
        /// </summary>
        /// <param name="rowIndex">Row index for alternation (0-based).</param>
        /// <returns>Palette-derived color for the row, or default based on editor skin if not in WGroup.</returns>
        internal static Color GetPaletteDerivedRowColor(int rowIndex)
        {
            if (_currentPalette == null)
            {
                // Fallback to default colors when not in WGroup
                Color defaultLight = new(0.88f, 0.88f, 0.88f, 1f);
                Color defaultDark = new(0.22f, 0.22f, 0.22f, 1f);
                Color baseColor = EditorGUIUtility.isProSkin ? defaultDark : defaultLight;
                // Slight variation for alternating rows
                return rowIndex % 2 == 0 ? baseColor : Color.Lerp(baseColor, Color.black, 0.05f);
            }

            Color bg = _currentPalette.Value.BackgroundColor;
            float luminance = 0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b;

            // Create subtle variations of the palette background for rows
            Color rowBase =
                rowIndex % 2 == 0
                    ? bg
                    : Color.Lerp(bg, luminance > 0.5f ? Color.black : Color.white, 0.05f);

            return rowBase;
        }

        /// <summary>
        /// Gets the current palette's background color, or a default color if not in a WGroup.
        /// </summary>
        internal static Color GetPaletteBackgroundColor()
        {
            if (_currentPalette == null)
            {
                return EditorGUIUtility.isProSkin
                    ? new Color(0.22f, 0.22f, 0.22f, 1f)
                    : new Color(0.88f, 0.88f, 0.88f, 1f);
            }

            return _currentPalette.Value.BackgroundColor;
        }

        /// <summary>
        /// Gets the current palette's text color, or a default color if not in a WGroup.
        /// </summary>
        internal static Color GetPaletteTextColor()
        {
            if (_currentPalette == null)
            {
                return EditorGUIUtility.isProSkin
                    ? new Color(0.9f, 0.9f, 0.9f, 1f)
                    : new Color(0.1f, 0.1f, 0.1f, 1f);
            }

            return _currentPalette.Value.TextColor;
        }

        /// <summary>
        /// Gets a palette-derived border color for collection containers.
        /// </summary>
        internal static Color GetPaletteDerivedBorderColor()
        {
            if (_currentPalette == null)
            {
                return EditorGUIUtility.isProSkin
                    ? new Color(0.15f, 0.15f, 0.15f, 1f)
                    : new Color(0.6f, 0.6f, 0.6f, 1f);
            }

            Color bg = _currentPalette.Value.BackgroundColor;
            float luminance = 0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b;

            // Border is a contrasting shade of the palette background
            return luminance > 0.5f
                ? Color.Lerp(bg, Color.black, 0.3f)
                : Color.Lerp(bg, Color.white, 0.3f);
        }

        /// <summary>
        /// Determines if the current context should use "light theme" styling.
        /// Returns true if inside a light-background WGroup, or if Unity skin is Personal (light).
        /// </summary>
        internal static bool ShouldUseLightThemeStyling()
        {
            if (_currentPalette != null)
            {
                Color bg = _currentPalette.Value.BackgroundColor;
                float luminance = 0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b;
                return luminance > 0.5f;
            }

            return !EditorGUIUtility.isProSkin;
        }
    }
#endif
}
