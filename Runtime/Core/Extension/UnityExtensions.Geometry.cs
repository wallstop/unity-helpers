namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using DataStructure;
    using DataStructure.Adapters;
    using Helper;
    using UnityEngine;
    using UnityEngine.UI;
    using Utils;
#if UNITY_EDITOR
    using UnityEditor;
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
        private const float ConvexHullRelationEpsilon = 1e-5f;
        private const double ConvexHullOrientationEpsilon = 1e-8d;

        private static readonly Comparison<Vector2> Vector2LexicographicalComparison = (lhs, rhs) =>
        {
            int cmp = lhs.x.CompareTo(rhs.x);
            return cmp != 0 ? cmp : lhs.y.CompareTo(rhs.y);
        };

        private static double ComputeAreaTolerance(Vector2 a, Vector2 b, Vector2 c)
        {
            double maxComponent = Math.Max(
                Math.Max(
                    Math.Max(Math.Abs(a.x), Math.Abs(a.y)),
                    Math.Max(Math.Abs(b.x), Math.Abs(b.y))
                ),
                Math.Max(Math.Abs(c.x), Math.Abs(c.y))
            );
            double scale = Math.Max(1d, maxComponent);
            return ConvexHullRelationEpsilon * scale * scale;
        }

        private static bool AreApproximatelyColinear(Vector2 a, Vector2 b, Vector2 c)
        {
            double cross = Geometry.IsAPointLeftOfVectorOrOnTheLineDouble(a, b, c);
            double tolerance = ComputeAreaTolerance(a, b, c);
            return Math.Abs(cross) <= tolerance;
        }

        private static bool AreVector2PointsEquivalent(Vector2 lhs, Vector2 rhs)
        {
            return Mathf.Abs(lhs.x - rhs.x) <= ConvexHullRelationEpsilon
                && Mathf.Abs(lhs.y - rhs.y) <= ConvexHullRelationEpsilon;
        }

        private static void DeduplicateSortedVector2(List<Vector2> points)
        {
            if (points == null || points.Count <= 1)
            {
                return;
            }

            int writeIndex = 1;
            for (int readIndex = 1; readIndex < points.Count; ++readIndex)
            {
                Vector2 previous = points[writeIndex - 1];
                Vector2 current = points[readIndex];
                if (!AreVector2PointsEquivalent(previous, current))
                {
                    if (writeIndex != readIndex)
                    {
                        points[writeIndex] = current;
                    }
                    ++writeIndex;
                }
            }

            if (writeIndex < points.Count)
            {
                points.RemoveRange(writeIndex, points.Count - writeIndex);
            }
        }

        private static void DeduplicateSortedVector3Int(List<Vector3Int> points)
        {
            if (points == null || points.Count <= 1)
            {
                return;
            }

            int writeIndex = 1;
            for (int readIndex = 1; readIndex < points.Count; ++readIndex)
            {
                Vector3Int previous = points[writeIndex - 1];
                Vector3Int current = points[readIndex];
                if (previous != current)
                {
                    if (writeIndex != readIndex)
                    {
                        points[writeIndex] = current;
                    }
                    ++writeIndex;
                }
            }

            if (writeIndex < points.Count)
            {
                points.RemoveRange(writeIndex, points.Count - writeIndex);
            }
        }

        private static void DeduplicateSortedFastVector3Int(List<FastVector3Int> points)
        {
            if (points == null || points.Count <= 1)
            {
                return;
            }

            int writeIndex = 1;
            for (int readIndex = 1; readIndex < points.Count; ++readIndex)
            {
                FastVector3Int previous = points[writeIndex - 1];
                FastVector3Int current = points[readIndex];
                if (previous != current)
                {
                    if (writeIndex != readIndex)
                    {
                        points[writeIndex] = current;
                    }
                    ++writeIndex;
                }
            }

            if (writeIndex < points.Count)
            {
                points.RemoveRange(writeIndex, points.Count - writeIndex);
            }
        }

        public enum ConvexHullAlgorithm
        {
            [Obsolete("Do not use default value; specify an algorithm explicitly.")]
            Unknown = 0,
            MonotoneChain = 1,
            Jarvis = 2,
        }

        private static int FindLowestGridPointIndex(List<FastVector3Int> points, Grid grid)
        {
            if (points == null || points.Count == 0)
            {
                return -1;
            }

            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            int lowestIndex = -1;
            float lowestY = float.MaxValue;
            for (int i = 0; i < points.Count; ++i)
            {
                Vector2 candidateWorld = grid.CellToWorld(points[i]);
                if (lowestIndex < 0 || candidateWorld.y < lowestY)
                {
                    lowestIndex = i;
                    lowestY = candidateWorld.y;
                }
            }

            return lowestIndex;
        }

        private static bool TryBuildGridConcaveHullAttempt(
            List<FastVector3Int> points,
            List<FastVector3Int> hull,
            Grid grid,
            bool[] availability,
            List<int> neighborIndices,
            float[] neighborDistances,
            Vector2[] worldPositions,
            int nearestNeighbors,
            int firstIndex,
            FastVector3Int firstPoint,
            int maxSteps
        )
        {
            int totalPoints = points.Count;
            int availableCount = ResetAvailabilityFlags(availability, totalPoints, firstIndex);

            int step = 2;
            float previousAngle = 0f;
            Vector2 currentWorld = worldPositions[firstIndex];
            bool includeFirstCandidate = false;

            while (availableCount > 0)
            {
                if (!includeFirstCandidate && step == 5)
                {
                    includeFirstCandidate = true;
                }

                FillNearestNeighborIndices(
                    availability,
                    neighborIndices,
                    neighborDistances,
                    worldPositions,
                    totalPoints,
                    nearestNeighbors,
                    currentWorld,
                    includeFirstCandidate,
                    firstIndex
                );

                if (neighborIndices.Count == 0)
                {
                    break;
                }

                SortByRightHandTurnIndices(
                    neighborIndices,
                    worldPositions,
                    currentWorld,
                    previousAngle
                );

                bool intersects = true;
                int neighborOffset = -1;
                while (intersects && neighborOffset < neighborIndices.Count - 1)
                {
                    ++neighborOffset;
                    int candidateIndex = neighborIndices[neighborOffset];
                    FastVector3Int candidate = points[candidateIndex];
                    int lastPoint = candidate == firstPoint ? 1 : 0;
                    int j = 2;
                    intersects = false;
                    Vector2 lhsTo = worldPositions[candidateIndex];
                    while (!intersects && j < hull.Count - lastPoint)
                    {
                        Vector2 lhsFrom = grid.CellToWorld(hull[step - 2]);
                        Vector2 rhsFrom = grid.CellToWorld(hull[step - 2 - j]);
                        Vector2 rhsTo = grid.CellToWorld(hull[step - 1 - j]);
                        intersects = Intersects(lhsFrom, lhsTo, rhsFrom, rhsTo);
                        ++j;
                    }
                }

                if (intersects)
                {
                    return RemainingPointsInside(points, availability, hull, grid);
                }

                int nextIndex = neighborIndices[neighborOffset];
                if (nextIndex == firstIndex)
                {
                    break;
                }

                FastVector3Int nextPoint = points[nextIndex];
                hull.Add(nextPoint);
                if (availability[nextIndex])
                {
                    availability[nextIndex] = false;
                    --availableCount;
                }

                currentWorld = worldPositions[nextIndex];
                previousAngle = CalculateAngle(
                    grid.CellToWorld(hull[step - 1]),
                    grid.CellToWorld(hull[step - 2])
                );
                ++step;
                if (step > maxSteps)
                {
                    break;
                }
            }

            return RemainingPointsInside(points, availability, hull, grid);
        }

        private static void FillNearestNeighborIndices(
            bool[] availability,
            List<int> neighborIndices,
            float[] neighborDistances,
            Vector2[] worldPositions,
            int totalPoints,
            int nearestNeighbors,
            Vector2 currentWorld,
            bool includeFirstCandidate,
            int firstIndex
        )
        {
            neighborIndices.Clear();
            if (nearestNeighbors <= 0 || worldPositions == null || totalPoints <= 0)
            {
                return;
            }

            int storedCount = 0;
            for (int i = 0; i < totalPoints; ++i)
            {
                if (!availability[i])
                {
                    continue;
                }

                Vector2 candidateWorld = worldPositions[i];
                float distance = (candidateWorld - currentWorld).sqrMagnitude;
                InsertNeighborCandidate(
                    neighborIndices,
                    neighborDistances,
                    i,
                    distance,
                    ref storedCount,
                    nearestNeighbors
                );
            }

            if (includeFirstCandidate)
            {
                Vector2 firstWorld = worldPositions[firstIndex];
                float firstDistance = (firstWorld - currentWorld).sqrMagnitude;
                InsertNeighborCandidate(
                    neighborIndices,
                    neighborDistances,
                    firstIndex,
                    firstDistance,
                    ref storedCount,
                    nearestNeighbors
                );
            }
        }

        private static void SortByRightHandTurnIndices(
            List<int> neighborIndices,
            Vector2[] worldPositions,
            Vector2 currentWorld,
            float previousAngle
        )
        {
            int count = neighborIndices.Count;
            if (count <= 1)
            {
                return;
            }

            using PooledResource<float[]> angleBufferResource = WallstopFastArrayPool<float>.Get(
                count
            );
            float[] angles = angleBufferResource.resource;
            for (int i = 0; i < count; ++i)
            {
                Vector2 candidatePoint = worldPositions[neighborIndices[i]];
                float candidateAngle = CalculateAngle(currentWorld, candidatePoint);
                angles[i] = -AngleDifference(previousAngle, candidateAngle);
            }

            SelectionSort(neighborIndices, angles, count);
        }

        private static bool RemainingPointsInside(
            List<FastVector3Int> points,
            bool[] availability,
            List<FastVector3Int> hull,
            Grid grid
        )
        {
            for (int i = 0; i < points.Count; ++i)
            {
                if (!availability[i])
                {
                    continue;
                }

                if (!IsPositionInside(hull, points[i], grid))
                {
                    return false;
                }
            }

            return true;
        }

        private static int FindLowestPointIndex(List<Vector2> points)
        {
            if (points == null || points.Count == 0)
            {
                return -1;
            }

            int lowestIndex = -1;
            float lowestY = float.MaxValue;
            Vector2 lowestPoint = default;
            for (int i = 0; i < points.Count; ++i)
            {
                Vector2 candidate = points[i];
                if (
                    lowestIndex < 0
                    || candidate.y < lowestY
                    || (Mathf.Approximately(candidate.y, lowestY) && candidate.x < lowestPoint.x)
                )
                {
                    lowestIndex = i;
                    lowestY = candidate.y;
                    lowestPoint = candidate;
                }
            }

            return lowestIndex;
        }

        private static bool TryBuildConcaveHull2Attempt(
            List<Vector2> points,
            List<Vector2> hull,
            bool[] availability,
            List<int> neighborIndices,
            float[] neighborDistances,
            int nearestNeighbors,
            int firstIndex,
            Vector2 firstPoint,
            int maxSteps
        )
        {
            int totalPoints = points.Count;
            int availableCount = ResetAvailabilityFlags(availability, totalPoints, firstIndex);

            int step = 2;
            float previousAngle = 0f;
            Vector2 currentPoint = firstPoint;
            bool includeFirstCandidate = false;

            while (availableCount > 0)
            {
                if (!includeFirstCandidate && step == 5)
                {
                    includeFirstCandidate = true;
                }

                FillNearestNeighborIndices(
                    points,
                    availability,
                    neighborIndices,
                    neighborDistances,
                    nearestNeighbors,
                    currentPoint,
                    includeFirstCandidate,
                    firstIndex
                );

                if (neighborIndices.Count == 0)
                {
                    break;
                }

                SortByRightHandTurnIndices(neighborIndices, points, currentPoint, previousAngle);

                bool intersects = true;
                int neighborOffset = -1;
                while (intersects && neighborOffset < neighborIndices.Count - 1)
                {
                    ++neighborOffset;
                    Vector2 candidate = points[neighborIndices[neighborOffset]];
                    int lastPoint = candidate == firstPoint ? 1 : 0;
                    int j = 2;
                    intersects = false;
                    Vector2 lhsTo = candidate;
                    while (!intersects && j < hull.Count - lastPoint)
                    {
                        Vector2 lhsFrom = hull[step - 2];
                        Vector2 rhsFrom = hull[step - 2 - j];
                        Vector2 rhsTo = hull[step - 1 - j];
                        intersects = Intersects(lhsFrom, lhsTo, rhsFrom, rhsTo);
                        ++j;
                    }
                }

                if (intersects)
                {
                    return RemainingPointsInside(points, availability, hull);
                }

                int nextIndex = neighborIndices[neighborOffset];
                if (nextIndex == firstIndex)
                {
                    break;
                }

                Vector2 nextPoint = points[nextIndex];
                hull.Add(nextPoint);
                if (availability[nextIndex])
                {
                    availability[nextIndex] = false;
                    --availableCount;
                }

                currentPoint = nextPoint;
                previousAngle = CalculateAngle(hull[step - 1], hull[step - 2]);
                ++step;
                if (step > maxSteps)
                {
                    break;
                }
            }

            return RemainingPointsInside(points, availability, hull);
        }

        private static void FillNearestNeighborIndices(
            List<Vector2> points,
            bool[] availability,
            List<int> neighborIndices,
            float[] neighborDistances,
            int nearestNeighbors,
            Vector2 currentPoint,
            bool includeFirstCandidate,
            int firstIndex
        )
        {
            neighborIndices.Clear();
            if (nearestNeighbors <= 0 || points.Count == 0)
            {
                return;
            }

            int storedCount = 0;
            int totalPoints = points.Count;
            for (int i = 0; i < totalPoints; ++i)
            {
                if (!availability[i])
                {
                    continue;
                }

                Vector2 candidate = points[i];
                float distance = (candidate - currentPoint).sqrMagnitude;
                InsertNeighborCandidate(
                    neighborIndices,
                    neighborDistances,
                    i,
                    distance,
                    ref storedCount,
                    nearestNeighbors
                );
            }

            if (includeFirstCandidate)
            {
                Vector2 firstPoint = points[firstIndex];
                float firstDistance = (firstPoint - currentPoint).sqrMagnitude;
                InsertNeighborCandidate(
                    neighborIndices,
                    neighborDistances,
                    firstIndex,
                    firstDistance,
                    ref storedCount,
                    nearestNeighbors
                );
            }
        }

        private static void SortByRightHandTurnIndices(
            List<int> neighborIndices,
            List<Vector2> points,
            Vector2 currentPoint,
            float previousAngle
        )
        {
            int count = neighborIndices.Count;
            if (count <= 1)
            {
                return;
            }

            using PooledResource<float[]> angleBufferResource = WallstopFastArrayPool<float>.Get(
                count
            );
            float[] angles = angleBufferResource.resource;
            for (int i = 0; i < count; ++i)
            {
                Vector2 candidatePoint = points[neighborIndices[i]];
                float candidateAngle = CalculateAngle(currentPoint, candidatePoint);
                angles[i] = -AngleDifference(previousAngle, candidateAngle);
            }

            SelectionSort(neighborIndices, angles, count);
        }

        private static bool RemainingPointsInside(
            List<Vector2> points,
            bool[] availability,
            List<Vector2> hull
        )
        {
            for (int i = 0; i < points.Count; ++i)
            {
                if (!availability[i])
                {
                    continue;
                }

                if (!IsPositionInside(hull, points[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static int ResetAvailabilityFlags(
            bool[] availability,
            int totalPoints,
            int skipIndex
        )
        {
            if (availability == null)
            {
                return 0;
            }

            int availableCount = 0;
            for (int i = 0; i < totalPoints; ++i)
            {
                bool available = i != skipIndex;
                availability[i] = available;
                if (available)
                {
                    ++availableCount;
                }
            }

            return availableCount;
        }

        internal static List<Vector2> BuildConvexHullJarvisFallback(
            List<Vector2> points,
            List<Vector2> hull,
            bool includeColinearPoints,
            List<int> scratchIndices,
            float[] scratchDistances,
            bool[] membershipFlags
        )
        {
            hull ??= new List<Vector2>();
            hull.Clear();
            int pointCount = points?.Count ?? 0;
            if (pointCount == 0 || points == null)
            {
                return hull;
            }

            if (pointCount <= 2)
            {
                hull.AddRange(points);
                return hull;
            }

            ResetBooleanFlags(membershipFlags, pointCount);

            int startIndex = FindLowestPointIndex(points);
            if (startIndex < 0)
            {
                hull.AddRange(points);
                return hull;
            }

            int currentIndex = startIndex;
            int guard = 0;
            int guardMax = Math.Max(8, pointCount * 8);

            do
            {
                Vector2 current = points[currentIndex];
                hull.Add(current);
                if (membershipFlags != null && membershipFlags.Length > currentIndex)
                {
                    membershipFlags[currentIndex] = true;
                }
                if (!includeColinearPoints)
                {
                    TrimTailColinear(hull);
                }

                int candidateIndex = -1;
                for (int i = 0; i < pointCount; ++i)
                {
                    if (i == currentIndex)
                    {
                        continue;
                    }

                    candidateIndex = i;
                    break;
                }

                if (candidateIndex < 0)
                {
                    break;
                }

                for (int i = 0; i < pointCount; ++i)
                {
                    if (i == currentIndex || i == candidateIndex)
                    {
                        continue;
                    }

                    Vector2 candidate = points[candidateIndex];
                    Vector2 point = points[i];
                    float relation = Geometry.IsAPointLeftOfVectorOrOnTheLine(
                        current,
                        candidate,
                        point
                    );
                    if (relation > ConvexHullRelationEpsilon)
                    {
                        candidateIndex = i;
                        continue;
                    }

                    if (Mathf.Abs(relation) <= ConvexHullRelationEpsilon)
                    {
                        float candidateDistance = (candidate - current).sqrMagnitude;
                        float pointDistance = (point - current).sqrMagnitude;
                        if (pointDistance > candidateDistance)
                        {
                            candidateIndex = i;
                        }
                    }
                }

                if (includeColinearPoints && scratchIndices != null)
                {
                    scratchIndices.Clear();
                    for (int i = 0; i < pointCount; ++i)
                    {
                        if (i == currentIndex || i == candidateIndex)
                        {
                            continue;
                        }

                        float relation = Geometry.IsAPointLeftOfVectorOrOnTheLine(
                            current,
                            points[candidateIndex],
                            points[i]
                        );
                        if (Mathf.Abs(relation) <= ConvexHullRelationEpsilon)
                        {
                            scratchIndices.Add(i);
                        }
                    }

                    SortIndicesByDistance(points, current, scratchIndices, scratchDistances);
                    if (scratchIndices.Count > 0)
                    {
                        foreach (int index in scratchIndices)
                        {
                            if (membershipFlags != null && membershipFlags[index])
                            {
                                continue;
                            }

                            hull.Add(points[index]);
                            if (membershipFlags != null && membershipFlags.Length > index)
                            {
                                membershipFlags[index] = true;
                            }
                        }
                    }
                }

                currentIndex = candidateIndex;
                if (++guard > guardMax)
                {
                    break;
                }
            } while (currentIndex != startIndex);

            if (!includeColinearPoints && hull.Count > 2)
            {
                PruneColinearOnHull(hull);
            }

            return hull;
        }

        internal static List<FastVector3Int> BuildGridConvexHullJarvisFallback(
            List<FastVector3Int> points,
            Vector2[] worldPositions,
            List<FastVector3Int> hull,
            bool includeColinearPoints,
            List<int> scratchIndices,
            float[] scratchDistances,
            bool[] membershipFlags
        )
        {
            hull ??= new List<FastVector3Int>();
            hull.Clear();
            int pointCount = points?.Count ?? 0;
            if (pointCount == 0 || points == null)
            {
                return hull;
            }

            if (pointCount <= 2)
            {
                hull.AddRange(points);
                return hull;
            }

            ResetBooleanFlags(membershipFlags, pointCount);

            int startIndex = FindLowestWorldPositionIndex(worldPositions, pointCount);
            if (startIndex < 0)
            {
                hull.AddRange(points);
                return hull;
            }

            int currentIndex = startIndex;
            int guard = 0;
            int guardMax = Math.Max(8, pointCount * 8);

            do
            {
                hull.Add(points[currentIndex]);
                if (membershipFlags != null && membershipFlags.Length > currentIndex)
                {
                    membershipFlags[currentIndex] = true;
                }
                if (!includeColinearPoints)
                {
                    TrimTailColinear(hull);
                }

                int candidateIndex = -1;
                for (int i = 0; i < pointCount; ++i)
                {
                    if (i == currentIndex)
                    {
                        continue;
                    }

                    candidateIndex = i;
                    break;
                }

                if (candidateIndex < 0)
                {
                    break;
                }

                for (int i = 0; i < pointCount; ++i)
                {
                    if (i == currentIndex || i == candidateIndex)
                    {
                        continue;
                    }

                    Vector2 candidate = worldPositions[candidateIndex];
                    Vector2 point = worldPositions[i];
                    float relation = Geometry.IsAPointLeftOfVectorOrOnTheLine(
                        worldPositions[currentIndex],
                        candidate,
                        point
                    );
                    if (relation > ConvexHullRelationEpsilon)
                    {
                        candidateIndex = i;
                        continue;
                    }

                    if (Mathf.Abs(relation) <= ConvexHullRelationEpsilon)
                    {
                        float candidateDistance = (
                            candidate - worldPositions[currentIndex]
                        ).sqrMagnitude;
                        float pointDistance = (point - worldPositions[currentIndex]).sqrMagnitude;
                        if (pointDistance > candidateDistance)
                        {
                            candidateIndex = i;
                        }
                    }
                }

                if (includeColinearPoints && scratchIndices != null)
                {
                    scratchIndices.Clear();
                    for (int i = 0; i < pointCount; ++i)
                    {
                        if (i == currentIndex || i == candidateIndex)
                        {
                            continue;
                        }

                        float relation = Geometry.IsAPointLeftOfVectorOrOnTheLine(
                            worldPositions[currentIndex],
                            worldPositions[candidateIndex],
                            worldPositions[i]
                        );
                        if (Mathf.Abs(relation) <= ConvexHullRelationEpsilon)
                        {
                            scratchIndices.Add(i);
                        }
                    }

                    SortIndicesByDistance(
                        worldPositions,
                        worldPositions[currentIndex],
                        scratchIndices,
                        scratchDistances
                    );
                    if (scratchIndices.Count > 0)
                    {
                        foreach (int index in scratchIndices)
                        {
                            if (membershipFlags != null && membershipFlags[index])
                            {
                                continue;
                            }

                            hull.Add(points[index]);
                            if (membershipFlags != null && membershipFlags.Length > index)
                            {
                                membershipFlags[index] = true;
                            }
                        }
                    }
                }

                currentIndex = candidateIndex;
                if (++guard > guardMax)
                {
                    break;
                }
            } while (currentIndex != startIndex);

            if (!includeColinearPoints && hull.Count > 2)
            {
                PruneColinearOnHull(hull);
            }

            return hull;
        }

        private static int FindLowestWorldPositionIndex(Vector2[] worldPositions, int count)
        {
            if (worldPositions == null || count <= 0)
            {
                return -1;
            }

            int lowestIndex = -1;
            float lowestY = float.MaxValue;
            float lowestX = float.MaxValue;
            for (int i = 0; i < count; ++i)
            {
                Vector2 candidate = worldPositions[i];
                if (
                    lowestIndex < 0
                    || candidate.y < lowestY
                    || (Mathf.Approximately(candidate.y, lowestY) && candidate.x < lowestX)
                )
                {
                    lowestIndex = i;
                    lowestY = candidate.y;
                    lowestX = candidate.x;
                }
            }

            return lowestIndex;
        }

        private static void ResetBooleanFlags(bool[] flags, int count)
        {
            if (flags == null || count <= 0)
            {
                return;
            }

            int length = Math.Min(flags.Length, count);
            Array.Clear(flags, 0, length);
        }

        private static void SortIndicesByDistance(
            List<Vector2> points,
            Vector2 origin,
            List<int> indices,
            float[] scratchDistances
        )
        {
            if (indices == null || scratchDistances == null)
            {
                return;
            }

            int count = indices.Count;
            if (count <= 1)
            {
                return;
            }

            for (int i = 0; i < count; ++i)
            {
                int pointIndex = indices[i];
                scratchDistances[i] = (points[pointIndex] - origin).sqrMagnitude;
            }

            SelectionSort(indices, scratchDistances, count);
        }

        private static void SortIndicesByDistance(
            Vector2[] worldPositions,
            Vector2 origin,
            List<int> indices,
            float[] scratchDistances
        )
        {
            if (worldPositions == null || indices == null || scratchDistances == null)
            {
                return;
            }

            int count = indices.Count;
            if (count <= 1)
            {
                return;
            }

            for (int i = 0; i < count; ++i)
            {
                int pointIndex = indices[i];
                scratchDistances[i] = (worldPositions[pointIndex] - origin).sqrMagnitude;
            }

            SelectionSort(indices, scratchDistances, count);
        }

        private static void InsertNeighborCandidate(
            List<int> neighborIndices,
            float[] neighborDistances,
            int candidateIndex,
            float candidateDistance,
            ref int storedCount,
            int maximumCount
        )
        {
            if (maximumCount <= 0)
            {
                return;
            }

            if (neighborIndices == null || neighborDistances == null)
            {
                return;
            }

            if (neighborDistances.Length < maximumCount)
            {
                throw new ArgumentException(
                    "Neighbor distance buffer must be at least as large as maximumCount.",
                    nameof(neighborDistances)
                );
            }

            if (storedCount < maximumCount)
            {
                neighborIndices.Add(candidateIndex);
                neighborDistances[storedCount] = candidateDistance;
                int insertPosition = storedCount;
                while (
                    insertPosition > 0 && candidateDistance < neighborDistances[insertPosition - 1]
                )
                {
                    neighborDistances[insertPosition] = neighborDistances[insertPosition - 1];
                    neighborIndices[insertPosition] = neighborIndices[insertPosition - 1];
                    --insertPosition;
                }

                neighborDistances[insertPosition] = candidateDistance;
                neighborIndices[insertPosition] = candidateIndex;
                ++storedCount;
                return;
            }

            if (candidateDistance >= neighborDistances[storedCount - 1])
            {
                return;
            }

            int replacePosition = storedCount - 1;
            while (
                replacePosition > 0 && candidateDistance < neighborDistances[replacePosition - 1]
            )
            {
                neighborDistances[replacePosition] = neighborDistances[replacePosition - 1];
                neighborIndices[replacePosition] = neighborIndices[replacePosition - 1];
                --replacePosition;
            }

            neighborDistances[replacePosition] = candidateDistance;
            neighborIndices[replacePosition] = candidateIndex;
        }

        private static void PopulateVectorBuffers(
            IEnumerable<FastVector3Int> source,
            List<Vector2> vectorPoints,
            Dictionary<Vector2, FastVector3Int> mapping,
            out int fallbackZ
        )
        {
            if (vectorPoints == null)
            {
                throw new ArgumentNullException(nameof(vectorPoints));
            }

            if (mapping == null)
            {
                throw new ArgumentNullException(nameof(mapping));
            }

            vectorPoints.Clear();
            mapping.Clear();
            fallbackZ = 0;
            bool fallbackAssigned = false;

            if (source is ICollection<FastVector3Int> collection)
            {
                if (vectorPoints.Capacity < collection.Count)
                {
                    vectorPoints.Capacity = collection.Count;
                }
            }

            foreach (FastVector3Int point in source)
            {
                Vector2 vectorPoint = new(point.x, point.y);
                vectorPoints.Add(vectorPoint);
                mapping.TryAdd(vectorPoint, point);

                if (fallbackAssigned)
                {
                    continue;
                }

                fallbackZ = point.z;
                fallbackAssigned = true;
            }
        }

        private static List<FastVector3Int> ConvertVector2HullToFastVector3(
            IList<Vector2> vectorHull,
            Dictionary<Vector2, FastVector3Int> mapping,
            int fallbackZ
        )
        {
            if (vectorHull == null)
            {
                return new List<FastVector3Int>();
            }

            List<FastVector3Int> converted = new(vectorHull.Count);
            for (int i = 0; i < vectorHull.Count; ++i)
            {
                Vector2 vertex = vectorHull[i];
                if (!mapping.TryGetValue(vertex, out FastVector3Int point))
                {
                    point = new FastVector3Int(
                        Mathf.RoundToInt(vertex.x),
                        Mathf.RoundToInt(vertex.y),
                        fallbackZ
                    );
                }

                converted.Add(point);
            }

            return converted;
        }

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

        // Unified concave hull entry point with explicit strategy handling.
        public static List<FastVector3Int> BuildConcaveHull(
            this IReadOnlyCollection<FastVector3Int> gridPositions,
            Grid grid,
            ConcaveHullOptions options
        )
        {
            options ??= new ConcaveHullOptions();
            switch (options.Strategy)
            {
                case ConcaveHullStrategy.Knn:
#pragma warning disable CS0618 // Type or member is obsolete
                    return BuildConcaveHull2(
                        gridPositions,
                        grid,
                        Math.Max(3, options.NearestNeighbors)
                    );
#pragma warning restore CS0618 // Type or member is obsolete
                case ConcaveHullStrategy.EdgeSplit:
#pragma warning disable CS0618 // Type or member is obsolete
                    return BuildConcaveHull3(
                        gridPositions,
                        grid,
                        Math.Max(1, options.BucketSize),
                        options.AngleThreshold
                    );
#pragma warning restore CS0618 // Type or member is obsolete
                default:
                    throw new InvalidEnumArgumentException(
                        nameof(options.Strategy),
                        (int)options.Strategy,
                        typeof(ConcaveHullStrategy)
                    );
            }
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
            using PooledResource<List<Vector2>> vectorPointsResource = Buffers<Vector2>.List.Get(
                out List<Vector2> vectorPoints
            );
            using PooledResource<Dictionary<Vector2, FastVector3Int>> mappingResource =
                DictionaryBuffer<Vector2, FastVector3Int>.Dictionary.Get(
                    out Dictionary<Vector2, FastVector3Int> mapping
                );

            PopulateVectorBuffers(positions, vectorPoints, mapping, out int fallbackZ);
            List<Vector2> vectorHull = vectorPoints.BuildConcaveHull(options);
            return ConvertVector2HullToFastVector3(vectorHull, mapping, fallbackZ);
        }

        public static List<FastVector3Int> BuildConcaveHullKnn(
            this IReadOnlyCollection<FastVector3Int> gridPositions,
            Grid grid,
            int nearestNeighbors = 3
        )
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return BuildConcaveHull2(gridPositions, grid, nearestNeighbors);
#pragma warning restore CS0618 // Type or member is obsolete
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
            this IReadOnlyCollection<FastVector3Int> gridPositions,
            Grid grid,
            int bucketSize = 40,
            float angleThreshold = 90f
        )
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return BuildConcaveHull3(gridPositions, grid, bucketSize, angleThreshold);
#pragma warning restore CS0618 // Type or member is obsolete
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
