// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using System;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Utils;

    internal static class NamespaceA
    {
        internal sealed class Duplicate { }
    }

    internal static class NamespaceB
    {
        internal sealed class Duplicate { }
    }

    public sealed class TypeNameSorterAdditionalTests
    {
        [Test]
        public void CompareReturnsZeroForSameNameDifferentNamespaces()
        {
            Type a = typeof(NamespaceA.Duplicate);
            Type b = typeof(NamespaceB.Duplicate);

            int comparison = TypeNameSorter.Instance.Compare(a, b);
            Assert.AreEqual(0, comparison);
        }

        [Test]
        public void CompareHandlesBothNullAsEqual()
        {
            int comparison = TypeNameSorter.Instance.Compare(null, null);
            Assert.AreEqual(0, comparison);
        }
    }
}
