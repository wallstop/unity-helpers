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

        public bool Intersects(Bounds bounds)
        {
            // Find the closest point on the bounds to the sphere center by clamping
            Vector3 closest = new(
                Mathf.Clamp(center.x, bounds.min.x, bounds.max.x),
                Mathf.Clamp(center.y, bounds.min.y, bounds.max.y),
                Mathf.Clamp(center.z, bounds.min.z, bounds.max.z)
            );
            Vector3 delta = closest - center;
            // Add a tiny tolerance to account for floating-point rounding when touching exactly at an edge/corner
            const float Tolerance = 1e-6f;
            return delta.sqrMagnitude <= (_radiusSquared + Tolerance);
        }

        public bool Overlaps(Bounds bounds)
        {
            return Contains(bounds.min) && Contains(bounds.max);
        }
    }
}
