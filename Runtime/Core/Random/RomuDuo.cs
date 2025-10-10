namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;
    using Extension;
    using Helper;
    using ProtoBuf;

    [Serializable]
    [DataContract]
    [ProtoContract]
    /// <summary>
    /// A member of the ROMU family (RomuDuo) emphasizing high speed and good statistical quality on modern CPUs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// RomuDuo maintains two 64-bit state variables and uses rotations and multiplies to evolve the state. It is
    /// competitive with Xoroshiro-style generators in speed while exhibiting strong distribution for general use.
    /// </para>
    /// <para>Pros:</para>
    /// <list type="bullet">
    /// <item><description>Very fast; excellent for real-time usage.</description></item>
    /// <item><description>Good statistical behavior for non-crypto applications.</description></item>
    /// <item><description>Deterministic and reproducible across platforms.</description></item>
    /// </list>
    /// <para>Cons:</para>
    /// <list type="bullet">
    /// <item><description>Not cryptographically secure.</description></item>
    /// <item><description>Relatively newer family; choose proven options if organizational policy requires long-term validation.</description></item>
    /// </list>
    /// <para>When to use:</para>
    /// <list type="bullet">
    /// <item><description>Gameplay RNG, procedural content generation, fast Monte Carlo sampling.</description></item>
    /// </list>
    /// <para>When not to use:</para>
    /// <list type="bullet">
    /// <item><description>Security/adversarial contexts.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// using WallstopStudios.UnityHelpers.Core.Random;
    ///
    /// var rng = new RomuDuo(Guid.NewGuid());
    /// var point = rng.NextVector3InSphere(5f); // via RandomExtensions
    /// double normal = rng.NextGaussian(0.0, 1.0);
    /// </code>
    /// </example>
    public sealed class RomuDuo
        : AbstractRandom,
            IEquatable<RomuDuo>,
            IComparable,
            IComparable<RomuDuo>
    {
        public static RomuDuo Instance => ThreadLocalRandom<RomuDuo>.Instance;
        public override RandomState InternalState => BuildState(_x, _y);

        [ProtoMember(6)]
        internal ulong _x;

        [ProtoMember(7)]
        internal ulong _y;

        private void EnsureNonZeroState()
        {
            if ((_x | _y) == 0)
            {
                _x = 0xD3833E804F4C574BUL;
                _y = 0x94D049BB133111EBUL;
            }
        }

        public RomuDuo()
            : this(Guid.NewGuid()) { }

        public RomuDuo(Guid guid)
        {
            (ulong a, ulong b) = RandomUtilities.GuidToUInt64Pair(guid);
            _x = a;
            _y = b;
            EnsureNonZeroState();
        }

        public RomuDuo(ulong seedX, ulong seedY)
        {
            _x = seedX;
            _y = seedY;
            EnsureNonZeroState();
        }

        [JsonConstructor]
        public RomuDuo(RandomState internalState)
        {
            _x = internalState.State1;
            _y = internalState.State2;
            RestoreCommonState(internalState);
            EnsureNonZeroState();
        }

        public override uint NextUint()
        {
            unchecked
            {
                EnsureNonZeroState();
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
            return Objects.HashCode(_x, _y);
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
