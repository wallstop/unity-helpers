namespace WallstopStudios.UnityHelpers.Editor.Utils.WGroup
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils;

    /// <summary>
    /// Diagnostics helper for debugging WGroup indentation issues.
    /// </summary>
    /// <remarks>
    /// Enable logging by setting <see cref="Enabled"/> to true. Logs are written to the Unity console
    /// with the prefix "[WGroupIndent]" for easy filtering.
    /// </remarks>
    internal static class WGroupIndentDiagnostics
    {
        /// <summary>
        /// When true, enables diagnostic logging for WGroup padding operations.
        /// </summary>
        internal static bool Enabled { get; set; }

        /// <summary>
        /// When set, only logs for groups matching this name (substring match).
        /// Leave null to log all groups.
        /// </summary>
        internal static string GroupNameFilter { get; set; }

        private const string LogPrefix = "[WGroupIndent] ";

        private static bool ShouldLog(string groupName)
        {
            if (!Enabled)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(GroupNameFilter))
            {
                if (string.IsNullOrEmpty(groupName))
                {
                    return false;
                }

                if (
                    groupName.IndexOf(GroupNameFilter, System.StringComparison.OrdinalIgnoreCase)
                    < 0
                )
                {
                    return false;
                }
            }

            return true;
        }

        internal static void LogPushPadding(
            string groupName,
            float horizontalPadding,
            float leftPadding,
            float rightPadding,
            int indentLevel
        )
        {
            if (!ShouldLog(groupName))
            {
                return;
            }

            Debug.Log(
                $"{LogPrefix}PushPadding: group={groupName ?? "(null)"}, "
                    + $"horizontal={horizontalPadding:F2}, left={leftPadding:F2}, right={rightPadding:F2}, "
                    + $"indentLevel={indentLevel}, "
                    + $"beforePush: totalLeft={GroupGUIWidthUtility.CurrentLeftPadding:F2}, "
                    + $"totalRight={GroupGUIWidthUtility.CurrentRightPadding:F2}, "
                    + $"depth={GroupGUIWidthUtility.CurrentScopeDepth}"
            );
        }

        internal static void LogAfterPush(string groupName)
        {
            if (!ShouldLog(groupName))
            {
                return;
            }

            Debug.Log(
                $"{LogPrefix}AfterPush: group={groupName ?? "(null)"}, "
                    + $"totalLeft={GroupGUIWidthUtility.CurrentLeftPadding:F2}, "
                    + $"totalRight={GroupGUIWidthUtility.CurrentRightPadding:F2}, "
                    + $"depth={GroupGUIWidthUtility.CurrentScopeDepth}"
            );
        }

        internal static void LogAfterPop(string groupName)
        {
            if (!ShouldLog(groupName))
            {
                return;
            }

            Debug.Log(
                $"{LogPrefix}AfterPop: group={groupName ?? "(null)"}, "
                    + $"totalLeft={GroupGUIWidthUtility.CurrentLeftPadding:F2}, "
                    + $"totalRight={GroupGUIWidthUtility.CurrentRightPadding:F2}, "
                    + $"depth={GroupGUIWidthUtility.CurrentScopeDepth}"
            );
        }

        internal static void LogDrawProperty(string groupName, string propertyPath, int indentLevel)
        {
            if (!ShouldLog(groupName))
            {
                return;
            }

            Debug.Log(
                $"{LogPrefix}DrawProperty: group={groupName ?? "(null)"}, "
                    + $"property={propertyPath}, indentLevel={indentLevel}, "
                    + $"totalLeft={GroupGUIWidthUtility.CurrentLeftPadding:F2}, "
                    + $"totalRight={GroupGUIWidthUtility.CurrentRightPadding:F2}"
            );
        }
    }

    internal static class WGroupGUI
    {
        internal delegate bool PropertyOverride(
            SerializedObject owner,
            SerializedProperty property
        );

        /// <summary>
        /// Draws a group using a pre-built property lookup to avoid FindProperty allocations.
        /// </summary>
        internal static void DrawGroup(
            WGroupDefinition definition,
            SerializedObject serializedObject,
            Dictionary<int, bool> foldoutStates,
            IReadOnlyDictionary<string, SerializedProperty> propertyLookup,
            PropertyOverride overrideDrawer = null
        )
        {
            DrawGroupInternal(
                definition,
                serializedObject,
                foldoutStates,
                propertyLookup,
                overrideDrawer
            );
        }

        internal static void DrawGroup(
            WGroupDefinition definition,
            SerializedObject serializedObject,
            Dictionary<int, bool> foldoutStates,
            PropertyOverride overrideDrawer = null
        )
        {
            DrawGroupInternal(definition, serializedObject, foldoutStates, null, overrideDrawer);
        }

        private static void DrawGroupInternal(
            WGroupDefinition definition,
            SerializedObject serializedObject,
            Dictionary<int, bool> foldoutStates,
            IReadOnlyDictionary<string, SerializedProperty> propertyLookup,
            PropertyOverride overrideDrawer
        )
        {
            if (definition == null || serializedObject == null)
            {
                return;
            }

            UnityHelpersSettings.WGroupPaletteEntry palette =
                UnityHelpersSettings.ResolveWGroupPalette(definition.ColorKey);

            Rect scopeRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (Event.current.type == EventType.Repaint)
            {
                Rect backgroundRect = scopeRect;
                backgroundRect.xMin += 2f;
                backgroundRect.xMax -= 2f;
                backgroundRect.yMin += 2f;
                backgroundRect.yMax -= 2f;
                EditorGUI.DrawRect(backgroundRect, palette.BackgroundColor);
            }

            {
                bool expanded = true;
                bool allowHeader = !definition.HideHeader;
                bool headerHasFoldout = HeaderHasFoldout(definition);
                if (headerHasFoldout)
                {
                    expanded = DrawFoldoutHeader(definition, palette, foldoutStates);
                }
                else if (allowHeader)
                {
                    DrawHeader(definition.DisplayName, palette);
                }

                // When foldout animation is enabled, use fade group for smooth transitions
                if (headerHasFoldout && UnityHelpersSettings.ShouldTweenWGroupFoldouts())
                {
                    float fade = WGroupAnimationState.GetFadeProgress(definition, expanded);
                    bool visible = EditorGUILayout.BeginFadeGroup(fade);
                    if (visible)
                    {
                        DrawGroupContent(
                            definition,
                            serializedObject,
                            foldoutStates,
                            propertyLookup,
                            overrideDrawer,
                            allowHeader,
                            palette
                        );
                    }
                    EditorGUILayout.EndFadeGroup();
                }
                else if (expanded)
                {
                    DrawGroupContent(
                        definition,
                        serializedObject,
                        foldoutStates,
                        propertyLookup,
                        overrideDrawer,
                        allowHeader,
                        palette
                    );
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
        }

        private static bool DrawFoldoutHeader(
            WGroupDefinition definition,
            UnityHelpersSettings.WGroupPaletteEntry palette,
            Dictionary<int, bool> foldoutStates
        )
        {
            int key = Objects.HashCode(definition.Name, definition.AnchorPropertyPath);
            if (foldoutStates != null && foldoutStates.TryGetValue(key, out bool expanded))
            {
                // value already loaded
            }
            else
            {
                expanded = !definition.StartCollapsed;
            }

            GUIStyle foldoutStyle = WGroupStyles.GetFoldoutStyle(palette.TextColor);
            float headerHeight = WGroupStyles.GetHeaderHeight(palette.TextColor);
            Rect headerRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                foldoutStyle,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(headerHeight)
            );

            WGroupStyles.DrawHeaderBackground(headerRect, palette.BackgroundColor);
            bool headerHasFoldout = HeaderHasFoldout(definition);
            Rect foldoutRect = WGroupHeaderVisualUtility.GetContentRect(
                headerRect,
                WGroupStyles.HeaderTopPadding,
                WGroupStyles.HeaderBottomPadding,
                headerHasFoldout
            );

            int originalIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            expanded = EditorGUI.Foldout(
                foldoutRect,
                expanded,
                definition.DisplayName,
                true,
                foldoutStyle
            );
            EditorGUI.indentLevel = originalIndent;

            WGroupStyles.DrawHeaderBorder(headerRect, palette.BackgroundColor);

            if (foldoutStates != null)
            {
                foldoutStates[key] = expanded;
            }

            GUILayout.Space(2f);
            return expanded;
        }

        private static Rect DrawHeader(
            string displayName,
            UnityHelpersSettings.WGroupPaletteEntry palette
        )
        {
            if (string.IsNullOrEmpty(displayName))
            {
                return Rect.zero;
            }

            GUIContent content = EditorGUIUtility.TrTextContent(displayName);
            GUIStyle labelStyle = WGroupStyles.GetHeaderLabelStyle(palette.TextColor);
            float headerHeight = WGroupStyles.GetHeaderHeight(palette.TextColor);
            Rect labelRect = GUILayoutUtility.GetRect(
                content,
                labelStyle,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(headerHeight)
            );
            WGroupStyles.DrawHeaderBackground(labelRect, palette.BackgroundColor);
            Rect contentRect = WGroupHeaderVisualUtility.GetContentRect(
                labelRect,
                WGroupStyles.HeaderTopPadding,
                WGroupStyles.HeaderBottomPadding
            );
            GUI.Label(contentRect, content, labelStyle);
            WGroupStyles.DrawHeaderBorder(labelRect, palette.BackgroundColor);
            GUILayout.Space(2f);
            return labelRect;
        }

        private static void DrawGroupContent(
            WGroupDefinition definition,
            SerializedObject serializedObject,
            Dictionary<int, bool> foldoutStates,
            IReadOnlyDictionary<string, SerializedProperty> propertyLookup,
            PropertyOverride overrideDrawer,
            bool allowHeader,
            UnityHelpersSettings.WGroupPaletteEntry palette
        )
        {
            if (allowHeader)
            {
                EditorGUILayout.Space(2f);
            }
            EditorGUI.indentLevel++;

            float horizontalPadding = GroupGUIWidthUtility.CalculateHorizontalPadding(
                EditorStyles.helpBox,
                out float leftPadding,
                out float rightPadding
            );

            WGroupIndentDiagnostics.LogPushPadding(
                definition?.Name,
                horizontalPadding,
                leftPadding,
                rightPadding,
                EditorGUI.indentLevel
            );

            using (
                GroupGUIWidthUtility.PushContentPadding(
                    horizontalPadding,
                    leftPadding,
                    rightPadding
                )
            )
            {
                WGroupIndentDiagnostics.LogAfterPush(definition?.Name);

                IReadOnlyList<string> propertyPaths = definition.PropertyPaths;
                int propertyCount = propertyPaths.Count;
                if (propertyCount > 0)
                {
                    AddContentPadding();
                }

                // Build lookup of child groups by their anchor path
                Dictionary<string, WGroupDefinition> childByAnchor = null;
                if (definition.ChildGroups.Count > 0)
                {
                    childByAnchor = new Dictionary<string, WGroupDefinition>(
                        StringComparer.Ordinal
                    );
                    foreach (WGroupDefinition child in definition.ChildGroups)
                    {
                        childByAnchor[child.AnchorPropertyPath] = child;
                    }
                }

                // Push the palette onto the stack so child drawers can access it for consistent theming
                using (GroupGUIWidthUtility.PushWGroupPalette(palette))
                // Apply comprehensive color overrides for cross-theme palette support
                using (var colorScope = new WGroupColorScope(palette))
                {
                    for (int index = 0; index < propertyCount; index++)
                    {
                        string propertyPath = propertyPaths[index];

                        // Check if this is a child group anchor
                        if (
                            childByAnchor != null
                            && childByAnchor.TryGetValue(
                                propertyPath,
                                out WGroupDefinition childGroup
                            )
                        )
                        {
                            // Render child group recursively
                            DrawGroup(
                                childGroup,
                                serializedObject,
                                foldoutStates,
                                propertyLookup,
                                overrideDrawer
                            );
                            continue;
                        }

                        SerializedProperty property = ResolveProperty(
                            serializedObject,
                            propertyPath,
                            propertyLookup
                        );
                        if (property == null)
                        {
                            continue;
                        }

                        WGroupIndentDiagnostics.LogDrawProperty(
                            definition?.Name,
                            propertyPath,
                            EditorGUI.indentLevel
                        );

                        if (overrideDrawer != null && overrideDrawer(serializedObject, property))
                        {
                            continue;
                        }

                        // All properties are drawn within a WGroup property context.
                        // Custom drawers can check GroupGUIWidthUtility.IsInsideWGroupPropertyDraw
                        // to detect they're inside a WGroup and adjust their layout accordingly.
                        // Simple properties use indent compensation; complex properties with custom
                        // drawers can handle their own layout knowing they're in WGroup context.
                        using (GroupGUIWidthUtility.PushWGroupPropertyContext())
                        {
                            if (property.hasVisibleChildren)
                            {
                                colorScope.DrawPropertyFieldWithBackground(property, true);
                            }
                            else
                            {
                                GroupGUIIndentUtility.ExecuteWithIndentCompensation(() =>
                                    colorScope.DrawPropertyFieldWithBackground(property, true)
                                );
                            }
                        }
                    }
                }

                if (propertyCount > 0)
                {
                    AddContentPadding();
                }
            }

            WGroupIndentDiagnostics.LogAfterPop(definition?.Name);
            EditorGUI.indentLevel--;
        }

        private static bool HeaderHasFoldout(WGroupDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            if (definition.HideHeader)
            {
                return false;
            }

            return definition.Collapsible;
        }

        private static void AddContentPadding()
        {
            float spacing = Mathf.Max(1f, EditorGUIUtility.standardVerticalSpacing);
            GUILayout.Space(spacing);
        }

        private static SerializedProperty ResolveProperty(
            SerializedObject serializedObject,
            string propertyPath,
            IReadOnlyDictionary<string, SerializedProperty> propertyLookup
        )
        {
            if (
                propertyLookup != null
                && propertyLookup.TryGetValue(propertyPath, out SerializedProperty cached)
            )
            {
                return cached;
            }

            return serializedObject.FindProperty(propertyPath);
        }
    }

    internal static class WGroupStyles
    {
        internal const float HeaderTopPadding = 1f;
        internal const float HeaderBottomPadding = 1f;
        internal const float HeaderVerticalPadding = 4f;
        private static readonly Dictionary<Color, GUIStyle> FoldoutStyles = new();
        private static readonly Dictionary<Color, GUIStyle> HeaderStyles = new();

        internal static float GetHeaderHeight(Color textColor)
        {
            GUIStyle foldoutStyle = GetFoldoutStyle(textColor);
            GUIStyle headerStyle = GetHeaderLabelStyle(textColor);

            float foldoutLineHeight = Mathf.Max(
                EditorGUIUtility.singleLineHeight,
                foldoutStyle.lineHeight
            );
            float headerLineHeight = Mathf.Max(
                EditorGUIUtility.singleLineHeight,
                headerStyle.lineHeight
            );

            float tallestLineHeight = Mathf.Max(foldoutLineHeight, headerLineHeight);
            return tallestLineHeight + HeaderVerticalPadding;
        }

        internal static GUIStyle GetFoldoutStyle(Color textColor)
        {
            if (!FoldoutStyles.TryGetValue(textColor, out GUIStyle style))
            {
                style = new GUIStyle(EditorStyles.foldoutHeader)
                {
                    fontStyle = FontStyle.Bold,
                    padding = new RectOffset(16, 6, 3, 3),
                    normal = { textColor = textColor },
                    onNormal = { textColor = textColor },
                    active = { textColor = textColor },
                    onActive = { textColor = textColor },
                    focused = { textColor = textColor },
                    onFocused = { textColor = textColor },
                };
                FoldoutStyles[textColor] = style;
            }
            return style;
        }

        internal static GUIStyle GetHeaderLabelStyle(Color textColor)
        {
            if (!HeaderStyles.TryGetValue(textColor, out GUIStyle style))
            {
                style = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(4, 4, 0, 0),
                    normal = { textColor = textColor },
                    active = { textColor = textColor },
                    focused = { textColor = textColor },
                };
                HeaderStyles[textColor] = style;
            }

            return style;
        }

        internal static void DrawHeaderBackground(Rect rect, Color baseColor)
        {
            WGroupHeaderVisualUtility.DrawHeaderBackground(rect, baseColor);
        }

        internal static void DrawHeaderBorder(Rect rect, Color baseColor)
        {
            WGroupHeaderVisualUtility.DrawHeaderBorder(rect, baseColor);
        }
    }

    /// <summary>
    /// Disposable scope that applies comprehensive GUI color overrides for cross-theme palette support.
    /// </summary>
    /// <remarks>
    /// When a WGroup uses a palette that conflicts with the editor theme (e.g., light palette on dark editor),
    /// this scope overrides GUI.contentColor and temporarily modifies EditorStyles during property drawing
    /// to achieve true cross-theme rendering. Style modifications are applied for the entire scope duration
    /// to ensure all child elements (including built-in arrays, lists, etc.) receive consistent theming.
    /// </remarks>
    internal sealed class WGroupColorScope : IDisposable
    {
        // Cache for dynamically created palette textures (keyed by color)
        private static readonly Dictionary<Color, Texture2D> PaletteTextureCache = new();
        private static readonly Dictionary<Color, Texture2D> PaletteBorderTextureCache = new();

        // Cache for ReorderableList instances (keyed by serializedObject + property path)
        // Must be static to persist across frames for drag/drop to work
        private static readonly Dictionary<string, ReorderableList> ReorderableListCache = new();

        private readonly Color _previousContentColor;
        private readonly Color _previousColor;
        private readonly Color _previousBackgroundColor;
        private readonly bool _isActive;
        private readonly Color _paletteBackgroundColor;
        private readonly Color _paletteTextColor;
        private readonly Color _fieldBackgroundColor;
        private readonly bool _useLightStyles;
        private readonly Texture2D _paletteTexture;
        private readonly Texture2D _paletteBorderTexture;
        private readonly Color _fieldTextColor;
        private bool _disposed;

        // Saved style states for restoration
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

        /// <summary>
        /// Gets whether this scope is actively overriding colors (cross-theme scenario detected).
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// Gets the background color to use for input fields when in cross-theme mode.
        /// </summary>
        public Color FieldBackgroundColor => _fieldBackgroundColor;

        /// <summary>
        /// Gets the palette background color for this scope.
        /// </summary>
        public Color PaletteBackgroundColor => _paletteBackgroundColor;

        /// <summary>
        /// Gets the palette text color for this scope.
        /// </summary>
        public Color PaletteTextColor => _paletteTextColor;

        /// <summary>
        /// Creates a new color scope that applies palette-appropriate colors when cross-theme rendering is detected.
        /// </summary>
        /// <param name="palette">The WGroup palette entry containing background and text colors.</param>
        public WGroupColorScope(UnityHelpersSettings.WGroupPaletteEntry palette)
        {
            _previousContentColor = GUI.contentColor;
            _previousColor = GUI.color;
            _previousBackgroundColor = GUI.backgroundColor;

            _paletteBackgroundColor = palette.BackgroundColor;
            _paletteTextColor = palette.TextColor;
            _isActive = IsCrossThemePalette(palette.BackgroundColor);
            _fieldBackgroundColor = CalculateFieldBackgroundColor(palette.BackgroundColor);

            float bgLuminance =
                0.299f * palette.BackgroundColor.r
                + 0.587f * palette.BackgroundColor.g
                + 0.114f * palette.BackgroundColor.b;
            _useLightStyles = bgLuminance > 0.5f;

            // Create palette-colored textures for backgrounds
            _paletteTexture = GetOrCreatePaletteTexture(palette.BackgroundColor);
            _paletteBorderTexture = GetOrCreatePaletteBorderTexture(palette.BackgroundColor);
            _fieldTextColor = palette.TextColor;

            if (_isActive)
            {
                // Override content color for labels (this is safe to keep for the scope duration)
                GUI.contentColor = palette.TextColor;

                // Adjust overall GUI color for icon tinting
                GUI.color = CalculateGuiColor(palette.BackgroundColor, palette.TextColor);

                // Set background color to influence built-in controls
                GUI.backgroundColor = _fieldBackgroundColor;

                // Save and override all relevant styles for the entire scope duration
                // This ensures built-in arrays, lists, and other controls get themed correctly
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

                // Create field texture from palette colors
                Texture2D fieldTexture = GetOrCreatePaletteFieldTexture(palette.BackgroundColor);

                ApplyFullStyleOverrides(EditorStyles.textField, fieldTexture, _fieldTextColor);
                ApplyFullStyleOverrides(EditorStyles.numberField, fieldTexture, _fieldTextColor);
                ApplyFullStyleOverrides(EditorStyles.objectField, fieldTexture, _fieldTextColor);
                ApplyFullStyleOverrides(EditorStyles.popup, fieldTexture, _fieldTextColor);
                ApplyBoxStyleOverrides(
                    EditorStyles.helpBox,
                    _paletteBorderTexture,
                    _fieldTextColor
                );
                ApplyTextOnlyOverrides(EditorStyles.foldout, _fieldTextColor);
                ApplyTextOnlyOverrides(EditorStyles.label, _fieldTextColor);
                ApplyTextOnlyOverrides(EditorStyles.toggle, _fieldTextColor);
                ApplyButtonStyleOverrides(EditorStyles.miniButton, fieldTexture, _fieldTextColor);
                ApplyButtonStyleOverrides(
                    EditorStyles.miniButtonLeft,
                    fieldTexture,
                    _fieldTextColor
                );
                ApplyButtonStyleOverrides(
                    EditorStyles.miniButtonMid,
                    fieldTexture,
                    _fieldTextColor
                );
                ApplyButtonStyleOverrides(
                    EditorStyles.miniButtonRight,
                    fieldTexture,
                    _fieldTextColor
                );
            }
            else
            {
                // Initialize saved states as invalid when not active
                _savedTextField = default;
                _savedNumberField = default;
                _savedObjectField = default;
                _savedPopup = default;
                _savedHelpBox = default;
                _savedFoldout = default;
                _savedLabel = default;
                _savedToggle = default;
                _savedMiniButton = default;
                _savedMiniButtonLeft = default;
                _savedMiniButtonMid = default;
                _savedMiniButtonRight = default;
            }
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

        private static void ApplyFullStyleOverrides(
            GUIStyle style,
            Texture2D background,
            Color textColor
        )
        {
            if (style == null)
            {
                return;
            }

            style.normal.background = background;
            style.focused.background = background;
            style.active.background = background;
            style.hover.background = background;
            style.onNormal.background = background;
            style.onFocused.background = background;
            style.onActive.background = background;
            style.onHover.background = background;
            style.normal.textColor = textColor;
            style.focused.textColor = textColor;
            style.active.textColor = textColor;
            style.hover.textColor = textColor;
            style.onNormal.textColor = textColor;
            style.onFocused.textColor = textColor;
            style.onActive.textColor = textColor;
            style.onHover.textColor = textColor;
        }

        private static void ApplyBoxStyleOverrides(
            GUIStyle style,
            Texture2D background,
            Color textColor
        )
        {
            if (style == null)
            {
                return;
            }

            style.normal.background = background;
            style.normal.textColor = textColor;
        }

        private static void ApplyTextOnlyOverrides(GUIStyle style, Color textColor)
        {
            if (style == null)
            {
                return;
            }

            style.normal.textColor = textColor;
            style.focused.textColor = textColor;
            style.active.textColor = textColor;
            style.hover.textColor = textColor;
            style.onNormal.textColor = textColor;
            style.onFocused.textColor = textColor;
            style.onActive.textColor = textColor;
            style.onHover.textColor = textColor;
        }

        private static void ApplyButtonStyleOverrides(
            GUIStyle style,
            Texture2D background,
            Color textColor
        )
        {
            if (style == null)
            {
                return;
            }

            // For buttons, we want to keep some visual feedback but adjust colors
            style.normal.textColor = textColor;
            style.focused.textColor = textColor;
            style.active.textColor = textColor;
            style.hover.textColor = textColor;
            style.onNormal.textColor = textColor;
            style.onFocused.textColor = textColor;
            style.onActive.textColor = textColor;
            style.onHover.textColor = textColor;
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

        private static Texture2D GetOrCreatePaletteTexture(Color paletteColor)
        {
            if (
                PaletteTextureCache.TryGetValue(paletteColor, out Texture2D existing)
                && existing != null
            )
            {
                return existing;
            }

            // Create a slightly adjusted version of the palette color for the background
            Color fillColor = paletteColor;
            Texture2D texture = new(4, 4, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    texture.SetPixel(x, y, fillColor);
                }
            }

            texture.Apply();
            PaletteTextureCache[paletteColor] = texture;
            return texture;
        }

        private static Texture2D GetOrCreatePaletteBorderTexture(Color paletteColor)
        {
            if (
                PaletteBorderTextureCache.TryGetValue(paletteColor, out Texture2D existing)
                && existing != null
            )
            {
                return existing;
            }

            // Calculate luminance to determine if we need darker or lighter borders
            float luminance =
                0.299f * paletteColor.r + 0.587f * paletteColor.g + 0.114f * paletteColor.b;

            // Border color is a contrasting shade of the palette color
            Color borderColor =
                luminance > 0.5f
                    ? Color.Lerp(paletteColor, Color.black, 0.3f)
                    : Color.Lerp(paletteColor, Color.white, 0.3f);

            Texture2D texture = new(8, 8, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    bool isBorder = x == 0 || x == 7 || y == 0 || y == 7;
                    texture.SetPixel(x, y, isBorder ? borderColor : paletteColor);
                }
            }

            texture.Apply();
            PaletteBorderTextureCache[paletteColor] = texture;
            return texture;
        }

        private static Texture2D GetOrCreatePaletteFieldTexture(Color paletteColor)
        {
            // For input fields, we want a slightly different shade to distinguish them
            float luminance =
                0.299f * paletteColor.r + 0.587f * paletteColor.g + 0.114f * paletteColor.b;

            // Adjust field fill to be slightly darker/lighter than the palette background
            Color fillColor =
                luminance > 0.5f
                    ? Color.Lerp(paletteColor, Color.white, 0.15f)
                    : Color.Lerp(paletteColor, Color.black, 0.15f);

            // Border is more contrasting
            Color borderColor =
                luminance > 0.5f
                    ? Color.Lerp(paletteColor, Color.black, 0.25f)
                    : Color.Lerp(paletteColor, Color.white, 0.25f);

            // Create unique key based on adjusted fill color
            Color cacheKey = new(fillColor.r, fillColor.g, fillColor.b, 0.5f); // Use alpha to differentiate
            if (
                PaletteTextureCache.TryGetValue(cacheKey, out Texture2D existing)
                && existing != null
            )
            {
                return existing;
            }

            Texture2D texture = new(8, 8, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    bool isBorder = x == 0 || x == 7 || y == 0 || y == 7;
                    texture.SetPixel(x, y, isBorder ? borderColor : fillColor);
                }
            }

            texture.Apply();
            PaletteTextureCache[cacheKey] = texture;
            return texture;
        }

        /// <summary>
        /// Determines if the palette represents a cross-theme scenario requiring color overrides.
        /// </summary>
        internal static bool IsCrossThemePalette(Color backgroundColor)
        {
            float bgLuminance =
                0.299f * backgroundColor.r
                + 0.587f * backgroundColor.g
                + 0.114f * backgroundColor.b;
            bool isLightBackground = bgLuminance > 0.5f;

            // Cross-theme: light background on dark editor, or dark background on light editor
            return isLightBackground == EditorGUIUtility.isProSkin;
        }

        /// <summary>
        /// Calculates an appropriate background color for input fields.
        /// </summary>
        internal static Color CalculateFieldBackgroundColor(Color paletteBackground)
        {
            float bgLuminance =
                0.299f * paletteBackground.r
                + 0.587f * paletteBackground.g
                + 0.114f * paletteBackground.b;

            return bgLuminance > 0.5f
                ? new Color(0.92f, 0.92f, 0.92f, 1f)
                : new Color(0.165f, 0.165f, 0.165f, 1f);
        }

        private static Color CalculateGuiColor(Color backgroundColor, Color textColor)
        {
            return Color.white;
        }

        /// <summary>
        /// Draws a property field with a themed background for complex properties
        /// (arrays, lists, etc.) whose built-in drawers use hardcoded colors.
        /// </summary>
        /// <remarks>
        /// Unity's built-in property drawers (especially for arrays/lists) use hardcoded
        /// colors based on EditorGUIUtility.isProSkin. For array properties, this method
        /// draws a custom ReorderableList with palette-themed callbacks. For other complex
        /// properties, it draws a background and overrides relevant styles.
        /// </remarks>
        public void DrawPropertyFieldWithBackground(
            SerializedProperty property,
            bool includeChildren
        )
        {
            // Determine if this property needs a themed background.
            // Arrays, lists, and other complex types with visible children need backgrounds
            // because Unity's built-in drawers use hardcoded colors.
            bool needsThemedBackground =
                _paletteTexture != null && property.hasVisibleChildren && includeChildren;

            if (!needsThemedBackground)
            {
                // Simple properties can be drawn normally - style overrides handle text/field colors
                EditorGUILayout.PropertyField(property, includeChildren);
                return;
            }

            // For array properties, use a custom drawing approach
            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                DrawThemedArrayProperty(property);
                return;
            }

            // For non-array complex properties, use the standard approach with background
            DrawThemedComplexProperty(property, includeChildren);
        }

        /// <summary>
        /// Draws an array property with fully themed colors using ReorderableList.
        /// </summary>
        private void DrawThemedArrayProperty(SerializedProperty property)
        {
            Color savedContentColor = GUI.contentColor;
            Color savedBgColor = GUI.backgroundColor;
            Color savedColor = GUI.color;

            // Draw foldout header with palette background
            Rect headerRect = EditorGUILayout.GetControlRect(
                true,
                EditorGUIUtility.singleLineHeight
            );

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(headerRect, _paletteBackgroundColor);
            }

            // Draw foldout with themed text
            GUI.contentColor = _fieldTextColor;

            GUIContent label = new(property.displayName);
            property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, label, true);

            // Draw size label on the right side of header
            Rect sizeRect = new(headerRect.xMax - 80, headerRect.y, 80, headerRect.height);
            EditorGUI.LabelField(sizeRect, $"Size: {property.arraySize}", EditorStyles.miniLabel);

            if (!property.isExpanded)
            {
                GUI.contentColor = savedContentColor;
                return;
            }

            // Get or create ReorderableList for this property
            ReorderableList reorderableList = GetOrCreateReorderableList(property);

            // Calculate list height (elements only, no footer since we draw our own)
            float listHeight = reorderableList.GetHeight();
            float footerHeight = EditorGUIUtility.singleLineHeight + 4f;

            // Reserve rect for list + our custom footer
            Rect totalRect = EditorGUILayout.GetControlRect(false, listHeight + footerHeight);
            Rect listRect = new(totalRect.x, totalRect.y, totalRect.width, listHeight);
            Rect footerRect = new(
                totalRect.x,
                totalRect.y + listHeight,
                totalRect.width,
                footerHeight
            );

            // Draw palette background for entire area
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(totalRect, _paletteBackgroundColor);
            }

            // Set colors to influence internal rendering
            GUI.backgroundColor = _paletteBackgroundColor;
            GUI.color = Color.white;

            // Override ReorderableList internal styles using reflection
            using (new ReorderableListStyleScope(_paletteTexture, _paletteBorderTexture))
            {
                // Draw the list (elements only)
                reorderableList.DoList(listRect);
            }

            // Draw our custom footer with +/- buttons
            DrawThemedArrayFooter(footerRect, reorderableList);

            GUI.contentColor = savedContentColor;
            GUI.backgroundColor = savedBgColor;
            GUI.color = savedColor;
        }

        /// <summary>
        /// Disposable scope that temporarily overrides ReorderableList's internal styles.
        /// </summary>
        private sealed class ReorderableListStyleScope : IDisposable
        {
            private static Type _defaultsType;
            private static FieldInfo _boxBackgroundField;
            private static FieldInfo _elementBackgroundField;
            private static bool _reflectionInitialized;
            private static bool _reflectionFailed;

            private GUIStyle _savedBoxBackground;
            private GUIStyle _savedElementBackground;
            private bool _isActive;

            public ReorderableListStyleScope(Texture2D paletteTexture, Texture2D borderTexture)
            {
                if (_reflectionFailed)
                {
                    return;
                }

                InitializeReflection();
                if (_reflectionFailed)
                {
                    return;
                }

                try
                {
                    // Get the defaults instance
                    object defaults = ReorderableList.defaultBehaviours;
                    if (defaults == null)
                    {
                        return;
                    }

                    // Save and override boxBackground
                    if (_boxBackgroundField != null)
                    {
                        _savedBoxBackground = _boxBackgroundField.GetValue(defaults) as GUIStyle;
                        if (_savedBoxBackground != null)
                        {
                            GUIStyle themedBox = new(_savedBoxBackground)
                            {
                                normal = { background = paletteTexture },
                            };
                            _boxBackgroundField.SetValue(defaults, themedBox);
                        }
                    }

                    // Save and override elementBackground
                    if (_elementBackgroundField != null)
                    {
                        _savedElementBackground =
                            _elementBackgroundField.GetValue(defaults) as GUIStyle;
                        if (_savedElementBackground != null)
                        {
                            GUIStyle themedElement = new(_savedElementBackground)
                            {
                                normal = { background = paletteTexture },
                            };
                            _elementBackgroundField.SetValue(defaults, themedElement);
                        }
                    }

                    _isActive = true;
                }
                catch
                {
                    _reflectionFailed = true;
                }
            }

            private static void InitializeReflection()
            {
                if (_reflectionInitialized)
                {
                    return;
                }

                _reflectionInitialized = true;

                try
                {
                    // Get the Defaults nested type
                    _defaultsType = typeof(ReorderableList).GetNestedType(
                        "Defaults",
                        BindingFlags.NonPublic | BindingFlags.Public
                    );
                    if (_defaultsType == null)
                    {
                        _reflectionFailed = true;
                        return;
                    }

                    // Get the style fields
                    _boxBackgroundField = _defaultsType.GetField(
                        "boxBackground",
                        BindingFlags.Public | BindingFlags.Instance
                    );
                    _elementBackgroundField = _defaultsType.GetField(
                        "elementBackground",
                        BindingFlags.Public | BindingFlags.Instance
                    );

                    if (_boxBackgroundField == null && _elementBackgroundField == null)
                    {
                        _reflectionFailed = true;
                    }
                }
                catch
                {
                    _reflectionFailed = true;
                }
            }

            public void Dispose()
            {
                if (!_isActive)
                {
                    return;
                }

                try
                {
                    object defaults = ReorderableList.defaultBehaviours;
                    if (defaults == null)
                    {
                        return;
                    }

                    if (_boxBackgroundField != null && _savedBoxBackground != null)
                    {
                        _boxBackgroundField.SetValue(defaults, _savedBoxBackground);
                    }

                    if (_elementBackgroundField != null && _savedElementBackground != null)
                    {
                        _elementBackgroundField.SetValue(defaults, _savedElementBackground);
                    }
                }
                catch
                {
                    // Ignore restore errors
                }
            }
        }

        /// <summary>
        /// Draws a themed footer with +/- buttons for the array.
        /// </summary>
        private void DrawThemedArrayFooter(Rect rect, ReorderableList list)
        {
            // Draw footer background
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, _paletteBackgroundColor);
            }

            // Button sizing
            float buttonWidth = 25f;
            float buttonHeight = rect.height - 4f;
            float spacing = 2f;
            float rightPadding = 4f;
            float topPadding = 2f;

            Rect addButtonRect = new(
                rect.xMax - rightPadding - buttonWidth * 2 - spacing,
                rect.y + topPadding,
                buttonWidth,
                buttonHeight
            );
            Rect removeButtonRect = new(
                rect.xMax - rightPadding - buttonWidth,
                rect.y + topPadding,
                buttonWidth,
                buttonHeight
            );

            // Create themed button style
            GUIStyle buttonStyle = new(GUI.skin.button)
            {
                normal = { background = _paletteBorderTexture, textColor = _fieldTextColor },
                hover = { background = _paletteBorderTexture, textColor = _fieldTextColor },
                active = { background = _paletteTexture, textColor = _fieldTextColor },
                focused = { background = _paletteBorderTexture, textColor = _fieldTextColor },
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };

            // Add button
            if (GUI.Button(addButtonRect, "+", buttonStyle))
            {
                SerializedProperty arrayProp = list.serializedProperty;
                if (arrayProp != null)
                {
                    arrayProp.arraySize++;
                    arrayProp.serializedObject.ApplyModifiedProperties();
                    list.index = arrayProp.arraySize - 1;
                }
            }

            // Remove button
            bool canRemove = list.index >= 0 && list.count > 0;
            using (new EditorGUI.DisabledScope(!canRemove))
            {
                if (GUI.Button(removeButtonRect, "-", buttonStyle))
                {
                    SerializedProperty arrayProp = list.serializedProperty;
                    if (arrayProp != null && list.index >= 0 && list.index < arrayProp.arraySize)
                    {
                        arrayProp.DeleteArrayElementAtIndex(list.index);
                        arrayProp.serializedObject.ApplyModifiedProperties();
                        if (list.index >= arrayProp.arraySize)
                        {
                            list.index = arrayProp.arraySize - 1;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets or creates a ReorderableList with themed callbacks for the given property.
        /// </summary>
        private ReorderableList GetOrCreateReorderableList(SerializedProperty property)
        {
            // Use target object instance ID + property path for unique cache key
            // This handles multiple objects with the same property path
            int instanceId =
                property.serializedObject.targetObject != null
                    ? property.serializedObject.targetObject.GetInstanceID()
                    : 0;
            string key = $"{instanceId}:{property.propertyPath}";

            if (
                ReorderableListCache.TryGetValue(key, out ReorderableList existing)
                && existing != null
                && existing.serializedProperty != null
                && existing.serializedProperty.serializedObject == property.serializedObject
            )
            {
                // Update the property reference in case it changed
                existing.serializedProperty = property;
                return existing;
            }

            // Create new ReorderableList - disable built-in header/footer to draw our own
            ReorderableList list = new(
                property.serializedObject,
                property,
                draggable: true,
                displayHeader: false,
                displayAddButton: false,
                displayRemoveButton: false
            );

            // IMPORTANT: Callbacks must NOT capture palette colors because:
            // 1. The ReorderableList is cached statically across frames
            // 2. Palette colors can change (different WGroups, different colorKeys)
            // Instead, callbacks query current palette via GroupGUIWidthUtility

            // Element drawing callback - use list.serializedProperty for fresh reference
            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty arrayProp = list.serializedProperty;
                if (arrayProp == null || index < 0 || index >= arrayProp.arraySize)
                {
                    return;
                }

                SerializedProperty element = arrayProp.GetArrayElementAtIndex(index);
                if (element == null)
                {
                    return;
                }

                // Offset for drag handle area (left side)
                float dragHandleWidth = 14f;
                Rect elementRect = new(
                    rect.x + dragHandleWidth,
                    rect.y + 1f,
                    rect.width - dragHandleWidth,
                    rect.height - 2f
                );

                // Query current palette colors (not captured)
                Color textColor = GroupGUIWidthUtility.GetPaletteTextColor();

                Color savedColor = GUI.contentColor;
                GUI.contentColor = textColor;
                EditorGUI.PropertyField(
                    elementRect,
                    element,
                    new GUIContent($"Element {index}"),
                    true
                );
                GUI.contentColor = savedColor;
            };

            // Element background callback - draws alternating themed rows
            list.drawElementBackgroundCallback = (
                Rect rect,
                int index,
                bool isActive,
                bool isFocused
            ) =>
            {
                if (Event.current.type != EventType.Repaint)
                {
                    return;
                }

                // Query current palette colors (not captured)
                Color bgColor = GroupGUIWidthUtility.GetPaletteBackgroundColor();
                bool useLightStyles = GroupGUIWidthUtility.ShouldUseLightThemeStyling();

                Color rowColor;
                if (isActive || isFocused)
                {
                    // Selection highlight - more visible contrast
                    rowColor = Color.Lerp(
                        bgColor,
                        useLightStyles ? Color.blue : new Color(0.3f, 0.5f, 0.8f, 1f),
                        0.3f
                    );
                }
                else if (index >= 0)
                {
                    // Alternating row colors
                    rowColor =
                        index % 2 == 0
                            ? bgColor
                            : Color.Lerp(
                                bgColor,
                                useLightStyles ? Color.black : Color.white,
                                0.05f
                            );
                }
                else
                {
                    rowColor = bgColor;
                }

                EditorGUI.DrawRect(rect, rowColor);
            };

            // Empty list callback
            list.drawNoneElementCallback = (Rect rect) =>
            {
                // Query current palette colors (not captured)
                Color bgColor = GroupGUIWidthUtility.GetPaletteBackgroundColor();
                Color textColor = GroupGUIWidthUtility.GetPaletteTextColor();

                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(rect, bgColor);
                }

                Color savedColor = GUI.contentColor;
                GUI.contentColor = textColor;
                EditorGUI.LabelField(rect, "List is Empty");
                GUI.contentColor = savedColor;
            };

            // Element height callback - use list.serializedProperty for fresh reference
            list.elementHeightCallback = (int index) =>
            {
                SerializedProperty arrayProp = list.serializedProperty;
                if (arrayProp == null || index < 0 || index >= arrayProp.arraySize)
                {
                    return EditorGUIUtility.singleLineHeight + 4f;
                }

                SerializedProperty element = arrayProp.GetArrayElementAtIndex(index);
                if (element == null)
                {
                    return EditorGUIUtility.singleLineHeight + 4f;
                }

                return EditorGUI.GetPropertyHeight(element, true) + 4f;
            };

            ReorderableListCache[key] = list;
            return list;
        }

        /// <summary>
        /// Draws a non-array complex property with a themed background.
        /// </summary>
        private void DrawThemedComplexProperty(SerializedProperty property, bool includeChildren)
        {
            GUIContent label = EditorGUIUtility.TrTextContent(property.displayName);

            float propertyHeight = EditorGUI.GetPropertyHeight(property, label, includeChildren);
            Rect propertyRect = EditorGUILayout.GetControlRect(false, propertyHeight);

            // Override GUI.backgroundColor to influence container appearance
            Color savedBgColor = GUI.backgroundColor;
            GUI.backgroundColor = _paletteBackgroundColor;

            // Override helpBox style temporarily
            GUIStyle savedHelpBox = new(EditorStyles.helpBox);
            EditorStyles.helpBox.normal.background = _paletteBorderTexture;
            EditorStyles.helpBox.normal.textColor = _fieldTextColor;

            // Draw background
            if (Event.current.type == EventType.Repaint)
            {
                GUIStyle bgStyle = new() { normal = { background = _paletteTexture } };
                bgStyle.Draw(propertyRect, GUIContent.none, false, false, false, false);
            }

            // Draw property
            EditorGUI.PropertyField(propertyRect, property, label, includeChildren);

            // Restore
            EditorStyles.helpBox.normal.background = savedHelpBox.normal.background;
            EditorStyles.helpBox.normal.textColor = savedHelpBox.normal.textColor;
            GUI.backgroundColor = savedBgColor;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_isActive)
            {
                // Restore all overridden styles
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

                // Restore GUI colors
                GUI.contentColor = _previousContentColor;
                GUI.color = _previousColor;
                GUI.backgroundColor = _previousBackgroundColor;
            }
        }
    }

    internal static class WGroupHeaderVisualUtility
    {
        private const float HorizontalContentPadding = 3f;
        private const float VerticalContentPaddingTop = 1f;
        private const float VerticalContentPaddingBottom = 3f;

        /// <summary>
        /// Additional left offset applied to foldout header content rects in Inspector context.
        /// </summary>
        /// <remarks>
        /// This offset ensures the foldout arrow is visually contained within the
        /// header background box. Combined with HorizontalContentPadding (3f) and
        /// the foldout style's internal left padding (16px), this creates proper
        /// visual encapsulation of the header content.
        /// Total left offset in Inspector: 3f (horizontal) + 9f (foldout) + 16f (style) = 28f
        /// </remarks>
        private const float FoldoutContentOffsetInspector = 9f;

        /// <summary>
        /// Additional left offset applied to foldout header content rects in Settings context.
        /// </summary>
        /// <remarks>
        /// In SettingsProvider contexts, the helpBox and ambient padding already provide
        /// sufficient visual structure. Using zero offset here prevents excessive indentation.
        /// Total left offset in Settings: 3f (horizontal) + 0f (foldout) + 16f (style) = 19f
        /// </remarks>
        private const float FoldoutContentOffsetSettings = 0f;

        private static bool _isSettingsContext;

        /// <summary>
        /// Gets or sets whether WGroup headers are being drawn in a SettingsProvider context.
        /// </summary>
        /// <remarks>
        /// When true, foldout headers use reduced left offset to prevent excessive indentation.
        /// This should be set to true before drawing WGroups in SettingsProvider.guiHandler
        /// and reset to false afterward.
        /// </remarks>
        internal static bool IsSettingsContext
        {
            get => _isSettingsContext;
            set => _isSettingsContext = value;
        }

        private static float CurrentFoldoutContentOffset =>
            _isSettingsContext ? FoldoutContentOffsetSettings : FoldoutContentOffsetInspector;

        /// <summary>
        /// A disposable scope that sets <see cref="IsSettingsContext"/> to true for its duration.
        /// </summary>
        /// <remarks>
        /// Use this scope when drawing WGroups in SettingsProvider contexts to ensure
        /// proper indentation behavior.
        /// </remarks>
        internal sealed class SettingsContextScope : IDisposable
        {
            private readonly bool _previousValue;
            private bool _disposed;

            public SettingsContextScope()
            {
                _previousValue = _isSettingsContext;
                _isSettingsContext = true;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _isSettingsContext = _previousValue;
            }
        }

        internal static void DrawHeaderBackground(Rect rect, Color baseColor)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Color tinted = GetHeaderTint(baseColor);
            EditorGUI.DrawRect(rect, tinted);
        }

        internal static Rect GetContentRect(
            Rect rect,
            float additionalTopPadding,
            float additionalBottomPadding,
            bool includeFoldoutOffset = false
        )
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return rect;
            }

            Rect contentRect = rect;
            float horizontal = HorizontalContentPadding;
            float topPadding = VerticalContentPaddingTop + Mathf.Max(0f, additionalTopPadding);
            float bottomPadding =
                VerticalContentPaddingBottom + Mathf.Max(0f, additionalBottomPadding);

            contentRect.xMin += horizontal;
            contentRect.xMax -= horizontal;

            if (includeFoldoutOffset)
            {
                contentRect.xMin += CurrentFoldoutContentOffset;
            }
            contentRect.yMin += topPadding;
            contentRect.yMax -= bottomPadding;

            if (contentRect.xMax < contentRect.xMin)
            {
                float centerX = rect.center.x;
                contentRect.xMin = centerX;
                contentRect.xMax = centerX;
            }

            if (contentRect.yMax < contentRect.yMin)
            {
                float centerY = rect.center.y;
                contentRect.yMin = centerY;
                contentRect.yMax = centerY;
            }

            return contentRect;
        }

        internal static void DrawHeaderBorder(Rect rect, Color baseColor)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            float borderThickness = GetBorderThickness(rect);
            if (borderThickness <= 0f)
            {
                return;
            }

            Color borderColor = GetHeaderBorderColor(baseColor);
            DrawHeaderBorderRects(rect, borderThickness, borderColor);
        }

        private static Color GetHeaderTint(Color baseColor)
        {
            // Use background luminance to determine appropriate alpha
            // Cross-theme palettes (e.g., light palette on dark editor) need sufficient opacity
            float bgLuminance = 0.299f * baseColor.r + 0.587f * baseColor.g + 0.114f * baseColor.b;
            bool isLightBackground = bgLuminance > 0.5f;

            // Light backgrounds need higher alpha on dark editors to stand out
            // Dark backgrounds need higher alpha on light editors to stand out
            bool isCrossTheme = isLightBackground == EditorGUIUtility.isProSkin;
            float alpha = isCrossTheme ? 0.85f : (EditorGUIUtility.isProSkin ? 0.62f : 0.55f);

            return new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }

        private static float GetBorderThickness(Rect rect)
        {
            float available = Mathf.Min(rect.width, rect.height);
            if (available <= 0f)
            {
                return 0f;
            }

            float pixelsPerPoint = Mathf.Max(1f, EditorGUIUtility.pixelsPerPoint);
            float capped = Mathf.Min(pixelsPerPoint, available * 0.5f);
            return Mathf.Max(1f, capped);
        }

        private static void DrawHeaderBorderRects(
            Rect rect,
            float borderThickness,
            Color borderColor
        )
        {
            Rect topBorder = new(rect.xMin, rect.yMin, rect.width, borderThickness);
            Rect bottomBorder = new(
                rect.xMin,
                rect.yMax - borderThickness,
                rect.width,
                borderThickness
            );
            Rect leftBorder = new(rect.xMin, rect.yMin, borderThickness, rect.height);
            Rect rightBorder = new(
                rect.xMax - borderThickness,
                rect.yMin,
                borderThickness,
                rect.height
            );

            EditorGUI.DrawRect(topBorder, borderColor);
            EditorGUI.DrawRect(bottomBorder, borderColor);
            EditorGUI.DrawRect(leftBorder, borderColor);
            EditorGUI.DrawRect(rightBorder, borderColor);
        }

        private static Color GetHeaderBorderColor(Color baseColor)
        {
            // Determine border color based on background luminance, not editor skin
            // This ensures light backgrounds get dark borders even in dark-mode editors
            float bgLuminance = 0.299f * baseColor.r + 0.587f * baseColor.g + 0.114f * baseColor.b;
            bool isLightBackground = bgLuminance > 0.5f;

            Color emphasisTarget = isLightBackground ? Color.black : Color.white;
            float emphasisWeight = isLightBackground ? 0.7f : 0.15f;
            Color emphasized = Color.Lerp(baseColor, emphasisTarget, emphasisWeight);
            float alpha = 0.9f;
            return new Color(emphasized.r, emphasized.g, emphasized.b, alpha);
        }
    }
#endif
}
