namespace UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DataStructure;
    using DataStructure.Adapters;
    using Helper;
    using Random;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;
    using Utils;

    public static class UnityExtensions
    {
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
            Vector3[] fourCorners = new Vector3[4];
            transform.GetWorldCorners(fourCorners);

            float[] xValues = fourCorners.Select(vector => vector.x).ToArray();
            float[] yValues = fourCorners.Select(vector => vector.y).ToArray();
            float minX = Mathf.Min(xValues);
            float maxX = Mathf.Max(xValues);
            float minY = Mathf.Min(yValues);
            float maxY = Mathf.Max(yValues);

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        public static Bounds OrthographicBounds(this Camera camera)
        {
            float screenAspect = (float)Screen.width / Screen.height;
            float cameraHeight = camera.orthographicSize * 2;
            Bounds bounds = new((Vector2) camera.transform.position, new Vector3(cameraHeight * screenAspect, cameraHeight, 1));
            return bounds;
        }

        public static string ToJsonString(this Vector3 vector)
        {
            return $"{{{vector.x}, {vector.y}, {vector.z}}}";
        }

        public static string ToJsonString(this Vector2 vector)
        {
            return $"{{{vector.x}, {vector.y}}}";
        }

        public static bool IsNoise(this Vector2 inputVector)
        {
            return Mathf.Abs(inputVector.x) <= 0.2f && Mathf.Abs(inputVector.y) <= 0.2f;
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

        public static BoundsInt? GetBounds(this IEnumerable<Vector3Int> positions)
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
            return new BoundsInt(xMin, yMin, zMin, (xMax - xMin) + 1, (yMax - yMin) + 1, (zMax - zMin) + 1);
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
            return new BoundsInt(xMin, yMin, zMin, (xMax - xMin) + 1, (yMax - yMin) + 1, (zMax - zMin) + 1);
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

        // https://www.habrador.com/tutorials/math/8-convex-hull/
        public static List<Vector3Int> BuildConvexHull(this IEnumerable<Vector3Int> pointsSet, Grid grid, IRandom random = null, bool includeColinearPoints = true)
        {
            List<Vector3Int> points = pointsSet.ToList();
            if (points.Count <= 3)
            {
                return points;
            }

            random ??= PcgRandom.Instance;

            Vector2 CellToWorld(Vector3Int position) => grid.CellToWorld(position);

            Vector3Int startPoint = points[0];
            Vector2 startPointWorldPosition = CellToWorld(startPoint);
            for (int i = 1; i < points.Count; ++i)
            {
                Vector3Int testPoint = points[i];
                Vector2 testPointWorldPosition = CellToWorld(testPoint);
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (testPointWorldPosition.x < startPointWorldPosition.x || (Mathf.Approximately(testPointWorldPosition.x, startPointWorldPosition.x) && testPointWorldPosition.y < startPointWorldPosition.y))
                {
                    startPoint = testPoint;
                    startPointWorldPosition = testPointWorldPosition;
                }
            }

            List<Vector3Int> convexHull = new() { startPoint };
            _ = points.Remove(startPoint);
            Vector3Int currentPoint = convexHull[0];
            List<Vector3Int> colinearPoints = new();
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

                Vector3Int nextPoint = random.Next(points);
                Vector2 currentPointWorldPosition = CellToWorld(currentPoint);
                Vector2 nextPointWorldPosition = CellToWorld(nextPoint);
                foreach (Vector3Int point in points)
                {
                    if (Equals(point, nextPoint))
                    {
                        continue;
                    }

                    float relation = Geometry.IsAPointLeftOfVectorOrOnTheLine(currentPointWorldPosition, nextPointWorldPosition, CellToWorld(point));
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
                    colinearPoints.Sort((lhs, rhs) =>
                            (CellToWorld(lhs) - currentPointWorldPosition).sqrMagnitude
                        .CompareTo(
                            (CellToWorld(rhs) - currentPointWorldPosition).sqrMagnitude));

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
                    _ = points.RemoveAll(colinearPoints.Contains);
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

        public static List<FastVector3Int> BuildConvexHull(this IEnumerable<FastVector3Int> pointsSet, Grid grid, IRandom random = null, bool includeColinearPoints = false)
        {
            List<FastVector3Int> points = pointsSet.ToList();
            if (points.Count <= 3)
            {
                return points;
            }
            
            random ??= PcgRandom.Instance;

            Vector2 CellToWorld(FastVector3Int position) => grid.CellToWorld(position);

            FastVector3Int startPoint = points[0];
            Vector2 startPointWorldPosition = CellToWorld(startPoint);
            for (int i = 1; i < points.Count; ++i)
            {
                FastVector3Int testPoint = points[i];
                Vector2 testPointWorldPosition = CellToWorld(testPoint);
                if (testPointWorldPosition.x < startPointWorldPosition.x || (Mathf.Approximately(testPointWorldPosition.x, startPointWorldPosition.x) && testPointWorldPosition.y < startPointWorldPosition.y))
                {
                    startPoint = testPoint;
                    startPointWorldPosition = testPointWorldPosition;
                }
            }

            List<FastVector3Int> convexHull = new() { startPoint };
            _ = points.Remove(startPoint);
            FastVector3Int currentPoint = convexHull[0];
            List<FastVector3Int> colinearPoints = new();
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

                FastVector3Int nextPoint = random.Next(points);
                Vector2 currentPointWorldPosition = CellToWorld(currentPoint);
                Vector2 nextPointWorldPosition = CellToWorld(nextPoint);
                foreach (FastVector3Int point in points)
                {
                    if (point.Equals(nextPoint))
                    {
                        continue;
                    }

                    float relation = Geometry.IsAPointLeftOfVectorOrOnTheLine(currentPointWorldPosition, nextPointWorldPosition, CellToWorld(point));
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
                    colinearPoints.Sort((lhs, rhs) =>
                            (CellToWorld(lhs) - currentPointWorldPosition).sqrMagnitude
                        .CompareTo(
                            (CellToWorld(rhs) - currentPointWorldPosition).sqrMagnitude));

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
                    _ = points.RemoveAll(colinearPoints.Contains);
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

        
        public static bool IsConvexHullInsideConvexHull(this List<FastVector3Int> convexHull, Grid grid, List<FastVector3Int> maybeInside)
        {
            foreach (FastVector3Int point in maybeInside)
            {
                if (!IsPointInsideConvexHull(convexHull, grid, point))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsPointInsideConvexHull(this List<Vector3Int> convexHull, Grid grid, Vector3Int point)
        {
            for (int i = 0; i < convexHull.Count; ++i)
            {
                Vector3Int lhs = convexHull[i];
                Vector3Int rhs = convexHull[(i + 1) % convexHull.Count];
                float relation = Geometry.IsAPointLeftOfVectorOrOnTheLine(grid.CellToWorld(lhs), grid.CellToWorld(rhs), grid.CellToWorld(point));
                if (relation < 0)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsPointInsideConvexHull(this List<FastVector3Int> convexHull, Grid grid, FastVector3Int point)
        {
            for (int i = 0; i < convexHull.Count; ++i)
            {
                FastVector3Int lhs = convexHull[i];
                FastVector3Int rhs = convexHull[(i + 1) % convexHull.Count];
                float relation = Geometry.IsAPointLeftOfVectorOrOnTheLine(grid.CellToWorld(lhs), grid.CellToWorld(rhs), grid.CellToWorld(point));
                if (relation < 0)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsConvexHullInsideConvexHull(this List<Vector3Int> convexHull, Grid grid, List<Vector3Int> maybeInside)
        {
            foreach (Vector3Int point in maybeInside)
            {
                if (!IsPointInsideConvexHull(convexHull, grid, point))
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
                return UnityExtensions.Intersects(fromWorld, toWorld, other.fromWorld, other.toWorld);
            }

            public float LargestAngle(FastVector3Int point)
            {
                Vector2 worldPoint = _grid.CellToWorld(point);
                float angleFrom = Vector2.Angle((toWorld - fromWorld), (worldPoint - fromWorld));
                float angleTo = Vector2.Angle((fromWorld - toWorld), (worldPoint - toWorld));
                return Math.Max(angleFrom, angleTo);
            }
        }

        private sealed class ConcaveHullComparer : IComparer<HullEdge>
        {
            public static readonly ConcaveHullComparer Instance = new();

            private ConcaveHullComparer()
            {

            }

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

        public static List<FastVector3Int> BuildConcaveHull3(this IReadOnlyCollection<FastVector3Int> gridPositions, Grid grid, IRandom random = null, int bucketSize = 40, float angleThreshold = 90f)
        {
            List<FastVector3Int> convexHull = gridPositions.BuildConvexHull(grid, random);
            List<HullEdge> concaveHullEdges = new();

            SortedSet<HullEdge> data = new(ConcaveHullComparer.Instance);
            for (int i = 0; i < convexHull.Count; ++i)
            {
                FastVector3Int lhs = convexHull[i];
                FastVector3Int rhs = convexHull[(i + 1) % convexHull.Count];
                HullEdge edge = new(lhs, rhs, grid);
                _ = data.Add(edge);
            }
            
            HashSet<FastVector3Int> remainingPoints = gridPositions.ToHashSet();
            remainingPoints.ExceptWith(convexHull);

            Vector2 CellToWorld(FastVector3Int cell) => grid.CellToWorld(cell);

            Bounds? maybeBounds = gridPositions.Select(CellToWorld).GetBounds();
            if (maybeBounds == null)
            {
                throw new ArgumentException(nameof(gridPositions));
            }

            QuadTree<FastVector3Int> NewQuadTree() => new(gridPositions, CellToWorld, maybeBounds.Value, bucketSize: bucketSize);

            QuadTree<FastVector3Int> quadTree = NewQuadTree();
            List<FastVector3Int> neighbors = Buffers<FastVector3Int>.List;
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
                
                    localMaximumDistance = Math.Max(localMaximumDistance, (CellToWorld(neighbor) - edgeCenter).sqrMagnitude);
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
                int nextIndex = concaveHullEdges.FindIndex(edge => edge.from == to);
                current = concaveHullEdges[nextIndex];
                concaveHullEdges.RemoveAtSwapBack(nextIndex);
                concaveHull.Add(current.from);
            }

            return concaveHull;
        }

        // https://www.researchgate.net/publication/220868874_Concave_hull_A_k-nearest_neighbours_approach_for_the_computation_of_the_region_occupied_by_a_set_of_points

        public static List<FastVector3Int> BuildConcaveHull2(this IReadOnlyCollection<FastVector3Int> gridPositions, Grid grid, IRandom random = null, int nearestNeighbors = 3)
        {
            const int minimumNearestNeighbors = 3;
            nearestNeighbors = Math.Max(minimumNearestNeighbors, nearestNeighbors);
            List<FastVector3Int> dataSet = gridPositions.Distinct().ToList();
            if (dataSet.Count <= 3)
            {
                return dataSet;
            }
            
            nearestNeighbors = Math.Min(dataSet.Count, nearestNeighbors);

            IComparer<FastVector3Int> comparison = Comparer<FastVector3Int>.Create(
                (lhs, rhs) =>
                    grid.CellToWorld(lhs).y.CompareTo(grid.CellToWorld(rhs).y));

            FastVector3Int? maybeFirst = null;
            foreach (FastVector3Int gridPosition in dataSet)
            {
                if (maybeFirst == null || comparison.Compare(gridPosition, maybeFirst.Value) < 0)
                {
                    maybeFirst = gridPosition;
                }
            }

            if (maybeFirst == null)
            {
                return dataSet;
            }

            FastVector3Int first = maybeFirst.Value;
            List<FastVector3Int> hull = new(dataSet.Count) { first };
            int step = 2;
            float previousAngle = 0f;
            FastVector3Int current = first;
            _ = dataSet.Remove(current);

            float CalculateAngle(Vector2 lhs, Vector2 rhs)
            {
                return Mathf.Atan2(rhs.y - lhs.y, rhs.x - lhs.x);
            }

            // https://github.com/merowech/java-concave-hull/blob/master/ConcaveHull.java
            float AngleDifference(float lhsAngle, float rhsAngle)
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

            // Order by descending right hand turns
            int RightHandTurnComparison(FastVector3Int lhs, FastVector3Int rhs)
            {
                // TODO: I think this is fucked
                Vector2 currentPoint = grid.CellToWorld(current);
                Vector2 lhsPoint = grid.CellToWorld(lhs);
                Vector2 rhsPoint = grid.CellToWorld(rhs);

                float lhsAngle = AngleDifference(previousAngle, CalculateAngle(currentPoint, lhsPoint));
                float rhsAngle = AngleDifference(previousAngle, CalculateAngle(currentPoint, rhsPoint));
                return rhsAngle.CompareTo(lhsAngle);
            }

            List<FastVector3Int> clockwisePoints = Buffers<FastVector3Int>.List;
            void FindNearestNeighborsAndPutInClockwisePoints()
            {
                clockwisePoints.Clear();
                clockwisePoints.AddRange(dataSet);
                Vector2 currentPoint = grid.CellToWorld(current);
                clockwisePoints.Sort(
                    (lhs, rhs) =>
                    {
                        Vector2 lhsPoint = grid.CellToWorld(lhs);
                        Vector2 rhsPoint = grid.CellToWorld(rhs);
                        return (lhsPoint - currentPoint).sqrMagnitude.CompareTo((rhsPoint - currentPoint).sqrMagnitude);
                    });
                if (nearestNeighbors < clockwisePoints.Count)
                {
                    clockwisePoints.RemoveRange(nearestNeighbors, clockwisePoints.Count - nearestNeighbors);
                }
            }

            while (0 < dataSet.Count)
            {
                if (step == 5)
                {
                    dataSet.Add(first);
                }

                FindNearestNeighborsAndPutInClockwisePoints();
                clockwisePoints.Sort(RightHandTurnComparison);

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
                            return BuildConcaveHull2(gridPositions, grid, random, nearestNeighbors + 1);
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

                previousAngle = CalculateAngle(grid.CellToWorld(hull[step - 1]), grid.CellToWorld(hull[step - 2]));
                ++step;
            }

            for (int i = dataSet.Count - 1; 0 <= i; --i)
            {
                if (!IsPositionInside(hull, dataSet[i], grid))
                {
                    return BuildConcaveHull2(gridPositions, grid, random, nearestNeighbors + 1);
                }
            }

            return hull;
        }

        // This one has bugs, user beware
        // https://github.com/Liagson/ConcaveHullGenerator/tree/master
        #region ConcaveHull Functions

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

        public static List<FastVector3Int> BuildConcaveHull(this IEnumerable<FastVector3Int> gridPositions, Grid grid, IRandom random = null, float scaleFactor = 1, float concavity = 0f)
        {
            if (concavity < -1 || 1 < concavity)
            {
                throw new ArgumentException($"Concavity must be between [-1, 1], was {concavity}");
            } 

            List<FastVector3Int> originalGridPositions = gridPositions.ToList();
            if (originalGridPositions.Count <= 3)
            {
                return originalGridPositions;
            }

            List<FastVector3Int> convexHull = originalGridPositions.BuildConvexHull(grid, random);
            HashSet<FastVector3Int> unusedNodes = originalGridPositions.ToHashSet();
            unusedNodes.ExceptWith(convexHull);
            List<Line> concaveHullLines = new(convexHull.Count);
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
            do
            {
                // Order by descending
                concaveHullLines.Sort((lhs, rhs) => rhs.sqrMagnitude.CompareTo(lhs.sqrMagnitude));

                aLineWasDividedInTheIteration = false;
                for (int i = 0; i < concaveHullLines.Count; ++i)
                {
                    Line line = concaveHullLines[i];
                    IEnumerable<FastVector3Int> nearbyPoints = GetNearbyPoints(line, unusedNodes, grid, scaleFactor);
                    List<Line> dividedLine = GetDividedLine(line, nearbyPoints, concaveHullLines, grid, concavity);
                    if (0 < dividedLine.Count)
                    {
                        aLineWasDividedInTheIteration = true;
                        FastVector3Int toRemove = grid.WorldToCell(dividedLine[0].to);
                        _ = unusedNodes.Remove(toRemove);
                        concaveHullLines.AddRange(dividedLine);
                        concaveHullLines.RemoveAtSwapBack(i);
                        break;
                    }
                }
            }
            while (aLineWasDividedInTheIteration);

            List<FastVector3Int> concaveHull = new(concaveHullLines.Count);
            if (concaveHullLines.Count <= 0)
            {
                return concaveHull;
            }

            Line currentlyConsideredLine = concaveHullLines[0];
            FastVector3Int from = grid.WorldToCell(currentlyConsideredLine.from);
            FastVector3Int to = grid.WorldToCell(currentlyConsideredLine.to);
            concaveHull.Add(from);
            concaveHull.Add(to);
            concaveHullLines.RemoveAtSwapBack(0);
            while (0 < concaveHullLines.Count)
            {
                int index = concaveHullLines.FindIndex(
                    line =>
                    {
                        FastVector3Int lineFrom = grid.WorldToCell(line.from);
                        return lineFrom == to;
                    });

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

        public static bool IsPositionInside(List<FastVector3Int> hull, FastVector3Int gridPosition, Grid grid)
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

                if ((newVector.x < position.x) == (position.x <= oldVector.x) &&
                    (position.y - (long)lhs.y) * (rhs.x - lhs.x) <
                    (rhs.y - (long)lhs.y) * (position.x - lhs.x))
                {
                    isPositionInside = !isPositionInside;
                }
            }

            return isPositionInside;
        }

        private static List<Line> GetDividedLine(Line line, IEnumerable<FastVector3Int> nearbyPoints, List<Line> concaveHull, Grid grid, float concavity)
        {
            return GetDividedLine(line.from, line.to, nearbyPoints, concaveHull, grid, concavity);
        }

        private static List<Line> GetDividedLine(
            Vector2 from, Vector2 to, IEnumerable<FastVector3Int> nearbyPoints, List<Line> concaveHull, Grid grid,
            float concavity)
        {
            List<Line> dividedLine = new(2);
            Dictionary<Vector2, double> okMiddlePoints = new();
            foreach (FastVector3Int gridPoint in nearbyPoints)
            {
                Vector2 point = grid.CellToWorld(gridPoint);
                double cosine = GetCosine(from, to, point);
                if (cosine < concavity)
                {
                    Line newLineA = new(from, point);
                    Line newLineB = new(point, to);
                    if (!LineCollidesWithHull(newLineA, concaveHull) && !LineCollidesWithHull(newLineB, concaveHull))
                    {
                        okMiddlePoints[point] = cosine;
                    }
                }
            }

            if (0 < okMiddlePoints.Count)
            {
                Vector2 middlePoint = new();
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
            }

            return dividedLine;
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

        private static IEnumerable<FastVector3Int> GetNearbyPoints(Line line, ICollection<FastVector3Int> points, Grid grid, float scaleFactor)
        {
            return GetNearbyPoints(line.from, line.to, points, grid, scaleFactor);
        }

        private static IEnumerable<FastVector3Int> GetNearbyPoints(Vector2 from, Vector2 to, ICollection<FastVector3Int> points, Grid grid, float scaleFactor)
        {
            const int maxTries = 2;
            for (int tries = 0; tries < maxTries; ++tries)
            {
                bool foundAnyPoints = false;
                Bounds boundary = GetBoundary(from, to, scaleFactor);
                foreach (FastVector3Int gridPoint in points)
                {
                    Vector2 point = grid.CellToWorld(gridPoint);
                    if (point != from && point != to)
                    {
                        if (boundary.FastContains2D(point))
                        {
                            foundAnyPoints = true;
                            yield return gridPoint;
                        }
                    }
                }

                if (foundAnyPoints)
                {
                    yield break;
                }

                scaleFactor *= (4 / 3f);
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
            return new Bounds(new Vector3(xMin + width / 2, yMin + height / 2), new Vector3(width, height) * scaleFactor + new Vector3(0.001f, 0.001f));
        }

        public static Bounds? GetBoundary(this IEnumerable<Vector2> pointsInput)
        {
            List<Vector2> points = pointsInput.ToList();
            if (points.Count <= 0)
            {
                return null;
            }

            float xMin = points.Select(point => point.x).Min();
            float yMin = points.Select(point => point.y).Min();
            float xMax = points.Select(point => point.x).Max();
            float yMax = points.Select(point => point.y).Max();

            float width = xMax - xMin + 0.001f;
            float height = yMax - yMin + 0.001f;
            return new Bounds(new Vector3(xMin + width / 2, yMin + height / 2), new Vector3(width, height));
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
        public static bool Intersects(Vector2 lhsFrom, Vector2 lhsTo, Vector2 rhsFrom, Vector2 rhsTo)
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
            return q.x <= Math.Max(p.x, r.x) && Math.Min(p.x, r.x) <= q.x && q.y <= Math.Max(p.y, r.y) && Math.Min(p.y, r.y) <= q.y;
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

        #endregion

        public static Vector2 Rotate(this Vector2 v, float degrees) {
            float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
            float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

            float tx = v.x;
            float ty = v.y;
            
            Vector2 rotatedVector;
            rotatedVector.x = (cos * tx) - (sin * ty);
            rotatedVector.y = (sin * tx) + (cos * ty);
            
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
            return boundsMax.x >= otherMin.x  && boundsMax.y >= otherMin.y && boundsMax.z >= otherMin.z;
        }

        public static bool FastContains2D(this BoundsInt bounds, FastVector3Int position)
        {
            return position.x >= bounds.xMin && position.y >= bounds.yMin && position.x < bounds.xMax && position.y < bounds.yMax;
        }

        public static bool FastIntersects2D(this BoundsInt bounds, BoundsInt other)
        {
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
            return position.x < max.x && position.y < max.y;
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
            Vector3 otherMin = other.min;
            if (otherMin.x < boundsMin.x || otherMin.y < boundsMin.y)
            {
                return false;
            }

            Vector3 boundsMax = bounds.max;
            Vector3 otherMax = other.max;
            return otherMax.x <= boundsMax.x && otherMax.y <= boundsMax.y;
        }

        public static BoundsInt WithPadding(this BoundsInt bounds, int xPadding, int yPadding)
        {
            Vector3Int size = bounds.size;
            return new BoundsInt(bounds.xMin - xPadding, bounds.yMin - yPadding, bounds.zMin, size.x + 2 * xPadding, size.y + 2 * yPadding, size.z);
        }

        public static void SetColors(this UnityEngine.UI.Slider slider, Color color)
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

        public static bool Contains(this BoundsInt bounds, FastVector3Int position)
        {
            return bounds.Contains(position);
        }

        public static bool IsOnEdge2D(this FastVector3Int position, BoundsInt bounds)
        {
            if (bounds.xMin == position.x || (bounds.xMax - 1) == position.x)
            {
                return bounds.yMin <= position.y && position.y < bounds.yMax;
            }

            if (bounds.yMin == position.y || (bounds.yMax - 1) == position.y)
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

            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
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
    }
}
