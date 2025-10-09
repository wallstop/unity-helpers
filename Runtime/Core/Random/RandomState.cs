namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;
    using Extension;
    using Helper;
    using ProtoBuf;

    [Serializable]
    [DataContract]
    [ProtoContract]
    public struct RandomState : IEquatable<RandomState>
    {
        public ulong State1 => _state1;

        public ulong State2 => _state2;

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

        public byte[] Payload => _payload;

        // Reservoir state (for AbstractRandom bit/byte reservoirs)
        public uint BitBuffer => _bitBuffer;
        public int BitCount => _bitCount;
        public uint ByteBuffer => _byteBuffer;
        public int ByteCount => _byteCount;

        [ProtoMember(1)]
        private ulong _state1;

        [ProtoMember(2)]
        private ulong _state2;

        [ProtoMember(3)]
        private bool _hasGaussian;

        [ProtoMember(4)]
        private double _gaussian;

        [ProtoMember(5)]
        private byte[] _payload;

        // Added fields for reservoir serialization
        [ProtoMember(6)]
        private uint _bitBuffer;

        [ProtoMember(7)]
        private int _bitCount;

        [ProtoMember(8)]
        private uint _byteBuffer;

        [ProtoMember(9)]
        private int _byteCount;

        private int _hashCode;

        [JsonConstructor]
        public RandomState(
            ulong state1,
            ulong state2 = 0,
            double? gaussian = null,
            byte[] payload = null,
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
            _payload = payload?.ToArray();
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
            byte[] guidBytes = guid.ToByteArray();
            _state1 = BitConverter.ToUInt64(guidBytes, 0);
            _state2 = BitConverter.ToUInt64(guidBytes, sizeof(ulong));
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

        [ProtoAfterDeserialization]
        private void OnProtoDeserialize()
        {
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
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            bool equivalent =
                _state1 == other._state1
                && _state2 == other._state2
                && _hasGaussian == other._hasGaussian
                && (!_hasGaussian || _gaussian == other._gaussian)
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
            if (_hashCode == 0)
            {
                return _hashCode = Objects.HashCode(
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

            return _hashCode;
        }

        public override string ToString()
        {
            return this.ToJson();
        }
    }
}
