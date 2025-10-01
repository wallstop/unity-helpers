namespace WallstopStudios.UnityHelpers.Tests.Extensions
{
    using System;
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
        public void AsListTurnsOtherCollectionsIntoLists()
        {
            HashSet<int> hashSet = new() { 1, 2, 3 };
            IList<int> list = hashSet.AsList();
            CollectionAssert.AreEqual(hashSet, list);

            Queue<int> queue = new();
            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.Enqueue(3);
            list = queue.AsList();
            CollectionAssert.AreEqual(queue, list);

            Stack<int> stack = new();
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);
            list = stack.AsList();
            CollectionAssert.AreEqual(stack, list);

            LinkedList<int> linked = new();
            linked.AddLast(1);
            linked.AddLast(2);
            linked.AddLast(3);
            list = linked.AsList();
            CollectionAssert.AreEqual(linked, list);

            SortedSet<int> sortedSet = new() { 1, 2, 3 };
            list = sortedSet.AsList();
            CollectionAssert.AreEqual(sortedSet, list);
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
            CollectionAssert.AreEqual(new[] { 5 }, partitions[2]);
        }

        [Test]
        public void PartitionReturnsNoPartitionsForEmptySequence()
        {
            int[][] partitions = Array
                .Empty<int>()
                .Partition(3)
                .Select(chunk => chunk.ToArray())
                .ToArray();

            Assert.IsEmpty(partitions);
        }

        [Test]
        public void PartitionHandlesRemainingElementsWithoutPadding()
        {
            int[] values = Enumerable.Range(1, 7).ToArray();
            List<int[]> partitions = values.Partition(5).Select(chunk => chunk.ToArray()).ToList();

            Assert.AreEqual(2, partitions.Count);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, partitions[0]);
            CollectionAssert.AreEqual(new[] { 6, 7 }, partitions[1]);
        }

        [Test]
        public void PartitionHandlesChunkSizeLargerThanSequence()
        {
            int[] values = { 1, 2, 3 };
            int[][] partitions = values.Partition(5).Select(chunk => chunk.ToArray()).ToArray();

            Assert.That(partitions.Length, Is.EqualTo(1));
            CollectionAssert.AreEqual(values, partitions[0]);
        }

        [Test]
        public void PartitionWithSizeOneProducesSingleElementChunks()
        {
            int[] values = { 9, 8, 7 };
            int[][] partitions = values.Partition(1).Select(chunk => chunk.ToArray()).ToArray();

            Assert.That(partitions.Length, Is.EqualTo(values.Length));
            for (int i = 0; i < values.Length; ++i)
            {
                CollectionAssert.AreEqual(new[] { values[i] }, partitions[i]);
            }
        }
    }
}
