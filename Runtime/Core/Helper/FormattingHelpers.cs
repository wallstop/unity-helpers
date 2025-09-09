namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using System.Text;
    using WallstopStudios.UnityHelpers.Utils;

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

            StringBuilder stringBuilder = Buffers.StringBuilder;
            stringBuilder.Clear();
            stringBuilder.AppendFormat("{0:0.##} ", len);
            stringBuilder.Append(ByteSizes[order]);
            return stringBuilder.ToString();
        }
    }
}
