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

        public readonly struct ConcaveHullOptions
        {
            private const int DefaultNearestNeighbors = 3;
            private const int DefaultBucketSize = 40;
            private const float DefaultAngleThreshold = 90f;

            private static readonly ConcaveHullOptions DefaultOptions = new(
                ConcaveHullStrategy.Knn,
                DefaultNearestNeighbors,
                DefaultBucketSize,
                DefaultAngleThreshold,
                initialized: true
            );

            private readonly ConcaveHullStrategy _strategy;
            private readonly int _nearestNeighbors;
            private readonly int _bucketSize;
            private readonly float _angleThreshold;
            private readonly bool _initialized;

            public ConcaveHullStrategy Strategy =>
                _initialized ? _strategy : ConcaveHullStrategy.Knn;

            public int NearestNeighbors =>
                _initialized ? _nearestNeighbors : DefaultNearestNeighbors;

            public int BucketSize => _initialized ? _bucketSize : DefaultBucketSize;

            public float AngleThreshold => _initialized ? _angleThreshold : DefaultAngleThreshold;

            public ConcaveHullOptions(
                ConcaveHullStrategy strategy,
                int nearestNeighbors = DefaultNearestNeighbors,
                int bucketSize = DefaultBucketSize,
                float angleThreshold = DefaultAngleThreshold
            )
                : this(strategy, nearestNeighbors, bucketSize, angleThreshold, initialized: true)
            { }

            private ConcaveHullOptions(
                ConcaveHullStrategy strategy,
                int nearestNeighbors,
                int bucketSize,
                float angleThreshold,
                bool initialized
            )
            {
                _strategy = strategy;
                _nearestNeighbors = Math.Max(DefaultNearestNeighbors, nearestNeighbors);
                _bucketSize = Math.Max(1, bucketSize);
                _angleThreshold = angleThreshold;
                _initialized = initialized;
            }

            public static ConcaveHullOptions Default => DefaultOptions;

            public static Builder CreateBuilder()
            {
                return new Builder(Default);
            }

            public static ConcaveHullOptions ForKnn(
                int nearestNeighbors = DefaultNearestNeighbors,
                float angleThreshold = DefaultAngleThreshold
            )
            {
                return Default
                    .WithStrategy(ConcaveHullStrategy.Knn)
                    .WithNearestNeighbors(nearestNeighbors)
                    .WithAngleThreshold(angleThreshold);
            }

            public static ConcaveHullOptions ForEdgeSplit(
                int bucketSize = DefaultBucketSize,
                float angleThreshold = DefaultAngleThreshold
            )
            {
                return Default
                    .WithStrategy(ConcaveHullStrategy.EdgeSplit)
                    .WithBucketSize(bucketSize)
                    .WithAngleThreshold(angleThreshold);
            }

            public ConcaveHullOptions WithStrategy(ConcaveHullStrategy strategy)
            {
                return new ConcaveHullOptions(
                    strategy,
                    NearestNeighbors,
                    BucketSize,
                    AngleThreshold
                );
            }

            public ConcaveHullOptions WithNearestNeighbors(int nearestNeighbors)
            {
                return new ConcaveHullOptions(
                    Strategy,
                    nearestNeighbors,
                    BucketSize,
                    AngleThreshold
                );
            }

            public ConcaveHullOptions WithBucketSize(int bucketSize)
            {
                return new ConcaveHullOptions(
                    Strategy,
                    NearestNeighbors,
                    bucketSize,
                    AngleThreshold
                );
            }

            public ConcaveHullOptions WithAngleThreshold(float angleThreshold)
            {
                return new ConcaveHullOptions(
                    Strategy,
                    NearestNeighbors,
                    BucketSize,
                    angleThreshold
                );
            }

            public struct Builder
            {
                private ConcaveHullStrategy _strategy;
                private int _nearestNeighbors;
                private int _bucketSize;
                private float _angleThreshold;

                internal Builder(ConcaveHullOptions seed)
                {
                    _strategy = seed.Strategy;
                    _nearestNeighbors = seed.NearestNeighbors;
                    _bucketSize = seed.BucketSize;
                    _angleThreshold = seed.AngleThreshold;
                }

                public Builder WithStrategy(ConcaveHullStrategy strategy)
                {
                    _strategy = strategy;
                    return this;
                }

                public Builder WithNearestNeighbors(int nearestNeighbors)
                {
                    _nearestNeighbors = nearestNeighbors;
                    return this;
                }

                public Builder WithBucketSize(int bucketSize)
                {
                    _bucketSize = bucketSize;
                    return this;
                }

                public Builder WithAngleThreshold(float angleThreshold)
                {
                    _angleThreshold = angleThreshold;
                    return this;
                }

                public ConcaveHullOptions Build()
                {
                    return new ConcaveHullOptions(
                        _strategy,
                        _nearestNeighbors,
                        _bucketSize,
                        _angleThreshold
                    );
                }
            }
        }

        public static List<FastVector3Int> BuildConcaveHull(
            this IReadOnlyCollection<FastVector3Int> positions,
            ConcaveHullOptions? options = null
        )
        {
            if (positions == null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            ConcaveHullOptions appliedOptions = options ?? ConcaveHullOptions.Default;
            using PooledResource<List<FastVector3Int>> sourcePointsLease =
                Buffers<FastVector3Int>.List.Get(out List<FastVector3Int> sourcePoints);
            sourcePoints.AddRange(positions);
            using PooledResource<List<Vector2>> vectorPointsResource = Buffers<Vector2>.List.Get(
                out List<Vector2> vectorPoints
            );
            using PooledResource<Dictionary<Vector2, FastVector3Int>> mappingResource =
                DictionaryBuffer<Vector2, FastVector3Int>.Dictionary.Get(
                    out Dictionary<Vector2, FastVector3Int> mapping
                );

            PopulateVectorBuffers(positions, vectorPoints, mapping, out int fallbackZ);
            List<Vector2> vectorHull = BuildConcaveHullRaw(vectorPoints, appliedOptions);
            List<FastVector3Int> fastHull = ConvertVector2HullToFastVector3(
                vectorHull,
                mapping,
                fallbackZ
            );
#if ENABLE_CONCAVE_HULL_STATS
            ConcaveHullRepairStats repairStats = new(fastHull.Count, sourcePoints.Count);
            MaybeRepairConcaveCorners(
                fastHull,
                sourcePoints,
                appliedOptions.Strategy,
                appliedOptions.AngleThreshold,
                repairStats
            );
            TrackHullRepairStats(fastHull, repairStats);
#else
            MaybeRepairConcaveCorners(
                fastHull,
                sourcePoints,
                appliedOptions.Strategy,
                appliedOptions.AngleThreshold
            );
#endif
            return fastHull;
        }

        public static List<FastVector3Int> BuildConcaveHullKnn(
            this IReadOnlyCollection<FastVector3Int> positions,
            int nearestNeighbors = 3
        )
        {
            ConcaveHullOptions options = ConcaveHullOptions
                .Default.WithStrategy(ConcaveHullStrategy.Knn)
                .WithNearestNeighbors(Math.Max(3, nearestNeighbors));
            return positions.BuildConcaveHull(options);
        }

        public static List<FastVector3Int> BuildConcaveHullEdgeSplit(
            this IReadOnlyCollection<FastVector3Int> positions,
            int bucketSize = 40,
            float angleThreshold = 90f
        )
        {
            int clampedBucketSize = Math.Max(1, bucketSize);
            float effectiveAngleThreshold = clampedBucketSize <= 1 ? 0f : angleThreshold;
            ConcaveHullOptions options = ConcaveHullOptions
                .Default.WithStrategy(ConcaveHullStrategy.EdgeSplit)
                .WithBucketSize(clampedBucketSize)
                .WithAngleThreshold(effectiveAngleThreshold);
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
