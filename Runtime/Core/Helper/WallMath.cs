namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using UnityEngine;

    public static class WallMath
    {
        /// <summary>
        /// Ensures a double value is strictly less than the specified maximum by decrementing
        /// its bit representation if necessary. Borrowed from Java's ThreadLocalRandom.
        /// </summary>
        /// <param name="max">The exclusive upper bound</param>
        /// <param name="value">The value to bound</param>
        /// <returns>A value strictly less than max</returns>
        /// <remarks>
        /// Reference: http://grepcode.com/file/repository.grepcode.com/java/root/jdk/openjdk/8-b132/java/util/concurrent/ThreadLocalRandom.java#356
        /// </remarks>
        public static double BoundedDouble(double max, double value)
        {
            return value < max
                ? value
                : BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(value) - 1);
        }

        /// <summary>
        /// Ensures a float value is strictly less than the specified maximum by decrementing
        /// its bit representation if necessary.
        /// </summary>
        /// <param name="max">The exclusive upper bound</param>
        /// <param name="value">The value to bound</param>
        /// <returns>A value strictly less than max</returns>
        public static float BoundedFloat(float max, float value)
        {
            if (value < max)
            {
                return value;
            }

            int bits = BitConverter.SingleToInt32Bits(value);
            return BitConverter.Int32BitsToSingle(bits - 1);
        }

        /// <summary>
        /// Computes a positive modulo operation that always returns a non-negative result.
        /// Unlike the % operator which can return negative values, this ensures the result is in [0, max).
        /// </summary>
        /// <param name="value">The value to compute modulo for</param>
        /// <param name="max">The modulo divisor (must be positive)</param>
        /// <returns>A value in the range [0, max)</returns>
        public static float PositiveMod(this float value, float max)
        {
            value %= max;
            value += max;
            return value % max;
        }

        /// <summary>
        /// Computes a positive modulo operation that always returns a non-negative result.
        /// Unlike the % operator which can return negative values, this ensures the result is in [0, max).
        /// </summary>
        /// <param name="value">The value to compute modulo for</param>
        /// <param name="max">The modulo divisor (must be positive)</param>
        /// <returns>A value in the range [0, max)</returns>
        public static double PositiveMod(this double value, double max)
        {
            value %= max;
            value += max;
            return value % max;
        }

        /// <summary>
        /// Computes a positive modulo operation that always returns a non-negative result.
        /// Unlike the % operator which can return negative values, this ensures the result is in [0, max).
        /// </summary>
        /// <param name="value">The value to compute modulo for</param>
        /// <param name="max">The modulo divisor (must be positive)</param>
        /// <returns>A value in the range [0, max)</returns>
        public static int PositiveMod(this int value, int max)
        {
            value %= max;
            value += max;
            return value % max;
        }

        /// <summary>
        /// Computes a positive modulo operation that always returns a non-negative result.
        /// Unlike the % operator which can return negative values, this ensures the result is in [0, max).
        /// </summary>
        /// <param name="value">The value to compute modulo for</param>
        /// <param name="max">The modulo divisor (must be positive)</param>
        /// <returns>A value in the range [0, max)</returns>
        public static long PositiveMod(this long value, long max)
        {
            value %= max;
            value += max;
            return value % max;
        }

        /// <summary>
        /// Adds an increment to a value and wraps around using modulo if it exceeds the maximum.
        /// This is a non-mutating version that returns the result without modifying the input.
        /// </summary>
        /// <param name="value">The base value</param>
        /// <param name="increment">The amount to add (can be negative)</param>
        /// <param name="max">The wrap-around boundary</param>
        /// <returns>The wrapped result in the range [0, max)</returns>
        public static int WrappedAdd(this int value, int increment, int max)
        {
            WrappedAdd(ref value, increment, max);
            return value;
        }

        /// <summary>
        /// Adds an increment to a value and wraps around using modulo if it exceeds the maximum.
        /// This mutates the value parameter in place.
        /// </summary>
        /// <param name="value">The base value (modified in place)</param>
        /// <param name="increment">The amount to add (can be negative)</param>
        /// <param name="max">The wrap-around boundary</param>
        /// <returns>The wrapped result in the range [0, max)</returns>
        public static int WrappedAdd(ref int value, int increment, int max)
        {
            value += increment;
            if (value >= 0 && value < max)
            {
                return value;
            }
            return value = value.PositiveMod(max);
        }

        /// <summary>
        /// Increments a value by 1 and wraps around if it reaches the maximum.
        /// This is a non-mutating version that returns the result without modifying the input.
        /// </summary>
        /// <param name="value">The value to increment</param>
        /// <param name="max">The wrap-around boundary</param>
        /// <returns>The incremented value, wrapped to [0, max)</returns>
        public static int WrappedIncrement(this int value, int max)
        {
            return WrappedAdd(value, 1, max);
        }

        /// <summary>
        /// Increments a value by 1 and wraps around if it reaches the maximum.
        /// This mutates the value parameter in place.
        /// </summary>
        /// <param name="value">The value to increment (modified in place)</param>
        /// <param name="max">The wrap-around boundary</param>
        /// <returns>The incremented value, wrapped to [0, max)</returns>
        public static int WrappedIncrement(ref int value, int max)
        {
            return WrappedAdd(ref value, 1, max);
        }

        /// <summary>
        /// Clamps a value between a minimum and maximum using generic comparison.
        /// Works with any type that implements IComparable.
        /// </summary>
        /// <typeparam name="T">The type being clamped (must implement IComparable)</typeparam>
        /// <param name="value">The value to clamp</param>
        /// <param name="min">The minimum allowed value</param>
        /// <param name="max">The maximum allowed value</param>
        /// <returns>The clamped value in the range [min, max]</returns>
        public static T Clamp<T>(this T value, T min, T max)
            where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0)
            {
                return min;
            }

            return max.CompareTo(value) < 0 ? max : value;
        }

        /// <summary>
        /// Clamps a point to the nearest position inside or on the boundary of a rectangle.
        /// If the point is outside, it finds the closest point on the rectangle's edge.
        /// </summary>
        /// <param name="bounds">The bounding rectangle</param>
        /// <param name="point">The point to clamp</param>
        /// <returns>The clamped point within the rectangle</returns>
        public static Vector2 Clamp(this Rect bounds, Vector2 point)
        {
            return Clamp(bounds, ref point);
        }

        /// <summary>
        /// Clamps a point to the nearest position inside or on the boundary of a rectangle.
        /// If the point is outside, it finds the closest point on the rectangle's edge.
        /// This version modifies the point parameter in place.
        /// </summary>
        /// <param name="bounds">The bounding rectangle</param>
        /// <param name="point">The point to clamp (modified in place)</param>
        /// <returns>The clamped point within the rectangle</returns>
        public static Vector2 Clamp(this Rect bounds, ref Vector2 point)
        {
            if (bounds.Contains(point))
            {
                return point;
            }

            Vector2 center = bounds.center;
            Vector2 direction = point - center;

            if (direction == Vector2.zero)
            {
                return center;
            }

            float tMax = float.MaxValue;
            Vector2 min = bounds.min;
            Vector2 max = bounds.max;

            if (direction.x != 0)
            {
                if (0 < direction.x)
                {
                    float t2 = (max.x - center.x) / direction.x;
                    tMax = Mathf.Min(tMax, t2);
                }
                else
                {
                    float t1 = (min.x - center.x) / direction.x;
                    tMax = Mathf.Min(tMax, t1);
                }
            }

            if (direction.y != 0)
            {
                if (direction.y > 0)
                {
                    float t2 = (max.y - center.y) / direction.y;
                    tMax = Mathf.Min(tMax, t2);
                }
                else
                {
                    float t1 = (min.y - center.y) / direction.y;
                    tMax = Mathf.Min(tMax, t1);
                }
            }

            tMax = Mathf.Clamp01(tMax);

            point = center + direction * tMax;
            point = new Vector2(
                Mathf.Clamp(point.x, min.x, max.x),
                Mathf.Clamp(point.y, min.y, max.y)
            );
            return point;
        }

        /// <summary>
        /// Checks if two float values are approximately equal within a specified tolerance.
        /// Uses absolute difference comparison with an epsilon-scaled cushion to handle rounding.
        /// </summary>
        /// <param name="lhs">The first value</param>
        /// <param name="rhs">The second value</param>
        /// <param name="tolerance">The maximum allowed difference (default 0.045)</param>
        /// <returns>True if the absolute difference is less than or equal to tolerance plus the floating-point cushion</returns>
        public static bool Approximately(this float lhs, float rhs, float tolerance = 0.045f)
        {
            if (float.IsNaN(lhs) || float.IsNaN(rhs))
            {
                return false;
            }

            if (float.IsInfinity(lhs) || float.IsInfinity(rhs))
            {
                return false;
            }

            float difference = Mathf.Abs(lhs - rhs);
            if (float.IsNaN(difference) || float.IsInfinity(difference))
            {
                return false;
            }

            float absTolerance = Mathf.Abs(tolerance);
            float maxMagnitude = Mathf.Max(Mathf.Abs(lhs), Mathf.Abs(rhs));
            float fudge = Mathf.Max(1e-6f * maxMagnitude, Mathf.Epsilon * 8f);

            return difference <= absTolerance + fudge;
        }

        /// <summary>
        /// Checks if two double values are approximately equal within a specified tolerance.
        /// Uses absolute difference comparison with an epsilon-scaled cushion to handle rounding.
        /// </summary>
        /// <param name="lhs">The first value</param>
        /// <param name="rhs">The second value</param>
        /// <param name="tolerance">The maximum allowed difference (default 0.045)</param>
        /// <returns>True if the absolute difference is less than or equal to tolerance plus the floating-point cushion</returns>
        public static bool Approximately(this double lhs, double rhs, double tolerance = 0.045f)
        {
            if (double.IsNaN(lhs) || double.IsNaN(rhs))
            {
                return false;
            }

            if (double.IsInfinity(lhs) || double.IsInfinity(rhs))
            {
                return false;
            }

            double difference = Math.Abs(lhs - rhs);
            if (double.IsNaN(difference) || double.IsInfinity(difference))
            {
                return false;
            }

            double absTolerance = Math.Abs(tolerance);
            double maxMagnitude = Math.Max(Math.Abs(lhs), Math.Abs(rhs));
            double fudge = Math.Max(1e-12d * maxMagnitude, double.Epsilon * 8d);

            return difference <= absTolerance + fudge;
        }
    }
}
