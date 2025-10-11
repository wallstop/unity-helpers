namespace WallstopStudios.UnityHelpers.Core.DataStructure.Adapters
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Text.Json.Serialization;
    using Helper;
    using ProtoBuf;
    using UnityEngine;

    [Serializable]
    [ProtoContract]
    public readonly struct FastVector3Int
        : IEquatable<FastVector3Int>,
            IEquatable<FastVector2Int>,
            IEquatable<Vector3Int>,
            IEquatable<Vector2Int>,
            IComparable<FastVector3Int>,
            IComparable<FastVector2Int>,
            IComparable<Vector3Int>,
            IComparable<Vector2Int>,
            IComparable
    {
        public static readonly FastVector3Int zero = new(0, 0, 0);

        [ProtoMember(1)]
        public readonly int x;

        [ProtoMember(2)]
        public readonly int y;

        [ProtoMember(4)]
        public readonly int z;

        // Out of order proto is expected
        [ProtoMember(3)]
        private readonly int _hash;

        [JsonConstructor]
        public FastVector3Int(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            _hash = Objects.HashCode(x, y, z);
        }

        public FastVector3Int(Vector3Int vector)
            : this(vector.x, vector.y, vector.z) { }

        public FastVector3Int(int x, int y)
            : this(x, y, 0) { }

        [JsonPropertyName("x")]
        public int X => x;

        [JsonPropertyName("y")]
        public int Y => y;

        [JsonPropertyName("z")]
        public int Z => z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FastVector3Int lhs, FastVector3Int rhs)
        {
            return lhs.Equals(rhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FastVector3Int lhs, FastVector3Int rhs)
        {
            return !lhs.Equals(rhs);
        }

        public static FastVector3Int operator +(FastVector3Int lhs, FastVector3Int rhs)
        {
            return new FastVector3Int(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);
        }

        public static FastVector3Int operator +(FastVector3Int lhs, Vector3Int rhs)
        {
            return new FastVector3Int(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);
        }

        public static FastVector3Int operator +(FastVector3Int lhs, Vector2Int rhs)
        {
            return new FastVector3Int(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z);
        }

        public static FastVector3Int operator +(FastVector3Int lhs, FastVector2Int rhs)
        {
            return new FastVector3Int(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z);
        }

        public static FastVector3Int operator -(FastVector3Int lhs, FastVector3Int rhs)
        {
            return new FastVector3Int(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z);
        }

        public static FastVector3Int operator -(FastVector3Int lhs, Vector3Int rhs)
        {
            return new FastVector3Int(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z);
        }

        public static FastVector3Int operator -(FastVector3Int lhs, FastVector2Int rhs)
        {
            return new FastVector3Int(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z);
        }

        public static FastVector3Int operator -(FastVector3Int lhs, Vector2Int rhs)
        {
            return new FastVector3Int(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z);
        }

        public static implicit operator Vector3Int(FastVector3Int vector)
        {
            return new Vector3Int(vector.x, vector.y, vector.z);
        }

        public static implicit operator FastVector3Int(Vector3Int vector)
        {
            return new FastVector3Int(vector.x, vector.y, vector.z);
        }

        public static implicit operator Vector2Int(FastVector3Int vector)
        {
            return new Vector2Int(vector.x, vector.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return _hash;
        }

        public int CompareTo(Vector3Int other)
        {
            int comparison = x.CompareTo(other.x);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = y.CompareTo(other.y);
            if (comparison != 0)
            {
                return comparison;
            }

            return z.CompareTo(other.z);
        }

        public bool Equals(Vector2Int other)
        {
            return x == other.x && y == other.y;
        }

        public int CompareTo(Vector2Int other)
        {
            int comparison = x.CompareTo(other.x);
            if (comparison != 0)
            {
                return comparison;
            }

            return y.CompareTo(other.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return obj switch
            {
                FastVector3Int vector => Equals(vector),
                Vector3Int vector => Equals(vector),
                FastVector2Int vector => Equals(vector),
                Vector2Int vector => Equals(vector),
                _ => false,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector3Int other)
        {
            return x == other.x && y == other.y && z == other.z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FastVector3Int other)
        {
            return GetHashCode() == other.GetHashCode()
                && x == other.x
                && y == other.y
                && z == other.z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FastVector2Int other)
        {
            return x == other.x && y == other.y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(FastVector3Int other)
        {
            int comparison = x.CompareTo(other.x);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = y.CompareTo(other.y);
            if (comparison != 0)
            {
                return comparison;
            }

            return z.CompareTo(other.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(FastVector2Int other)
        {
            int comparison = x.CompareTo(other.x);
            if (comparison != 0)
            {
                return comparison;
            }

            return y.CompareTo(other.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(object other)
        {
            return other switch
            {
                FastVector3Int vector => CompareTo(vector),
                Vector3Int vector => CompareTo(vector),
                FastVector2Int vector => CompareTo(vector),
                Vector2Int vector => CompareTo(vector),
                _ => -1,
            };
        }

        public override string ToString()
        {
            return $"({x}, {y}, {z})";
        }

        public FastVector2Int FastVector2Int()
        {
            return new FastVector2Int(x, y);
        }

        public Vector2 AsVector2()
        {
            return new Vector2(x, y);
        }

        public Vector3 AsVector3()
        {
            return new Vector3(x, y, z);
        }
    }
}
