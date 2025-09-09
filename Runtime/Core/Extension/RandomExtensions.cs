namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Helper;
    using Random;
    using UnityEngine;

    public static class RandomExtensions
    {
        public static Vector2 NextVector2(this IRandom random, float amplitude)
        {
            return random.NextVector2(-amplitude, amplitude);
        }

        public static T NextEnumExcept<T>(this IRandom random, params T[] exceptions)
            where T : struct, Enum
        {
            T value;
            do
            {
                value = random.NextEnum<T>();
            } while (0 <= Array.IndexOf(exceptions, value));

            return value;
        }

        public static T NextOfExcept<T>(
            this IRandom random,
            IEnumerable<T> values,
            params T[] exceptions
        )
        {
            T value;

            switch (values)
            {
                case IReadOnlyList<T> list:
                {
                    do
                    {
                        value = random.NextOf(list);
                    } while (0 <= Array.IndexOf(exceptions, value));

                    break;
                }
                case IReadOnlyCollection<T> collection:
                {
                    do
                    {
                        value = random.NextOf(collection);
                    } while (0 <= Array.IndexOf(exceptions, value));

                    break;
                }
                default:
                {
                    T[] input = values.ToArray();
                    do
                    {
                        value = random.NextOf(input);
                    } while (0 <= Array.IndexOf(exceptions, value));

                    break;
                }
            }

            return value;
        }

        public static Vector2 NextVector2(
            this IRandom random,
            float minAmplitude,
            float maxAmplitude
        )
        {
            float x = random.NextFloat(minAmplitude, maxAmplitude);
            float y = random.NextFloat(minAmplitude, maxAmplitude);
            return new Vector2(x, y);
        }

        public static Vector2 NextVector2InRange(
            this IRandom random,
            float range,
            Vector2? origin = null
        )
        {
            return Helpers.GetRandomPointInCircle(origin ?? Vector2.zero, range, random);
        }

        public static Vector3 NextVector3(this IRandom random, float amplitude)
        {
            return random.NextVector3(-amplitude, amplitude);
        }

        public static Vector3 NextVector3(
            this IRandom random,
            float minAmplitude,
            float maxAmplitude
        )
        {
            float z = random.NextFloat(minAmplitude, maxAmplitude);
            Vector3 result = random.NextVector2(minAmplitude, maxAmplitude);
            result.z = z;
            return result;
        }
    }
}
