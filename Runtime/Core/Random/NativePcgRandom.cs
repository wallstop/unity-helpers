// MIT License - Copyright (c) 2023 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System;

    /// <summary>
    /// A lightweight PCG-based struct RNG intended for low-overhead use (e.g., Burst/Jobs-friendly contexts).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides a minimal, non-allocating random number source patterned after PCG. Because it is a <c>struct</c>,
    /// it is copied by value; take care to pass by ref where you want a single advancing sequence. Not wired into
    /// <see cref="IRandom"/>; use this when allocations or interface dispatch must be avoided.
    /// </para>
    /// <para>Pros:</para>
    /// <list type="bullet">
    /// <item><description>No allocations, tiny footprint; suitable for tight inner loops.</description></item>
    /// <item><description>Deterministic with good general-purpose quality.</description></item>
    /// </list>
    /// <para>Cons:</para>
    /// <list type="bullet">
    /// <item><description>Value-type semantics—accidental copying can lead to diverging sequences.</description></item>
    /// <item><description>Not cryptographically secure; limited convenience API.</description></item>
    /// </list>
    /// <para>When to use:</para>
    /// <list type="bullet">
    /// <item><description>Performance-critical contexts (Burst/Jobs) where you control by-ref semantics.</description></item>
    /// </list>
    /// <para>When not to use:</para>
    /// <list type="bullet">
    /// <item><description>General gameplay—prefer <see cref="PRNG.Instance"/> with the richer <see cref="IRandom"/> API.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var rng = new NativePcgRandom(seed: 123);
    /// uint u = rng.NextUint();
    /// bool b = rng.NextBool();
    /// float t = rng.NextFloat();
    /// </code>
    /// </example>
    public struct NativePcgRandom
    {
        private const uint HalfwayUint = uint.MaxValue / 2;
        private const double MagicDouble = 4.6566128752458E-10;
        private const float MagicFloat = 5.960465E-008F;

        private readonly ulong _increment;
        private ulong _state;

        public NativePcgRandom(Guid seed)
        {
            (ulong a, ulong b) = RandomUtilities.GuidToUInt64Pair(seed);
            _state = a;
            _increment = b;
        }

        public NativePcgRandom(int seed)
        {
            _increment = 6554638469UL;
            _state = unchecked((ulong)seed);
            _increment = NextUlong();
        }

        public uint NextUint()
        {
            unchecked
            {
                ulong oldState = _state;
                _state = oldState * 6364136223846793005UL + _increment;
                uint xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
                int rot = (int)(oldState >> 59);
                return (xorShifted >> rot) | (xorShifted << (-rot & 31));
            }
        }

        public int Next(int max)
        {
            if (max <= 0)
            {
                throw new ArgumentException($"Max {max} cannot be less-than or equal-to 0");
            }

            return unchecked((int)NextUint(unchecked((uint)max)));
        }

        public uint NextUint(uint max)
        {
            /*
                https://github.com/libevent/libevent/blob/3807a30b03ab42f2f503f2db62b1ef5876e2be80/arc4random.c#L531

                http://cs.stackexchange.com/questions/570/generating-uniformly-distributed-random-numbers-using-a-coin
                Generates a uniform random number within the bound, avoiding modulo bias
            */
            uint threshold = unchecked((uint)((0x100000000UL - max) % max));
            int attempts = 0;
            while (true)
            {
                uint randomValue = NextUint();
                if (threshold <= randomValue)
                {
                    return randomValue % max;
                }
                if (++attempts > 1 << 16)
                {
                    // Prevent infinite loop: return modulo (introduces tiny bias) rather than hang
                    return randomValue % max;
                }
            }
        }

        public long NextLong()
        {
            uint upper = NextUint();
            uint lower = NextUint();
            // Mix things up a little
            if (NextBool())
            {
                return unchecked((long)((ulong)upper << 32) | lower);
            }
            return unchecked((long)((ulong)lower << 32) | upper);
        }

        public ulong NextUlong()
        {
            return unchecked((ulong)NextLong());
        }

        public bool NextBool()
        {
            return NextUint() < HalfwayUint;
        }

        public float NextFloat()
        {
            uint floatAsInt = NextUint();
            return (floatAsInt >> 8) * MagicFloat;
        }
    }
}
