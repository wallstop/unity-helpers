// ReSharper disable StaticMemberInGenericType
namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Attributes;
    using Helper;

    /// <summary>
    /// Internal cache data structure for storing enum name mappings optimized for fast lookup.
    /// </summary>
    internal sealed class EnumNameCacheData
    {
        public readonly string[] namesArray;
        public readonly ConcurrentDictionary<ulong, string> namesDict;
        public readonly bool useArray;
        public readonly ulong minValue;
        public readonly int arrayLength;

        public EnumNameCacheData(
            string[] namesArray,
            ConcurrentDictionary<ulong, string> namesDict,
            bool useArray,
            ulong minValue,
            int arrayLength
        )
        {
            this.namesArray = namesArray;
            this.namesDict = namesDict;
            this.useArray = useArray;
            this.minValue = minValue;
            this.arrayLength = arrayLength;
        }
    }

    /// <summary>
    /// Provides high-performance cached enum name lookups with zero allocation for frequently accessed enum values.
    /// </summary>
    /// <typeparam name="T">The unmanaged enum type to cache names for.</typeparam>
    /// <remarks>
    /// Uses array-based lookup for enums with small ranges (≤256 values) and dictionary-based lookup for larger enums.
    /// Thread-safe with reader-writer locking for dictionary operations.
    /// Performance: O(1) lookups for both array and dictionary strategies.
    /// </remarks>
    public static class EnumNameCache<T>
        where T : unmanaged, Enum
    {
        // Use instance holder to avoid static field access overhead on Mono
        private static readonly EnumNameCacheData Cache;

        static EnumNameCache()
        {
            Array rawValues = Enum.GetValues(typeof(T));
            T[] values = Unsafe.As<Array, T[]>(ref rawValues);
            string[] names = Enum.GetNames(typeof(T));

            // Try to determine if we can use array-based lookup
            ulong minVal = ulong.MaxValue;
            ulong maxVal = 0;
            bool hasValidRange = true;

            for (int i = 0; i < values.Length; i++)
            {
                if (!EnumNumericHelper<T>.TryConvertToUInt64(values[i], out ulong val))
                {
                    hasValidRange = false;
                    break;
                }

                if (val < minVal)
                {
                    minVal = val;
                }

                if (val > maxVal)
                {
                    maxVal = val;
                }
            }

            // Use array if the range is reasonable (< 256 elements)
            ulong range = hasValidRange && maxVal >= minVal ? maxVal - minVal + 1 : 0;
            bool useArray = hasValidRange && range <= 256 && range > 0;

            string[] namesArray;
            ConcurrentDictionary<ulong, string> namesDict;
            ulong minValue;
            int arrayLength;

            if (useArray)
            {
                minValue = minVal;
                arrayLength = (int)range;
                namesArray = new string[arrayLength];

                for (int i = 0; i < values.Length; i++)
                {
                    T value = values[i];
                    if (EnumNumericHelper<T>.TryConvertToUInt64(value, out ulong key))
                    {
                        int index = (int)(key - minValue);
                        if (index >= 0 && index < arrayLength)
                        {
                            string name = names[i];
                            if (namesArray[index] == null)
                            {
                                namesArray[index] = name;
                            }
                        }
                    }
                }
                namesDict = new ConcurrentDictionary<ulong, string>();
            }
            else
            {
                // Fall back to dictionary
                namesDict = new ConcurrentDictionary<ulong, string>();

                for (int i = 0; i < values.Length; i++)
                {
                    T value = values[i];
                    if (!EnumNumericHelper<T>.TryConvertToUInt64(value, out ulong key))
                    {
                        continue;
                    }

                    string name = value.ToString("G");
                    namesDict.TryAdd(key, name);
                }
                namesArray = null;
                minValue = 0;
                arrayLength = 0;
            }

            Cache = new EnumNameCacheData(namesArray, namesDict, useArray, minValue, arrayLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToCachedName(T value)
        {
            if (!EnumNumericHelper<T>.TryConvertToUInt64(value, out ulong key))
            {
                return value.ToString("G");
            }

            EnumNameCacheData cache = Cache;
            if (cache.useArray && cache.namesArray != null)
            {
                ulong index = key - cache.minValue;
                if (index < (ulong)cache.arrayLength)
                {
                    string existing = cache.namesArray[index];
                    if (existing != null)
                    {
                        return existing;
                    }

                    string generated = value.ToString("G");
                    string prior = Interlocked.CompareExchange(
                        ref cache.namesArray[index],
                        generated,
                        null
                    );
                    return prior ?? generated;
                }
            }

            ConcurrentDictionary<ulong, string> namesDict = cache.namesDict;
            if (namesDict != null)
            {
                if (namesDict.TryGetValue(key, out string name))
                {
                    return name;
                }
                return namesDict.GetOrAdd(key, enumValue => enumValue.ToString("G"));
            }

            return value.ToString("G");
        }
    }

    /// <summary>
    /// Internal cache data structure for storing enum display name mappings from EnumDisplayNameAttribute.
    /// </summary>
    internal sealed class EnumDisplayNameCacheData
    {
        public readonly string[] namesArray;
        public readonly ConcurrentDictionary<ulong, string> namesDict;
        public readonly bool useArray;
        public readonly ulong minValue;
        public readonly int arrayLength;

        public EnumDisplayNameCacheData(
            string[] namesArray,
            ConcurrentDictionary<ulong, string> namesDict,
            bool useArray,
            ulong minValue,
            int arrayLength
        )
        {
            this.namesArray = namesArray;
            this.namesDict = namesDict;
            this.useArray = useArray;
            this.minValue = minValue;
            this.arrayLength = arrayLength;
        }
    }

    /// <summary>
    /// Provides high-performance cached enum display name lookups using EnumDisplayNameAttribute values.
    /// </summary>
    /// <typeparam name="T">The unmanaged enum type to cache display names for.</typeparam>
    /// <remarks>
    /// Uses reflection to extract EnumDisplayNameAttribute values at startup, then caches for fast access.
    /// Falls back to field name if attribute is not present.
    /// Uses array-based lookup for enums with small ranges (≤256 values) and dictionary-based lookup for larger enums.
    /// Thread-safe with concurrent dictionary operations.
    /// Performance: O(1) lookups for both array and dictionary strategies.
    /// </remarks>
    public static class EnumDisplayNameCache<T>
        where T : unmanaged, Enum
    {
        // Use instance holder to avoid static field access overhead on Mono
        private static readonly EnumDisplayNameCacheData Cache;

        static EnumDisplayNameCache()
        {
            Type type = typeof(T);
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            // First pass: determine range
            ulong minVal = ulong.MaxValue;
            ulong maxVal = 0;
            bool hasValidRange = true;

            for (int i = 0; i < fields.Length; i++)
            {
                T value = (T)fields[i].GetValue(null);
                if (!EnumNumericHelper<T>.TryConvertToUInt64(value, out ulong val))
                {
                    hasValidRange = false;
                    break;
                }

                if (val < minVal)
                {
                    minVal = val;
                }

                if (val > maxVal)
                {
                    maxVal = val;
                }
            }

            // Use array if the range is reasonable (< 256 elements)
            ulong range = hasValidRange && maxVal >= minVal ? maxVal - minVal + 1 : 0;
            bool useArray = hasValidRange && range <= 256 && range > 0;

            string[] namesArray;
            ConcurrentDictionary<ulong, string> namesDict;
            ulong minValue;
            int arrayLength;

            if (useArray)
            {
                minValue = minVal;
                arrayLength = (int)range;
                namesArray = new string[arrayLength];

                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    string name = field.IsAttributeDefined(
                        out EnumDisplayNameAttribute displayName,
                        inherit: false
                    )
                        ? displayName.DisplayName
                        : field.Name;
                    T value = (T)field.GetValue(null);

                    if (EnumNumericHelper<T>.TryConvertToUInt64(value, out ulong key))
                    {
                        int index = (int)(key - minValue);
                        if (index >= 0 && index < arrayLength)
                        {
                            namesArray[index] = name;
                        }
                    }
                }
                namesDict = new ConcurrentDictionary<ulong, string>(
                    Environment.ProcessorCount,
                    fields.Length
                );
            }
            else
            {
                // Fall back to dictionary
                namesDict = new ConcurrentDictionary<ulong, string>(
                    Environment.ProcessorCount,
                    fields.Length
                );

                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    string name = field.IsAttributeDefined(
                        out EnumDisplayNameAttribute displayName,
                        inherit: false
                    )
                        ? displayName.DisplayName
                        : field.Name;
                    T value = (T)field.GetValue(null);

                    if (!EnumNumericHelper<T>.TryConvertToUInt64(value, out ulong key))
                    {
                        continue;
                    }

                    namesDict.TryAdd(key, name);
                }
                namesArray = null;
                minValue = 0;
                arrayLength = 0;
            }

            Cache = new EnumDisplayNameCacheData(
                namesArray,
                namesDict,
                useArray,
                minValue,
                arrayLength
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToDisplayName(T value)
        {
            if (!EnumNumericHelper<T>.TryConvertToUInt64(value, out ulong key))
            {
                return value.ToString("G");
            }

            EnumDisplayNameCacheData cache = Cache;
            if (cache.useArray && cache.namesArray != null)
            {
                ulong index = key - cache.minValue;
                if (index < (ulong)cache.arrayLength)
                {
                    string existing = cache.namesArray[index];
                    if (existing != null)
                    {
                        return existing;
                    }

                    string generated = value.ToString("G");
                    string prior = Interlocked.CompareExchange(
                        ref cache.namesArray[index],
                        generated,
                        null
                    );
                    return prior ?? generated;
                }
            }

            ConcurrentDictionary<ulong, string> namesDict = cache.namesDict;
            if (namesDict != null)
            {
                if (namesDict.TryGetValue(key, out string name))
                {
                    return name;
                }
                return namesDict.GetOrAdd(key, enumValue => enumValue.ToString("G"));
            }

            return value.ToString("G");
        }
    }

    /// <summary>
    /// Extension methods for enum types providing allocation-free flag checking and cached name conversions.
    /// </summary>
    /// <remarks>
    /// Thread Safety: All methods are thread-safe.
    /// Performance: Methods use caching and aggressive inlining for optimal performance.
    /// </remarks>
    public static class EnumExtensions
    {
        /// <summary>
        /// Checks if an enum value has a specific flag set without boxing allocation.
        /// </summary>
        /// <typeparam name="T">The unmanaged enum type (must be a flags enum for meaningful results).</typeparam>
        /// <param name="value">The enum value to check.</param>
        /// <param name="flag">The flag to check for.</param>
        /// <returns>True if the flag is set, false otherwise.</returns>
        /// <remarks>
        /// Null handling: N/A - operates on value types.
        /// Thread-safe: Yes.
        /// Performance: O(1) - uses bitwise operations on underlying numeric type.
        /// Allocations: Zero allocations (no boxing). Falls back to built-in HasFlag for unsupported enum sizes.
        /// Edge cases: Works with enum sizes 1, 2, 4, or 8 bytes. Larger sizes fall back to HasFlag.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasFlagNoAlloc<T>(this T value, T flag)
            where T : unmanaged, Enum
        {
            if (
                !EnumNumericHelper<T>.TryConvertToUInt64(value, out ulong valueUnderlying)
                || !EnumNumericHelper<T>.TryConvertToUInt64(flag, out ulong flagUnderlying)
            )
            {
                // Fallback for unsupported enum sizes
                return value.HasFlag(flag);
            }

            return (valueUnderlying & flagUnderlying) == flagUnderlying;
        }

        /// <summary>
        /// Converts an enum value to its display name using the EnumDisplayNameAttribute if present, otherwise the field name.
        /// </summary>
        /// <typeparam name="T">The unmanaged enum type.</typeparam>
        /// <param name="value">The enum value to convert.</param>
        /// <returns>The display name string from the attribute, or the enum's ToString() if not cached.</returns>
        /// <remarks>
        /// Null handling: N/A - operates on value types.
        /// Thread-safe: Yes.
        /// Performance: O(1) - uses cached lookups via EnumDisplayNameCache.
        /// Allocations: Zero for cached values, one string allocation for uncached values on first access.
        /// Edge cases: Returns ToString("G") for values not in the cache.
        /// </remarks>
        public static string ToDisplayName<T>(this T value)
            where T : unmanaged, Enum
        {
            return EnumDisplayNameCache<T>.ToDisplayName(value);
        }

        /// <summary>
        /// Converts a collection of enum values to their display names.
        /// </summary>
        /// <typeparam name="T">The unmanaged enum type.</typeparam>
        /// <param name="enumerable">The collection of enum values to convert.</param>
        /// <returns>An enumerable of display name strings.</returns>
        /// <remarks>
        /// Null handling: Throws if enumerable is null when enumerated.
        /// Thread-safe: Yes for reads.
        /// Performance: O(n) where n is the number of enum values. Uses cached lookups.
        /// Allocations: Allocates LINQ iterator. Minimal allocations for cached display names.
        /// Edge cases: Empty collection returns empty enumerable.
        /// </remarks>
        public static IEnumerable<string> ToDisplayNames<T>(this IEnumerable<T> enumerable)
            where T : unmanaged, Enum
        {
            return enumerable.Select(value => value.ToDisplayName());
        }

        /// <summary>
        /// Converts an enum value to its name string using a high-performance cache.
        /// </summary>
        /// <typeparam name="T">The unmanaged enum type.</typeparam>
        /// <param name="value">The enum value to convert.</param>
        /// <returns>The cached name string, or ToString("G") if not cached.</returns>
        /// <remarks>
        /// Null handling: N/A - operates on value types.
        /// Thread-safe: Yes with reader-writer locking.
        /// Performance: O(1) - uses cached lookups via EnumNameCache.
        /// Allocations: Zero for cached values, one string allocation for uncached values on first access.
        /// Edge cases: Returns ToString("G") for values not in the cache.
        /// </remarks>
        public static string ToCachedName<T>(this T value)
            where T : unmanaged, Enum
        {
            return EnumNameCache<T>.ToCachedName(value);
        }

        /// <summary>
        /// Converts a collection of enum values to their cached name strings.
        /// </summary>
        /// <typeparam name="T">The unmanaged enum type.</typeparam>
        /// <param name="enumerable">The collection of enum values to convert.</param>
        /// <returns>An enumerable of cached name strings.</returns>
        /// <remarks>
        /// Null handling: Throws if enumerable is null when enumerated.
        /// Thread-safe: Yes for reads.
        /// Performance: O(n) where n is the number of enum values. Uses cached lookups.
        /// Allocations: Allocates LINQ iterator. Minimal allocations for cached names.
        /// Edge cases: Empty collection returns empty enumerable.
        /// </remarks>
        public static IEnumerable<string> ToCachedNames<T>(this IEnumerable<T> enumerable)
            where T : unmanaged, Enum
        {
            return enumerable.Select(value => value.ToCachedName());
        }
    }

    /// <summary>
    /// Internal helper class for converting enum values to their underlying numeric representation without boxing.
    /// </summary>
    /// <typeparam name="T">The unmanaged enum type.</typeparam>
    internal static class EnumNumericHelper<T>
        where T : unmanaged, Enum
    {
        private static readonly int Size = Unsafe.SizeOf<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryConvertToUInt64(T value, out ulong result)
        {
            ref T valueRef = ref Unsafe.AsRef(in value);

            switch (Size)
            {
                case 1:
                    result = Unsafe.As<T, byte>(ref valueRef);
                    return true;
                case 2:
                    result = Unsafe.As<T, ushort>(ref valueRef);
                    return true;
                case 4:
                    result = Unsafe.As<T, uint>(ref valueRef);
                    return true;
                case 8:
                    result = Unsafe.As<T, ulong>(ref valueRef);
                    return true;
                default:
                    result = default;
                    return false;
            }
        }
    }
}
