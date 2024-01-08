namespace UnityHelpers.Core.Random
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.Serialization;

    [Serializable]
    [DataContract]
    public abstract class AbstractRandom : IRandom
    {
        private static readonly ConcurrentDictionary<Type, Array> EnumTypeCache = new();

        protected const float MagicFloat = 5.960465E-008F;

        protected double? _cachedGaussian;

        protected AbstractRandom() { }

        public abstract RandomState InternalState { get; }


        // Internal sampler
        public abstract uint NextUint();

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
            IRandom random = this;
            do
            {
                x = 2 * random.NextDouble() - 1;
                y = 2 * random.NextDouble() - 1;
                square = x * x + y * y;
            }
            while (square > 1 || square == 0);

            double fac = Math.Sqrt(-2 * Math.Log(square) / square);
            _cachedGaussian = x * fac;
            return y * fac;
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

        protected T RandomOf<T>(T[] values)
        {
            switch (values.Length)
            {
                case 0:
                    return default;
                case 1:
                    return values[0];
                case 2:
                    return ((IRandom)this).NextBool() ? values[0] : values[1];
                default:
                    return values[((IRandom)this).Next(values.Length)];
            }
        }

        public abstract IRandom Copy();
    }
}
