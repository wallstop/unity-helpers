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
        private static readonly Dictionary<ButtonStyleKey, GUIStyle> ColoredButtonStyles = new(
            new ButtonStyleKeyComparer()
        );
        private static readonly Dictionary<Color, Texture2D> SolidColorTextures = new(
            new ColorComparer()
        );

        internal const float ButtonHeight = 18f;

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

        internal static GUIStyle GetColoredButtonStyle(Color buttonColor, Color textColor)
        {
            GUIStyle baseStyle = ButtonStyle;
            ButtonStyleKey key = new(buttonColor, textColor);
            if (ColoredButtonStyles.TryGetValue(key, out GUIStyle cached))
            {
                return cached;
            }

            GUIStyle style = new(baseStyle) { fixedHeight = ButtonHeight };
            style.normal.textColor = textColor;
            style.focused.textColor = textColor;
            style.active.textColor = textColor;
            style.hover.textColor = textColor;
            style.onNormal.textColor = textColor;
            style.onFocused.textColor = textColor;
            style.onActive.textColor = textColor;
            style.onHover.textColor = textColor;

            Texture2D normal = GetSolidTexture(buttonColor);
            Texture2D hover = GetSolidTexture(WButtonColorUtility.GetHoverColor(buttonColor));
            Texture2D active = GetSolidTexture(WButtonColorUtility.GetActiveColor(buttonColor));

            style.normal.background = normal;
            style.focused.background = normal;
            style.onNormal.background = normal;
            style.onFocused.background = normal;

            style.hover.background = hover;
            style.onHover.background = hover;

            style.active.background = active;
            style.onActive.background = active;

            ColoredButtonStyles[key] = style;
            return style;
        }

        private static Texture2D GetSolidTexture(Color color)
        {
            if (SolidColorTextures.TryGetValue(color, out Texture2D cached))
            {
                return cached;
            }

            Texture2D texture = new(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            SolidColorTextures[color] = texture;
            return texture;
        }

        private readonly struct ButtonStyleKey : System.IEquatable<ButtonStyleKey>
        {
            internal ButtonStyleKey(Color buttonColor, Color textColor)
            {
                ButtonColor = buttonColor;
                TextColor = textColor;
            }

            internal Color ButtonColor { get; }

            internal Color TextColor { get; }

            public bool Equals(ButtonStyleKey other)
            {
                return ColorComparer.AreEqual(ButtonColor, other.ButtonColor)
                    && ColorComparer.AreEqual(TextColor, other.TextColor);
            }

            public override bool Equals(object obj)
            {
                return obj is ButtonStyleKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = (hash * 23) + ButtonColor.GetHashCode();
                    hash = (hash * 23) + TextColor.GetHashCode();
                    return hash;
                }
            }
        }

        private sealed class ButtonStyleKeyComparer : IEqualityComparer<ButtonStyleKey>
        {
            public bool Equals(ButtonStyleKey x, ButtonStyleKey y)
            {
                return x.Equals(y);
            }

            public int GetHashCode(ButtonStyleKey obj)
            {
                return obj.GetHashCode();
            }
        }

        private sealed class ColorComparer : IEqualityComparer<Color>
        {
            public bool Equals(Color x, Color y)
            {
                return AreEqual(x, y);
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

            public static bool AreEqual(Color x, Color y)
            {
                return Mathf.Approximately(x.r, y.r)
                    && Mathf.Approximately(x.g, y.g)
                    && Mathf.Approximately(x.b, y.b)
                    && Mathf.Approximately(x.a, y.a);
            }
        }
    }
#endif
}
