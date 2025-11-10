namespace WallstopStudios.UnityHelpers.Editor.WButton
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;
    using System.Collections.Generic;

    internal static class WButtonStyles
    {
        private static GUIStyle _groupStyle;
        private static GUIStyle _headerStyle;
        private static GUIStyle _baseButtonStyle;
        private static GUIStyle _arrayHeaderStyle;
        private static GUIContent _topHeaderContent;
        private static GUIContent _bottomHeaderContent;
        private static readonly Dictionary<Color, GUIStyle> ColoredButtonStyles = new Dictionary<
            Color,
            GUIStyle
        >(new ColorComparer());

        internal const float ButtonHeight = 28f;

        internal static GUIStyle GroupStyle
        {
            get
            {
                _groupStyle ??= new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(12, 12, 10, 10),
                    margin = new RectOffset(2, 2, 4, 4),
                };
                return _groupStyle;
            }
        }

        internal static GUIStyle HeaderStyle
        {
            get
            {
                _headerStyle ??= new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                };
                return _headerStyle;
            }
        }

        internal static GUIStyle ButtonStyle
        {
            get
            {
                _baseButtonStyle ??= new GUIStyle(GUI.skin.button)
                {
                    wordWrap = true,
                    fontSize = 11,
                    richText = false,
                    fixedHeight = ButtonHeight,
                    alignment = TextAnchor.MiddleCenter,
                    margin = new RectOffset(2, 2, 2, 2),
                };
                return _baseButtonStyle;
            }
        }

        internal static GUIStyle ArrayHeaderStyle
        {
            get
            {
                _arrayHeaderStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 10 };
                return _arrayHeaderStyle;
            }
        }

        internal static GUIContent TopGroupLabel
        {
            get
            {
                _topHeaderContent ??= new GUIContent("Actions");
                return _topHeaderContent;
            }
        }

        internal static GUIContent BottomGroupLabel
        {
            get
            {
                _bottomHeaderContent ??= new GUIContent("Additional Actions");
                return _bottomHeaderContent;
            }
        }

        internal static GUIStyle GetColoredButtonStyle(Color background)
        {
            GUIStyle baseStyle = ButtonStyle;
            if (ColoredButtonStyles.TryGetValue(background, out GUIStyle cached))
            {
                return cached;
            }

            GUIStyle style = new GUIStyle(baseStyle) { fixedHeight = ButtonHeight };
            Color textColor = GetPreferredTextColor(background);
            style.normal.textColor = textColor;
            style.focused.textColor = textColor;
            style.active.textColor = textColor;
            style.hover.textColor = textColor;
            style.onNormal.textColor = textColor;
            style.onFocused.textColor = textColor;
            style.onActive.textColor = textColor;
            style.onHover.textColor = textColor;
            ColoredButtonStyles[background] = style;
            return style;
        }

        internal static void DrawButtonBackground(Rect rect, Color color)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            EditorGUI.DrawRect(rect, color);
        }

        private static Color GetPreferredTextColor(Color background)
        {
            float luminance =
                (0.299f * background.r) + (0.587f * background.g) + (0.114f * background.b);
            return luminance > 0.5f ? Color.black : Color.white;
        }

        private sealed class ColorComparer : IEqualityComparer<Color>
        {
            public bool Equals(Color x, Color y)
            {
                return Mathf.Approximately(x.r, y.r)
                    && Mathf.Approximately(x.g, y.g)
                    && Mathf.Approximately(x.b, y.b)
                    && Mathf.Approximately(x.a, y.a);
            }

            public int GetHashCode(Color obj)
            {
                unchecked
                {
                    int hash = 17;
                    hash = (hash * 23) + obj.r.GetHashCode();
                    hash = (hash * 23) + obj.g.GetHashCode();
                    hash = (hash * 23) + obj.b.GetHashCode();
                    hash = (hash * 23) + obj.a.GetHashCode();
                    return hash;
                }
            }
        }
    }
#endif
}
