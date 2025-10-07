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

    public static class EnumNameCache<T>
        where T : unmanaged, Enum
    {
        // Use instance holder to avoid static field access overhead on Mono
        private static readonly EnumNameCacheData Cache;
        private static readonly ReaderWriterLockSlim CacheLock = new ReaderWriterLockSlim(
            LockRecursionPolicy.NoRecursion
        );

        static EnumNameCache()
        {
            T[] values = (T[])Enum.GetValues(typeof(T));
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
                CacheLock.EnterReadLock();
                try
                {
                    if (namesDict.TryGetValue(key, out string cached))
                    {
                        return cached;
                    }
                }
                finally
                {
                    CacheLock.ExitReadLock();
                }

                CacheLock.EnterUpgradeableReadLock();
                try
                {
                    if (namesDict.TryGetValue(key, out string cached))
                    {
                        return cached;
                    }

                    string generated = value.ToString("G");
                    CacheLock.EnterWriteLock();
                    try
                    {
                        if (!namesDict.TryGetValue(key, out cached))
                        {
                            namesDict[key] = generated;
                            cached = generated;
                        }
                    }
                    finally
                    {
                        CacheLock.ExitWriteLock();
                    }

                    return cached;
                }
                finally
                {
                    CacheLock.ExitUpgradeableReadLock();
                }
            }

            return value.ToString("G");
        }
    }

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
                if (namesDict.TryGetValue(key, out string cached))
                {
                    return cached;
                }

                return namesDict.GetOrAdd(key, enumValue => enumValue.ToString("G"));
            }

            return value.ToString("G");
        }
    }

    public static class EnumExtensions
    {
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

        public static string ToDisplayName<T>(this T value)
            where T : unmanaged, Enum
        {
            return EnumDisplayNameCache<T>.ToDisplayName(value);
        }

        public static IEnumerable<string> ToDisplayNames<T>(this IEnumerable<T> enumerable)
            where T : unmanaged, Enum
        {
            return enumerable.Select(value => value.ToDisplayName());
        }

        public static string ToCachedName<T>(this T value)
            where T : unmanaged, Enum
        {
            return EnumNameCache<T>.ToCachedName(value);
        }

        public static IEnumerable<string> ToCachedNames<T>(this IEnumerable<T> enumerable)
            where T : unmanaged, Enum
        {
            return enumerable.Select(value => value.ToCachedName());
        }
    }

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
