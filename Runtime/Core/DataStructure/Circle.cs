namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using Extension;
    using UnityEngine;

    public readonly struct Circle
    {
        public readonly Vector2 center;
        public readonly float radius;
        private readonly float _radiusSquared;

        public Circle(Vector2 center, float radius)
        {
            this.center = center;
            this.radius = radius;
            _radiusSquared = radius * radius;
        }

        public bool Contains(Vector2 point)
        {
            return (center - point).sqrMagnitude <= _radiusSquared;
        }

        public bool Intersects(Bounds bounds)
        {
            return Intersects(bounds.Rect());
        }

        // https://www.geeksforgeeks.org/check-if-any-point-overlaps-the-given-circle-and-rectangle/
        public bool Intersects(Rect rectangle)
        {
            // Compute the closest point on the rectangle to the circle center by clamping
            float xN = Mathf.Clamp(center.x, rectangle.xMin, rectangle.xMax);
            float yN = Mathf.Clamp(center.y, rectangle.yMin, rectangle.yMax);
            float dX = xN - center.x;
            float dY = yN - center.y;
            // Add a tiny tolerance to account for floating-point rounding when touching exactly at an edge/corner
            const float Tolerance = 1e-6f;
            return (dX * dX + dY * dY) <= (_radiusSquared + Tolerance);
        }

        public bool Overlaps(Bounds bounds)
        {
            return Overlaps(bounds.Rect());
        }

        public bool Overlaps(Rect rectangle)
        {
            return Contains(rectangle.min) && Contains(rectangle.max);
        }
    }
}
