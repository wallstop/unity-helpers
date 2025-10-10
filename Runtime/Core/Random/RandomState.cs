namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;
    using Extension;
    using Helper;
    using ProtoBuf;

    [Serializable]
    [DataContract]
    [ProtoContract]
    public readonly struct RandomState : IEquatable<RandomState>
    {
        [JsonInclude]
        public ulong State1 => _state1;

        [JsonInclude]
        public ulong State2 => _state2;

        [JsonInclude]
        public double? Gaussian
        {
            get
            {
                if (_hasGaussian)
                {
                    return _gaussian;
                }

                return null;
            }
        }

        [JsonIgnore]
        public IReadOnlyList<byte> PayloadBytes => _payload;

        [JsonInclude]
        [JsonPropertyName("Payload")]
        public IReadOnlyList<byte> Payload => _payload;

        // Reservoir state (for AbstractRandom bit/byte reservoirs)
        [JsonInclude]
        public uint BitBuffer => _bitBuffer;

        [JsonInclude]
        public int BitCount => _bitCount;

        [JsonInclude]
        public uint ByteBuffer => _byteBuffer;

        [JsonInclude]
        public int ByteCount => _byteCount;

        [ProtoMember(1)]
        [JsonIgnore]
        private readonly ulong _state1;

        [ProtoMember(2)]
        [JsonIgnore]
        private readonly ulong _state2;

        [ProtoMember(3)]
        [JsonIgnore]
        private readonly bool _hasGaussian;

        [ProtoMember(4)]
        [JsonIgnore]
        private readonly double _gaussian;

        [ProtoMember(5)]
        [JsonIgnore]
        internal readonly byte[] _payload;

        // Added fields for reservoir serialization
        [ProtoMember(6)]
        [JsonIgnore]
        private readonly uint _bitBuffer;

        [ProtoMember(7)]
        [JsonIgnore]
        private readonly int _bitCount;

        [ProtoMember(8)]
        [JsonIgnore]
        private readonly uint _byteBuffer;

        [ProtoMember(9)]
        [JsonIgnore]
        private readonly int _byteCount;

        [ProtoMember(10)]
        [JsonIgnore]
        private readonly int _hashCode;

        [JsonConstructor]
        public RandomState(
            ulong state1,
            ulong state2 = 0,
            double? gaussian = null,
            IReadOnlyList<byte> payload = null,
            uint bitBuffer = 0,
            int bitCount = 0,
            uint byteBuffer = 0,
            int byteCount = 0
        )
        {
            _state1 = state1;
            _state2 = state2;
            _hasGaussian = gaussian.HasValue;
            _gaussian = gaussian ?? 0;
            _payload = (payload as byte[]) ?? payload?.ToArray();
            _bitBuffer = bitBuffer;
            _bitCount = bitCount;
            _byteBuffer = byteBuffer;
            _byteCount = byteCount;
            _hashCode = Objects.HashCode(
                _state1,
                _state2,
                _hasGaussian,
                _gaussian,
                _payload?.Length,
                _bitBuffer,
                _bitCount,
                _byteBuffer,
                _byteCount
            );
        }

        public RandomState(Guid guid)
        {
            (ulong s1, ulong s2) = RandomUtilities.GuidToUInt64Pair(guid);
            _state1 = s1;
            _state2 = s2;
            _hasGaussian = false;
            _gaussian = 0;
            _payload = null;
            _bitBuffer = 0;
            _bitCount = 0;
            _byteBuffer = 0;
            _byteCount = 0;
            _hashCode = Objects.HashCode(
                _state1,
                _state2,
                _hasGaussian,
                _gaussian,
                _payload?.Length,
                _bitBuffer,
                _bitCount,
                _byteBuffer,
                _byteCount
            );
        }

        public override bool Equals(object other)
        {
            return other is RandomState randomState && Equals(randomState);
        }

        public bool Equals(RandomState other)
        {
            bool equivalent =
                _state1 == other._state1
                && _state2 == other._state2
                && _hasGaussian == other._hasGaussian
                && (!_hasGaussian || _gaussian.TotalEquals(other._gaussian))
                && _bitBuffer == other._bitBuffer
                && _bitCount == other._bitCount
                && _byteBuffer == other._byteBuffer
                && _byteCount == other._byteCount;
            if (!equivalent)
            {
                return false;
            }

            if (_payload == null && other._payload == null)
            {
                return true;
            }

            if (_payload == null || other._payload == null)
            {
                return false;
            }

            if (_payload.Length != other._payload.Length)
            {
                return false;
            }

            return _payload.AsSpan().SequenceEqual(other._payload);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override string ToString()
        {
            return this.ToJson();
        }
    }
}
