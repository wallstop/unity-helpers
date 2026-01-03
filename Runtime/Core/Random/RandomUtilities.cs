// MIT License - Copyright (c) 2023 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Random
{
    using System;
    using System.Buffers.Binary;
    using UnityEngine;

    public static class RandomUtilities
    {
        public static (ulong First, ulong Second) GuidToUInt64Pair(Guid guid)
        {
            Span<byte> bytes = stackalloc byte[16];
            guid.TryWriteBytes(bytes);
            ulong a = BinaryPrimitives.ReadUInt64LittleEndian(bytes);
            ulong b = BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(8));
            return (a, b);
        }

        public static (uint A, uint B, uint C, uint D) GuidToUInt32Quad(Guid guid)
        {
            Span<byte> bytes = stackalloc byte[16];
            guid.TryWriteBytes(bytes);
            uint a = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
            uint b = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(4));
            uint c = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(8));
            uint d = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(12));
            return (a, b, c, d);
        }

        public static int GuidToInt32(Guid guid)
        {
            Span<byte> bytes = stackalloc byte[16];
            guid.TryWriteBytes(bytes);
            return BinaryPrimitives.ReadInt32LittleEndian(bytes);
        }

        public static float GetRandomVariance(this IRandom random, float baseValue, float variance)
        {
            if (variance < 0.0f)
            {
                Debug.LogError("Variance cannot be negative");
                return baseValue;
            }

            if (variance == 0.0f)
            {
                return baseValue;
            }

            float higher = variance / 2;
            float lower = -higher;

            return baseValue + random.NextFloat(lower, higher);
        }
    }
}
