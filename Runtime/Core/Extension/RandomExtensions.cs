namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Helper;
    using Random;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Utils;

    public static class RandomExtensions
    {
        public static Vector2 NextVector2(this IRandom random, float amplitude)
        {
            return random.NextVector2(-amplitude, amplitude);
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

        public static Vector3 NextVector3InRange(
            this IRandom random,
            float range,
            Vector3? origin = null
        )
        {
            return Helpers.GetRandomPointInSphere(origin ?? Vector3.zero, range, random);
        }

        public static Vector3 NextVector3OnSphere(
            this IRandom random,
            float radius,
            Vector3? center = null
        )
        {
            // Marsaglia's method for uniform sphere surface sampling
            float x,
                y,
                z,
                lengthSquared;
            do
            {
                x = random.NextFloat(-1f, 1f);
                y = random.NextFloat(-1f, 1f);
                z = random.NextFloat(-1f, 1f);
                lengthSquared = x * x + y * y + z * z;
            } while (lengthSquared > 1f || lengthSquared < 0.0001f);

            float invLength = radius / Mathf.Sqrt(lengthSquared);
            Vector3 result = new Vector3(x * invLength, y * invLength, z * invLength);
            return center.HasValue ? result + center.Value : result;
        }

        public static Vector3 NextVector3InSphere(
            this IRandom random,
            float radius,
            Vector3? center = null
        )
        {
            return Helpers.GetRandomPointInSphere(center ?? Vector3.zero, radius, random);
        }

        public static Quaternion NextQuaternion(this IRandom random)
        {
            // Uniform random rotation using Shoemake's algorithm
            float u1 = random.NextFloat();
            float u2 = random.NextFloat();
            float u3 = random.NextFloat();

            float sqrt1MinusU1 = Mathf.Sqrt(1f - u1);
            float sqrtU1 = Mathf.Sqrt(u1);

            float twoPiU2 = 2f * Mathf.PI * u2;
            float twoPiU3 = 2f * Mathf.PI * u3;

            return new Quaternion(
                sqrt1MinusU1 * Mathf.Sin(twoPiU2),
                sqrt1MinusU1 * Mathf.Cos(twoPiU2),
                sqrtU1 * Mathf.Sin(twoPiU3),
                sqrtU1 * Mathf.Cos(twoPiU3)
            );
        }

        public static Quaternion NextQuaternionAxisAngle(
            this IRandom random,
            Vector3 axis,
            float minAngle,
            float maxAngle
        )
        {
            float angle = random.NextFloat(minAngle, maxAngle);
            return Quaternion.AngleAxis(angle, axis.normalized);
        }

        public static Quaternion NextQuaternionLookRotation(this IRandom random)
        {
            Vector3 direction = random.NextDirection3D();
            return Quaternion.LookRotation(direction);
        }

        public static Color NextColor(this IRandom random, bool randomAlpha = false)
        {
            float r = random.NextFloat();
            float g = random.NextFloat();
            float b = random.NextFloat();
            float a = randomAlpha ? random.NextFloat() : 1f;
            return new Color(r, g, b, a);
        }

        public static Color NextColorInRange(
            this IRandom random,
            Color baseColor,
            float hueVariance,
            float saturationVariance,
            float valueVariance
        )
        {
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);

            h += random.NextFloat(-hueVariance, hueVariance);
            h = Mathf.Repeat(h, 1f); // Wrap hue to [0, 1]

            s = Mathf.Clamp01(s + random.NextFloat(-saturationVariance, saturationVariance));
            v = Mathf.Clamp01(v + random.NextFloat(-valueVariance, valueVariance));

            return Color.HSVToRGB(h, s, v, true);
        }

        public static Color32 NextColor32(this IRandom random, bool randomAlpha = false)
        {
            byte r = random.NextByte();
            byte g = random.NextByte();
            byte b = random.NextByte();
            byte a = randomAlpha ? random.NextByte() : (byte)255;
            return new Color32(r, g, b, a);
        }

        public static Vector2Int NextVector2Int(this IRandom random, int amplitude)
        {
            return random.NextVector2Int(-amplitude, amplitude);
        }

        public static Vector2Int NextVector2Int(
            this IRandom random,
            int minAmplitude,
            int maxAmplitude
        )
        {
            int x = random.Next(minAmplitude, maxAmplitude);
            int y = random.Next(minAmplitude, maxAmplitude);
            return new Vector2Int(x, y);
        }

        public static Vector2Int NextVector2Int(this IRandom random, Vector2Int min, Vector2Int max)
        {
            int x = random.Next(min.x, max.x);
            int y = random.Next(min.y, max.y);
            return new Vector2Int(x, y);
        }

        public static Vector3Int NextVector3Int(this IRandom random, int amplitude)
        {
            return random.NextVector3Int(-amplitude, amplitude);
        }

        public static Vector3Int NextVector3Int(
            this IRandom random,
            int minAmplitude,
            int maxAmplitude
        )
        {
            int x = random.Next(minAmplitude, maxAmplitude);
            int y = random.Next(minAmplitude, maxAmplitude);
            int z = random.Next(minAmplitude, maxAmplitude);
            return new Vector3Int(x, y, z);
        }

        public static Vector3Int NextVector3Int(this IRandom random, Vector3Int min, Vector3Int max)
        {
            int x = random.Next(min.x, max.x);
            int y = random.Next(min.y, max.y);
            int z = random.Next(min.z, max.z);
            return new Vector3Int(x, y, z);
        }

        public static Vector2 NextDirection2D(this IRandom random)
        {
            float angle = random.NextFloat(0f, 2f * Mathf.PI);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        public static Vector3 NextDirection3D(this IRandom random)
        {
            return random.NextVector3OnSphere(1f, Vector3.zero);
        }

        public static float NextAngle(this IRandom random, float min = 0f, float max = 360f)
        {
            return random.NextFloat(min, max);
        }

        public static Vector2 NextVector2InRect(this IRandom random, Rect rect)
        {
            float x = random.NextFloat(rect.xMin, rect.xMax);
            float y = random.NextFloat(rect.yMin, rect.yMax);
            return new Vector2(x, y);
        }

        public static Vector3 NextVector3InBounds(this IRandom random, Bounds bounds)
        {
            float x = random.NextFloat(bounds.min.x, bounds.max.x);
            float y = random.NextFloat(bounds.min.y, bounds.max.y);
            float z = random.NextFloat(bounds.min.z, bounds.max.z);
            return new Vector3(x, y, z);
        }

        public static T NextWeighted<T>(
            this IRandom random,
            IEnumerable<(T item, float weight)> weighted
        )
        {
            IReadOnlyList<(T, float)> items =
                weighted as IReadOnlyList<(T, float)> ?? weighted.ToArray();
            if (items.Count == 0)
            {
                throw new ArgumentException(
                    "Weighted collection cannot be empty",
                    nameof(weighted)
                );
            }

            float totalWeight = 0f;
            foreach ((T _, float weight) in items)
            {
                if (weight < 0f)
                {
                    throw new ArgumentException("Weights cannot be negative", nameof(weighted));
                }

                totalWeight += weight;
            }

            if (totalWeight <= 0f)
            {
                throw new ArgumentException(
                    "Total weight must be greater than zero",
                    nameof(weighted)
                );
            }

            float randomValue = random.NextFloat(0f, totalWeight);
            float cumulative = 0f;

            foreach ((T item, float weight) in items)
            {
                cumulative += weight;
                if (randomValue < cumulative)
                {
                    return item;
                }
            }

            // Fallback due to floating point precision
            return items[^1].Item1;
        }

        public static int NextWeightedIndex(this IRandom random, float[] weights)
        {
            if (weights == null || weights.Length == 0)
            {
                throw new ArgumentException(
                    "Weights array cannot be null or empty",
                    nameof(weights)
                );
            }

            float totalWeight = 0f;
            foreach (float weight in weights)
            {
                if (weight < 0f)
                {
                    throw new ArgumentException("Weights cannot be negative", nameof(weights));
                }

                totalWeight += weight;
            }

            if (totalWeight <= 0f)
            {
                throw new ArgumentException(
                    "Total weight must be greater than zero",
                    nameof(weights)
                );
            }

            float randomValue = random.NextFloat(0f, totalWeight);
            float cumulative = 0f;

            for (int i = 0; i < weights.Length; ++i)
            {
                cumulative += weights[i];
                if (randomValue < cumulative)
                {
                    return i;
                }
            }

            // Fallback due to floating point precision
            return weights.Length - 1;
        }

        public static bool NextBool(this IRandom random, float probability)
        {
            if (probability < 0f || probability > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(probability),
                    "Probability must be between 0 and 1"
                );
            }

            return random.NextFloat() < probability;
        }

        public static int NextSign(this IRandom random)
        {
            return random.NextBool() ? 1 : -1;
        }

        public static float NextFloatAround(this IRandom random, float center, float variance)
        {
            return random.NextFloat(center - variance, center + variance);
        }

        public static int NextIntAround(this IRandom random, int center, int variance)
        {
            return random.Next(center - variance, center + variance + 1);
        }

        public static IEnumerable<T> NextSubset<T>(
            this IRandom random,
            IEnumerable<T> items,
            int count
        )
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative");
            }

            IReadOnlyList<T> itemsList = items as IReadOnlyList<T> ?? items.ToArray();
            if (count > itemsList.Count)
            {
                throw new ArgumentException(
                    "Count cannot exceed the number of items",
                    nameof(count)
                );
            }

            if (count == 0)
            {
                return Enumerable.Empty<T>();
            }

            using PooledResource<HashSet<int>> hashSetBuffer = Buffers<int>.HashSet.Get(
                out HashSet<int> selectedIndices
            );
            while (selectedIndices.Count < count)
            {
                selectedIndices.Add(random.Next(0, itemsList.Count));
            }

            return selectedIndices.Select(index => itemsList[index]);
        }
    }
}
