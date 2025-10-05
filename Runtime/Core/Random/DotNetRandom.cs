namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;
    using ProtoBuf;

    [Serializable]
    [DataContract]
    [ProtoContract]
    public sealed class DotNetRandom : AbstractRandom
    {
        public static DotNetRandom Instance => ThreadLocalRandom<DotNetRandom>.Instance;

        public override RandomState InternalState =>
            new(unchecked((ulong)_seed), state2: _numberGenerated);

        [ProtoMember(2)]
        private ulong _numberGenerated;

        [ProtoMember(3)]
        private int _seed;

        private Random _random;

        public DotNetRandom()
            : this(Guid.NewGuid()) { }

        public DotNetRandom(Guid guid)
        {
            _seed = guid.GetHashCode();
            _random = new Random(_seed);
        }

        [JsonConstructor]
        public DotNetRandom(RandomState internalState)
        {
            _seed = unchecked((int)internalState.State1);
            _random = new Random(_seed);
            _numberGenerated = 0;
            ulong generationCount = internalState.State2;

            while (_numberGenerated < generationCount)
            {
                _ = NextUint();
            }
        }

        [ProtoAfterDeserialization]
        private void OnProtoDeserialize()
        {
            _random = new Random(_seed);
            ulong count = _numberGenerated;
            _numberGenerated = 0;
            while (_numberGenerated < count)
            {
                _ = NextUint();
            }
        }

        public override uint NextUint()
        {
            ++_numberGenerated;
            return unchecked((uint)_random.Next(int.MinValue, int.MaxValue));
        }

        public override IRandom Copy()
        {
            return new DotNetRandom(InternalState);
        }
    }
}
