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
        private static readonly Dictionary<T, string> Names;

        static EnumNameCache()
        {
            T[] values = Enum.GetValues(typeof(T)).OfType<T>().ToArray();
            Names = new Dictionary<T, string>(values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                T value = values[i];
                string name = value.ToString("G");
                Names.TryAdd(value, name);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToCachedName(T value)
        {
            if (Names.TryGetValue(value, out string name))
            {
                return name;
            }

            return value.ToString("G");
        }
    }

    public static class EnumDisplayNameCache<T>
        where T : struct, Enum
    {
        private static readonly Dictionary<T, string> Names;

        static EnumDisplayNameCache()
        {
            Type type = typeof(T);
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
            Names = new Dictionary<T, string>(fields.Length);

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
                Names.TryAdd(value, name);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToDisplayName(T value)
        {
            if (Names.TryGetValue(value, out string name))
            {
                return name;
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
            ulong? valueUnderlying = GetUInt64(value);
            ulong? flagUnderlying = GetUInt64(flag);
            if (valueUnderlying == null || flagUnderlying == null)
            {
                // Fallback for unsupported enum sizes
                return value.HasFlag(flag);
            }

            return (valueUnderlying.Value & flagUnderlying.Value) == flagUnderlying.Value;
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
        private static ulong? GetUInt64<T>(T value)
            where T : unmanaged
        {
            try
            {
                return Unsafe.SizeOf<T>() switch
                {
                    1 => Unsafe.As<T, byte>(ref value),
                    2 => Unsafe.As<T, ushort>(ref value),
                    4 => Unsafe.As<T, uint>(ref value),
                    8 => Unsafe.As<T, ulong>(ref value),
                    _ => null,
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
