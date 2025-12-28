// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Math
{
    using System;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Math;

    [TestFixture]
    public sealed class RangeTests
    {
        [Test]
        public void ConstructorSetsValuesAndDefaults()
        {
            Range<int> range = new(1, 5);

            Assert.AreEqual(1, range.Min);
            Assert.AreEqual(5, range.Max);
            Assert.IsTrue(range.StartInclusive);
            Assert.IsTrue(range.EndInclusive);
        }

        [Test]
        public void ConstructorUsesProvidedInclusivityFlags()
        {
            Range<int> range = new(1, 5, false, false);

            Assert.IsFalse(range.StartInclusive);
            Assert.IsFalse(range.EndInclusive);
        }

        [Test]
        public void ConstructorThrowsWhenMinIsGreaterThanMax()
        {
            Assert.Throws<ArgumentException>(() => new Range<int>(5, 4));
        }

        [Test]
        public void WithinRangeReturnsTrueWhenValueInsideInclusiveBounds()
        {
            Range<int> range = new(0, 10);

            Assert.IsTrue(range.WithinRange(5));
        }

        [Test]
        public void WithinRangeRespectsExclusiveStart()
        {
            Range<int> range = new(0, 10, false, true);

            Assert.IsFalse(range.WithinRange(0));
            Assert.IsTrue(range.WithinRange(1));
        }

        [Test]
        public void WithinRangeRespectsExclusiveEnd()
        {
            Range<int> range = new(0, 10, true, false);

            Assert.IsFalse(range.WithinRange(10));
            Assert.IsTrue(range.WithinRange(9));
        }

        [Test]
        public void WithinRangeReturnsFalseWhenValueOutsideBounds()
        {
            Range<int> range = new(0, 10);

            Assert.IsFalse(range.WithinRange(-1));
            Assert.IsFalse(range.WithinRange(11));
        }

        [Test]
        public void ContainsDelegatesToWithinRange()
        {
            Range<int> range = Range<int>.Inclusive(0, 10);

            Assert.IsTrue(range.Contains(10));
            Assert.IsFalse(range.Contains(11));
        }

        [Test]
        public void OverlapsReturnsTrueWhenRangesIntersect()
        {
            Range<int> first = Range<int>.Inclusive(0, 10);
            Range<int> second = Range<int>.Inclusive(5, 15);

            Assert.IsTrue(first.Overlaps(second));
            Assert.IsTrue(second.Overlaps(first));
        }

        [Test]
        public void OverlapsReturnsFalseWhenRangesAreDisjoint()
        {
            Range<int> first = Range<int>.Inclusive(0, 10);
            Range<int> second = Range<int>.Inclusive(11, 20);

            Assert.IsFalse(first.Overlaps(second));
            Assert.IsFalse(second.Overlaps(first));
        }

        [Test]
        public void OverlapsHandlesExclusiveBoundaries()
        {
            Range<int> first = Range<int>.InclusiveExclusive(0, 10);
            Range<int> second = Range<int>.Inclusive(10, 20);
            Range<int> third = Range<int>.ExclusiveInclusive(10, 20);

            Assert.IsTrue(first.Overlaps(second));
            Assert.IsFalse(first.Overlaps(third));
        }

        [Test]
        public void CompareToOrdersByMinThenMaxThenInclusivity()
        {
            Range<int> baseline = new(0, 10);
            Range<int> largerMin = new(1, 10);
            Range<int> smallerMax = new(0, 9);
            Range<int> exclusiveStart = new(0, 10, false, true);
            Range<int> inclusive = new(0, 10, true, true);

            Assert.Less(baseline.CompareTo(largerMin), 0);
            Assert.Greater(baseline.CompareTo(smallerMax), 0);
            Assert.Less(inclusive.CompareTo(exclusiveStart), 0);
            Assert.Greater(exclusiveStart.CompareTo(inclusive), 0);
            Assert.AreEqual(0, baseline.CompareTo(inclusive));
        }

        [Test]
        public void EqualsReturnsTrueForIdenticalRanges()
        {
            Range<int> first = new(0, 10);
            Range<int> second = new(0, 10);

            Assert.IsTrue(first.Equals(second));
            Assert.IsTrue(first == second);
        }

        [Test]
        public void EqualsReturnsFalseWhenAnyComponentDiffers()
        {
            Range<int> first = new(0, 10);
            Range<int> differentMin = new(1, 10);
            Range<int> differentMax = new(0, 11);
            Range<int> differentInclusivity = new(0, 10, false, true);

            Assert.IsFalse(first.Equals(differentMin));
            Assert.IsFalse(first.Equals(differentMax));
            Assert.IsFalse(first.Equals(differentInclusivity));
            Assert.IsTrue(first != differentInclusivity);
        }

        [Test]
        public void GetHashCodeMatchesForEqualRanges()
        {
            Range<int> first = new(0, 10, false, true);
            Range<int> second = new(0, 10, false, true);

            Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
        }

        [Test]
        public void GetHashCodeDiffersForDifferentRanges()
        {
            Range<int> first = new(0, 10);
            Range<int> second = new(0, 9);

            Assert.AreNotEqual(first.GetHashCode(), second.GetHashCode());
        }

        [Test]
        public void StaticFactoriesConfigureInclusivity()
        {
            Range<int> inclusive = Range<int>.Inclusive(0, 1);
            Range<int> exclusive = Range<int>.Exclusive(0, 1);
            Range<int> inclusiveExclusive = Range<int>.InclusiveExclusive(0, 1);
            Range<int> exclusiveInclusive = Range<int>.ExclusiveInclusive(0, 1);

            Assert.IsTrue(inclusive.StartInclusive);
            Assert.IsTrue(inclusive.EndInclusive);
            Assert.IsFalse(exclusive.StartInclusive);
            Assert.IsFalse(exclusive.EndInclusive);
            Assert.IsTrue(inclusiveExclusive.StartInclusive);
            Assert.IsFalse(inclusiveExclusive.EndInclusive);
            Assert.IsFalse(exclusiveInclusive.StartInclusive);
            Assert.IsTrue(exclusiveInclusive.EndInclusive);
        }

        [Test]
        public void ToStringUsesMatchingBracketStyles()
        {
            Range<int> inclusive = new(1, 2, true, true);
            Range<int> mixed = new(1, 2, false, true);

            Assert.AreEqual("[1, 2]", inclusive.ToString());
            Assert.AreEqual("(1, 2]", mixed.ToString());
        }
    }
}
