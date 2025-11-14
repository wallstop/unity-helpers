namespace WallstopStudios.UnityHelpers.Editor.Utils.WFoldoutGroup
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.AnimatedValues;
    using UnityEditorInternal;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils.WGroup;

    internal static class WFoldoutGroupGUI
    {
        internal delegate bool PropertyOverride(
            SerializedObject owner,
            SerializedProperty property
        );

        private static readonly Dictionary<int, AnimBool> FoldoutAnimations = new();

        internal static void DrawGroup(
            WFoldoutGroupDefinition definition,
            SerializedObject serializedObject,
            Dictionary<int, bool> foldoutStates,
            PropertyOverride overrideDrawer = null
        )
        {
            if (definition == null || serializedObject == null)
            {
                return;
            }

            UnityHelpersSettings.WFoldoutGroupPaletteEntry palette =
                UnityHelpersSettings.ResolveWFoldoutGroupPalette(definition.ColorKey);

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

            bool allowHeader = !definition.HideHeader;
            bool expanded = true;
            int key = Objects.HashCode(definition.Name, definition.AnchorPropertyPath);
            bool tweenEnabled = allowHeader && UnityHelpersSettings.ShouldTweenWFoldoutGroups();
            AnimBool foldoutAnim = tweenEnabled ? GetFoldoutAnimation(key, definition) : null;

            if (!tweenEnabled)
            {
                ReleaseFoldoutAnimation(key);
            }

            if (allowHeader)
            {
                expanded = GetFoldoutState(foldoutStates, key, definition);

                GUIStyle foldoutStyle = WFoldoutGroupStyles.GetFoldoutStyle(palette.TextColor);
                Rect headerRect = GUILayoutUtility.GetRect(
                    GUIContent.none,
                    foldoutStyle,
                    GUILayout.ExpandWidth(true)
                );

                WFoldoutGroupStyles.DrawHeaderBackground(headerRect, palette.BackgroundColor);
                headerRect.xMin += WFoldoutGroupStyles.HeaderContentOffset;

                int originalIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                bool newExpanded = EditorGUI.Foldout(
                    headerRect,
                    expanded,
                    definition.DisplayName,
                    true,
                    foldoutStyle
                );
                EditorGUI.indentLevel = originalIndent;

                if (foldoutStates != null)
                {
                    foldoutStates[key] = newExpanded;
                }

                if (foldoutAnim != null)
                {
                    foldoutAnim.target = newExpanded;
                }

                expanded = newExpanded;
                EditorGUILayout.Space(2f);
            }

            bool drawContent =
                !allowHeader || expanded || (foldoutAnim != null && foldoutAnim.faded > 0f);
            if (!drawContent)
            {
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(6f);
                return;
            }

            EditorGUI.indentLevel++;
            if (!allowHeader || foldoutAnim == null)
            {
                DrawProperties(definition, serializedObject, overrideDrawer);
            }
            else
            {
                bool visible = EditorGUILayout.BeginFadeGroup(foldoutAnim.faded);
                if (visible)
                {
                    DrawProperties(definition, serializedObject, overrideDrawer);
                }
                EditorGUILayout.EndFadeGroup();
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
        }

        private static void DrawProperties(
            WFoldoutGroupDefinition definition,
            SerializedObject serializedObject,
            PropertyOverride overrideDrawer
        )
        {
            IReadOnlyList<string> propertyPaths = definition.PropertyPaths;
            int propertyCount = propertyPaths.Count;
            if (propertyCount == 0)
            {
                return;
            }

            AddContentPadding();
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

                EditorGUILayout.PropertyField(property, true);
            }
            AddContentPadding();
        }

        private static bool GetFoldoutState(
            Dictionary<int, bool> foldoutStates,
            int key,
            WFoldoutGroupDefinition definition
        )
        {
            if (foldoutStates != null && foldoutStates.TryGetValue(key, out bool storedState))
            {
                return storedState;
            }

            return !definition.StartCollapsed;
        }

        private static AnimBool GetFoldoutAnimation(int key, WFoldoutGroupDefinition definition)
        {
            bool expandedByDefault = !definition.StartCollapsed;
            if (!FoldoutAnimations.TryGetValue(key, out AnimBool anim) || anim == null)
            {
                anim = new AnimBool(expandedByDefault)
                {
                    speed = UnityHelpersSettings.GetWFoldoutGroupTweenSpeed(),
                };
                anim.valueChanged.AddListener(RequestRepaint);
                FoldoutAnimations[key] = anim;
                return anim;
            }

            anim.speed = UnityHelpersSettings.GetWFoldoutGroupTweenSpeed();
            return anim;
        }

        private static void ReleaseFoldoutAnimation(int key)
        {
            if (!FoldoutAnimations.TryGetValue(key, out AnimBool anim) || anim == null)
            {
                FoldoutAnimations.Remove(key);
                return;
            }

            anim.valueChanged.RemoveListener(RequestRepaint);
            FoldoutAnimations.Remove(key);
        }

        private static void RequestRepaint()
        {
            InternalEditorUtility.RepaintAllViews();
        }

        private static void AddContentPadding()
        {
            float spacing = Mathf.Max(1f, EditorGUIUtility.standardVerticalSpacing);
            GUILayout.Space(spacing);
        }
    }

    internal static class WFoldoutGroupStyles
    {
        internal const float HeaderContentOffset = 10f;
        private static readonly Dictionary<Color, GUIStyle> FoldoutStyles = new();

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
