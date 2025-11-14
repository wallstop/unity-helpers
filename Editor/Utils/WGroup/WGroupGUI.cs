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
                if (definition.Collapsible && allowHeader)
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
                    IReadOnlyList<string> propertyPaths = definition.PropertyPaths;
                    int propertyCount = propertyPaths.Count;
                    if (propertyCount > 0)
                    {
                        AddContentPadding();
                    }
                    for (int index = 0; index < propertyCount; index++)
                    {
                        string propertyPath = propertyPaths[index];
                        SerializedProperty property = serializedObject.FindProperty(propertyPath);
                        if (property == null)
                        {
                            continue;
                        }

                        if (overrideDrawer != null && overrideDrawer(serializedObject, property))
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
            Rect headerRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                foldoutStyle,
                GUILayout.ExpandWidth(true)
            );

            WGroupStyles.DrawHeaderBackground(headerRect, palette.BackgroundColor);

            int originalIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            expanded = EditorGUI.Foldout(
                headerRect,
                expanded,
                definition.DisplayName,
                true,
                foldoutStyle
            );
            EditorGUI.indentLevel = originalIndent;

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
            GUI.Label(labelRect, content, labelStyle);
            GUILayout.Space(2f);
            return labelRect;
        }

        private static void AddContentPadding()
        {
            float spacing = Mathf.Max(1f, EditorGUIUtility.standardVerticalSpacing);
            GUILayout.Space(spacing);
        }
    }

    internal static class WGroupStyles
    {
        private static readonly Dictionary<Color, GUIStyle> FoldoutStyles = new();
        private static readonly Dictionary<Color, GUIStyle> HeaderStyles = new();

        internal static GUIStyle GetFoldoutStyle(Color textColor)
        {
            if (!FoldoutStyles.TryGetValue(textColor, out GUIStyle style))
            {
                style = new GUIStyle(EditorStyles.foldoutHeader)
                {
                    fontStyle = FontStyle.Bold,
                    padding = new RectOffset(16, 6, 4, 4),
                };
                style.normal.textColor = textColor;
                style.onNormal.textColor = textColor;
                style.active.textColor = textColor;
                style.onActive.textColor = textColor;
                style.focused.textColor = textColor;
                style.onFocused.textColor = textColor;
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
                    padding = new RectOffset(4, 4, 2, 2),
                };
                style.normal.textColor = textColor;
                style.active.textColor = textColor;
                style.focused.textColor = textColor;
                HeaderStyles[textColor] = style;
            }

            return style;
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

        private static Color GetHeaderTint(Color baseColor)
        {
            float alpha = EditorGUIUtility.isProSkin ? 0.62f : 0.28f;
            return new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }
    }
#endif
}
