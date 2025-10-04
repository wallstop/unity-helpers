namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
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

        public bool Contains(Vector3 point)
        {
            return (center - point).sqrMagnitude <= _radiusSquared;
        }

        public bool Intersects(BoundingBox3D bounds)
        {
            Vector3 closest = bounds.ClosestPoint(center);
            Vector3 delta = closest - center;
            // Add a tiny tolerance to account for floating-point rounding when touching exactly at an edge/corner
            const float Tolerance = 1e-6f;
            return delta.sqrMagnitude <= (_radiusSquared + Tolerance);
        }

        public bool Overlaps(BoundingBox3D bounds)
        {
            return Contains(bounds.min) && Contains(bounds.max);
        }
    }
}
