namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using System.Text;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// Human-friendly formatting helpers (sizes, numbers).
    /// </summary>
    public static class FormattingHelpers
    {
        private static readonly string[] ByteSizes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };

        /// <summary>
        /// Formats a byte count into a human-readable string (e.g., "1.23 MB").
        /// </summary>
        /// <param name="bytes">The number of bytes (negative values are clamped to 0).</param>
        /// <returns>Formatted string with up to two decimal places and appropriate unit.</returns>
        public static string FormatBytes(long bytes)
        {
            bytes = Math.Max(0L, bytes);
            long workingValue = bytes;
            int order = 0;

            const int bitShift = 10; // 2^10 = 1024
            // Use bit shifting to determine the order without precision loss
            while (workingValue >= 1024 && order < ByteSizes.Length - 1)
            {
                workingValue >>= bitShift;
                ++order;
            }

            // Check if we still have a value >= 1024 after exhausting all units
            if (workingValue >= 1024)
            {
                throw new ArgumentException($"Too many bytes! Cannot parse {bytes}");
            }

            // Now calculate the precise double value for display
            double displayValue = bytes / Math.Pow(1024, order);

            using PooledResource<StringBuilder> stringBuilderResource = Buffers.StringBuilder.Get();
            StringBuilder stringBuilder = stringBuilderResource.resource;
            stringBuilder.AppendFormat("{0:0.##} ", displayValue);
            stringBuilder.Append(ByteSizes[order]);
            return stringBuilder.ToString();
        }
    }
}
