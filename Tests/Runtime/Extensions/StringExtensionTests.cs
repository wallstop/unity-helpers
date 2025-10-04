namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System;
    using System.Linq;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Extension;

    public sealed class StringExtensionTests
    {
        [Test]
        public void ToPascalCaseNominal()
        {
            Assert.AreEqual("PascalCase", "PascalCase".ToPascalCase());
            Assert.AreEqual("PascalCase", "pascalCase".ToPascalCase());
            Assert.AreEqual("PascalCase", "_pascalCase".ToPascalCase());
            Assert.AreEqual("PascalCase", "_PascalCase".ToPascalCase());
            Assert.AreEqual("PascalCase", "pascal case".ToPascalCase());
            Assert.AreEqual("PascalCase", "  __Pascal____   ___Case__ ".ToPascalCase());
        }

        [Test]
        public void ToPascalCaseCustomSeparator()
        {
            const string separator = ".";
            Assert.AreEqual("Pascal.Case", "PascalCase".ToPascalCase(separator));
            Assert.AreEqual("Pascal.Case", "pascalCase".ToPascalCase(separator));
            Assert.AreEqual("Pascal.Case", "_pascalCase".ToPascalCase(separator));
            Assert.AreEqual("Pascal.Case", "_PascalCase".ToPascalCase(separator));
            Assert.AreEqual("Pascal.Case", "pascal case".ToPascalCase(separator));
            Assert.AreEqual("Pascal.Case", "  __Pascal____   ___Case__ ".ToPascalCase(separator));
        }

        [Test]
        public void ToPascalCaseEdgeCases()
        {
            Assert.AreEqual(string.Empty, string.Empty.ToPascalCase());
            Assert.AreEqual(string.Empty, ((string)null).ToPascalCase());
            Assert.AreEqual("A", "a".ToPascalCase());
            Assert.AreEqual("A", "A".ToPascalCase());
            Assert.AreEqual(string.Empty, "___".ToPascalCase());
            Assert.AreEqual("Abc", "ABC".ToPascalCase());
        }

        [Test]
        public void CenterNullOrShorterString()
        {
            Assert.AreEqual(null, ((string)null).Center(10));
            Assert.AreEqual("test", "test".Center(4));
            Assert.AreEqual("test", "test".Center(3));
            Assert.AreEqual("test", "test".Center(0));
            Assert.AreEqual("test", "test".Center(-1));
        }

        [Test]
        public void CenterPadsCorrectly()
        {
            Assert.AreEqual("  test  ", "test".Center(8));
            Assert.AreEqual(" test  ", "test".Center(7));
            Assert.AreEqual("  test   ", "test".Center(9));
            Assert.AreEqual("     a     ", "a".Center(11));
            Assert.AreEqual(string.Empty, string.Empty.Center(0));
        }

        [Test]
        public void GetBytesHandlesNullAndEmpty()
        {
            byte[] nullResult = ((string)null).GetBytes();
            Assert.IsNotNull(nullResult);
            Assert.AreEqual(0, nullResult.Length);

            byte[] emptyResult = string.Empty.GetBytes();
            Assert.IsNotNull(emptyResult);
            Assert.AreEqual(0, emptyResult.Length);
        }

        [Test]
        public void GetBytesConvertsString()
        {
            byte[] result = "test".GetBytes();
            Assert.IsNotNull(result);
            Assert.Greater(result.Length, 0);

            // Verify round-trip
            string converted = result.GetString();
            Assert.AreEqual("test", converted);
        }

        [Test]
        public void GetStringConvertsBytes()
        {
            byte[] bytes = new byte[] { 116, 101, 115, 116 }; // "test" in ASCII
            string result = bytes.GetString();
            Assert.IsNotNull(result);
            Assert.AreEqual("test", result);
        }

        [Test]
        public void GetBytesAndGetStringRoundTrip()
        {
            string original = "Hello, World! 123 @#$";
            byte[] bytes = original.GetBytes();
            string result = bytes.GetString();
            Assert.AreEqual(original, result);
        }

        [Test]
        public void ToJsonSerializesValue()
        {
            int intValue = 42;
            string json = intValue.ToJson();
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("42"));

            string stringValue = "test";
            string stringJson = stringValue.ToJson();
            Assert.IsNotNull(stringJson);
            Assert.IsTrue(stringJson.Contains("test"));
        }

        [Test]
        public void LevenshteinDistanceIdenticalStrings()
        {
            Assert.AreEqual(0, "test".LevenshteinDistance("test"));
            Assert.AreEqual(0, "".LevenshteinDistance(""));
            Assert.AreEqual(0, "hello world".LevenshteinDistance("hello world"));
            Assert.AreEqual(0, "A".LevenshteinDistance("A"));
        }

        [Test]
        public void LevenshteinDistanceEmptyStrings()
        {
            Assert.AreEqual(4, "test".LevenshteinDistance(""));
            Assert.AreEqual(5, "".LevenshteinDistance("hello"));
            Assert.AreEqual(0, "".LevenshteinDistance(""));
        }

        [Test]
        public void LevenshteinDistanceNullStrings()
        {
            Assert.AreEqual(4, "test".LevenshteinDistance(null));
            Assert.AreEqual(5, ((string)null).LevenshteinDistance("hello"));
            Assert.AreEqual(0, ((string)null).LevenshteinDistance(null));
        }

        [Test]
        public void LevenshteinDistanceSingleCharacterDifference()
        {
            Assert.AreEqual(1, "test".LevenshteinDistance("text"));
            Assert.AreEqual(1, "cat".LevenshteinDistance("bat"));
            Assert.AreEqual(1, "hello".LevenshteinDistance("hallo"));
        }

        [Test]
        public void LevenshteinDistanceInsertions()
        {
            Assert.AreEqual(1, "cat".LevenshteinDistance("cats"));
            Assert.AreEqual(2, "cat".LevenshteinDistance("catch"));
            Assert.AreEqual(3, "foo".LevenshteinDistance("foobar"));
        }

        [Test]
        public void LevenshteinDistanceDeletions()
        {
            Assert.AreEqual(1, "cats".LevenshteinDistance("cat"));
            Assert.AreEqual(2, "catch".LevenshteinDistance("cat"));
            Assert.AreEqual(3, "foobar".LevenshteinDistance("foo"));
        }

        [Test]
        public void LevenshteinDistanceCompletelyDifferent()
        {
            Assert.AreEqual(3, "abc".LevenshteinDistance("xyz"));
            Assert.AreEqual(4, "hello".LevenshteinDistance("world"));
        }

        [Test]
        public void LevenshteinDistanceClassicExamples()
        {
            Assert.AreEqual(3, "kitten".LevenshteinDistance("sitting"));
            Assert.AreEqual(3, "saturday".LevenshteinDistance("sunday"));
            Assert.AreEqual(2, "book".LevenshteinDistance("back"));
        }

        [Test]
        public void LevenshteinDistanceCaseSensitive()
        {
            Assert.AreEqual(1, "Test".LevenshteinDistance("test"));
            Assert.AreEqual(4, "TEST".LevenshteinDistance("test"));
        }

        [Test]
        public void NeedsLowerInvariantConversionReturnsFalseForNullOrWhitespace()
        {
            Assert.IsFalse(((string)null).NeedsLowerInvariantConversion());
            Assert.IsFalse(string.Empty.NeedsLowerInvariantConversion());
            Assert.IsFalse("   ".NeedsLowerInvariantConversion());
            Assert.IsFalse("\t\n\r".NeedsLowerInvariantConversion());
        }

        [Test]
        public void NeedsLowerInvariantConversionReturnsTrueForUpperCase()
        {
            Assert.IsTrue("Test".NeedsLowerInvariantConversion());
            Assert.IsTrue("TEST".NeedsLowerInvariantConversion());
            Assert.IsTrue("Hello World".NeedsLowerInvariantConversion());
            Assert.IsTrue("A".NeedsLowerInvariantConversion());
        }

        [Test]
        public void NeedsLowerInvariantConversionReturnsFalseForLowerCase()
        {
            Assert.IsFalse("test".NeedsLowerInvariantConversion());
            Assert.IsFalse("hello world".NeedsLowerInvariantConversion());
            Assert.IsFalse("a".NeedsLowerInvariantConversion());
            Assert.IsFalse("lowercase123".NeedsLowerInvariantConversion());
        }

        [Test]
        public void NeedsLowerInvariantConversionHandlesMixedCase()
        {
            Assert.IsTrue("helloWorld".NeedsLowerInvariantConversion());
            Assert.IsTrue("test123Test".NeedsLowerInvariantConversion());
            Assert.IsFalse("hello123".NeedsLowerInvariantConversion());
        }

        [Test]
        public void NeedsTrimReturnsFalseForNullOrEmpty()
        {
            Assert.IsFalse(((string)null).NeedsTrim());
            Assert.IsFalse(string.Empty.NeedsTrim());
        }

        [Test]
        public void NeedsTrimReturnsTrueForLeadingWhitespace()
        {
            Assert.IsTrue(" test".NeedsTrim());
            Assert.IsTrue("  test".NeedsTrim());
            Assert.IsTrue("\ttest".NeedsTrim());
            Assert.IsTrue("\ntest".NeedsTrim());
        }

        [Test]
        public void NeedsTrimReturnsTrueForTrailingWhitespace()
        {
            Assert.IsTrue("test ".NeedsTrim());
            Assert.IsTrue("test  ".NeedsTrim());
            Assert.IsTrue("test\t".NeedsTrim());
            Assert.IsTrue("test\n".NeedsTrim());
        }

        [Test]
        public void NeedsTrimReturnsTrueForBothEndsWhitespace()
        {
            Assert.IsTrue(" test ".NeedsTrim());
            Assert.IsTrue("  test  ".NeedsTrim());
            Assert.IsTrue("\ttest\n".NeedsTrim());
        }

        [Test]
        public void NeedsTrimReturnsFalseForNoWhitespace()
        {
            Assert.IsFalse("test".NeedsTrim());
            Assert.IsFalse("hello world".NeedsTrim());
            Assert.IsFalse("a".NeedsTrim());
            Assert.IsFalse("no trim needed".NeedsTrim());
        }

        [Test]
        public void NeedsTrimHandlesMiddleWhitespace()
        {
            Assert.IsFalse("hello world".NeedsTrim());
            Assert.IsFalse("test\tvalue".NeedsTrim());
        }
    }
}
