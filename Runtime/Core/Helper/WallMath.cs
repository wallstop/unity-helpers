namespace Core.Helper
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
            return value < max ? value : BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(value) - 1);
        }

        public static float BoundedFloat(float max, float value)
        {
            return value < max
                ? value
                : BitConverter.ToSingle(
                    BitConverter.GetBytes(BitConverter.ToInt32(BitConverter.GetBytes(value), 0) - 1), 0);
        }

        public static int WrappedAdd(int value, int increment, int max)
        {
            WrappedAdd(ref value, increment, max);
            return value;
        }

        public static void WrappedAdd(ref int value, int increment, int max)
        {
            value = value + increment;
            if (value < max)
            {
                return;
            }
            value %= max;
        }

        public static int WrappedIncrement(int value, int max)
        {
            return WrappedAdd(value, 1, max);
        }

        public static void WrappedIncrement(ref int value, int max)
        {
            WrappedAdd(ref value, 1, max);
        }

        public static void Clamp(this Rect bounds, ref Vector2 point)
        {
            if (!bounds.Contains(point))
            {
                Vector2 xClamp = bounds.width < 0 ?
                    new Vector2(bounds.x + bounds.width, bounds.x) :
                    new Vector2(bounds.x, bounds.x + bounds.width);
                Vector2 yClamp = bounds.height < 0 ?
                    new Vector2(bounds.y + bounds.height, bounds.y) :
                    new Vector2(bounds.y, bounds.y + bounds.height);

                point.x = Mathf.Clamp(point.x, xClamp.x, xClamp.y);
                point.y = Mathf.Clamp(point.y, yClamp.x, yClamp.y);
            }
        }

        public static bool Approximately(float lhs, float rhs, float tolerance = 0.045f)
        {
            return Math.Abs(lhs - rhs) <= tolerance;
        }
    }
}
