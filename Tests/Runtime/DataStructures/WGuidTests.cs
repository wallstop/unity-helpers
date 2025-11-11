namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System;
    using System.Buffers.Binary;
    using System.Text;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    public sealed class WGuidTests
    {
        [Test]
        public void DefaultValueIsEmpty()
        {
            WGuid guid = default;
            Assert.IsTrue(guid.IsEmpty);
            Assert.AreEqual(WGuid.EmptyGuid, guid);
        }

        [Test]
        public void NewGuidProducesVersion4()
        {
            WGuid guid = WGuid.NewGuid();
            Assert.IsFalse(guid.IsEmpty);
            Assert.IsTrue(guid.IsVersion4);
        }

        [Test]
        public void ConstructorFromGuidPreservesValue()
        {
            Guid expected = Guid.NewGuid();
            WGuid guid = new WGuid(expected);
            Assert.AreEqual(expected, guid.ToGuid());
            Assert.IsTrue(guid.IsVersion4);
        }

        [Test]
        public void ImplicitConversionRoundTripsGuid()
        {
            Guid source = Guid.NewGuid();
            WGuid wrapper = source;
            Guid converted = wrapper;
            Assert.AreEqual(source, converted);
            Assert.AreEqual(source, wrapper.ToGuid());
        }

        [Test]
        public void ToByteArrayMatchesGuidBytes()
        {
            Guid source = Guid.NewGuid();
            WGuid guid = new WGuid(source);
            byte[] expected = source.ToByteArray();
            byte[] actual = guid.ToByteArray();
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void TryWriteBytesWritesIntoSpan()
        {
            Guid source = Guid.NewGuid();
            WGuid guid = new WGuid(source);
            Span<byte> destination = stackalloc byte[16];
            bool success = guid.TryWriteBytes(destination);
            Assert.IsTrue(success);
            byte[] expected = source.ToByteArray();
            byte[] actual = new byte[16];
            destination.CopyTo(actual);
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void TryParseAcceptsValidString()
        {
            Guid source = Guid.NewGuid();
            bool success = WGuid.TryParse(source.ToString("D"), out WGuid parsed);
            Assert.IsTrue(success);
            Assert.AreEqual(source, parsed.ToGuid());
        }

        [Test]
        public void TryParseRejectsInvalidString()
        {
            bool success = WGuid.TryParse("not-a-guid", out WGuid parsed);
            Assert.IsFalse(success);
            Assert.AreEqual(WGuid.EmptyGuid, parsed);
        }

        [Test]
        public void TryParseRejectsNonVersion4Guid()
        {
            string versionOneGuid = "00000000-0000-1000-8000-000000000000";
            bool success = WGuid.TryParse(versionOneGuid, out WGuid parsed);
            Assert.IsFalse(success);
            Assert.AreEqual(WGuid.EmptyGuid, parsed);
        }

        [Test]
        public void StringConstructorRejectsNonVersionFourGuid()
        {
            Assert.Throws<FormatException>(() =>
                _ = new WGuid("00000000-0000-1000-8000-000000000000")
            );
        }

        [Test]
        public void ParseThrowsForInvalidGuid()
        {
            Assert.Throws<FormatException>(() =>
                WGuid.Parse("00000000-0000-1000-8000-000000000000")
            );
        }

        [Test]
        public void TryFormatWritesGuidString()
        {
            WGuid guid = WGuid.NewGuid();
            Span<char> destination = stackalloc char[36];
            bool formatted = guid.TryFormat(
                destination,
                out int charsWritten,
                ReadOnlySpan<char>.Empty
            );
            Assert.IsTrue(formatted);
            Assert.AreEqual(36, charsWritten);
            string formattedString = new string(destination.Slice(0, charsWritten));
            Assert.IsTrue(Guid.TryParse(formattedString, out Guid parsed));
            Assert.AreEqual(guid.ToGuid(), parsed);
        }

        [Test]
        public void CompareToMatchesGuidComparison()
        {
            WGuid left = WGuid.NewGuid();
            WGuid right = WGuid.NewGuid();
            int expected = left.ToGuid().CompareTo(right.ToGuid());
            int actual = left.CompareTo(right);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void EqualityUsesUnderlyingValue()
        {
            Guid source = Guid.NewGuid();
            WGuid first = new WGuid(source);
            WGuid second = new WGuid(source);
            Assert.IsTrue(first.Equals(second));
            Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
        }

        [Test]
        public void ByteConstructorRejectsInvalidLength()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("too_short");
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new WGuid(bytes));
        }
    }
}
