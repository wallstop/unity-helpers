namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;
    using ProtoBuf;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// StormDrop32: a large-state ARX generator inspired by SHISHUA-style buffer mixing, emphasizing long periods and diffusion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// https://github.com/wileylooper/stormdrop
    /// Ported from <c>wileylooper/stormdrop</c>. The 32-bit variant maintains a 1024-element ring buffer and two 32-bit
    /// accumulators. Each step mixes the current index with the accumulators, rotates, and feeds the buffer to provide
    /// high-quality sequences suitable for heavy simulation workloads.
    /// </para>
    /// <para>Pros:</para>
    /// <list type="bullet">
    /// <item><description>Large period and strong diffusion thanks to the 4 KB buffer.</description></item>
    /// <item><description>Deterministic snapshots via <see cref="RandomState"/>.</description></item>
    /// <item><description>Thread-local access available via <see cref="ThreadLocalRandom{T}.Instance"/>.</description></item>
    /// </list>
    /// <para>Cons:</para>
    /// <list type="bullet">
    /// <item><description>Higher per-instance memory compared to smaller generators.</description></item>
    /// <item><description>Not cryptographically secure.</description></item>
    /// </list>
    /// <para>When to use:</para>
    /// <list type="bullet">
    /// <item><description>Procedural workloads needing long non-overlapping streams or large batches.</description></item>
    /// </list>
    /// <para>When not to use:</para>
    /// <list type="bullet">
    /// <item><description>Memory-constrained contexts; prefer smaller-state generators like FlurryBurst.</description></item>
    /// <item><description>Security/adversarial scenarios.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// using WallstopStudios.UnityHelpers.Core.Random;
    ///
    /// StormDropRandom rng = new StormDropRandom(seed: 42u);
    /// float noise = rng.NextFloat();
    /// Vector3 point = rng.NextVector3InSphere(10f); // via RandomExtensions
    /// </code>
    /// </example>
    [Serializable]
    [DataContract]
    [ProtoContract(SkipConstructor = true)]
    public sealed class StormDropRandom
        : AbstractRandom,
            IEquatable<StormDropRandom>,
            IComparable,
            IComparable<StormDropRandom>
    {
        private const uint Increment = 1_111_111_111U;
        private const int ElementCount = 1024;
        private const int ElementMask = ElementCount - 1;
        private const int ElementByteSize = ElementCount * sizeof(uint);
        private const int WarmupRounds = 128;

        public static StormDropRandom Instance => ThreadLocalRandom<StormDropRandom>.Instance;

        public override RandomState InternalState
        {
            get
            {
                using PooledResource<byte[]> payloadLease = WallstopArrayPool<byte>.Get(
                    ElementByteSize,
                    out byte[] buffer
                );
                Buffer.BlockCopy(_elements, 0, buffer, 0, ElementByteSize);

                ulong state1 = ((ulong)_a << 32) | _b;
                return BuildState(
                    state1,
                    payload: new ArraySegment<byte>(buffer, 0, ElementByteSize)
                );
            }
        }

        [ProtoMember(6)]
        private uint[] _elements = new uint[ElementCount];

        [ProtoMember(7)]
        private uint _a;

        [ProtoMember(8)]
        private uint _b;

        public StormDropRandom()
            : this(Guid.NewGuid()) { }

        public StormDropRandom(Guid guid)
        {
            InitializeFromGuid(guid);
        }

        public StormDropRandom(uint seed)
        {
            uint seedB = seed ^ 0x9E3779B9U;
            if (seedB == 0)
            {
                seedB = 1U;
            }

            InitializeFromScalars(seed, seedB);
        }

        public StormDropRandom(uint seedA, uint seedB)
        {
            InitializeFromScalars(seedA, seedB == 0 ? 1U : seedB);
        }

        [JsonConstructor]
        public StormDropRandom(RandomState internalState)
        {
            _a = (uint)(internalState.State1 >> 32);
            _b = (uint)internalState.State1;
            LoadSerializedElements(internalState._payload);
            RestoreCommonState(internalState);
        }

        public override uint NextUint()
        {
            unchecked
            {
                uint index = _b & ElementMask;
                uint mix = (_elements[index] ^ _a) + _b;

                _a = RotateLeft(_a, 17) ^ _b;
                _b += Increment;

                _elements[_b & ElementMask] += RotateLeft(mix, 13);

                return mix;
            }
        }

        public override IRandom Copy()
        {
            return new StormDropRandom(InternalState);
        }

        public bool Equals(StormDropRandom other)
        {
            if (other == null)
            {
                return false;
            }

            if (_a != other._a || _b != other._b)
            {
                return false;
            }

            if (!_elements.AsSpan().SequenceEqual(other._elements))
            {
                return false;
            }

            return _cachedGaussian == other._cachedGaussian;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as StormDropRandom);
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj as StormDropRandom);
        }

        public int CompareTo(StormDropRandom other)
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

            for (int i = 0; i < ElementCount; ++i)
            {
                comparison = _elements[i].CompareTo(other._elements[i]);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return 0;
        }

        public override int GetHashCode()
        {
            return Objects.HashCode(_a, _b);
        }

        public override string ToString()
        {
            return this.ToJson();
        }

        private void InitializeFromGuid(Guid guid)
        {
            (ulong seed0, ulong seed1) = RandomUtilities.GuidToUInt64Pair(guid);
            ulong mixer = seed0 ^ (seed1 << 1) ^ 0x9E3779B97F4A7C15UL;
            InitializeFromMixer(ref mixer);
        }

        private void InitializeFromScalars(uint seedA, uint seedB)
        {
            ulong mixer = ((ulong)seedA << 32) | seedB;
            mixer ^= 0xD2B74407B1CE6E93UL;
            InitializeFromMixer(ref mixer);
        }

        private void InitializeFromMixer(ref ulong mixer)
        {
            if (_elements == null || _elements.Length != ElementCount)
            {
                _elements = new uint[ElementCount];
            }

            for (int i = 0; i < ElementCount; ++i)
            {
                _elements[i] = Mix32(ref mixer);
            }

            _a = Mix32(ref mixer);
            _b = Mix32(ref mixer) | 1U;

            for (int i = 0; i < WarmupRounds; ++i)
            {
                _ = NextUint();
            }
        }

        private void LoadSerializedElements(byte[] payload)
        {
            if (_elements == null || _elements.Length != ElementCount)
            {
                _elements = new uint[ElementCount];
            }

            if (payload != null && payload.Length >= ElementByteSize)
            {
                Buffer.BlockCopy(payload, 0, _elements, 0, ElementByteSize);
                return;
            }

            Array.Clear(_elements, 0, _elements.Length);
        }
    }
}
