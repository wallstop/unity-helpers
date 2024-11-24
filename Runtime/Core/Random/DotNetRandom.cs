namespace UnityHelpers.Core.Random
{
    using System;

    [Serializable]
    public sealed class DotNetRandom : AbstractRandom
    {
        public override RandomState InternalState =>
            new RandomState(unchecked((ulong)_seed), state2: _numberGenerated);

        private ulong _numberGenerated;
        private int _seed;
        private Random _random;

        public DotNetRandom()
            : this(Guid.NewGuid()) { }

        public DotNetRandom(Guid guid)
        {
            byte[] guidArray = guid.ToByteArray();
            _seed = BitConverter.ToInt32(guidArray, 0);
            _random = new Random(_seed);
        }

        public DotNetRandom(RandomState state)
        {
            _seed = unchecked((int)state.State1);
            _random = new Random(_seed);
            _numberGenerated = 0;
            ulong generationCount = state.State2;

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
