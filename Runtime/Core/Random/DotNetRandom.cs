namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;
    using ProtoBuf;

    [Serializable]
    [DataContract]
    [ProtoContract(SkipConstructor = true)]
    /// <summary>
    /// A thin wrapper around <c>System.Random</c> that exposes the <see cref="IRandom"/> API and supports state capture.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses a real <c>System.Random</c> internally and advances it to reflect the captured <see cref="RandomState"/>.
    /// This makes it easy to interop with code that expects <c>System.Random</c> semantics while using the unified
    /// <see cref="IRandom"/> interface.
    /// </para>
    /// <para>Pros:</para>
    /// <list type="bullet">
    /// <item><description>Compatibility with <c>System.Random</c> behavior.</description></item>
    /// <item><description>Unified API; determinism via state capture.</description></item>
    /// </list>
    /// <para>Cons:</para>
    /// <list type="bullet">
    /// <item><description>Slower than modern PRNGs; not cryptographically secure.</description></item>
    /// <item><description>Internal advance required after deserialization to sync to generation count.</description></item>
    /// </list>
    /// <para>When to use:</para>
    /// <list type="bullet">
    /// <item><description>Bridging code that uses <c>System.Random</c> to the <see cref="IRandom"/> ecosystem.</description></item>
    /// </list>
    /// <para>When not to use:</para>
    /// <list type="bullet">
    /// <item><description>Performance-critical or quality-sensitive randomness—prefer PCG or IllusionFlow.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// using WallstopStudios.UnityHelpers.Core.Random;
    ///
    /// var compat = new DotNetRandom(Guid.NewGuid());
    /// // Use IRandom methods while maintaining System.Random semantics
    /// byte b = compat.NextByte();
    /// float f = compat.NextFloat();
    /// </code>
    /// </example>
    public sealed class DotNetRandom : AbstractRandom
    {
        public static DotNetRandom Instance => ThreadLocalRandom<DotNetRandom>.Instance;

        public override RandomState InternalState =>
            BuildState(unchecked((ulong)_seed), state2: _numberGenerated);

        [ProtoMember(6)]
        private ulong _numberGenerated;

        [ProtoMember(7)]
        private int _seed;

        private Random _random;

        public DotNetRandom()
            : this(Guid.NewGuid()) { }

        public DotNetRandom(Guid guid)
        {
            // Derive a deterministic 32-bit seed from GUID bytes rather than GetHashCode()
            // to avoid runtime/platform hash differences.
            unchecked
            {
                byte[] gb = guid.ToByteArray();
                _seed = gb[0] | (gb[1] << 8) | (gb[2] << 16) | (gb[3] << 24);
            }
            _random = new Random(_seed);
        }

        [JsonConstructor]
        public DotNetRandom(RandomState internalState)
        {
            _seed = unchecked((int)internalState.State1);
            _numberGenerated = internalState.State2;
            RestoreCommonState(internalState);
            EnsureRandomInitialized();
        }

        [ProtoAfterDeserialization]
        private void OnProtoDeserialize()
        {
            EnsureRandomInitialized();
        }

        private void EnsureRandomInitialized()
        {
            if (_random != null)
            {
                return;
            }

            // Reconstruct System.Random with the deserialized seed
            _random = new Random(_seed);

            // Advance System.Random to match the generation count without touching protobuf fields
            for (ulong i = 0; i < _numberGenerated; ++i)
            {
                _ = _random.Next(int.MinValue, int.MaxValue);
            }
        }

        public override uint NextUint()
        {
            EnsureRandomInitialized();
            ++_numberGenerated;
            return unchecked((uint)_random.Next(int.MinValue, int.MaxValue));
        }

        public override IRandom Copy()
        {
            return new DotNetRandom(InternalState);
        }
    }
}
