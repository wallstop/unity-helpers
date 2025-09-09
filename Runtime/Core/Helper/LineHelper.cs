namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    public static class LineHelper
    {
        private static float PerpendicularDistance(
            Vector2 point,
            Vector2 lineStart,
            Vector2 lineEnd
        )
        {
            float xDistance = lineEnd.x - lineStart.x;
            float yDistance = lineEnd.y - lineStart.y;

            if (Mathf.Approximately(xDistance, 0) && Mathf.Approximately(yDistance, 0))
            {
                return Vector2.Distance(point, lineStart);
            }

            float t =
                ((point.x - lineStart.x) * xDistance + (point.y - lineStart.y) * yDistance)
                / (xDistance * xDistance + yDistance * yDistance);

            Vector2 closestPoint = t switch
            {
                < 0 => lineStart,
                > 1 => lineEnd,
                _ => new Vector2(lineStart.x + t * xDistance, lineStart.y + t * yDistance),
            };

            return Vector2.Distance(point, closestPoint);
        }

        // c# implementation of the Ramer-Douglas-Peucker-Algorithm by Craig Selbert slightly adapted for Unity Vector Types
        //http://www.codeproject.com/Articles/18936/A-Csharp-Implementation-of-Douglas-Peucker-Line-Ap
        public static List<Vector2> SimplifyPrecise(List<Vector2> points, double tolerance)
        {
            if (points == null || points.Count < 3)
            {
                return points;
            }

            int firstPoint = 0;
            int lastPoint = points.Count - 1;

            //Add the first and last index to the keepers
            List<int> pointIndexsToKeep = new() { firstPoint, lastPoint };

            //The first and the last point cannot be the same
            while (points[firstPoint] == points[lastPoint])
            {
                lastPoint--;
            }

            DouglasPeuckerReductionRecursive(
                points,
                firstPoint,
                lastPoint,
                tolerance,
                ref pointIndexsToKeep
            );

            List<Vector2> returnPoints = new();
            pointIndexsToKeep.Sort();
            foreach (int index in pointIndexsToKeep)
            {
                returnPoints.Add(points[index]);
            }

            return returnPoints;
        }

        private static void DouglasPeuckerReductionRecursive(
            List<Vector2> points,
            int firstPoint,
            int lastPoint,
            double tolerance,
            ref List<int> pointIndexesToKeep
        )
        {
            while (true)
            {
                double maxDistance = 0;
                int indexFarthest = 0;

                for (int index = firstPoint; index < lastPoint; index++)
                {
                    double distance = InternalPerpendicularDistance(
                        points[firstPoint],
                        points[lastPoint],
                        points[index]
                    );
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        indexFarthest = index;
                    }
                }

                if (maxDistance > tolerance && indexFarthest != 0)
                {
                    //Add the largest point that exceeds the tolerance
                    pointIndexesToKeep.Add(indexFarthest);

                    DouglasPeuckerReductionRecursive(
                        points,
                        firstPoint,
                        indexFarthest,
                        tolerance,
                        ref pointIndexesToKeep
                    );

                    firstPoint = indexFarthest;
                    continue;
                }

                break;
            }

            return;

            static double InternalPerpendicularDistance(
                Vector2 point1,
                Vector2 point2,
                Vector2 point
            )
            {
                double area = System.Math.Abs(
                    .5f
                        * (
                            point1.x * point2.y
                            + point2.x * point.y
                            + point.x * point1.y
                            - point2.x * point1.y
                            - point.x * point2.y
                            - point1.x * point.y
                        )
                );
                double bottom = System.Math.Sqrt(
                    System.Math.Pow(point1.x - point2.x, 2.0)
                        + System.Math.Pow(point1.y - point2.y, 2.0)
                );
                double height = area / bottom * 2.0;
                return height;
            }
        }

        public static List<Vector2> Simplify(List<Vector2> points, float epsilon)
        {
            if (points == null || points.Count < 3 || epsilon <= 0)
            {
                return new List<Vector2>(points ?? Enumerable.Empty<Vector2>());
            }

            float maxDistance = 0;
            int index = 0;
            int end = points.Count - 1;

            for (int i = 1; i < end; ++i)
            {
                float distance = PerpendicularDistance(points[i], points[0], points[end]);
                if (distance > maxDistance)
                {
                    index = i;
                    maxDistance = distance;
                }
            }

            List<Vector2> result = new();

            if (maxDistance > epsilon)
            {
                List<Vector2> recResults1 = Simplify(points.GetRange(0, index + 1), epsilon);
                List<Vector2> recResults2 = Simplify(
                    points.GetRange(index, points.Count - index),
                    epsilon
                );

                result.AddRange(recResults1.Take(recResults1.Count - 1));
                result.AddRange(recResults2);
            }
            else
            {
                result.Add(points[0]);
                result.Add(points[end]);
            }

            return result;
        }
    }
}
