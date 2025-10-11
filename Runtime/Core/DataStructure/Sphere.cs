namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Text.Json.Serialization;
    using Helper;
    using Math;
    using UnityEngine;

    /// <summary>
    /// Represents a sphere in 3D space defined by a center point and radius.
    /// </summary>
    public readonly struct Sphere : IEquatable<Sphere>
    {
        public readonly Vector3 center;
        public readonly float radius;
        private readonly float _radiusSquared;

        /// <summary>
        /// Initializes a new sphere with the specified center and radius.
        /// </summary>
        /// <param name="center">The center point of the sphere.</param>
        /// <param name="radius">The radius of the sphere.</param>
        [JsonConstructor]
        public Sphere(Vector3 center, float radius)
        {
            this.center = center;
            this.radius = radius;
            _radiusSquared = radius * radius;
        }

        /// <summary>
        /// Determines whether the sphere contains the specified point.
        /// Points on the surface are considered contained.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns>True if the point is inside or on the sphere's surface.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Vector3 point)
        {
            float dx = center.x - point.x;
            float dy = center.y - point.y;
            float dz = center.z - point.z;
            return dx * dx + dy * dy + dz * dz <= _radiusSquared;
        }

        /// <summary>
        /// Determines whether this sphere intersects with the specified Unity Bounds.
        /// Returns true if there is any overlap between the sphere and bounds.
        /// </summary>
        /// <param name="bounds">The Unity Bounds to test for intersection.</param>
        /// <returns>True if the sphere and bounds intersect.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(Bounds bounds)
        {
            return Intersects(BoundingBox3D.FromClosedBounds(bounds));
        }

        /// <summary>
        /// Determines whether this sphere intersects with the specified bounding box.
        /// Returns true if there is any overlap between the sphere and bounds.
        /// </summary>
        /// <param name="bounds">The bounding box to test for intersection.</param>
        /// <returns>True if the sphere and bounds intersect.</returns>
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

        /// <summary>
        /// Determines whether this sphere intersects with another sphere.
        /// Returns true if there is any overlap between the two spheres.
        /// </summary>
        /// <param name="other">The other sphere to test for intersection.</param>
        /// <returns>True if the spheres intersect.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(Sphere other)
        {
            float combinedRadius = radius + other.radius;
            float combinedRadiusSquared = combinedRadius * combinedRadius;
            float dx = center.x - other.center.x;
            float dy = center.y - other.center.y;
            float dz = center.z - other.center.z;
            return dx * dx + dy * dy + dz * dz <= combinedRadiusSquared;
        }

        /// <summary>
        /// Determines whether this sphere intersects with a line segment.
        /// Returns true if the line segment intersects or touches the sphere.
        /// </summary>
        /// <param name="line">The line segment to test for intersection.</param>
        /// <returns>True if the line segment intersects the sphere.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(Line3D line)
        {
            return line.Intersects(this);
        }

        /// <summary>
        /// Calculates the shortest distance from this sphere to a line segment.
        /// Returns 0 if the line intersects the sphere.
        /// </summary>
        /// <param name="line">The line segment to measure distance from.</param>
        /// <returns>The shortest distance from the sphere's surface to the line segment.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float DistanceToLine(Line3D line)
        {
            return line.DistanceToSphere(this);
        }

        /// <summary>
        /// Finds the closest point on a line segment to this sphere's center.
        /// </summary>
        /// <param name="line">The line segment.</param>
        /// <returns>The closest point on the line segment to the sphere's center.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 ClosestPointOnLine(Line3D line)
        {
            return line.ClosestPointOnLine(center);
        }

        /// <summary>
        /// Determines whether the specified Unity Bounds is completely contained within this sphere.
        /// All corners of the bounds must be inside the sphere.
        /// </summary>
        /// <param name="bounds">The Unity Bounds to test for containment.</param>
        /// <returns>True if the bounds is completely contained within the sphere.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(Bounds bounds)
        {
            return Overlaps(BoundingBox3D.FromClosedBounds(bounds));
        }

        /// <summary>
        /// Determines whether the specified bounding box is completely contained within this sphere.
        /// All corners of the bounding box must be inside the sphere.
        /// </summary>
        /// <param name="bounds">The bounding box to test for containment.</param>
        /// <returns>True if the bounding box is completely contained within the sphere.</returns>
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

        /// <summary>
        /// Determines whether this sphere equals another sphere.
        /// </summary>
        /// <param name="other">The other sphere to compare.</param>
        /// <returns>True if the spheres have the same center and radius.</returns>
        public bool Equals(Sphere other)
        {
            return center.Equals(other.center) && Mathf.Approximately(radius, other.radius);
        }

        /// <summary>
        /// Determines whether this sphere equals another object.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if the object is a Sphere with the same center and radius.</returns>
        public override bool Equals(object obj)
        {
            return obj is Sphere other && Equals(other);
        }

        /// <summary>
        /// Gets the hash code for this sphere.
        /// </summary>
        /// <returns>A hash code for the current sphere.</returns>
        public override int GetHashCode()
        {
            return Objects.HashCode(center, radius);
        }

        /// <summary>
        /// Determines whether two spheres are equal.
        /// </summary>
        public static bool operator ==(Sphere left, Sphere right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two spheres are not equal.
        /// </summary>
        public static bool operator !=(Sphere left, Sphere right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Returns a string representation of this sphere.
        /// </summary>
        /// <returns>A string describing the sphere's center and radius.</returns>
        public override string ToString()
        {
            return $"Sphere(center: {center}, radius: {radius})";
        }
    }
}
