namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    public static class Geometry
    {
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
        public static float IsAPointLeftOfVectorOrOnTheLine(Vector2 a, Vector2 b, Vector2 p)
        {
            return (a.x - p.x) * (b.y - p.y) - (a.y - p.y) * (b.x - p.x);
        }

        public static float IsAPointLeftOfVectorOrOnTheLine(Vector3 a, Vector3 b, Vector3 p)
        {
            return (a.x - p.x) * (b.y - p.y) - (a.y - p.y) * (b.x - p.x);
        }

        public static int IsAPointLeftOfVectorOrOnTheLine(Vector2Int a, Vector2Int b, Vector2Int p)
        {
            return (a.x - p.x) * (b.y - p.y) - (a.y - p.y) * (b.x - p.x);
        }
    }
}
