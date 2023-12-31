namespace UnityHelpers.Core.Random
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public sealed class XorShiftRandom : AbstractRandom
    {
        public static IRandom Instance => ThreadLocalRandom<XorShiftRandom>.Instance;
        public override RandomState InternalState => new(_state, 0, _cachedGaussian);

        [DataMember(Name = "State")]
        private uint _state;

        public XorShiftRandom() : this(Guid.NewGuid().GetHashCode())
        {
        }

        public XorShiftRandom(int state)
        {
            _state = unchecked((uint)state);
        }

        public XorShiftRandom(RandomState state)
        {
            _state = unchecked((uint)state.State1);
            _cachedGaussian = state.Gaussian;
        }

        public override uint NextUint()
        {
            uint state = _state;
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            _state = state;
            return state;
        }

        public override IRandom Copy()
        {
            return new XorShiftRandom(InternalState);
        }
    }
}
