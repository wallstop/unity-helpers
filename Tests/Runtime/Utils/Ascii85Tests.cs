namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System;
    using System.Linq;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class Ascii85Tests
    {
        [Test]
        public void EncodeNullReturnsNull()
        {
            Assert.IsNull(Ascii85.Encode(null));
        }

        [Test]
        public void EncodeDecodeEmpty()
        {
            string encoded = Ascii85.Encode(Array.Empty<byte>());
            Assert.AreEqual(string.Empty, encoded);
            byte[] decoded = Ascii85.Decode(encoded);
            Assert.AreEqual(0, decoded.Length);
        }

        [Test]
        public void ZeroChunkUsesZShorthand()
        {
            byte[] bytes = new byte[4];
            string encoded = Ascii85.Encode(bytes);
            Assert.AreEqual("z", encoded);
            byte[] decoded = Ascii85.Decode(encoded);
            Assert.IsTrue(decoded.SequenceEqual(bytes));
        }

        [Test]
        public void RoundtripVariousLengths()
        {
            Random random = new(123);
            foreach (int length in new[] { 1, 2, 3, 4, 5, 7, 11, 16, 31 })
            {
                byte[] data = new byte[length];
                random.NextBytes(data);
                string encoded = Ascii85.Encode(data);
                byte[] decoded = Ascii85.Decode(encoded);
                Assert.AreEqual(data, decoded);
            }
        }

        [Test]
        public void DecodeHandlesExpandedZ()
        {
            byte[] decoded = Ascii85.Decode("!!!!!");
            Assert.AreEqual(new byte[] { 0, 0, 0, 0 }, decoded);
        }
    }
}
