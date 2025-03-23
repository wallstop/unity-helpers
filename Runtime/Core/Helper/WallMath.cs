namespace UnityHelpers.Core.Helper
{
    using System;
    using UnityEngine;

    public static class WallMath
    {
        /**
            http://grepcode.com/file/repository.grepcode.com/java/root/jdk/openjdk/8-b132/java/util/concurrent/ThreadLocalRandom.java#356
            <summary>
                BoundedDouble borrowed from Java's ThreadLocalRandom
            </summary>
        */

        public static double BoundedDouble(double max, double value)
        {
            return value < max
                ? value
                : BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(value) - 1);
        }

        public static float BoundedFloat(float max, float value)
        {
            return value < max
                ? value
                : BitConverter.ToSingle(
                    BitConverter.GetBytes(
                        BitConverter.ToInt32(BitConverter.GetBytes(value), 0) - 1
                    ),
                    0
                );
        }

        public static float PositiveMod(this float value, float max)
        {
            value %= max;
            value += max;
            return value % max;
        }

        public static double PositiveMod(this double value, double max)
        {
            value %= max;
            value += max;
            return value % max;
        }

        public static int PositiveMod(this int value, int max)
        {
            value %= max;
            value += max;
            return value % max;
        }

        public static long PositiveMod(this long value, long max)
        {
            value %= max;
            value += max;
            return value % max;
        }

        public static int WrappedAdd(this int value, int increment, int max)
        {
            WrappedAdd(ref value, increment, max);
            return value;
        }

        public static int WrappedAdd(ref int value, int increment, int max)
        {
            value += increment;
            if (value < max)
            {
                return value;
            }
            return value %= max;
        }

        public static int WrappedIncrement(this int value, int max)
        {
            return WrappedAdd(value, 1, max);
        }

        public static int WrappedIncrement(ref int value, int max)
        {
            return WrappedAdd(ref value, 1, max);
        }

        public static T Clamp<T>(this T value, T min, T max)
            where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0)
            {
                return min;
            }

            return max.CompareTo(value) < 0 ? max : value;
        }

        public static Vector2 Clamp(this Rect bounds, Vector2 point)
        {
            return Clamp(bounds, ref point);
        }

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

        public static bool Approximately(this float lhs, float rhs, float tolerance = 0.045f)
        {
            return Mathf.Abs(lhs - rhs) <= tolerance;
        }
    }
}
