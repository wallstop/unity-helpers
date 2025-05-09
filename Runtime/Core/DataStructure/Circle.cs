namespace WallstopStudios.UnityHelpers.Core.DataStructure
{
    using System;
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
            float xN = Math.Max(center.x, rectangle.x);
            float yN = Math.Max(center.y, rectangle.y);
            float dX = xN - center.x;
            float dY = yN - center.y;
            return dX * dX + dY * dY <= _radiusSquared;
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
