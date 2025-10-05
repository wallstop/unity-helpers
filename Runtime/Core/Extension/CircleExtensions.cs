namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System.Collections.Generic;
    using DataStructure;
    using DataStructure.Adapters;
    using UnityEngine;

    public static class CircleExtensions
    {
        /// <summary>
        /// Enumerates all integer grid points within the circle's area.
        /// </summary>
        /// <param name="circle">The circle to enumerate.</param>
        /// <param name="z">The z-coordinate to use for all enumerated points.</param>
        /// <returns>An enumerable of FastVector3Int points within the circle.</returns>
        public static IEnumerable<FastVector3Int> EnumerateArea(this Circle circle, int z = 0)
        {
            // Calculate integer bounds for the circle
            int radiusCeil = Mathf.CeilToInt(circle.radius);
            int minX = Mathf.FloorToInt(circle.center.x - circle.radius);
            int maxX = Mathf.CeilToInt(circle.center.x + circle.radius);
            int minY = Mathf.FloorToInt(circle.center.y - circle.radius);
            int maxY = Mathf.CeilToInt(circle.center.y + circle.radius);

            // Pre-cache radiusSquared for Contains check
            float radiusSquared = circle.radius * circle.radius;
            Vector2 center = circle.center;

            for (int x = minX; x <= maxX; ++x)
            {
                for (int y = minY; y <= maxY; ++y)
                {
                    // Calculate squared distance without allocating Vector2
                    float dx = x - center.x;
                    float dy = y - center.y;
                    float distanceSquared = dx * dx + dy * dy;

                    if (distanceSquared <= radiusSquared)
                    {
                        yield return new FastVector3Int(x, y, z);
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates all integer grid points within the circle's area into a buffer.
        /// </summary>
        /// <param name="circle">The circle to enumerate.</param>
        /// <param name="buffer">The list to populate with points. Will be cleared before use.</param>
        /// <param name="z">The z-coordinate to use for all enumerated points.</param>
        /// <returns>The buffer list containing all points within the circle.</returns>
        public static List<FastVector3Int> EnumerateArea(
            this Circle circle,
            List<FastVector3Int> buffer,
            int z = 0
        )
        {
            buffer.Clear();

            // Calculate integer bounds for the circle
            int radiusCeil = Mathf.CeilToInt(circle.radius);
            int minX = Mathf.FloorToInt(circle.center.x - circle.radius);
            int maxX = Mathf.CeilToInt(circle.center.x + circle.radius);
            int minY = Mathf.FloorToInt(circle.center.y - circle.radius);
            int maxY = Mathf.CeilToInt(circle.center.y + circle.radius);

            // Pre-cache radiusSquared for Contains check
            float radiusSquared = circle.radius * circle.radius;
            Vector2 center = circle.center;

            for (int x = minX; x <= maxX; ++x)
            {
                for (int y = minY; y <= maxY; ++y)
                {
                    // Calculate squared distance without allocating Vector2
                    float dx = x - center.x;
                    float dy = y - center.y;
                    float distanceSquared = dx * dx + dy * dy;

                    if (distanceSquared <= radiusSquared)
                    {
                        buffer.Add(new FastVector3Int(x, y, z));
                    }
                }
            }

            return buffer;
        }
    }
}
