namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Random;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;

    public sealed class RandomExtensionsTests : CommonTestBase
    {
        private static readonly SystemRandom DeterministicRandom = new(1234);

        [Test]
        public void NextOfExceptThrowsWhenCollectionEmpty()
        {
            SystemRandom rng = new(42);
            Assert.Throws<ArgumentException>(() => rng.NextOfExcept(Array.Empty<int>(), 1));
        }

        [Test]
        public void NextOfExceptThrowsWhenAllValuesExcluded()
        {
            SystemRandom rng = new(42);
            int[] source = { 1, 2 };
            Assert.Throws<ArgumentException>(() => rng.NextOfExcept(source, 1, 2));
        }

        [Test]
        public void NextOfExceptReturnsValueNotInExceptions()
        {
            SystemRandom rng = new(42);
            int[] source = { 1, 2, 3, 4 };
            int selected = rng.NextOfExcept(source, 2, 2, 3, 3);
            CollectionAssert.DoesNotContain(new[] { 2, 3 }, selected);
        }

        [Test]
        public void NextWeightedIndexThrowsWhenWeightsDoNotSumPositive()
        {
            SystemRandom rng = new(1);
            Assert.Throws<ArgumentException>(() => rng.NextWeightedIndex(new[] { 0f, -1f }));
        }

        [Test]
        public void NextSubsetReturnsDeterministicReservoirSample()
        {
            SystemRandom rng = new(99);
            int[] source = { 10, 11, 12, 13, 14 };

            IEnumerable<int> subset = rng.NextSubset(source, 3);
            int[] result = subset.ToArray();

            Assert.AreEqual(3, result.Length);
            CollectionAssert.AllItemsAreUnique(result);
            CollectionAssert.IsSubsetOf(result, source);

            // Deterministic snapshot ensures algorithm stability for fixed seed.
            CollectionAssert.AreEqual(new[] { 10, 14, 13 }, result);
        }

        [Test]
        public void NextWeightedIndexHandlesExtremeValues()
        {
            SystemRandom rng = new(1);
            float[] weights = { float.MaxValue / 4f, float.MaxValue / 4f, float.MaxValue / 2f };

            Assert.DoesNotThrow(() => rng.NextWeightedIndex(weights));
        }

        [Test]
        public void NextWeightedPrefersHigherWeights()
        {
            SystemRandom rng = new(2);
            (string label, float weight)[] weighted = { ("low", 1f), ("high", 4f) };

            int lowCount = 0;
            int highCount = 0;
            for (int i = 0; i < 1000; ++i)
            {
                string choice = rng.NextWeighted(weighted);
                if (choice == "low")
                {
                    lowCount++;
                }
                else
                {
                    highCount++;
                }
            }

            Assert.Greater(highCount, lowCount, "Higher weights should be selected more often.");
        }

        [Test]
        public void NextSubsetCountZeroReturnsEmpty()
        {
            SystemRandom rng = new(5);
            int[] source = { 1, 2, 3 };
            IEnumerable<int> subset = rng.NextSubset(source, 0);
            CollectionAssert.IsEmpty(subset);
        }

        [Test]
        public void NextSubsetThrowsWhenCountExceedsSource()
        {
            SystemRandom rng = new(5);
            Assert.Throws<ArgumentException>(() => rng.NextSubset(new[] { 1, 2 }, 3));
        }

        [Test]
        public void NextWeightedElementThrowsWhenLengthsMismatch()
        {
            SystemRandom rng = new(1);
            Assert.Throws<ArgumentException>(() =>
                rng.NextWeightedElement(new[] { "a", "b" }, new[] { 0.5f })
            );
        }

        [Test]
        public void NextWeightedIndexHandlesTinyWeights()
        {
            SystemRandom rng = new(1);
            float tiny = float.Epsilon;
            float[] weights = { tiny, tiny, tiny };
            Assert.DoesNotThrow(() => rng.NextWeightedIndex(weights));
        }

        [Test]
        public void NextFloatAroundRespectsVariance()
        {
            SystemRandom rng = new(3);
            float center = 5f;
            float variance = 0f;
            float sample = rng.NextFloatAround(center, variance);
            Assert.AreEqual(center, sample);

            float rangedSample = rng.NextFloatAround(2f, 0.5f);
            Assert.That(rangedSample, Is.InRange(1.5f, 2.5f));
        }

        [Test]
        public void NextIntAroundRespectsVariance()
        {
            SystemRandom rng = new(3);
            int center = 10;
            int variance = 0;
            int sample = rng.NextIntAround(center, variance);
            Assert.AreEqual(center, sample);
        }

        [Test]
        public void NextOfExceptHandlesAllButOneExcluded()
        {
            SystemRandom rng = new(10);
            int[] values = { 1, 2, 3 };
            int result = rng.NextOfExcept(values, 1, 2);
            Assert.AreEqual(3, result);
        }

        [Test]
        public void NextSubsetEqualCountReturnsCopy()
        {
            SystemRandom rng = new(77);
            int[] source = { 1, 2, 3 };
            int[] subset = rng.NextSubset(source, 3).ToArray();
            CollectionAssert.AreEqual(source, subset);
        }

        [Test]
        public void NextWeightedThrowsWhenAllWeightsZero()
        {
            SystemRandom rng = new(1);
            Assert.Throws<ArgumentException>(() => rng.NextWeightedIndex(new[] { 0f, 0f }));
        }

        [Test]
        public void NextSubsetDeferredEnumerationKeepsResults()
        {
            SystemRandom rng = new(5);
            int[] source = { 1, 2, 3, 4, 5 };
            IEnumerable<int> subset = rng.NextSubset(source, 2);

            using IEnumerator<int> enumerator = subset.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            int first = enumerator.Current;
            Assert.IsTrue(enumerator.MoveNext());
            int second = enumerator.Current;
            CollectionAssert.Contains(source, first);
            CollectionAssert.Contains(source, second);
        }

        [Test]
        public void NextSubsetNegativeCountThrows()
        {
            SystemRandom rng = new(5);
            Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextSubset(new[] { 1 }, -1));
        }

        [Test]
        public void NextSubsetCountOneReturnsSingleElement()
        {
            SystemRandom rng = new(42);
            int[] subset = rng.NextSubset(new[] { 1, 2, 3 }, 1).ToArray();
            Assert.AreEqual(1, subset.Length);
            CollectionAssert.Contains(new[] { 1, 2, 3 }, subset[0]);
        }
    }
}
