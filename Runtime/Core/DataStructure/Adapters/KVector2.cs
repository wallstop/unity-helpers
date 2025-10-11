namespace WallstopStudios.UnityHelpers.Core.DataStructure.Adapters
{
    using System;
    using System.Runtime.Serialization;
    using Helper;
    using ProtoBuf;
    using UnityEngine;

    [Serializable]
    [DataContract]
    [ProtoContract]
    public struct KVector2
        : IEquatable<KVector2>,
            IEquatable<Vector2>,
            IEquatable<Vector3>,
            IComparable<KVector2>,
            IComparable<Vector2>,
            IComparable<Vector3>,
            IComparable
    {
        [DataMember]
        [ProtoMember(1)]
        public float x;

        [DataMember]
        [ProtoMember(2)]
        public float y;

        public float Magnitude => (float)Math.Sqrt(x * (double)x + y * (double)y);

        public KVector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public KVector2(Vector2 vector)
        {
            x = vector.x;
            y = vector.y;
        }

        public static implicit operator Vector2(KVector2 vector)
        {
            return new Vector2(vector.x, vector.y);
        }

        public static implicit operator KVector2(Vector2 vector)
        {
            return new KVector2(vector);
        }

        public bool Equals(Vector2 other)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (x != other.x)
            {
                return false;
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return y == other.y;
        }

        public bool Equals(KVector2 other)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (x != other.x)
            {
                return false;
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return y == other.y;
        }

        public int CompareTo(KVector2 other)
        {
            int comparison = x.CompareTo(other.x);
            if (comparison != 0)
            {
                return comparison;
            }
            return y.CompareTo(other.y);
        }

        public int CompareTo(Vector2 other)
        {
            int comparison = x.CompareTo(other.x);
            if (comparison != 0)
            {
                return comparison;
            }
            return y.CompareTo(other.y);
        }

        public bool Equals(Vector3 other)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (x != other.x)
            {
                return false;
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return y == other.y;
        }

        public int CompareTo(Vector3 other)
        {
            int comparison = x.CompareTo(other.x);
            if (comparison != 0)
            {
                return comparison;
            }
            return y.CompareTo(other.y);
        }

        public override bool Equals(object obj)
        {
            return obj switch
            {
                KVector2 vector => Equals(vector),
                Vector2 vector => Equals(vector),
                Vector3 vector3 => Equals(vector3),
                _ => false,
            };
        }

        public override int GetHashCode()
        {
            return Objects.HashCode(x, y);
        }

        public int CompareTo(object obj)
        {
            return obj switch
            {
                KVector2 vector => CompareTo(vector),
                Vector2 vector => CompareTo(vector),
                Vector3 vector3 => CompareTo(vector3),
                _ => -1,
            };
        }
    }
}
