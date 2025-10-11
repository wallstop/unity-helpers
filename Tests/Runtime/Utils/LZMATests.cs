namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class LZMATests
    {
        [Test]
        public void CompressDecompressRoundtrip()
        {
            Random random = new(42);
            foreach (int length in new[] { 0, 1, 5, 64, 257 })
            {
                byte[] data = new byte[length];
                random.NextBytes(data);
                byte[] compressed = LZMA.Compress(data);
                Assert.GreaterOrEqual(compressed.Length, 13);
                byte[] decompressed = LZMA.Decompress(compressed);
                Assert.AreEqual(data, decompressed);
            }
        }

        [Test]
        public void DecompressingGarbageThrows()
        {
            byte[] garbage = new byte[] { 1, 2, 3, 4, 5 };
            Assert.Throws<Exception>(() => LZMA.Decompress(garbage));
        }
    }
}
