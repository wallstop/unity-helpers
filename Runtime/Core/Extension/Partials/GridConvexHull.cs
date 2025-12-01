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

    public static partial class UnityExtensions
    {
        public static List<Vector3Int> BuildConvexHull(
            this IEnumerable<Vector3Int> pointsSet,
            Grid grid,
            bool includeColinearPoints = true
        )
        {
            return BuildConvexHullMonotoneChain(pointsSet, grid, includeColinearPoints);
        }

        /// <summary>
        /// Builds a convex hull from a set of FastVector3Int grid positions using the Gift Wrapping (Jarvis March) algorithm.
        /// </summary>
        /// <param name="pointsSet">The collection of FastVector3Int grid positions to build the hull from.</param>
        /// <param name="grid">The Grid used to convert cell positions to world coordinates.</param>
        /// <param name="includeColinearPoints">
        /// If true, includes points that lie on the hull edges. If false, only includes corner points. Default is false.
        /// </param>
        /// <returns>
        /// A list of FastVector3Int positions forming the convex hull in counterclockwise order.
        /// </returns>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread (uses Grid).
        /// Null Handling: Throws if pointsSet or grid is null.
        /// Performance: O(nh) where n is input size and h is hull size. Uses pooled buffers.
        /// Allocations: Allocates return list and uses pooled temporary buffers.
        /// Unity Behavior: Uses grid.CellToWorld for spatial calculations.
        /// Edge Cases: Collections with 3 or fewer points return all points.
        /// Algorithm: Gift Wrapping (Jarvis March). See https://www.habrador.com/tutorials/math/8-convex-hull/
        /// </remarks>
        public static List<FastVector3Int> BuildConvexHull(
            this IEnumerable<FastVector3Int> pointsSet,
            Grid grid,
            bool includeColinearPoints = false
        )
        {
            return BuildConvexHullMonotoneChain(pointsSet, grid, includeColinearPoints);
        }

        public static List<FastVector3Int> BuildConvexHull(
            this IEnumerable<FastVector3Int> pointsSet,
            bool includeColinearPoints = false
        )
        {
            if (pointsSet == null)
            {
                throw new ArgumentNullException(nameof(pointsSet));
            }

            using PooledResource<List<Vector2>> vectorPointsResource = Buffers<Vector2>.List.Get(
                out List<Vector2> vectorPoints
            );
            using PooledResource<Dictionary<Vector2, FastVector3Int>> mappingResource =
                DictionaryBuffer<Vector2, FastVector3Int>.Dictionary.Get(
                    out Dictionary<Vector2, FastVector3Int> mapping
                );

            PopulateVectorBuffers(pointsSet, vectorPoints, mapping, out int fallbackZ);

            List<Vector2> vectorHull = BuildConvexHull(vectorPoints, includeColinearPoints);
            return ConvertVector2HullToFastVector3(vectorHull, mapping, fallbackZ);
        }

        // Disambiguation overload to ensure List<FastVector3Int> resolves here (not via implicit Vector3Int)
        public static List<FastVector3Int> BuildConvexHull(
            this List<FastVector3Int> pointsSet,
            Grid grid,
            bool includeColinearPoints = false
        )
        {
            return BuildConvexHullMonotoneChain(pointsSet, grid, includeColinearPoints);
        }

        public static List<FastVector3Int> BuildConvexHull(
            this IEnumerable<FastVector3Int> pointsSet,
            Grid grid,
            bool includeColinearPoints,
            ConvexHullAlgorithm algorithm
        )
        {
            switch (algorithm)
            {
                case ConvexHullAlgorithm.MonotoneChain:
                    return BuildConvexHullMonotoneChain(pointsSet, grid, includeColinearPoints);
                case ConvexHullAlgorithm.Jarvis:
                    return BuildConvexHullJarvis(pointsSet, grid, includeColinearPoints);
                default:
                    throw new InvalidEnumArgumentException(
                        nameof(algorithm),
                        (int)algorithm,
                        typeof(ConvexHullAlgorithm)
                    );
            }
        }

        public static List<FastVector3Int> BuildConvexHull(
            this IEnumerable<FastVector3Int> pointsSet,
            bool includeColinearPoints,
            ConvexHullAlgorithm algorithm
        )
        {
            switch (algorithm)
            {
                case ConvexHullAlgorithm.MonotoneChain:
                    return pointsSet.BuildConvexHull(includeColinearPoints);
                case ConvexHullAlgorithm.Jarvis:
                    return BuildFastVectorJarvisHull(pointsSet, includeColinearPoints);
                default:
                    throw new InvalidEnumArgumentException(
                        nameof(algorithm),
                        (int)algorithm,
                        typeof(ConvexHullAlgorithm)
                    );
            }
        }

        public static List<Vector3Int> BuildConvexHull(
            this IEnumerable<Vector3Int> pointsSet,
            Grid grid,
            bool includeColinearPoints,
            ConvexHullAlgorithm algorithm
        )
        {
            switch (algorithm)
            {
                case ConvexHullAlgorithm.MonotoneChain:
                    return BuildConvexHullMonotoneChain(pointsSet, grid, includeColinearPoints);
                case ConvexHullAlgorithm.Jarvis:
                {
                    using PooledResource<List<FastVector3Int>> fastLease =
                        Buffers<FastVector3Int>.List.Get(out List<FastVector3Int> fast);
                    foreach (Vector3Int point in pointsSet)
                    {
                        fast.Add(new FastVector3Int(point.x, point.y, point.z));
                    }

                    List<FastVector3Int> hullFast = BuildConvexHullJarvis(
                        fast,
                        grid,
                        includeColinearPoints
                    );
                    return hullFast.ConvertAll(p => (Vector3Int)p);
                }
                default:
                    throw new InvalidEnumArgumentException(
                        nameof(algorithm),
                        (int)algorithm,
                        typeof(ConvexHullAlgorithm)
                    );
            }
        }

        private static List<Vector3Int> BuildConvexHullMonotoneChain(
            IEnumerable<Vector3Int> pointsSet,
            Grid grid,
            bool includeColinearPoints
        )
        {
            using PooledResource<List<Vector3Int>> pointsResource = Buffers<Vector3Int>.List.Get(
                out List<Vector3Int> points
            );
            points.AddRange(pointsSet);

            // Sort by world-space X then Y
            points.Sort(
                (lhs, rhs) =>
                {
                    int cmp = lhs.x.CompareTo(rhs.x);
                    return cmp != 0 ? cmp : lhs.y.CompareTo(rhs.y);
                }
            );
            DeduplicateSortedVector3Int(points);
            if (points.Count <= 1)
            {
                return new List<Vector3Int>(points);
            }

            // Degenerate: all points are colinear → return endpoints (or all if requested)
            if (points.Count >= 2)
            {
                Vector3Int first = points[0];
                Vector3Int last = points[^1];
                bool allColinear = true;
                for (int i = 1; i < points.Count - 1; ++i)
                {
                    long cross = Turn(first, last, points[i]);
                    if (cross != 0)
                    {
                        allColinear = false;
                        break;
                    }
                }
                if (allColinear)
                {
                    if (includeColinearPoints)
                    {
                        return new List<Vector3Int>(points);
                    }
                    else
                    {
                        return new List<Vector3Int> { points[0], points[^1] };
                    }
                }
            }

            using PooledResource<List<Vector3Int>> lowerRes = Buffers<Vector3Int>.List.Get(
                out List<Vector3Int> lower
            );
            using PooledResource<List<Vector3Int>> upperRes = Buffers<Vector3Int>.List.Get(
                out List<Vector3Int> upper
            );

            foreach (Vector3Int p in points)
            {
                while (lower.Count >= 2)
                {
                    long cross = Turn(lower[^2], lower[^1], p);
                    bool isRightTurn = cross < 0;
                    bool isColinear = cross == 0;
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
                Vector3Int p = points[i];
                while (upper.Count >= 2)
                {
                    long cross = Turn(upper[^2], upper[^1], p);
                    bool isRightTurn = cross < 0;
                    bool isColinear = cross == 0;
                    if (isRightTurn || (!includeColinearPoints && isColinear))
                    {
                        upper.RemoveAt(upper.Count - 1);
                        continue;
                    }
                    break;
                }
                upper.Add(p);
            }

            List<Vector3Int> hull = new(lower.Count + upper.Count - 2);
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

            long Turn(Vector3Int a, Vector3Int b, Vector3Int c)
            {
                long abx = (long)b.x - a.x;
                long aby = (long)b.y - a.y;
                long acx = (long)c.x - a.x;
                long acy = (long)c.y - a.y;
                return abx * acy - aby * acx;
            }
        }

        private static List<FastVector3Int> BuildConvexHullMonotoneChain(
            IEnumerable<FastVector3Int> pointsSet,
            Grid grid,
            bool includeColinearPoints
        )
        {
            using PooledResource<List<FastVector3Int>> pointsResource =
                Buffers<FastVector3Int>.List.Get(out List<FastVector3Int> points);
            points.AddRange(pointsSet);

            points.Sort(
                (lhs, rhs) =>
                {
                    int cmp = lhs.x.CompareTo(rhs.x);
                    return cmp != 0 ? cmp : lhs.y.CompareTo(rhs.y);
                }
            );
            DeduplicateSortedFastVector3Int(points);
            if (points.Count <= 1)
            {
                return new List<FastVector3Int>(points);
            }

            // Degenerate: all points are colinear → return endpoints (or all if requested)
            if (points.Count >= 2)
            {
                FastVector3Int first = points[0];
                FastVector3Int last = points[^1];
                bool allColinear = true;
                for (int i = 1; i < points.Count - 1; ++i)
                {
                    long cross = Turn(first, last, points[i]);
                    if (cross != 0)
                    {
                        allColinear = false;
                        break;
                    }
                }
                if (allColinear)
                {
                    if (includeColinearPoints)
                    {
                        return new List<FastVector3Int>(points);
                    }
                    else
                    {
                        return new List<FastVector3Int> { points[0], points[^1] };
                    }
                }
            }

            using PooledResource<List<FastVector3Int>> lowerRes = Buffers<FastVector3Int>.List.Get(
                out List<FastVector3Int> lower
            );
            using PooledResource<List<FastVector3Int>> upperRes = Buffers<FastVector3Int>.List.Get(
                out List<FastVector3Int> upper
            );

            foreach (FastVector3Int p in points)
            {
                while (lower.Count >= 2)
                {
                    long cross = Turn(lower[^2], lower[^1], p);
                    bool isRightTurn = cross < 0;
                    bool isColinear = cross == 0;
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
                FastVector3Int p = points[i];
                while (upper.Count >= 2)
                {
                    long cross = Turn(upper[^2], upper[^1], p);
                    bool isRightTurn = cross < 0;
                    bool isColinear = cross == 0;
                    if (isRightTurn || (!includeColinearPoints && isColinear))
                    {
                        upper.RemoveAt(upper.Count - 1);
                        continue;
                    }
                    break;
                }
                upper.Add(p);
            }

            List<FastVector3Int> hull = new(lower.Count + upper.Count - 2);
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

            long Turn(FastVector3Int a, FastVector3Int b, FastVector3Int c)
            {
                long abx = (long)b.x - a.x;
                long aby = (long)b.y - a.y;
                long acx = (long)c.x - a.x;
                long acy = (long)c.y - a.y;
                return abx * acy - aby * acx;
            }
        }

        private static List<FastVector3Int> BuildConvexHullJarvis(
            IEnumerable<FastVector3Int> pointsSet,
            Grid grid,
            bool includeColinearPoints
        )
        {
            using PooledResource<List<FastVector3Int>> ptsRes = Buffers<FastVector3Int>.List.Get(
                out List<FastVector3Int> points
            );
            points.AddRange(pointsSet);
            if (points.Count == 0)
            {
                return new List<FastVector3Int>();
            }

            points.Sort(
                (lhs, rhs) =>
                {
                    int cmp = lhs.x.CompareTo(rhs.x);
                    return cmp != 0 ? cmp : lhs.y.CompareTo(rhs.y);
                }
            );
            DeduplicateSortedFastVector3Int(points);
            if (points.Count == 0)
            {
                return new List<FastVector3Int>();
            }
            if (points.Count == 1)
            {
                return new List<FastVector3Int>(points);
            }

            // Find leftmost (then lowest Y) start
            FastVector3Int start = points[0];
            for (int i = 1; i < points.Count; ++i)
            {
                FastVector3Int w = points[i];
                if (w.x < start.x || (w.x == start.x && w.y < start.y))
                {
                    start = w;
                }
            }

            // Degenerate: all colinear → endpoints only (or keep all if requested)
            bool allColinear = true;
            FastVector3Int anyOther = start;
            for (int i = 0; i < points.Count; ++i)
            {
                if (points[i] != start)
                {
                    anyOther = points[i];
                    break;
                }
            }
            for (int i = 0; i < points.Count; ++i)
            {
                FastVector3Int p = points[i];
                if (p == start || p == anyOther)
                {
                    continue;
                }
                if (Cross(start, anyOther, p) != 0)
                {
                    allColinear = false;
                    break;
                }
            }
            if (allColinear)
            {
                if (includeColinearPoints)
                {
                    points.Sort(
                        (a, b) =>
                        {
                            int cmp = a.x.CompareTo(b.x);
                            return cmp != 0 ? cmp : a.y.CompareTo(b.y);
                        }
                    );
                    return points;
                }
                else
                {
                    // Return endpoints by min/max grid position
                    FastVector3Int min = start;
                    FastVector3Int max = start;
                    for (int i = 0; i < points.Count; ++i)
                    {
                        FastVector3Int w = points[i];
                        if (w.x < min.x || (w.x == min.x && w.y < min.y))
                        {
                            min = w;
                        }
                        if (w.x > max.x || (w.x == max.x && w.y > max.y))
                        {
                            max = w;
                        }
                    }
                    return new List<FastVector3Int> { min, max };
                }
            }

            List<FastVector3Int> hull = new(points.Count + 1);
            FastVector3Int current = start;
            int guard = 0;
            int guardMax = Math.Max(8, points.Count * 8);
            do
            {
                hull.Add(current);
                if (!includeColinearPoints)
                {
                    TrimTailColinear(hull);
                }

                // Phase 1: Find the most counterclockwise point
                FastVector3Int candidate =
                    points[0] == current && points.Count > 1 ? points[1] : points[0];
                for (int i = 0; i < points.Count; ++i)
                {
                    FastVector3Int p = points[i];
                    if (p == current)
                    {
                        continue;
                    }
                    long rel = Cross(current, candidate, p);
                    if (rel > 0)
                    {
                        // p is more counterclockwise
                        candidate = p;
                    }
                    else if (rel == 0)
                    {
                        // p is collinear with candidate, prefer the farther one
                        long distCandidate = DistanceSquared(current, candidate);
                        long distP = DistanceSquared(current, p);
                        if (distP > distCandidate)
                        {
                            candidate = p;
                        }
                    }
                }

                // Phase 2: If requested, collect ALL collinear points with the candidate direction
                if (includeColinearPoints)
                {
                    using PooledResource<List<FastVector3Int>> colinearRes =
                        Buffers<FastVector3Int>.List.Get(out List<FastVector3Int> colinear);
                    colinear.Clear();

                    for (int i = 0; i < points.Count; ++i)
                    {
                        FastVector3Int p = points[i];
                        // Skip current point AND the candidate (candidate will be added in next iteration)
                        if (p == current || p == candidate)
                        {
                            continue;
                        }
                        long rel = Cross(current, candidate, p);
                        if (rel == 0)
                        {
                            colinear.Add(p);
                        }
                    }

                    if (colinear.Count > 0)
                    {
                        // Sort by distance and add all (excluding duplicates)
                        using PooledResource<float[]> distancesRes =
                            WallstopFastArrayPool<float>.Get(colinear.Count, out float[] distances);
                        for (int i = 0; i < colinear.Count; ++i)
                        {
                            distances[i] = (float)DistanceSquared(current, colinear[i]);
                        }
                        SelectionSort(colinear, distances, colinear.Count);

                        using PooledResource<HashSet<FastVector3Int>> hullSetRes =
                            Buffers<FastVector3Int>.HashSet.Get(
                                out HashSet<FastVector3Int> hullSet
                            );
                        foreach (FastVector3Int h in hull)
                        {
                            hullSet.Add(h);
                        }
                        foreach (FastVector3Int p in colinear)
                        {
                            if (!hullSet.Contains(p))
                            {
                                hull.Add(p);
                            }
                        }
                        // Current becomes the candidate (the farthest collinear point)
                        // Note: we don't use colinear[^1] because candidate is not in colinear list
                        current = candidate;
                    }
                    else
                    {
                        current = candidate;
                    }
                }
                else
                {
                    // Just move to the farthest counterclockwise point
                    current = candidate;
                }

                if (++guard > guardMax)
                {
                    break;
                }
            } while (current != start);

            // Close loop: remove duplicate last if present
            if (hull.Count > 1 && hull[0] == hull[^1])
            {
                hull.RemoveAt(hull.Count - 1);
            }

            if (!includeColinearPoints && hull.Count > 2)
            {
                // Final pruning of any accidental colinear triples
                PruneColinearOnHull(hull);
            }
            return hull;
        }

        private static List<FastVector3Int> BuildFastVectorJarvisHull(
            IEnumerable<FastVector3Int> pointsSet,
            bool includeColinearPoints
        )
        {
            using PooledResource<List<Vector2>> vectorPointsResource = Buffers<Vector2>.List.Get(
                out List<Vector2> vectorPoints
            );
            using PooledResource<Dictionary<Vector2, FastVector3Int>> mappingResource =
                DictionaryBuffer<Vector2, FastVector3Int>.Dictionary.Get(
                    out Dictionary<Vector2, FastVector3Int> mapping
                );

            PopulateVectorBuffers(pointsSet, vectorPoints, mapping, out int fallbackZ);

            using PooledResource<List<Vector2>> hullResource = Buffers<Vector2>.List.Get(
                out List<Vector2> hullBuffer
            );
            using PooledResource<List<int>> indicesResource = Buffers<int>.List.Get(
                out List<int> scratchIndices
            );
            using PooledResource<float[]> distancesResource = WallstopFastArrayPool<float>.Get(
                Math.Max(1, vectorPoints.Count),
                out float[] scratchDistances
            );
            using PooledResource<bool[]> membershipResource = WallstopFastArrayPool<bool>.Get(
                Math.Max(1, vectorPoints.Count),
                out bool[] membership
            );

            List<Vector2> vectorHull = BuildConvexHullJarvisFallback(
                vectorPoints,
                hullBuffer,
                includeColinearPoints,
                scratchIndices,
                scratchDistances,
                membership
            );

            return ConvertVector2HullToFastVector3(vectorHull, mapping, fallbackZ);
        }

        private static void PruneColinearOnHull(List<Vector3Int> hull, Grid grid = null)
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
                    int prevIndex = (i - 1 + hull.Count) % hull.Count;
                    int nextIndex = (i + 1) % hull.Count;
                    Vector3Int prevPoint = hull[prevIndex];
                    Vector3Int currentPoint = hull[i];
                    Vector3Int nextPoint = hull[nextIndex];

                    if (
                        IsColinear(prevPoint, currentPoint, nextPoint)
                        || (
                            grid != null
                            && AreWorldColinear(grid, prevPoint, currentPoint, nextPoint)
                        )
                    )
                    {
                        hull.RemoveAt(i);
                        removed = true;
                        break;
                    }
                }
            } while (removed);
        }

        private static void PruneColinearOnHull(List<FastVector3Int> hull, Grid grid = null)
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
                    int prevIndex = (i - 1 + hull.Count) % hull.Count;
                    int nextIndex = (i + 1) % hull.Count;
                    FastVector3Int prevPoint = hull[prevIndex];
                    FastVector3Int currentPoint = hull[i];
                    FastVector3Int nextPoint = hull[nextIndex];

                    if (
                        IsColinear(prevPoint, currentPoint, nextPoint)
                        || (
                            grid != null
                            && AreWorldColinear(grid, prevPoint, currentPoint, nextPoint)
                        )
                    )
                    {
                        hull.RemoveAt(i);
                        removed = true;
                        break;
                    }
                }
            } while (removed);
        }

        private static long Cross(Vector3Int origin, Vector3Int first, Vector3Int second)
        {
            long firstX = (long)first.x - origin.x;
            long firstY = (long)first.y - origin.y;
            long secondX = (long)second.x - origin.x;
            long secondY = (long)second.y - origin.y;
            return firstX * secondY - firstY * secondX;
        }

        private static long Cross(
            FastVector3Int origin,
            FastVector3Int first,
            FastVector3Int second
        )
        {
            long firstX = (long)first.x - origin.x;
            long firstY = (long)first.y - origin.y;
            long secondX = (long)second.x - origin.x;
            long secondY = (long)second.y - origin.y;
            return firstX * secondY - firstY * secondX;
        }

        private static Vector2 ToWorld2D(Grid grid, Vector3Int cell)
        {
            Vector3 world = grid.CellToWorld(cell);
            return new Vector2(world.x, world.y);
        }

        private static Vector2 ToWorld2D(Grid grid, FastVector3Int cell)
        {
            Vector3 world = grid.CellToWorld((Vector3Int)cell);
            return new Vector2(world.x, world.y);
        }

        private static long DistanceSquared(Vector3Int a, Vector3Int b)
        {
            long dx = (long)b.x - a.x;
            long dy = (long)b.y - a.y;
            return dx * dx + dy * dy;
        }

        private static long DistanceSquared(FastVector3Int a, FastVector3Int b)
        {
            long dx = (long)b.x - a.x;
            long dy = (long)b.y - a.y;
            return dx * dx + dy * dy;
        }

        private static bool IsPointOnSegment(Vector3Int start, Vector3Int end, Vector3Int point)
        {
            if (Cross(start, end, point) != 0)
            {
                return false;
            }

            long dot =
                ((long)point.x - start.x) * ((long)end.x - start.x)
                + ((long)point.y - start.y) * ((long)end.y - start.y);
            if (dot < 0)
            {
                return false;
            }

            long segmentLengthSquared = DistanceSquared(start, end);
            return dot <= segmentLengthSquared;
        }

        private static bool IsPointOnSegment(
            FastVector3Int start,
            FastVector3Int end,
            FastVector3Int point
        )
        {
            if (Cross(start, end, point) != 0)
            {
                return false;
            }

            long dot =
                ((long)point.x - start.x) * ((long)end.x - start.x)
                + ((long)point.y - start.y) * ((long)end.y - start.y);
            if (dot < 0)
            {
                return false;
            }

            long segmentLengthSquared = DistanceSquared(start, end);
            return dot <= segmentLengthSquared;
        }

        private static bool AreAllPointsOnHullEdges(
            IEnumerable<FastVector3Int> points,
            List<FastVector3Int> convexHull
        )
        {
            if (points == null)
            {
                return true;
            }

            int hullCount = convexHull?.Count ?? 0;
            if (hullCount < 2)
            {
                return true;
            }

            foreach (FastVector3Int point in points)
            {
                bool isCorner = convexHull.Contains(point);
                if (isCorner)
                {
                    continue;
                }

                bool onEdge = false;
                for (int i = 0; i < hullCount; ++i)
                {
                    FastVector3Int start = convexHull[i];
                    FastVector3Int end = convexHull[(i + 1) % hullCount];
                    if (IsPointOnSegment(start, end, point))
                    {
                        onEdge = true;
                        break;
                    }
                }

                if (!onEdge)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsColinear(Vector3Int prev, Vector3Int current, Vector3Int next)
        {
            return Cross(prev, current, next) == 0;
        }

        private static bool IsColinear(
            FastVector3Int prev,
            FastVector3Int current,
            FastVector3Int next
        )
        {
            return Cross(prev, current, next) == 0;
        }

        private static bool AreWorldColinear(
            Grid grid,
            Vector3Int prev,
            Vector3Int current,
            Vector3Int next
        )
        {
            if (grid == null)
            {
                return false;
            }

            Vector2 prevWorld = ToWorld2D(grid, prev);
            Vector2 currentWorld = ToWorld2D(grid, current);
            Vector2 nextWorld = ToWorld2D(grid, next);
            float cross = Geometry.IsAPointLeftOfVectorOrOnTheLine(
                prevWorld,
                currentWorld,
                nextWorld
            );
            return Mathf.Abs(cross) <= ConvexHullRelationEpsilon;
        }

        private static bool AreWorldColinear(
            Grid grid,
            FastVector3Int prev,
            FastVector3Int current,
            FastVector3Int next
        )
        {
            if (grid == null)
            {
                return false;
            }

            Vector2 prevWorld = ToWorld2D(grid, prev);
            Vector2 currentWorld = ToWorld2D(grid, current);
            Vector2 nextWorld = ToWorld2D(grid, next);
            float cross = Geometry.IsAPointLeftOfVectorOrOnTheLine(
                prevWorld,
                currentWorld,
                nextWorld
            );
            return Mathf.Abs(cross) <= ConvexHullRelationEpsilon;
        }
    }
}
