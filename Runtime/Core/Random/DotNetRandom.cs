namespace UnityHelpers.Core.Random
{
    using System;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    [Serializable]
    [DataContract]
    public sealed class DotNetRandom : AbstractRandom
    {
        public static DotNetRandom Instance => ThreadLocalRandom<DotNetRandom>.Instance;

        public override RandomState InternalState =>
            new RandomState(unchecked((ulong)_seed), state2: _numberGenerated);

        private ulong _numberGenerated;
        private int _seed;
        private Random _random;

        public DotNetRandom()
            : this(Guid.NewGuid()) { }

        public DotNetRandom(Guid guid)
        {
            _seed = guid.GetHashCode();
            _random = new Random(_seed);
        }

        [JsonConstructor]
        public DotNetRandom(RandomState internalState)
        {
            _seed = unchecked((int)internalState.State1);
            _random = new Random(_seed);
            _numberGenerated = 0;
            ulong generationCount = internalState.State2;

            while (_numberGenerated < generationCount)
            {
                _ = NextUint();
            }
        }

        public override uint NextUint()
        {
            ++_numberGenerated;
            return unchecked((uint)_random.Next(int.MinValue, int.MaxValue));
        }

        public override IRandom Copy()
        {
            return new DotNetRandom(InternalState);
        }
    }
}
