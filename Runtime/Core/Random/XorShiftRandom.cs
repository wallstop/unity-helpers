namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;
    using ProtoBuf;

    [Serializable]
    [DataContract]
    [ProtoContract]
    public sealed class XorShiftRandom : AbstractRandom
    {
        public static XorShiftRandom Instance => ThreadLocalRandom<XorShiftRandom>.Instance;

        public override RandomState InternalState => new(_state, 0, _cachedGaussian);

        private const uint DefaultState = 2463534242U;

        [ProtoMember(2)]
        private uint _state;

        private static uint NormalizeState(uint state)
        {
            return state != 0 ? state : DefaultState;
        }

        public XorShiftRandom()
            : this(Guid.NewGuid()) { }

        public XorShiftRandom(int state)
        {
            _state = NormalizeState(unchecked((uint)state));
        }

        public XorShiftRandom(Guid seed)
        {
            _state = NormalizeState(unchecked((uint)seed.GetHashCode()));
        }

        [JsonConstructor]
        public XorShiftRandom(RandomState internalState)
        {
            _state = NormalizeState(unchecked((uint)internalState.State1));
            _cachedGaussian = internalState.Gaussian;
        }

        public override uint NextUint()
        {
            _state = NormalizeState(_state);
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
