namespace WallstopStudios.UnityHelpers.Core.DataStructure.Adapters
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;
    using Helper;
    using ProtoBuf;

    [Serializable]
    [DataContract]
    [ProtoContract]
    public readonly struct KGuid
        : IEquatable<KGuid>,
            IEquatable<Guid>,
            IComparable<KGuid>,
            IComparable<Guid>,
            IComparable
    {
        /*
            We need to store this in a NetworkList somewhere, and that means we can't have arrays.
            Since we know the underlying data, do this shit (this is what a Guid looks like anyway)...
         */
        [ProtoMember(1)]
        private readonly int _a;

        [ProtoMember(2)]
        private readonly short _b;

        [ProtoMember(3)]
        private readonly short _c;

        [ProtoMember(4)]
        private readonly byte _d;

        [ProtoMember(5)]
        private readonly byte _e;

        [ProtoMember(6)]
        private readonly byte _f;

        [ProtoMember(7)]
        private readonly byte _g;

        [ProtoMember(8)]
        private readonly byte _h;

        [ProtoMember(9)]
        private readonly byte _i;

        [ProtoMember(10)]
        private readonly byte _j;

        [ProtoMember(11)]
        private readonly byte _k;

        [ProtoMember(12)]
        private readonly int _hashCode;

        [JsonInclude]
        [DataMember]
        private string Guid => ((Guid)this).ToString();

        public static KGuid NewGuid()
        {
            return new KGuid(System.Guid.NewGuid());
        }

        public KGuid(Guid guid)
        {
            ReadOnlySpan<byte> guidBytes = MemoryMarshal.AsBytes(
                MemoryMarshal.CreateReadOnlySpan(ref guid, 1)
            );
            _a = guidBytes[3] << 24 | guidBytes[2] << 16 | guidBytes[1] << 8 | guidBytes[0];
            _b = (short)(guidBytes[5] << 8 | guidBytes[4]);
            _c = (short)(guidBytes[7] << 8 | guidBytes[6]);
            _d = guidBytes[8];
            _e = guidBytes[9];
            _f = guidBytes[10];
            _g = guidBytes[11];
            _h = guidBytes[12];
            _i = guidBytes[13];
            _j = guidBytes[14];
            _k = guidBytes[15];
            _hashCode = Objects.HashCode(_a, _b, _c, _d, _e, _f, _g, _h, _i, _j, _k);
        }

        [JsonConstructor]
        public KGuid(string guid)
            : this(System.Guid.Parse(guid)) { }

        public KGuid(byte[] guidBytes)
        {
            _a = guidBytes[3] << 24 | guidBytes[2] << 16 | guidBytes[1] << 8 | guidBytes[0];
            _b = (short)(guidBytes[5] << 8 | guidBytes[4]);
            _c = (short)(guidBytes[7] << 8 | guidBytes[6]);
            _d = guidBytes[8];
            _e = guidBytes[9];
            _f = guidBytes[10];
            _g = guidBytes[11];
            _h = guidBytes[12];
            _i = guidBytes[13];
            _j = guidBytes[14];
            _k = guidBytes[15];
            _hashCode = Objects.HashCode(_a, _b, _c, _d, _e, _f, _g, _h, _i, _j, _k);
        }

        public static implicit operator Guid(KGuid guid)
        {
            return new Guid(
                guid._a,
                guid._b,
                guid._c,
                guid._d,
                guid._e,
                guid._f,
                guid._g,
                guid._h,
                guid._i,
                guid._j,
                guid._k
            );
        }

        public static implicit operator KGuid(Guid guid)
        {
            return new KGuid(guid);
        }

        public static bool operator ==(KGuid lhs, KGuid rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(KGuid lhs, KGuid rhs)
        {
            return !lhs.Equals(rhs);
        }

        public bool Equals(KGuid other)
        {
            if (GetHashCode() != other.GetHashCode())
            {
                return false;
            }

            if (_a != other._a)
            {
                return false;
            }

            if (_b != other._b)
            {
                return false;
            }

            if (_c != other._c)
            {
                return false;
            }

            if (_d != other._d)
            {
                return false;
            }

            if (_e != other._e)
            {
                return false;
            }

            if (_f != other._f)
            {
                return false;
            }

            if (_g != other._g)
            {
                return false;
            }

            if (_h != other._h)
            {
                return false;
            }

            if (_i != other._i)
            {
                return false;
            }

            if (_j != other._j)
            {
                return false;
            }

            return _k == other._k;
        }

        public bool Equals(Guid other)
        {
            return other.Equals(new Guid(_a, _b, _c, _d, _e, _f, _g, _h, _i, _j, _k));
        }

        public int CompareTo(KGuid other)
        {
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

            comparison = _f.CompareTo(other._f);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = _g.CompareTo(other._g);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = _h.CompareTo(other._h);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = _i.CompareTo(other._i);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = _j.CompareTo(other._j);
            if (comparison != 0)
            {
                return comparison;
            }

            return _k.CompareTo(other._k);
        }

        public int CompareTo(Guid other)
        {
            return CompareTo(new KGuid(other));
        }

        public override bool Equals(object other)
        {
            return other switch
            {
                KGuid otherKGuid => Equals(otherKGuid),
                Guid otherGuid => Equals(otherGuid),
                _ => false,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override string ToString()
        {
            return Guid;
        }

        public int CompareTo(object obj)
        {
            return obj switch
            {
                KGuid otherKGuid => CompareTo(otherKGuid),
                Guid otherGuid => CompareTo(otherGuid),
                _ => -1,
            };
        }

        public byte[] ToByteArray()
        {
            return new[]
            {
                (byte)_a,
                (byte)(_a >> 8),
                (byte)(_a >> 16),
                (byte)(_a >> 24),
                (byte)_b,
                (byte)((uint)_b >> 8),
                (byte)_c,
                (byte)((uint)_c >> 8),
                _d,
                _e,
                _f,
                _g,
                _h,
                _i,
                _j,
                _k,
            };
        }

        public bool TryWriteBytes(Span<byte> destination)
        {
            if (destination.Length < 16)
            {
                return false;
            }

            destination[0] = (byte)_a;
            destination[1] = (byte)(_a >> 8);
            destination[2] = (byte)(_a >> 16);
            destination[3] = (byte)(_a >> 24);

            destination[4] = (byte)_b;
            destination[5] = (byte)((uint)_b >> 8);

            destination[6] = (byte)_c;
            destination[7] = (byte)((uint)_c >> 8);

            destination[8] = _d;
            destination[9] = _e;
            destination[10] = _f;
            destination[11] = _g;
            destination[12] = _h;
            destination[13] = _i;
            destination[14] = _j;
            destination[15] = _k;

            return true;
        }
    }
}
