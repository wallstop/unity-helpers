namespace WallstopStudios.UnityHelpers.Tests
{
    using System.Collections.Generic;
    using NUnit.Framework;

    public static class SpatialAssert
    {
        public static void AreEquivalentOrCountEqual<T>(
            ICollection<T> expected,
            ICollection<T> actual,
            int maxCountForEquivalence = 100000
        )
        {
            Assert.NotNull(expected);
            Assert.NotNull(actual);

            if (expected.Count != actual.Count)
            {
                Assert.AreEqual(expected.Count, actual.Count);
                return;
            }

            if (expected.Count <= maxCountForEquivalence)
            {
                CollectionAssert.AreEquivalent(expected, actual);
            }
            else
            {
                Assert.AreEqual(expected.Count, actual.Count);
            }
        }
    }
}
