namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Polyline simplification and distance helpers.
    /// </summary>
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
        /// <summary>
        /// Douglas-Peucker simplification that preserves extreme points with high precision (double tolerance).
        /// </summary>
        /// <param name="points">Input polyline points.</param>
        /// <param name="tolerance">Maximum allowable deviation.</param>
        /// <param name="buffer">Optional destination list (reused if provided).</param>
        /// <returns>Output simplified points (in buffer if provided).</returns>
        public static List<Vector2> SimplifyPrecise(
            List<Vector2> points,
            double tolerance,
            List<Vector2> buffer = null
        )
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
            while (lastPoint > firstPoint && points[firstPoint] == points[lastPoint])
            {
                lastPoint--;
            }

            // If all points are the same, return just the first point
            if (lastPoint <= firstPoint)
            {
                buffer ??= new List<Vector2>(1);
                buffer.Clear();
                buffer.Add(points[firstPoint]);
                return buffer;
            }

            DouglasPeuckerReductionRecursive(
                points,
                firstPoint,
                lastPoint,
                tolerance,
                ref pointIndexsToKeep
            );

            buffer ??= new List<Vector2>(pointIndexsToKeep.Count);
            buffer.Clear();
            pointIndexsToKeep.Sort();
            foreach (int index in pointIndexsToKeep)
            {
                buffer.Add(points[index]);
            }

            return buffer;
        }

        private static void DouglasPeuckerReductionRecursive(
            List<Vector2> points,
            int firstPoint,
            int lastPoint,
            double tolerance,
            ref List<int> pointIndexesToKeep
        )
        {
            do
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
            } while (true);

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

        /// <summary>
        /// Fast Douglas-Peucker simplification using float epsilon.
        /// </summary>
        /// <param name="points">Input polyline points.</param>
        /// <param name="epsilon">Maximum allowable deviation.</param>
        /// <param name="buffer">Optional destination list (reused if provided).</param>
        /// <returns>Output simplified points (in buffer if provided).</returns>
        public static List<Vector2> Simplify(
            List<Vector2> points,
            float epsilon,
            List<Vector2> buffer = null
        )
        {
            buffer ??= new List<Vector2>();
            buffer.Clear();
            if (points == null)
            {
                return buffer;
            }

            if (points.Count < 3 || epsilon <= 0)
            {
                buffer.AddRange(points);
                return buffer;
            }

            SimplifyRecursive(points, 0, points.Count - 1, epsilon, buffer);
            buffer.Add(points[points.Count - 1]);
            return buffer;
        }

        private static void SimplifyRecursive(
            List<Vector2> points,
            int startIndex,
            int endIndex,
            float epsilon,
            List<Vector2> buffer
        )
        {
            if (endIndex <= startIndex + 1)
            {
                buffer.Add(points[startIndex]);
                return;
            }

            float maxDistance = 0;
            int maxIndex = startIndex;

            for (int i = startIndex + 1; i < endIndex; ++i)
            {
                float distance = PerpendicularDistance(
                    points[i],
                    points[startIndex],
                    points[endIndex]
                );
                if (distance > maxDistance)
                {
                    maxIndex = i;
                    maxDistance = distance;
                }
            }

            if (maxDistance > epsilon)
            {
                SimplifyRecursive(points, startIndex, maxIndex, epsilon, buffer);
                SimplifyRecursive(points, maxIndex, endIndex, epsilon, buffer);
            }
            else
            {
                buffer.Add(points[startIndex]);
            }
        }
    }
}
