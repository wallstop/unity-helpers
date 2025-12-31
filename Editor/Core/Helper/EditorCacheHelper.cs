// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Editor.Core.Helper
{
#if UNITY_EDITOR

    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;

    /// <summary>
    /// Internal wrapper for tracking LRU order per-dictionary.
    /// Uses a LinkedList to maintain access order and a Dictionary for O(1) node lookup.
    /// </summary>
    /// <typeparam name="TKey">The type of dictionary key.</typeparam>
    internal sealed class LRUOrderTracker<TKey>
    {
        private readonly LinkedList<TKey> _accessOrder = new();
        private readonly Dictionary<TKey, LinkedListNode<TKey>> _nodeMap = new();

        /// <summary>
        /// Marks a key as recently accessed by moving it to the end of the access order.
        /// If the key doesn't exist in tracking, adds it.
        /// </summary>
        /// <param name="key">The key to mark as accessed.</param>
        public void MarkAccessed(TKey key)
        {
            if (_nodeMap.TryGetValue(key, out LinkedListNode<TKey> node))
            {
                _accessOrder.Remove(node);
                _accessOrder.AddLast(node);
            }
            else
            {
                LinkedListNode<TKey> newNode = _accessOrder.AddLast(key);
                _nodeMap[key] = newNode;
            }
        }

        /// <summary>
        /// Removes a key from the LRU tracking.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        public void Remove(TKey key)
        {
            if (_nodeMap.TryGetValue(key, out LinkedListNode<TKey> node))
            {
                _accessOrder.Remove(node);
                _nodeMap.Remove(key);
            }
        }

        /// <summary>
        /// Clears all keys from the LRU tracker.
        /// This is useful for synchronizing the tracker when the dictionary is cleared externally.
        /// </summary>
        public void Clear()
        {
            _accessOrder.Clear();
            _nodeMap.Clear();
        }

        /// <summary>
        /// Gets the least recently used key (first in access order).
        /// </summary>
        /// <param name="key">The LRU key if found.</param>
        /// <returns>True if there is at least one key being tracked; otherwise, false.</returns>
        public bool TryGetLeastRecentlyUsed(out TKey key)
        {
            if (_accessOrder.First != null)
            {
                key = _accessOrder.First.Value;
                return true;
            }

            key = default;
            return false;
        }
    }

    /// <summary>
    /// Provides centralized caching utilities for editor code to avoid repeated allocations.
    /// </summary>
    /// <remarks>
    /// This helper consolidates common caching patterns used across property drawers, inspectors,
    /// and editor windows. Using a single cache improves memory efficiency and reduces duplication.
    /// </remarks>
    public static class EditorCacheHelper
    {
        /// <summary>
        /// Default maximum size for bounded UI state caches (foldouts, scroll positions).
        /// </summary>
        public const int DefaultUIStateCacheSize = 5000;

        /// <summary>
        /// Default maximum size for bounded reflection caches (accessors, field info).
        /// </summary>
        public const int DefaultReflectionCacheSize = 2000;

        /// <summary>
        /// Default maximum size for bounded editor instance caches.
        /// </summary>
        public const int DefaultEditorCacheSize = 500;

        private const int MaxIntCacheSize = 10000;
        private const int MaxPaginationCacheSize = 1000;
        private const int MaxGUIStyleCacheSize = 500;

        private static readonly Dictionary<int, string> IntToStringCache = new();
        private static readonly Dictionary<(int, int), string> PaginationLabelCache = new();
        private static readonly Dictionary<Color, Texture2D> SolidTextureCache = new(
            new ColorComparer()
        );
        private static readonly Dictionary<Type, string[]> EnumDisplayNameCache = new();
        private static readonly Dictionary<string, GUIStyle> GUIStyleCache = new();

        /// <summary>
        /// Tracks LRU order for bounded caches. Uses ConditionalWeakTable so that
        /// when a dictionary is garbage collected, its LRU tracker is also collected.
        /// </summary>
        private static readonly ConditionalWeakTable<object, object> LRUOrderTracking = new();

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
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
            };

            texture.SetPixel(0, 0, color);
            texture.Apply(false, true);
            SolidTextureCache[color] = texture;
            return texture;
        }

        /// <summary>
        /// Gets a solid-color texture from cache, creating it if necessary.
        /// Alias for <see cref="GetSolidTexture(Color)"/> for backwards compatibility.
        /// </summary>
        /// <param name="color">The color of the texture.</param>
        /// <returns>A 1x1 texture filled with the specified color.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Texture2D GetOrCreateTexture(Color color)
        {
            return GetSolidTexture(color);
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
            return GetSolidTexture(color);
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
        /// Gets or creates the LRU order tracker for a given dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of dictionary key.</typeparam>
        /// <typeparam name="TValue">The type of dictionary value.</typeparam>
        /// <param name="cache">The dictionary to get the tracker for.</param>
        /// <returns>The LRU order tracker associated with this dictionary.</returns>
        private static LRUOrderTracker<TKey> GetOrCreateLRUTracker<TKey, TValue>(
            Dictionary<TKey, TValue> cache
        )
        {
            if (!LRUOrderTracking.TryGetValue(cache, out object trackerObj))
            {
                LRUOrderTracker<TKey> newTracker = new();
                LRUOrderTracking.Add(cache, newTracker);
                return newTracker;
            }

            return (LRUOrderTracker<TKey>)trackerObj;
        }

        /// <summary>
        /// Attempts to add or update a value in a bounded dictionary cache using LRU eviction.
        /// When the cache is at capacity, the least-recently-used entry is evicted.
        /// </summary>
        /// <typeparam name="TKey">The type of dictionary key.</typeparam>
        /// <typeparam name="TValue">The type of dictionary value.</typeparam>
        /// <param name="cache">The dictionary cache to add to.</param>
        /// <param name="key">The key to add or update.</param>
        /// <param name="value">The value to store.</param>
        /// <param name="maxSize">The maximum number of entries allowed in the cache.</param>
        /// <remarks>
        /// This method uses LRU (Least Recently Used) eviction when the cache is full.
        /// When updating an existing key, the entry is marked as recently used.
        /// The least-recently-used entry is evicted when capacity is reached.
        /// LRU order is tracked using an internal linked list per dictionary instance.
        /// </remarks>
        public static void AddToBoundedCache<TKey, TValue>(
            Dictionary<TKey, TValue> cache,
            TKey key,
            TValue value,
            int maxSize
        )
        {
            if (cache == null)
            {
                return;
            }

            if (maxSize <= 0)
            {
                return;
            }

            if (key == null)
            {
                return;
            }

            LRUOrderTracker<TKey> tracker = GetOrCreateLRUTracker<TKey, TValue>(cache);

            // Synchronize tracker if dictionary was cleared externally
            if (cache.Count == 0)
            {
                tracker.Clear();
            }

            if (cache.ContainsKey(key))
            {
                cache[key] = value;
                tracker.MarkAccessed(key);
                return;
            }

            while (cache.Count >= maxSize)
            {
                if (tracker.TryGetLeastRecentlyUsed(out TKey lruKey))
                {
                    cache.Remove(lruKey);
                    tracker.Remove(lruKey);
                }
                else
                {
                    break;
                }
            }

            cache[key] = value;
            tracker.MarkAccessed(key);
        }

        /// <summary>
        /// Attempts to get a value from a bounded LRU cache, updating the access order if found.
        /// </summary>
        /// <typeparam name="TKey">The type of dictionary key.</typeparam>
        /// <typeparam name="TValue">The type of dictionary value.</typeparam>
        /// <param name="cache">The dictionary cache to get from.</param>
        /// <param name="key">The key to look up.</param>
        /// <param name="value">The value if found; otherwise, the default value.</param>
        /// <returns>True if the key was found; otherwise, false.</returns>
        /// <remarks>
        /// When a key is found, it is marked as recently used in the LRU tracking.
        /// This ensures LRU behavior where frequently accessed items are less likely to be evicted.
        /// </remarks>
        public static bool TryGetFromBoundedLRUCache<TKey, TValue>(
            Dictionary<TKey, TValue> cache,
            TKey key,
            out TValue value
        )
        {
            if (cache == null)
            {
                value = default;
                return false;
            }

            if (key == null)
            {
                value = default;
                return false;
            }

            if (cache.TryGetValue(key, out value))
            {
                LRUOrderTracker<TKey> tracker = GetOrCreateLRUTracker<TKey, TValue>(cache);
                tracker.MarkAccessed(key);
                return true;
            }

            return false;
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
            foreach (Texture2D texture in SolidTextureCache.Values)
            {
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
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
            return Objects.HashCode(
                Mathf.RoundToInt(color.r * 255f),
                Mathf.RoundToInt(color.g * 255f),
                Mathf.RoundToInt(color.b * 255f),
                Mathf.RoundToInt(color.a * 255f)
            );
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
