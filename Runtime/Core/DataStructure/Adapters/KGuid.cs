namespace UnityHelpers.Core.DataStructure.Adapters
{
    using System;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;
    using Helper;
    using ProtoBuf;

    [Serializable]
    [DataContract]
    [ProtoContract]
    public struct KGuid : IEquatable<KGuid>, IEquatable<Guid>, IComparable<KGuid>
    {
        /*
            We need to store this in a NetworkList somewhere, and that means we can't have arrays.
            Since we know the underlying data, do this shit (this is what a Guid looks like anyway)...
         */
        [ProtoMember(1)]
        private int _a;

        [ProtoMember(2)]
        private short _b;

        [ProtoMember(3)]
        private short _c;

        [ProtoMember(4)]
        private byte _d;

        [ProtoMember(5)]
        private byte _e;

        [ProtoMember(6)]
        private byte _f;

        [ProtoMember(7)]
        private byte _g;

        [ProtoMember(8)]
        private byte _h;

        [ProtoMember(9)]
        private byte _i;

        [ProtoMember(10)]
        private byte _j;

        [ProtoMember(11)]
        private byte _k;

        private int _hashCode;

        [JsonInclude]
        [DataMember]
        private string Guid => ((Guid)this).ToString();

        public static KGuid NewGuid()
        {
            return new KGuid(System.Guid.NewGuid());
        }

        public KGuid(Guid guid)
            : this(guid.ToByteArray()) { }

        [JsonConstructor]
        public KGuid(string guid)
            : this(System.Guid.Parse(guid)) { }

        public KGuid(byte[] guidBytes)
        {
            _a =
                (int)guidBytes[3] << 24
                | (int)guidBytes[2] << 16
                | (int)guidBytes[1] << 8
                | (int)guidBytes[0];
            _b = (short)((int)guidBytes[5] << 8 | (int)guidBytes[4]);
            _c = (short)((int)guidBytes[7] << 8 | (int)guidBytes[6]);
            _d = guidBytes[8];
            _e = guidBytes[9];
            _f = guidBytes[10];
            _g = guidBytes[11];
            _h = guidBytes[12];
            _i = guidBytes[13];
            _j = guidBytes[14];
            _k = guidBytes[15];
            _hashCode = 0;
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

        public override bool Equals(object other)
        {
            return other switch
            {
                KGuid otherKGuid => Equals(otherKGuid),
                Guid otherGuid => Equals(otherGuid),
                _ => false,
            };
        }

        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                _hashCode = Objects.ValueTypeHashCode(_a, _b, _c, _d, _e, _f, _g, _h, _i, _j, _k);
            }

            return _hashCode;
        }

        public override string ToString()
        {
            return Guid;
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

        [ProtoAfterDeserialization]
        private void AfterDeserialize()
        {
            _hashCode = 0;
        }
    }
}
