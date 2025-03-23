namespace UnityHelpers.Core.Extension
{
    using System;
    using System.Runtime.CompilerServices;

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
