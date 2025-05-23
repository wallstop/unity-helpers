﻿namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System;

    public struct NativePcgRandom
    {
        private const uint HalfwayUint = uint.MaxValue / 2;
        private const double MagicDouble = 4.6566128752458E-10;
        private const float MagicFloat = 5.960465E-008F;

        private readonly ulong _increment;
        private ulong _state;

        public NativePcgRandom(Guid seed)
        {
            byte[] guidArray = seed.ToByteArray();
            _state = BitConverter.ToUInt64(guidArray, 0);
            _increment = BitConverter.ToUInt64(guidArray, sizeof(ulong));
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
            while (true)
            {
                uint randomValue = NextUint();
                if (threshold <= randomValue)
                {
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
