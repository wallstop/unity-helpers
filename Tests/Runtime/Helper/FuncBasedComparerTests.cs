// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Tests.Core;

    public sealed class FuncBasedComparerTests : CommonTestBase
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
