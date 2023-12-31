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
        protected const double MagicDouble = 4.6566128752458E-10;
        protected const float MagicFloat = 5.960465E-008F;

        protected double? _cachedGaussian;

        protected AbstractRandom() { }

        public abstract RandomState InternalState { get; }

        public int Next()
        {
            int result;
            do
            {
                result = unchecked((int)NextUint());
            }
            while (result < 0);

            return result;
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
                throw new ArgumentException($"Min {min} cannot be larger-than or equal-to max {max}");
            }

            uint range = unchecked((uint)(max - min));
            return unchecked((int)NextUint(range)) + min;
        }

        // Internal sampler
        public abstract uint NextUint();

        public uint NextUint(uint max)
        {
            /*
                https://github.com/libevent/libevent/blob/3807a30b03ab42f2f503f2db62b1ef5876e2be80/arc4random.c#L531

                http://cs.stackexchange.com/questions/570/generating-uniformly-distributed-random-numbers-using-a-coin
                Generates a uniform random number within the bound, avoiding modulo bias
            */
            uint threshold = unchecked((uint)((0x100000000UL - max) % max));
            while (true)
            {
                uint randomValue = NextUint();
                if (threshold <= randomValue)
                {
                    return randomValue % max;
                }
            }
        }

        public uint NextUint(uint min, uint max)
        {
            if (max <= min)
            {
                throw new ArgumentException($"Min {min} cannot be larger-than or equal-to max {max}");
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
            // Mix things up a little
            if (NextBool())
            {
                return unchecked((long)((ulong)upper << 32) | lower);
            }
            return unchecked((long)((ulong)lower << 32) | upper);
        }

        public long NextLong(long max)
        {
            if (max <= 0)
            {
                throw new ArgumentException($"Max {max} cannot be less-than or equal-to 0");
            }

            if (max < int.MaxValue)
            {
                return Next(unchecked((int)max));
            }

            long withinRange;
            do
            {
                withinRange = NextLong();
            }
            while (withinRange < 0 || max <= withinRange);
            return withinRange;
        }

        public long NextLong(long min, long max)
        {
            if (max <= min)
            {
                throw new ArgumentException($"Min {min} cannot be larger-than or equal-to Max {max}");
            }

            return min + NextLong(max - min);
        }

        public ulong NextUlong()
        {
            return unchecked((ulong)NextLong());
        }

        public ulong NextUlong(ulong max)
        {
            return unchecked((ulong)NextLong(unchecked((long)max)));
        }

        public ulong NextUlong(ulong min, ulong max)
        {
            if (max <= min)
            {
                throw new ArgumentException($"Min {min} cannot be larger-than or equal-to max {max}");
            }

            return unchecked((ulong)NextLong(unchecked((long)min), unchecked((long)max)));
        }

        public bool NextBool()
        {
            return NextUint() < HalfwayUint;
        }

        public void NextBytes(byte[] buffer)
        {
            if (ReferenceEquals(buffer, null))
            {
                throw new ArgumentException(nameof(buffer));
            }

            const byte sizeOfInt = 4; // May differ on some platforms

            // See how many ints we can slap into it.
            int chunks = buffer.Length / sizeOfInt;
            byte spare = unchecked((byte)(buffer.Length - (chunks * sizeOfInt)));
            for (int i = 0; i < chunks; ++i)
            {
                int offset = i * chunks;
                int random = Next();
                buffer[offset] = unchecked((byte)(random & 0xFF000000));
                buffer[offset + 1] = unchecked((byte)(random & 0x00FF0000));
                buffer[offset + 2] = unchecked((byte)(random & 0x0000FF00));
                buffer[offset + 3] = unchecked((byte)(random & 0x000000FF));
            }

            {
                /*
                    This could be implemented more optimally by generating a single int and
                    bit shifting along the position, but that is too much for me right now.
                 */
                for (byte i = 0; i < spare; ++i)
                {
                    buffer[buffer.Length - 1 - i] = unchecked((byte)Next());
                }
            }
        }

        public virtual double NextDouble()
        {
            double value;
            do
            {
                value = NextUint() * MagicDouble;
            }
            while (value < 0 || 1 <= value);

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
                throw new ArgumentException($"Min {min} cannot be larger-than or equal-to max {max}");
            }

            double range = max - min;
            return min + NextDouble(range);
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
            }
            while (square > 1 || square == 0);

            double fac = Math.Sqrt(-2 * Math.Log(square) / square);
            _cachedGaussian = x * fac;
            return y * fac;
        }

        public virtual float NextFloat()
        {
            float value;
            do
            {
                uint floatAsInt = NextUint();
                value = (floatAsInt >> 8) * MagicFloat;
            }
            while (value < 0 || 1 <= value);

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
                throw new ArgumentException($"Min {min} cannot be larger-than or equal-to max {max}");
            }

            return min + NextFloat(max - min);
        }

        public T Next<T>(IEnumerable<T> enumerable)
        {
            if (enumerable is ICollection<T> collection)
            {
                return Next(collection);
            }

            return Next((IReadOnlyList<T>)enumerable.ToList());
        }

        public T Next<T>(ICollection<T> collection)
        {
            int count = collection.Count;
            if (count <= 0)
            {
                throw new ArgumentException("Collection size cannot be less-than or equal-to 0");
            }

            switch (collection)
            {
                case IList<T> list:
                    return Next(list);
                case IReadOnlyList<T> readOnlyList:
                    return Next(readOnlyList);
            }

            int index = Next(count);
            int i = 0;
            foreach (T element in collection)
            {
                if (i++ == index)
                {
                    return element;
                }
            }

            // Should never happen
            return default;
        }

        public T Next<T>(IList<T> list)
        {
            if (ReferenceEquals(list, null))
            {
                throw new ArgumentNullException(nameof(list));
            }

            /*
                For small lists, it's much more efficient to simply return one of their elements
                instead of trying to generate a random number within bounds (which is implemented as a while(true) loop)
             */
            switch (list.Count)
            {
                case 1:
                    return list[0];
                case 2:
                    return NextBool() ? list[0] : list[1];
                default:
                    return list[Next(list.Count)];
            }
        }

        private T Next<T>(IReadOnlyList<T> list)
        {
            /*
                For small lists, it's much more efficient to simply return one of their elements
                instead of trying to generate a random number within bounds (which is implemented as a while(true) loop)
             */
            switch (list.Count)
            {
                case 1:
                    return list[0];
                case 2:
                    return NextBool() ? list[0] : list[1];
                default:
                    return list[Next(list.Count)];
            }
        }

        public T Next<T>() where T : struct, Enum
        {
            Type enumType = typeof(T);
            T[] enumValues;
            if (EnumTypeCache.TryGetValue(enumType, out Array enumArray))
            {
                enumValues = (T[])enumArray;
            }
            else
            {
                enumValues = (T[])Enum.GetValues(enumType);
            }

            return RandomOf(enumValues);
        }

        public T NextCachedEnum<T>() where T : struct, Enum
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
        public float[,] NextNoiseMap(int width, int height, float scale, int octaves)
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

                        // Use unity's implementation of perlin noise
                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
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
                    noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
                }
            }
            return noiseMap;
        }

        protected T RandomOf<T>(T[] values)
        {
            switch (values.Length)
            {
                case 0:
                    return default;
                case 1:
                    return values[0];
                case 2:
                    return NextBool() ? values[0] : values[1];
                default:
                    return values[Next(values.Length)];
            }
        }

        public abstract IRandom Copy();
    }
}
