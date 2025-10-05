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

        private int _hashCode;

        [JsonConstructor]
        public RandomState(
            ulong state1,
            ulong state2 = 0,
            double? gaussian = null,
            byte[] payload = null
        )
        {
            _state1 = state1;
            _state2 = state2;
            _hasGaussian = gaussian.HasValue;
            _gaussian = gaussian ?? 0;
            _payload = payload?.ToArray();
            _hashCode = Objects.HashCode(
                _state1,
                _state2,
                _hasGaussian,
                _gaussian,
                _payload?.Length
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
            _hashCode = Objects.HashCode(
                _state1,
                _state2,
                _hasGaussian,
                _gaussian,
                _payload?.Length
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
                _payload?.Length
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
                && (!_hasGaussian || _gaussian == other._gaussian);
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
                    _payload?.Length
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
