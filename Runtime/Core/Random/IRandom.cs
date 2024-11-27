namespace UnityHelpers.Core.Random
{
    using System;
    using System.Collections.Generic;
    using DataStructure.Adapters;

    public interface IRandom
    {
        RandomState InternalState { get; }

        /// <summary>
        /// </summary>
        /// <returns>A number within the range [0, int.MaxValue).</returns>
        int Next();

        /// <summary>
        /// </summary>
        /// <returns>A number within the range [0, max).</returns>
        int Next(int max);

        /// <summary>
        /// </summary>
        /// <returns>A number within the range [min, max).</returns>
        int Next(int min, int max);

        /// <summary>
        /// </summary>
        /// <returns>A number within the range [0, uint.MaxValue].</returns>
        uint NextUint();

        /// <summary>
        /// </summary>
        /// <returns>A number within the range [0, max).</returns>
        uint NextUint(uint max);

        /// <summary>
        /// </summary>
        /// <returns>A number within the range [min, max).</returns>
        uint NextUint(uint min, uint max);

        /// <summary>
        /// </summary>
        /// <returns>A number within the range [0, short.MaxValue).</returns>
        short NextShort();

        /// <summary>
        /// </summary>
        /// <returns>A number within the range [0, max).</returns>
        short NextShort(short max);

        /// <summary>
        /// </summary>
        /// <returns>A number within the range [min, max).</returns>
        short NextShort(short min, short max);

        /// <summary>
        /// </summary>
        /// <returns>A number within the range [0, byte.MaxValue).</returns>
        byte NextByte();

        /// <summary>
        /// </summary>
        /// <returns>A number within the range [0, max).</returns>
        byte NextByte(byte max);

        /// <summary>
        /// </summary>
        /// <returns>A number within the range [min, max).</returns>
        byte NextByte(byte min, byte max);

        /// <summary>
        /// </summary>
        /// <returns>A number within the range [0, long.MaxValue).</returns>
        long NextLong();

        /// <summary>
        /// </summary>
        /// <returns>A number within the range [0, max).</returns>
        long NextLong(long max);

        /// <summary>
        /// </summary>
        /// <returns>A number within the range [min, max).</returns>
        long NextLong(long min, long max);

        /// <summary>
        /// </summary>
        /// <returns>A number within the range [0, ulong.MaxValue).</returns>
        ulong NextUlong();

        /// <summary>
        /// </summary>
        /// <returns>A number within the range [0, max).</returns>
        ulong NextUlong(ulong max);

        /// <summary>
        /// </summary>
        /// <returns>A number within the range [min, max).</returns>
        ulong NextUlong(ulong min, ulong max);

        /// <summary>
        /// </summary>
        /// <returns>50% chance of true or false.</returns>
        bool NextBool();

        void NextBytes(byte[] buffer);

        /// <summary>
        /// </summary>
        /// <returns>A number within the range [0, 1).</returns>
        float NextFloat();

        /// <summary>
        /// </summary>
        /// <returns>A number within the range [0, max).</returns>
        float NextFloat(float max);

        /// <summary>
        /// </summary>
        /// <returns>A number within the range min, max).</returns>
        float NextFloat(float min, float max);

        /// <summary>
        /// </summary>
        /// <returns>A number within the range [0, 1).</returns>
        double NextDouble();

        /// <summary>
        /// </summary>
        /// <returns>A number within the range [0, max).</returns>
        double NextDouble(double max);

        /// <summary>
        /// </summary>
        /// <returns>A number within the range min, max).</returns>
        double NextDouble(double min, double max);

        double NextGaussian(double mean = 0, double stdDev = 1);

        Guid NextGuid();
        KGuid NextKGuid();

        T NextOf<T>(IEnumerable<T> enumerable);
        T NextOf<T>(IReadOnlyCollection<T> collection);
        T NextOf<T>(IReadOnlyList<T> list);

        T NextEnum<T>()
            where T : struct, Enum;

        float[,] NextNoiseMap(
            int width,
            int height,
            PerlinNoise noise = null,
            float scale = 2.5f,
            int octaves = 8
        );

        IRandom Copy();
    }
}
