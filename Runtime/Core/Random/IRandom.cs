// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System;
    using System.Collections.Generic;
    using DataStructure.Adapters;
    using UnityEngine;

    /// <summary>
    /// Unified random number generator interface implemented by all PRNGs in this package.
    /// </summary>
    /// <remarks>
    /// Serialization guidance:
    /// - JSON: Works out of the box using runtime type information in the serializer entry points.
    /// - Protobuf: You can serialize an IRandom instance (the runtime type is used), but when
    ///   deserializing to IRandom you must either register a concrete root via
    ///   <c>Serializer.RegisterProtobufRoot&lt;IRandom, ConcreteRandom&gt;()</c> or declare your model
    ///   field as <see cref="AbstractRandom"/>, which is annotated with [ProtoInclude] for all
    ///   known implementations.
    ///   Alternatively, use the overload <c>Serializer.ProtoDeserialize&lt;T&gt;(byte[], Type)</c> and
    ///   pass the concrete type.
    /// </remarks>
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

        WGuid NextWGuid();

        T NextOf<T>(IEnumerable<T> enumerable);
        T NextOf<T>(IReadOnlyCollection<T> collection);
        T NextOf<T>(IReadOnlyList<T> list);

        T NextOfParams<T>(params T[] elements);

        T NextEnum<T>()
            where T : unmanaged, Enum;

        T NextEnumExcept<T>(T exception1)
            where T : unmanaged, Enum;

        T NextEnumExcept<T>(T exception1, T exception2)
            where T : unmanaged, Enum;

        T NextEnumExcept<T>(T exception1, T exception2, T exception3)
            where T : unmanaged, Enum;

        T NextEnumExcept<T>(T exception1, T exception2, T exception3, T exception4)
            where T : unmanaged, Enum;

        T NextEnumExcept<T>(
            T exception1,
            T exception2,
            T exception3,
            T exception4,
            params T[] exceptions
        )
            where T : unmanaged, Enum;

        float[,] NextNoiseMap(
            float[,] noiseMap,
            PerlinNoise noise = null,
            float scale = 2.5f,
            int octaves = 8,
            float persistence = 0.5f,
            float lacunarity = 2f,
            Vector2 baseOffset = default,
            float octaveOffsetRange = 100000f,
            bool normalize = true
        );

        IRandom Copy();
    }
}
