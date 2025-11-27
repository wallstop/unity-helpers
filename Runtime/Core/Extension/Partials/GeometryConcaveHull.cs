// ReSharper disable once CheckNamespace
namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using DataStructure.Adapters;
    using Helper;
    using UnityEngine;
    using Utils;
#if UNITY_EDITOR
#endif

    /// <summary>
    /// Provides extension methods for Unity types including GameObject, Transform, Bounds, Rect,
    /// Vector types, UI components, and advanced geometric algorithms such as convex/concave hull generation.
    /// </summary>
    /// <remarks>
    /// Thread Safety: Most methods require execution on the Unity main thread due to Unity API calls.
    /// Performance: Methods use object pooling where possible to minimize allocations.
    /// This class contains geometric algorithms adapted from various sources (see method-level comments).
    /// </remarks>
    public static partial class UnityExtensions
    {
        public enum ConcaveHullStrategy
        {
            [Obsolete("Do not use default value; specify a strategy explicitly.")]
            Unknown = 0,
            Knn = 1,
            EdgeSplit = 2,
        }

        public sealed class ConcaveHullOptions
        {
            public ConcaveHullStrategy Strategy = ConcaveHullStrategy.Knn;
            public int NearestNeighbors = 3;
            public int BucketSize = 40;
            public float AngleThreshold = 90f;
        }

        public static List<FastVector3Int> BuildConcaveHull(
            this IReadOnlyCollection<FastVector3Int> positions,
            ConcaveHullOptions options
        )
        {
            if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            options ??= new ConcaveHullOptions();
            List<FastVector3Int> sourcePoints = ClonePositions(positions);
            using PooledResource<List<Vector2>> vectorPointsResource = Buffers<Vector2>.List.Get(
                out List<Vector2> vectorPoints
            );
            using PooledResource<Dictionary<Vector2, FastVector3Int>> mappingResource =
                DictionaryBuffer<Vector2, FastVector3Int>.Dictionary.Get(
                    out Dictionary<Vector2, FastVector3Int> mapping
                );

            PopulateVectorBuffers(positions, vectorPoints, mapping, out int fallbackZ);
            List<Vector2> vectorHull = vectorPoints.BuildConcaveHull(options);
            List<FastVector3Int> fastHull = ConvertVector2HullToFastVector3(
                vectorHull,
                mapping,
                fallbackZ
            );
            MaybeRepairConcaveCorners(
                fastHull,
                sourcePoints,
                options.Strategy,
                options.AngleThreshold
            );
            return fastHull;
        }

        public static List<FastVector3Int> BuildConcaveHullKnn(
            this IReadOnlyCollection<FastVector3Int> positions,
            int nearestNeighbors = 3
        )
        {
            ConcaveHullOptions options = new()
            {
                Strategy = ConcaveHullStrategy.Knn,
                NearestNeighbors = Math.Max(3, nearestNeighbors),
            };
            return positions.BuildConcaveHull(options);
        }

        public static List<FastVector3Int> BuildConcaveHullEdgeSplit(
            this IReadOnlyCollection<FastVector3Int> positions,
            int bucketSize = 40,
            float angleThreshold = 90f
        )
        {
            ConcaveHullOptions options = new()
            {
                Strategy = ConcaveHullStrategy.EdgeSplit,
                BucketSize = Math.Max(1, bucketSize),
                AngleThreshold = angleThreshold,
            };
            return positions.BuildConcaveHull(options);
        }

        private static List<Vector2> BuildConvexHullJarvis(
            IEnumerable<Vector2> pointsSet,
            bool includeColinearPoints
        )
        {
            using PooledResource<List<Vector2>> ptsRes = Buffers<Vector2>.List.Get(
                out List<Vector2> points
            );
            points.AddRange(pointsSet);
            if (points.Count == 0)
            {
                return new List<Vector2>();
            }

            points.Sort(Vector2LexicographicalComparison);
            DeduplicateSortedVector2(points);
            if (points.Count <= 2)
            {
                return new List<Vector2>(points);
            }
            Vector2 start = points[0];

            bool allColinear = true;
            for (int i = 1; i < points.Count - 1; ++i)
            {
                if (AreApproximatelyColinear(start, points[^1], points[i]))
                {
                    continue;
                }

                allColinear = false;
                break;
            }
            if (allColinear)
            {
                if (includeColinearPoints)
                {
                    return new List<Vector2>(points);
                }
                Vector2 min = start;
                Vector2 max = start;
                foreach (Vector2 w in points)
                {
                    if (w.x < min.x || (Mathf.Approximately(w.x, min.x) && w.y < min.y))
                    {
                        min = w;
                    }
                    if (w.x > max.x || (Mathf.Approximately(w.x, max.x) && w.y > max.y))
                    {
                        max = w;
                    }
                }
                return new List<Vector2> { min, max };
            }

            List<Vector2> hull = new(points.Count + 1);
            Vector2 current = start;
            int guard = 0;
            int guardMax = Math.Max(8, points.Count * 8);
            do
            {
                hull.Add(current);
                if (!includeColinearPoints)
                {
                    TrimTailColinear(hull);
                }

                Vector2 candidate =
                    points[0] == current && points.Count > 1 ? points[1] : points[0];
                for (int i = 0; i < points.Count; ++i)
                {
                    Vector2 p = points[i];
                    if (p == current)
                    {
                        continue;
                    }
                    double rel = Geometry.IsAPointLeftOfVectorOrOnTheLineDouble(
                        current,
                        candidate,
                        p
                    );
                    double tolerance = ComputeAreaTolerance(current, candidate, p);
                    if (rel > tolerance)
                    {
                        candidate = p;
                    }
                    else if (Math.Abs(rel) <= tolerance)
                    {
                        float distCandidate = (candidate - current).sqrMagnitude;
                        float distP = (p - current).sqrMagnitude;
                        if (distP > distCandidate)
                        {
                            candidate = p;
                        }
                    }
                }

                if (includeColinearPoints)
                {
                    using PooledResource<List<Vector2>> colinearRes = Buffers<Vector2>.List.Get(
                        out List<Vector2> colinear
                    );
                    colinear.Clear();
                    for (int i = 0; i < points.Count; ++i)
                    {
                        Vector2 p = points[i];
                        if (p == current || p == candidate)
                        {
                            continue;
                        }
                        double rel = Geometry.IsAPointLeftOfVectorOrOnTheLineDouble(
                            current,
                            candidate,
                            p
                        );
                        double tolerance = ComputeAreaTolerance(current, candidate, p);
                        if (Math.Abs(rel) <= tolerance)
                        {
                            colinear.Add(p);
                        }
                    }
                    if (colinear.Count > 0)
                    {
                        SortByDistanceAscending(colinear, current);
                        using PooledResource<HashSet<Vector2>> hullSetRes =
                            Buffers<Vector2>.HashSet.Get(out HashSet<Vector2> hullSet);
                        foreach (Vector2 h in hull)
                        {
                            hullSet.Add(h);
                        }
                        foreach (Vector2 p in colinear)
                        {
                            if (!hullSet.Contains(p))
                            {
                                hull.Add(p);
                            }
                        }
                        current = candidate;
                    }
                    else
                    {
                        current = candidate;
                    }
                }
                else
                {
                    current = candidate;
                }

                if (++guard > guardMax)
                {
                    break;
                }
            } while (current != start);

            if (hull.Count > 1 && hull[0] == hull[^1])
            {
                hull.RemoveAt(hull.Count - 1);
            }
            if (!includeColinearPoints && hull.Count > 2)
            {
                PruneColinearOnHull(hull);
            }
            return hull;
        }

        private static void PruneColinearOnHull(List<Vector2> hull)
        {
            if (hull == null || hull.Count <= 2)
            {
                return;
            }

            bool removed;
            do
            {
                removed = false;
                for (int i = 0; hull.Count > 2 && i < hull.Count; ++i)
                {
                    int prev = (i - 1 + hull.Count) % hull.Count;
                    int next = (i + 1) % hull.Count;
                    if (AreApproximatelyColinear(hull[prev], hull[i], hull[next]))
                    {
                        hull.RemoveAt(i);
                        removed = true;
                        break;
                    }
                }
            } while (removed);
        }

        private static void TrimTailColinear(List<Vector2> hull)
        {
            if (hull == null)
            {
                return;
            }

            while (hull.Count >= 3)
            {
                int count = hull.Count;
                if (AreApproximatelyColinear(hull[count - 3], hull[count - 2], hull[count - 1]))
                {
                    hull.RemoveAt(count - 2);
                    continue;
                }
                break;
            }
        }

        private static void TrimTailColinear(List<FastVector3Int> hull)
        {
            if (hull == null)
            {
                return;
            }

            while (hull.Count >= 3)
            {
                int count = hull.Count;
                FastVector3Int a = hull[count - 3];
                FastVector3Int b = hull[count - 2];
                FastVector3Int c = hull[count - 1];
                if (Cross(a, b, c) == 0)
                {
                    hull.RemoveAt(count - 2);
                    continue;
                }
                break;
            }
        }

        private static List<Vector2> BuildConvexHullMonotoneChain(
            IEnumerable<Vector2> pointsSet,
            bool includeColinearPoints
        )
        {
            using PooledResource<List<Vector2>> pointsResource = Buffers<Vector2>.List.Get(
                out List<Vector2> points
            );
            points.AddRange(pointsSet);

            points.Sort(Vector2LexicographicalComparison);
            DeduplicateSortedVector2(points);
            if (points.Count <= 1)
            {
                return new List<Vector2>(points);
            }

            if (points.Count >= 2)
            {
                Vector2 first = points[0];
                Vector2 last = points[^1];
                bool allColinear = true;
                for (int i = 1; i < points.Count - 1; ++i)
                {
                    if (!AreApproximatelyColinear(first, last, points[i]))
                    {
                        allColinear = false;
                        break;
                    }
                }
                if (allColinear)
                {
                    if (includeColinearPoints)
                    {
                        return new List<Vector2>(points);
                    }
                    else
                    {
                        return new List<Vector2> { points[0], points[^1] };
                    }
                }
            }

            using PooledResource<List<Vector2>> lowerRes = Buffers<Vector2>.List.Get(
                out List<Vector2> lower
            );
            using PooledResource<List<Vector2>> upperRes = Buffers<Vector2>.List.Get(
                out List<Vector2> upper
            );

            foreach (Vector2 p in points)
            {
                while (lower.Count >= 2)
                {
                    Vector2 a = lower[^2];
                    Vector2 b = lower[^1];
                    double cross = Geometry.IsAPointLeftOfVectorOrOnTheLineDouble(a, b, p);
                    double tolerance = ComputeAreaTolerance(a, b, p);
                    bool isRightTurn = cross < -tolerance;
                    bool isColinear = Math.Abs(cross) <= tolerance;
                    if (isRightTurn || (!includeColinearPoints && isColinear))
                    {
                        lower.RemoveAt(lower.Count - 1);
                        continue;
                    }
                    break;
                }
                lower.Add(p);
            }

            for (int i = points.Count - 1; i >= 0; --i)
            {
                Vector2 p = points[i];
                while (upper.Count >= 2)
                {
                    Vector2 a = upper[^2];
                    Vector2 b = upper[^1];
                    double cross = Geometry.IsAPointLeftOfVectorOrOnTheLineDouble(a, b, p);
                    double tolerance = ComputeAreaTolerance(a, b, p);
                    bool isRightTurn = cross < -tolerance;
                    bool isColinear = Math.Abs(cross) <= tolerance;
                    if (isRightTurn || (!includeColinearPoints && isColinear))
                    {
                        upper.RemoveAt(upper.Count - 1);
                        continue;
                    }
                    break;
                }
                upper.Add(p);
            }

            List<Vector2> hull = new(lower.Count + upper.Count - 2);
            for (int i = 0; i < lower.Count; ++i)
            {
                hull.Add(lower[i]);
            }
            for (int i = 1; i < upper.Count - 1; ++i)
            {
                hull.Add(upper[i]);
            }
            if (!includeColinearPoints && hull.Count > 2)
            {
                PruneColinearOnHull(hull);
            }
            return hull;
        }

        // ===================== Vector2 Convex Hulls =====================

        /// <summary>
        /// Builds a convex hull from a set of Vector2 points using the Monotone Chain algorithm.
        /// </summary>
        /// <param name="pointsSet">The collection of points.</param>
        /// <param name="includeColinearPoints">When true, includes colinear points along edges.</param>
        public static List<Vector2> BuildConvexHull(
            this IEnumerable<Vector2> pointsSet,
            bool includeColinearPoints = true
        )
        {
            return BuildConvexHullMonotoneChain(pointsSet, includeColinearPoints);
        }

        /// <summary>
        /// Builds a convex hull from Vector2 with an explicit algorithm selection.
        /// </summary>
        public static List<Vector2> BuildConvexHull(
            this IEnumerable<Vector2> pointsSet,
            bool includeColinearPoints,
            ConvexHullAlgorithm algorithm
        )
        {
            switch (algorithm)
            {
                case ConvexHullAlgorithm.MonotoneChain:
                    return BuildConvexHullMonotoneChain(pointsSet, includeColinearPoints);
                case ConvexHullAlgorithm.Jarvis:
                    return BuildConvexHullJarvis(pointsSet, includeColinearPoints);
                default:
                    throw new InvalidEnumArgumentException(
                        nameof(algorithm),
                        (int)algorithm,
                        typeof(ConvexHullAlgorithm)
                    );
            }
        }
    }
}
