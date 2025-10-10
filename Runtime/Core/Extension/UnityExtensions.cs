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
    public static class UnityExtensions
    {
        private const float ConvexHullRelationEpsilon = 1e-5f;
        private const double ConvexHullOrientationEpsilon = 1e-8d;

        public enum ConvexHullAlgorithm
        {
            [Obsolete("Do not use default value; specify an algorithm explicitly.")]
            Unknown = 0,
            MonotoneChain = 1,
            Jarvis = 2,
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

        private static List<Vector2> BuildConvexHullJarvis(
            IEnumerable<Vector2> pointsSet,
            bool includeColinearPoints
        )
        {
            using PooledResource<List<Vector2>> ptsRes = Buffers<Vector2>.List.Get(
                out List<Vector2> pts
            );
            pts.AddRange(pointsSet);

            using PooledResource<HashSet<Vector2>> uniqRes = Buffers<Vector2>.HashSet.Get(
                out HashSet<Vector2> uniq
            );
            foreach (Vector2 p in pts)
            {
                uniq.Add(p);
            }
            List<Vector2> points = new(uniq);
            if (points.Count == 0)
            {
                return new List<Vector2>();
            }
            if (points.Count <= 2)
            {
                return new List<Vector2>(points);
            }

            points.Sort(
                (a, b) =>
                {
                    int cmp = a.x.CompareTo(b.x);
                    return cmp != 0 ? cmp : a.y.CompareTo(b.y);
                }
            );
            Vector2 start = points[0];

            bool allColinear = true;
            for (int i = 1; i < points.Count - 1; ++i)
            {
                float rel = Geometry.IsAPointLeftOfVectorOrOnTheLine(start, points[^1], points[i]);
                if (Mathf.Abs(rel) > ConvexHullRelationEpsilon)
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
                Vector2 min = start;
                Vector2 max = start;
                for (int i = 0; i < points.Count; ++i)
                {
                    Vector2 w = points[i];
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

                Vector2 candidate =
                    points[0] == current && points.Count > 1 ? points[1] : points[0];
                for (int i = 0; i < points.Count; ++i)
                {
                    Vector2 p = points[i];
                    if (p == current)
                    {
                        continue;
                    }
                    float rel = Geometry.IsAPointLeftOfVectorOrOnTheLine(current, candidate, p);
                    if (rel > ConvexHullRelationEpsilon)
                    {
                        candidate = p;
                    }
                    else if (Mathf.Abs(rel) <= ConvexHullRelationEpsilon)
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
                        float rel = Geometry.IsAPointLeftOfVectorOrOnTheLine(current, candidate, p);
                        if (Mathf.Abs(rel) <= ConvexHullRelationEpsilon)
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
            int i = 0;
            while (i < hull.Count)
            {
                int prev = (i - 1 + hull.Count) % hull.Count;
                int next = (i + 1) % hull.Count;
                float cross = Geometry.IsAPointLeftOfVectorOrOnTheLine(
                    hull[prev],
                    hull[i],
                    hull[next]
                );
                if (Mathf.Abs(cross) <= ConvexHullRelationEpsilon)
                {
                    hull.RemoveAt(i);
                    if (hull.Count < 3)
                    {
                        break;
                    }
                    continue;
                }
                ++i;
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

            using PooledResource<HashSet<Vector2>> uniqueRes = Buffers<Vector2>.HashSet.Get(
                out HashSet<Vector2> unique
            );
            foreach (Vector2 p in points)
            {
                unique.Add(p);
            }
            points = new List<Vector2>(unique);
            if (points.Count <= 1)
            {
                return new List<Vector2>(points);
            }

            points.Sort(
                (a, b) =>
                {
                    int cmp = a.x.CompareTo(b.x);
                    return cmp != 0 ? cmp : a.y.CompareTo(b.y);
                }
            );

            if (points.Count >= 2)
            {
                Vector2 first = points[0];
                Vector2 last = points[^1];
                bool allColinear = true;
                for (int i = 1; i < points.Count - 1; ++i)
                {
                    float cross = Geometry.IsAPointLeftOfVectorOrOnTheLine(first, last, points[i]);
                    if (Mathf.Abs(cross) > ConvexHullRelationEpsilon)
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
                    float cross = Turn(lower[^2], lower[^1], p);
                    if (cross < -ConvexHullRelationEpsilon)
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
                    float cross = Turn(upper[^2], upper[^1], p);
                    if (cross < -ConvexHullRelationEpsilon)
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

            float Turn(Vector2 a, Vector2 b, Vector2 c)
            {
                return Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, c);
            }
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

        /// <summary>
        /// Gets the center point of a GameObject, considering any CenterPointOffset component.
        /// </summary>
        /// <param name="gameObject">The GameObject to get the center of.</param>
        /// <returns>
        /// The center point from the CenterPointOffset component if present, otherwise the transform position.
        /// </returns>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread.
        /// Null Handling: Returns default Vector2 if gameObject is null (Unity's default behavior).
        /// Performance: O(1) - Single component lookup.
        /// Allocations: None.
        /// </remarks>
        public static Vector2 GetCenter(this GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out CenterPointOffset centerPointOffset))
            {
                return centerPointOffset.CenterPoint;
            }

            return gameObject.transform.position;
        }

        /// <summary>
        /// Converts a Unity Rect to a Bounds.
        /// </summary>
        /// <param name="rect">The Rect to convert.</param>
        /// <returns>A Bounds with the same center and size as the input Rect.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe, no Unity API calls.
        /// Null Handling: Not applicable for value types.
        /// Performance: O(1) - Simple struct construction.
        /// Allocations: None - returns value type.
        /// Unity Behavior: The resulting Bounds will have zero Z-axis extent.
        /// </remarks>
        public static Bounds Bounds(this Rect rect)
        {
            return new Bounds(rect.center, rect.size);
        }

        /// <summary>
        /// Converts a Unity Bounds to a Rect.
        /// </summary>
        /// <param name="bounds">The Bounds to convert.</param>
        /// <returns>A Rect with position at (center - extents) and the same size as the Bounds.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe, no Unity API calls.
        /// Null Handling: Not applicable for value types.
        /// Performance: O(1) - Simple struct construction.
        /// Allocations: None - returns value type.
        /// Unity Behavior: Z-axis information from Bounds is discarded.
        /// Edge Cases: The Rect position is at the minimum corner (center - extents).
        /// </remarks>
        public static Rect Rect(this Bounds bounds)
        {
            return new Rect(bounds.center - bounds.extents, bounds.size);
        }

        /// <summary>
        /// Gets the world-space rectangular bounds of a RectTransform.
        /// </summary>
        /// <param name="transform">The RectTransform to get world bounds for.</param>
        /// <returns>A Rect representing the axis-aligned bounding box in world space.</returns>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread.
        /// Null Handling: Will throw NullReferenceException if transform is null.
        /// Performance: O(1) - Iterates through 4 corners.
        /// Allocations: Uses array pooling to avoid allocations.
        /// Unity Behavior: Accounts for rotation and scale by computing axis-aligned bounding box.
        /// Edge Cases: For rotated RectTransforms, the resulting Rect will be larger than the actual visual bounds.
        /// </remarks>
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

        /// <summary>
        /// Calculates the world-space bounds visible to an orthographic camera.
        /// </summary>
        /// <param name="camera">The orthographic camera.</param>
        /// <returns>A Bounds representing the visible volume of the camera.</returns>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread.
        /// Null Handling: Throws ArgumentNullException if camera is null.
        /// Performance: O(1) - Simple calculations based on camera properties.
        /// Allocations: None - returns value type.
        /// Unity Behavior: Uses Screen dimensions, orthographicSize, and clip planes.
        /// Edge Cases: Clamps screenHeight to minimum of 1 to avoid division by zero.
        /// If depth (farClipPlane - nearClipPlane) is <= 0, defaults to 1.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when camera is null.</exception>
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

        /// <summary>
        /// Converts a Vector3 to a JSON-formatted string representation.
        /// </summary>
        /// <param name="vector">The Vector3 to convert.</param>
        /// <returns>A string in the format "{x, y, z}" using invariant culture.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe.
        /// Null Handling: Not applicable for value types.
        /// Performance: O(1) - String interpolation.
        /// Allocations: Allocates string.
        /// Unity Behavior: Uses InvariantCulture for consistent formatting across locales.
        /// </remarks>
        public static string ToJsonString(this Vector3 vector)
        {
            return FormattableString.Invariant($"{{{vector.x}, {vector.y}, {vector.z}}}");
        }

        /// <summary>
        /// Converts a Vector2 to a JSON-formatted string representation.
        /// </summary>
        /// <param name="vector">The Vector2 to convert.</param>
        /// <returns>A string in the format "{x, y}" using invariant culture.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe.
        /// Null Handling: Not applicable for value types.
        /// Performance: O(1) - String interpolation.
        /// Allocations: Allocates string.
        /// Unity Behavior: Uses InvariantCulture for consistent formatting across locales.
        /// </remarks>
        public static string ToJsonString(this Vector2 vector)
        {
            return FormattableString.Invariant($"{{{vector.x}, {vector.y}}}");
        }

        /// <summary>
        /// Determines if a Vector2 represents insignificant input (noise) below a threshold.
        /// </summary>
        /// <param name="inputVector">The input vector to check.</param>
        /// <param name="threshold">The threshold below which input is considered noise. Default is 0.2.</param>
        /// <returns>True if both x and y components are within the threshold; otherwise, false.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe.
        /// Null Handling: Not applicable for value types.
        /// Performance: O(1) - Two comparisons.
        /// Allocations: None.
        /// Unity Behavior: Uses absolute value of threshold to handle negative thresholds.
        /// Edge Cases: A threshold of 0 means any non-zero input is not noise.
        /// Useful for filtering controller drift or touch input noise.
        /// </remarks>
        public static bool IsNoise(this Vector2 inputVector, float threshold = 0.2f)
        {
            float limit = Mathf.Abs(threshold);
            return Mathf.Abs(inputVector.x) <= limit && Mathf.Abs(inputVector.y) <= limit;
        }

        /// <summary>
        /// Stops a Rigidbody2D by zeroing its velocity, angular velocity, and putting it to sleep.
        /// </summary>
        /// <param name="rigidBody">The Rigidbody2D to stop.</param>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread.
        /// Null Handling: Silently returns if rigidBody is null.
        /// Performance: O(1) - Sets two properties and calls Sleep.
        /// Allocations: None.
        /// Unity Behavior: Calling Sleep() tells the physics engine to skip this body until awakened.
        /// Edge Cases: Safe to call on null or already-sleeping rigidbodies.
        /// </remarks>
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

        /// <summary>
        /// Expands a BoundsInt to encompass another BoundsInt.
        /// </summary>
        /// <param name="source">The source BoundsInt to expand.</param>
        /// <param name="other">The BoundsInt to include in the expansion.</param>
        /// <returns>A new BoundsInt that encompasses both input bounds.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe, no Unity API calls.
        /// Null Handling: Not applicable for value types.
        /// Performance: O(1) - Six min/max comparisons.
        /// Allocations: None - returns value type.
        /// Unity Behavior: Creates axis-aligned bounding box containing both bounds.
        /// Edge Cases: Works correctly with negative or zero-size bounds.
        /// </remarks>
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

        /// <summary>
        /// Calculates the minimum BoundsInt that contains all the given positions.
        /// </summary>
        /// <param name="positions">The collection of positions to encompass.</param>
        /// <param name="inclusive">
        /// If true, treats positions as inclusive (size = max - min).
        /// If false, treats positions as cell centers (size = max - min + 1). Default is false.
        /// </param>
        /// <returns>
        /// A BoundsInt containing all positions, or null if the collection is empty.
        /// </returns>
        /// <remarks>
        /// Thread Safety: Thread-safe if the collection is not modified during enumeration.
        /// Null Handling: Returns null if positions is empty. Throws if positions is null.
        /// Performance: O(n) where n is the number of positions.
        /// Allocations: None beyond enumeration overhead.
        /// Unity Behavior: The 'inclusive' parameter affects how bounds size is calculated.
        /// Edge Cases: Returns null for empty collections. Single position creates a bounds of size (1,1,1) when inclusive=false.
        /// </remarks>
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

        /// <summary>
        /// Calculates the minimum BoundsInt that contains all the given FastVector3Int positions.
        /// </summary>
        /// <param name="positions">The collection of FastVector3Int positions to encompass.</param>
        /// <returns>
        /// A BoundsInt containing all positions with size = max - min + 1, or null if the collection is empty.
        /// </returns>
        /// <remarks>
        /// Thread Safety: Thread-safe if the collection is not modified during enumeration.
        /// Null Handling: Returns null if positions is empty. Throws if positions is null.
        /// Performance: O(n) where n is the number of positions.
        /// Allocations: None beyond enumeration overhead.
        /// Unity Behavior: Always treats positions as cell centers (size = max - min + 1).
        /// Edge Cases: Returns null for empty collections. Single position creates a bounds of size (1,1,1).
        /// </remarks>
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

        /// <summary>
        /// Calculates the minimum Bounds that contains all the given Vector2 positions.
        /// </summary>
        /// <param name="positions">The collection of Vector2 positions to encompass.</param>
        /// <returns>
        /// A Bounds centered on the midpoint with size encompassing all positions, or null if the collection is empty.
        /// </returns>
        /// <remarks>
        /// Thread Safety: Thread-safe if the collection is not modified during enumeration.
        /// Null Handling: Returns null if positions is empty. Throws if positions is null.
        /// Performance: O(n) where n is the number of positions.
        /// Allocations: None beyond enumeration overhead.
        /// Unity Behavior: Creates a 2D bounds (Z component is zero). Center is at the geometric center.
        /// Edge Cases: Returns null for empty collections. Single position creates a zero-size bounds.
        /// </remarks>
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

        /// <summary>
        /// Calculates the minimum Bounds that contains all the given Bounds.
        /// </summary>
        /// <param name="boundaries">The collection of Bounds to encompass.</param>
        /// <returns>
        /// A Bounds with center at the average of input centers and size encompassing all boundaries,
        /// or null if the collection is empty.
        /// </returns>
        /// <remarks>
        /// Thread Safety: Thread-safe if the collection is not modified during enumeration.
        /// Null Handling: Returns null if boundaries is empty. Throws if boundaries is null.
        /// Performance: O(n) where n is the number of boundaries.
        /// Allocations: None beyond enumeration overhead.
        /// Unity Behavior: Center is the average of all input centers, size encompasses all extents.
        /// Edge Cases: Returns null for empty collections. Single boundary returns a copy.
        /// </remarks>
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

        /// <summary>
        /// Builds a convex hull from a set of Vector3Int grid positions using the Gift Wrapping (Jarvis March) algorithm.
        /// </summary>
        /// <param name="pointsSet">The collection of grid positions to build the hull from.</param>
        /// <param name="grid">The Grid used to convert cell positions to world coordinates.</param>
        /// <param name="includeColinearPoints">
        /// If true, includes points that lie on the hull edges. If false, only includes corner points. Default is true.
        /// </param>
        /// <returns>
        /// A list of Vector3Int positions forming the convex hull in counterclockwise order.
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
                    List<FastVector3Int> fast = new();
                    foreach (Vector3Int p in pointsSet)
                    {
                        fast.Add(new FastVector3Int(p.x, p.y, p.z));
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

            // Deduplicate
            points = new List<Vector3Int>(new HashSet<Vector3Int>(points));
            if (points.Count <= 1)
            {
                return new List<Vector3Int>(points);
            }

            // Sort by world-space X then Y
            points.Sort(
                (lhs, rhs) =>
                {
                    Vector2 a = grid.CellToWorld(lhs);
                    Vector2 b = grid.CellToWorld(rhs);
                    int cmp = a.x.CompareTo(b.x);
                    return cmp != 0 ? cmp : a.y.CompareTo(b.y);
                }
            );

            // Degenerate: all points are colinear → return endpoints (or all if requested)
            if (points.Count >= 2)
            {
                Vector2 firstW = grid.CellToWorld(points[0]);
                Vector2 lastW = grid.CellToWorld(points[^1]);
                bool allColinear = true;
                for (int i = 1; i < points.Count - 1; ++i)
                {
                    Vector2 pi = grid.CellToWorld(points[i]);
                    float cross = Geometry.IsAPointLeftOfVectorOrOnTheLine(firstW, lastW, pi);
                    if (Mathf.Abs(cross) > ConvexHullRelationEpsilon)
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
                    float cross = Turn(lower[^2], lower[^1], p);
                    // Keep colinear points during construction; prune later if needed
                    if (cross < -ConvexHullRelationEpsilon)
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
                    float cross = Turn(upper[^2], upper[^1], p);
                    // Keep colinear points during construction; prune later if needed
                    if (cross < -ConvexHullRelationEpsilon)
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
                PruneColinearOnHull(hull, grid);
            }
            return hull;

            float Turn(Vector3Int a, Vector3Int b, Vector3Int c)
            {
                Vector2 aw = grid.CellToWorld(a);
                Vector2 bw = grid.CellToWorld(b);
                Vector2 cw = grid.CellToWorld(c);
                return Geometry.IsAPointLeftOfVectorOrOnTheLine(aw, bw, cw);
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

            using PooledResource<HashSet<FastVector3Int>> uniqueBuffer =
                Buffers<FastVector3Int>.HashSet.Get(out HashSet<FastVector3Int> unique);
            foreach (FastVector3Int p in points)
            {
                unique.Add(p);
            }
            points = new List<FastVector3Int>(unique);
            if (points.Count <= 1)
            {
                return new List<FastVector3Int>(points);
            }

            points.Sort(
                (lhs, rhs) =>
                {
                    Vector2 a = grid.CellToWorld(lhs);
                    Vector2 b = grid.CellToWorld(rhs);
                    int cmp = a.x.CompareTo(b.x);
                    return cmp != 0 ? cmp : a.y.CompareTo(b.y);
                }
            );

            // Degenerate: all points are colinear → return endpoints (or all if requested)
            if (points.Count >= 2)
            {
                Vector2 firstW = grid.CellToWorld(points[0]);
                Vector2 lastW = grid.CellToWorld(points[^1]);
                bool allColinear = true;
                for (int i = 1; i < points.Count - 1; ++i)
                {
                    Vector2 pi = grid.CellToWorld(points[i]);
                    float cross = Geometry.IsAPointLeftOfVectorOrOnTheLine(firstW, lastW, pi);
                    if (Mathf.Abs(cross) > ConvexHullRelationEpsilon)
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
                    float cross = Turn(lower[^2], lower[^1], p);
                    // Keep colinear points during construction; prune later if needed
                    if (cross < -ConvexHullRelationEpsilon)
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
                    float cross = Turn(upper[^2], upper[^1], p);
                    // Keep colinear points during construction; prune later if needed
                    if (cross < -ConvexHullRelationEpsilon)
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
                PruneColinearOnHull(hull, grid);
            }
            return hull;

            float Turn(FastVector3Int a, FastVector3Int b, FastVector3Int c)
            {
                Vector2 aw = grid.CellToWorld(a);
                Vector2 bw = grid.CellToWorld(b);
                Vector2 cw = grid.CellToWorld(c);
                return Geometry.IsAPointLeftOfVectorOrOnTheLine(aw, bw, cw);
            }
        }

        private static List<FastVector3Int> BuildConvexHullJarvis(
            IEnumerable<FastVector3Int> pointsSet,
            Grid grid,
            bool includeColinearPoints
        )
        {
            using PooledResource<List<FastVector3Int>> ptsRes = Buffers<FastVector3Int>.List.Get(
                out List<FastVector3Int> pts
            );
            pts.AddRange(pointsSet);
            // Deduplicate
            using PooledResource<HashSet<FastVector3Int>> uniqRes =
                Buffers<FastVector3Int>.HashSet.Get(out HashSet<FastVector3Int> uniq);
            foreach (FastVector3Int p in pts)
            {
                uniq.Add(p);
            }
            List<FastVector3Int> points = new(uniq);
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
            Vector2 startW = grid.CellToWorld(start);
            for (int i = 1; i < points.Count; ++i)
            {
                Vector2 w = grid.CellToWorld(points[i]);
                if (w.x < startW.x || (Mathf.Approximately(w.x, startW.x) && w.y < startW.y))
                {
                    start = points[i];
                    startW = w;
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
                float rel = Geometry.IsAPointLeftOfVectorOrOnTheLine(
                    grid.CellToWorld(start),
                    grid.CellToWorld(anyOther),
                    grid.CellToWorld(p)
                );
                if (Mathf.Abs(rel) > ConvexHullRelationEpsilon)
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
                            Vector2 aw = grid.CellToWorld(a);
                            Vector2 bw = grid.CellToWorld(b);
                            int cmp = aw.x.CompareTo(bw.x);
                            return cmp != 0 ? cmp : aw.y.CompareTo(bw.y);
                        }
                    );
                    return points;
                }
                else
                {
                    // Return endpoints by min/max world
                    FastVector3Int min = start;
                    FastVector3Int max = start;
                    Vector2 minW = grid.CellToWorld(min);
                    Vector2 maxW = minW;
                    for (int i = 0; i < points.Count; ++i)
                    {
                        Vector2 w = grid.CellToWorld(points[i]);
                        if (w.x < minW.x || (Mathf.Approximately(w.x, minW.x) && w.y < minW.y))
                        {
                            min = points[i];
                            minW = w;
                        }
                        if (w.x > maxW.x || (Mathf.Approximately(w.x, maxW.x) && w.y > maxW.y))
                        {
                            max = points[i];
                            maxW = w;
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
                Vector2 worldPoint = grid.CellToWorld(current);

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
                    Vector2 pW = grid.CellToWorld(p);
                    Vector2 cW = grid.CellToWorld(candidate);
                    float rel = Geometry.IsAPointLeftOfVectorOrOnTheLine(worldPoint, cW, pW);

                    if (rel > ConvexHullRelationEpsilon)
                    {
                        // p is more counterclockwise
                        candidate = p;
                    }
                    else if (Mathf.Abs(rel) <= ConvexHullRelationEpsilon)
                    {
                        // p is collinear with candidate, prefer the farther one
                        float distCandidate = (cW - worldPoint).sqrMagnitude;
                        float distP = (pW - worldPoint).sqrMagnitude;
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

                    Vector2 candidateW = grid.CellToWorld(candidate);
                    for (int i = 0; i < points.Count; ++i)
                    {
                        FastVector3Int p = points[i];
                        // Skip current point AND the candidate (candidate will be added in next iteration)
                        if (p == current || p == candidate)
                        {
                            continue;
                        }
                        Vector2 pW = grid.CellToWorld(p);
                        float rel = Geometry.IsAPointLeftOfVectorOrOnTheLine(
                            worldPoint,
                            candidateW,
                            pW
                        );

                        if (Mathf.Abs(rel) <= ConvexHullRelationEpsilon)
                        {
                            colinear.Add(p);
                        }
                    }

                    if (colinear.Count > 0)
                    {
                        // Sort by distance and add all (excluding duplicates)
                        SortByDistanceAscending(colinear, grid, worldPoint);

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
                PruneColinearOnHull(hull, grid);
            }
            return hull;
        }

        private static void PruneColinearOnHull(List<Vector3Int> hull, Grid grid)
        {
            int i = 0;
            while (i < hull.Count)
            {
                int prev = (i - 1 + hull.Count) % hull.Count;
                int next = (i + 1) % hull.Count;
                Vector2 a = grid.CellToWorld(hull[prev]);
                Vector2 b = grid.CellToWorld(hull[i]);
                Vector2 c = grid.CellToWorld(hull[next]);
                float cross = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, c);
                if (Mathf.Abs(cross) <= ConvexHullRelationEpsilon)
                {
                    hull.RemoveAt(i);
                    if (hull.Count < 3)
                    {
                        break;
                    }
                    // Stay at same index to re-evaluate after removal
                    continue;
                }
                ++i;
            }
        }

        private static void PruneColinearOnHull(List<FastVector3Int> hull, Grid grid)
        {
            int i = 0;
            while (i < hull.Count)
            {
                int prev = (i - 1 + hull.Count) % hull.Count;
                int next = (i + 1) % hull.Count;
                Vector2 a = grid.CellToWorld(hull[prev]);
                Vector2 b = grid.CellToWorld(hull[i]);
                Vector2 c = grid.CellToWorld(hull[next]);
                float cross = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, c);
                if (Mathf.Abs(cross) <= ConvexHullRelationEpsilon)
                {
                    hull.RemoveAt(i);
                    if (hull.Count < 3)
                    {
                        break;
                    }
                    continue;
                }
                ++i;
            }
        }

        // ===================== Vector2 Convex Hull Containment =====================

        public static bool IsConvexHullInsideConvexHull(
            this List<Vector2> convexHull,
            List<Vector2> maybeInside
        )
        {
            int orientation = DetermineConvexHullOrientation(convexHull);
            foreach (Vector2 point in maybeInside)
            {
                if (!IsPointInsideConvexHull(convexHull, point, orientation))
                {
                    return false;
                }
            }
            return true;
        }

        private static int DetermineConvexHullOrientation(List<Vector2> convexHull)
        {
            if (convexHull == null || convexHull.Count < 3)
            {
                return 0;
            }
            double area = 0d;
            for (int i = 0; i < convexHull.Count; ++i)
            {
                Vector2 a = convexHull[i];
                Vector2 b = convexHull[(i + 1) % convexHull.Count];
                area += (double)a.x * b.y - (double)a.y * b.x;
            }
            if (Math.Abs(area) <= ConvexHullOrientationEpsilon)
            {
                return 0;
            }
            return area > 0d ? 1 : -1;
        }

        private static bool IsPointInsideConvexHull(
            List<Vector2> convexHull,
            Vector2 point,
            int expectedSide
        )
        {
            if (convexHull == null || convexHull.Count == 0)
            {
                return true;
            }
            int requiredSide = expectedSide;
            for (int i = 0; i < convexHull.Count; ++i)
            {
                Vector2 lhs = convexHull[i];
                Vector2 rhs = convexHull[(i + 1) % convexHull.Count];
                float relation = Geometry.IsAPointLeftOfVectorOrOnTheLine(lhs, rhs, point);
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

        /// <summary>
        /// Determines if one convex hull is completely inside another convex hull.
        /// </summary>
        /// <param name="convexHull">The outer convex hull to test against.</param>
        /// <param name="grid">The Grid used for coordinate conversion.</param>
        /// <param name="maybeInside">The convex hull to test if it's inside.</param>
        /// <returns>True if all points of maybeInside are inside convexHull; otherwise, false.</returns>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread (uses Grid).
        /// Null Handling: Returns true if convexHull is null or empty. Throws if grid or maybeInside is null.
        /// Performance: O(nm) where n is outer hull size and m is inner hull size.
        /// Allocations: None.
        /// Unity Behavior: Uses grid.CellToWorld for spatial calculations.
        /// Edge Cases: Empty or null outer hull returns true. Determines hull orientation automatically.
        /// </remarks>
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

        // ===================== Vector2 Concave Hulls =====================

        public static List<Vector2> BuildConcaveHull(
            this IReadOnlyCollection<Vector2> points,
            ConcaveHullOptions options
        )
        {
            options ??= new ConcaveHullOptions();
            switch (options.Strategy)
            {
                case ConcaveHullStrategy.Knn:
                    return BuildConcaveHull2(points, Math.Max(3, options.NearestNeighbors));
                case ConcaveHullStrategy.EdgeSplit:
                    return BuildConcaveHull3(
                        points,
                        Math.Max(1, options.BucketSize),
                        options.AngleThreshold
                    );
                default:
                    throw new InvalidEnumArgumentException(
                        nameof(options.Strategy),
                        (int)options.Strategy,
                        typeof(ConcaveHullStrategy)
                    );
            }
        }

        public static List<Vector2> BuildConcaveHullKnn(
            this IReadOnlyCollection<Vector2> points,
            int nearestNeighbors = 3
        )
        {
            return BuildConcaveHull2(points, nearestNeighbors);
        }

        public static List<Vector2> BuildConcaveHullEdgeSplit(
            this IReadOnlyCollection<Vector2> points,
            int bucketSize = 40,
            float angleThreshold = 90f
        )
        {
            return BuildConcaveHull3(points, bucketSize, angleThreshold);
        }

        /// <summary>
        /// Determines if a Vector3Int point is inside a convex hull.
        /// </summary>
        /// <param name="convexHull">The convex hull to test against.</param>
        /// <param name="grid">The Grid used for coordinate conversion.</param>
        /// <param name="point">The point to test.</param>
        /// <returns>True if the point is inside or on the boundary of the convex hull; otherwise, false.</returns>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread (uses Grid).
        /// Null Handling: Returns true if convexHull is null or empty. Throws if grid is null.
        /// Performance: O(n) where n is the hull size.
        /// Allocations: None.
        /// Unity Behavior: Uses grid.CellToWorld for spatial calculations.
        /// Edge Cases: Points on the hull boundary are considered inside. Determines orientation automatically.
        /// </remarks>
        public static bool IsPointInsideConvexHull(
            this List<Vector3Int> convexHull,
            Grid grid,
            Vector3Int point
        )
        {
            int orientation = DetermineConvexHullOrientation(convexHull, grid);
            return IsPointInsideConvexHull(convexHull, grid, point, orientation);
        }

        /// <summary>
        /// Determines if a FastVector3Int point is inside a convex hull.
        /// </summary>
        /// <param name="convexHull">The convex hull to test against.</param>
        /// <param name="grid">The Grid used for coordinate conversion.</param>
        /// <param name="point">The point to test.</param>
        /// <returns>True if the point is inside or on the boundary of the convex hull; otherwise, false.</returns>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread (uses Grid).
        /// Null Handling: Returns true if convexHull is null or empty. Throws if grid is null.
        /// Performance: O(n) where n is the hull size.
        /// Allocations: None.
        /// Unity Behavior: Uses grid.CellToWorld for spatial calculations.
        /// Edge Cases: Points on the hull boundary are considered inside. Determines orientation automatically.
        /// </remarks>
        public static bool IsPointInsideConvexHull(
            this List<FastVector3Int> convexHull,
            Grid grid,
            FastVector3Int point
        )
        {
            int orientation = DetermineConvexHullOrientation(convexHull, grid);
            return IsPointInsideConvexHull(convexHull, grid, point, orientation);
        }

        /// <summary>
        /// Determines if one convex hull is completely inside another convex hull.
        /// </summary>
        /// <param name="convexHull">The outer convex hull to test against.</param>
        /// <param name="grid">The Grid used for coordinate conversion.</param>
        /// <param name="maybeInside">The convex hull to test if it's inside.</param>
        /// <returns>True if all points of maybeInside are inside convexHull; otherwise, false.</returns>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread (uses Grid).
        /// Null Handling: Returns true if convexHull is null or empty. Throws if grid or maybeInside is null.
        /// Performance: O(nm) where n is outer hull size and m is inner hull size.
        /// Allocations: None.
        /// Unity Behavior: Uses grid.CellToWorld for spatial calculations.
        /// Edge Cases: Empty or null outer hull returns true. Determines hull orientation automatically.
        /// </remarks>
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

            foreach (Vector2 p in original)
            {
                if (unique.Add(p))
                {
                    dataSet.Add(p);
                }
            }
            int maximumNearestNeighbors = dataSet.Count;
            if (dataSet.Count <= 4)
            {
                return input.BuildConvexHull(includeColinearPoints: false);
            }
            nearestNeighbors = Math.Min(dataSet.Count, nearestNeighbors);

            Vector2 first = default;
            bool firstInit = false;
            float lowestY = float.MaxValue;
            foreach (Vector2 p in dataSet)
            {
                if (
                    !firstInit
                    || p.y < lowestY
                    || (Mathf.Approximately(p.y, lowestY) && p.x < first.x)
                )
                {
                    first = p;
                    lowestY = p.y;
                    firstInit = true;
                }
            }
            if (!firstInit)
            {
                return new List<Vector2>(dataSet);
            }

            List<Vector2> hull = new(dataSet.Count) { first };
            int step = 2;
            int maxSteps = Math.Max(16, dataSet.Count * 6);
            float previousAngle = 0f;
            Vector2 current = first;
            _ = dataSet.Remove(current);

            using PooledResource<List<Vector2>> clockwisePointsRes = Buffers<Vector2>.List.Get(
                out List<Vector2> clockwisePoints
            );
            while (0 < dataSet.Count)
            {
                if (step == 5)
                {
                    dataSet.Add(first);
                }

                FindNearestNeighborsAndPutInClockwisePoints();
                SortByRightHandTurn(clockwisePoints, current, previousAngle);

                bool intersects = true;
                int i = -1;
                while (intersects && i < clockwisePoints.Count - 1)
                {
                    ++i;
                    Vector2 indexedPoint = clockwisePoints[i];
                    int lastPoint = indexedPoint == first ? 1 : 0;
                    int j = 2;
                    intersects = false;
                    Vector2 lhsTo = indexedPoint;
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
                    for (i = dataSet.Count - 1; 0 <= i; --i)
                    {
                        if (!IsPositionInside(hull, dataSet[i]))
                        {
                            if (nearestNeighbors >= maximumNearestNeighbors)
                            {
                                return input.BuildConvexHull(includeColinearPoints: false);
                            }
                            return BuildConcaveHull2(input, nearestNeighbors + 1);
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

                previousAngle = CalculateAngle(hull[step - 1], hull[step - 2]);
                ++step;
                if (step > maxSteps)
                {
                    break;
                }
            }

            for (int i = dataSet.Count - 1; 0 <= i; --i)
            {
                if (!IsPositionInside(hull, dataSet[i]))
                {
                    if (nearestNeighbors >= maximumNearestNeighbors)
                    {
                        return input.BuildConvexHull(includeColinearPoints: false);
                    }
                    return BuildConcaveHull2(input, nearestNeighbors + 1);
                }
            }
            return hull;

            void FindNearestNeighborsAndPutInClockwisePoints()
            {
                clockwisePoints.Clear();
                clockwisePoints.AddRange(dataSet);
                SortByDistanceAscending(clockwisePoints, current);
                if (nearestNeighbors < clockwisePoints.Count)
                {
                    clockwisePoints.RemoveRange(
                        nearestNeighbors,
                        clockwisePoints.Count - nearestNeighbors
                    );
                }
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
            if (dataSet.Count <= 4)
            {
                return gridPositions.BuildConvexHull(grid);
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
            int maxSteps = Math.Max(16, dataSet.Count * 6);
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
                                return gridPositions.BuildConvexHull(grid);
                            }

                            return BuildConcaveHull2(gridPositions, grid, nearestNeighbors + 1);
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
                if (step > maxSteps)
                {
                    // Safety break to avoid potential infinite loop; fall through to final containment check
                    break;
                }
            }

            for (int i = dataSet.Count - 1; 0 <= i; --i)
            {
                if (!IsPositionInside(hull, dataSet[i], grid))
                {
                    if (nearestNeighbors >= maximumNearestNeighbors)
                    {
                        return gridPositions.BuildConvexHull(grid);
                    }

                    return BuildConcaveHull2(gridPositions, grid, nearestNeighbors + 1);
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
        /// Fast 3D bounds intersection test optimized to minimize property accesses.
        /// </summary>
        /// <param name="bounds">The first bounds.</param>
        /// <param name="other">The second bounds to test intersection with.</param>
        /// <returns>True if the bounds intersect; otherwise, false.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe, no Unity API calls beyond property access.
        /// Null Handling: Not applicable for value types.
        /// Performance: O(1) - Optimized to cache min/max values. Faster than Unity's built-in Bounds.Intersects.
        /// Allocations: None.
        /// Unity Behavior: Uses bounds.min and bounds.max properties.
        /// Edge Cases: Bounds that touch but don't overlap return false. Zero-size bounds can intersect.
        /// </remarks>
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

        /// <summary>
        /// Fast 2D containment test for BoundsInt and FastVector3Int (ignores Z axis).
        /// </summary>
        /// <param name="bounds">The bounds to test containment in.</param>
        /// <param name="position">The position to test.</param>
        /// <returns>True if the position is inside the 2D bounds (XY plane only); otherwise, false.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe, no Unity API calls.
        /// Null Handling: Not applicable for value types.
        /// Performance: O(1) - Four comparisons.
        /// Allocations: None.
        /// Unity Behavior: Uses half-open interval [min, max) for containment test.
        /// Edge Cases: Point on max boundary is NOT contained. Z coordinate is ignored.
        /// </remarks>
        public static bool FastContains2D(this BoundsInt bounds, FastVector3Int position)
        {
            return position.x >= bounds.xMin
                && position.y >= bounds.yMin
                && position.x < bounds.xMax
                && position.y < bounds.yMax;
        }

        /// <summary>
        /// Fast 2D intersection test for BoundsInt (ignores Z axis).
        /// </summary>
        /// <param name="bounds">The first bounds.</param>
        /// <param name="other">The second bounds to test intersection with.</param>
        /// <returns>True if the 2D bounds intersect (XY plane only); otherwise, false.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe, no Unity API calls.
        /// Null Handling: Not applicable for value types.
        /// Performance: O(1) - Optimized comparisons.
        /// Allocations: None.
        /// Unity Behavior: Uses BoundsInt min/max properties.
        /// Edge Cases: Zero-size bounds (size <= 0 in X or Y) cannot intersect and return false.
        /// Bounds that touch but don't overlap return false. Z axis is ignored.
        /// </remarks>
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

        /// <summary>
        /// Fast 2D containment test for Bounds and Vector2 (ignores Z axis).
        /// </summary>
        /// <param name="bounds">The bounds to test containment in.</param>
        /// <param name="position">The 2D position to test.</param>
        /// <returns>True if the position is inside the 2D bounds (XY plane only); otherwise, false.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe, no Unity API calls beyond property access.
        /// Null Handling: Not applicable for value types.
        /// Performance: O(1) - Optimized to cache min/max values.
        /// Allocations: None.
        /// Unity Behavior: Uses closed interval [min, max] for containment test (unlike BoundsInt).
        /// Edge Cases: Points on the boundary ARE contained. Z coordinate is ignored.
        /// </remarks>
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

        /// <summary>
        /// Fast 2D containment test to check if one Bounds contains another (ignores Z axis).
        /// </summary>
        /// <param name="bounds">The outer bounds.</param>
        /// <param name="other">The inner bounds to test if contained.</param>
        /// <returns>True if other is completely inside bounds (XY plane only); otherwise, false.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe, no Unity API calls beyond property access.
        /// Null Handling: Not applicable for value types.
        /// Performance: O(1) - Optimized to cache min/max values.
        /// Allocations: None.
        /// Unity Behavior: Uses Bounds min/max properties.
        /// Edge Cases: If other touches the boundary of bounds, it's still considered contained.
        /// Z axis is ignored.
        /// </remarks>
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

        /// <summary>
        /// Fast 2D intersection test for Bounds (ignores Z axis).
        /// </summary>
        /// <param name="bounds">The first bounds.</param>
        /// <param name="other">The second bounds to test intersection with.</param>
        /// <returns>True if the 2D bounds intersect (XY plane only); otherwise, false.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe, no Unity API calls beyond property access.
        /// Null Handling: Not applicable for value types.
        /// Performance: O(1) - Optimized to cache min/max values.
        /// Allocations: None.
        /// Unity Behavior: Uses Bounds min/max properties.
        /// Edge Cases: Bounds that touch but don't overlap return false. Z axis is ignored.
        /// </remarks>
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

        /// <summary>
        /// Fast 2D overlap test for Bounds (ignores Z axis). Functionally identical to FastIntersects2D.
        /// </summary>
        /// <param name="bounds">The first bounds.</param>
        /// <param name="other">The second bounds to test overlap with.</param>
        /// <returns>True if the 2D bounds overlap (XY plane only); otherwise, false.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe, no Unity API calls beyond property access.
        /// Null Handling: Not applicable for value types.
        /// Performance: O(1) - Optimized to cache min/max values.
        /// Allocations: None.
        /// Unity Behavior: Uses Bounds min/max properties. Identical to FastIntersects2D.
        /// Edge Cases: Bounds that touch but don't overlap return false. Z axis is ignored.
        /// </remarks>
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

        /// <summary>
        /// Creates a new BoundsInt with additional padding in the X and Y directions.
        /// </summary>
        /// <param name="bounds">The source bounds to add padding to.</param>
        /// <param name="xPadding">The padding to add to both left and right sides.</param>
        /// <param name="yPadding">The padding to add to both top and bottom sides.</param>
        /// <returns>A new BoundsInt expanded by the specified padding amounts.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe, no Unity API calls.
        /// Null Handling: Not applicable for value types.
        /// Performance: O(1) - Simple arithmetic.
        /// Allocations: None - returns value type.
        /// Unity Behavior: Z dimension remains unchanged.
        /// Edge Cases: Negative padding shrinks the bounds. Size increases by 2*padding in each direction.
        /// </remarks>
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

        /// <summary>
        /// Sets all color states of a UI Slider to the same color.
        /// </summary>
        /// <param name="slider">The slider to modify.</param>
        /// <param name="color">The color to apply to all states.</param>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread.
        /// Null Handling: Will throw NullReferenceException if slider is null.
        /// Performance: O(1) - Simple property assignments.
        /// Allocations: One ColorBlock struct allocation.
        /// Unity Behavior: Sets normalColor, highlightedColor, pressedColor, selectedColor, and disabledColor.
        /// Edge Cases: Overwrites all existing color states.
        /// </remarks>
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

        /// <summary>
        /// Sets the left offset of a RectTransform.
        /// </summary>
        /// <param name="rt">The RectTransform to modify.</param>
        /// <param name="left">The left offset value.</param>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread.
        /// Null Handling: Will throw NullReferenceException if rt is null.
        /// Performance: O(1) - Single property assignment.
        /// Allocations: One Vector2 struct allocation.
        /// Unity Behavior: Modifies offsetMin.x which controls the left anchor offset.
        /// Edge Cases: Preserves the bottom offset (offsetMin.y).
        /// </remarks>
        public static void SetLeft(this RectTransform rt, float left)
        {
            rt.offsetMin = new Vector2(left, rt.offsetMin.y);
        }

        /// <summary>
        /// Sets the right offset of a RectTransform.
        /// </summary>
        /// <param name="rt">The RectTransform to modify.</param>
        /// <param name="right">The right offset value (will be negated internally).</param>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread.
        /// Null Handling: Will throw NullReferenceException if rt is null.
        /// Performance: O(1) - Single property assignment.
        /// Allocations: One Vector2 struct allocation.
        /// Unity Behavior: Modifies offsetMax.x. Note that the value is negated (-right).
        /// Edge Cases: Preserves the top offset (offsetMax.y).
        /// </remarks>
        public static void SetRight(this RectTransform rt, float right)
        {
            rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
        }

        /// <summary>
        /// Sets the top offset of a RectTransform.
        /// </summary>
        /// <param name="rt">The RectTransform to modify.</param>
        /// <param name="top">The top offset value (will be negated internally).</param>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread.
        /// Null Handling: Will throw NullReferenceException if rt is null.
        /// Performance: O(1) - Single property assignment.
        /// Allocations: One Vector2 struct allocation.
        /// Unity Behavior: Modifies offsetMax.y. Note that the value is negated (-top).
        /// Edge Cases: Preserves the right offset (offsetMax.x).
        /// </remarks>
        public static void SetTop(this RectTransform rt, float top)
        {
            rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
        }

        /// <summary>
        /// Sets the bottom offset of a RectTransform.
        /// </summary>
        /// <param name="rt">The RectTransform to modify.</param>
        /// <param name="bottom">The bottom offset value.</param>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread.
        /// Null Handling: Will throw NullReferenceException if rt is null.
        /// Performance: O(1) - Single property assignment.
        /// Allocations: One Vector2 struct allocation.
        /// Unity Behavior: Modifies offsetMin.y which controls the bottom anchor offset.
        /// Edge Cases: Preserves the left offset (offsetMin.x).
        /// </remarks>
        public static void SetBottom(this RectTransform rt, float bottom)
        {
            rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
        }

        /// <summary>
        /// Enumerates all FastVector3Int positions within the bounds.
        /// </summary>
        /// <param name="bounds">The bounds to enumerate positions within.</param>
        /// <returns>An enumerable of all FastVector3Int positions within the bounds.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe, no Unity API calls.
        /// Null Handling: Not applicable for value types.
        /// Performance: O(volume) where volume is the number of cells in the bounds.
        /// Allocations: Uses yield return, allocates enumerator. Each FastVector3Int is a value type.
        /// Unity Behavior: Uses half-open interval [min, max) consistent with BoundsInt.
        /// Edge Cases: Zero or negative size bounds yield no positions.
        /// Iteration order is X (innermost), then Y, then Z (outermost).
        /// </remarks>
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

        /// <summary>
        /// Fills a list with all FastVector3Int positions within the bounds.
        /// </summary>
        /// <param name="bounds">The bounds to get positions from.</param>
        /// <param name="buffer">The list to clear and fill with positions.</param>
        /// <returns>The buffer list containing all positions.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe if buffer is not accessed concurrently.
        /// Null Handling: Throws NullReferenceException if buffer is null.
        /// Performance: O(volume) where volume is the number of cells in the bounds.
        /// Allocations: May allocate if buffer capacity is insufficient. Clears buffer first.
        /// Unity Behavior: Uses half-open interval [min, max) consistent with BoundsInt.
        /// Edge Cases: Zero or negative size bounds result in an empty buffer.
        /// Iteration order is X (innermost), then Y, then Z (outermost).
        /// </remarks>
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

        /// <summary>
        /// Determines if a BoundsInt contains a FastVector3Int position.
        /// </summary>
        /// <param name="bounds">The bounds to test containment in.</param>
        /// <param name="position">The position to test.</param>
        /// <returns>True if the position is within the bounds; otherwise, false.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe, no Unity API calls.
        /// Null Handling: Not applicable for value types.
        /// Performance: O(1) - Delegates to BoundsInt.Contains via implicit conversion.
        /// Allocations: None.
        /// Unity Behavior: Uses half-open interval [min, max) for containment test.
        /// Edge Cases: Points on the max boundary are NOT contained.
        /// </remarks>
        public static bool Contains(this BoundsInt bounds, FastVector3Int position)
        {
            return bounds.Contains(position);
        }

        /// <summary>
        /// Determines if a FastVector3Int position is on the 2D edge of a BoundsInt (ignores Z axis).
        /// </summary>
        /// <param name="position">The position to test.</param>
        /// <param name="bounds">The bounds to test against.</param>
        /// <returns>True if the position is on the 2D boundary of the bounds; otherwise, false.</returns>
        /// <remarks>
        /// Thread Safety: Thread-safe, no Unity API calls.
        /// Null Handling: Not applicable for value types.
        /// Performance: O(1) - Simple comparisons.
        /// Allocations: None.
        /// Unity Behavior: Tests if position is on the min or max-1 boundary in X or Y.
        /// Edge Cases: Position must be within bounds AND on an edge to return true.
        /// Uses max-1 because BoundsInt uses half-open intervals. Z axis is ignored.
        /// </remarks>
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
        /// <summary>
        /// Extracts all Sprite objects referenced in an AnimationClip.
        /// </summary>
        /// <param name="clip">The AnimationClip to extract sprites from.</param>
        /// <returns>An enumerable of all Sprite objects found in the animation clip.</returns>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread. Editor-only.
        /// Null Handling: Returns empty enumerable if clip is null.
        /// Performance: O(n*m) where n is number of bindings and m is keyframes per binding.
        /// Allocations: Allocates arrays for bindings and keyframes.
        /// Unity Behavior: Only available in Unity Editor. Uses AnimationUtility.
        /// Edge Cases: Only returns Sprite object references, ignores other object types.
        /// </remarks>
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

        /// <summary>
        /// Determines if a GameObject is in the DontDestroyOnLoad scene.
        /// </summary>
        /// <param name="gameObjectToCheck">The GameObject to check.</param>
        /// <returns>True if the GameObject is in the DontDestroyOnLoad scene; otherwise, false.</returns>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread.
        /// Null Handling: Returns false if gameObjectToCheck is null.
        /// Performance: O(1) - Simple string comparison.
        /// Allocations: None beyond string comparison.
        /// Unity Behavior: Checks if the GameObject's scene name is exactly "DontDestroyOnLoad".
        /// Edge Cases: Returns false for null GameObjects. Uses ordinal string comparison.
        /// </remarks>
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

        /// <summary>
        /// Determines if a circle is fully contained within a Collider2D by sampling points around its perimeter.
        /// </summary>
        /// <param name="targetCollider">The collider to test containment in.</param>
        /// <param name="center">The center of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="sampleCount">The number of points to sample around the circle. Default is 16.</param>
        /// <returns>True if all sampled points on the circle are inside the collider; otherwise, false.</returns>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread.
        /// Null Handling: Will throw NullReferenceException if targetCollider is null.
        /// Performance: O(sampleCount) - Uses Physics2D.OverlapPoint for each sample.
        /// Allocations: Minimal - Vector2 allocations for sample points.
        /// Unity Behavior: Uses Collider2D.OverlapPoint which respects physics layers and triggers.
        /// Edge Cases: Higher sampleCount provides more accurate results but is slower.
        /// Does not check the circle interior, only the perimeter.
        /// May return false positives for very small circles or low sample counts.
        /// </remarks>
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

        /// <summary>
        /// Inverts a PolygonCollider2D by making holes become solid and the outside become a hole, bounded by outerRect.
        /// </summary>
        /// <param name="col">The PolygonCollider2D to invert.</param>
        /// <param name="outerRect">The rectangular boundary for the inverted polygon.</param>
        /// <remarks>
        /// Thread Safety: Must be called from Unity main thread.
        /// Null Handling: Will throw NullReferenceException if col is null.
        /// Performance: O(n*m) where n is pathCount and m is average path length. Uses array pooling.
        /// Allocations: Uses pooled arrays to minimize allocations.
        /// Unity Behavior: Modifies the PolygonCollider2D in place. Sets pathCount to originalCount + 1.
        /// The first path becomes the outer rectangle, subsequent paths are reversed original paths (holes).
        /// Edge Cases: If the collider has no paths (pathCount == 0), returns without modification.
        /// Original paths are reversed (Array.Reverse) to invert their winding order.
        /// Algorithm: Creates outer boundary and reverses inner paths to create holes.
        /// </remarks>
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
