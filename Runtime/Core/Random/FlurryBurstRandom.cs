namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System;
    using System.Buffers.Binary;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;
    using Extension;
    using Helper;
    using ProtoBuf;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// FlurryBurst32: a six-word ARX-style generator offering high quality and excellent parallel sequencing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// https://github.com/wileylooper/flurryburst
    /// Based on <c>wileylooper/flurryburst</c>, this implementation captures the 32-bit variant that balances
    /// speed, period (~2<sup>128</sup>) and state size for gameplay workloads. It is suitable as a drop-in
    /// alternative to Xoshiro128** and similar families, while retaining deterministic serialization support.
    /// </para>
    /// <para>Pros:</para>
    /// <list type="bullet">
    /// <item><description>Small state (6×32-bit) with excellent statistical behaviour.</description></item>
    /// <item><description>Deterministic snapshots via <see cref="RandomState"/> and protobuf/JSON.</description></item>
    /// <item><description>Easy to create parallel streams by varying the <c>d</c> word.</description></item>
    /// </list>
    /// <para>Cons:</para>
    /// <list type="bullet">
    /// <item><description>Not cryptographically secure.</description></item>
    /// <item><description>Requires a short warm-up (performed automatically) to avoid transient bias.</description></item>
    /// </list>
    /// <para>When to use:</para>
    /// <list type="bullet">
    /// <item><description>Deterministic gameplay, procedural content, Monte-Carlo style sampling.</description></item>
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
    /// IRandom rng = new FlurryBurstRandom(seed: 123u);
    /// int value = rng.Next(0, 100);
    /// float weight = rng.NextFloat();
    /// </code>
    /// </example>
    [Serializable]
    [DataContract]
    [ProtoContract]
    public sealed class FlurryBurstRandom
        : AbstractRandom,
            IEquatable<FlurryBurstRandom>,
            IComparable,
            IComparable<FlurryBurstRandom>
    {
        private const uint Increment = 1_111_111_111U;
        private const int PayloadByteCount = sizeof(uint) * 2;

        public static FlurryBurstRandom Instance => ThreadLocalRandom<FlurryBurstRandom>.Instance;

        public override RandomState InternalState
        {
            get
            {
                ulong state1 = ((ulong)_a << 32) | _b;
                ulong state2 = ((ulong)_c << 32) | _d;
                using PooledResource<byte[]> payloadLease = WallstopArrayPool<byte>.Get(
                    PayloadByteCount,
                    out byte[] buffer
                );
                Span<byte> payload = buffer.AsSpan(0, PayloadByteCount);
                BinaryPrimitives.WriteUInt32LittleEndian(payload.Slice(0, sizeof(uint)), _e);
                BinaryPrimitives.WriteUInt32LittleEndian(
                    payload.Slice(sizeof(uint), sizeof(uint)),
                    _f
                );

                return BuildState(
                    state1,
                    state2,
                    new ArraySegment<byte>(buffer, 0, PayloadByteCount)
                );
            }
        }

        [ProtoMember(6)]
        private uint _a;

        [ProtoMember(7)]
        private uint _b;

        [ProtoMember(8)]
        private uint _c;

        [ProtoMember(9)]
        private uint _d;

        [ProtoMember(10)]
        private uint _e;

        [ProtoMember(11)]
        private uint _f;

        public FlurryBurstRandom()
            : this(Guid.NewGuid()) { }

        public FlurryBurstRandom(Guid guid)
        {
            InitializeFromGuid(guid);
        }

        [JsonConstructor]
        public FlurryBurstRandom(RandomState internalState)
        {
            ulong state1 = internalState.State1;
            ulong state2 = internalState.State2;
            _a = (uint)(state1 >> 32);
            _b = (uint)state1;
            _c = (uint)(state2 >> 32);
            _d = (uint)state2;

            byte[] payload = internalState._payload;
            if (payload != null && payload.Length >= sizeof(uint) * 2)
            {
                _e = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(0, sizeof(uint)));
                _f = BinaryPrimitives.ReadUInt32LittleEndian(
                    payload.AsSpan(sizeof(uint), sizeof(uint))
                );
            }
            else
            {
                _e = 0;
                _f = 0;
            }

            RestoreCommonState(internalState);
        }

        public override uint NextUint()
        {
            unchecked
            {
                uint mix = RotateLeft(_a, 13);

                _a = _b;
                _b = _e;
                _c += _d;
                _d += _b;
                _e = _f + Increment;
                _f = _c ^ mix;

                return (_e >> 1) ^ _f;
            }
        }

        public override IRandom Copy()
        {
            return new FlurryBurstRandom(InternalState);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FlurryBurstRandom);
        }

        public bool Equals(FlurryBurstRandom other)
        {
            if (other == null)
            {
                return false;
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return _a == other._a
                && _b == other._b
                && _c == other._c
                && _d == other._d
                && _e == other._e
                && _f == other._f
                && _cachedGaussian == other._cachedGaussian;
        }

        public override int GetHashCode()
        {
            return Objects.HashCode(_a, _b, _c, _d, _e, _f);
        }

        public override string ToString()
        {
            return this.ToJson();
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj as FlurryBurstRandom);
        }

        public int CompareTo(FlurryBurstRandom other)
        {
            if (other == null)
            {
                return -1;
            }

            int comparison = _a.CompareTo(other._a);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = _b.CompareTo(other._b);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = _c.CompareTo(other._c);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = _d.CompareTo(other._d);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = _e.CompareTo(other._e);
            if (comparison != 0)
            {
                return comparison;
            }

            return _f.CompareTo(other._f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint RotateLeft(uint value, int count)
        {
            return (value << count) | (value >> (32 - count));
        }

        private void InitializeFromGuid(Guid guid)
        {
            (ulong seed0, ulong seed1) = RandomUtilities.GuidToUInt64Pair(guid);
            InitializeFromUlongs(seed0, seed1);
        }

        private void InitializeFromUlongs(ulong seed0, ulong seed1)
        {
            ulong mixer = seed0 ^ (seed1 << 1) ^ 0x9E3779B97F4A7C15UL;

            _a = Mix(ref mixer);
            _b = Mix(ref mixer);
            _c = Mix(ref mixer);
            _d = Mix(ref mixer);
            if (_d == 0)
            {
                _d = 1;
            }
            _e = Mix(ref mixer);
            _f = Mix(ref mixer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Mix(ref ulong state)
        {
            unchecked
            {
                state += 0x9E3779B97F4A7C15UL;
                ulong z = state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                z ^= z >> 31;
                return (uint)z;
            }
        }
    }
}
