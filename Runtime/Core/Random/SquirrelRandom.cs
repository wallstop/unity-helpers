namespace UnityHelpers.Core.Random
{
    using System;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    // https://youtu.be/LWFzPP8ZbdU?t=2673
    [DataContract]
    [Serializable]
    public sealed class SquirrelRandom : AbstractRandom
    {
        private const uint BitNoise1 = 0xB5297A4D;
        private const uint BitNoise2 = 0x68E31DA4;
        private const uint BitNoise3 = 0x1B56C4E9;
        private const int LargePrime = 198491317;

        public static readonly SquirrelRandom Instance = ThreadLocalRandom<SquirrelRandom>.Instance;

        public override RandomState InternalState => new(_position, gaussian: _cachedGaussian);

        private uint _position;

        public SquirrelRandom()
            : this(Guid.NewGuid().GetHashCode()) { }

        public SquirrelRandom(int seed)
        {
            _position = unchecked((uint)seed);
        }

        [JsonConstructor]
        public SquirrelRandom(RandomState internalState)
        {
            _position = unchecked((uint)internalState.State1);
            _cachedGaussian = internalState.Gaussian;
        }

        public override uint NextUint()
        {
            return _position = NextUintInternal(_position);
        }

        // Does not advance the RNG
        public float NextNoise(int x, int y)
        {
            return NextNoise(x, y, _position);
        }

        public override IRandom Copy()
        {
            return new SquirrelRandom(InternalState);
        }

        private static uint NextUintInternal(uint seed)
        {
            uint result = seed;
            result *= BitNoise1;
            result ^= (result >> 8);
            result += BitNoise2;
            result ^= (result << 8);
            result *= BitNoise3;
            result ^= (result >> 8);
            return result;
        }

        // https://youtu.be/LWFzPP8ZbdU?t=2906
        private static float NextNoise(int x, uint seed)
        {
            uint result = unchecked((uint)x);
            result *= BitNoise1;
            result += seed;
            result ^= (result >> 8);
            result += BitNoise2;
            result ^= (result << 8);
            result *= BitNoise3;
            result ^= (result >> 8);
            return (result >> 8) * MagicFloat;
        }

        private static float NextNoise(int x, int y, uint seed)
        {
            return NextNoise(x + (LargePrime * y), seed);
        }
    }
}
