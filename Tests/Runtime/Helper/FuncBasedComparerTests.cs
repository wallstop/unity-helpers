namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Helper;

    public sealed class FuncBasedComparerTests
    {
        [Test]
        public void CompareUsesProvidedDelegate()
        {
            FuncBasedComparer<int> comparer = new((x, y) => y.CompareTo(x));

            List<int> values = new() { 1, 2, 3 };
            values.Sort(comparer);

            Assert.That(values, Is.EqualTo(new[] { 3, 2, 1 }));
        }

        [Test]
        public void ConstructorThrowsWhenComparerNull()
        {
            Assert.Throws<ArgumentNullException>(() => new FuncBasedComparer<int>(null));
        }
    }
}
