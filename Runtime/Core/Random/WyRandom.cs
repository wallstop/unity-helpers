﻿namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    // https://github.com/cocowalla/wyhash-dotnet/blob/master/src/WyHash/WyRng.cs
    [Serializable]
    [DataContract]
    public sealed class WyRandom : AbstractRandom
    {
        private const ulong Prime0 = 0xa0761d6478bd642f;
        private const ulong Prime1 = 0xe7037ed1a0b428db;

        public static WyRandom Instance => ThreadLocalRandom<WyRandom>.Instance;

        public override RandomState InternalState => new(_state);

        private ulong _state;

        public WyRandom()
            : this(Guid.NewGuid()) { }

        public WyRandom(Guid guid)
        {
            byte[] guidArray = guid.ToByteArray();
            _state = BitConverter.ToUInt64(guidArray, 0);
        }

        [JsonConstructor]
        public WyRandom(RandomState internalState)
        {
            _state = internalState.State1;
        }

        public WyRandom(ulong state)
        {
            _state = state;
        }

        public override uint NextUint()
        {
            unchecked
            {
                _state += Prime0;
                return (uint)Mum(_state ^ Prime1, _state);
            }
        }

        /// <summary>
        /// Perform a MUM (MUltiply and Mix) operation. Multiplies 2 unsigned 64-bit integers, then combines the
        /// hi and lo bits of the resulting 128-bit integer using XOR
        /// </summary>
        /// <param name="x">First 64-bit integer</param>
        /// <param name="y">Second 64-bit integer</param>
        /// <returns>Result of the MUM (MUltiply and Mix) operation</returns>
        private static ulong Mum(ulong x, ulong y)
        {
            (ulong hi, ulong lo) = Multiply64(x, y);
            return hi ^ lo;
        }

        /// <summary>
        /// Multiplies 2 unsigned 64-bit integers, returning the result in 2 ulongs representing the hi and lo bits
        /// of the resulting 128-bit integer
        ///
        /// Source: https://stackoverflow.com/a/51587262/25758, but with a faster lo calculation
        /// </summary>
        /// <remarks>
        /// <seealso cref="System.Numerics.BigInteger"/> can perform multiplication on large integers, but it's
        /// comparatively slow, and an equivalent method allocates around 360B/call
        /// </remarks>
        /// <param name="x">First 64-bit integer</param>
        /// <param name="y">Second 64-bit integer</param>
        /// <returns>Product of <paramref name="x"/> and <paramref name="y"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage("ReSharper", "JoinDeclarationAndInitializer")]
        private static unsafe (ulong Hi, ulong Lo) Multiply64(ulong x, ulong y)
        {
            ulong hi;
            ulong lo;

            // Use BMI2 intrinsics where available
#if NETCOREAPP3_0_OR_GREATER
            if (System.Runtime.Intrinsics.X86.Bmi2.X64.IsSupported)
            {
                hi = System.Runtime.Intrinsics.X86.Bmi2.X64.MultiplyNoFlags(x, y, &lo);
                return (hi, lo);
            }
#endif

            lo = x * y;

            ulong x0 = (uint)x;
            ulong x1 = x >> 32;

            ulong y0 = (uint)y;
            ulong y1 = y >> 32;

            ulong p11 = x1 * y1;
            ulong p01 = x0 * y1;
            ulong p10 = x1 * y0;
            ulong p00 = x0 * y0;

            // 64-bit product + two 32-bit values
            ulong middle = p10 + (p00 >> 32) + (uint)p01;

            // 64-bit product + two 32-bit values
            hi = p11 + (middle >> 32) + (p01 >> 32);

            return (hi, lo);
        }

        public override IRandom Copy()
        {
            return new WyRandom(_state);
        }
    }
}
