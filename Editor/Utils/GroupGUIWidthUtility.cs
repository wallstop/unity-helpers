namespace WallstopStudios.UnityHelpers.Editor.Utils
{
#if UNITY_EDITOR
    using System;
    using System.Runtime.CompilerServices;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils.WGroup;

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

        /// <summary>
        /// Creates a scope that temporarily exits WGroup theming context.
        /// Within this scope, IsInsideWGroup will return false and GUI colors will be reset to defaults.
        /// Use this for complex property drawers (like SerializableDictionary/SerializableHashSet)
        /// that have their own theming and don't work well with WGroup palette overrides.
        /// </summary>
        internal static IDisposable ExitWGroupTheming()
        {
            return new ExitWGroupThemingScope();
        }

        /// <summary>
        /// Saved state for a GUIStyle, used to restore EditorStyles after exiting WGroup theming.
        /// </summary>
        private struct StyleState
        {
            public Texture2D NormalBackground;
            public Texture2D FocusedBackground;
            public Texture2D ActiveBackground;
            public Texture2D HoverBackground;
            public Texture2D OnNormalBackground;
            public Texture2D OnFocusedBackground;
            public Texture2D OnActiveBackground;
            public Texture2D OnHoverBackground;
            public Color NormalTextColor;
            public Color FocusedTextColor;
            public Color ActiveTextColor;
            public Color HoverTextColor;
            public Color OnNormalTextColor;
            public Color OnFocusedTextColor;
            public Color OnActiveTextColor;
            public Color OnHoverTextColor;
            public bool IsValid;
        }

        private static StyleState SaveFullStyleState(GUIStyle style)
        {
            if (style == null)
            {
                return default;
            }

            return new StyleState
            {
                NormalBackground = style.normal.background,
                FocusedBackground = style.focused.background,
                ActiveBackground = style.active.background,
                HoverBackground = style.hover.background,
                OnNormalBackground = style.onNormal.background,
                OnFocusedBackground = style.onFocused.background,
                OnActiveBackground = style.onActive.background,
                OnHoverBackground = style.onHover.background,
                NormalTextColor = style.normal.textColor,
                FocusedTextColor = style.focused.textColor,
                ActiveTextColor = style.active.textColor,
                HoverTextColor = style.hover.textColor,
                OnNormalTextColor = style.onNormal.textColor,
                OnFocusedTextColor = style.onFocused.textColor,
                OnActiveTextColor = style.onActive.textColor,
                OnHoverTextColor = style.onHover.textColor,
                IsValid = true,
            };
        }

        private static void RestoreFullStyleState(GUIStyle style, StyleState saved)
        {
            if (style == null || !saved.IsValid)
            {
                return;
            }

            style.normal.background = saved.NormalBackground;
            style.focused.background = saved.FocusedBackground;
            style.active.background = saved.ActiveBackground;
            style.hover.background = saved.HoverBackground;
            style.onNormal.background = saved.OnNormalBackground;
            style.onFocused.background = saved.OnFocusedBackground;
            style.onActive.background = saved.OnActiveBackground;
            style.onHover.background = saved.OnHoverBackground;
            style.normal.textColor = saved.NormalTextColor;
            style.focused.textColor = saved.FocusedTextColor;
            style.active.textColor = saved.ActiveTextColor;
            style.hover.textColor = saved.HoverTextColor;
            style.onNormal.textColor = saved.OnNormalTextColor;
            style.onFocused.textColor = saved.OnFocusedTextColor;
            style.onActive.textColor = saved.OnActiveTextColor;
            style.onHover.textColor = saved.OnHoverTextColor;
        }

        private sealed class ExitWGroupThemingScope : IDisposable
        {
            private readonly UnityHelpersSettings.WGroupPaletteEntry? _previousPalette;
            private readonly bool _previousIsInsideWGroupPropertyDraw;
            private readonly Color _previousContentColor;
            private readonly Color _previousColor;
            private readonly Color _previousBackgroundColor;

            // Saved EditorStyles states - WGroupColorScope modifies these globally
            private readonly StyleState _savedTextField;
            private readonly StyleState _savedNumberField;
            private readonly StyleState _savedObjectField;
            private readonly StyleState _savedPopup;
            private readonly StyleState _savedHelpBox;
            private readonly StyleState _savedFoldout;
            private readonly StyleState _savedLabel;
            private readonly StyleState _savedToggle;
            private readonly StyleState _savedMiniButton;
            private readonly StyleState _savedMiniButtonLeft;
            private readonly StyleState _savedMiniButtonMid;
            private readonly StyleState _savedMiniButtonRight;

            private bool _disposed;

            internal ExitWGroupThemingScope()
            {
                _previousPalette = _currentPalette;
                _previousIsInsideWGroupPropertyDraw = _isInsideWGroupPropertyDraw;
                _previousContentColor = GUI.contentColor;
                _previousColor = GUI.color;
                _previousBackgroundColor = GUI.backgroundColor;

                // Save current EditorStyles state (which may have been modified by WGroupColorScope)
                _savedTextField = SaveFullStyleState(EditorStyles.textField);
                _savedNumberField = SaveFullStyleState(EditorStyles.numberField);
                _savedObjectField = SaveFullStyleState(EditorStyles.objectField);
                _savedPopup = SaveFullStyleState(EditorStyles.popup);
                _savedHelpBox = SaveFullStyleState(EditorStyles.helpBox);
                _savedFoldout = SaveFullStyleState(EditorStyles.foldout);
                _savedLabel = SaveFullStyleState(EditorStyles.label);
                _savedToggle = SaveFullStyleState(EditorStyles.toggle);
                _savedMiniButton = SaveFullStyleState(EditorStyles.miniButton);
                _savedMiniButtonLeft = SaveFullStyleState(EditorStyles.miniButtonLeft);
                _savedMiniButtonMid = SaveFullStyleState(EditorStyles.miniButtonMid);
                _savedMiniButtonRight = SaveFullStyleState(EditorStyles.miniButtonRight);

                // Clear WGroup context
                _currentPalette = null;
                _isInsideWGroupPropertyDraw = false;

                // Reset GUI colors to defaults
                GUI.contentColor = Color.white;
                GUI.color = Color.white;
                GUI.backgroundColor = Color.white;

                // Reset EditorStyles backgrounds to null so Unity uses its internal defaults.
                // We only clear backgrounds (not text colors) because:
                // 1. WGroupColorScope sets custom background textures that need to be removed
                // 2. Text colors set to default(Color) can make text invisible in some contexts
                // 3. The saved state will restore everything properly on dispose anyway
                ResetStyleBackgrounds(EditorStyles.textField);
                ResetStyleBackgrounds(EditorStyles.numberField);
                ResetStyleBackgrounds(EditorStyles.objectField);
                ResetStyleBackgrounds(EditorStyles.popup);
                ResetStyleBackgrounds(EditorStyles.helpBox);
                // For text-only styles (foldout, label, toggle, miniButton variants),
                // we don't reset anything - just rely on save/restore
            }

            private static void ResetStyleBackgrounds(GUIStyle style)
            {
                if (style == null)
                {
                    return;
                }

                // Clear any custom backgrounds - Unity will use its internal defaults
                style.normal.background = null;
                style.focused.background = null;
                style.active.background = null;
                style.hover.background = null;
                style.onNormal.background = null;
                style.onFocused.background = null;
                style.onActive.background = null;
                style.onHover.background = null;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                // Restore EditorStyles to their previous state (with WGroup theming if applicable)
                RestoreFullStyleState(EditorStyles.textField, _savedTextField);
                RestoreFullStyleState(EditorStyles.numberField, _savedNumberField);
                RestoreFullStyleState(EditorStyles.objectField, _savedObjectField);
                RestoreFullStyleState(EditorStyles.popup, _savedPopup);
                RestoreFullStyleState(EditorStyles.helpBox, _savedHelpBox);
                RestoreFullStyleState(EditorStyles.foldout, _savedFoldout);
                RestoreFullStyleState(EditorStyles.label, _savedLabel);
                RestoreFullStyleState(EditorStyles.toggle, _savedToggle);
                RestoreFullStyleState(EditorStyles.miniButton, _savedMiniButton);
                RestoreFullStyleState(EditorStyles.miniButtonLeft, _savedMiniButtonLeft);
                RestoreFullStyleState(EditorStyles.miniButtonMid, _savedMiniButtonMid);
                RestoreFullStyleState(EditorStyles.miniButtonRight, _savedMiniButtonRight);

                // Restore WGroup context
                _currentPalette = _previousPalette;
                _isInsideWGroupPropertyDraw = _previousIsInsideWGroupPropertyDraw;

                // Restore GUI colors
                GUI.contentColor = _previousContentColor;
                GUI.color = _previousColor;
                GUI.backgroundColor = _previousBackgroundColor;
            }
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
        /// Gets the palette-aware row color. When inside a WGroup:
        /// - Returns the palette's explicit RowColor if set
        /// - Otherwise derives an appropriate row color from the palette's BackgroundColor
        /// When not inside a WGroup, uses the provided fallback colors based on editor skin.
        /// </summary>
        /// <param name="fallbackLight">Color to use for light themes when not in WGroup context.</param>
        /// <param name="fallbackDark">Color to use for dark themes when not in WGroup context.</param>
        /// <returns>The appropriate row color for the current context.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Color GetPaletteRowColor(Color fallbackLight, Color fallbackDark)
        {
            if (_currentPalette.HasValue)
            {
                UnityHelpersSettings.WGroupPaletteEntry palette = _currentPalette.Value;
                return WGroupColorDerivation.GetEffectiveRowColor(
                    palette.BackgroundColor,
                    palette.RowColor
                );
            }

            return EditorGUIUtility.isProSkin ? fallbackDark : fallbackLight;
        }

        /// <summary>
        /// Gets the palette-aware alternate row color. When inside a WGroup:
        /// - Returns the palette's explicit AlternateRowColor if set
        /// - Otherwise derives an appropriate alternate row color from the palette's BackgroundColor
        /// When not inside a WGroup, uses the provided fallback colors based on editor skin.
        /// </summary>
        /// <param name="fallbackLight">Color to use for light themes when not in WGroup context.</param>
        /// <param name="fallbackDark">Color to use for dark themes when not in WGroup context.</param>
        /// <returns>The appropriate alternate row color for the current context.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Color GetPaletteAlternateRowColor(Color fallbackLight, Color fallbackDark)
        {
            if (_currentPalette.HasValue)
            {
                UnityHelpersSettings.WGroupPaletteEntry palette = _currentPalette.Value;
                return WGroupColorDerivation.GetEffectiveAlternateRowColor(
                    palette.BackgroundColor,
                    palette.AlternateRowColor
                );
            }

            return EditorGUIUtility.isProSkin ? fallbackDark : fallbackLight;
        }

        /// <summary>
        /// Gets the palette-aware selection/hover color. When inside a WGroup:
        /// - Returns the palette's explicit SelectionColor if set
        /// - Otherwise derives an appropriate selection color from the palette's BackgroundColor
        /// When not inside a WGroup, uses the provided fallback colors based on editor skin.
        /// </summary>
        /// <param name="fallbackLight">Color to use for light themes when not in WGroup context.</param>
        /// <param name="fallbackDark">Color to use for dark themes when not in WGroup context.</param>
        /// <returns>The appropriate selection color for the current context.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Color GetPaletteSelectionColor(Color fallbackLight, Color fallbackDark)
        {
            if (_currentPalette.HasValue)
            {
                UnityHelpersSettings.WGroupPaletteEntry palette = _currentPalette.Value;
                return WGroupColorDerivation.GetEffectiveSelectionColor(
                    palette.BackgroundColor,
                    palette.SelectionColor
                );
            }

            return EditorGUIUtility.isProSkin ? fallbackDark : fallbackLight;
        }

        /// <summary>
        /// Gets the palette-aware border color. When inside a WGroup:
        /// - Returns the palette's explicit BorderColor if set
        /// - Otherwise derives an appropriate border color from the palette's BackgroundColor
        /// When not inside a WGroup, uses the provided fallback colors based on editor skin.
        /// </summary>
        /// <param name="fallbackLight">Color to use for light themes when not in WGroup context.</param>
        /// <param name="fallbackDark">Color to use for dark themes when not in WGroup context.</param>
        /// <returns>The appropriate border color for the current context.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Color GetPaletteBorderColor(Color fallbackLight, Color fallbackDark)
        {
            if (_currentPalette.HasValue)
            {
                UnityHelpersSettings.WGroupPaletteEntry palette = _currentPalette.Value;
                return WGroupColorDerivation.GetEffectiveBorderColor(
                    palette.BackgroundColor,
                    palette.BorderColor
                );
            }

            return EditorGUIUtility.isProSkin ? fallbackDark : fallbackLight;
        }

        /// <summary>
        /// Gets the palette-aware pending background color. When inside a WGroup:
        /// - Returns the palette's explicit PendingBackgroundColor if set
        /// - Otherwise derives an appropriate pending background color from the palette's BackgroundColor
        /// When not inside a WGroup, uses the provided fallback colors based on editor skin.
        /// </summary>
        /// <param name="fallbackLight">Color to use for light themes when not in WGroup context.</param>
        /// <param name="fallbackDark">Color to use for dark themes when not in WGroup context.</param>
        /// <returns>The appropriate pending background color for the current context.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Color GetPalettePendingBackgroundColor(
            Color fallbackLight,
            Color fallbackDark
        )
        {
            if (_currentPalette.HasValue)
            {
                UnityHelpersSettings.WGroupPaletteEntry palette = _currentPalette.Value;
                return WGroupColorDerivation.GetEffectivePendingBackgroundColor(
                    palette.BackgroundColor,
                    palette.PendingBackgroundColor
                );
            }

            return EditorGUIUtility.isProSkin ? fallbackDark : fallbackLight;
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
