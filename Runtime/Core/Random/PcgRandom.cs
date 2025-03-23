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
    public sealed class PcgRandom
        : AbstractRandom,
            IEquatable<PcgRandom>,
            IComparable,
            IComparable<PcgRandom>
    {
        public static PcgRandom Instance => ThreadLocalRandom<PcgRandom>.Instance;

        public override RandomState InternalState => new(_state, _increment, _cachedGaussian);

        internal readonly ulong _increment;

        internal ulong _state;

        public PcgRandom()
            : this(Guid.NewGuid()) { }

        public PcgRandom(Guid guid)
        {
            byte[] guidArray = guid.ToByteArray();
            _state = BitConverter.ToUInt64(guidArray, 0);
            _increment = BitConverter.ToUInt64(guidArray, sizeof(ulong));
        }

        [JsonConstructor]
        public PcgRandom(RandomState internalState)
        {
            _state = internalState.State1;
            _increment = internalState.State2;
            _cachedGaussian = internalState.Gaussian;
        }

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
            return _increment == other._increment
                && _state == other._state
                && _cachedGaussian == other._cachedGaussian;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PcgRandom);
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

        public int CompareTo(object obj)
        {
            return CompareTo(obj as PcgRandom);
        }

        public override int GetHashCode()
        {
            return _increment.GetHashCode();
        }

        public override string ToString()
        {
            return $"{{\"Increment\": {_increment}, \"State\": {_state}}}";
        }

        public override IRandom Copy()
        {
            return new PcgRandom(InternalState);
        }
    }
}
