namespace UnityHelpers.Core.Helper
{
    using System;

    public static class FormattingHelpers
    {
        public static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            double len = bytes;
            int order = 0;

            bytes = Math.Max(0, bytes);

            const int byteInChunk = 1024;
            while (byteInChunk <= len)
            {
                len /= byteInChunk;
                if (order < sizes.Length - 1)
                {
                    ++order;
                }
                else
                {
                    throw new ArgumentException($"Too many bytes! Cannot parse {bytes}");
                }
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}
