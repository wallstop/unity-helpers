namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    /// Lightweight geometric helpers for Rect accumulation and sidedness tests.
    /// </summary>
    public static class Geometry
    {
        /// <summary>
        /// Computes the axis-aligned bounding <see cref="Rect"/> that contains all input rects.
        /// </summary>
        /// <param name="rects">A non-empty sequence of rectangles to accumulate.</param>
        /// <returns>The minimal axis-aligned <see cref="Rect"/> that contains all input rectangles.</returns>
        /// <remarks>
        /// Expects a non-empty sequence. Passing an empty sequence throws <see cref="System.InvalidOperationException"/>
        /// from LINQ <c>Aggregate</c>.
        /// </remarks>
        public static Rect Accumulate(this IEnumerable<Rect> rects)
        {
            return rects.Aggregate(
                (accumulated, next) =>
                    new Rect(
                        Mathf.Min(accumulated.xMin, next.xMin),
                        Mathf.Min(accumulated.yMin, next.yMin),
                        Mathf.Max(accumulated.xMax, next.xMax)
                            - Mathf.Min(accumulated.xMin, next.xMin),
                        Mathf.Max(accumulated.yMax, next.yMax)
                            - Mathf.Min(accumulated.yMin, next.yMin)
                    )
            );
        }

        //Where is p in relation to a-b
        // < 0 -> to the right
        // = 0 -> on the line
        // > 0 -> to the left
        /// <summary>
        /// Returns signed area indicating where point p lies relative to vector a→b in 2D.
        /// &lt; 0 → right, 0 → on line, &gt; 0 → left.
        /// </summary>
        public static float IsAPointLeftOfVectorOrOnTheLine(Vector2 a, Vector2 b, Vector2 p)
        {
            return (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
        }

        /// <summary>
        /// Returns signed area indicating where point p lies relative to vector a→b in 2D (using Vector3 x/y).
        /// </summary>
        public static float IsAPointLeftOfVectorOrOnTheLine(Vector3 a, Vector3 b, Vector3 p)
        {
            return (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
        }

        /// <summary>
        /// Returns signed area indicating where point p lies relative to vector a→b in 2D (int version).
        /// </summary>
        public static int IsAPointLeftOfVectorOrOnTheLine(Vector2Int a, Vector2Int b, Vector2Int p)
        {
            return (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
        }
    }
}
