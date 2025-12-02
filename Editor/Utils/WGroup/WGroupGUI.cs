namespace WallstopStudios.UnityHelpers.Editor.Utils.WGroup
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils;

    internal static class WGroupGUI
    {
        internal delegate bool PropertyOverride(
            SerializedObject owner,
            SerializedProperty property
        );

        internal static void DrawGroup(
            WGroupDefinition definition,
            SerializedObject serializedObject,
            Dictionary<int, bool> foldoutStates,
            PropertyOverride overrideDrawer = null
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

                if (expanded)
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
                    using (
                        GroupGUIWidthUtility.PushContentPadding(
                            horizontalPadding,
                            leftPadding,
                            rightPadding
                        )
                    )
                    {
                        IReadOnlyList<string> propertyPaths = definition.PropertyPaths;
                        int propertyCount = propertyPaths.Count;
                        if (propertyCount > 0)
                        {
                            AddContentPadding();
                        }
                        for (int index = 0; index < propertyCount; index++)
                        {
                            string propertyPath = propertyPaths[index];
                            SerializedProperty property = serializedObject.FindProperty(
                                propertyPath
                            );
                            if (property == null)
                            {
                                continue;
                            }

                            if (
                                overrideDrawer != null
                                && overrideDrawer(serializedObject, property)
                            )
                            {
                                continue;
                            }

                            GroupGUIIndentUtility.ExecuteWithIndentCompensation(() =>
                                EditorGUILayout.PropertyField(property, true)
                            );
                        }
                        if (propertyCount > 0)
                        {
                            AddContentPadding();
                        }
                    }

                    EditorGUI.indentLevel--;
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
            float headerHeight =
                EditorGUIUtility.singleLineHeight + WGroupStyles.HeaderVerticalPadding;
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
            Rect labelRect = GUILayoutUtility.GetRect(
                content,
                labelStyle,
                GUILayout.ExpandWidth(true)
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
    }

    internal static class WGroupStyles
    {
        internal const float HeaderTopPadding = 1f;
        internal const float HeaderBottomPadding = 1f;
        internal const float HeaderVerticalPadding = 4f;
        private static readonly Dictionary<Color, GUIStyle> FoldoutStyles = new();
        private static readonly Dictionary<Color, GUIStyle> HeaderStyles = new();

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

    internal static class WGroupHeaderVisualUtility
    {
        private const float HorizontalContentPadding = 3f;
        private const float VerticalContentPaddingTop = 1f;
        private const float VerticalContentPaddingBottom = 3f;
        private const float FoldoutContentOffset = 9f;

        internal static void DrawHeaderBackground(Rect rect, Color baseColor)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            Color tinted = GetHeaderTint(baseColor);
            EditorGUI.DrawRect(rect, tinted);
        }

        internal static Rect GetContentRect(Rect rect)
        {
            return GetContentRect(rect, 0f, 0f, false);
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
                contentRect.xMin += FoldoutContentOffset;
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
            float alpha = EditorGUIUtility.isProSkin ? 0.62f : 0.28f;
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
            Color emphasisTarget = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            float emphasisWeight = EditorGUIUtility.isProSkin ? 0.15f : 0.4f;
            Color emphasized = Color.Lerp(baseColor, emphasisTarget, emphasisWeight);
            float alpha = EditorGUIUtility.isProSkin ? 0.9f : 0.8f;
            return new Color(emphasized.r, emphasized.g, emphasized.b, alpha);
        }
    }
#endif
}
