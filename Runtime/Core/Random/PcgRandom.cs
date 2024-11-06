namespace UnityHelpers.Core.Random
{
    using System;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    ///     Implementation based off of the reference PCG Random, found here: https://www.pcg-random.org/index.html
    /// </summary>
    [Serializable]
    [DataContract]
    public sealed class PcgRandom : AbstractRandom, IEquatable<PcgRandom>, IComparable, IComparable<PcgRandom>
    {
        public static IRandom Instance => ThreadLocalRandom<PcgRandom>.Instance;

        [JsonInclude]
        [JsonPropertyName("Increment")]
        [DataMember(Name = "Increment")]
        internal readonly ulong _increment;

        [JsonInclude]
        [JsonPropertyName("State")]
        [DataMember(Name = "State")]
        internal ulong _state;

        public PcgRandom() : this(Guid.NewGuid()) { }

        public PcgRandom(Guid guid)
        {
            byte[] guidArray = guid.ToByteArray();
            _state = BitConverter.ToUInt64(guidArray, 0);
            _increment = BitConverter.ToUInt64(guidArray, sizeof(ulong));
        }

        public PcgRandom(RandomState randomState)
        {
            _state = randomState.State1;
            _increment = randomState.State2;
            _cachedGaussian = randomState.Gaussian;
        }

        [JsonConstructor]
        public PcgRandom(ulong increment, ulong state)
        {
            _increment = increment;
            _state = state;
        }

        public PcgRandom(long seed)
        {
            // Start with a nice prime
            _increment = 6554638469UL;
            _state = unchecked((ulong)seed);
            _increment = NextUlong();
        }

        public override RandomState InternalState => new(_state, _increment, _cachedGaussian);

        public override uint NextUint()
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

        public bool Equals(PcgRandom other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return _increment == other._increment && _state == other._state && _cachedGaussian == other._cachedGaussian;
        }

        public int CompareTo(PcgRandom other)
        {
            if (ReferenceEquals(other, null))
            {
                return -1;
            }

            if (_increment == other._increment)
            {
                if (_state == other._state)
                {
                    return 0;
                }

                if (_state < other._state)
                {
                    return -1;
                }

                return 1;
            }

            if (_increment < other._increment)
            {
                return -1;
            }

            if (_cachedGaussian.HasValue != other._cachedGaussian.HasValue)
            {
                return _cachedGaussian.HasValue ? -1 : 1;
            }

            if (!_cachedGaussian.HasValue)
            {
                return 0;
            }

            // ReSharper disable once PossibleInvalidOperationException
            return _cachedGaussian.Value.CompareTo(other._cachedGaussian.Value);
        }

        public override bool Equals(object other)
        {
            return Equals(other as PcgRandom);
        }

        public override int GetHashCode()
        {
            return _increment.GetHashCode();
        }

        public override string ToString()
        {
            return $"{{\"Increment\": {_increment}, \"State\": {_state}}}";
        }

        public int CompareTo(object other)
        {
            return CompareTo(other as PcgRandom);
        }

        public override IRandom Copy()
        {
            return new PcgRandom(InternalState);
        }
    }
}