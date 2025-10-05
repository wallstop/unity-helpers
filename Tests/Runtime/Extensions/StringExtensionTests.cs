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
            byte[] bytes = { 116, 101, 115, 116 }; // "test" in ASCII
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

        [Test]
        public void TruncateHandlesNullAndEmpty()
        {
            Assert.AreEqual(null, ((string)null).Truncate(5));
            Assert.AreEqual(string.Empty, string.Empty.Truncate(5));
        }

        [Test]
        public void TruncateHandlesNegativeLength()
        {
            Assert.AreEqual("test", "test".Truncate(-1));
        }

        [Test]
        public void TruncateDoesNotTruncateShortStrings()
        {
            Assert.AreEqual("test", "test".Truncate(10));
            Assert.AreEqual("test", "test".Truncate(4));
        }

        [Test]
        public void TruncateWithDefaultEllipsis()
        {
            Assert.AreEqual("hel...", "hello world".Truncate(6));
            Assert.AreEqual("...", "hello".Truncate(3));
            Assert.AreEqual("hello w...", "hello world".Truncate(10));
        }

        [Test]
        public void TruncateWithCustomEllipsis()
        {
            Assert.AreEqual("hel--", "hello world".Truncate(5, "--"));
            Assert.AreEqual("hello world", "hello world".Truncate(12, " [more]"));
        }

        [Test]
        public void TruncateWithEmptyEllipsis()
        {
            Assert.AreEqual("hello", "hello world".Truncate(5, ""));
            Assert.AreEqual("hel", "hello".Truncate(3, ""));
        }

        [Test]
        public void TruncateWithEllipsisLongerThanMaxLength()
        {
            Assert.AreEqual("...", "hello".Truncate(3, "..."));
            Assert.AreEqual("...", "hello".Truncate(2, "..."));
        }

        [Test]
        public void ToCamelCaseFromPascalCase()
        {
            Assert.AreEqual("pascalCase", "PascalCase".ToCamelCase());
            Assert.AreEqual("helloWorld", "HelloWorld".ToCamelCase());
            Assert.AreEqual("a", "A".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseFromSnakeCase()
        {
            Assert.AreEqual("snakeCase", "snake_case".ToCamelCase());
            Assert.AreEqual("helloWorldTest", "hello_world_test".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseEdgeCases()
        {
            Assert.AreEqual(string.Empty, ((string)null).ToCamelCase());
            Assert.AreEqual(string.Empty, string.Empty.ToCamelCase());
            Assert.AreEqual(string.Empty, "___".ToCamelCase());
            Assert.AreEqual("abc", "ABC".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseFromKebabCase()
        {
            Assert.AreEqual("kebabCase", "kebab-case".ToCamelCase());
            Assert.AreEqual("helloWorldTest", "hello-world-test".ToCamelCase());
            Assert.AreEqual("myVariable", "my-variable".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseFromSpaces()
        {
            Assert.AreEqual("helloWorld", "hello world".ToCamelCase());
            Assert.AreEqual("testCase", "test case".ToCamelCase());
            Assert.AreEqual("multipleWords", "multiple  words".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseMultipleConsecutiveUppercase()
        {
            Assert.AreEqual("xmlHttpRequest", "XMLHttpRequest".ToCamelCase());
            Assert.AreEqual("htmlParser", "HTMLParser".ToCamelCase());
            Assert.AreEqual("ioError", "IOError".ToCamelCase());
            Assert.AreEqual("ioException", "IOException".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseWithNumbers()
        {
            Assert.AreEqual("test123", "test123".ToCamelCase());
            Assert.AreEqual("test123Test", "test_123_test".ToCamelCase());
            Assert.AreEqual("version2", "version2".ToCamelCase());
            Assert.AreEqual("version2Beta", "version_2_beta".ToCamelCase());
            Assert.AreEqual("my3DModel", "my_3D_model".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseSingleCharacter()
        {
            Assert.AreEqual("a", "a".ToCamelCase());
            Assert.AreEqual("a", "A".ToCamelCase());
            Assert.AreEqual("x", "x".ToCamelCase());
            Assert.AreEqual("z", "Z".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseSingleWord()
        {
            Assert.AreEqual("test", "test".ToCamelCase());
            Assert.AreEqual("test", "TEST".ToCamelCase());
            Assert.AreEqual("test", "Test".ToCamelCase());
            Assert.AreEqual("hello", "hello".ToCamelCase());
            Assert.AreEqual("hello", "HELLO".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseOnlySeparators()
        {
            Assert.AreEqual(string.Empty, "___".ToCamelCase());
            Assert.AreEqual(string.Empty, "---".ToCamelCase());
            Assert.AreEqual(string.Empty, "   ".ToCamelCase());
            Assert.AreEqual(string.Empty, "_-_ ".ToCamelCase());
            Assert.AreEqual(string.Empty, "...".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseLeadingAndTrailingSeparators()
        {
            Assert.AreEqual("testCase", "_test_case".ToCamelCase());
            Assert.AreEqual("testCase", "test_case_".ToCamelCase());
            Assert.AreEqual("testCase", "_test_case_".ToCamelCase());
            Assert.AreEqual("testCase", "-test-case-".ToCamelCase());
            Assert.AreEqual("testCase", "__test__case__".ToCamelCase());
            Assert.AreEqual("testCase", "  test  case  ".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseMultipleSeparators()
        {
            Assert.AreEqual("testCase", "test__case".ToCamelCase());
            Assert.AreEqual("testCase", "test--case".ToCamelCase());
            Assert.AreEqual("testCase", "test__--__case".ToCamelCase());
            Assert.AreEqual("testCase", "test   case".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseMixedSeparators()
        {
            Assert.AreEqual("thisIsATest", "this_is-a_test".ToCamelCase());
            Assert.AreEqual("helloWorldTest", "hello-world_test".ToCamelCase());
            Assert.AreEqual("mixedCaseTest", "mixed_Case-Test".ToCamelCase());
            Assert.AreEqual("testCase", "test.case".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseAllSeparators()
        {
            Assert.AreEqual("testCase", "test_case".ToCamelCase());
            Assert.AreEqual("testCase", "test-case".ToCamelCase());
            Assert.AreEqual("testCase", "test case".ToCamelCase());
            Assert.AreEqual("testCase", "test.case".ToCamelCase());
            Assert.AreEqual("testCase", "test\tcase".ToCamelCase());
            Assert.AreEqual("testCase", "test\ncase".ToCamelCase());
            Assert.AreEqual("testCase", "test\rcase".ToCamelCase());
            Assert.AreEqual("testCase", "test\"Case".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseUppercaseWords()
        {
            Assert.AreEqual("thisIsATest", "THIS-IS-A-TEST".ToCamelCase());
            Assert.AreEqual("helloWorld", "HELLO_WORLD".ToCamelCase());
            Assert.AreEqual("abc", "ABC".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseMixedCamelAndSnake()
        {
            Assert.AreEqual("helloWorldTest", "helloWorld_test".ToCamelCase());
            Assert.AreEqual("testCaseExample", "testCase_example".ToCamelCase());
            Assert.AreEqual("myTestValue", "myTest_value".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseWithNumbersAndSeparators()
        {
            Assert.AreEqual("test123Case456", "test_123_case_456".ToCamelCase());
            Assert.AreEqual("version2Update", "version-2-update".ToCamelCase());
            Assert.AreEqual("test1Test2", "test1_test2".ToCamelCase());
            Assert.AreEqual("api2Endpoint", "api_2_endpoint".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseSpecialCharacterSeparators()
        {
            Assert.AreEqual("testCase", "test'Case".ToCamelCase());
            Assert.AreEqual("helloWorld", "hello\"World".ToCamelCase());
            Assert.AreEqual("mixedTest", "mixed.'Test".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseComplexStrings()
        {
            Assert.AreEqual("thisIsAComplexTest", "this_is_a_complex_test".ToCamelCase());
            Assert.AreEqual("thisIsAComplexTest", "THIS_IS_A_COMPLEX_TEST".ToCamelCase());
            Assert.AreEqual("thisIsAComplexTest", "this-is-a-complex-test".ToCamelCase());
            Assert.AreEqual("thisIsAComplexTest", "ThisIsAComplexTest".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseAlreadyCamelCase()
        {
            Assert.AreEqual("camelCase", "camelCase".ToCamelCase());
            Assert.AreEqual("alreadyCamelCase", "alreadyCamelCase".ToCamelCase());
            Assert.AreEqual("myVariableName", "myVariableName".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseNumbersAtBoundaries()
        {
            Assert.AreEqual("test123", "Test123".ToCamelCase());
            Assert.AreEqual("123test", "123test".ToCamelCase());
            Assert.AreEqual("123Test", "123Test".ToCamelCase());
            Assert.AreEqual("test123Test", "test123Test".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseConsecutiveNumbers()
        {
            Assert.AreEqual("test123456", "test123456".ToCamelCase());
            Assert.AreEqual("test123456End", "test123456End".ToCamelCase());
            Assert.AreEqual("abc123def456", "abc123def456".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseMixedNumbersAndUppercase()
        {
            Assert.AreEqual("user123Name", "user123Name".ToCamelCase());
            Assert.AreEqual("test456Value", "Test456Value".ToCamelCase());
            Assert.AreEqual("model3D", "model3D".ToCamelCase());
            Assert.AreEqual("http2Client", "http2Client".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseIdempotent()
        {
            // Applying ToCamelCase twice should give the same result
            Assert.AreEqual("testValue", "testValue".ToCamelCase());
            Assert.AreEqual("helloWorld", "helloWorld".ToCamelCase());

            // Verify idempotence
            string test1 = "TestValue123";
            string camel1 = test1.ToCamelCase();
            string camel2 = camel1.ToCamelCase();
            Assert.AreEqual(camel1, camel2);

            string test2 = "hello_world_test";
            string camel3 = test2.ToCamelCase();
            string camel4 = camel3.ToCamelCase();
            Assert.AreEqual(camel3, camel4);
        }

        [Test]
        public void ToCamelCasePerformanceEdgeCases()
        {
            // Very long strings
            string longString = "a".Repeat(100);
            Assert.AreEqual(longString, longString.ToCamelCase());

            string longPascal = "Test" + "Value".Repeat(50);
            string result = longPascal.ToCamelCase();
            Assert.IsTrue(char.IsLower(result[0]));

            // Many separators
            string manySeparators = "test_value_test_value_test_value_test_value";
            string camelResult = manySeparators.ToCamelCase();
            Assert.IsTrue(char.IsLower(camelResult[0]));
            Assert.IsFalse(camelResult.Contains("_"));
        }

        [Test]
        public void ToCamelCaseUnicodeCharacters()
        {
            // Test with non-ASCII characters
            Assert.AreEqual("tëstCäse", "tëst_cäse".ToCamelCase());
            Assert.AreEqual("hëlloWörld", "hëllo_wörld".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseWhitespaceVariations()
        {
            Assert.AreEqual("testCase", "test\tcase".ToCamelCase());
            Assert.AreEqual("testCase", "test\ncase".ToCamelCase());
            Assert.AreEqual("testCase", "test\rcase".ToCamelCase());
            Assert.AreEqual("testCase", "test\r\ncase".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseNumbersOnly()
        {
            Assert.AreEqual("123", "123".ToCamelCase());
            Assert.AreEqual("456789", "456789".ToCamelCase());
            Assert.AreEqual("0", "0".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseMixedCasingPatterns()
        {
            Assert.AreEqual("tEstCase", "TEstCase".ToCamelCase());
            Assert.AreEqual("hElLoWoRlD", "HElLO_WoRlD".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseAcronymsAndAbbreviations()
        {
            Assert.AreEqual("httpRequest", "HTTPRequest".ToCamelCase());
            Assert.AreEqual("xmlParser", "XMLParser".ToCamelCase());
            Assert.AreEqual("ioStream", "IOStream".ToCamelCase());
            Assert.AreEqual("urlPath", "URLPath".ToCamelCase());
            Assert.AreEqual("apiKey", "APIKey".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseQuotesAndApostrophes()
        {
            Assert.AreEqual("dontStop", "don't_stop".ToCamelCase());
            Assert.AreEqual("itsWorking", "it's_working".ToCamelCase());
            Assert.AreEqual("helloWorld", "hello\"World".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseDotsAndPeriods()
        {
            Assert.AreEqual("fileName", "file.name".ToCamelCase());
            Assert.AreEqual("testCaseValue", "test.case.value".ToCamelCase());
            Assert.AreEqual("myTestFile", "my.test.file".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseConversionFromAllFormats()
        {
            string expected = "myVariableName";
            Assert.AreEqual(expected, "MyVariableName".ToCamelCase()); // PascalCase
            Assert.AreEqual(expected, "my_variable_name".ToCamelCase()); // snake_case
            Assert.AreEqual(expected, "my-variable-name".ToCamelCase()); // kebab-case
            Assert.AreEqual(expected, "my variable name".ToCamelCase()); // space separated
            Assert.AreEqual(expected, "MY_VARIABLE_NAME".ToCamelCase()); // SCREAMING_SNAKE_CASE
        }

        [Test]
        public void ToCamelCaseExtremeEdgeCases()
        {
            // Only uppercase
            Assert.AreEqual("aaaa", "AAAA".ToCamelCase());

            // Only lowercase (should remain lowercase)
            Assert.AreEqual("aaaa", "aaaa".ToCamelCase());

            // Alternating case
            Assert.AreEqual("aBaBaB", "aBaBaB".ToCamelCase());

            // Mixed with all separator types
            Assert.AreEqual("testValueExample", "test_value-example".ToCamelCase());
            Assert.AreEqual("myTestCaseHere", "my test-case_here".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseVeryLongStrings()
        {
            // Test with very long input to ensure performance
            string longInput = string.Join("_", Enumerable.Range(0, 100).Select(i => "word" + i));
            string result = longInput.ToCamelCase();
            Assert.IsTrue(char.IsLower(result[0]));
            Assert.IsFalse(result.Contains("_"));
            Assert.IsTrue(result.Length > 0);

            // Verify it starts correctly
            Assert.IsTrue(result.StartsWith("word0"));
        }

        [Test]
        public void ToCamelCaseRepeatedConversions()
        {
            // Ensure repeated conversions are stable (idempotent)
            string[] testCases =
            {
                "hello_world",
                "HelloWorld",
                "HELLO_WORLD",
                "helloWorld",
                "hello-world",
                "hello world",
            };

            foreach (string testCase in testCases)
            {
                string first = testCase.ToCamelCase();
                string second = first.ToCamelCase();
                string third = second.ToCamelCase();

                Assert.AreEqual(first, second, $"Failed for {testCase}: first != second");
                Assert.AreEqual(second, third, $"Failed for {testCase}: second != third");
                Assert.IsTrue(
                    char.IsLower(first[0]) || char.IsDigit(first[0]) || !char.IsLetter(first[0]),
                    $"First character should be lowercase or non-letter for {testCase}"
                );
            }
        }

        [Test]
        public void ToCamelCaseWithManyConsecutiveCapitals()
        {
            Assert.AreEqual("abcdefg", "ABCDEFG".ToCamelCase());
            Assert.AreEqual("xmlHttpsApiRequest", "XmlHttpsAPIRequest".ToCamelCase());
            Assert.AreEqual("ioExceptionError", "IOExceptionError".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseWithNumbersBetweenWords()
        {
            Assert.AreEqual("word1Word2Word3", "word1_word2_word3".ToCamelCase());
            Assert.AreEqual("word1Word2Word3", "Word1_Word2_Word3".ToCamelCase());
            Assert.AreEqual("test123Middle456End", "test_123_middle_456_end".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseSingleLetterWords()
        {
            Assert.AreEqual("aBC", "a_b_c".ToCamelCase());
            Assert.AreEqual("aBC", "A_B_C".ToCamelCase());
            Assert.AreEqual("aBCDEF", "a_b_c_d_e_f".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseWithTrailingNumbers()
        {
            Assert.AreEqual("test1", "test1".ToCamelCase());
            Assert.AreEqual("test123", "Test123".ToCamelCase());
            Assert.AreEqual("myValue2", "my_value2".ToCamelCase());
            Assert.AreEqual("myValue42", "MyValue42".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseWithLeadingNumbers()
        {
            Assert.AreEqual("1test", "1test".ToCamelCase());
            Assert.AreEqual("123Test", "123Test".ToCamelCase());
            Assert.AreEqual("2FastAnd2Furious", "2_fast_and_2_furious".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseRoundTripWithOtherFormats()
        {
            string original = "myVariableName";

            // Convert to various formats and back
            string snake = original.ToSnakeCase();
            string camelFromSnake = snake.ToCamelCase();
            Assert.AreEqual(original, camelFromSnake);

            string kebab = original.ToKebabCase();
            string camelFromKebab = kebab.ToCamelCase();
            Assert.AreEqual(original, camelFromKebab);

            string pascal = original.ToPascalCase();
            string camelFromPascal = pascal.ToCamelCase();
            Assert.AreEqual(original, camelFromPascal);
        }

        [Test]
        public void ToCamelCaseEmptyPascalCaseResult()
        {
            // When ToPascalCase returns empty, ToCamelCase should too
            Assert.AreEqual(string.Empty, "___".ToCamelCase());
            Assert.AreEqual(string.Empty, "---".ToCamelCase());
            Assert.AreEqual(string.Empty, "   ".ToCamelCase());
        }

        [Test]
        public void ToCamelCasePreservesNumbersAndSpecialPatterns()
        {
            Assert.AreEqual("version2Point0", "version2Point0".ToCamelCase());
            Assert.AreEqual("version2Point0", "Version2Point0".ToCamelCase());
            Assert.AreEqual("iPhone11", "iPhone11".ToCamelCase());
            Assert.AreEqual("iOs14", "iOS14".ToCamelCase());
        }

        [Test]
        public void ToCamelCaseStressTest()
        {
            // Multiple conversions with different patterns
            string[] patterns =
            {
                "simple",
                "Simple",
                "SIMPLE",
                "simple_case",
                "SimpleCase",
                "SIMPLE_CASE",
                "simple-case",
                "simple case",
                "simpleCase",
                "a",
                "A",
                "aB",
                "AB",
                "a1",
                "A1",
                "1a",
                "1A",
                "XMLHttpRequest",
                "xml_http_request",
                "xml-http-request",
            };

            foreach (string pattern in patterns)
            {
                string result = pattern.ToCamelCase();
                // Basic invariants
                Assert.IsNotNull(result, $"Result should not be null for: {pattern}");

                if (result.Length > 0)
                {
                    // First character should be lowercase or a non-letter
                    Assert.IsTrue(
                        char.IsLower(result[0]) || !char.IsLetter(result[0]),
                        $"First char should be lowercase or non-letter for {pattern}, got: {result}"
                    );
                }

                // Should not contain separators
                Assert.IsFalse(
                    result.Contains("_"),
                    $"Should not contain underscore: {pattern} -> {result}"
                );
                Assert.IsFalse(
                    result.Contains("-"),
                    $"Should not contain dash: {pattern} -> {result}"
                );
                Assert.IsFalse(
                    result.Contains(" "),
                    $"Should not contain space: {pattern} -> {result}"
                );

                // Idempotent
                string second = result.ToCamelCase();
                Assert.AreEqual(result, second, $"Should be idempotent for: {pattern}");
            }
        }

        [Test]
        public void ToSnakeCaseFromPascalCase()
        {
            Assert.AreEqual("pascal_case", "PascalCase".ToSnakeCase());
            Assert.AreEqual("hello_world", "HelloWorld".ToSnakeCase());
        }

        [Test]
        public void ToSnakeCaseFromCamelCase()
        {
            Assert.AreEqual("camel_case", "camelCase".ToSnakeCase());
            Assert.AreEqual("hello_world_test", "helloWorldTest".ToSnakeCase());
        }

        [Test]
        public void ToSnakeCaseHandlesConsecutiveUppercase()
        {
            Assert.AreEqual("html_parser", "HTMLParser".ToSnakeCase());
            Assert.AreEqual("xml_http_request", "XMLHttpRequest".ToSnakeCase());
            Assert.AreEqual("io_exception", "IOException".ToSnakeCase());
        }

        [Test]
        public void ToSnakeCaseEdgeCases()
        {
            Assert.AreEqual(string.Empty, ((string)null).ToSnakeCase());
            Assert.AreEqual(string.Empty, string.Empty.ToSnakeCase());
            Assert.AreEqual("a", "A".ToSnakeCase());
            Assert.AreEqual("abc", "abc".ToSnakeCase());
            Assert.AreEqual(string.Empty, "___".ToSnakeCase());
        }

        [Test]
        public void ToSnakeCaseHandlesSeparators()
        {
            Assert.AreEqual("hello_world", "hello world".ToSnakeCase());
            Assert.AreEqual("hello_world", "hello__world".ToSnakeCase());
            Assert.AreEqual("hello_world", "hello   world".ToSnakeCase());
        }

        [Test]
        public void ToKebabCaseFromPascalCase()
        {
            Assert.AreEqual("pascal-case", "PascalCase".ToKebabCase());
            Assert.AreEqual("hello-world", "HelloWorld".ToKebabCase());
        }

        [Test]
        public void ToKebabCaseFromCamelCase()
        {
            Assert.AreEqual("camel-case", "camelCase".ToKebabCase());
            Assert.AreEqual("hello-world-test", "helloWorldTest".ToKebabCase());
        }

        [Test]
        public void ToKebabCaseEdgeCases()
        {
            Assert.AreEqual(string.Empty, ((string)null).ToKebabCase());
            Assert.AreEqual(string.Empty, string.Empty.ToKebabCase());
            Assert.AreEqual("html-parser", "HTMLParser".ToKebabCase());
        }

        [Test]
        public void ToTitleCaseNominal()
        {
            Assert.AreEqual("Hello World", "hello world".ToTitleCase());
            Assert.AreEqual("The Quick Brown Fox", "the quick brown fox".ToTitleCase());
            Assert.AreEqual("A", "a".ToTitleCase());
        }

        [Test]
        public void ToTitleCasePreservesWhitespace()
        {
            Assert.AreEqual("Hello  World", "hello  world".ToTitleCase());
            Assert.AreEqual("Hello\tWorld", "hello\tworld".ToTitleCase());
        }

        [Test]
        public void ToTitleCaseEdgeCases()
        {
            Assert.AreEqual(string.Empty, ((string)null).ToTitleCase());
            Assert.AreEqual(string.Empty, string.Empty.ToTitleCase());
            Assert.AreEqual("Hello World", "HELLO WORLD".ToTitleCase());
            Assert.AreEqual("Hello World", "hELLO wORLD".ToTitleCase());
        }

        [Test]
        public void ToTitleCaseHandlesSeparators()
        {
            Assert.AreEqual("Hello_World", "hello_world".ToTitleCase());
            Assert.AreEqual("Hello's World", "hello's world".ToTitleCase());
        }

        [Test]
        public void ContainsIgnoreCaseNominal()
        {
            Assert.IsTrue("Hello World".ContainsIgnoreCase("hello"));
            Assert.IsTrue("Hello World".ContainsIgnoreCase("WORLD"));
            Assert.IsTrue("Hello World".ContainsIgnoreCase("o W"));
            Assert.IsTrue("test".ContainsIgnoreCase("TEST"));
        }

        [Test]
        public void ContainsIgnoreCaseReturnsFalseWhenNotFound()
        {
            Assert.IsFalse("Hello World".ContainsIgnoreCase("foo"));
            Assert.IsFalse("test".ContainsIgnoreCase("testing"));
        }

        [Test]
        public void ContainsIgnoreCaseHandlesNulls()
        {
            Assert.IsFalse(((string)null).ContainsIgnoreCase("test"));
            Assert.IsFalse("test".ContainsIgnoreCase(null));
            Assert.IsFalse(((string)null).ContainsIgnoreCase(null));
        }

        [Test]
        public void EqualsIgnoreCaseNominal()
        {
            Assert.IsTrue("Hello".EqualsIgnoreCase("hello"));
            Assert.IsTrue("WORLD".EqualsIgnoreCase("world"));
            Assert.IsTrue("TeSt".EqualsIgnoreCase("tEsT"));
        }

        [Test]
        public void EqualsIgnoreCaseReturnsFalseForDifferent()
        {
            Assert.IsFalse("Hello".EqualsIgnoreCase("World"));
            Assert.IsFalse("test".EqualsIgnoreCase("testing"));
        }

        [Test]
        public void EqualsIgnoreCaseHandlesNulls()
        {
            Assert.IsTrue(((string)null).EqualsIgnoreCase(null));
            Assert.IsFalse("test".EqualsIgnoreCase(null));
            Assert.IsFalse(((string)null).EqualsIgnoreCase("test"));
        }

        [Test]
        public void ReverseNominal()
        {
            Assert.AreEqual("tset", "test".Reverse());
            Assert.AreEqual("dlrow olleh", "hello world".Reverse());
            Assert.AreEqual("a", "a".Reverse());
        }

        [Test]
        public void ReverseEdgeCases()
        {
            Assert.AreEqual(null, ((string)null).Reverse());
            Assert.AreEqual(string.Empty, string.Empty.Reverse());
        }

        [Test]
        public void ReverseIsPalindrome()
        {
            Assert.AreEqual("racecar", "racecar".Reverse());
            Assert.AreEqual("noon", "noon".Reverse());
        }

        [Test]
        public void RemoveWhitespaceNominal()
        {
            Assert.AreEqual("helloworld", "hello world".RemoveWhitespace());
            Assert.AreEqual("test", "  test  ".RemoveWhitespace());
            Assert.AreEqual("abc", "a b c".RemoveWhitespace());
        }

        [Test]
        public void RemoveWhitespaceHandlesAllTypes()
        {
            Assert.AreEqual("test", "t\te\ns\rt ".RemoveWhitespace());
            Assert.AreEqual("abc", "  a  b  c  ".RemoveWhitespace());
        }

        [Test]
        public void RemoveWhitespaceEdgeCases()
        {
            Assert.AreEqual(null, ((string)null).RemoveWhitespace());
            Assert.AreEqual(string.Empty, string.Empty.RemoveWhitespace());
            Assert.AreEqual(string.Empty, "   ".RemoveWhitespace());
        }

        [Test]
        public void CountOccurrencesCharNominal()
        {
            Assert.AreEqual(2, "hello".CountOccurrences('l'));
            Assert.AreEqual(1, "hello".CountOccurrences('h'));
            Assert.AreEqual(0, "hello".CountOccurrences('x'));
        }

        [Test]
        public void CountOccurrencesCharEdgeCases()
        {
            Assert.AreEqual(0, ((string)null).CountOccurrences('a'));
            Assert.AreEqual(0, string.Empty.CountOccurrences('a'));
            Assert.AreEqual(5, "aaaaa".CountOccurrences('a'));
        }

        [Test]
        public void CountOccurrencesStringNominal()
        {
            Assert.AreEqual(2, "hello world hello".CountOccurrences("hello"));
            Assert.AreEqual(1, "test".CountOccurrences("test"));
            Assert.AreEqual(0, "hello".CountOccurrences("world"));
        }

        [Test]
        public void CountOccurrencesStringOverlapping()
        {
            Assert.AreEqual(2, "aaaa".CountOccurrences("aa"));
            Assert.AreEqual(4, "abababab".CountOccurrences("ab"));
        }

        [Test]
        public void CountOccurrencesStringEdgeCases()
        {
            Assert.AreEqual(0, ((string)null).CountOccurrences("test"));
            Assert.AreEqual(0, "test".CountOccurrences(null));
            Assert.AreEqual(0, string.Empty.CountOccurrences("test"));
            Assert.AreEqual(0, "test".CountOccurrences(string.Empty));
        }

        [Test]
        public void IsNumericNominal()
        {
            Assert.IsTrue("123".IsNumeric());
            Assert.IsTrue("0".IsNumeric());
            Assert.IsTrue("9876543210".IsNumeric());
        }

        [Test]
        public void IsNumericReturnsFalseForNonNumeric()
        {
            Assert.IsFalse("abc".IsNumeric());
            Assert.IsFalse("123abc".IsNumeric());
            Assert.IsFalse("12.34".IsNumeric());
            Assert.IsFalse("-123".IsNumeric());
            Assert.IsFalse("1 2 3".IsNumeric());
        }

        [Test]
        public void IsNumericEdgeCases()
        {
            Assert.IsFalse(((string)null).IsNumeric());
            Assert.IsFalse(string.Empty.IsNumeric());
            Assert.IsFalse(" ".IsNumeric());
        }

        [Test]
        public void IsAlphabeticNominal()
        {
            Assert.IsTrue("abc".IsAlphabetic());
            Assert.IsTrue("ABC".IsAlphabetic());
            Assert.IsTrue("AbCdEf".IsAlphabetic());
        }

        [Test]
        public void IsAlphabeticReturnsFalseForNonAlphabetic()
        {
            Assert.IsFalse("abc123".IsAlphabetic());
            Assert.IsFalse("123".IsAlphabetic());
            Assert.IsFalse("a b c".IsAlphabetic());
            Assert.IsFalse("hello world".IsAlphabetic());
        }

        [Test]
        public void IsAlphabeticEdgeCases()
        {
            Assert.IsFalse(((string)null).IsAlphabetic());
            Assert.IsFalse(string.Empty.IsAlphabetic());
            Assert.IsFalse(" ".IsAlphabetic());
        }

        [Test]
        public void IsAlphanumericNominal()
        {
            Assert.IsTrue("abc123".IsAlphanumeric());
            Assert.IsTrue("ABC".IsAlphanumeric());
            Assert.IsTrue("123".IsAlphanumeric());
            Assert.IsTrue("Test123".IsAlphanumeric());
        }

        [Test]
        public void IsAlphanumericReturnsFalseForNonAlphanumeric()
        {
            Assert.IsFalse("abc 123".IsAlphanumeric());
            Assert.IsFalse("hello-world".IsAlphanumeric());
            Assert.IsFalse("test!".IsAlphanumeric());
        }

        [Test]
        public void IsAlphanumericEdgeCases()
        {
            Assert.IsFalse(((string)null).IsAlphanumeric());
            Assert.IsFalse(string.Empty.IsAlphanumeric());
            Assert.IsFalse(" ".IsAlphanumeric());
        }

        [Test]
        public void ToBase64AndFromBase64RoundTrip()
        {
            string original = "Hello, World!";
            string base64 = original.ToBase64();
            Assert.IsNotNull(base64);
            Assert.AreNotEqual(original, base64);

            string decoded = base64.FromBase64();
            Assert.AreEqual(original, decoded);
        }

        [Test]
        public void ToBase64HandlesSpecialCharacters()
        {
            string original = "Test @#$%^&*()";
            string base64 = original.ToBase64();
            string decoded = base64.FromBase64();
            Assert.AreEqual(original, decoded);
        }

        [Test]
        public void ToBase64EdgeCases()
        {
            Assert.AreEqual(string.Empty, ((string)null).ToBase64());
            Assert.AreEqual(string.Empty, string.Empty.ToBase64());
        }

        [Test]
        public void FromBase64HandlesInvalidInput()
        {
            Assert.AreEqual(string.Empty, "not valid base64!@#".FromBase64());
            Assert.AreEqual(string.Empty, ((string)null).FromBase64());
            Assert.AreEqual(string.Empty, string.Empty.FromBase64());
        }

        [Test]
        public void RepeatNominal()
        {
            Assert.AreEqual("aaa", "a".Repeat(3));
            Assert.AreEqual("testtest", "test".Repeat(2));
            Assert.AreEqual("hello", "hello".Repeat(1));
        }

        [Test]
        public void RepeatEdgeCases()
        {
            Assert.AreEqual(string.Empty, ((string)null).Repeat(5));
            Assert.AreEqual(string.Empty, string.Empty.Repeat(5));
            Assert.AreEqual(string.Empty, "test".Repeat(0));
            Assert.AreEqual(string.Empty, "test".Repeat(-1));
        }

        [Test]
        public void RepeatLargeCount()
        {
            string result = "a".Repeat(100);
            Assert.AreEqual(100, result.Length);
            Assert.IsTrue(result.All(c => c == 'a'));
        }

        [Test]
        public void SplitCamelCaseNominal()
        {
            string[] result = "HelloWorld".SplitCamelCase();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual("Hello", result[0]);
            Assert.AreEqual("World", result[1]);
        }

        [Test]
        public void SplitCamelCaseHandlesConsecutiveUppercase()
        {
            string[] result = "HTMLParser".SplitCamelCase();
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual("HTML", result[0]);
            Assert.AreEqual("Parser", result[1]);
        }

        [Test]
        public void SplitCamelCaseHandlesSeparators()
        {
            string[] result = "hello_world_test".SplitCamelCase();
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual("hello", result[0]);
            Assert.AreEqual("world", result[1]);
            Assert.AreEqual("test", result[2]);
        }

        [Test]
        public void SplitCamelCaseEdgeCases()
        {
            Assert.AreEqual(0, ((string)null).SplitCamelCase().Length);
            Assert.AreEqual(0, string.Empty.SplitCamelCase().Length);
            Assert.AreEqual(1, "test".SplitCamelCase().Length);
        }

        [Test]
        public void SplitCamelCaseMixed()
        {
            string[] result = "XMLHttpRequest".SplitCamelCase();
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual("XML", result[0]);
            Assert.AreEqual("Http", result[1]);
            Assert.AreEqual("Request", result[2]);
        }

        [Test]
        public void ReplaceFirstNominal()
        {
            Assert.AreEqual("bbc", "abc".ReplaceFirst("a", "b"));
            Assert.AreEqual("test world test", "hello world test".ReplaceFirst("hello", "test"));
            Assert.AreEqual("abcdefabc", "abcabc".ReplaceFirst("abc", "abcdef"));
        }

        [Test]
        public void ReplaceFirstOnlyReplacesFirst()
        {
            Assert.AreEqual("xc abc abc", "abc abc abc".ReplaceFirst("ab", "x"));
            Assert.AreEqual("test test hello", "hello test hello".ReplaceFirst("hello", "test"));
        }

        [Test]
        public void ReplaceFirstEdgeCases()
        {
            Assert.AreEqual(null, ((string)null).ReplaceFirst("a", "b"));
            Assert.AreEqual(string.Empty, string.Empty.ReplaceFirst("a", "b"));
            Assert.AreEqual("abc", "abc".ReplaceFirst("x", "y"));
            Assert.AreEqual("abc", "abc".ReplaceFirst("", "x"));
        }

        [Test]
        public void ReplaceFirstWithNull()
        {
            Assert.AreEqual("bc", "abc".ReplaceFirst("a", null));
        }

        [Test]
        public void ReplaceLastNominal()
        {
            Assert.AreEqual("abb", "abc".ReplaceLast("c", "b"));
            Assert.AreEqual("hello test world", "hello test test".ReplaceLast("test", "world"));
            Assert.AreEqual("abcabcdef", "abcabc".ReplaceLast("abc", "abcdef"));
        }

        [Test]
        public void ReplaceLastOnlyReplacesLast()
        {
            Assert.AreEqual("abc abc xc", "abc abc abc".ReplaceLast("ab", "x"));
            Assert.AreEqual("hello test test", "hello test hello".ReplaceLast("hello", "test"));
        }

        [Test]
        public void ReplaceLastEdgeCases()
        {
            Assert.AreEqual(null, ((string)null).ReplaceLast("a", "b"));
            Assert.AreEqual(string.Empty, string.Empty.ReplaceLast("a", "b"));
            Assert.AreEqual("abc", "abc".ReplaceLast("x", "y"));
            Assert.AreEqual("abc", "abc".ReplaceLast("", "x"));
        }

        [Test]
        public void ReplaceLastWithNull()
        {
            Assert.AreEqual("ab", "abc".ReplaceLast("c", null));
        }

        [Test]
        public void GetBytesHandlesUnicodeCharacters()
        {
            string unicode = "Hello 世界 🌍";
            byte[] bytes = unicode.GetBytes();
            Assert.IsNotNull(bytes);
            Assert.Greater(bytes.Length, unicode.Length);

            string decoded = bytes.GetString();
            Assert.AreEqual(unicode, decoded);
        }

        [Test]
        public void GetStringHandlesEmptyArray()
        {
            Assert.AreEqual(string.Empty, Array.Empty<byte>().GetString());
            Assert.AreEqual(string.Empty, ((byte[])null).GetString());
        }

        [Test]
        public void ToPascalCaseMultipleConsecutiveUppercase()
        {
            Assert.AreEqual("XmlHttpRequest", "XMLHttpRequest".ToPascalCase());
            Assert.AreEqual("HtmlParser", "HTMLParser".ToPascalCase());
            Assert.AreEqual("IoError", "IOError".ToPascalCase());
        }

        [Test]
        public void ToPascalCaseWithNumbers()
        {
            Assert.AreEqual("Test123", "test123".ToPascalCase());
            Assert.AreEqual("Test123Test", "test_123_test".ToPascalCase());
        }

        [Test]
        public void ToPascalCaseComplexStrings()
        {
            Assert.AreEqual("ThisIsAComplexTest", "this_is_a_complex_test".ToPascalCase());
            Assert.AreEqual("ThisIsAComplexTest", "THIS_IS_A_COMPLEX_TEST".ToPascalCase());
            Assert.AreEqual("ThisIsAComplexTest", "this-is-a-complex-test".ToPascalCase());
        }

        [Test]
        public void CenterHandlesEmptyString()
        {
            Assert.AreEqual("     ", string.Empty.Center(5));
        }

        [Test]
        public void LevenshteinDistanceLongerStrings()
        {
            Assert.AreEqual(5, "intention".LevenshteinDistance("execution"));
            Assert.AreEqual(6, "algorithm".LevenshteinDistance("altruistic"));
        }

        [Test]
        public void ToSnakeCasePreservesNumbers()
        {
            Assert.AreEqual("test123", "test123".ToSnakeCase());
            Assert.AreEqual("test_123_value", "test123Value".ToSnakeCase());
        }

        [Test]
        public void ToSnakeCaseWithNumbers()
        {
            // Letter to digit transitions (only when there are uppercase letters)
            Assert.AreEqual("test_456_end", "test456End".ToSnakeCase());

            // Digit to letter transitions (only when there are uppercase letters)
            Assert.AreEqual("version_2_beta", "version2Beta".ToSnakeCase());
            Assert.AreEqual("my_3_d_model", "my3DModel".ToSnakeCase());

            // Multiple number groups
            Assert.AreEqual("test_123_value_456", "test123Value456".ToSnakeCase());
            Assert.AreEqual("api_v_2_endpoint_3", "apiV2Endpoint3".ToSnakeCase());

            // Edge cases
            Assert.AreEqual("123", "123".ToSnakeCase());
            Assert.AreEqual("test_123_value_456_end", "test123Value456End".ToSnakeCase());

            // Combined with uppercase
            Assert.AreEqual("my_class_2_d", "MyClass2D".ToSnakeCase());
            Assert.AreEqual("http_2_client", "HTTP2Client".ToSnakeCase());

            // Already lowercase - should be preserved
            Assert.AreEqual("abc123", "abc123".ToSnakeCase());
            Assert.AreEqual("value1", "value1".ToSnakeCase());
            Assert.AreEqual("123abc", "123abc".ToSnakeCase());
            Assert.AreEqual("a1b2c3", "a1b2c3".ToSnakeCase());
        }

        [Test]
        public void ToKebabCasePreservesNumbers()
        {
            Assert.AreEqual("test-123-value", "test123Value".ToKebabCase());
        }

        [Test]
        public void ToKebabCaseWithNumbers()
        {
            // Letter to digit transitions (only when there are uppercase letters)
            Assert.AreEqual("test-456-end", "test456End".ToKebabCase());

            // Digit to letter transitions (only when there are uppercase letters)
            Assert.AreEqual("version-2-beta", "version2Beta".ToKebabCase());
            Assert.AreEqual("my-3-d-model", "my3DModel".ToKebabCase());

            // Multiple number groups
            Assert.AreEqual("test-123-value-456", "test123Value456".ToKebabCase());
            Assert.AreEqual("api-v-2-endpoint-3", "apiV2Endpoint3".ToKebabCase());

            // Edge cases
            Assert.AreEqual("123", "123".ToKebabCase());
            Assert.AreEqual("test-123-value-456-end", "test123Value456End".ToKebabCase());

            // Already lowercase - should be preserved
            Assert.AreEqual("abc123", "abc123".ToKebabCase());
            Assert.AreEqual("value1", "value1".ToKebabCase());
            Assert.AreEqual("123abc", "123abc".ToKebabCase());
            Assert.AreEqual("a1b2c3", "a1b2c3".ToKebabCase());

            // Combined with uppercase
            Assert.AreEqual("my-class-2-d", "MyClass2D".ToKebabCase());
            Assert.AreEqual("http-2-client", "HTTP2Client".ToKebabCase());
        }

        [Test]
        public void ToSnakeCasePreservesLowercaseWithNumbers()
        {
            // Pure lowercase strings with numbers should be preserved
            Assert.AreEqual("user123", "user123".ToSnakeCase());
            Assert.AreEqual("test456", "test456".ToSnakeCase());
            Assert.AreEqual("abc789xyz", "abc789xyz".ToSnakeCase());
            Assert.AreEqual("model3d", "model3d".ToSnakeCase());
            Assert.AreEqual("http2", "http2".ToSnakeCase());

            // Pure numbers
            Assert.AreEqual("12345", "12345".ToSnakeCase());
            Assert.AreEqual("0", "0".ToSnakeCase());

            // Numbers at start
            Assert.AreEqual("2fast", "2fast".ToSnakeCase());
            Assert.AreEqual("404error", "404error".ToSnakeCase());

            // Numbers at end
            Assert.AreEqual("version2", "version2".ToSnakeCase());
            Assert.AreEqual("player1", "player1".ToSnakeCase());

            // Mixed numbers
            Assert.AreEqual("abc123def456", "abc123def456".ToSnakeCase());
            Assert.AreEqual("test1middle2end3", "test1middle2end3".ToSnakeCase());
        }

        [Test]
        public void ToSnakeCaseHandlesNumbersWithCasing()
        {
            // When uppercase letters are present, digit boundaries should get separators
            Assert.AreEqual("user_123_name", "user123Name".ToSnakeCase());
            Assert.AreEqual("test_456_value", "Test456Value".ToSnakeCase());
            Assert.AreEqual("model_3_d", "model3D".ToSnakeCase());
            Assert.AreEqual("http_2_client", "http2Client".ToSnakeCase());

            // Multiple transitions
            Assert.AreEqual("api_v_1_endpoint_2", "apiV1Endpoint2".ToSnakeCase());
            Assert.AreEqual("test_123_abc_456_def", "test123Abc456Def".ToSnakeCase());

            // Numbers with consecutive uppercase
            Assert.AreEqual("http_200_ok", "HTTP200OK".ToSnakeCase());
            Assert.AreEqual("xml_2_parser", "XML2Parser".ToSnakeCase());
        }

        [Test]
        public void ToSnakeCaseHandlesUnderscoresWithNumbers()
        {
            // Strings already with underscores
            Assert.AreEqual("test_123", "test_123".ToSnakeCase());
            Assert.AreEqual("value_456_end", "value_456_end".ToSnakeCase());
            Assert.AreEqual("my_3d_model", "my_3d_model".ToSnakeCase());

            // Mixed underscores and uppercase
            Assert.AreEqual("test_123_value", "test_123Value".ToSnakeCase());
            Assert.AreEqual("my_test_2_beta", "my_test2Beta".ToSnakeCase());
        }

        [Test]
        public void ToSnakeCaseHandlesEdgeCasesWithNumbers()
        {
            // Single character with number
            Assert.AreEqual("a1", "a1".ToSnakeCase());
            Assert.AreEqual("a_1", "A1".ToSnakeCase());

            // Multiple single digits
            Assert.AreEqual("a1b2c3d4", "a1b2c3d4".ToSnakeCase());
            Assert.AreEqual("a_1_b_2_c_3_d_4", "A1B2C3D4".ToSnakeCase());

            // Consecutive numbers
            Assert.AreEqual("test123456", "test123456".ToSnakeCase());
            Assert.AreEqual("test_123456_end", "test123456End".ToSnakeCase());

            // Number at very start
            Assert.AreEqual("1test", "1test".ToSnakeCase());
            Assert.AreEqual("1_test", "1Test".ToSnakeCase());
            Assert.AreEqual("123test456", "123test456".ToSnakeCase());
            Assert.AreEqual("123_test_456", "123Test456".ToSnakeCase());
        }

        [Test]
        public void ToSnakeCaseIdempotent()
        {
            // Applying ToSnakeCase twice should give the same result
            Assert.AreEqual("test_value", "test_value".ToSnakeCase());
            Assert.AreEqual("hello_world", "hello_world".ToSnakeCase());
            Assert.AreEqual("test123", "test123".ToSnakeCase());
            Assert.AreEqual("abc_123_def", "abc_123_def".ToSnakeCase());

            // Verify idempotence
            string test1 = "TestValue123";
            string snake1 = test1.ToSnakeCase();
            string snake2 = snake1.ToSnakeCase();
            Assert.AreEqual(snake1, snake2);

            string test2 = "user123Name";
            string snake3 = test2.ToSnakeCase();
            string snake4 = snake3.ToSnakeCase();
            Assert.AreEqual(snake3, snake4);
        }

        [Test]
        public void ToSnakeCasePerformanceEdgeCases()
        {
            // Very long strings
            string longString = "a".Repeat(100) + "123" + "b".Repeat(100);
            Assert.AreEqual(longString, longString.ToSnakeCase());

            string longMixed = "Test" + "Value".Repeat(50);
            string result = longMixed.ToSnakeCase();
            Assert.IsTrue(result.Contains("_"));
            Assert.IsFalse(result.Contains("__")); // No double underscores

            // Many number transitions
            string manyNumbers = "a1b2c3d4e5f6g7h8i9j0";
            Assert.AreEqual(manyNumbers, manyNumbers.ToSnakeCase());
        }

        [Test]
        public void TruncateExactlyAtLimit()
        {
            Assert.AreEqual("tes...", "testing".Truncate(6));
            Assert.AreEqual("te...", "testing".Truncate(5));
        }

        [Test]
        public void RepeatWithMultiCharacterString()
        {
            Assert.AreEqual("abcabcabc", "abc".Repeat(3));
            Assert.AreEqual("hello hello ", "hello ".Repeat(2));
        }

        [Test]
        public void CountOccurrencesWithSingleCharSubstring()
        {
            Assert.AreEqual(2, "hello".CountOccurrences("l"));
            Assert.AreEqual(2, "test".CountOccurrences("t"));
        }

        [Test]
        public void ContainsIgnoreCaseEmptyString()
        {
            Assert.IsTrue("test".ContainsIgnoreCase(""));
            Assert.IsTrue("".ContainsIgnoreCase(""));
        }

        [Test]
        public void ToBase64HandlesEmptyString()
        {
            string base64 = string.Empty.ToBase64();
            Assert.AreEqual(string.Empty, base64);
        }

        [Test]
        public void FromBase64HandlesValidBase64()
        {
            string base64 = "SGVsbG8gV29ybGQ=";
            string decoded = base64.FromBase64();
            Assert.AreEqual("Hello World", decoded);
        }

        [Test]
        public void ToPascalCaseMixedSeparators()
        {
            Assert.AreEqual("ThisIsATest", "this_is-a_test".ToPascalCase());
            Assert.AreEqual("HelloWorldTest", "hello-world_test".ToPascalCase());
            Assert.AreEqual("MixedCaseTest", "mixed_Case-Test".ToPascalCase());
        }

        [Test]
        public void ToPascalCaseMultipleSeparators()
        {
            Assert.AreEqual("TestCase", "test__case".ToPascalCase());
            Assert.AreEqual("TestCase", "test--case".ToPascalCase());
            Assert.AreEqual("TestCase", "test__--__case".ToPascalCase());
        }

        [Test]
        public void ToPascalCaseLeadingAndTrailingSeparators()
        {
            Assert.AreEqual("TestCase", "_test_case".ToPascalCase());
            Assert.AreEqual("TestCase", "test_case_".ToPascalCase());
            Assert.AreEqual("TestCase", "_test_case_".ToPascalCase());
            Assert.AreEqual("TestCase", "-test-case-".ToPascalCase());
            Assert.AreEqual("TestCase", "__test__case__".ToPascalCase());
        }

        [Test]
        public void ToPascalCaseAllSeparators()
        {
            Assert.AreEqual("TestCase", "test_case".ToPascalCase());
            Assert.AreEqual("TestCase", "test-case".ToPascalCase());
            Assert.AreEqual("TestCase", "test case".ToPascalCase());
            Assert.AreEqual("TestCase", "test.case".ToPascalCase());
            Assert.AreEqual("TestCase", "test\tcase".ToPascalCase());
            Assert.AreEqual("TestCase", "test\ncase".ToPascalCase());
            Assert.AreEqual("TestCase", "test\rcase".ToPascalCase());
        }

        [Test]
        public void ToPascalCaseSingleWord()
        {
            Assert.AreEqual("Test", "test".ToPascalCase());
            Assert.AreEqual("Test", "TEST".ToPascalCase());
            Assert.AreEqual("Test", "Test".ToPascalCase());
        }

        [Test]
        public void ToPascalCaseSingleCharacter()
        {
            Assert.AreEqual("A", "a".ToPascalCase());
            Assert.AreEqual("A", "A".ToPascalCase());
            Assert.AreEqual(string.Empty, "_".ToPascalCase());
            Assert.AreEqual(string.Empty, "-".ToPascalCase());
        }

        [Test]
        public void ToPascalCaseOnlySeparators()
        {
            Assert.AreEqual(string.Empty, "___".ToPascalCase());
            Assert.AreEqual(string.Empty, "---".ToPascalCase());
            Assert.AreEqual(string.Empty, "   ".ToPascalCase());
            Assert.AreEqual(string.Empty, "_-_ ".ToPascalCase());
        }

        [Test]
        public void ToPascalCaseUppercaseWords()
        {
            Assert.AreEqual("ThisIsATest", "THIS-IS-A-TEST".ToPascalCase());
            Assert.AreEqual("HelloWorld", "HELLO_WORLD".ToPascalCase());
        }

        [Test]
        public void ToPascalCaseMixedCamelAndSnake()
        {
            Assert.AreEqual("HelloWorldTest", "helloWorld_test".ToPascalCase());
            Assert.AreEqual("TestCaseExample", "testCase_example".ToPascalCase());
        }

        [Test]
        public void ToPascalCaseWithNumbersAndSeparators()
        {
            Assert.AreEqual("Test123Case456", "test_123_case_456".ToPascalCase());
            Assert.AreEqual("Version2Update", "version-2-update".ToPascalCase());
            Assert.AreEqual("Test1Test2", "test1_test2".ToPascalCase());
        }

        [Test]
        public void ToPascalCaseSpecialCharacterSeparators()
        {
            Assert.AreEqual("TestCase", "test'Case".ToPascalCase());
            Assert.AreEqual("HelloWorld", "hello\"World".ToPascalCase());
            Assert.AreEqual("MixedTest", "mixed.'Test".ToPascalCase());
        }
    }
}
