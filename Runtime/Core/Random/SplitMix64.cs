namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    [Serializable]
    [DataContract]
    public sealed class SplitMix64
        : AbstractRandom,
            IEquatable<SplitMix64>,
            IComparable,
            IComparable<SplitMix64>
    {
        public static SplitMix64 Instance => ThreadLocalRandom<SplitMix64>.Instance;

        public override RandomState InternalState => new(_state, 0, _cachedGaussian);

        internal ulong _state;

        public SplitMix64()
            : this(Guid.NewGuid()) { }

        public SplitMix64(Guid guid)
            : this(BitConverter.ToUInt64(guid.ToByteArray(), 0)) { }

        public SplitMix64(ulong seed)
        {
            _state = seed;
        }

        [JsonConstructor]
        public SplitMix64(RandomState internalState)
        {
            _state = internalState.State1;
            _cachedGaussian = internalState.Gaussian;
        }

        public override uint NextUint()
        {
            unchecked
            {
                _state += 0x9E3779B97F4A7C15UL;

                ulong z = _state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                z ^= z >> 31;

                return (uint)z;
            }
        }

        public override IRandom Copy()
        {
            return new SplitMix64(InternalState);
        }

        public bool Equals(SplitMix64 other)
        {
            if (other == null)
            {
                return false;
            }

            return _state == other._state;
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj as SplitMix64);
        }

        public int CompareTo(SplitMix64 other)
        {
            if (other == null)
            {
                return -1;
            }

            return _state.CompareTo(other._state);
        }

        public override int GetHashCode()
        {
            return _state.GetHashCode();
        }

        public override string ToString()
        {
            return $"{{\"State\": {_state}}}";
        }
    }
}
