namespace WallstopStudios.UnityHelpers.Core.DataStructure.Adapters
{
    using System;
    using System.Buffers.Binary;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;
    using Helper;
    using ProtoBuf;
    using UnityEngine;

    [Serializable]
    [DataContract]
    [ProtoContract]
    public struct WGuid
        : IEquatable<WGuid>,
            IEquatable<Guid>,
            IComparable<WGuid>,
            IComparable<Guid>,
            IComparable,
            IFormattable
    {
        public static readonly WGuid EmptyGuid = default;

        public static WGuid Empty => default;

        internal const string LowFieldName = nameof(_low);
        internal const string HighFieldName = nameof(_high);
        internal const string GuidPropertyName = nameof(Guid);

        [ProtoMember(1)]
        [SerializeField]
        private long _low;

        [ProtoMember(2)]
        [SerializeField]
        private long _high;

        [JsonInclude]
        [DataMember]
        private string Guid => ToString();

        public static WGuid NewGuid()
        {
            return new WGuid(global::System.Guid.NewGuid());
        }

        public WGuid(Guid guid)
        {
            _low = 0L;
            _high = 0L;
            SetFromGuid(guid);
        }

        [JsonConstructor]
        public WGuid(string guid)
            : this(ParseGuidString(guid)) { }

        public WGuid(byte[] guidBytes)
        {
            if (guidBytes == null)
            {
                throw new ArgumentNullException(nameof(guidBytes));
            }

            if (guidBytes.Length != 16)
            {
                throw new ArgumentOutOfRangeException(nameof(guidBytes));
            }

            _low = 0L;
            _high = 0L;
            SetFromBytes(guidBytes);
        }

        public static implicit operator Guid(WGuid guid)
        {
            return guid.ToGuid();
        }

        public static implicit operator WGuid(Guid guid)
        {
            return new WGuid(guid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(WGuid lhs, WGuid rhs)
        {
            return lhs.Equals(rhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(WGuid lhs, WGuid rhs)
        {
            return !lhs.Equals(rhs);
        }

        public static WGuid Parse(string value)
        {
            Guid parsed = ParseGuidString(value);
            return new WGuid(parsed);
        }

        public static bool TryParse(string value, out WGuid guid)
        {
            if (global::System.Guid.TryParse(value, out Guid parsed) && IsVersionFour(parsed))
            {
                guid = new WGuid(parsed);
                return true;
            }

            guid = EmptyGuid;
            return false;
        }

        public static bool TryParse(ReadOnlySpan<char> value, out WGuid guid)
        {
            if (global::System.Guid.TryParse(value, out Guid parsed) && IsVersionFour(parsed))
            {
                guid = new WGuid(parsed);
                return true;
            }

            guid = EmptyGuid;
            return false;
        }

        public bool TryFormat(
            Span<char> destination,
            out int charsWritten,
            ReadOnlySpan<char> format = default
        )
        {
            Guid guid = ToGuid();
            return guid.TryFormat(destination, out charsWritten, format);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(WGuid other)
        {
            return _low == other._low && _high == other._high;
        }

        public bool Equals(Guid other)
        {
            WGuid converted = new(other);
            return Equals(converted);
        }

        public override bool Equals(object obj)
        {
            return obj switch
            {
                WGuid otherWGuid => Equals(otherWGuid),
                Guid otherGuid => Equals(otherGuid),
                _ => false,
            };
        }

        public int CompareTo(WGuid other)
        {
            Guid self = ToGuid();
            Guid otherGuid = other.ToGuid();
            return self.CompareTo(otherGuid);
        }

        public int CompareTo(Guid other)
        {
            Guid self = ToGuid();
            return self.CompareTo(other);
        }

        public int CompareTo(object obj)
        {
            return obj switch
            {
                WGuid otherWGuid => CompareTo(otherWGuid),
                Guid otherGuid => CompareTo(otherGuid),
                _ => -1,
            };
        }

        public override int GetHashCode()
        {
            return Objects.HashCode(_low, _high);
        }

        public override string ToString()
        {
            return ToGuid().ToString();
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            Guid self = ToGuid();
            return self.ToString(format, formatProvider);
        }

        public byte[] ToByteArray()
        {
            byte[] bytes = new byte[16];
            Span<byte> span = bytes.AsSpan();
            WriteBytes(span);
            return bytes;
        }

        public bool TryWriteBytes(Span<byte> destination)
        {
            if (destination.Length < 16)
            {
                return false;
            }

            WriteBytes(destination);
            return true;
        }

        public bool IsEmpty => _low == 0L && _high == 0L;

        public int Version
        {
            get
            {
                ulong low = unchecked((ulong)_low);
                ushort segment = (ushort)((low >> 48) & 0xFFFF);
                return (segment >> 12) & 0x0F;
            }
        }

        public bool IsVersion4 => Version == 4;

        public Guid ToGuid()
        {
            Span<byte> buffer = stackalloc byte[16];
            WriteBytes(buffer);
            return new Guid(buffer);
        }

        private void SetFromGuid(Guid guid)
        {
            Span<byte> buffer = stackalloc byte[16];
            bool success = guid.TryWriteBytes(buffer);
            if (!success)
            {
                throw new InvalidOperationException("Failed to convert Guid to bytes.");
            }

            SetFromBytes(buffer);
        }

        private void SetFromBytes(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != 16)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes));
            }

            ulong low = BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(0, 8));
            ulong high = BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(8, 8));
            _low = unchecked((long)low);
            _high = unchecked((long)high);
        }

        private void WriteBytes(Span<byte> destination)
        {
            ulong low = unchecked((ulong)_low);
            ulong high = unchecked((ulong)_high);
            BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(0, 8), low);
            BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(8, 8), high);
        }

        private static Guid ParseGuidString(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Guid parsed = global::System.Guid.Parse(value);
            if (!IsVersionFour(parsed))
            {
                throw new FormatException($"{nameof(WGuid)} requires a version 4 {nameof(Guid)}.");
            }

            return parsed;
        }

        private static bool IsVersionFour(Guid guid)
        {
            Span<byte> buffer = stackalloc byte[16];
            bool success = guid.TryWriteBytes(buffer);
            if (!success)
            {
                return false;
            }

            ulong low = BinaryPrimitives.ReadUInt64LittleEndian(buffer.Slice(0, 8));
            ushort segment = (ushort)((low >> 48) & 0xFFFF);
            int version = (segment >> 12) & 0x0F;
            return version == 4;
        }
    }
}
