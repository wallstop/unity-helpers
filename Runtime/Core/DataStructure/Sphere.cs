namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System.Runtime.CompilerServices;
    using UnityEngine;

    public readonly struct Sphere
    {
        public readonly Vector3 center;
        public readonly float radius;
        private readonly float _radiusSquared;

        public Sphere(Vector3 center, float radius)
        {
            this.center = center;
            this.radius = radius;
            _radiusSquared = radius * radius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Vector3 point)
        {
            float dx = center.x - point.x;
            float dy = center.y - point.y;
            float dz = center.z - point.z;
            return dx * dx + dy * dy + dz * dz <= _radiusSquared;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(BoundingBox3D bounds)
        {
            Vector3 closest = bounds.ClosestPoint(center);
            float dx = closest.x - center.x;
            float dy = closest.y - center.y;
            float dz = closest.z - center.z;
            float distanceSquared = dx * dx + dy * dy + dz * dz;
            // Add a tiny tolerance to account for floating-point rounding when touching exactly at an edge/corner
            const float Tolerance = 1e-6f;
            return distanceSquared <= (_radiusSquared + Tolerance);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(BoundingBox3D bounds)
        {
            // Empty bounds are considered to overlap any sphere
            if (bounds.IsEmpty)
            {
                return true;
            }

            // Special case: if the bounds.min is at the sphere center and the bounds is very small
            // (nearly a point due to half-open semantics), it should be considered contained.
            // This handles zero-size Bounds converted to BoundingBox3D where max is nudged to be exclusive.
            float minDx = bounds.min.x - center.x;
            float minDy = bounds.min.y - center.y;
            float minDz = bounds.min.z - center.z;
            float minDistSquared = minDx * minDx + minDy * minDy + minDz * minDz;

            if (minDistSquared <= _radiusSquared)
            {
                // Check if bounds is very small (point-like)
                float sizeX = bounds.max.x - bounds.min.x;
                float sizeY = bounds.max.y - bounds.min.y;
                float sizeZ = bounds.max.z - bounds.min.z;
                float maxSize =
                    sizeX > sizeY
                        ? (sizeX > sizeZ ? sizeX : sizeZ)
                        : (sizeY > sizeZ ? sizeY : sizeZ);

                // If bounds is tiny and min is inside sphere, consider it overlapping
                if (maxSize < 1e-5f)
                {
                    return true;
                }
            }

            // A sphere overlaps (contains) a bounds if the farthest corner of the bounds is within the sphere
            // For an axis-aligned bounding box, the farthest point from sphere center is one of the 8 corners
            float toMinX = bounds.min.x - center.x;
            float toMinY = bounds.min.y - center.y;
            float toMinZ = bounds.min.z - center.z;
            float toMaxX = bounds.max.x - center.x;
            float toMaxY = bounds.max.y - center.y;
            float toMaxZ = bounds.max.z - center.z;

            // Find the corner farthest from the sphere center by choosing the coordinate with max absolute distance
            float absMinX = toMinX < 0 ? -toMinX : toMinX;
            float absMaxX = toMaxX < 0 ? -toMaxX : toMaxX;
            float farthestX = absMinX > absMaxX ? toMinX : toMaxX;

            float absMinY = toMinY < 0 ? -toMinY : toMinY;
            float absMaxY = toMaxY < 0 ? -toMaxY : toMaxY;
            float farthestY = absMinY > absMaxY ? toMinY : toMaxY;

            float absMinZ = toMinZ < 0 ? -toMinZ : toMinZ;
            float absMaxZ = toMaxZ < 0 ? -toMaxZ : toMaxZ;
            float farthestZ = absMinZ > absMaxZ ? toMinZ : toMaxZ;

            float farthestDistanceSquared =
                farthestX * farthestX + farthestY * farthestY + farthestZ * farthestZ;
            return farthestDistanceSquared <= _radiusSquared;
        }
    }
}
