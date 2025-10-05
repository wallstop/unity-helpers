namespace WallstopStudios.UnityHelpers.Core.Math
{
    using System;
    using System.Runtime.Serialization;
    using Extension;
    using ProtoBuf;
    using UnityEngine;

    // https://pastebin.com/iQDhQTFN
    /// <summary>
    /// Represents a line segment defined by two endpoints in 2D space.
    /// </summary>
    [Serializable]
    [DataContract]
    [ProtoContract]
    public readonly struct Line : IEquatable<Line>
    {
        /// <summary>
        /// The starting point of the line segment.
        /// </summary>
        [DataMember]
        [ProtoMember(1)]
        public readonly Vector2 from;

        /// <summary>
        /// The ending point of the line segment.
        /// </summary>
        [DataMember]
        [ProtoMember(2)]
        public readonly Vector2 to;

        /// <summary>
        /// Constructs a line segment from two points.
        /// </summary>
        /// <param name="from">The starting point.</param>
        /// <param name="to">The ending point.</param>
        public Line(Vector2 from, Vector2 to)
        {
            this.from = from;
            this.to = to;
        }

        /// <summary>
        /// Gets the length of the line segment.
        /// </summary>
        public float Length => Vector2.Distance(from, to);

        /// <summary>
        /// Gets the squared length of the line segment (more performant than Length).
        /// </summary>
        public float LengthSquared
        {
            get
            {
                float dx = to.x - from.x;
                float dy = to.y - from.y;
                return dx * dx + dy * dy;
            }
        }

        /// <summary>
        /// Gets the direction vector from 'from' to 'to' (unnormalized).
        /// </summary>
        public Vector2 Direction => to - from;

        /// <summary>
        /// Gets the normalized direction vector from 'from' to 'to'.
        /// </summary>
        public Vector2 NormalizedDirection => (to - from).normalized;

        /// <summary>
        /// Checks if this line segment intersects with another line segment.
        /// </summary>
        /// <param name="other">The other line segment to test.</param>
        /// <returns>True if the segments intersect, false otherwise.</returns>
        public bool Intersects(Line other)
        {
            return UnityExtensions.Intersects(from, to, other.from, other.to);
        }

        /// <summary>
        /// Attempts to find the intersection point between this line segment and another.
        /// </summary>
        /// <param name="other">The other line segment to test.</param>
        /// <param name="intersection">The intersection point if found.</param>
        /// <returns>True if an intersection point exists, false otherwise (including parallel/collinear cases).</returns>
        public bool TryGetIntersectionPoint(Line other, out Vector2 intersection)
        {
            // Direction vectors
            Vector2 d1 = to - from;
            Vector2 d2 = other.to - other.from;

            // Cross product (determinant)
            float determinant = d1.x * d2.y - d1.y * d2.x;

            // Parallel or collinear lines
            if (Mathf.Approximately(determinant, 0))
            {
                intersection = default;
                return false;
            }

            // Vector from this.from to other.from
            Vector2 diff = other.from - from;

            // Parametric t values for both line segments
            float t1 = (diff.x * d2.y - diff.y * d2.x) / determinant;
            float t2 = (diff.x * d1.y - diff.y * d1.x) / determinant;

            // Check if intersection point lies on both segments (t in [0, 1])
            if (t1 >= 0 && t1 <= 1 && t2 >= 0 && t2 <= 1)
            {
                intersection = from + t1 * d1;
                return true;
            }

            intersection = default;
            return false;
        }

        /// <summary>
        /// Calculates the shortest distance from a point to this line segment.
        /// </summary>
        /// <param name="point">The point to measure distance from.</param>
        /// <returns>The shortest distance from the point to the line segment.</returns>
        public float DistanceToPoint(Vector2 point)
        {
            Vector2 closestPoint = ClosestPointOnLine(point);
            return Vector2.Distance(point, closestPoint);
        }

        /// <summary>
        /// Finds the closest point on this line segment to the given point.
        /// </summary>
        /// <param name="point">The point to project onto the line.</param>
        /// <returns>The closest point on the line segment.</returns>
        public Vector2 ClosestPointOnLine(Vector2 point)
        {
            Vector2 dir = to - from;
            float lengthSq = dir.sqrMagnitude;

            // If the line segment has zero length, return 'from'
            if (Mathf.Approximately(lengthSq, 0))
            {
                return from;
            }

            // Calculate projection parameter t
            float t = Vector2.Dot(point - from, dir) / lengthSq;

            // Clamp t to [0, 1] to stay on the segment
            t = Mathf.Clamp01(t);

            return from + t * dir;
        }

        /// <summary>
        /// Checks if a point lies on this line segment (within floating point tolerance).
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>True if the point lies on the line segment, false otherwise.</returns>
        public bool Contains(Vector2 point)
        {
            // Check if point is collinear using cross product
            Vector2 toPoint = point - from;
            Vector2 toEnd = to - from;
            float cross = toPoint.x * toEnd.y - toPoint.y * toEnd.x;

            if (!Mathf.Approximately(cross, 0))
            {
                return false;
            }

            // Check if point is within segment bounds
            return UnityExtensions.LiesOnSegment(from, point, to);
        }

        /// <summary>
        /// Checks if this line is equal to another line.
        /// Two lines are equal if they have the same endpoints (in the same order).
        /// </summary>
        public bool Equals(Line other)
        {
            return from == other.from && to == other.to;
        }

        /// <summary>
        /// Checks if this line is equal to another object.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is Line other && Equals(other);
        }

        /// <summary>
        /// Gets the hash code for this line.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return (from.GetHashCode() * 397) ^ to.GetHashCode();
            }
        }

        /// <summary>
        /// Returns a string representation of this line.
        /// </summary>
        public override string ToString()
        {
            return $"Line(from: {from}, to: {to})";
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(Line left, Line right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(Line left, Line right)
        {
            return !left.Equals(right);
        }
    }
}
