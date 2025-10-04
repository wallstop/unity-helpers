namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Represents an axis-aligned bounding box with half-open semantics on the maximum edge.
    /// </summary>
    public readonly struct BoundingBox3D
    {
        private const float MinimumExclusivePadding = 1e-6f;

        public readonly Vector3 min;
        public readonly Vector3 max;

        public BoundingBox3D(Vector3 min, Vector3 max)
        {
            if (min.x > max.x || min.y > max.y || min.z > max.z)
            {
                throw new ArgumentException("Min must be less than or equal to max on all axes.");
            }

            this.min = min;
            this.max = EnsureExclusiveMax(min, max);
        }

        public Vector3 Center => (min + max) * 0.5f;

        public Vector3 Size => max - min;

        public bool IsEmpty => max.x <= min.x || max.y <= min.y || max.z <= min.z;

        public static BoundingBox3D Empty => default;

        public static BoundingBox3D FromCenterAndSize(Vector3 center, Vector3 size)
        {
            Vector3 half = size * 0.5f;
            Vector3 min = center - half;
            Vector3 max = center + half;
            return new BoundingBox3D(min, max);
        }

        public static BoundingBox3D FromClosedBounds(Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            return new BoundingBox3D(min, MoveMaxExclusive(min, max));
        }

        public static BoundingBox3D FromPoint(Vector3 point)
        {
            return new BoundingBox3D(point, MoveMaxExclusive(point, point));
        }

        public BoundingBox3D ExpandToInclude(Vector3 point)
        {
            Vector3 localMin = min;
            if (point.x < localMin.x)
            {
                localMin.x = point.x;
            }
            if (point.y < localMin.y)
            {
                localMin.y = point.y;
            }
            if (point.z < localMin.z)
            {
                localMin.z = point.z;
            }

            Vector3 localMax = max;
            if (point.x >= localMax.x)
            {
                localMax.x = NextFloat(point.x);
            }
            if (point.y >= localMax.y)
            {
                localMax.y = NextFloat(point.y);
            }
            if (point.z >= localMax.z)
            {
                localMax.z = NextFloat(point.z);
            }

            return new BoundingBox3D(localMin, localMax);
        }

        public BoundingBox3D ExpandToInclude(BoundingBox3D other)
        {
            if (other.IsEmpty)
            {
                return this;
            }

            if (IsEmpty)
            {
                return other;
            }

            Vector3 localMin = new(
                Math.Min(min.x, other.min.x),
                Math.Min(min.y, other.min.y),
                Math.Min(min.z, other.min.z)
            );

            Vector3 localMax = new(
                Math.Max(max.x, other.max.x),
                Math.Max(max.y, other.max.y),
                Math.Max(max.z, other.max.z)
            );

            return new BoundingBox3D(localMin, localMax);
        }

        public BoundingBox3D EnsureMinimumSize(float minimum)
        {
            if (minimum <= 0f || IsEmpty)
            {
                return this;
            }

            Vector3 size = Size;
            Vector3 localMin = min;
            Vector3 localMax = max;

            if (size.x < minimum)
            {
                float delta = (minimum - size.x) * 0.5f;
                localMin.x -= delta;
                localMax.x += delta;
            }

            if (size.y < minimum)
            {
                float delta = (minimum - size.y) * 0.5f;
                localMin.y -= delta;
                localMax.y += delta;
            }

            if (size.z < minimum)
            {
                float delta = (minimum - size.z) * 0.5f;
                localMin.z -= delta;
                localMax.z += delta;
            }

            return new BoundingBox3D(localMin, localMax);
        }

        public bool Contains(Vector3 point)
        {
            return point.x >= min.x
                && point.y >= min.y
                && point.z >= min.z
                && point.x < max.x
                && point.y < max.y
                && point.z < max.z;
        }

        public bool Contains(BoundingBox3D other)
        {
            return min.x <= other.min.x
                && min.y <= other.min.y
                && min.z <= other.min.z
                && max.x >= other.max.x
                && max.y >= other.max.y
                && max.z >= other.max.z;
        }

        public bool Intersects(BoundingBox3D other)
        {
            return min.x < other.max.x
                && max.x > other.min.x
                && min.y < other.max.y
                && max.y > other.min.y
                && min.z < other.max.z
                && max.z > other.min.z;
        }

        public Vector3 ClosestPoint(Vector3 point)
        {
            float x = point.x;
            if (x < min.x)
            {
                x = min.x;
            }
            else if (x > max.x)
            {
                x = max.x;
            }

            float y = point.y;
            if (y < min.y)
            {
                y = min.y;
            }
            else if (y > max.y)
            {
                y = max.y;
            }

            float z = point.z;
            if (z < min.z)
            {
                z = min.z;
            }
            else if (z > max.z)
            {
                z = max.z;
            }

            return new Vector3(x, y, z);
        }

        public float DistanceSquaredTo(Vector3 point)
        {
            Vector3 closest = ClosestPoint(point);
            return (closest - point).sqrMagnitude;
        }

        public Bounds ToBounds()
        {
            Vector3 size = Size;
            Vector3 center = new(
                min.x + (size.x * 0.5f),
                min.y + (size.y * 0.5f),
                min.z + (size.z * 0.5f)
            );
            return new Bounds(center, size);
        }

        private static Vector3 MoveMaxExclusive(Vector3 min, Vector3 inclusiveMax)
        {
            Vector3 exclusive = inclusiveMax;
            if (exclusive.x <= min.x)
            {
                exclusive.x = NextFloat(min.x + MinimumExclusivePadding);
            }
            else
            {
                exclusive.x = NextFloat(exclusive.x);
            }

            if (exclusive.y <= min.y)
            {
                exclusive.y = NextFloat(min.y + MinimumExclusivePadding);
            }
            else
            {
                exclusive.y = NextFloat(exclusive.y);
            }

            if (exclusive.z <= min.z)
            {
                exclusive.z = NextFloat(min.z + MinimumExclusivePadding);
            }
            else
            {
                exclusive.z = NextFloat(exclusive.z);
            }

            return exclusive;
        }

        private static Vector3 EnsureExclusiveMax(Vector3 min, Vector3 max)
        {
            Vector3 exclusive = max;
            if (exclusive.x <= min.x)
            {
                exclusive.x = NextFloat(min.x + MinimumExclusivePadding);
            }
            if (exclusive.y <= min.y)
            {
                exclusive.y = NextFloat(min.y + MinimumExclusivePadding);
            }
            if (exclusive.z <= min.z)
            {
                exclusive.z = NextFloat(min.z + MinimumExclusivePadding);
            }
            return exclusive;
        }

        private static float NextFloat(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return value;
            }

            if (value == float.MaxValue)
            {
                return value;
            }

            if (value == float.MinValue)
            {
                return BitConverter.ToSingle(BitConverter.GetBytes(unchecked((int)0xFF7FFFFF)), 0);
            }

            if (value == 0f)
            {
                return float.Epsilon;
            }

            int bits = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
            if (value > 0f)
            {
                bits++;
            }
            else
            {
                bits--;
            }

            return BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
        }
    }
}
