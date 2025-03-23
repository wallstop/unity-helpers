namespace UnityHelpers.Core.Random
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;
    using Extension;
    using Helper;

    [Serializable]
    [DataContract]
    public sealed class RomuDuo
        : AbstractRandom,
            IEquatable<RomuDuo>,
            IComparable,
            IComparable<RomuDuo>
    {
        public static RomuDuo Instance => ThreadLocalRandom<RomuDuo>.Instance;
        public override RandomState InternalState => new(_x, _y, _cachedGaussian);

        internal ulong _x;
        internal ulong _y;

        public RomuDuo()
            : this(Guid.NewGuid()) { }

        public RomuDuo(Guid guid)
        {
            byte[] bytes = guid.ToByteArray();
            _x = BitConverter.ToUInt64(bytes, 0);
            _y = BitConverter.ToUInt64(bytes, sizeof(ulong));
        }

        public RomuDuo(ulong seedX, ulong seedY)
        {
            _x = seedX;
            _y = seedY;
        }

        [JsonConstructor]
        public RomuDuo(RandomState internalState)
        {
            _x = internalState.State1;
            _y = internalState.State2;
            _cachedGaussian = internalState.Gaussian;
        }

        public override uint NextUint()
        {
            unchecked
            {
                ulong xp = _x;
                _x = 15241094284759029579UL * _y;
                _y = Rol64(_y, 27) + xp;
                return (uint)xp;
            }
        }

        public override IRandom Copy()
        {
            return new RomuDuo(InternalState);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Rol64(ulong x, int k)
        {
            return (x << k) | (x >> (64 - k));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RomuDuo);
        }

        public bool Equals(RomuDuo other)
        {
            if (other == null)
            {
                return false;
            }

            return _x == other._x && _y == other._y;
        }

        public override int GetHashCode()
        {
            return Objects.ValueTypeHashCode(_x, _y);
        }

        public override string ToString()
        {
            return this.ToJson();
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj as RomuDuo);
        }

        public int CompareTo(RomuDuo other)
        {
            if (other == null)
            {
                return -1;
            }

            int comparison = _x.CompareTo(other._x);
            if (comparison != 0)
            {
                return comparison;
            }

            return _y.CompareTo(other._y);
        }
    }
}
