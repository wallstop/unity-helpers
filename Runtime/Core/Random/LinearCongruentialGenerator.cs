namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System;
    using System.Text.Json.Serialization;

    public sealed class LinearCongruentialGenerator : AbstractRandom
    {
        public static LinearCongruentialGenerator Instance =>
            ThreadLocalRandom<LinearCongruentialGenerator>.Instance;

        public override RandomState InternalState => new(_state, 0, _cachedGaussian);

        private uint _state;

        public LinearCongruentialGenerator()
            : this(Guid.NewGuid()) { }

        public LinearCongruentialGenerator(int seed)
        {
            _state = unchecked((uint)seed);
        }

        public LinearCongruentialGenerator(Guid seed)
        {
            _state = unchecked((uint)seed.GetHashCode());
        }

        [JsonConstructor]
        public LinearCongruentialGenerator(RandomState internalState)
        {
            _state = unchecked((uint)internalState.State1);
            _cachedGaussian = internalState.Gaussian;
        }

        public override uint NextUint()
        {
            unchecked
            {
                _state = _state * 1664525U + 1013904223U;
            }
            return _state;
        }

        public override IRandom Copy()
        {
            return new LinearCongruentialGenerator(InternalState);
        }
    }
}
