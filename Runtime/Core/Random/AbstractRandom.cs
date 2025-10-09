namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using DataStructure.Adapters;
    using ProtoBuf;
    using UnityEngine;
    using Utils;
#if !SINGLE_THREADED
    using System.Collections.Concurrent;
#else
    using Extension;
#endif

    [Serializable]
    [DataContract]
    [ProtoContract]
    [ProtoInclude(100, typeof(DotNetRandom))]
    [ProtoInclude(101, typeof(PcgRandom))]
    [ProtoInclude(102, typeof(XorShiftRandom))]
    [ProtoInclude(103, typeof(WyRandom))]
    [ProtoInclude(104, typeof(XoroShiroRandom))]
    [ProtoInclude(105, typeof(UnityRandom))]
    [ProtoInclude(106, typeof(SystemRandom))]
    [ProtoInclude(107, typeof(LinearCongruentialGenerator))]
    [ProtoInclude(108, typeof(SquirrelRandom))]
    [ProtoInclude(109, typeof(RomuDuo))]
    [ProtoInclude(110, typeof(SplitMix64))]
    [ProtoInclude(111, typeof(IllusionFlow))]
    public abstract class AbstractRandom : IRandom
    {
#if SINGLE_THREADED
        private static readonly Dictionary<Type, Array> EnumTypeCache = new();
#else
        private static readonly ConcurrentDictionary<Type, Array> EnumTypeCache = new();
#endif

        protected const uint HalfwayUint = uint.MaxValue / 2;
        protected const float MagicFloat = 5.960465E-008F;
        private const ulong LongBias = 1UL << 63;

        [ProtoMember(1)]
        protected double? _cachedGaussian;

        public abstract RandomState InternalState { get; }

        private readonly byte[] _guidBytes = new byte[16];

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

            uint remainder = unchecked((0u - max) % max);
            if (remainder == 0)
            {
                return NextUint() % max;
            }

            uint threshold = unchecked(0u - remainder);
            uint value;
            do
            {
                value = NextUint();
            } while (value >= threshold);

            return value % max;
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

            return unchecked((long)NextUlong(unchecked((ulong)max)));
        }

        public long NextLong(long min, long max)
        {
            if (max <= min)
            {
                throw new ArgumentException(
                    $"Min {min} cannot be larger-than or equal-to Max {max}"
                );
            }

            ulong biasedMin = BiasLong(min);
            ulong biasedMax = BiasLong(max);
            ulong range = biasedMax - biasedMin;

            ulong sample = NextUlong(range);
            ulong biasedResult = biasedMin + sample;
            return UnbiasLong(biasedResult);
        }

        public ulong NextUlong()
        {
            uint upper = NextUint();
            uint lower = NextUint();
            return ((ulong)upper << 32) | lower;
        }

        public ulong NextUlong(ulong max)
        {
            if (max == 0)
            {
                throw new ArgumentException("Max cannot be zero");
            }

            ulong remainder = unchecked((0UL - max) % max);
            if (remainder == 0)
            {
                return NextUlong() % max;
            }

            ulong threshold = unchecked(0UL - remainder);
            ulong value;
            do
            {
                value = NextUlong();
            } while (value >= threshold);

            return value % max;
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
            return (NextUint() & 1u) == 0;
        }

        public virtual void NextBytes(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            const int sizeOfUint = 4; // May differ on some platforms

            int chunks = buffer.Length / sizeOfUint;
            int spare = buffer.Length - chunks * sizeOfUint;
            for (int i = 0; i < chunks; ++i)
            {
                int offset = i * sizeOfUint;
                uint random = NextUint();
                buffer[offset] = unchecked((byte)random);
                buffer[offset + 1] = unchecked((byte)(random >> 8));
                buffer[offset + 2] = unchecked((byte)(random >> 16));
                buffer[offset + 3] = unchecked((byte)(random >> 24));
            }

            if (0 < spare)
            {
                uint spareRandom = NextUint();
                for (int i = 0; i < spare; ++i)
                {
                    buffer[buffer.Length - spare + i] = unchecked((byte)(spareRandom >> (i * 8)));
                }
            }
        }

        public virtual double NextDouble()
        {
            const double scale = 1.0 / 9007199254740992.0; // 2^53
            ulong high = NextUint() >> 5;
            ulong low = NextUint() >> 6;
            ulong combined = (high << 26) | low;
            return combined * scale;
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
            ulong orderedMin = ToOrderedDouble(min);
            ulong orderedMax = ToOrderedDouble(max);

            if (orderedMax <= orderedMin)
            {
                throw new ArgumentException(
                    $"Invalid range [{min}, {max}) for infinite-range sampling."
                );
            }

            ulong range = orderedMax - orderedMin;
            while (true)
            {
                ulong sample = orderedMin + NextUlong(range);
                double value = FromOrderedDouble(sample);

                if (!double.IsNaN(value) && !double.IsInfinity(value))
                {
                    return value;
                }
            }
        }

        protected double NextDoubleFullRange()
        {
            const ulong exponentMask = 0x7FF0000000000000;
            ulong randomBits;
            do
            {
                randomBits = NextUlong();
            } while ((randomBits & exponentMask) == exponentMask);

            return BitConverter.Int64BitsToDouble(unchecked((long)randomBits));
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
            const float scale = 1f / 4294967296f;
            return NextUint() * scale;
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
            if (enumerable is null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }

            return enumerable switch
            {
                IReadOnlyList<T> list => NextOf(list),
                IReadOnlyCollection<T> collection => NextOf(collection),
                _ => NextFromEnumerable(enumerable),
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

            switch (collection)
            {
                case HashSet<T> hashSet:
                {
                    int i = 0;
                    foreach (T element in hashSet)
                    {
                        if (index == i++)
                        {
                            return element;
                        }
                    }
                    throw new ArgumentException(nameof(collection));
                }
                case SortedSet<T> sortedSet:
                {
                    int i = 0;
                    foreach (T element in sortedSet)
                    {
                        if (index == i++)
                        {
                            return element;
                        }
                    }

                    throw new ArgumentException(nameof(collection));
                }
                case LinkedList<T> linkedList:
                {
                    int i = 0;
                    foreach (T element in linkedList)
                    {
                        if (index == i++)
                        {
                            return element;
                        }
                    }

                    throw new ArgumentException(nameof(collection));
                }
                case Queue<T> queue:
                {
                    int i = 0;
                    foreach (T element in queue)
                    {
                        if (index == i++)
                        {
                            return element;
                        }
                    }

                    throw new ArgumentException(nameof(collection));
                }
                case Stack<T> stack:
                {
                    int i = 0;
                    foreach (T element in stack)
                    {
                        if (index == i++)
                        {
                            return element;
                        }
                    }

                    throw new ArgumentException(nameof(collection));
                }
                default:
                {
                    return collection.ElementAt(index);
                }
            }
        }

        public T NextOf<T>(IReadOnlyList<T> list)
        {
            if (list is not { Count: > 0 })
            {
                throw new ArgumentException("Collection cannot be empty", nameof(list));
            }

            /*
                For small lists, it's much more efficient to simply return one of their elements
                instead of trying to generate a random number within bounds (which is implemented as a while(true) loop)
             */
            return RandomOf(list);
        }

        public T NextOfParams<T>(params T[] elements)
        {
            if (elements.Length == 0)
            {
                throw new ArgumentException(nameof(elements));
            }

            return RandomOf(elements);
        }

        private T NextFromEnumerable<T>(IEnumerable<T> enumerable)
        {
            using IEnumerator<T> enumerator = enumerable.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                throw new ArgumentException("Collection cannot be empty", nameof(enumerable));
            }

            T selection = enumerator.Current;
            ulong seen = 1;

            while (enumerator.MoveNext())
            {
                seen++;
                if (NextUlong(seen) == 0)
                {
                    selection = enumerator.Current;
                }
            }

            return selection;
        }

        private static T[] GetEnumValues<T>()
            where T : unmanaged, Enum
        {
            Type enumType = typeof(T);
            Array boxedValues = EnumTypeCache.GetOrAdd(enumType, type => Enum.GetValues(type));
            return Unsafe.As<Array, T[]>(ref boxedValues);
        }

        private static void EnsureEnumHasAvailableValues<T>(
            T[] enumValues,
            ReadOnlySpan<T> exclusions
        )
            where T : unmanaged, Enum
        {
            if (enumValues.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Enum {typeof(T).Name} does not define any values."
                );
            }

            if (exclusions.IsEmpty)
            {
                return;
            }

            int enumCount = enumValues.Length;
            const int StackThreshold = 8;

            if (exclusions.Length <= StackThreshold)
            {
                Span<T> unique = stackalloc T[StackThreshold];
                int uniqueCount = 0;

                foreach (T exclusion in exclusions)
                {
                    if (Array.IndexOf(enumValues, exclusion) < 0)
                    {
                        continue;
                    }

                    bool seen = false;
                    for (int i = 0; i < uniqueCount; ++i)
                    {
                        if (EqualityComparer<T>.Default.Equals(unique[i], exclusion))
                        {
                            seen = true;
                            break;
                        }
                    }

                    if (seen)
                    {
                        continue;
                    }

                    unique[uniqueCount++] = exclusion;
                    if (uniqueCount >= enumCount)
                    {
                        ThrowAllEnumValuesExcluded<T>(enumCount);
                    }
                }

                return;
            }

            using PooledResource<HashSet<T>> pooledSet = Buffers<T>.HashSet.Get();
            HashSet<T> set = pooledSet.resource;
            foreach (T exclusion in exclusions)
            {
                if (Array.IndexOf(enumValues, exclusion) < 0)
                {
                    continue;
                }

                set.Add(exclusion);
                if (set.Count >= enumCount)
                {
                    set.Clear();
                    ThrowAllEnumValuesExcluded<T>(enumCount);
                }
            }

            set.Clear();
        }

        private T NextEnumExceptInternal<T>(ReadOnlySpan<T> exclusions)
            where T : unmanaged, Enum
        {
            T[] enumValues = GetEnumValues<T>();
            EnsureEnumHasAvailableValues(enumValues, exclusions);
            return SelectEnumValue(enumValues, exclusions);
        }

        private T SelectEnumValue<T>(T[] enumValues, ReadOnlySpan<T> exclusions)
            where T : unmanaged, Enum
        {
            if (exclusions.IsEmpty)
            {
                return RandomOf(enumValues);
            }

            const int StackThreshold = 128;
            int enumCount = enumValues.Length;
            Span<T> buffer = enumCount <= StackThreshold ? stackalloc T[enumCount] : default;

            if (!buffer.IsEmpty)
            {
                int count = PopulateAllowedValues(enumValues, exclusions, buffer);
                if (count == 0)
                {
                    ThrowAllEnumValuesExcluded<T>(enumCount);
                }

                return count == 1 ? buffer[0] : buffer[Next(count)];
            }

            using PooledResource<T[]> pooled = WallstopFastArrayPool<T>.Get(
                enumCount,
                out T[] temp
            );
            Span<T> tempSpan = temp.AsSpan(0, enumCount);
            int index = PopulateAllowedValues(enumValues, exclusions, tempSpan);
            if (index == 0)
            {
                ThrowAllEnumValuesExcluded<T>(enumCount);
            }

            return index == 1 ? temp[0] : temp[Next(index)];
        }

        private T SelectEnumValue<T>(T[] enumValues, HashSet<T> exclusions)
            where T : unmanaged, Enum
        {
            if (exclusions == null || exclusions.Count == 0)
            {
                return RandomOf(enumValues);
            }

            const int StackThreshold = 128;
            int enumCount = enumValues.Length;
            Span<T> buffer = enumCount <= StackThreshold ? stackalloc T[enumCount] : default;

            if (!buffer.IsEmpty)
            {
                int count = PopulateAllowedValues(enumValues, exclusions, buffer);
                if (count == 0)
                {
                    ThrowAllEnumValuesExcluded<T>(enumCount);
                }

                return count == 1 ? buffer[0] : buffer[Next(count)];
            }

            using PooledResource<T[]> pooled = WallstopFastArrayPool<T>.Get(
                enumCount,
                out T[] temp
            );
            Span<T> tempSpan = temp.AsSpan(0, enumCount);
            int index = PopulateAllowedValues(enumValues, exclusions, tempSpan);
            if (index == 0)
            {
                ThrowAllEnumValuesExcluded<T>(enumCount);
            }

            return index == 1 ? temp[0] : temp[Next(index)];
        }

        private static void EnsureEnumHasAvailableValues<T>(T[] enumValues, HashSet<T> exclusions)
            where T : struct, Enum
        {
            if (enumValues.Length == 0)
            {
                exclusions.Clear();
                throw new InvalidOperationException(
                    $"Enum {typeof(T).Name} does not define any values."
                );
            }

            if (exclusions.Count == 0)
            {
                return;
            }

            int excludedCount = 0;
            foreach (T value in enumValues)
            {
                if (exclusions.Contains(value))
                {
                    excludedCount++;
                }
            }

            if (excludedCount >= enumValues.Length)
            {
                exclusions.Clear();
                ThrowAllEnumValuesExcluded<T>(enumValues.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool SpanContains<T>(ReadOnlySpan<T> span, T value)
        {
            for (int i = 0; i < span.Length; ++i)
            {
                if (EqualityComparer<T>.Default.Equals(span[i], value))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PopulateAllowedValues<T>(
            T[] enumValues,
            ReadOnlySpan<T> exclusions,
            Span<T> destination
        )
            where T : unmanaged, Enum
        {
            int count = 0;
            foreach (T value in enumValues)
            {
                if (!SpanContains(exclusions, value))
                {
                    destination[count++] = value;
                }
            }

            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PopulateAllowedValues<T>(
            T[] enumValues,
            HashSet<T> exclusions,
            Span<T> destination
        )
            where T : unmanaged, Enum
        {
            int count = 0;
            foreach (T value in enumValues)
            {
                if (!exclusions.Contains(value))
                {
                    destination[count++] = value;
                }
            }

            return count;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowAllEnumValuesExcluded<T>(int enumCount)
            where T : struct, Enum
        {
            throw new InvalidOperationException(
                $"Cannot select a value from enum {typeof(T).Name} because all {enumCount} defined values are excluded."
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong BiasLong(long value)
        {
            return unchecked((ulong)value + LongBias);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long UnbiasLong(ulong value)
        {
            return unchecked((long)(value - LongBias));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ToOrderedDouble(double value)
        {
            if (double.IsNaN(value))
            {
                throw new ArgumentException(
                    "NaN is not a valid bound for random sampling.",
                    nameof(value)
                );
            }

            ulong bits = unchecked((ulong)BitConverter.DoubleToInt64Bits(value));
            const ulong signBit = 1UL << 63;
            return (bits & signBit) != 0 ? ~bits : bits | signBit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double FromOrderedDouble(ulong value)
        {
            const ulong signBit = 1UL << 63;
            ulong bits = (value & signBit) != 0 ? value & ~signBit : ~value;
            return BitConverter.Int64BitsToDouble(unchecked((long)bits));
        }

        public T NextEnum<T>()
            where T : unmanaged, Enum
        {
            T[] enumValues = GetEnumValues<T>();
            if (enumValues.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Enum {typeof(T).Name} does not define any values."
                );
            }

            return RandomOf(enumValues);
        }

        public T NextEnumExcept<T>(T exception1)
            where T : unmanaged, Enum
        {
            Span<T> exclusions = stackalloc T[1];
            exclusions[0] = exception1;
            return NextEnumExceptInternal<T>(exclusions);
        }

        public T NextEnumExcept<T>(T exception1, T exception2)
            where T : unmanaged, Enum
        {
            Span<T> exclusions = stackalloc T[2];
            exclusions[0] = exception1;
            exclusions[1] = exception2;
            return NextEnumExceptInternal<T>(exclusions);
        }

        public T NextEnumExcept<T>(T exception1, T exception2, T exception3)
            where T : unmanaged, Enum
        {
            Span<T> exclusions = stackalloc T[3];
            exclusions[0] = exception1;
            exclusions[1] = exception2;
            exclusions[2] = exception3;
            return NextEnumExceptInternal<T>(exclusions);
        }

        public T NextEnumExcept<T>(T exception1, T exception2, T exception3, T exception4)
            where T : unmanaged, Enum
        {
            Span<T> exclusions = stackalloc T[4];
            exclusions[0] = exception1;
            exclusions[1] = exception2;
            exclusions[2] = exception3;
            exclusions[3] = exception4;
            return NextEnumExceptInternal<T>(exclusions);
        }

        public T NextEnumExcept<T>(
            T exception1,
            T exception2,
            T exception3,
            T exception4,
            params T[] exceptions
        )
            where T : unmanaged, Enum
        {
            T[] enumValues = GetEnumValues<T>();

            using PooledResource<HashSet<T>> bufferResource = Buffers<T>.HashSet.Get();
            HashSet<T> set = bufferResource.resource;
            set.Add(exception1);
            set.Add(exception2);
            set.Add(exception3);
            set.Add(exception4);
            foreach (T exception in exceptions)
            {
                set.Add(exception);
            }

            EnsureEnumHasAvailableValues(enumValues, set);

            return SelectEnumValue(enumValues, set);
        }

        public Guid NextGuid()
        {
            return new Guid(GenerateGuidBytes(_guidBytes));
        }

        public KGuid NextKGuid()
        {
            return new KGuid(GenerateGuidBytes(_guidBytes));
        }

        private byte[] GenerateGuidBytes(byte[] guidBytes)
        {
            NextBytes(guidBytes);
            SetUuidV4Bits(guidBytes);
            return guidBytes;
        }

        public static void SetUuidV4Bits(byte[] bytes)
        {
            // Set version to 4 (bits 6-7 of byte 6)

            // Clear the version bits first (clear bits 4-7)
            byte value = bytes[6];
            value &= 0x0f;
            // Set version 4 (set bits 4-7 to 0100)
            value |= 0x40;
            bytes[6] = value;

            // Set variant to RFC 4122 (bits 6-7 of byte 8)
            value = bytes[8];
            // Clear the variant bits first (clear bits 6-7)
            value &= 0x3f;
            // Set RFC 4122 variant (set bits 6-7 to 10)
            value |= 0x80;
            bytes[8] = value;
        }

        public float[,] NextNoiseMap(
            float[,] noiseMap,
            PerlinNoise noise = null,
            float scale = 2.5f,
            int octaves = 8,
            float persistence = 0.5f,
            float lacunarity = 2f,
            Vector2 baseOffset = default,
            float octaveOffsetRange = 100000f,
            bool normalize = true
        )
        {
            if (noiseMap is null)
            {
                throw new ArgumentNullException(nameof(noiseMap));
            }

            if (scale <= 0)
            {
                throw new ArgumentException(nameof(scale));
            }

            if (octaves < 1)
            {
                throw new ArgumentException(nameof(octaves));
            }

            if (persistence <= 0)
            {
                throw new ArgumentException(nameof(persistence));
            }

            if (lacunarity <= 0)
            {
                throw new ArgumentException(nameof(lacunarity));
            }

            if (octaveOffsetRange <= 0)
            {
                throw new ArgumentException(nameof(octaveOffsetRange));
            }

            noise ??= PerlinNoise.Instance;

            int width = noiseMap.GetLength(0);
            int height = noiseMap.GetLength(1);
            using PooledResource<Vector2[]> octaveOffsetBuffer = WallstopFastArrayPool<Vector2>.Get(
                octaves,
                out Vector2[] octaveOffsets
            );
            for (int i = 0; i < octaves; i++)
            {
                float offsetX = NextFloat(-octaveOffsetRange, octaveOffsetRange);
                float offsetY = NextFloat(-octaveOffsetRange, octaveOffsetRange);
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
                        float sampleX =
                            (x - halfWidth) / scale * frequency + octaveOffsets[i].x + baseOffset.x;
                        float sampleY =
                            (y - halfHeight) / scale * frequency
                            + octaveOffsets[i].y
                            + baseOffset.y;

                        float perlinValue = noise.Noise(sampleX, sampleY) * 2 - 1;
                        noiseHeight += perlinValue * amplitude;
                        amplitude *= persistence;
                        frequency *= lacunarity;
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

            if (normalize)
            {
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
            }
            return noiseMap;
        }

        protected T RandomOf<T>(IReadOnlyList<T> values)
        {
            int count = values.Count;
            return count switch
            {
                0 => default,
                1 => values[0],
                2 => NextBool() ? values[0] : values[1],
                _ => values[Next(count)],
            };
        }

        public abstract IRandom Copy();
    }
}
