﻿namespace UnityHelpers.Core.Helper
{
    using System;

    public static class ArrayConverter
    {
        public static byte[] IntArrayToByteArray_BlockCopy(int[] ints)
        {
            if (ints == null)
            {
                throw new ArgumentNullException(nameof(ints));
            }

            // Each int is 4 bytes
            byte[] bytes = new byte[ints.Length * sizeof(int)];
            Buffer.BlockCopy(ints, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static int[] ByteArrayToIntArray_BlockCopy(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes), "Byte array cannot be null.");
            }

            if (bytes.Length % sizeof(int) != 0)
            {
                throw new ArgumentException(
                    $"Byte array length must be a multiple of {sizeof(int)}.",
                    nameof(bytes)
                );
            }

            int[] ints = new int[bytes.Length / sizeof(int)];
            Buffer.BlockCopy(bytes, 0, ints, 0, bytes.Length);
            return ints;
        }
    }
}