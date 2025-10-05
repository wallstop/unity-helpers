namespace WallstopStudios.UnityHelpers.Core.DataStructure.Adapters
{
    using System;
    using System.Runtime.CompilerServices;
    using Helper;
    using ProtoBuf;
    using UnityEngine;

    [Serializable]
    [ProtoContract]
    public struct FastVector2Int
        : IEquatable<FastVector2Int>,
            IEquatable<FastVector3Int>,
            IEquatable<Vector2Int>,
            IEquatable<Vector3Int>,
            IComparable<FastVector2Int>,
            IComparable<FastVector3Int>,
            IComparable<Vector2Int>,
            IComparable<Vector3Int>,
            IComparable
    {
        [ProtoMember(1)]
        public readonly int x;

        [ProtoMember(2)]
        public readonly int y;

        private int _hash;

        public FastVector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
            _hash = Objects.ValueTypeHashCode(x, y);
        }

        public static implicit operator Vector2Int(FastVector2Int vector)
        {
            return new Vector2Int(vector.x, vector.y);
        }

        public static implicit operator FastVector2Int(FastVector3Int vector)
        {
            return new FastVector2Int(vector.x, vector.y);
        }

        public static implicit operator FastVector2Int(Vector2Int vector)
        {
            return new FastVector2Int(vector.x, vector.y);
        }

        public static FastVector2Int operator +(FastVector2Int lhs, FastVector2Int rhs)
        {
            return new FastVector2Int(lhs.x + rhs.x, lhs.y + rhs.y);
        }

        public static FastVector2Int operator -(FastVector2Int lhs, FastVector2Int rhs)
        {
            return new FastVector2Int(lhs.x - rhs.x, lhs.y - rhs.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FastVector2Int other)
        {
            return GetHashCode() == other.GetHashCode() && x == other.x && y == other.y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector2Int other)
        {
            return x == other.x && y == other.y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(FastVector2Int other)
        {
            int comparison = x.CompareTo(other.x);
            return comparison != 0 ? comparison : y.CompareTo(other.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(Vector2Int other)
        {
            int comparison = x.CompareTo(other.x);
            return comparison != 0 ? comparison : y.CompareTo(other.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FastVector3Int other)
        {
            return x == other.x && y == other.y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(FastVector3Int other)
        {
            int comparison = x.CompareTo(other.x);
            return comparison != 0 ? comparison : y.CompareTo(other.y);
        }

        public bool Equals(Vector3Int other)
        {
            return x == other.x && y == other.y;
        }

        public int CompareTo(Vector3Int other)
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
                FastVector2Int vector => Equals(vector),
                Vector2Int vector => Equals(vector),
                FastVector3Int vector => Equals(vector),
                Vector3Int vector => Equals(vector),
                _ => false,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            if (_hash == 0)
            {
                _hash = Objects.ValueTypeHashCode(x, y);
            }
            return _hash;
        }

        public override string ToString()
        {
            return $"({x}, {y})";
        }

        public int CompareTo(object obj)
        {
            return obj switch
            {
                FastVector2Int vector => CompareTo(vector),
                Vector2Int vector => CompareTo(vector),
                FastVector3Int vector => CompareTo(vector),
                Vector3Int vector => CompareTo(vector),
                _ => -1,
            };
        }

        [ProtoAfterDeserialization]
        private void AfterDeserialize()
        {
            _hash = Objects.ValueTypeHashCode(x, y);
        }

        public readonly FastVector3Int AsFastVector3Int()
        {
            return new FastVector3Int(x, y);
        }

        public readonly Vector2 AsVector2()
        {
            return new Vector2(x, y);
        }

        public readonly Vector3 AsVector3()
        {
            return new Vector3(x, y);
        }
    }
}
