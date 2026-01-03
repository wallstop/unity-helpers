// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Random;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class LZMATests
    {
        [Test]
        public void CompressDecompressRoundtrip()
        {
            IRandom random = new PcgRandom(42);
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
            byte[] garbage = { 1, 2, 3, 4, 5 };
            Assert.Throws<Exception>(() => LZMA.Decompress(garbage));
        }
    }
}
