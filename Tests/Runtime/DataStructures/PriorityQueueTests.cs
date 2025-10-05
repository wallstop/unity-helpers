namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure;

    public sealed class PriorityQueueTests
    {
        [Test]
        public void EnqueueAddsElements()
        {
            PriorityQueue<int> pq = PriorityQueue<int>.CreateMin();
            pq.Enqueue(5);
            pq.Enqueue(3);
            pq.Enqueue(7);

            Assert.AreEqual(3, pq.Count);
        }

        [Test]
        public void TryDequeueRemovesMinElement()
        {
            PriorityQueue<int> pq = PriorityQueue<int>.CreateMin();
            pq.Enqueue(5);
            pq.Enqueue(3);
            pq.Enqueue(7);

            Assert.IsTrue(pq.TryDequeue(out int result));
            Assert.AreEqual(3, result);
        }

        [Test]
        public void TryPeekReturnsWithoutRemoving()
        {
            PriorityQueue<int> pq = PriorityQueue<int>.CreateMin();
            pq.Enqueue(5);

            Assert.IsTrue(pq.TryPeek(out int result));
            Assert.AreEqual(5, result);
            Assert.AreEqual(1, pq.Count);
        }

        [Test]
        public void MaxPriorityQueueWorks()
        {
            PriorityQueue<int> pq = PriorityQueue<int>.CreateMax();
            pq.Enqueue(5);
            pq.Enqueue(3);
            pq.Enqueue(7);

            Assert.IsTrue(pq.TryDequeue(out int result));
            Assert.AreEqual(7, result);
        }

        [Test]
        public void EnumerationWorks()
        {
            PriorityQueue<int> pq = PriorityQueue<int>.CreateMin();
            pq.Enqueue(5);
            pq.Enqueue(3);

            List<int> elements = new();
            foreach (int element in pq)
            {
                elements.Add(element);
            }

            Assert.AreEqual(2, elements.Count);
        }
    }
}
