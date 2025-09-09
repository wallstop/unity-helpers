// ReSharper disable StaticMemberInGenericType
namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Attributes;
    using Helper;

    public static class EnumNameCache<T>
        where T : struct, Enum
    {
        private const int MaxDenseRange = 1024;

        private static readonly bool UseDensePacking;
        private static readonly int Min;
        private static readonly string[] DenseNames;
        private static readonly Dictionary<int, string> SparseNames;

        static EnumNameCache()
        {
            T[] values = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
            int[] intValues = values.Select(v => Unsafe.As<T, int>(ref v)).ToArray();
            int min = intValues.Min();
            int max = intValues.Max();
            int range = max - min + 1;

            if (range <= MaxDenseRange)
            {
                UseDensePacking = true;
                Min = min;
                DenseNames = new string[range];

                for (int i = 0; i < values.Length; i++)
                {
                    int key = intValues[i] - min;
                    T value = values[i];
                    DenseNames[key] = value.ToString("G");
                }
            }
            else
            {
                UseDensePacking = false;
                SparseNames = new Dictionary<int, string>();
                for (int i = 0; i < values.Length; i++)
                {
                    int key = Unsafe.As<T, int>(ref values[i]);
                    T value = values[i];
                    string name = value.ToString("G");
                    SparseNames.TryAdd(key, name);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToCachedName(T value)
        {
            int key = Unsafe.As<T, int>(ref value);
            if (UseDensePacking)
            {
                int idx = key - Min;
                if ((uint)idx < (uint)DenseNames!.Length)
                {
                    return DenseNames[idx];
                }
            }
            else
            {
                if (SparseNames!.TryGetValue(key, out string name))
                {
                    return name;
                }
            }

            return value.ToString();
        }
    }

    public static class EnumDisplayNameCache<T>
        where T : struct, Enum
    {
        private const int MaxDenseRange = 1024;

        private static readonly bool UseDensePacking;
        private static readonly int Min;
        private static readonly string[] DenseNames;
        private static readonly Dictionary<int, string> SparseNames;

        static EnumDisplayNameCache()
        {
            Type type = typeof(T);
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
            T[] values = fields.Select(f => (T)f.GetValue(null)).ToArray();
            int[] intValues = values.Select(v => Unsafe.As<T, int>(ref v)).ToArray();
            int min = intValues.Min();
            int max = intValues.Max();
            int range = max - min + 1;

            if (range <= MaxDenseRange)
            {
                UseDensePacking = true;
                Min = min;
                DenseNames = new string[range];

                for (int i = 0; i < fields.Length; i++)
                {
                    int key = intValues[i] - min;
                    FieldInfo field = fields[i];
                    string name = field.IsAttributeDefined(out EnumDisplayNameAttribute displayName)
                        ? displayName.DisplayName
                        : field.Name;
                    DenseNames[key] = name;
                }
            }
            else
            {
                UseDensePacking = false;
                SparseNames = new Dictionary<int, string>();
                for (int i = 0; i < fields.Length; i++)
                {
                    int key = Unsafe.As<T, int>(ref values[i]);
                    FieldInfo field = fields[i];
                    string name = field.IsAttributeDefined(out EnumDisplayNameAttribute displayName)
                        ? displayName.DisplayName
                        : field.Name;
                    SparseNames.TryAdd(key, name);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToDisplayName(T value)
        {
            int key = Unsafe.As<T, int>(ref value);
            if (UseDensePacking)
            {
                int idx = key - Min;
                if ((uint)idx < (uint)DenseNames!.Length)
                {
                    return DenseNames[idx];
                }
            }
            else
            {
                if (SparseNames!.TryGetValue(key, out string name))
                {
                    return name;
                }
            }

            return value.ToString();
        }
    }

    public static class EnumExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasFlagNoAlloc<T>(this T value, T flag)
            where T : unmanaged, Enum
        {
            ulong valueUnderlying = GetUInt64(value);
            ulong flagUnderlying = GetUInt64(flag);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong GetUInt64<T>(T value)
            where T : unmanaged
        {
            /*
                Works because T is constrained to unmanaged, so it's safe to reinterpret
                All enums are value types and have a fixed size
             */
            return sizeof(T) switch
            {
                1 => *(byte*)&value,
                2 => *(ushort*)&value,
                4 => *(uint*)&value,
                8 => *(ulong*)&value,
                _ => throw new ArgumentException(
                    $"Unsupported enum size: {sizeof(T)} for type {typeof(T)}"
                ),
            };
        }
    }
}
