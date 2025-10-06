namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DataStructure;
    using DataStructure.Adapters;
    using Helper;
    using Random;
    using UnityEngine;
    using UnityEngine.UI;
    using Utils;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    public static class UnityExtensions
    {
        private const float ConvexHullRelationEpsilon = 1e-5f;
        private const double ConvexHullOrientationEpsilon = 1e-8d;

        public static Vector2 GetCenter(this GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out CenterPointOffset centerPointOffset))
            {
                return centerPointOffset.CenterPoint;
            }

            return gameObject.transform.position;
        }

        public static Bounds Bounds(this Rect rect)
        {
            return new Bounds(rect.center, rect.size);
        }

        public static Rect Rect(this Bounds bounds)
        {
            return new Rect(bounds.center - bounds.extents, bounds.size);
        }

        public static Rect GetWorldRect(this RectTransform transform)
        {
            using PooledResource<Vector3[]> fourCornersResource =
                WallstopFastArrayPool<Vector3>.Get(4);
            Vector3[] fourCorners = fourCornersResource.resource;
            transform.GetWorldCorners(fourCorners);
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            foreach (Vector3 corner in fourCorners)
            {
                minX = Mathf.Min(minX, corner.x);
                maxX = Mathf.Max(maxX, corner.x);
                minY = Mathf.Min(minY, corner.y);
                maxY = Mathf.Max(maxY, corner.y);
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        public static Bounds OrthographicBounds(this Camera camera)
        {
            if (camera == null)
            {
                throw new ArgumentNullException(nameof(camera));
            }

            int screenHeight = Screen.height;
            if (screenHeight == 0)
            {
                screenHeight = 1;
            }

            float screenAspect = (float)Screen.width / screenHeight;
            float cameraHeight = camera.orthographicSize * 2;
            float depth = camera.farClipPlane - camera.nearClipPlane;
            if (depth <= 0f)
            {
                depth = 1f;
            }

            Vector3 size = new(cameraHeight * screenAspect, cameraHeight, depth);
            return new Bounds(camera.transform.position, size);
        }

        public static string ToJsonString(this Vector3 vector)
        {
            return FormattableString.Invariant($"{{{vector.x}, {vector.y}, {vector.z}}}");
        }

        public static string ToJsonString(this Vector2 vector)
        {
            return FormattableString.Invariant($"{{{vector.x}, {vector.y}}}");
        }

        public static bool IsNoise(this Vector2 inputVector, float threshold = 0.2f)
        {
            float limit = Mathf.Abs(threshold);
            return Mathf.Abs(inputVector.x) <= limit && Mathf.Abs(inputVector.y) <= limit;
        }

        public static void Stop(this Rigidbody2D rigidBody)
        {
            if (rigidBody == null)
            {
                return;
            }

            rigidBody.velocity = Vector2.zero;
            rigidBody.angularVelocity = 0;
            rigidBody.Sleep();
        }

        public static BoundsInt ExpandBounds(this BoundsInt source, BoundsInt other)
        {
            int xMin = Math.Min(source.xMin, other.xMin);
            int xMax = Math.Max(source.xMax, other.xMax);
            int yMin = Math.Min(source.yMin, other.yMin);
            int yMax = Math.Max(source.yMax, other.yMax);
            int zMin = Math.Min(source.zMin, other.zMin);
            int zMax = Math.Max(source.zMax, other.zMax);
            return new BoundsInt(xMin, yMin, zMin, xMax - xMin, yMax - yMin, zMax - zMin);
        }

        public static BoundsInt? GetBounds(
            this IEnumerable<Vector3Int> positions,
            bool inclusive = false
        )
        {
            bool any = false;
            int xMin = int.MaxValue;
            int xMax = int.MinValue;
            int yMin = int.MaxValue;
            int yMax = int.MinValue;
            int zMin = int.MaxValue;
            int zMax = int.MinValue;
            foreach (Vector3Int position in positions)
            {
                any = true;
                xMin = Math.Min(xMin, position.x);
                xMax = Math.Max(xMax, position.x);
                yMin = Math.Min(yMin, position.y);
                yMax = Math.Max(yMax, position.y);
                zMin = Math.Min(zMin, position.z);
                zMax = Math.Max(zMax, position.z);
            }

            if (!any)
            {
                return null;
            }
            return new BoundsInt(
                xMin,
                yMin,
                zMin,
                xMax - xMin + (inclusive ? 0 : 1),
                yMax - yMin + (inclusive ? 0 : 1),
                zMax - zMin + (inclusive ? 0 : 1)
            );
        }

        public static BoundsInt? GetBounds(this IEnumerable<FastVector3Int> positions)
        {
            bool any = false;
            int xMin = int.MaxValue;
            int xMax = int.MinValue;
            int yMin = int.MaxValue;
            int yMax = int.MinValue;
            int zMin = int.MaxValue;
            int zMax = int.MinValue;
            foreach (FastVector3Int position in positions)
            {
                any = true;
                xMin = Math.Min(xMin, position.x);
                xMax = Math.Max(xMax, position.x);
                yMin = Math.Min(yMin, position.y);
                yMax = Math.Max(yMax, position.y);
                zMin = Math.Min(zMin, position.z);
                zMax = Math.Max(zMax, position.z);
            }

            if (!any)
            {
                return null;
            }
            return new BoundsInt(
                xMin,
                yMin,
                zMin,
                xMax - xMin + 1,
                yMax - yMin + 1,
                zMax - zMin + 1
            );
        }

        public static Bounds? GetBounds(this IEnumerable<Vector2> positions)
        {
            bool any = false;
            float xMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMin = float.MaxValue;
            float yMax = float.MinValue;
            foreach (Vector2 position in positions)
            {
                any = true;
                xMin = Math.Min(xMin, position.x);
                xMax = Math.Max(xMax, position.x);
                yMin = Math.Min(yMin, position.y);
                yMax = Math.Max(yMax, position.y);
            }

            if (!any)
            {
                return null;
            }

            Vector3 center = new((xMax + xMin) / 2f, (yMax + yMin) / 2f);
            Vector3 size = new(xMax - xMin, yMax - yMin);
            return new Bounds(center, size);
        }

        public static Bounds? GetBounds(this IEnumerable<Bounds> boundaries)
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;
            Vector3 centerSum = Vector3.zero;
            int count = 0;
            foreach (Bounds boundary in boundaries)
            {
                centerSum += boundary.center;
                count++;
                Vector3 min = boundary.min;
                Vector3 max = boundary.max;
                minX = Math.Min(minX, min.x);
                maxX = Math.Max(maxX, max.x);
                minY = Math.Min(minY, min.y);
                maxY = Math.Max(maxY, max.y);
                minZ = Math.Min(minZ, min.z);
                maxZ = Math.Max(maxZ, max.z);
            }

            if (count == 0)
            {
                return null;
            }

            return new Bounds(
                centerSum / count,
                new Vector3(maxX - minX, maxY - minY, maxZ - minZ)
            );
        }

        // https://www.habrador.com/tutorials/math/8-convex-hull/
        public static List<Vector3Int> BuildConvexHull(
            this IEnumerable<Vector3Int> pointsSet,
            Grid grid,
            IRandom random = null,
            bool includeColinearPoints = true
        )
        {
            using PooledResource<List<Vector3Int>> pointsResource = Buffers<Vector3Int>.List.Get(
                out List<Vector3Int> points
            );
            foreach (Vector3Int point in pointsSet)
            {
                points.Add(point);
            }

            if (points.Count <= 3)
            {
                return new List<Vector3Int>(points);
            }

            random ??= PRNG.Instance;

            Vector2 CellToWorld(Vector3Int position) => grid.CellToWorld(position);

            Vector3Int startPoint = points[0];
            Vector2 startPointWorldPosition = CellToWorld(startPoint);
            for (int i = 1; i < points.Count; ++i)
            {
                Vector3Int testPoint = points[i];
                Vector2 testPointWorldPosition = CellToWorld(testPoint);
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (
                    testPointWorldPosition.x < startPointWorldPosition.x
                    || (
                        Mathf.Approximately(testPointWorldPosition.x, startPointWorldPosition.x)
                        && testPointWorldPosition.y < startPointWorldPosition.y
                    )
                )
                {
                    startPoint = testPoint;
                    startPointWorldPosition = testPointWorldPosition;
                }
            }

            List<Vector3Int> convexHull = new(points.Count);
            convexHull.Add(startPoint);
            _ = points.Remove(startPoint);
            Vector3Int currentPoint = convexHull[0];
            using PooledResource<List<Vector3Int>> colinearPointsResource =
                Buffers<Vector3Int>.List.Get(out List<Vector3Int> colinearPoints);
            int counter = 0;
            while (true)
            {
                if (counter == 2)
                {
                    points.Add(convexHull[0]);
                }

                if (points.Count <= 0)
                {
                    return convexHull;
                }

                Vector3Int nextPoint = random.NextOf(points);
                Vector2 currentPointWorldPosition = CellToWorld(currentPoint);
                Vector2 nextPointWorldPosition = CellToWorld(nextPoint);
                foreach (Vector3Int point in points)
                {
                    if (Equals(point, nextPoint))
                    {
                        continue;
                    }

                    float relation = Geometry.IsAPointLeftOfVectorOrOnTheLine(
                        currentPointWorldPosition,
                        nextPointWorldPosition,
                        CellToWorld(point)
                    );
                    if (Mathf.Approximately(relation, 0))
                    {
                        colinearPoints.Add(point);
                    }
                    else if (relation < 0)
                    {
                        nextPoint = point;
                        nextPointWorldPosition = CellToWorld(nextPoint);
                        colinearPoints.Clear();
                    }
                }

                if (0 < colinearPoints.Count)
                {
                    colinearPoints.Add(nextPoint);
                    SortByDistanceAscending(colinearPoints, grid, currentPointWorldPosition);

                    if (includeColinearPoints)
                    {
                        convexHull.AddRange(colinearPoints);
                    }
                    else
                    {
                        convexHull.Add(colinearPoints[^1]);
                        _ = points.Remove(colinearPoints[^1]);
                    }

                    currentPoint = colinearPoints[^1];
                    RemovePoints(points, colinearPoints);
                    colinearPoints.Clear();
                }
                else
                {
                    convexHull.Add(nextPoint);
                    _ = points.Remove(nextPoint);
                    currentPoint = nextPoint;
                }

                if (Equals(currentPoint, convexHull[0]))
                {
                    convexHull.RemoveAt(convexHull.Count - 1);
                    break;
                }

                ++counter;
            }

            return convexHull;
        }

        public static List<FastVector3Int> BuildConvexHull(
            this IEnumerable<FastVector3Int> pointsSet,
            Grid grid,
            IRandom random = null,
            bool includeColinearPoints = false
        )
        {
            using PooledResource<List<FastVector3Int>> pointsResource =
                Buffers<FastVector3Int>.List.Get(out List<FastVector3Int> points);
            foreach (FastVector3Int point in pointsSet)
            {
                points.Add(point);
            }

            if (points.Count <= 3)
            {
                return new List<FastVector3Int>(points);
            }

            random ??= PRNG.Instance;

            Vector2 CellToWorld(FastVector3Int position) => grid.CellToWorld(position);

            FastVector3Int startPoint = points[0];
            Vector2 startPointWorldPosition = CellToWorld(startPoint);
            for (int i = 1; i < points.Count; ++i)
            {
                FastVector3Int testPoint = points[i];
                Vector2 testPointWorldPosition = CellToWorld(testPoint);
                if (
                    testPointWorldPosition.x < startPointWorldPosition.x
                    || (
                        Mathf.Approximately(testPointWorldPosition.x, startPointWorldPosition.x)
                        && testPointWorldPosition.y < startPointWorldPosition.y
                    )
                )
                {
                    startPoint = testPoint;
                    startPointWorldPosition = testPointWorldPosition;
                }
            }

            List<FastVector3Int> convexHull = new(points.Count);
            convexHull.Add(startPoint);
            _ = points.Remove(startPoint);
            FastVector3Int currentPoint = convexHull[0];
            using PooledResource<List<FastVector3Int>> colinearPointsResource =
                Buffers<FastVector3Int>.List.Get();
            List<FastVector3Int> colinearPoints = colinearPointsResource.resource;
            int counter = 0;
            while (true)
            {
                if (counter == 2)
                {
                    points.Add(convexHull[0]);
                }

                if (points.Count <= 0)
                {
                    return convexHull;
                }

                FastVector3Int nextPoint = random.NextOf(points);
                Vector2 currentPointWorldPosition = CellToWorld(currentPoint);
                Vector2 nextPointWorldPosition = CellToWorld(nextPoint);
                foreach (FastVector3Int point in points)
                {
                    if (point.Equals(nextPoint))
                    {
                        continue;
                    }

                    float relation = Geometry.IsAPointLeftOfVectorOrOnTheLine(
                        currentPointWorldPosition,
                        nextPointWorldPosition,
                        CellToWorld(point)
                    );
                    if (Mathf.Approximately(relation, 0))
                    {
                        colinearPoints.Add(point);
                    }
                    else if (relation < 0)
                    {
                        nextPoint = point;
                        nextPointWorldPosition = CellToWorld(nextPoint);
                        colinearPoints.Clear();
                    }
                }

                if (0 < colinearPoints.Count)
                {
                    colinearPoints.Add(nextPoint);
                    SortByDistanceAscending(colinearPoints, grid, currentPointWorldPosition);

                    if (includeColinearPoints)
                    {
                        convexHull.AddRange(colinearPoints);
                    }
                    else
                    {
                        convexHull.Add(colinearPoints[^1]);
                        _ = points.Remove(colinearPoints[^1]);
                    }

                    currentPoint = colinearPoints[^1];
                    RemovePoints(points, colinearPoints);
                    colinearPoints.Clear();
                }
                else
                {
                    convexHull.Add(nextPoint);
                    _ = points.Remove(nextPoint);
                    currentPoint = nextPoint;
                }

                if (currentPoint.Equals(convexHull[0]))
                {
                    convexHull.RemoveAt(convexHull.Count - 1);
                    break;
                }

                ++counter;
            }

            return convexHull;
        }

        public static bool IsConvexHullInsideConvexHull(
            this List<FastVector3Int> convexHull,
            Grid grid,
            List<FastVector3Int> maybeInside
        )
        {
            int orientation = DetermineConvexHullOrientation(convexHull, grid);
            foreach (FastVector3Int point in maybeInside)
            {
                if (!IsPointInsideConvexHull(convexHull, grid, point, orientation))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsPointInsideConvexHull(
            this List<Vector3Int> convexHull,
            Grid grid,
            Vector3Int point
        )
        {
            int orientation = DetermineConvexHullOrientation(convexHull, grid);
            return IsPointInsideConvexHull(convexHull, grid, point, orientation);
        }

        public static bool IsPointInsideConvexHull(
            this List<FastVector3Int> convexHull,
            Grid grid,
            FastVector3Int point
        )
        {
            int orientation = DetermineConvexHullOrientation(convexHull, grid);
            return IsPointInsideConvexHull(convexHull, grid, point, orientation);
        }

        public static bool IsConvexHullInsideConvexHull(
            this List<Vector3Int> convexHull,
            Grid grid,
            List<Vector3Int> maybeInside
        )
        {
            int orientation = DetermineConvexHullOrientation(convexHull, grid);
            foreach (Vector3Int point in maybeInside)
            {
                if (!IsPointInsideConvexHull(convexHull, grid, point, orientation))
                {
                    return false;
                }
            }

            return true;
        }

        private static int DetermineConvexHullOrientation(
            IReadOnlyList<FastVector3Int> convexHull,
            Grid grid
        )
        {
            if (convexHull == null || convexHull.Count < 3)
            {
                return 0;
            }

            double twiceArea = 0d;
            Vector3 previousWorld = grid.CellToWorld(convexHull[^1]);
            Vector2 previous = new(previousWorld.x, previousWorld.y);
            for (int i = 0; i < convexHull.Count; ++i)
            {
                Vector3 currentWorld = grid.CellToWorld(convexHull[i]);
                Vector2 current = new(currentWorld.x, currentWorld.y);
                twiceArea += (previous.x * current.y) - (current.x * previous.y);
                previous = current;
            }

            if (Math.Abs(twiceArea) <= ConvexHullOrientationEpsilon)
            {
                return 0;
            }

            return twiceArea > 0d ? 1 : -1;
        }

        private static int DetermineConvexHullOrientation(
            IReadOnlyList<Vector3Int> convexHull,
            Grid grid
        )
        {
            if (convexHull == null || convexHull.Count < 3)
            {
                return 0;
            }

            double twiceArea = 0d;
            Vector3 previousWorld = grid.CellToWorld(convexHull[^1]);
            Vector2 previous = new(previousWorld.x, previousWorld.y);
            for (int i = 0; i < convexHull.Count; ++i)
            {
                Vector3 currentWorld = grid.CellToWorld(convexHull[i]);
                Vector2 current = new(currentWorld.x, currentWorld.y);
                twiceArea += (previous.x * current.y) - (current.x * previous.y);
                previous = current;
            }

            if (Math.Abs(twiceArea) <= ConvexHullOrientationEpsilon)
            {
                return 0;
            }

            return twiceArea > 0d ? 1 : -1;
        }

        private static bool IsPointInsideConvexHull(
            List<FastVector3Int> convexHull,
            Grid grid,
            FastVector3Int point,
            int expectedSide
        )
        {
            if (convexHull == null || convexHull.Count == 0)
            {
                return true;
            }

            int requiredSide = expectedSide;
            Vector3 pointWorld = grid.CellToWorld(point);
            for (int i = 0; i < convexHull.Count; ++i)
            {
                FastVector3Int lhs = convexHull[i];
                FastVector3Int rhs = convexHull[(i + 1) % convexHull.Count];
                float relation = Geometry.IsAPointLeftOfVectorOrOnTheLine(
                    grid.CellToWorld(lhs),
                    grid.CellToWorld(rhs),
                    pointWorld
                );

                if (Mathf.Abs(relation) <= ConvexHullRelationEpsilon)
                {
                    continue;
                }

                int side = requiredSide;
                if (side == 0)
                {
                    side = relation > 0f ? 1 : -1;
                    requiredSide = side;
                }

                if (relation * side < -ConvexHullRelationEpsilon)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsPointInsideConvexHull(
            List<Vector3Int> convexHull,
            Grid grid,
            Vector3Int point,
            int expectedSide
        )
        {
            if (convexHull == null || convexHull.Count == 0)
            {
                return true;
            }

            int requiredSide = expectedSide;
            Vector3 pointWorld = grid.CellToWorld(point);
            for (int i = 0; i < convexHull.Count; ++i)
            {
                Vector3Int lhs = convexHull[i];
                Vector3Int rhs = convexHull[(i + 1) % convexHull.Count];
                float relation = Geometry.IsAPointLeftOfVectorOrOnTheLine(
                    grid.CellToWorld(lhs),
                    grid.CellToWorld(rhs),
                    pointWorld
                );

                if (Mathf.Abs(relation) <= ConvexHullRelationEpsilon)
                {
                    continue;
                }

                int side = requiredSide;
                if (side == 0)
                {
                    side = relation > 0f ? 1 : -1;
                    requiredSide = side;
                }

                if (relation * side < -ConvexHullRelationEpsilon)
                {
                    return false;
                }
            }

            return true;
        }

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

        public static List<FastVector3Int> BuildConcaveHull3(
            this IReadOnlyCollection<FastVector3Int> gridPositions,
            Grid grid,
            IRandom random = null,
            int bucketSize = 40,
            float angleThreshold = 90f
        )
        {
            using PooledResource<List<FastVector3Int>> originalGridPositionsBuffer =
                Buffers<FastVector3Int>.List.Get(out List<FastVector3Int> originalGridPositions);
            originalGridPositions.AddRange(gridPositions);

            List<FastVector3Int> convexHull = gridPositions.BuildConvexHull(grid, random);
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
                    continue;
                }
                current = concaveHullEdges[nextIndex];
                concaveHullEdges.RemoveAtSwapBack(nextIndex);
                concaveHull.Add(current.from);
            }

            return concaveHull;

            Vector2 CellToWorld(FastVector3Int cell) => grid.CellToWorld(cell);
        }

        // https://www.researchgate.net/publication/220868874_Concave_hull_A_k-nearest_neighbours_approach_for_the_computation_of_the_region_occupied_by_a_set_of_points

        public static List<FastVector3Int> BuildConcaveHull2(
            this IReadOnlyCollection<FastVector3Int> gridPositions,
            Grid grid,
            IRandom random = null,
            int nearestNeighbors = 3
        )
        {
            const int minimumNearestNeighbors = 3;
            nearestNeighbors = Math.Max(minimumNearestNeighbors, nearestNeighbors);
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
            int maximumNearestNeighbors = dataSet.Count;
            if (dataSet.Count <= 3)
            {
                return new List<FastVector3Int>(dataSet);
            }

            nearestNeighbors = Math.Min(dataSet.Count, nearestNeighbors);

            FastVector3Int? maybeFirst = null;
            float lowestY = float.MaxValue;
            foreach (FastVector3Int gridPosition in dataSet)
            {
                float candidateY = grid.CellToWorld(gridPosition).y;
                if (maybeFirst == null || candidateY < lowestY)
                {
                    maybeFirst = gridPosition;
                    lowestY = candidateY;
                }
            }

            if (maybeFirst == null)
            {
                return new List<FastVector3Int>(dataSet);
            }

            FastVector3Int first = maybeFirst.Value;
            List<FastVector3Int> hull = new(dataSet.Count) { first };
            int step = 2;
            float previousAngle = 0f;
            FastVector3Int current = first;
            _ = dataSet.Remove(current);

            using PooledResource<List<FastVector3Int>> clockwisePointsResource =
                Buffers<FastVector3Int>.List.Get();
            List<FastVector3Int> clockwisePoints = clockwisePointsResource.resource;

            while (0 < dataSet.Count)
            {
                if (step == 5)
                {
                    dataSet.Add(first);
                }

                FindNearestNeighborsAndPutInClockwisePoints();
                SortByRightHandTurn(clockwisePoints, grid, current, previousAngle);

                bool intersects = true;
                int i = -1;
                while (intersects && i < clockwisePoints.Count - 1)
                {
                    ++i;

                    FastVector3Int indexedPoint = clockwisePoints[i];
                    int lastPoint = indexedPoint == first ? 1 : 0;
                    int j = 2;
                    intersects = false;
                    Vector2 lhsTo = grid.CellToWorld(indexedPoint);
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
                    for (i = dataSet.Count - 1; 0 <= i; --i)
                    {
                        if (!IsPositionInside(hull, dataSet[i], grid))
                        {
                            if (nearestNeighbors >= maximumNearestNeighbors)
                            {
                                return gridPositions.BuildConvexHull(grid, random);
                            }

                            return BuildConcaveHull2(
                                gridPositions,
                                grid,
                                random,
                                nearestNeighbors + 1
                            );
                        }
                    }

                    return hull;
                }

                current = clockwisePoints[i];
                if (current != first)
                {
                    hull.Add(current);
                }
                else
                {
                    break;
                }

                int currentIndex = dataSet.IndexOf(current);
                if (0 <= currentIndex)
                {
                    dataSet.RemoveAtSwapBack(currentIndex);
                }

                previousAngle = CalculateAngle(
                    grid.CellToWorld(hull[step - 1]),
                    grid.CellToWorld(hull[step - 2])
                );
                ++step;
            }

            for (int i = dataSet.Count - 1; 0 <= i; --i)
            {
                if (!IsPositionInside(hull, dataSet[i], grid))
                {
                    if (nearestNeighbors >= maximumNearestNeighbors)
                    {
                        return gridPositions.BuildConvexHull(grid, random);
                    }

                    return BuildConcaveHull2(gridPositions, grid, random, nearestNeighbors + 1);
                }
            }

            return hull;

            // TODO: Remove allocations
            void FindNearestNeighborsAndPutInClockwisePoints()
            {
                clockwisePoints.Clear();
                clockwisePoints.AddRange(dataSet);
                Vector2 currentPointWorld = grid.CellToWorld(current);
                SortByDistanceAscending(clockwisePoints, grid, currentPointWorld);
                if (nearestNeighbors < clockwisePoints.Count)
                {
                    clockwisePoints.RemoveRange(
                        nearestNeighbors,
                        clockwisePoints.Count - nearestNeighbors
                    );
                }
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

        private static float CalculateAngle(Vector2 lhs, Vector2 rhs)
        {
            return Mathf.Atan2(rhs.y - lhs.y, rhs.x - lhs.x);
        }

        private static float AngleDifference(float lhsAngle, float rhsAngle)
        {
            if (0 < lhsAngle && 0 <= rhsAngle && rhsAngle < lhsAngle)
            {
                return Math.Abs(lhsAngle - rhsAngle);
            }

            if (0 <= lhsAngle && 0 < rhsAngle && lhsAngle < rhsAngle)
            {
                return 2 * Mathf.PI + lhsAngle - rhsAngle;
            }

            if (lhsAngle < 0 && rhsAngle <= 0 && lhsAngle < rhsAngle)
            {
                return 2 * Mathf.PI + lhsAngle + Math.Abs(rhsAngle);
            }

            if (lhsAngle <= 0 && rhsAngle < 0 && rhsAngle < lhsAngle)
            {
                return Math.Abs(lhsAngle - rhsAngle);
            }

            if (lhsAngle <= 0 && 0 < rhsAngle)
            {
                return 2 * Mathf.PI + lhsAngle - rhsAngle;
            }

            if (0 <= lhsAngle && rhsAngle <= 0)
            {
                return lhsAngle + Math.Abs(rhsAngle);
            }

            return 0f;
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

        public static List<FastVector3Int> BuildConcaveHull(
            this IEnumerable<FastVector3Int> gridPositions,
            Grid grid,
            IRandom random = null,
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

            List<FastVector3Int> convexHull = originalGridPositions.BuildConvexHull(grid, random);
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
            }

            return concaveHull;
        }

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

        // https://www.geeksforgeeks.org/how-to-check-if-a-given-point-lies-inside-a-polygon/#

        /// <summary>
        ///     Returns true if a line segment 'lhsFrom->lhsTo' intersects the line segment
        ///     'rhsFrom->rhsTo'
        /// </summary>
        /// <param name="lhsFrom">LineSegmentA start point.</param>
        /// <param name="lhsTo">LineSegmentA end point.</param>
        /// <param name="rhsFrom">LineSegmentB start point.</param>
        /// <param name="rhsTo">LineSegmentB end point.</param>
        /// <returns>True if the line segments intersect.</returns>
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
        ///     Given three colinear points p, q, r, returns whether the
        ///     point q lines on the line segment pr.
        /// </summary>
        /// <param name="p">Beginning of line segment.</param>
        /// <param name="q">Check if on line segment.</param>
        /// <param name="r">End of line segment.</param>
        /// <returns>True if q lies on the line segment pr.</returns>
        public static bool LiesOnSegment(Vector2 p, Vector2 q, Vector2 r)
        {
            return q.x <= Math.Max(p.x, r.x)
                && Math.Min(p.x, r.x) <= q.x
                && q.y <= Math.Max(p.y, r.y)
                && Math.Min(p.y, r.y) <= q.y;
        }

        public enum OrientationType
        {
            Colinear = 0,
            Clockwise = 1,
            Counterclockwise = 2,
        }

        /// <summary>
        ///     Finds the orientation of an ordered triplet (p, q, r).
        /// </summary>
        /// <param name="p">Triplet element 1.</param>
        /// <param name="q">Triplet element 2.</param>
        /// <param name="r">Triplet element 3.</param>
        /// <returns>The orientation of the triplet</returns>
        public static OrientationType Orientation(Vector2 p, Vector2 q, Vector2 r)
        {
            float value = (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);
            if (Mathf.Approximately(value, 0))
            {
                return OrientationType.Colinear;
            }

            return 0 < value ? OrientationType.Clockwise : OrientationType.Counterclockwise;
        }

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

        public static bool FastIntersects(this Bounds bounds, Bounds other)
        {
            Vector3 boundsMin = bounds.min;
            Vector3 otherMax = other.max;
            if (otherMax.x < boundsMin.x || otherMax.y < boundsMin.y || otherMax.z < boundsMin.z)
            {
                return false;
            }

            Vector3 boundsMax = bounds.max;
            Vector3 otherMin = other.min;
            return boundsMax.x >= otherMin.x
                && boundsMax.y >= otherMin.y
                && boundsMax.z >= otherMin.z;
        }

        public static bool FastContains2D(this BoundsInt bounds, FastVector3Int position)
        {
            return position.x >= bounds.xMin
                && position.y >= bounds.yMin
                && position.x < bounds.xMax
                && position.y < bounds.yMax;
        }

        public static bool FastIntersects2D(this BoundsInt bounds, BoundsInt other)
        {
            // Zero-size bounds cannot intersect
            if (bounds.size.x <= 0 || bounds.size.y <= 0 || other.size.x <= 0 || other.size.y <= 0)
            {
                return false;
            }

            if (other.xMax < bounds.xMin || other.yMax < bounds.yMin)
            {
                return false;
            }

            return bounds.xMax >= other.xMin && bounds.yMax >= other.yMin;
        }

        public static bool FastContains2D(this Bounds bounds, Vector2 position)
        {
            Vector3 min = bounds.min;
            if (position.x < min.x || position.y < bounds.min.y)
            {
                return false;
            }
            Vector3 max = bounds.max;
            return position.x <= max.x && position.y <= max.y;
        }

        public static bool FastContains2D(this Bounds bounds, Bounds other)
        {
            Vector3 boundsMin = bounds.min;
            Vector3 otherMin = other.min;
            if (otherMin.x < boundsMin.x || otherMin.y < boundsMin.y)
            {
                return false;
            }

            Vector3 boundsMax = bounds.max;
            Vector3 otherMax = other.max;
            return otherMax.x <= boundsMax.x && otherMax.y <= boundsMax.y;
        }

        public static bool FastIntersects2D(this Bounds bounds, Bounds other)
        {
            Vector3 boundsMin = bounds.min;
            Vector3 otherMax = other.max;
            if (otherMax.x < boundsMin.x || otherMax.y < boundsMin.y)
            {
                return false;
            }

            Vector3 boundsMax = bounds.max;
            Vector3 otherMin = other.min;
            return boundsMax.x >= otherMin.x && boundsMax.y >= otherMin.y;
        }

        public static bool Overlaps2D(this Bounds bounds, Bounds other)
        {
            Vector3 boundsMin = bounds.min;
            Vector3 otherMax = other.max;
            if (otherMax.x < boundsMin.x || otherMax.y < boundsMin.y)
            {
                return false;
            }

            Vector3 boundsMax = bounds.max;
            Vector3 otherMin = other.min;
            return boundsMax.x >= otherMin.x && boundsMax.y >= otherMin.y;
        }

        public static BoundsInt WithPadding(this BoundsInt bounds, int xPadding, int yPadding)
        {
            Vector3Int size = bounds.size;
            return new BoundsInt(
                bounds.xMin - xPadding,
                bounds.yMin - yPadding,
                bounds.zMin,
                size.x + 2 * xPadding,
                size.y + 2 * yPadding,
                size.z
            );
        }

        public static void SetColors(this Slider slider, Color color)
        {
            ColorBlock block = slider.colors;

            block.normalColor = color;
            block.highlightedColor = color;
            block.pressedColor = color;
            block.selectedColor = color;
            block.disabledColor = color;

            slider.colors = block;
        }

        public static void SetLeft(this RectTransform rt, float left)
        {
            rt.offsetMin = new Vector2(left, rt.offsetMin.y);
        }

        public static void SetRight(this RectTransform rt, float right)
        {
            rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
        }

        public static void SetTop(this RectTransform rt, float top)
        {
            rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
        }

        public static void SetBottom(this RectTransform rt, float bottom)
        {
            rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
        }

        public static IEnumerable<FastVector3Int> AllFastPositionsWithin(this BoundsInt bounds)
        {
            Vector3Int min = bounds.min;
            Vector3Int max = bounds.max;
            for (int x = min.x; x < max.x; ++x)
            {
                for (int y = min.y; y < max.y; ++y)
                {
                    for (int z = min.z; z < max.z; ++z)
                    {
                        yield return new FastVector3Int(x, y, z);
                    }
                }
            }
        }

        public static List<FastVector3Int> AllFastPositionsWithin(
            this BoundsInt bounds,
            List<FastVector3Int> buffer
        )
        {
            buffer.Clear();
            Vector3Int min = bounds.min;
            Vector3Int max = bounds.max;
            for (int x = min.x; x < max.x; ++x)
            {
                for (int y = min.y; y < max.y; ++y)
                {
                    for (int z = min.z; z < max.z; ++z)
                    {
                        FastVector3Int position = new(x, y, z);
                        buffer.Add(position);
                    }
                }
            }

            return buffer;
        }

        public static bool Contains(this BoundsInt bounds, FastVector3Int position)
        {
            return bounds.Contains(position);
        }

        public static bool IsOnEdge2D(this FastVector3Int position, BoundsInt bounds)
        {
            if (bounds.xMin == position.x || bounds.xMax - 1 == position.x)
            {
                return bounds.yMin <= position.y && position.y < bounds.yMax;
            }

            if (bounds.yMin == position.y || bounds.yMax - 1 == position.y)
            {
                return bounds.xMin <= position.x && position.x < bounds.xMax;
            }

            return false;
        }

#if UNITY_EDITOR
        public static IEnumerable<Sprite> GetSpritesFromClip(this AnimationClip clip)
        {
            if (clip == null)
            {
                yield break;
            }

            foreach (
                EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip)
            )
            {
                ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(
                    clip,
                    binding
                );
                foreach (ObjectReferenceKeyframe frame in keyframes)
                {
                    if (frame.value is Sprite sprite)
                    {
                        yield return sprite;
                    }
                }
            }
        }
#endif

        public static bool IsDontDestroyOnLoad(this GameObject gameObjectToCheck)
        {
            if (gameObjectToCheck == null)
            {
                return false;
            }

            return string.Equals(
                gameObjectToCheck.scene.name,
                "DontDestroyOnLoad",
                StringComparison.Ordinal
            );
        }

        public static bool IsCircleFullyContained(
            this Collider2D targetCollider,
            Vector2 center,
            float radius,
            int sampleCount = 16
        )
        {
            for (int i = 0; i < sampleCount; ++i)
            {
                float angle = 2 * Mathf.PI / sampleCount * i;
                Vector2 pointOnCircle =
                    center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                if (!targetCollider.OverlapPoint(pointOnCircle))
                {
                    return false;
                }
            }
            return true;
        }

        public static void Invert(this PolygonCollider2D col, Rect outerRect)
        {
            int originalCount = col.pathCount;
            if (originalCount == 0)
            {
                return;
            }

            using PooledResource<Vector2[][]> originalBuffer = WallstopArrayPool<Vector2[]>.Get(
                originalCount,
                out Vector2[][] originals
            );
            using PooledResource<List<PooledResource<Vector2[]>>> pathBuffer = Buffers<
                PooledResource<Vector2[]>
            >.List.Get(out List<PooledResource<Vector2[]>> paths);

            for (int i = 0; i < originalCount; i++)
            {
                Vector2[] path = col.GetPath(i);
                PooledResource<Vector2[]> buffer = WallstopArrayPool<Vector2>.Get(
                    path.Length,
                    out Vector2[] points
                );
                paths.Add(buffer);
                Array.Copy(path, points, path.Length);
                originals[i] = points;
            }

            Vector2[] outerPath =
            {
                new(outerRect.xMin, outerRect.yMin),
                new(outerRect.xMin, outerRect.yMax),
                new(outerRect.xMax, outerRect.yMax),
                new(outerRect.xMax, outerRect.yMin),
            };

            col.pathCount = originalCount + 1;
            col.SetPath(0, outerPath);

            for (int i = 0; i < originalCount; ++i)
            {
                Vector2[] hole = originals[i];
                Array.Reverse(hole);
                col.SetPath(i + 1, hole);
            }

            foreach (PooledResource<Vector2[]> path in paths)
            {
                path.Dispose();
            }
        }
    }
}
