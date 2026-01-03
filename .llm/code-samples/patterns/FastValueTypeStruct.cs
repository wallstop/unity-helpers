// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

// Value type struct with cached hash - zero allocation pattern
// Use for small data containers that will be used as dictionary keys

namespace WallstopStudios.UnityHelpers.Examples
{
    using System;
    using System.Runtime.CompilerServices;
    using WallstopStudios.UnityHelpers.Core.Helper;

    public readonly struct FastVector2Int : IEquatable<FastVector2Int>
    {
        public readonly int x;
        public readonly int y;
        private readonly int _hash;

        public FastVector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
            _hash = Objects.HashCode(x, y); // Compute once at construction
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => _hash;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FastVector2Int other)
        {
            return _hash == other._hash && x == other.x && y == other.y;
        }

        public override bool Equals(object obj) => obj is FastVector2Int other && Equals(other);

        public static bool operator ==(FastVector2Int lhs, FastVector2Int rhs) => lhs.Equals(rhs);

        public static bool operator !=(FastVector2Int lhs, FastVector2Int rhs) => !lhs.Equals(rhs);
    }
}
