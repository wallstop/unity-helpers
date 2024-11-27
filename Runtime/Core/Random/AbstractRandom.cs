namespace UnityHelpers.Core.Random
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using DataStructure.Adapters;
    using UnityEngine;

    [Serializable]
    [DataContract]
    public abstract class AbstractRandom : IRandom
    {
        private static readonly ConcurrentDictionary<Type, Array> EnumTypeCache = new();

        protected const uint HalfwayUint = uint.MaxValue / 2;
        protected const float MagicFloat = 5.960465E-008F;

        protected double? _cachedGaussian;

        public abstract RandomState InternalState { get; }

        public virtual int Next()
        {
            // Mask out the MSB to ensure the value is within [0, int.MaxValue]
            return unchecked((int)NextUint() & 0x7FFFFFFF);
        }

        public int Next(int max)
        {
            if (max <= 0)
            {
                throw new ArgumentException($"Max {max} cannot be less-than or equal-to 0");
            }

            return unchecked((int)NextUint(unchecked((uint)max)));
        }

        public int Next(int min, int max)
        {
            if (max <= min)
            {
                throw new ArgumentException(
                    $"Min {min} cannot be larger-than or equal-to max {max}"
                );
            }

            uint range = (uint)(max - min);
            if (range == 0)
            {
                return unchecked((int)NextUint());
            }

            return unchecked((int)(min + NextUint(range)));
        }

        // Internal sampler
        public abstract uint NextUint();

        public uint NextUint(uint max)
        {
            if (max == 0)
            {
                throw new ArgumentException("Max cannot be zero");
            }

            return (uint)(NextDouble() * max);
        }

        public uint NextUint(uint min, uint max)
        {
            if (max <= min)
            {
                throw new ArgumentException(
                    $"Min {min} cannot be larger-than or equal-to max {max}"
                );
            }

            return min + NextUint(max - min);
        }

        public short NextShort()
        {
            return NextShort(short.MaxValue);
        }

        public short NextShort(short max)
        {
            return NextShort(0, max);
        }

        public short NextShort(short min, short max)
        {
            return unchecked((short)Next(min, max));
        }

        public byte NextByte()
        {
            return NextByte(byte.MaxValue);
        }

        public byte NextByte(byte max)
        {
            return NextByte(0, max);
        }

        public byte NextByte(byte min, byte max)
        {
            return unchecked((byte)Next(min, max));
        }

        public long NextLong()
        {
            uint upper = NextUint();
            uint lower = NextUint();
            unchecked
            {
                return (long)((((ulong)upper << 32) | lower) & 0x7FFFFFFFFFFFFFFF);
            }
        }

        public long NextLong(long max)
        {
            if (max <= 0)
            {
                throw new ArgumentException($"Max {max} cannot be less-than or equal-to 0");
            }

            return (long)(NextDouble() * max);
        }

        public long NextLong(long min, long max)
        {
            if (max <= min)
            {
                throw new ArgumentException(
                    $"Min {min} cannot be larger-than or equal-to Max {max}"
                );
            }

            ulong range = (ulong)(max - min);
            if (range == 0)
            {
                return unchecked((long)NextUlong());
            }

            return unchecked((long)(NextDouble() * range + min));
        }

        public ulong NextUlong()
        {
            uint upper = NextUint();
            uint lower = NextUint();
            return ((ulong)upper << 32) | lower;
        }

        public ulong NextUlong(ulong max)
        {
            return (ulong)(NextDouble() * max);
        }

        public ulong NextUlong(ulong min, ulong max)
        {
            if (max <= min)
            {
                throw new ArgumentException(
                    $"Min {min} cannot be larger-than or equal-to max {max}"
                );
            }

            return NextUlong(max - min) + min;
        }

        public virtual bool NextBool()
        {
            return NextUint() < HalfwayUint;
        }

        public void NextBytes(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentException(nameof(buffer));
            }

            const int sizeOfInt = 4; // May differ on some platforms

            // See how many ints we can slap into it.
            int chunks = buffer.Length / sizeOfInt;
            int spare = buffer.Length - chunks * sizeOfInt;
            for (int i = 0; i < chunks; ++i)
            {
                int offset = i * sizeOfInt;
                uint random = NextUint();
                for (int j = 0; j < sizeOfInt; ++j)
                {
                    buffer[offset + j] = unchecked(
                        (byte)((random >> (j * sizeOfInt)) & 0x000000FF)
                    );
                }
            }

            if (0 < spare)
            {
                uint spareRandom = NextUint();
                for (int i = 0; i < spare; ++i)
                {
                    buffer[buffer.Length - 1 - i] = unchecked(
                        (byte)((spareRandom >> (i * sizeOfInt)) & 0x000000FF)
                    );
                }
            }
        }

        public virtual double NextDouble()
        {
            double value;
            do
            {
                value = NextUint() * (1.0 / uint.MaxValue);
            } while (1.0 <= value);

            return value;
        }

        public double NextDouble(double max)
        {
            if (max <= 0)
            {
                throw new ArgumentException($"Max {max} cannot be less-than or equal-to 0");
            }

            return NextDouble() * max;
        }

        public double NextDouble(double min, double max)
        {
            if (max <= min)
            {
                throw new ArgumentException(
                    $"Min {min} cannot be larger-than or equal-to max {max}"
                );
            }

            double range = max - min;
            if (double.IsInfinity(range))
            {
                return NextDoubleWithInfiniteRange(min, max);
            }

            return min + NextDouble() * range;
        }

        protected double NextDoubleWithInfiniteRange(double min, double max)
        {
            double random;
            do
            {
                random = NextDoubleFullRange();
            } while (random < min || max <= random);

            return random;
        }

        protected double NextDoubleFullRange()
        {
            double value = double.NaN;
            do
            {
                ulong randomBits = NextUlong();

                // Extract exponent (bits 52-62)
                const ulong exponentMask = 0x7FF0000000000000;

                ulong exponent = (randomBits & exponentMask) >> 52;

                // Ensure exponent is not all 1's to avoid Inf and NaN
                if (exponent == 0x7FF)
                {
                    continue; // Regenerate
                }

                /*
                    For uniform distribution over all finite doubles, no further masking is necessary,
                    reassemble the bits
                 */
                value = BitConverter.Int64BitsToDouble(unchecked((long)randomBits));
            } while (double.IsInfinity(value) || double.IsNaN(value));

            return value;
        }

        public double NextGaussian(double mean = 0, double stdDev = 1)
        {
            return mean + NextGaussianInternal() * stdDev;
        }

        private double NextGaussianInternal()
        {
            if (_cachedGaussian != null)
            {
                double gaussian = _cachedGaussian.Value;
                _cachedGaussian = null;
                return gaussian;
            }

            // https://stackoverflow.com/q/7183229/1917135
            double x;
            double y;
            double square;
            do
            {
                x = 2 * NextDouble() - 1;
                y = 2 * NextDouble() - 1;
                square = x * x + y * y;
            } while (square is 0 or > 1);

            double fac = Math.Sqrt(-2 * Math.Log(square) / square);
            _cachedGaussian = x * fac;
            return y * fac;
        }

        public virtual float NextFloat()
        {
            float value;
            do
            {
                value = NextUint() / (1f * uint.MaxValue);
            } while (1f <= value);

            return value;
        }

        public float NextFloat(float max)
        {
            if (max <= 0)
            {
                throw new ArgumentException($"{max} cannot be less-than or equal-to 0");
            }

            return NextFloat() * max;
        }

        public float NextFloat(float min, float max)
        {
            if (max <= min)
            {
                throw new ArgumentException(
                    $"Min {min} cannot be larger-than or equal-to max {max}"
                );
            }

            float range = max - min;
            if (float.IsInfinity(range))
            {
                return (float)NextDouble(min, max);
            }

            return min + NextFloat(range);
        }

        public T NextOf<T>(IEnumerable<T> enumerable)
        {
            return enumerable switch
            {
                IReadOnlyList<T> list => NextOf(list),
                IReadOnlyCollection<T> collection => NextOf(collection),
                null => throw new ArgumentNullException(nameof(enumerable)),
                _ => NextOf(enumerable.ToArray()),
            };
        }

        public T NextOf<T>(IReadOnlyCollection<T> collection)
        {
            if (collection is not { Count: > 0 })
            {
                throw new ArgumentException("Collection cannot be empty");
            }

            if (collection is IReadOnlyList<T> list)
            {
                return NextOf(list);
            }

            int index = Next(collection.Count);
            return collection.ElementAt(index);
        }

        public T NextOf<T>(IReadOnlyList<T> list)
        {
            if (list is not { Count: > 0 })
            {
                throw new ArgumentNullException(nameof(list));
            }

            /*
                For small lists, it's much more efficient to simply return one of their elements
                instead of trying to generate a random number within bounds (which is implemented as a while(true) loop)
             */
            return list.Count switch
            {
                1 => list[0],
                2 => NextBool() ? list[0] : list[1],
                _ => list[Next(list.Count)],
            };
        }

        public T NextEnum<T>()
            where T : struct, Enum
        {
            Type enumType = typeof(T);
            T[] enumValues = (T[])EnumTypeCache.GetOrAdd(enumType, Enum.GetValues);

            return RandomOf(enumValues);
        }

        public Guid NextGuid()
        {
            byte[] guidBytes = new byte[16];
            NextBytes(guidBytes);
            return new Guid(guidBytes);
        }

        public KGuid NextKGuid()
        {
            byte[] guidBytes = new byte[16];
            NextBytes(guidBytes);
            return new KGuid(guidBytes);
        }

        // Advances the RNG
        // https://code2d.wordpress.com/2020/07/21/perlin-noise/
        public float[,] NextNoiseMap(
            int width,
            int height,
            PerlinNoise noise = null,
            float scale = 2.5f,
            int octaves = 8
        )
        {
            if (width <= 0)
            {
                throw new ArgumentException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentException(nameof(height));
            }

            if (scale <= 0)
            {
                throw new ArgumentException(nameof(scale));
            }

            if (octaves < 1)
            {
                throw new ArgumentException(nameof(octaves));
            }

            noise ??= PerlinNoise.Instance;
            float[,] noiseMap = new float[width, height];

            Vector2[] octaveOffsets = new Vector2[octaves];
            for (int i = 0; i < octaves; i++)
            {
                float offsetX = Next(-100000, 100000);
                float offsetY = Next(-100000, 100000);
                octaveOffsets[i] = new Vector2(offsetX, offsetY);
            }

            float maxNoiseHeight = float.MinValue;
            float minNoiseHeight = float.MaxValue;

            float halfWidth = width / 2f;
            float halfHeight = height / 2f;

            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;
                    for (int i = 0; i < octaves; i++)
                    {
                        float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x;
                        float sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].y;

                        float perlinValue = noise.Noise(sampleX, sampleY) * 2 - 1;
                        noiseHeight += perlinValue * amplitude;
                    }

                    if (noiseHeight > maxNoiseHeight)
                    {
                        maxNoiseHeight = noiseHeight;
                    }
                    else if (noiseHeight < minNoiseHeight)
                    {
                        minNoiseHeight = noiseHeight;
                    }

                    noiseMap[x, y] = noiseHeight;
                }
            }

            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    // Returns a value between 0f and 1f based on noiseMap value
                    // minNoiseHeight being 0f, and maxNoiseHeight being 1f
                    noiseMap[x, y] = Mathf.InverseLerp(
                        minNoiseHeight,
                        maxNoiseHeight,
                        noiseMap[x, y]
                    );
                }
            }
            return noiseMap;
        }

        protected T RandomOf<T>(T[] values)
        {
            return values.Length switch
            {
                0 => default,
                1 => values[0],
                2 => NextBool() ? values[0] : values[1],
                _ => values[Next(values.Length)],
            };
        }

        public abstract IRandom Copy();
    }
}
