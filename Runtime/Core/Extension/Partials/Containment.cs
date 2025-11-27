// ReSharper disable once CheckNamespace
namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    public static partial class UnityExtensions
    {
        public static bool FastIntersects(this Bounds bounds, Bounds other)
        {
            // Degenerate bounds (zero volume) do not intersect
            Vector3 sizeA = bounds.size;
            Vector3 sizeB = other.size;
            if (
                sizeA.x <= 0f
                || sizeA.y <= 0f
                || sizeA.z <= 0f
                || sizeB.x <= 0f
                || sizeB.y <= 0f
                || sizeB.z <= 0f
            )
            {
                return false;
            }
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
        /// Bounds that touch at an edge are considered intersecting (inclusive). Z axis is ignored.
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
        /// Edge Cases: Bounds that touch at edges are considered intersecting (inclusive). Z axis is ignored.
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

        // =========================
        // 3D Bounds helpers (opt-in tolerance)
        // =========================

        /// <summary>
        /// Fast 3D point containment with optional tolerance and half-open semantics [min, max).
        /// A point on the max face is NOT contained.
        /// </summary>
        public static bool FastContains3D(this Bounds bounds, Vector3 p, float tolerance = 0f)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            return p.x >= min.x - tolerance
                && p.x < max.x + tolerance
                && p.y >= min.y - tolerance
                && p.y < max.y + tolerance
                && p.z >= min.z - tolerance
                && p.z < max.z + tolerance;
        }

        /// <summary>
        /// Fast 3D containment test (box in box) with optional tolerance and inclusive semantics on max faces.
        /// Returns true if 'other' is fully inside or touching 'bounds' (with tolerance).
        /// </summary>
        public static bool FastContains3D(this Bounds bounds, Bounds other, float tolerance = 0f)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector3 omin = other.min;
            Vector3 omax = other.max;
            if (
                omin.x < min.x - tolerance
                || omin.y < min.y - tolerance
                || omin.z < min.z - tolerance
            )
            {
                return false;
            }
            return omax.x <= max.x + tolerance
                && omax.y <= max.y + tolerance
                && omax.z <= max.z + tolerance;
        }

        // =========================
        // 3D Bounds helpers (opt-in tolerance)
        // =========================

        /// <summary>
        /// Fast 3D bounds intersection with optional tolerance.
        /// Touching at faces is considered intersection (inclusive at boundaries).
        /// </summary>
        public static bool FastIntersects3D(this Bounds a, Bounds b, float tolerance = 0f)
        {
            // Degenerate bounds (zero volume) do not intersect
            Vector3 asize = a.size;
            Vector3 bsize = b.size;
            if (
                asize.x <= 0f
                || asize.y <= 0f
                || asize.z <= 0f
                || bsize.x <= 0f
                || bsize.y <= 0f
                || bsize.z <= 0f
            )
            {
                return false;
            }
            Vector3 amin = a.min;
            Vector3 bmax = b.max;
            if (
                bmax.x < amin.x - tolerance
                || bmax.y < amin.y - tolerance
                || bmax.z < amin.z - tolerance
            )
            {
                return false;
            }

            Vector3 amax = a.max;
            Vector3 bmin = b.min;
            return amax.x + tolerance >= bmin.x
                && amax.y + tolerance >= bmin.y
                && amax.z + tolerance >= bmin.z;
        }

        /// <summary>
        /// Fast 3D containment test (box in box) with optional tolerance and half-open semantics on max faces.
        /// Returns true only if 'other' is fully inside 'bounds' and does NOT touch the max faces.
        /// Equivalent to: other.min >= bounds.min and other.max < bounds.max (with tolerance).
        /// </summary>
        public static bool FastContainsHalfOpen3D(
            this Bounds bounds,
            Bounds other,
            float tolerance = 0f
        )
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector3 omin = other.min;
            Vector3 omax = other.max;
            if (
                omin.x < min.x - tolerance
                || omin.y < min.y - tolerance
                || omin.z < min.z - tolerance
            )
            {
                return false;
            }
            return omax.x < max.x - tolerance
                && omax.y < max.y - tolerance
                && omax.z < max.z - tolerance;
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
    }
}
