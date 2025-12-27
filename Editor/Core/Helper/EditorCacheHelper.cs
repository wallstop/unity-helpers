namespace WallstopStudios.UnityHelpers.Editor.Core.Helper
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Extension;

    /// <summary>
    /// Provides centralized caching utilities for editor code to avoid repeated allocations.
    /// </summary>
    /// <remarks>
    /// This helper consolidates common caching patterns used across property drawers, inspectors,
    /// and editor windows. Using a single cache improves memory efficiency and reduces duplication.
    /// </remarks>
    public static class EditorCacheHelper
    {
        private static readonly Dictionary<int, string> IntToStringCache = new();
        private static readonly Dictionary<(int, int), string> PaginationLabelCache = new();
        private static readonly Dictionary<Color, Texture2D> SolidTextureCache = new(
            new ColorComparer()
        );

        private const int MaxIntCacheSize = 10000;
        private const int MaxPaginationCacheSize = 1000;

        /// <summary>
        /// Gets the cached string representation of an integer value.
        /// </summary>
        /// <param name="value">The integer value to convert to string.</param>
        /// <returns>The cached string representation.</returns>
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

            string result = CreateIntString(value);
            IntToStringCache[value] = result;
            return result;
        }

        /// <summary>
        /// Gets a cached pagination label in the format "Page X / Y".
        /// </summary>
        /// <param name="page">The current page number.</param>
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
                return CreatePaginationLabel(key);
            }

            string result = CreatePaginationLabel(key);
            PaginationLabelCache[key] = result;
            return result;
        }

        /// <summary>
        /// Gets a solid-color texture from cache, creating it if necessary.
        /// </summary>
        /// <param name="color">The color of the texture.</param>
        /// <returns>A 1x1 texture filled with the specified color.</returns>
        public static Texture2D GetSolidTexture(Color color)
        {
            if (SolidTextureCache.TryGetValue(color, out Texture2D cached) && cached != null)
            {
                return cached;
            }

            Texture2D texture = new(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();

            SolidTextureCache[color] = texture;
            return texture;
        }

        /// <summary>
        /// Clears all cached data. Useful for freeing memory during domain reload.
        /// </summary>
        public static void ClearAll()
        {
            IntToStringCache.Clear();
            PaginationLabelCache.Clear();

            foreach (KeyValuePair<Color, Texture2D> pair in SolidTextureCache)
            {
                if (pair.Value != null)
                {
                    Object.DestroyImmediate(pair.Value);
                }
            }
            SolidTextureCache.Clear();
        }

        private static string CreateIntString(int value)
        {
            return value.ToString();
        }

        private static string CreatePaginationLabel((int, int) key)
        {
            return "Page " + GetCachedIntString(key.Item1) + " / " + GetCachedIntString(key.Item2);
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
                    hash = hash * 31 + Mathf.RoundToInt(obj.r * 255f);
                    hash = hash * 31 + Mathf.RoundToInt(obj.g * 255f);
                    hash = hash * 31 + Mathf.RoundToInt(obj.b * 255f);
                    hash = hash * 31 + Mathf.RoundToInt(obj.a * 255f);
                    return hash;
                }
            }
        }
    }
#endif
}
