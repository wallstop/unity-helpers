namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Extension;

    public sealed class IEnumerableExtensionsTests
    {
        [Test]
        public void ToLinkedListPreservesOrder()
        {
            int[] values = { 1, 2, 3 };
            LinkedList<int> linked = values.ToLinkedList();
            CollectionAssert.AreEqual(values, linked);
        }

        [Test]
        public void AsListReturnsExistingListWhenPossible()
        {
            List<int> list = new() { 1, 2, 3 };
            Assert.AreSame(list, list.AsList());

            int[] array = { 1, 2, 3 };
            IList<int> converted = array.AsList();
            CollectionAssert.AreEqual(array, converted);
            Assert.IsInstanceOf<int[]>(converted);
        }

        [Test]
        public void OrderByUsesProvidedComparer()
        {
            int[] values = { 3, 1, 2 };
            IEnumerable<int> ordered = values.OrderBy((x, y) => y.CompareTo(x));
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, ordered.ToArray());
        }

        [Test]
        public void OrderedUsesNaturalOrdering()
        {
            int[] values = { 5, 2, 3 };
            CollectionAssert.AreEqual(new[] { 2, 3, 5 }, values.Ordered().ToArray());
        }

        [Test]
        public void ShuffledRetainsAllElements()
        {
            int[] values = Enumerable.Range(0, 10).ToArray();
            IReadOnlyCollection<int> shuffled = values.Shuffled().ToArray();
            CollectionAssert.AreEquivalent(values, shuffled);
        }

        [Test]
        public void InfiniteRepeatsSequence()
        {
            int[] values = { 1, 2 };
            int[] repeated = values.Infinite().Take(5).ToArray();
            CollectionAssert.AreEqual(new[] { 1, 2, 1, 2, 1 }, repeated);
        }

        [Test]
        public void PartitionSplitsIntoChunks()
        {
            int[] values = { 1, 2, 3, 4, 5 };
            List<int[]> partitions = new();
            foreach (List<int> chunk in values.Partition(2))
            {
                partitions.Add(chunk.ToArray());
            }

            Assert.AreEqual(3, partitions.Count);
            CollectionAssert.AreEqual(new[] { 1, 2 }, partitions[0]);
            CollectionAssert.AreEqual(new[] { 3, 4 }, partitions[1]);
            CollectionAssert.AreEqual(new[] { 5, 0 }, partitions[2]);
        }
    }
}
