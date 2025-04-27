namespace WallstopStudios.UnityHelpers.Core.DataStructure.Adapters
{
    using System;
    using System.Runtime.CompilerServices;
    using Helper;
    using ProtoBuf;
    using UnityEngine;

    [Serializable]
    [ProtoContract]
    public struct FastVector3Int
        : IEquatable<FastVector3Int>,
            IComparable<FastVector3Int>,
            IComparable
    {
        public static readonly FastVector3Int zero = new(0, 0, 0);

        [ProtoMember(1)]
        public int x;

        [ProtoMember(2)]
        public int y;

        [ProtoMember(3)]
        public int z;

        private int _hash;

        public FastVector3Int(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            _hash = Objects.ValueTypeHashCode(x, y, z);
        }

        public FastVector3Int(Vector3Int vector)
            : this(vector.x, vector.y, vector.z) { }

        public FastVector3Int(int x, int y)
            : this(x, y, 0) { }

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
            if (_hash == 0)
            {
                _hash = Objects.ValueTypeHashCode(x, y, z);
            }
            return _hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return obj is FastVector3Int other && Equals(other);
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
        public int CompareTo(object other)
        {
            if (other is FastVector3Int vector)
            {
                return CompareTo(vector);
            }

            return -1;
        }

        public override string ToString()
        {
            return $"({x}, {y}, {z})";
        }

        [ProtoAfterDeserialization]
        private void AfterDeserialize()
        {
            _hash = Objects.ValueTypeHashCode(x, y, z);
        }

        public readonly Vector2 AsVector2()
        {
            return new Vector2(x, y);
        }

        public readonly Vector3 AsVector3()
        {
            return new Vector3(x, y, z);
        }
    }
}
