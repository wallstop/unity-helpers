namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Text.Json.Serialization;
    using Extension;
    using Helper;

    public sealed class XorShiroRandom
        : AbstractRandom,
            IEquatable<XorShiroRandom>,
            IComparable,
            IComparable<XorShiroRandom>
    {
        public static XorShiroRandom Instance => ThreadLocalRandom<XorShiroRandom>.Instance;

        public override RandomState InternalState => new(_s0, _s1, _cachedGaussian);

        internal ulong _s0;
        internal ulong _s1;

        public XorShiroRandom()
            : this(Guid.NewGuid()) { }

        public XorShiroRandom(Guid guid)
        {
            byte[] bytes = guid.ToByteArray();
            _s0 = BitConverter.ToUInt64(bytes, 0);
            _s1 = BitConverter.ToUInt64(bytes, 8);
        }

        public XorShiroRandom(ulong seed1, ulong seed2)
        {
            _s0 = seed1;
            _s1 = seed2;
        }

        [JsonConstructor]
        public XorShiroRandom(RandomState internalState)
        {
            _s0 = internalState.State1;
            _s1 = internalState.State2;
            _cachedGaussian = internalState.Gaussian;
        }

        public override uint NextUint()
        {
            unchecked
            {
                ulong s0 = _s0;
                ulong s1 = _s1;
                ulong result = s0 + s1;

                s1 ^= s0;
                _s0 = Rotl(s0, 24) ^ s1 ^ (s1 << 16);
                _s1 = Rotl(s1, 37);

                return (uint)result;
            }
        }

        public override IRandom Copy()
        {
            return new XorShiroRandom(InternalState);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Rotl(ulong x, int k)
        {
            return (x << k) | (x >> (64 - k));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as XorShiroRandom);
        }

        public bool Equals(XorShiroRandom other)
        {
            if (other == null)
            {
                return false;
            }

            return _s0 == other._s0 && _s1 == other._s1;
        }

        public override int GetHashCode()
        {
            return Objects.ValueTypeHashCode(_s0, _s1);
        }

        public override string ToString()
        {
            return this.ToJson();
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj as XorShiroRandom);
        }

        public int CompareTo(XorShiroRandom other)
        {
            if (other == null)
            {
                return -1;
            }

            int comparison = _s0.CompareTo(other._s0);
            if (comparison != 0)
            {
                return comparison;
            }

            return _s1.CompareTo(other._s1);
        }
    }
}
