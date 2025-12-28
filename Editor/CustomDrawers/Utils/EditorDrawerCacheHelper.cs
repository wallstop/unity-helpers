// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers.Utils
{
#if UNITY_EDITOR

    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using UnityEngine;

    /// <summary>
    /// Provides centralized caching utilities for property drawers to avoid repeated allocations.
    /// </summary>
    /// <remarks>
    /// This helper consolidates common caching patterns used across property drawers in both
    /// standard Unity and Odin Inspector implementations. Using a single cache improves memory
    /// efficiency, reduces code duplication, and ensures consistent behavior.
    /// </remarks>
    public static class EditorDrawerCacheHelper
    {
        private static readonly Dictionary<int, string> IntToStringCache = new();

        private static readonly Dictionary<(int, int), string> PaginationLabelCache = new();

        private static readonly Dictionary<Color, Texture2D> SolidTextureCache = new(
            new ColorComparer()
        );

        private static readonly Dictionary<Type, string[]> EnumDisplayNameCache = new();

        private static readonly Dictionary<string, GUIStyle> GUIStyleCache = new();

        private const int MaxIntCacheSize = 10000;

        private const int MaxPaginationCacheSize = 1000;

        private const int MaxGUIStyleCacheSize = 500;

        /// <summary>
        /// Gets the cached string representation of an integer value.
        /// </summary>
        /// <param name="value">The integer value to convert to string.</param>
        /// <returns>The cached string representation, or an empty string if cache is full and value is new.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetCachedIntString(int value)
        {
            if (IntToStringCache.TryGetValue(value, out string cached))
            {
                return cached;
            }

            if (IntToStringCache.Count >= MaxIntCacheSize)
            {
                return value.ToString();
            }

            string result = value.ToString();

            IntToStringCache[value] = result;

            return result;
        }

        /// <summary>
        /// Gets a cached pagination label in the format "Page X / Y".
        /// </summary>
        /// <param name="page">The current page number (1-based).</param>
        /// <param name="totalPages">The total number of pages.</param>
        /// <returns>The cached pagination label string.</returns>
        public static string GetPaginationLabel(int page, int totalPages)
        {
            (int, int) key = (page, totalPages);

            if (PaginationLabelCache.TryGetValue(key, out string cached))
            {
                return cached;
            }

            if (PaginationLabelCache.Count >= MaxPaginationCacheSize)
            {
                return "Page " + GetCachedIntString(page) + " / " + GetCachedIntString(totalPages);
            }

            string result =
                "Page " + GetCachedIntString(page) + " / " + GetCachedIntString(totalPages);

            PaginationLabelCache[key] = result;

            return result;
        }

        /// <summary>
        /// Gets the cached display name for an enum value using InspectorName attribute or ObjectNames.NicifyVariableName.
        /// </summary>
        /// <param name="value">The enum value to get the display name for.</param>
        /// <returns>The cached display name, or the enum's ToString() if value is null or not an enum.</returns>
        public static string GetEnumDisplayName(Enum value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            Type enumType = value.GetType();

            string[] displayNames = GetEnumDisplayNames(enumType);

            try
            {
                int index = Array.IndexOf(Enum.GetValues(enumType), value);

                if (index >= 0 && index < displayNames.Length)
                {
                    return displayNames[index];
                }
            }
            catch
            {
                // Fall through to default
            }

            return value.ToString();
        }

        /// <summary>
        /// Gets all cached display names for an enum type.
        /// </summary>
        /// <param name="enumType">The enum type to get display names for.</param>
        /// <returns>An array of display names corresponding to each enum value, or an empty array if enumType is invalid.</returns>
        public static string[] GetEnumDisplayNames(Type enumType)
        {
            if (enumType == null || !enumType.IsEnum)
            {
                return Array.Empty<string>();
            }

            if (EnumDisplayNameCache.TryGetValue(enumType, out string[] cached))
            {
                return cached;
            }

            Array values = Enum.GetValues(enumType);

            string[] names = new string[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                object enumValue = values.GetValue(i);

                string fieldName = enumValue.ToString();

                System.Reflection.FieldInfo field = enumType.GetField(fieldName);

                string displayName = fieldName;

                if (field != null)
                {
                    object[] inspectorNameAttributes = field.GetCustomAttributes(
                        typeof(UnityEngine.InspectorNameAttribute),
                        false
                    );

                    if (inspectorNameAttributes.Length > 0)
                    {
                        InspectorNameAttribute attr =
                            inspectorNameAttributes[0] as InspectorNameAttribute;

                        if (attr != null && !string.IsNullOrEmpty(attr.displayName))
                        {
                            displayName = attr.displayName;
                        }
                    }
                    else
                    {
                        displayName = UnityEditor.ObjectNames.NicifyVariableName(fieldName);
                    }
                }

                names[i] = displayName;
            }

            EnumDisplayNameCache[enumType] = names;

            return names;
        }

        /// <summary>
        /// Gets a solid-color texture from cache, creating it if necessary.
        /// </summary>
        /// <param name="color">The color of the texture.</param>
        /// <returns>A 1x1 texture filled with the specified color.</returns>
        public static Texture2D GetOrCreateTexture(Color color)
        {
            if (SolidTextureCache.TryGetValue(color, out Texture2D cached) && cached != null)
            {
                return cached;
            }

            Texture2D texture = new(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,

                wrapMode = TextureWrapMode.Clamp,

                filterMode = FilterMode.Point,
            };

            texture.SetPixel(0, 0, color);

            texture.Apply(false, true);

            SolidTextureCache[color] = texture;

            return texture;
        }

        /// <summary>
        /// Gets a solid-color texture from cache using an integer color key (RGBA packed into int).
        /// </summary>
        /// <param name="colorKey">The color key representing RGBA packed as: (r &lt;&lt; 24) | (g &lt;&lt; 16) | (b &lt;&lt; 8) | a.</param>
        /// <returns>A 1x1 texture filled with the color represented by the key.</returns>
        public static Texture2D GetOrCreateTexture(int colorKey)
        {
            float r = ((colorKey >> 24) & 0xFF) / 255f;

            float g = ((colorKey >> 16) & 0xFF) / 255f;

            float b = ((colorKey >> 8) & 0xFF) / 255f;

            float a = (colorKey & 0xFF) / 255f;

            Color color = new(r, g, b, a);

            return GetOrCreateTexture(color);
        }

        /// <summary>
        /// Gets or creates a cached GUIStyle using the provided key and factory function.
        /// </summary>
        /// <param name="key">A unique string key identifying the style.</param>
        /// <param name="factory">A factory function that creates the style if not cached. May be null, in which case null is returned if not cached.</param>
        /// <returns>The cached or newly created GUIStyle, or null if factory is null and style is not cached.</returns>
        public static GUIStyle GetOrCreateStyle(string key, Func<GUIStyle> factory)
        {
            if (string.IsNullOrEmpty(key))
            {
                if (factory == null)
                {
                    return null;
                }

                return factory();
            }

            if (GUIStyleCache.TryGetValue(key, out GUIStyle cached))
            {
                return cached;
            }

            if (factory == null)
            {
                return null;
            }

            if (GUIStyleCache.Count >= MaxGUIStyleCacheSize)
            {
                return factory();
            }

            GUIStyle style = factory();

            if (style != null)
            {
                GUIStyleCache[key] = style;
            }

            return style;
        }

        /// <summary>
        /// Clears all caches. Useful for freeing memory during domain reload.
        /// </summary>
        public static void ClearAllCaches()
        {
            IntToStringCache.Clear();

            PaginationLabelCache.Clear();

            EnumDisplayNameCache.Clear();

            GUIStyleCache.Clear();

            foreach (KeyValuePair<Color, Texture2D> pair in SolidTextureCache)
            {
                if (pair.Value != null)
                {
                    UnityEngine.Object.DestroyImmediate(pair.Value);
                }
            }

            SolidTextureCache.Clear();
        }

        /// <summary>
        /// Compares two colors for approximate equality.
        /// </summary>
        /// <param name="x">The first color.</param>
        /// <param name="y">The second color.</param>
        /// <returns>True if the colors are approximately equal; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreColorsEqual(Color x, Color y)
        {
            return Mathf.Approximately(x.r, y.r)
                && Mathf.Approximately(x.g, y.g)
                && Mathf.Approximately(x.b, y.b)
                && Mathf.Approximately(x.a, y.a);
        }

        /// <summary>
        /// Gets the hash code for a color suitable for use in dictionaries.
        /// </summary>
        /// <param name="color">The color to hash.</param>
        /// <returns>A hash code for the color.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetColorHashCode(Color color)
        {
            unchecked
            {
                int hash = 17;

                hash = hash * 31 + Mathf.RoundToInt(color.r * 255f);

                hash = hash * 31 + Mathf.RoundToInt(color.g * 255f);

                hash = hash * 31 + Mathf.RoundToInt(color.b * 255f);

                hash = hash * 31 + Mathf.RoundToInt(color.a * 255f);

                return hash;
            }
        }

        /// <summary>
        /// Comparer for Unity Color values that uses approximate equality.
        /// </summary>
        public sealed class ColorComparer : IEqualityComparer<Color>
        {
            /// <inheritdoc />
            public bool Equals(Color x, Color y)
            {
                return AreColorsEqual(x, y);
            }

            /// <inheritdoc />
            public int GetHashCode(Color obj)
            {
                return GetColorHashCode(obj);
            }
        }
    }

#endif
}
