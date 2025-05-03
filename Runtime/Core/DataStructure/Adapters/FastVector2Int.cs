namespace WallstopStudios.UnityHelpers.Core.DataStructure.Adapters
{
    using System;
    using System.Runtime.CompilerServices;
    using Helper;
    using ProtoBuf;
    using UnityEngine;

    [Serializable]
    [ProtoContract]
    public struct FastVector2Int : IEquatable<FastVector2Int>
    {
        [ProtoMember(1)]
        public int x;

        [ProtoMember(2)]
        public int y;

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
        public override bool Equals(object obj)
        {
            return obj is FastVector2Int other && Equals(other);
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

        [ProtoAfterDeserialization]
        private void AfterDeserialize()
        {
            _hash = Objects.ValueTypeHashCode(x, y);
        }

        public readonly FastVector3Int AsFastVector3Int()
        {
            return new FastVector3Int(x, y);
        }
    }
}
