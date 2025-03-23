namespace UnityHelpers.Core.Random
{
    using System;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    [Serializable]
    [DataContract]
    public sealed class XorShiftRandom : AbstractRandom
    {
        public static XorShiftRandom Instance => ThreadLocalRandom<XorShiftRandom>.Instance;

        public override RandomState InternalState => new(_state, 0, _cachedGaussian);

        private uint _state;

        public XorShiftRandom()
            : this(Guid.NewGuid()) { }

        public XorShiftRandom(int state)
        {
            _state = unchecked((uint)state);
            _state = _state != 0 ? _state : 2463534242U;
        }

        public XorShiftRandom(Guid seed)
        {
            _state = unchecked((uint)seed.GetHashCode());
            _state = _state != 0 ? _state : 2463534242U;
        }

        [JsonConstructor]
        public XorShiftRandom(RandomState internalState)
        {
            _state = unchecked((uint)internalState.State1);
            _cachedGaussian = internalState.Gaussian;
        }

        public override uint NextUint()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }

        public override IRandom Copy()
        {
            return new XorShiftRandom(InternalState);
        }
    }
}
