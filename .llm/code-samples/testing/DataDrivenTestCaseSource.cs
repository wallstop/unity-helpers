// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

// Data-driven testing with TestCaseSource - comprehensive test data patterns

namespace WallstopStudios.UnityHelpers.Examples
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DataDrivenTestExamples
    {
        [Test]
        [TestCaseSource(nameof(EdgeCaseTestData))]
        public void ProcessHandlesEdgeCases(int[] input, int expected)
        {
            int result = MyProcessor.Process(input);

            Assert.AreEqual(expected, result);
        }

        private static IEnumerable<TestCaseData> EdgeCaseTestData()
        {
            // Empty
            yield return new TestCaseData(Array.Empty<int>(), 0).SetName("Input.Empty.ReturnsZero");

            // Single element
            yield return new TestCaseData(new[] { 42 }, 42).SetName(
                "Input.SingleElement.ReturnsElement"
            );

            // Boundary values
            yield return new TestCaseData(new[] { int.MaxValue }, int.MaxValue).SetName(
                "Input.MaxValue.HandlesCorrectly"
            );
            yield return new TestCaseData(new[] { int.MinValue }, int.MinValue).SetName(
                "Input.MinValue.HandlesCorrectly"
            );
            yield return new TestCaseData(new[] { 0 }, 0).SetName("Input.Zero.ReturnsZero");
            yield return new TestCaseData(new[] { -1 }, -1).SetName(
                "Input.Negative.HandlesCorrectly"
            );

            // Multiple elements
            yield return new TestCaseData(new[] { 1, 2, 3, 4, 5 }, 15).SetName(
                "Input.MultipleElements.SumsCorrectly"
            );

            // Large collection
            yield return new TestCaseData(CreateLargeArray(10000), 50005000).SetName(
                "Input.LargeCollection.HandlesScale"
            );

            // Duplicates
            yield return new TestCaseData(new[] { 5, 5, 5, 5 }, 20).SetName(
                "Input.AllDuplicates.SumsCorrectly"
            );
        }

        // Comprehensive test data template covering ALL categories
        private static IEnumerable<TestCaseData> ComprehensiveTestData()
        {
            // === NORMAL CASES ===
            yield return new TestCaseData("normal input", "expected output").SetName(
                "Normal.TypicalInput.ProducesExpected"
            );
            yield return new TestCaseData("another normal", "another expected").SetName(
                "Normal.AlternateInput.ProducesExpected"
            );

            // === EDGE CASES ===
            // Empty
            yield return new TestCaseData("", "default").SetName("Edge.EmptyString.ReturnsDefault");

            // Single element
            yield return new TestCaseData("a", "a").SetName("Edge.SingleChar.Preserved");

            // Boundaries
            yield return new TestCaseData(new string('x', 1), "x").SetName(
                "Edge.MinLength.Handled"
            );
            yield return new TestCaseData(new string('x', 10000), "truncated").SetName(
                "Edge.MaxLength.Truncated"
            );

            // === NEGATIVE CASES ===
            yield return new TestCaseData(null, null).SetName("Negative.NullInput.ReturnsNull");
            yield return new TestCaseData("invalid@#$", "sanitized").SetName(
                "Negative.SpecialChars.Sanitized"
            );

            // === EXTREME CASES ===
            yield return new TestCaseData(new string('a', 100000), "handled").SetName(
                "Extreme.VeryLongInput.Handled"
            );

            // === UNEXPECTED SITUATIONS ===
            yield return new TestCaseData("\0\0\0", "handled").SetName(
                "Unexpected.NullChars.Handled"
            );
            yield return new TestCaseData("   \t\n\r   ", "handled").SetName(
                "Unexpected.OnlyWhitespace.Handled"
            );
        }

        private static int[] CreateLargeArray(int count)
        {
            int[] array = new int[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = i + 1;
            }
            return array;
        }

        // Dummy class for example
        private static class MyProcessor
        {
            public static int Process(int[] input)
            {
                if (input == null)
                {
                    return 0;
                }

                int sum = 0;
                for (int i = 0; i < input.Length; i++)
                {
                    sum += input[i];
                }
                return sum;
            }
        }
    }
}
