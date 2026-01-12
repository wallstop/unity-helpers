// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

// Array pooling patterns - choose the right pool for your use case

namespace WallstopStudios.UnityHelpers.Examples
{
    using WallstopStudios.UnityHelpers.Core.Helper;

    public static class ArrayPoolingExamples
    {
        // WallstopArrayPool<T> - Fixed/constant sizes only, returns exact size
        public static void FixedSizePoolExample()
        {
            // Good for: PRNG state buffers, fixed-size work arrays
            using PooledArray<ulong> pooled = WallstopArrayPool<ulong>.Get(4, out ulong[] state);
            // state.Length == 4 (exact)

            // Use state array...
            state[0] = 123UL;
            state[1] = 456UL;
        }

        // WallstopFastArrayPool<T> - Fixed sizes, unmanaged types, no clearing
        // Fastest option when you don't need array clearing
        public static void FastPoolExample()
        {
            using PooledArray<int> pooled = WallstopFastArrayPool<int>.Get(16, out int[] buffer);
            // buffer may contain stale data - caller must initialize
            for (int i = 0; i < pooled.Length; i++)
            {
                buffer[i] = 0;
            }
        }

        // SystemArrayPool<T> - Variable/dynamic sizes, returns at least requested
        public static void VariableSizePoolExample(int dynamicSize)
        {
            // Good for: Sorting buffers, temporary work arrays of varying size
            using PooledArray<float> pooled = SystemArrayPool<float>.Get(
                dynamicSize,
                out float[] temp
            );

            // CRITICAL: Use pooled.Length, NOT temp.Length!
            // temp.Length may be larger than dynamicSize
            for (int i = 0; i < pooled.Length; i++)
            {
                temp[i] = i * 0.5f;
            }
        }
    }

    // Pool selection guide:
    // | Pool                       | Use Case                                  | Returns            |
    // | WallstopArrayPool<T>       | Fixed/constant sizes only                 | Exact size         |
    // | WallstopFastArrayPool<T>   | Fixed sizes, unmanaged types, no clearing | Exact size         |
    // | SystemArrayPool<T>         | Variable/dynamic sizes                    | At least requested |
}
