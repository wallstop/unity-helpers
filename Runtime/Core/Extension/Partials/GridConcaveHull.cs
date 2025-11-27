// ReSharper disable once CheckNamespace
namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using DataStructure;
    using DataStructure.Adapters;
    using UnityEngine;
    using Utils;

    /// <summary>
    /// Grid-focused concave hull builders (KNN, edge-split, and line-divider variants).
    /// </summary>
    public static partial class UnityExtensions
    {
        private const float ConcaveCornerRepairThresholdDegrees = 90f;

        private readonly struct HullEdge
        {
            public readonly float edgeLength;

            public readonly FastVector3Int from;
            public readonly FastVector3Int to;

            public readonly Vector2 fromWorld;
            public readonly Vector2 toWorld;

            private readonly Grid _grid;

            public HullEdge(FastVector3Int from, FastVector3Int to, Grid grid)
            {
                this.from = from;
                this.to = to;
                _grid = grid;
                fromWorld = grid.CellToWorld(from);
                toWorld = grid.CellToWorld(to);
                edgeLength = (fromWorld - toWorld).sqrMagnitude;
            }

            public bool Intersects(HullEdge other)
            {
                return UnityExtensions.Intersects(
                    fromWorld,
                    toWorld,
                    other.fromWorld,
                    other.toWorld
                );
            }

            public float LargestAngle(FastVector3Int point)
            {
                Vector2 worldPoint = _grid.CellToWorld(point);
                float angleFrom = Vector2.Angle(toWorld - fromWorld, worldPoint - fromWorld);
                float angleTo = Vector2.Angle(fromWorld - toWorld, worldPoint - toWorld);
                return Math.Max(angleFrom, angleTo);
            }
        }

        // =============== Vector2 helpers used by concave hull ===============
        private static void SortByDistanceAscending(List<Vector2> points, Vector2 origin)
        {
            int count = points.Count;
            if (count <= 1)
            {
                return;
            }
            using PooledResource<float[]> distancesResource = WallstopFastArrayPool<float>.Get(
                count
            );
            float[] distances = distancesResource.resource;
            for (int i = 0; i < count; ++i)
            {
                distances[i] = (points[i] - origin).sqrMagnitude;
            }
            SelectionSort(points, distances, count);
        }

        private static void SortByRightHandTurn(
            List<Vector2> points,
            Vector2 current,
            float previousAngle
        )
        {
            int count = points.Count;
            if (count <= 1)
            {
                return;
            }
            using PooledResource<float[]> angleRes = WallstopFastArrayPool<float>.Get(count);
            float[] angles = angleRes.resource;
            for (int i = 0; i < count; ++i)
            {
                float candidateAngle = CalculateAngle(current, points[i]);
                angles[i] = -AngleDifference(previousAngle, candidateAngle);
            }
            SelectionSort(points, angles, count);
        }

        /// <summary>
        /// Determines if a world-space position is inside a polygon hull using the ray-casting algorithm.
        /// </summary>
        public static bool IsPositionInside(List<Vector2> hull, Vector2 position)
        {
            bool isPositionInside = false;
            for (int i = 0; i < hull.Count; ++i)
            {
                Vector2 oldVector = hull[i];
                int nextIndex = (i + 1) % hull.Count;
                Vector2 newVector = hull[nextIndex];

                Vector2 lhs;
                Vector2 rhs;
                if (oldVector.x < newVector.x)
                {
                    lhs = oldVector;
                    rhs = newVector;
                }
                else
                {
                    lhs = newVector;
                    rhs = oldVector;
                }

                if (
                    (newVector.x < position.x) == (position.x <= oldVector.x)
                    && (position.y - (long)lhs.y) * (rhs.x - lhs.x)
                        < (rhs.y - (long)lhs.y) * (position.x - lhs.x)
                )
                {
                    isPositionInside = !isPositionInside;
                }
            }
            return isPositionInside;
        }

        private sealed class ConcaveHullComparer : IComparer<HullEdge>
        {
            public static readonly ConcaveHullComparer Instance = new();

            private ConcaveHullComparer() { }

            public int Compare(HullEdge lhs, HullEdge rhs)
            {
                int comparison = lhs.edgeLength.CompareTo(rhs.edgeLength);
                if (comparison != 0)
                {
                    return comparison;
                }

                comparison = lhs.from.CompareTo(rhs.from);
                if (comparison != 0)
                {
                    return comparison;
                }

                return lhs.to.CompareTo(rhs.to);
            }
        }

        /// <summary>
        /// Builds a concave hull from grid positions using an edge-splitting approach with nearest-neighbor queries.
        /// </summary>
        /// <param name="gridPositions">The collection of grid positions to build the hull from.</param>
        /// <param name="grid">The Grid used for coordinate conversion.</param>
        /// <param name="bucketSize">The number of nearest neighbors to consider for each edge. Default is 40.</param>
        /// <param name="angleThreshold">
        /// The maximum angle (in degrees) for including a point. Higher values create more concave hulls. Default is 90.
        /// </param>
        /// <returns>A list of FastVector3Int positions forming the concave hull.</returns>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread (uses Grid).
        /// Null Handling: Throws ArgumentException if gridPositions is empty. Throws if grid is null.
        /// Performance: O(n²) worst case. Uses QuadTree for spatial queries. Uses pooled buffers extensively.
        /// Allocations: Allocates return list and QuadTree, uses pooled temporary buffers.
        /// Unity Behavior: Uses grid.CellToWorld for spatial calculations.
        /// Edge Cases: Starts with convex hull then iteratively refines edges. May fall back to convex hull if no suitable points found.
        /// Algorithm: Custom edge-splitting with angle-based point selection and intersection checking.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when gridPositions is empty.</exception>
        [Obsolete("Use BuildConcaveHullEdgeSplit or BuildConcaveHull with options.")]
        public static List<FastVector3Int> BuildConcaveHull3(
            this IReadOnlyCollection<FastVector3Int> gridPositions,
            Grid grid,
            int bucketSize = 40,
            float angleThreshold = 90f
        )
        {
            using PooledResource<List<FastVector3Int>> originalGridPositionsBuffer =
                Buffers<FastVector3Int>.List.Get(out List<FastVector3Int> originalGridPositions);
            originalGridPositions.AddRange(gridPositions);

            List<FastVector3Int> convexHull = gridPositions.BuildConvexHull(grid);
            using (
                PooledResource<HashSet<FastVector3Int>> uniqueBuffer =
                    Buffers<FastVector3Int>.HashSet.Get(out HashSet<FastVector3Int> unique)
            )
            {
                foreach (FastVector3Int p in originalGridPositions)
                {
                    unique.Add(p);
                }
                if (unique.Count <= 4)
                {
                    return new List<FastVector3Int>(convexHull);
                }
            }

            if (AreAllPointsOnHullEdges(originalGridPositions, convexHull))
            {
                return new List<FastVector3Int>(convexHull);
            }
            using PooledResource<List<HullEdge>> concaveHullEdgesResource =
                Buffers<HullEdge>.List.Get();
            List<HullEdge> concaveHullEdges = concaveHullEdgesResource.resource;
            if (concaveHullEdges.Capacity < convexHull.Count)
            {
                concaveHullEdges.Capacity = convexHull.Count;
            }

            using PooledResource<SortedSet<HullEdge>> sortedSetBuffer = SetBuffers<HullEdge>
                .GetSortedSetPool(ConcaveHullComparer.Instance)
                .Get(out SortedSet<HullEdge> data);

            for (int i = 0; i < convexHull.Count; ++i)
            {
                FastVector3Int lhs = convexHull[i];
                FastVector3Int rhs = convexHull[(i + 1) % convexHull.Count];
                HullEdge edge = new(lhs, rhs, grid);
                _ = data.Add(edge);
            }

            using PooledResource<HashSet<FastVector3Int>> remainingPointsBuffer =
                Buffers<FastVector3Int>.HashSet.Get(out HashSet<FastVector3Int> remainingPoints);
            foreach (FastVector3Int gridPosition in originalGridPositions)
            {
                remainingPoints.Add(gridPosition);
            }

            remainingPoints.ExceptWith(convexHull);

            Bounds? maybeBounds = CalculateWorldBounds(originalGridPositions, grid);
            if (maybeBounds == null)
            {
                throw new ArgumentException(nameof(gridPositions));
            }

            using PooledResource<List<QuadTree2D<FastVector3Int>.Entry>> quadTreeEntriesResource =
                Buffers<QuadTree2D<FastVector3Int>.Entry>.List.Get(
                    out List<QuadTree2D<FastVector3Int>.Entry> quadTreeEntries
                );
            foreach (FastVector3Int gridPosition in originalGridPositions)
            {
                Vector2 worldPosition = grid.CellToWorld(gridPosition);
                quadTreeEntries.Add(
                    new QuadTree2D<FastVector3Int>.Entry(gridPosition, worldPosition)
                );
            }

            QuadTree2D<FastVector3Int> quadTree = new(
                quadTreeEntries,
                maybeBounds.Value,
                bucketSize: bucketSize
            );
            using PooledResource<List<FastVector3Int>> neighborsBuffer =
                Buffers<FastVector3Int>.List.Get();
            List<FastVector3Int> neighbors = neighborsBuffer.resource;
            if (neighbors.Capacity < bucketSize)
            {
                neighbors.Capacity = bucketSize;
            }
            int iterations = 0;
            int maxIterations = Math.Max(32, originalGridPositions.Count * 16);
            while (0 < data.Count)
            {
                HullEdge edge = data.Max;
                _ = data.Remove(edge);

                Vector2 edgeCenter = edge.fromWorld + (edge.toWorld - edge.fromWorld) / 2;
                quadTree.GetApproximateNearestNeighbors(edgeCenter, bucketSize, neighbors);
                float localMaximumDistance = float.MinValue;
                foreach (FastVector3Int neighbor in neighbors)
                {
                    if (neighbor == edge.to || neighbor == edge.from)
                    {
                        continue;
                    }

                    localMaximumDistance = Math.Max(
                        localMaximumDistance,
                        (CellToWorld(neighbor) - edgeCenter).sqrMagnitude
                    );
                }

                if (edge.edgeLength <= localMaximumDistance)
                {
                    concaveHullEdges.Add(edge);
                    continue;
                }

                float smallestAngle = float.MaxValue;
                FastVector3Int? maybeChosenPoint = null;
                foreach (FastVector3Int remainingPoint in remainingPoints)
                {
                    float maximumAngle = edge.LargestAngle(remainingPoint);
                    if (maximumAngle < smallestAngle)
                    {
                        maybeChosenPoint = remainingPoint;
                        smallestAngle = maximumAngle;
                    }
                }

                if (angleThreshold < smallestAngle)
                {
                    concaveHullEdges.Add(edge);
                    continue;
                }

                if (maybeChosenPoint == null)
                {
                    concaveHullEdges.Add(edge);
                    continue;
                }

                FastVector3Int chosenPoint = maybeChosenPoint.Value;
                HullEdge e2 = new(edge.from, chosenPoint, grid);
                HullEdge e3 = new(chosenPoint, edge.to, grid);
                bool intersects = false;
                foreach (HullEdge convexHullEdge in data)
                {
                    if (convexHullEdge.Intersects(e2))
                    {
                        intersects = true;
                        break;
                    }

                    if (convexHullEdge.Intersects(e3))
                    {
                        intersects = true;
                        break;
                    }
                }

                if (!intersects)
                {
                    foreach (HullEdge concaveHullEdge in concaveHullEdges)
                    {
                        if (concaveHullEdge.Intersects(e2))
                        {
                            intersects = true;
                            break;
                        }

                        if (concaveHullEdge.Intersects(e3))
                        {
                            intersects = true;
                            break;
                        }
                    }
                }

                if (!intersects)
                {
                    _ = data.Add(e2);
                    _ = data.Add(e3);
                    _ = remainingPoints.Remove(maybeChosenPoint.Value);
                }
                else
                {
                    concaveHullEdges.Add(edge);
                }

                ++iterations;
                if (iterations > maxIterations)
                {
                    // Safety: avoid runaway refinement by flushing remaining edges to concave set
                    concaveHullEdges.AddRange(data);
                    break;
                }
            }

            List<FastVector3Int> concaveHull = new(concaveHullEdges.Count);
            HullEdge current = concaveHullEdges[0];
            concaveHullEdges.RemoveAtSwapBack(0);
            concaveHull.Add(current.from);
            while (0 < concaveHullEdges.Count)
            {
                FastVector3Int to = current.to;
                int nextIndex = -1;
                for (int i = 0; i < concaveHullEdges.Count; ++i)
                {
                    HullEdge edge = concaveHullEdges[i];
                    if (edge.from == to)
                    {
                        nextIndex = i;
                        break;
                    }
                }

                if (nextIndex < 0)
                {
                    // Try to recover by using a reversed edge if available.
                    int reverseIndex = -1;
                    for (int i = 0; i < concaveHullEdges.Count; ++i)
                    {
                        HullEdge edge = concaveHullEdges[i];
                        if (edge.to == to)
                        {
                            reverseIndex = i;
                            break;
                        }
                    }

                    if (reverseIndex >= 0)
                    {
                        HullEdge reversed = new(
                            concaveHullEdges[reverseIndex].to,
                            concaveHullEdges[reverseIndex].from,
                            grid
                        );
                        concaveHullEdges.RemoveAtSwapBack(reverseIndex);
                        current = reversed;
                        concaveHull.Add(current.from);
                        continue;
                    }

                    // No connecting edge found; break to avoid infinite loop.
                    break;
                }
                current = concaveHullEdges[nextIndex];
                concaveHullEdges.RemoveAtSwapBack(nextIndex);
                concaveHull.Add(current.from);
            }

            PruneColinearOnHull(concaveHull);
            return concaveHull;

            Vector2 CellToWorld(FastVector3Int cell) => grid.CellToWorld(cell);
        }

        // KNN-style concave hull for Vector2 (port of BuildConcaveHull2)
        private static List<Vector2> BuildConcaveHull2(
            this IReadOnlyCollection<Vector2> input,
            int nearestNeighbors
        )
        {
            const int minimumNearestNeighbors = 3;
            nearestNeighbors = Math.Max(minimumNearestNeighbors, nearestNeighbors);

            using PooledResource<List<Vector2>> dataSetRes = Buffers<Vector2>.List.Get(
                out List<Vector2> dataSet
            );
            using PooledResource<HashSet<Vector2>> uniqueRes = Buffers<Vector2>.HashSet.Get(
                out HashSet<Vector2> unique
            );
            using PooledResource<List<Vector2>> originalRes = Buffers<Vector2>.List.Get(
                out List<Vector2> original
            );
            original.AddRange(input);

            foreach (Vector2 point in original)
            {
                if (unique.Add(point))
                {
                    dataSet.Add(point);
                }
            }

            int totalPoints = dataSet.Count;
            if (totalPoints <= 4)
            {
                return input.BuildConvexHull(includeColinearPoints: false);
            }

            int maximumNearestNeighbors = totalPoints;
            int attemptNearestNeighbors = Math.Min(totalPoints, nearestNeighbors);

            int firstIndex = FindLowestPointIndex(dataSet);
            if (firstIndex < 0)
            {
                return new List<Vector2>(dataSet);
            }

            Vector2 firstPoint = dataSet[firstIndex];
            int maxSteps = Math.Max(16, totalPoints * 6);

            using PooledResource<bool[]> availabilityResource = WallstopFastArrayPool<bool>.Get(
                totalPoints
            );
            bool[] availability = availabilityResource.resource;

            using PooledResource<List<int>> neighborIndicesRes = Buffers<int>.List.Get(
                out List<int> neighborIndices
            );

            using PooledResource<float[]> distanceBufferRes = WallstopFastArrayPool<float>.Get(
                totalPoints
            );
            float[] neighborDistances = distanceBufferRes.resource;

            List<Vector2> hull = new(totalPoints);

            while (true)
            {
                hull.Clear();
                hull.Add(firstPoint);

                if (neighborIndices.Capacity < attemptNearestNeighbors)
                {
                    neighborIndices.Capacity = attemptNearestNeighbors;
                }

                bool success = TryBuildConcaveHull2Attempt(
                    dataSet,
                    hull,
                    availability,
                    neighborIndices,
                    neighborDistances,
                    attemptNearestNeighbors,
                    firstIndex,
                    firstPoint,
                    maxSteps
                );

                if (success)
                {
                    PruneColinearOnHull(hull);
                    return hull;
                }

                if (attemptNearestNeighbors >= maximumNearestNeighbors)
                {
                    return BuildConvexHullJarvisFallback(
                        dataSet,
                        hull,
                        includeColinearPoints: false,
                        neighborIndices,
                        neighborDistances,
                        availability
                    );
                }

                ++attemptNearestNeighbors;
            }
        }

        /// <summary>
        /// Builds a concave hull using a k-nearest neighbors approach.
        /// </summary>
        /// <param name="gridPositions">The collection of grid positions to build the hull from.</param>
        /// <param name="grid">The Grid used for coordinate conversion.</param>
        /// <param name="nearestNeighbors">
        /// The number of nearest neighbors to consider (k parameter). Minimum is 3. Default is 3.
        /// Lower values create more concave hulls, higher values approach convex hull.
        /// </param>
        /// <returns>A list of FastVector3Int positions forming the concave hull.</returns>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread (uses Grid).
        /// Null Handling: Throws if gridPositions or grid is null.
        /// Performance: O(n² k) where n is point count and k is nearestNeighbors. Uses pooled buffers.
        /// Allocations: Allocates return list, uses pooled temporary buffers.
        /// Unity Behavior: Uses grid.CellToWorld for spatial calculations.
        /// Edge Cases: Collections with 3 or fewer points return all points.
        /// Automatically increases k and retries if algorithm fails to encompass all points.
        /// Falls back to convex hull if k reaches the maximum (point count).
        /// Algorithm: k-nearest neighbors concave hull. See https://www.researchgate.net/publication/220868874
        /// </remarks>
        [Obsolete("Use BuildConcaveHullKnn or BuildConcaveHull with options.")]
        public static List<FastVector3Int> BuildConcaveHull2(
            this IReadOnlyCollection<FastVector3Int> gridPositions,
            Grid grid,
            int nearestNeighbors = 3
        )
        {
            const int minimumNearestNeighbors = 3;
            nearestNeighbors = Math.Max(minimumNearestNeighbors, nearestNeighbors);
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }
            using PooledResource<List<FastVector3Int>> dataSetResource =
                Buffers<FastVector3Int>.List.Get(out List<FastVector3Int> dataSet);
            using PooledResource<HashSet<FastVector3Int>> uniquePositionsResource =
                Buffers<FastVector3Int>.HashSet.Get(out HashSet<FastVector3Int> uniquePositions);

            using PooledResource<List<FastVector3Int>> originalGridPositionsBuffer =
                Buffers<FastVector3Int>.List.Get(out List<FastVector3Int> originalGridPositions);
            originalGridPositions.AddRange(gridPositions);

            foreach (FastVector3Int gridPosition in originalGridPositions)
            {
                if (uniquePositions.Add(gridPosition))
                {
                    dataSet.Add(gridPosition);
                }
            }
            int totalPoints = dataSet.Count;
            if (totalPoints <= 4)
            {
                return gridPositions.BuildConvexHull(grid);
            }

            List<FastVector3Int> convexHullSnapshot = dataSet.BuildConvexHull(grid);
            if (AreAllPointsOnHullEdges(dataSet, convexHullSnapshot))
            {
                return new List<FastVector3Int>(convexHullSnapshot);
            }

            int maximumNearestNeighbors = totalPoints;
            int attemptNearestNeighbors = Math.Min(totalPoints, nearestNeighbors);

            int firstIndex = FindLowestGridPointIndex(dataSet, grid);
            if (firstIndex < 0)
            {
                return new List<FastVector3Int>(dataSet);
            }

            FastVector3Int firstPoint = dataSet[firstIndex];
            int maxSteps = Math.Max(16, dataSet.Count * 6);

            using PooledResource<bool[]> availabilityResource = WallstopFastArrayPool<bool>.Get(
                totalPoints
            );
            bool[] availability = availabilityResource.resource;

            using PooledResource<List<int>> neighborIndicesResource = Buffers<int>.List.Get(
                out List<int> neighborIndices
            );

            using PooledResource<float[]> distanceBufferResource = WallstopFastArrayPool<float>.Get(
                totalPoints
            );
            float[] neighborDistances = distanceBufferResource.resource;

            using PooledResource<Vector2[]> worldPositionsResource =
                WallstopFastArrayPool<Vector2>.Get(totalPoints);
            Vector2[] worldPositions = worldPositionsResource.resource;
            for (int i = 0; i < totalPoints; ++i)
            {
                worldPositions[i] = grid.CellToWorld(dataSet[i]);
            }

            List<FastVector3Int> hull = new(totalPoints);

            while (true)
            {
                hull.Clear();
                hull.Add(firstPoint);

                if (neighborIndices.Capacity < attemptNearestNeighbors)
                {
                    neighborIndices.Capacity = attemptNearestNeighbors;
                }

                bool success = TryBuildGridConcaveHullAttempt(
                    dataSet,
                    hull,
                    grid,
                    availability,
                    neighborIndices,
                    neighborDistances,
                    worldPositions,
                    attemptNearestNeighbors,
                    firstIndex,
                    firstPoint,
                    maxSteps
                );

                if (success)
                {
                    PruneColinearOnHull(hull);
                    return hull;
                }

                if (attemptNearestNeighbors >= maximumNearestNeighbors)
                {
                    return BuildGridConvexHullJarvisFallback(
                        dataSet,
                        worldPositions,
                        hull,
                        includeColinearPoints: false,
                        neighborIndices,
                        neighborDistances,
                        availability
                    );
                }

                ++attemptNearestNeighbors;
            }
        }

        private static Bounds? CalculateWorldBounds(
            IEnumerable<FastVector3Int> positions,
            Grid grid
        )
        {
            bool any = false;
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            foreach (FastVector3Int position in positions)
            {
                Vector3 world = grid.CellToWorld(position);
                float x = world.x;
                float y = world.y;
                if (x < minX)
                {
                    minX = x;
                }

                if (x > maxX)
                {
                    maxX = x;
                }

                if (y < minY)
                {
                    minY = y;
                }

                if (y > maxY)
                {
                    maxY = y;
                }

                any = true;
            }

            if (!any)
            {
                return null;
            }

            Vector3 center = new((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
            Vector3 size = new(maxX - minX, maxY - minY, 1f);
            return new Bounds(center, size);
        }

        private static void SortByDistanceAscending(
            List<Vector3Int> points,
            Grid grid,
            Vector2 origin
        )
        {
            int count = points.Count;
            if (count <= 1)
            {
                return;
            }

            using PooledResource<float[]> distancesResource = WallstopFastArrayPool<float>.Get(
                count
            );
            float[] distances = distancesResource.resource;
            for (int i = 0; i < count; ++i)
            {
                Vector2 worldPosition = grid.CellToWorld(points[i]);
                distances[i] = (worldPosition - origin).sqrMagnitude;
            }

            SelectionSort(points, distances, count);
        }

        private static void SortByDistanceAscending(
            List<FastVector3Int> points,
            Grid grid,
            Vector2 origin
        )
        {
            int count = points.Count;
            if (count <= 1)
            {
                return;
            }

            using PooledResource<float[]> distancesResource = WallstopFastArrayPool<float>.Get(
                count
            );
            float[] distances = distancesResource.resource;
            for (int i = 0; i < count; ++i)
            {
                Vector2 worldPosition = grid.CellToWorld(points[i]);
                distances[i] = (worldPosition - origin).sqrMagnitude;
            }

            SelectionSort(points, distances, count);
        }

        private static void SortByRightHandTurn(
            List<FastVector3Int> points,
            Grid grid,
            FastVector3Int current,
            float previousAngle
        )
        {
            int count = points.Count;
            if (count <= 1)
            {
                return;
            }

            using PooledResource<float[]> angleBufferResource = WallstopFastArrayPool<float>.Get(
                count
            );
            float[] angles = angleBufferResource.resource;
            Vector2 currentPoint = grid.CellToWorld(current);
            for (int i = 0; i < count; ++i)
            {
                Vector2 candidatePoint = grid.CellToWorld(points[i]);
                float candidateAngle = CalculateAngle(currentPoint, candidatePoint);
                angles[i] = -AngleDifference(previousAngle, candidateAngle);
            }

            SelectionSort(points, angles, count);
        }

        private static void SelectionSort<T>(List<T> points, float[] distances, int count)
        {
            for (int i = 0; i < count - 1; ++i)
            {
                int minIndex = i;
                float minDistance = distances[i];
                for (int j = i + 1; j < count; ++j)
                {
                    float candidateDistance = distances[j];
                    if (candidateDistance < minDistance)
                    {
                        minDistance = candidateDistance;
                        minIndex = j;
                    }
                }

                if (minIndex != i)
                {
                    (points[i], points[minIndex]) = (points[minIndex], points[i]);
                    (distances[i], distances[minIndex]) = (distances[minIndex], distances[i]);
                }
            }
        }

        private static void RemovePoints<T>(List<T> source, List<T> toRemove)
        {
            for (int i = toRemove.Count - 1; i >= 0; --i)
            {
                _ = source.Remove(toRemove[i]);
            }
        }

        private static bool ShouldRepairConcaveCorners(float angleThreshold)
        {
            return angleThreshold >= ConcaveCornerRepairThresholdDegrees;
        }

        private static List<FastVector3Int> ClonePositions(
            IReadOnlyCollection<FastVector3Int> positions
        )
        {
            if (positions == null)
            {
                return new List<FastVector3Int>();
            }

            if (positions is List<FastVector3Int> list)
            {
                return new List<FastVector3Int>(list);
            }

            return new List<FastVector3Int>(positions);
        }

        private static void MaybeRepairConcaveCorners(
            List<FastVector3Int> hull,
            List<FastVector3Int> originalPoints,
            ConcaveHullStrategy strategy,
            float angleThreshold
        )
        {
            if (
                hull == null
                || originalPoints == null
                || (
                    strategy != ConcaveHullStrategy.Knn
                    && !ShouldRepairConcaveCorners(angleThreshold)
                )
            )
            {
                return;
            }

            InsertMissingAxisCorners(hull, originalPoints);
            RemoveDuplicateVertices(hull);
        }

        private static void InsertMissingAxisCorners(
            List<FastVector3Int> hull,
            List<FastVector3Int> originalPoints
        )
        {
            if (hull == null || originalPoints == null || hull.Count < 3)
            {
                return;
            }

            using PooledResource<HashSet<FastVector3Int>> pointSetResource =
                Buffers<FastVector3Int>.HashSet.Get(out HashSet<FastVector3Int> pointSet);
            pointSet.UnionWith(originalPoints);

            bool inserted;
            do
            {
                inserted = false;
                int count = hull.Count;
                for (int i = 0; i < count; ++i)
                {
                    int nextIndex = (i + 1) % count;
                    FastVector3Int start = hull[i];
                    FastVector3Int end = hull[nextIndex];
                    if (start.x == end.x || start.y == end.y)
                    {
                        continue;
                    }

                    _ = pointSet.Add(start);
                    _ = pointSet.Add(end);

                    List<FastVector3Int> path = TryFindAxisPath(start, end, pointSet);
                    if (path != null && path.Count > 2)
                    {
                        int insertIndex = nextIndex;
                        for (int p = 1; p < path.Count - 1; ++p)
                        {
                            FastVector3Int waypoint = path[p];
                            InsertCorner(hull, insertIndex, waypoint);
                            ++insertIndex;
                            pointSet.Add(waypoint);
                        }

                        inserted = true;
                        break;
                    }

                    FastVector3Int candidateA = new(start.x, end.y, start.z);
                    FastVector3Int candidateB = new(end.x, start.y, start.z);
                    if (pointSet.Contains(candidateA))
                    {
                        InsertCorner(hull, nextIndex, candidateA);
                        pointSet.Add(candidateA);
                        inserted = true;
                        break;
                    }

                    if (pointSet.Contains(candidateB))
                    {
                        InsertCorner(hull, nextIndex, candidateB);
                        pointSet.Add(candidateB);
                        inserted = true;
                        break;
                    }
                }
            } while (inserted);

            EnsureAxisCornersIncluded(hull, originalPoints, pointSet);
            PruneDiagonalOnlyVertices(hull, pointSet);
        }

        private static List<FastVector3Int> TryFindAxisPath(
            FastVector3Int start,
            FastVector3Int end,
            HashSet<FastVector3Int> pointSet
        )
        {
            using PooledResource<Queue<FastVector3Int>> queueResource =
                Buffers<FastVector3Int>.Queue.Get(out Queue<FastVector3Int> frontier);
            using PooledResource<Dictionary<FastVector3Int, FastVector3Int>> parentResource =
                DictionaryBuffer<FastVector3Int, FastVector3Int>.Dictionary.Get(
                    out Dictionary<FastVector3Int, FastVector3Int> parents
                );

            frontier.Enqueue(start);
            parents[start] = start;

            while (frontier.Count > 0)
            {
                FastVector3Int current = frontier.Dequeue();
                if (current == end)
                {
                    break;
                }

                foreach (FastVector3Int neighbor in EnumerateAxisNeighbors(current, pointSet))
                {
                    if (parents.ContainsKey(neighbor))
                    {
                        continue;
                    }

                    parents[neighbor] = current;
                    frontier.Enqueue(neighbor);
                }
            }

            if (!parents.ContainsKey(end))
            {
                return null;
            }

            using PooledResource<List<FastVector3Int>> pathResource =
                Buffers<FastVector3Int>.List.Get(out List<FastVector3Int> pathBuffer);
            pathBuffer.Clear();

            FastVector3Int cursor = end;
            pathBuffer.Add(cursor);
            while (cursor != start)
            {
                cursor = parents[cursor];
                pathBuffer.Add(cursor);
            }

            pathBuffer.Reverse();
            return new List<FastVector3Int>(pathBuffer);
        }

        private static IEnumerable<FastVector3Int> EnumerateAxisNeighbors(
            FastVector3Int origin,
            HashSet<FastVector3Int> pointSet
        )
        {
            FastVector3Int right = new(origin.x + 1, origin.y, origin.z);
            if (pointSet.Contains(right))
            {
                yield return right;
            }

            FastVector3Int left = new(origin.x - 1, origin.y, origin.z);
            if (pointSet.Contains(left))
            {
                yield return left;
            }

            FastVector3Int up = new(origin.x, origin.y + 1, origin.z);
            if (pointSet.Contains(up))
            {
                yield return up;
            }

            FastVector3Int down = new(origin.x, origin.y - 1, origin.z);
            if (pointSet.Contains(down))
            {
                yield return down;
            }
        }

        private static void InsertCorner(
            List<FastVector3Int> hull,
            int insertIndex,
            FastVector3Int candidate
        )
        {
            if (insertIndex == hull.Count)
            {
                hull.Add(candidate);
            }
            else
            {
                hull.Insert(insertIndex, candidate);
            }
        }

        private static void EnsureAxisCornersIncluded(
            List<FastVector3Int> hull,
            List<FastVector3Int> originalPoints,
            HashSet<FastVector3Int> pointSet
        )
        {
            if (hull == null || originalPoints == null)
            {
                return;
            }

            using PooledResource<HashSet<FastVector3Int>> hullSetResource =
                Buffers<FastVector3Int>.HashSet.Get(out HashSet<FastVector3Int> hullSet);
            hullSet.UnionWith(hull);

            bool added;
            do
            {
                added = false;
                foreach (FastVector3Int candidate in originalPoints)
                {
                    if (hullSet.Contains(candidate))
                    {
                        continue;
                    }

                    if (!HasAxisCornerSupport(candidate, pointSet))
                    {
                        continue;
                    }

                    if (!TryConnectCandidateToHull(candidate, hull, pointSet, hullSet))
                    {
                        continue;
                    }

                    added = true;
                    break;
                }
            } while (added);
        }

        private static bool HasAxisCornerSupport(
            FastVector3Int candidate,
            HashSet<FastVector3Int> points
        )
        {
            if (points == null || points.Count == 0)
            {
                return false;
            }

            bool hasX = false;
            bool hasY = false;
            foreach (FastVector3Int point in points)
            {
                if (point == candidate)
                {
                    continue;
                }

                if (!hasX && point.x == candidate.x)
                {
                    hasX = true;
                }

                if (!hasY && point.y == candidate.y)
                {
                    hasY = true;
                }

                if (hasX && hasY)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryConnectCandidateToHull(
            FastVector3Int candidate,
            List<FastVector3Int> hull,
            HashSet<FastVector3Int> pointSet,
            HashSet<FastVector3Int> hullSet
        )
        {
            if (hull == null || hull.Count < 2)
            {
                return false;
            }

            int count = hull.Count;
            for (int i = 0; i < count; ++i)
            {
                FastVector3Int anchor = hull[i];
                if (anchor.x != candidate.x && anchor.y != candidate.y)
                {
                    continue;
                }

                List<FastVector3Int> path = TryFindAxisPath(anchor, candidate, pointSet);
                if (path == null || path.Count <= 1)
                {
                    continue;
                }

                InsertPathAfterIndex(hull, i, path);
                for (int p = 1; p < path.Count; ++p)
                {
                    FastVector3Int waypoint = path[p];
                    hullSet.Add(waypoint);
                    pointSet.Add(waypoint);
                }

                return true;
            }

            return false;
        }

        private static void InsertPathAfterIndex(
            List<FastVector3Int> hull,
            int anchorIndex,
            List<FastVector3Int> path
        )
        {
            int insertIndex = anchorIndex + 1;
            for (int p = 1; p < path.Count; ++p)
            {
                FastVector3Int waypoint = path[p];
                if (insertIndex >= hull.Count)
                {
                    hull.Add(waypoint);
                }
                else
                {
                    hull.Insert(insertIndex, waypoint);
                }

                ++insertIndex;
            }
        }

        private static bool TryInsertAxisCorner(List<FastVector3Int> hull, FastVector3Int candidate)
        {
            if (hull == null || hull.Count < 2)
            {
                return false;
            }

            int count = hull.Count;
            for (int i = 0; i < count; ++i)
            {
                FastVector3Int prev = hull[i];
                FastVector3Int next = hull[(i + 1) % count];
                bool prevSharesX = prev.x == candidate.x && IsBetween(candidate.y, prev.y, next.y);
                bool prevSharesY = prev.y == candidate.y && IsBetween(candidate.x, prev.x, next.x);
                bool nextSharesX = next.x == candidate.x && IsBetween(candidate.y, prev.y, next.y);
                bool nextSharesY = next.y == candidate.y && IsBetween(candidate.x, prev.x, next.x);
                if ((prevSharesX && nextSharesY) || (prevSharesY && nextSharesX))
                {
                    InsertCorner(hull, (i + 1) % count, candidate);
                    return true;
                }
            }

            return false;
        }

        private static bool IsBetween(int value, int boundA, int boundB)
        {
            return boundA <= boundB
                ? boundA <= value && value <= boundB
                : boundB <= value && value <= boundA;
        }

        private static bool HasSharedAxisNeighbor(
            FastVector3Int candidate,
            HashSet<FastVector3Int> points
        )
        {
            if (points == null || points.Count == 0)
            {
                return false;
            }

            foreach (FastVector3Int point in points)
            {
                if (point == candidate)
                {
                    continue;
                }

                if (point.x == candidate.x || point.y == candidate.y)
                {
                    return true;
                }
            }

            return false;
        }

        private static void RemoveDuplicateVertices(List<FastVector3Int> hull)
        {
            if (hull == null || hull.Count <= 1)
            {
                return;
            }

            using PooledResource<HashSet<FastVector3Int>> seenResource =
                Buffers<FastVector3Int>.HashSet.Get(out HashSet<FastVector3Int> seen);
            int index = 0;
            while (index < hull.Count)
            {
                FastVector3Int vertex = hull[index];
                if (seen.Contains(vertex))
                {
                    hull.RemoveAt(index);
                    continue;
                }

                seen.Add(vertex);
                ++index;
            }
        }

        private static void PruneDiagonalOnlyVertices(
            List<FastVector3Int> hull,
            HashSet<FastVector3Int> originalPoints
        )
        {
            if (hull == null || originalPoints == null || originalPoints.Count == 0)
            {
                return;
            }

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            foreach (FastVector3Int point in originalPoints)
            {
                if (point.x < minX)
                {
                    minX = point.x;
                }

                if (point.x > maxX)
                {
                    maxX = point.x;
                }

                if (point.y < minY)
                {
                    minY = point.y;
                }

                if (point.y > maxY)
                {
                    maxY = point.y;
                }
            }

            for (int i = hull.Count - 1; i >= 0; --i)
            {
                FastVector3Int vertex = hull[i];
                if (HasSharedAxisNeighbor(vertex, originalPoints))
                {
                    continue;
                }

                bool insideX = minX < vertex.x && vertex.x < maxX;
                bool insideY = minY < vertex.y && vertex.y < maxY;
                if (insideX && insideY)
                {
                    hull.RemoveAt(i);
                }
            }
        }

        private static float CalculateAngle(Vector2 lhs, Vector2 rhs)
        {
            return Mathf.Atan2(rhs.y - lhs.y, rhs.x - lhs.x);
        }

        private static float AngleDifference(float lhsAngle, float rhsAngle)
        {
            float delta = lhsAngle - rhsAngle;
            float twoPi = Mathf.PI * 2f;
            delta %= twoPi;
            if (delta < 0f)
            {
                delta += twoPi;
            }

            return delta;
        }

        // This one has bugs, user beware
        // https://github.com/Liagson/ConcaveHullGenerator/tree/master

        private readonly struct Line
        {
            public readonly double sqrMagnitude;

            public readonly Vector2 from;
            public readonly Vector2 to;

            public Line(Vector2 from, Vector2 to)
            {
                this.from = from;
                this.to = to;
                sqrMagnitude = (from - to).sqrMagnitude;
            }
        }

        private static void SortLinesByLengthDescending(List<Line> lines)
        {
            int count = lines.Count;
            if (count <= 1)
            {
                return;
            }

            using PooledResource<float[]> lengthBufferResource = WallstopFastArrayPool<float>.Get(
                count
            );
            float[] lengths = lengthBufferResource.resource;
            for (int i = 0; i < count; ++i)
            {
                lengths[i] = -(float)lines[i].sqrMagnitude;
            }

            SelectionSort(lines, lengths, count);
        }

        /// <summary>
        /// Builds a concave hull using iterative line division based on cosine similarity.
        /// </summary>
        /// <param name="gridPositions">The collection of grid positions to build the hull from.</param>
        /// <param name="grid">The Grid used for coordinate conversion.</param>
        /// <param name="scaleFactor">
        /// Scale factor for the search area when finding nearby points. Default is 1.
        /// </param>
        /// <param name="concavity">
        /// Controls hull concavity via cosine threshold. Must be in [-1, 1].
        /// Lower values create more concave hulls. Default is 0.
        /// </param>
        /// <returns>A list of FastVector3Int positions forming the concave hull.</returns>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread (uses Grid).
        /// Null Handling: Throws if gridPositions or grid is null.
        /// Performance: O(n² m) where n is point count and m is iterations. Uses pooled buffers.
        /// Allocations: Allocates return list, uses pooled temporary buffers.
        /// Unity Behavior: Uses grid.CellToWorld and WorldToCell for coordinate conversion.
        /// Edge Cases: Collections with 3 or fewer points return all points.
        /// Warning: This implementation has known bugs (see source comment at line 1705).
        /// Algorithm: Based on https://github.com/Liagson/ConcaveHullGenerator
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when concavity is not in the range [-1, 1].</exception>
        public static List<FastVector3Int> BuildConcaveHull(
            this IEnumerable<FastVector3Int> gridPositions,
            Grid grid,
            float scaleFactor = 1,
            float concavity = 0f
        )
        {
            if (concavity < -1 || 1 < concavity)
            {
                throw new ArgumentException($"Concavity must be between [-1, 1], was {concavity}");
            }

            using PooledResource<List<FastVector3Int>> originalPositionsResource =
                Buffers<FastVector3Int>.List.Get(out List<FastVector3Int> originalGridPositions);
            originalGridPositions.AddRange(gridPositions);

            if (originalGridPositions.Count <= 3)
            {
                return new List<FastVector3Int>(originalGridPositions);
            }

            List<FastVector3Int> convexHull = originalGridPositions.BuildConvexHull(grid);
            using (
                PooledResource<HashSet<FastVector3Int>> uniqueBuffer =
                    Buffers<FastVector3Int>.HashSet.Get(out HashSet<FastVector3Int> unique)
            )
            {
                foreach (FastVector3Int p in originalGridPositions)
                {
                    unique.Add(p);
                }
                if (unique.Count <= 4)
                {
                    return new List<FastVector3Int>(convexHull);
                }
            }
            using PooledResource<HashSet<FastVector3Int>> unusedNodesResource =
                Buffers<FastVector3Int>.HashSet.Get(out HashSet<FastVector3Int> unusedNodes);
            unusedNodes.UnionWith(originalGridPositions);
            unusedNodes.ExceptWith(convexHull);

            using PooledResource<List<Line>> concaveHullLinesResource = Buffers<Line>.List.Get(
                out List<Line> concaveHullLines
            );
            if (concaveHullLines.Capacity < convexHull.Count)
            {
                concaveHullLines.Capacity = convexHull.Count;
            }
            for (int i = 0; i < convexHull.Count; ++i)
            {
                FastVector3Int lhsGridPoint = convexHull[i];
                FastVector3Int rhsGridPoint = convexHull[(i + 1) % convexHull.Count];
                Vector2 lhs = grid.CellToWorld(lhsGridPoint);
                Vector2 rhs = grid.CellToWorld(rhsGridPoint);
                Line line = new(lhs, rhs);
                concaveHullLines.Add(line);
            }

            bool aLineWasDividedInTheIteration;
            int splitIterations = 0;
            int maxSplitIterations = Math.Max(16, originalGridPositions.Count * 8);
            using PooledResource<List<Line>> dividedLineResource = Buffers<Line>.List.Get(
                out List<Line> dividedLine
            );
            if (dividedLine.Capacity < 2)
            {
                dividedLine.Capacity = 2;
            }
            using PooledResource<List<FastVector3Int>> nearbyPointsResource =
                Buffers<FastVector3Int>.List.Get(out List<FastVector3Int> nearbyPoints);
            using PooledResource<Dictionary<Vector2, double>> okMiddlePointsResource =
                DictionaryBuffer<Vector2, double>.Dictionary.Get(
                    out Dictionary<Vector2, double> okMiddlePoints
                );
            do
            {
                // Order by descending length to evaluate the longest edges first
                SortLinesByLengthDescending(concaveHullLines);

                aLineWasDividedInTheIteration = false;
                for (int i = 0; i < concaveHullLines.Count; ++i)
                {
                    Line line = concaveHullLines[i];
                    GetNearbyPoints(line, unusedNodes, grid, scaleFactor, nearbyPoints);
                    if (
                        TryDivideLine(
                            line,
                            nearbyPoints,
                            concaveHullLines,
                            grid,
                            concavity,
                            dividedLine,
                            okMiddlePoints
                        )
                    )
                    {
                        aLineWasDividedInTheIteration = true;
                        FastVector3Int toRemove = grid.WorldToCell(dividedLine[0].to);
                        _ = unusedNodes.Remove(toRemove);
                        concaveHullLines.AddRange(dividedLine);
                        concaveHullLines.RemoveAtSwapBack(i);
                        break;
                    }
                }
                if (++splitIterations > maxSplitIterations)
                {
                    break;
                }
            } while (aLineWasDividedInTheIteration);

            List<FastVector3Int> concaveHull = new(concaveHullLines.Count);
            if (concaveHullLines.Count <= 0)
            {
                return concaveHull;
            }

            Line currentlyConsideredLine = concaveHullLines[0];
            Vector3Int from = grid.WorldToCell(currentlyConsideredLine.from);
            Vector3Int to = grid.WorldToCell(currentlyConsideredLine.to);
            concaveHull.Add(from);
            concaveHull.Add(to);
            concaveHullLines.RemoveAtSwapBack(0);
            int linkIterations = 0;
            int maxLinkIterations = Math.Max(8, concaveHullLines.Count * 3);
            while (0 < concaveHullLines.Count)
            {
                int index = -1;
                for (int i = 0; i < concaveHullLines.Count; ++i)
                {
                    Line candidate = concaveHullLines[i];
                    if (grid.WorldToCell(candidate.from) == to)
                    {
                        index = i;
                        break;
                    }
                }

                if (index < 0)
                {
                    break;
                }

                currentlyConsideredLine = concaveHullLines[index];
                to = grid.WorldToCell(currentlyConsideredLine.to);
                if (to == from)
                {
                    break;
                }
                concaveHull.Add(to);
                concaveHullLines.RemoveAtSwapBack(index);

                if (++linkIterations > maxLinkIterations)
                {
                    break;
                }
            }

            return concaveHull;
        }

        /// <summary>
        /// Determines if a grid position is inside a polygon hull using the ray-casting algorithm.
        /// </summary>
        /// <param name="hull">The polygon hull defined as a list of grid positions.</param>
        /// <param name="gridPosition">The grid position to test.</param>
        /// <param name="grid">The Grid used for coordinate conversion.</param>
        /// <returns>True if the position is inside the hull; otherwise, false.</returns>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread (uses Grid).
        /// Null Handling: Throws if any parameter is null.
        /// Performance: O(n) where n is the hull size.
        /// Allocations: None.
        /// Unity Behavior: Uses grid.CellToWorld for spatial calculations.
        /// Edge Cases: Points on the boundary may or may not be considered inside depending on precision.
        /// Algorithm: Ray-casting (even-odd rule) point-in-polygon test.
        /// </remarks>
        public static bool IsPositionInside(
            List<FastVector3Int> hull,
            FastVector3Int gridPosition,
            Grid grid
        )
        {
            bool isPositionInside = false;
            Vector2 position = grid.CellToWorld(gridPosition);
            for (int i = 0; i < hull.Count; ++i)
            {
                Vector2 oldVector = grid.CellToWorld(hull[i]);
                int nextIndex = (i + 1) % hull.Count;
                Vector2 newVector = grid.CellToWorld(hull[nextIndex]);

                Vector2 lhs;
                Vector2 rhs;
                if (oldVector.x < newVector.x)
                {
                    lhs = oldVector;
                    rhs = newVector;
                }
                else
                {
                    lhs = newVector;
                    rhs = oldVector;
                }

                if (
                    newVector.x < position.x == position.x <= oldVector.x
                    && (position.y - (long)lhs.y) * (rhs.x - lhs.x)
                        < (rhs.y - (long)lhs.y) * (position.x - lhs.x)
                )
                {
                    isPositionInside = !isPositionInside;
                }
            }

            return isPositionInside;
        }

        private static bool TryDivideLine(
            Line line,
            IReadOnlyList<FastVector3Int> nearbyPoints,
            List<Line> concaveHull,
            Grid grid,
            float concavity,
            List<Line> dividedLine,
            Dictionary<Vector2, double> okMiddlePoints
        )
        {
            return TryDivideLine(
                line.from,
                line.to,
                nearbyPoints,
                concaveHull,
                grid,
                concavity,
                dividedLine,
                okMiddlePoints
            );
        }

        private static bool TryDivideLine(
            Vector2 from,
            Vector2 to,
            IReadOnlyList<FastVector3Int> nearbyPoints,
            List<Line> concaveHull,
            Grid grid,
            float concavity,
            List<Line> dividedLine,
            Dictionary<Vector2, double> okMiddlePoints
        )
        {
            dividedLine.Clear();
            okMiddlePoints.Clear();
            for (int i = 0; i < nearbyPoints.Count; ++i)
            {
                FastVector3Int gridPoint = nearbyPoints[i];
                Vector2 point = grid.CellToWorld(gridPoint);
                // Skip strictly colinear points that lie on the current edge to avoid
                // subdividing edges with mid-edge points (which inflates the hull with
                // redundant colinear vertices and breaks expected convex equivalence).
                // This guards against cases like rectangles with extra edge points.
                if (
                    Orientation(from, point, to) == OrientationType.Colinear
                    && LiesOnSegment(from, point, to)
                )
                {
                    continue;
                }
                double cosine = GetCosine(from, to, point);
                if (cosine < concavity)
                {
                    Line newLineA = new(from, point);
                    Line newLineB = new(point, to);
                    if (
                        !LineCollidesWithHull(newLineA, concaveHull)
                        && !LineCollidesWithHull(newLineB, concaveHull)
                    )
                    {
                        okMiddlePoints[point] = cosine;
                    }
                }
            }

            if (okMiddlePoints.Count == 0)
            {
                return false;
            }

            Vector2 middlePoint = default;
            double minCosine = double.MaxValue;
            foreach (KeyValuePair<Vector2, double> entry in okMiddlePoints)
            {
                double cosine = entry.Value;
                if (cosine < minCosine)
                {
                    minCosine = cosine;
                    middlePoint = entry.Key;
                }
            }

            dividedLine.Add(new Line(from, middlePoint));
            dividedLine.Add(new Line(middlePoint, to));
            return true;
        }

        private static bool LineCollidesWithHull(Line line, List<Line> concaveHull)
        {
            return LineCollidesWithHull(line.from, line.to, concaveHull);
        }

        private static bool LineCollidesWithHull(Vector2 from, Vector2 to, List<Line> concaveHull)
        {
            foreach (Line line in concaveHull)
            {
                Vector2 lhs = line.from;
                Vector2 rhs = line.to;

                if (from != lhs && from != rhs && to != lhs && to != rhs)
                {
                    if (Intersects(from, to, lhs, rhs))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Edge-splitting + kNN queries via QuadTree for Vector2 (port of BuildConcaveHull3)
        private static List<Vector2> BuildConcaveHull3(
            this IReadOnlyCollection<Vector2> input,
            int bucketSize,
            float angleThreshold
        )
        {
            using PooledResource<List<Vector2>> originalRes = Buffers<Vector2>.List.Get(
                out List<Vector2> original
            );
            original.AddRange(input);

            List<Vector2> convexHull = input.BuildConvexHull(includeColinearPoints: false);
            using (
                PooledResource<HashSet<Vector2>> uniqRes = Buffers<Vector2>.HashSet.Get(
                    out HashSet<Vector2> uniq
                )
            )
            {
                foreach (Vector2 p in original)
                {
                    uniq.Add(p);
                }
                if (uniq.Count <= 4)
                {
                    return new List<Vector2>(convexHull);
                }
            }

            using PooledResource<List<HullEdgeV2>> edgeListRes = Buffers<HullEdgeV2>.List.Get(
                out List<HullEdgeV2> concaveHullEdges
            );
            if (concaveHullEdges.Capacity < convexHull.Count)
            {
                concaveHullEdges.Capacity = convexHull.Count;
            }
            using PooledResource<SortedSet<HullEdgeV2>> sortedSetRes = SetBuffers<HullEdgeV2>
                .GetSortedSetPool(ConcaveHullComparerV2.Instance)
                .Get(out SortedSet<HullEdgeV2> data);

            for (int i = 0; i < convexHull.Count; ++i)
            {
                Vector2 lhs = convexHull[i];
                Vector2 rhs = convexHull[(i + 1) % convexHull.Count];
                _ = data.Add(new HullEdgeV2(lhs, rhs));
            }

            Bounds? maybeBounds = input.GetBounds();
            if (maybeBounds == null)
            {
                throw new ArgumentException(nameof(input));
            }

            using PooledResource<List<QuadTree2D<Vector2>.Entry>> entriesRes =
                Buffers<QuadTree2D<Vector2>.Entry>.List.Get(
                    out List<QuadTree2D<Vector2>.Entry> entries
                );
            foreach (Vector2 p in original)
            {
                entries.Add(new QuadTree2D<Vector2>.Entry(p, p));
            }
            QuadTree2D<Vector2> quadTree = new(entries, maybeBounds.Value, bucketSize);
            using PooledResource<List<Vector2>> neighborsRes = Buffers<Vector2>.List.Get(
                out List<Vector2> neighbors
            );
            if (neighbors.Capacity < bucketSize)
            {
                neighbors.Capacity = bucketSize;
            }

            int iterations = 0;
            int maxIterations = Math.Max(32, original.Count * 16);
            while (0 < data.Count)
            {
                HullEdgeV2 edge = data.Max;
                _ = data.Remove(edge);

                Vector2 edgeCenter = edge.from + (edge.to - edge.from) / 2f;
                quadTree.GetApproximateNearestNeighbors(edgeCenter, bucketSize, neighbors);
                float localMaximumDistance = float.MinValue;
                foreach (Vector2 n in neighbors)
                {
                    if (n == edge.to || n == edge.from)
                    {
                        continue;
                    }
                    localMaximumDistance = Math.Max(
                        localMaximumDistance,
                        (n - edgeCenter).sqrMagnitude
                    );
                }
                if (edge.edgeLength <= localMaximumDistance)
                {
                    concaveHullEdges.Add(edge);
                    continue;
                }

                float smallestAngle = float.MaxValue;
                Vector2? maybeChosen = null;
                foreach (Vector2 n in neighbors)
                {
                    if (n == edge.to || n == edge.from)
                    {
                        continue;
                    }
                    float angle = edge.LargestAngle(n);
                    if (angle < smallestAngle)
                    {
                        smallestAngle = angle;
                        maybeChosen = n;
                    }
                }
                if (!maybeChosen.HasValue || smallestAngle > angleThreshold)
                {
                    concaveHullEdges.Add(edge);
                    continue;
                }

                Vector2 chosen = maybeChosen.Value;
                HullEdgeV2 e2 = new(edge.from, chosen);
                HullEdgeV2 e3 = new(chosen, edge.to);
                bool intersects = false;
                foreach (HullEdgeV2 hullEdge in data)
                {
                    if (hullEdge.Intersects(e2) || hullEdge.Intersects(e3))
                    {
                        intersects = true;
                        break;
                    }
                }
                if (!intersects)
                {
                    foreach (HullEdgeV2 hullEdge in concaveHullEdges)
                    {
                        if (hullEdge.Intersects(e2) || hullEdge.Intersects(e3))
                        {
                            intersects = true;
                            break;
                        }
                    }
                }
                if (!intersects)
                {
                    _ = data.Add(e2);
                    _ = data.Add(e3);
                }
                else
                {
                    concaveHullEdges.Add(edge);
                }

                ++iterations;
                if (iterations > maxIterations)
                {
                    concaveHullEdges.AddRange(data);
                    break;
                }
            }

            List<Vector2> result = new(concaveHullEdges.Count);
            if (concaveHullEdges.Count == 0)
            {
                return result;
            }
            HullEdgeV2 current = concaveHullEdges[0];
            concaveHullEdges.RemoveAtSwapBack(0);
            result.Add(current.from);
            while (0 < concaveHullEdges.Count)
            {
                Vector2 to = current.to;
                int nextIndex = -1;
                for (int i = 0; i < concaveHullEdges.Count; ++i)
                {
                    HullEdgeV2 e = concaveHullEdges[i];
                    if (e.from == to)
                    {
                        nextIndex = i;
                        break;
                    }
                }
                if (nextIndex < 0)
                {
                    int reverseIndex = -1;
                    for (int i = 0; i < concaveHullEdges.Count; ++i)
                    {
                        HullEdgeV2 e = concaveHullEdges[i];
                        if (e.to == to)
                        {
                            reverseIndex = i;
                            break;
                        }
                    }
                    if (reverseIndex >= 0)
                    {
                        HullEdgeV2 reversed = new(
                            concaveHullEdges[reverseIndex].to,
                            concaveHullEdges[reverseIndex].from
                        );
                        concaveHullEdges.RemoveAtSwapBack(reverseIndex);
                        current = reversed;
                        result.Add(current.from);
                        continue;
                    }
                    break;
                }
                current = concaveHullEdges[nextIndex];
                concaveHullEdges.RemoveAtSwapBack(nextIndex);
                result.Add(current.from);
            }
            return result;
        }

        private readonly struct HullEdgeV2
        {
            public readonly float edgeLength;
            public readonly Vector2 from;
            public readonly Vector2 to;

            public HullEdgeV2(Vector2 from, Vector2 to)
            {
                this.from = from;
                this.to = to;
                edgeLength = (from - to).sqrMagnitude;
            }

            public bool Intersects(HullEdgeV2 other)
            {
                return UnityExtensions.Intersects(from, to, other.from, other.to);
            }

            public float LargestAngle(Vector2 point)
            {
                float angleFrom = Vector2.Angle(to - from, point - from);
                float angleTo = Vector2.Angle(from - to, point - to);
                return Math.Max(angleFrom, angleTo);
            }
        }

        private sealed class ConcaveHullComparerV2 : IComparer<HullEdgeV2>
        {
            public static readonly ConcaveHullComparerV2 Instance = new();

            private ConcaveHullComparerV2() { }

            public int Compare(HullEdgeV2 lhs, HullEdgeV2 rhs)
            {
                int comparison = lhs.edgeLength.CompareTo(rhs.edgeLength);
                if (comparison != 0)
                {
                    return comparison;
                }
                comparison = lhs.from.x.CompareTo(rhs.from.x);
                if (comparison != 0)
                {
                    return comparison;
                }

                comparison = lhs.from.y.CompareTo(rhs.from.y);
                if (comparison != 0)
                {
                    return comparison;
                }

                comparison = lhs.to.x.CompareTo(rhs.to.x);
                if (comparison != 0)
                {
                    return comparison;
                }

                return lhs.to.y.CompareTo(rhs.to.y);
            }
        }

        /// <summary>
        /// Calculates the cosine of the angle at point o formed by points a and b using the law of cosines.
        /// </summary>
        /// <param name="a">First point.</param>
        /// <param name="b">Second point.</param>
        /// <param name="o">The vertex point where the angle is measured.</param>
        /// <returns>The cosine of the angle at o, rounded to 4 decimal places.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe, no Unity API calls.
        /// Null Handling: Not applicable for value types.
        /// Performance: O(1) - Basic trigonometric calculation.
        /// Allocations: None.
        /// Unity Behavior: Uses double precision for accuracy.
        /// Edge Cases: Returns rounded value to avoid floating-point precision issues.
        /// If points are collinear or coincident, may return NaN or extreme values.
        /// Algorithm: Law of cosines: cos(C) = (a² + b² - c²) / (2ab)
        /// </remarks>
        public static double GetCosine(Vector2 a, Vector3 b, Vector3 o)
        {
            /* Law of cosines */
            double aPow2 = (a.x - o.x) * (a.x - o.x) + (a.y - o.y) * (a.y - o.y);
            double bPow2 = (b.x - o.x) * (b.x - o.x) + (b.y - o.y) * (b.y - o.y);
            double cPow2 = (a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y);
            double cos = (aPow2 + bPow2 - cPow2) / (2 * Math.Sqrt(aPow2 * bPow2));
            return Math.Round(cos, 4);
        }

        private static void GetNearbyPoints(
            Line line,
            ICollection<FastVector3Int> points,
            Grid grid,
            float scaleFactor,
            List<FastVector3Int> buffer
        )
        {
            GetNearbyPoints(line.from, line.to, points, grid, scaleFactor, buffer);
        }

        private static void GetNearbyPoints(
            Vector2 from,
            Vector2 to,
            ICollection<FastVector3Int> points,
            Grid grid,
            float scaleFactor,
            List<FastVector3Int> buffer
        )
        {
            buffer.Clear();
            const int maxTries = 2;
            for (int tries = 0; tries < maxTries; ++tries)
            {
                Bounds boundary = GetBoundary(from, to, scaleFactor);
                bool foundAnyPoints = false;
                foreach (FastVector3Int gridPoint in points)
                {
                    Vector2 point = grid.CellToWorld(gridPoint);
                    if (point != from && point != to && boundary.FastContains2D(point))
                    {
                        buffer.Add(gridPoint);
                        foundAnyPoints = true;
                    }
                }

                if (foundAnyPoints)
                {
                    return;
                }

                buffer.Clear();
                scaleFactor *= 4f / 3f;
            }
        }

        private static Bounds GetBoundary(Vector2 from, Vector2 to, float scaleFactor)
        {
            float xMin = Math.Min(from.x, to.x);
            float yMin = Math.Min(from.y, to.y);
            float xMax = Math.Max(from.x, to.x);
            float yMax = Math.Max(from.y, to.y);

            float width = xMax - xMin;
            float height = yMax - yMin;
            return new Bounds(
                new Vector3(xMin + width / 2, yMin + height / 2),
                new Vector3(width, height) * scaleFactor + new Vector3(0.001f, 0.001f)
            );
        }

        /// <summary>
        /// Determines if two line segments intersect.
        /// </summary>
        /// <param name="lhsFrom">Start point of the first line segment.</param>
        /// <param name="lhsTo">End point of the first line segment.</param>
        /// <param name="rhsFrom">Start point of the second line segment.</param>
        /// <param name="rhsTo">End point of the second line segment.</param>
        /// <returns>True if the line segments intersect or overlap; otherwise, false.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe, no Unity API calls.
        /// Null Handling: Not applicable for value types.
        /// Performance: O(1) - Four orientation checks.
        /// Allocations: None.
        /// Unity Behavior: None - pure geometric calculation.
        /// Edge Cases: Segments sharing an endpoint return false. Collinear overlapping segments may return true.
        /// Algorithm: Uses orientation tests. See https://www.geeksforgeeks.org/how-to-check-if-a-given-point-lies-inside-a-polygon/
        /// </remarks>
        public static bool Intersects(
            Vector2 lhsFrom,
            Vector2 lhsTo,
            Vector2 rhsFrom,
            Vector2 rhsTo
        )
        {
            if (lhsFrom == rhsFrom)
            {
                return false;
            }

            if (lhsFrom == rhsTo)
            {
                return false;
            }

            if (lhsTo == rhsFrom)
            {
                return false;
            }

            if (lhsTo == rhsTo)
            {
                return false;
            }

            OrientationType orientation1 = Orientation(lhsFrom, lhsTo, rhsFrom);
            OrientationType orientation2 = Orientation(lhsFrom, lhsTo, rhsTo);
            OrientationType orientation3 = Orientation(rhsFrom, rhsTo, lhsFrom);
            OrientationType orientation4 = Orientation(rhsFrom, rhsTo, lhsTo);

            if (orientation1 != orientation2 && orientation3 != orientation4)
            {
                return true;
            }

            if (orientation1 == OrientationType.Colinear && LiesOnSegment(lhsFrom, rhsFrom, lhsTo))
            {
                return true;
            }

            if (orientation2 == OrientationType.Colinear && LiesOnSegment(lhsFrom, rhsTo, lhsTo))
            {
                return true;
            }

            if (orientation3 == OrientationType.Colinear && LiesOnSegment(rhsFrom, lhsFrom, rhsTo))
            {
                return true;
            }

            if (orientation4 == OrientationType.Colinear && LiesOnSegment(rhsFrom, lhsTo, rhsTo))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if a point q lies on the line segment pr, assuming the points are collinear.
        /// </summary>
        /// <param name="p">Start point of the line segment.</param>
        /// <param name="q">The point to test.</param>
        /// <param name="r">End point of the line segment.</param>
        /// <returns>True if q lies on the line segment pr; otherwise, false.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe, no Unity API calls.
        /// Null Handling: Not applicable for value types.
        /// Performance: O(1) - Four comparisons.
        /// Allocations: None.
        /// Unity Behavior: None - pure geometric calculation.
        /// Edge Cases: Assumes points are collinear. If not collinear, result is undefined.
        /// Uses less-than-or-equal comparisons, so endpoints are considered on the segment.
        /// </remarks>
        public static bool LiesOnSegment(Vector2 p, Vector2 q, Vector2 r)
        {
            return q.x <= Math.Max(p.x, r.x)
                && Math.Min(p.x, r.x) <= q.x
                && q.y <= Math.Max(p.y, r.y)
                && Math.Min(p.y, r.y) <= q.y;
        }

        /// <summary>
        /// Defines the orientation of three ordered points.
        /// </summary>
        public enum OrientationType
        {
            /// <summary>Points are collinear (lie on the same line).</summary>
            Colinear = 0,

            /// <summary>Points form a clockwise turn.</summary>
            Clockwise = 1,

            /// <summary>Points form a counterclockwise turn.</summary>
            Counterclockwise = 2,
        }

        /// <summary>
        /// Determines the orientation of an ordered triplet of points (p, q, r).
        /// </summary>
        /// <param name="p">First point of the triplet.</param>
        /// <param name="q">Second point of the triplet.</param>
        /// <param name="r">Third point of the triplet.</param>
        /// <returns>The orientation type: Colinear, Clockwise, or Counterclockwise.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe, no Unity API calls.
        /// Null Handling: Not applicable for value types.
        /// Performance: O(1) - Simple cross product calculation.
        /// Allocations: None.
        /// Unity Behavior: Uses Mathf.Approximately for floating-point comparison.
        /// Edge Cases: Uses epsilon comparison for determining collinearity.
        /// Algorithm: Based on cross product sign of vectors (q-p) and (r-q).
        /// </remarks>
        public static OrientationType Orientation(Vector2 p, Vector2 q, Vector2 r)
        {
            float value = (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);
            if (Mathf.Approximately(value, 0))
            {
                return OrientationType.Colinear;
            }

            return 0 < value ? OrientationType.Clockwise : OrientationType.Counterclockwise;
        }

        /// <summary>
        /// Rotates a Vector2 by the specified angle in degrees.
        /// </summary>
        /// <param name="v">The vector to rotate.</param>
        /// <param name="degrees">The rotation angle in degrees (positive = counterclockwise).</param>
        /// <returns>The rotated vector.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe, no Unity API calls.
        /// Null Handling: Not applicable for value types.
        /// Performance: O(1) - Two trigonometric functions and basic arithmetic.
        /// Allocations: None - returns value type.
        /// Unity Behavior: Uses Mathf.Sin and Mathf.Cos for rotation.
        /// Edge Cases: Rotation is counterclockwise for positive degrees, clockwise for negative.
        /// Algorithm: Standard 2D rotation matrix application.
        /// </remarks>
        public static Vector2 Rotate(this Vector2 v, float degrees)
        {
            float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
            float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

            float tx = v.x;
            float ty = v.y;

            Vector2 rotatedVector;
            rotatedVector.x = cos * tx - sin * ty;
            rotatedVector.y = sin * tx + cos * ty;

            return rotatedVector;
        }

        /// <summary>
    }
}
