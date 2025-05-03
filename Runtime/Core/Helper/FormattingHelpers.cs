namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;

    public static class FormattingHelpers
    {
        private static readonly string[] ByteSizes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };

        public static string FormatBytes(long bytes)
        {
            bytes = Math.Max(0L, bytes);
            double len = bytes;
            int order = 0;

            const int byteInChunk = 1024;
            while (byteInChunk <= len)
            {
                len /= byteInChunk;
                if (order < ByteSizes.Length - 1)
                {
                    ++order;
                }
                else
                {
                    throw new ArgumentException($"Too many bytes! Cannot parse {bytes}");
                }
            }

            return $"{len:0.##} {ByteSizes[order]}";
        }
    }
}
