namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class LZMAComprehensiveTests
    {
        [Test]
        public void RoundtripVariousSizes()
        {
            Random random = new(12345);
            int[] sizes = new[] { 0, 1, 3, 5, 32, 64, 257, 1024, 4096 };
            foreach (int length in sizes)
            {
                byte[] data = new byte[length];
                random.NextBytes(data);
                byte[] compressed = LZMA.Compress(data);
                Assert.That(
                    compressed.Length,
                    Is.GreaterThanOrEqualTo(13),
                    "Compressed length should include header"
                );
                byte[] roundtripped = LZMA.Decompress(compressed);
                Assert.AreEqual(data, roundtripped, $"Roundtrip data mismatch for size {length}");
            }
        }

        [Test]
        public void MultipleSequentialRoundtripsReuseCodecSafely()
        {
            Random random = new(42);
            for (int i = 0; i < 10; i++)
            {
                int length = 128 + i * 17;
                byte[] data = new byte[length];
                random.NextBytes(data);
                byte[] compressed = LZMA.Compress(data);
                byte[] roundtripped = LZMA.Decompress(compressed);
                Assert.AreEqual(data, roundtripped, $"Roundtrip failed on iteration {i}");
            }
        }

        [Test]
        public void NullInputThrowsArgumentNull()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
                LZMA.Decompress(null)
            );
            Assert.That(ex.ParamName, Is.EqualTo("input"), "Parameter name should be 'input'");
        }

        [Test]
        public void TooShortInputThrowsWithMessage()
        {
            byte[] input = new byte[12];
            Exception ex = Assert.Throws<Exception>(() => LZMA.Decompress(input));
            Assert.That(
                ex.Message,
                Does.Contain("too short"),
                "Exception should indicate input too short"
            );
        }

        [Test]
        public void InvalidNegativeLengthHeaderThrows()
        {
            byte[] input = new byte[13];
            input[0] = 0x5D; // typical lc/lp/pb placeholder; still invalid overall
            long negative = -1;
            byte[] len = BitConverter.GetBytes(negative);
            Array.Copy(len, 0, input, 5, 8);

            Exception ex = Assert.Throws<Exception>(() => LZMA.Decompress(input));
            Assert.That(
                ex.Message,
                Does.Contain("Invalid LZMA length header"),
                "Should flag invalid length header"
            );
        }

        [Test]
        public void EmptyPayloadWithValidHeadersThrows()
        {
            byte[] input = new byte[13];
            byte[] len = BitConverter.GetBytes(0L);
            Array.Copy(len, 0, input, 5, 8);

            Exception ex = Assert.Throws<Exception>(() => LZMA.Decompress(input));
            Assert.That(
                ex.Message,
                Does.Contain("Failed to decompress"),
                "Should fail to decompress with empty payload"
            );
        }

        [Test]
        public void TruncatedPayloadThrows()
        {
            byte[] data = new byte[256];
            new Random(7).NextBytes(data);
            byte[] compressed = LZMA.Compress(data);

            int keep = Math.Max(13, compressed.Length / 2);
            byte[] truncated = new byte[keep];
            Array.Copy(compressed, truncated, keep);

            Exception ex = Assert.Throws<Exception>(() => LZMA.Decompress(truncated));
            Assert.That(
                ex.Message,
                Does.Contain("Failed to decompress"),
                "Should fail to decompress truncated payload"
            );
        }

        [Test]
        public void CorruptedPropertiesThrows()
        {
            byte[] data = new byte[32];
            new Random(9).NextBytes(data);
            byte[] compressed = LZMA.Compress(data);

            byte[] corrupted = new byte[compressed.Length];
            Array.Copy(compressed, corrupted, compressed.Length);
            corrupted[0] ^= 0xFF; // flip bits in properties

            Exception ex = Assert.Throws<Exception>(() => LZMA.Decompress(corrupted));
            Assert.That(
                ex.Message,
                Does.Contain("Failed to decompress"),
                "Should fail with corrupted properties"
            );
        }

        [Test]
        public void CorruptedBodyThrows()
        {
            byte[] data = new byte[64];
            new Random(21).NextBytes(data);
            byte[] compressed = LZMA.Compress(data);

            byte[] corrupted = new byte[compressed.Length];
            Array.Copy(compressed, corrupted, compressed.Length);
            int start = Math.Max(13, corrupted.Length / 3);
            int span = Math.Min(32, corrupted.Length - start);
            for (int i = 0; i < span; i++)
            {
                corrupted[start + i] ^= 0x5A;
            }

            Exception ex = Assert.Throws<Exception>(() => LZMA.Decompress(corrupted));
            Assert.That(
                ex.Message,
                Does.Contain("Failed to decompress"),
                "Should fail with corrupted body"
            );
        }
    }
}
