namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Core.Random;

    public sealed class FormattingHelperTests
    {
        private const int NumTries = 1_000;

        [Test]
        public void FormatNegative()
        {
            for (int i = 0; i < NumTries; ++i)
            {
                long bytes = PRNG.Instance.NextLong(long.MinValue, 0);
                const string expected = "0 B";
                string found = FormattingHelpers.FormatBytes(bytes);
                Assert.AreEqual(expected, found, $"{bytes} failed to convert");
            }
        }

        [Test]
        public void FormatZeroBytes()
        {
            long bytes = 0L;
            const string expected = "0 B";
            string found = FormattingHelpers.FormatBytes(bytes);
            Assert.AreEqual(expected, found);
        }

        [Test]
        public void FormatBytes()
        {
            for (int i = 0; i < NumTries; ++i)
            {
                long bytes = PRNG.Instance.NextLong(1024L);
                string expected = $"{bytes} B";
                string found = FormattingHelpers.FormatBytes(bytes);
                Assert.AreEqual(expected, found);
            }
        }

        [Test]
        public void FormatKiloBytes()
        {
            for (int i = 0; i < NumTries; ++i)
            {
                long bytes = PRNG.Instance.NextLong(1024L, 1024L * 1024L);
                string expected = $"{(bytes / 1024.0):0.##} KB";
                string found = FormattingHelpers.FormatBytes(bytes);
                Assert.AreEqual(expected, found);
            }
        }

        [Test]
        public void FormatMegaBytes()
        {
            for (int i = 0; i < NumTries; ++i)
            {
                long bytes = PRNG.Instance.NextLong(1024L * 1024L, 1024L * 1024L * 1024L);
                string expected = $"{(bytes / 1024.0 / 1024.0):0.##} MB";
                string found = FormattingHelpers.FormatBytes(bytes);
                Assert.AreEqual(expected, found);
            }
        }

        [Test]
        public void FormatGigaBytes()
        {
            for (int i = 0; i < NumTries; ++i)
            {
                long bytes = PRNG.Instance.NextLong(
                    1024L * 1024L * 1024L,
                    1024L * 1024L * 1024L * 1024L
                );
                string expected = $"{(bytes / 1024.0 / 1024.0 / 1024.0):0.##} GB";
                string found = FormattingHelpers.FormatBytes(bytes);
                Assert.AreEqual(expected, found);
            }
        }

        [Test]
        public void FormatTeraBytes()
        {
            for (int i = 0; i < NumTries; ++i)
            {
                long bytes = PRNG.Instance.NextLong(
                    1024L * 1024L * 1024L * 1024L,
                    1024L * 1024L * 1024L * 1024L * 1024L
                );
                string expected = $"{(bytes / 1024.0 / 1024.0 / 1024.0 / 1024.0):0.##} TB";
                string found = FormattingHelpers.FormatBytes(bytes);
                Assert.AreEqual(expected, found);
            }
        }

        [Test]
        public void FormatPetaBytes()
        {
            for (int i = 0; i < NumTries; ++i)
            {
                long bytes = PRNG.Instance.NextLong(
                    1024L * 1024L * 1024L * 1024L * 1024L,
                    1024L * 1024L * 1024L * 1024L * 1024L * 1024L
                );
                string expected = $"{(bytes / 1024.0 / 1024.0 / 1024.0 / 1024.0 / 1024.0):0.##} PB";
                string found = FormattingHelpers.FormatBytes(bytes);
                Assert.AreEqual(expected, found);
            }
        }

        [Test]
        public void FormatExaBytes()
        {
            for (int i = 0; i < NumTries; ++i)
            {
                long bytes = PRNG.Instance.NextLong(
                    1024L * 1024L * 1024L * 1024L * 1024L * 1024L,
                    long.MaxValue
                );
                string expected =
                    $"{(bytes / 1024.0 / 1024.0 / 1024.0 / 1024.0 / 1024.0 / 1024.0):0.##} EB";
                string found = FormattingHelpers.FormatBytes(bytes);
                Assert.AreEqual(expected, found);
            }
        }
    }
}
