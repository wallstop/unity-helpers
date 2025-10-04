namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure;

    public sealed class DequeTests
    {
        [Test]
        public void PushFrontAddsFront()
        {
            Deque<int> deque = new();
            deque.PushFront(1);
            deque.PushFront(2);

            Assert.AreEqual(2, deque.Count);
            Assert.AreEqual(2, deque[0]);
            Assert.AreEqual(1, deque[1]);
        }

        [Test]
        public void PushBackAddsBack()
        {
            Deque<int> deque = new();
            deque.PushBack(1);
            deque.PushBack(2);

            Assert.AreEqual(2, deque.Count);
            Assert.AreEqual(1, deque[0]);
            Assert.AreEqual(2, deque[1]);
        }

        [Test]
        public void TryPopFrontRemovesFront()
        {
            Deque<int> deque = new();
            deque.PushBack(1);
            deque.PushBack(2);
            deque.PushBack(3);

            Assert.IsTrue(deque.TryPopFront(out int result));
            Assert.AreEqual(1, result);
            Assert.AreEqual(2, deque.Count);
        }

        [Test]
        public void TryPopBackRemovesBack()
        {
            Deque<int> deque = new();
            deque.PushBack(1);
            deque.PushBack(2);
            deque.PushBack(3);

            Assert.IsTrue(deque.TryPopBack(out int result));
            Assert.AreEqual(3, result);
            Assert.AreEqual(2, deque.Count);
        }

        [Test]
        public void TryPopReturnsFalseWhenEmpty()
        {
            Deque<int> deque = new();

            Assert.IsFalse(deque.TryPopFront(out _));
            Assert.IsFalse(deque.TryPopBack(out _));
        }

        [Test]
        public void TryPeekFrontReturnsWithoutRemoving()
        {
            Deque<int> deque = new();
            deque.PushBack(1);
            deque.PushBack(2);

            Assert.IsTrue(deque.TryPeekFront(out int result));
            Assert.AreEqual(1, result);
            Assert.AreEqual(2, deque.Count);
        }

        [Test]
        public void TryPeekBackReturnsWithoutRemoving()
        {
            Deque<int> deque = new();
            deque.PushBack(1);
            deque.PushBack(2);

            Assert.IsTrue(deque.TryPeekBack(out int result));
            Assert.AreEqual(2, result);
            Assert.AreEqual(2, deque.Count);
        }

        [Test]
        public void GrowsAutomatically()
        {
            Deque<int> deque = new(2);
            deque.PushBack(1);
            deque.PushBack(2);
            deque.PushBack(3);

            Assert.AreEqual(3, deque.Count);
            Assert.IsTrue(deque.Capacity >= 3);
        }

        [Test]
        public void CircularWrapping()
        {
            Deque<int> deque = new(4);
            deque.PushBack(1);
            deque.PushBack(2);
            deque.TryPopFront(out _);
            deque.TryPopFront(out _);
            deque.PushBack(3);
            deque.PushBack(4);
            deque.PushBack(5);

            Assert.AreEqual(3, deque.Count);
            Assert.AreEqual(3, deque[0]);
        }
    }

    public sealed class DisjointSetTests
    {
        [Test]
        public void InitiallyAllElementsSeparate()
        {
            DisjointSet ds = new(5);

            Assert.AreEqual(5, ds.Count);
            Assert.AreEqual(5, ds.SetCount);
        }

        [Test]
        public void TryUnionConnectsTwoElements()
        {
            DisjointSet ds = new(5);

            Assert.IsTrue(ds.TryUnion(0, 1));
            Assert.AreEqual(4, ds.SetCount);
        }

        [Test]
        public void TryUnionReturnsFalseForSameSet()
        {
            DisjointSet ds = new(5);
            ds.TryUnion(0, 1);

            Assert.IsFalse(ds.TryUnion(0, 1));
            Assert.AreEqual(4, ds.SetCount);
        }

        [Test]
        public void TryIsConnectedWorks()
        {
            DisjointSet ds = new(5);
            ds.TryUnion(0, 1);
            ds.TryUnion(1, 2);

            Assert.IsTrue(ds.TryIsConnected(0, 2, out bool connected));
            Assert.IsTrue(connected);

            Assert.IsTrue(ds.TryIsConnected(0, 3, out bool notConnected));
            Assert.IsFalse(notConnected);
        }

        [Test]
        public void TryFindReturnsRepresentative()
        {
            DisjointSet ds = new(5);
            ds.TryUnion(0, 1);
            ds.TryUnion(1, 2);

            Assert.IsTrue(ds.TryFind(0, out int rep0));
            Assert.IsTrue(ds.TryFind(2, out int rep2));
            Assert.AreEqual(rep0, rep2);
        }

        [Test]
        public void TryGetSetReturnsCorrectElements()
        {
            DisjointSet ds = new(5);
            ds.TryUnion(0, 1);
            ds.TryUnion(1, 2);

            List<int> results = new();
            List<int> set = ds.TryGetSet(0, results);

            Assert.IsNotNull(set);
            Assert.AreEqual(3, set.Count);
            CollectionAssert.Contains(set, 0);
            CollectionAssert.Contains(set, 1);
            CollectionAssert.Contains(set, 2);
        }

        [Test]
        public void ResetSeparatesAllElements()
        {
            DisjointSet ds = new(5);
            ds.TryUnion(0, 1);
            ds.TryUnion(2, 3);

            ds.Reset();

            Assert.AreEqual(5, ds.SetCount);
        }

        [Test]
        public void GenericVersionWorks()
        {
            List<string> elements = new() { "a", "b", "c", "d" };
            DisjointSet<string> ds = new(elements);

            Assert.IsTrue(ds.TryUnion("a", "b"));
            Assert.IsTrue(ds.TryIsConnected("a", "b", out bool connected));
            Assert.IsTrue(connected);
        }
    }

    public sealed class SparseSetTests
    {
        [Test]
        public void TryAddAddsElement()
        {
            SparseSet set = new(100);

            Assert.IsTrue(set.TryAdd(5));
            Assert.AreEqual(1, set.Count);
            Assert.IsTrue(set.Contains(5));
        }

        [Test]
        public void TryAddReturnsFalseForDuplicate()
        {
            SparseSet set = new(100);
            set.TryAdd(5);

            Assert.IsFalse(set.TryAdd(5));
            Assert.AreEqual(1, set.Count);
        }

        [Test]
        public void TryRemoveRemovesElement()
        {
            SparseSet set = new(100);
            set.TryAdd(5);
            set.TryAdd(10);

            Assert.IsTrue(set.TryRemove(5));
            Assert.AreEqual(1, set.Count);
            Assert.IsFalse(set.Contains(5));
            Assert.IsTrue(set.Contains(10));
        }

        [Test]
        public void ContainsWorksCorrectly()
        {
            SparseSet set = new(100);
            set.TryAdd(42);

            Assert.IsTrue(set.Contains(42));
            Assert.IsFalse(set.Contains(43));
        }

        [Test]
        public void ClearRemovesAllElements()
        {
            SparseSet set = new(100);
            set.TryAdd(1);
            set.TryAdd(2);
            set.TryAdd(3);

            set.Clear();

            Assert.AreEqual(0, set.Count);
            Assert.IsTrue(set.IsEmpty);
        }

        [Test]
        public void EnumerationWorks()
        {
            SparseSet set = new(100);
            set.TryAdd(5);
            set.TryAdd(10);
            set.TryAdd(15);

            List<int> elements = new();
            foreach (int element in set)
            {
                elements.Add(element);
            }

            Assert.AreEqual(3, elements.Count);
            CollectionAssert.Contains(elements, 5);
            CollectionAssert.Contains(elements, 10);
            CollectionAssert.Contains(elements, 15);
        }

        [Test]
        public void GenericVersionWorks()
        {
            SparseSet<string> set = new(100);

            Assert.IsTrue(set.TryAdd("hello"));
            Assert.IsTrue(set.TryAdd("world"));
            Assert.IsFalse(set.TryAdd("hello"));
            Assert.AreEqual(2, set.Count);
        }
    }

    public sealed class BitSetTests
    {
        [Test]
        public void TrySetSetsBit()
        {
            BitSet bits = new(64);

            Assert.IsTrue(bits.TrySet(5));
            Assert.IsTrue(bits.TryGet(5, out bool value));
            Assert.IsTrue(value);
        }

        [Test]
        public void TryClearClearsBit()
        {
            BitSet bits = new(64);
            bits.TrySet(5);

            Assert.IsTrue(bits.TryClear(5));
            Assert.IsTrue(bits.TryGet(5, out bool value));
            Assert.IsFalse(value);
        }

        [Test]
        public void TryFlipTogglesBit()
        {
            BitSet bits = new(64);

            bits.TryFlip(5);
            Assert.IsTrue(bits.TryGet(5, out bool value1));
            Assert.IsTrue(value1);

            bits.TryFlip(5);
            Assert.IsTrue(bits.TryGet(5, out bool value2));
            Assert.IsFalse(value2);
        }

        [Test]
        public void CountSetBitsWorks()
        {
            BitSet bits = new(64);
            bits.TrySet(0);
            bits.TrySet(1);
            bits.TrySet(10);

            Assert.AreEqual(3, bits.CountSetBits());
        }

        [Test]
        public void SetAllSetsAllBits()
        {
            BitSet bits = new(10);
            bits.SetAll();

            Assert.AreEqual(10, bits.CountSetBits());
            Assert.IsTrue(bits.All());
        }

        [Test]
        public void ClearAllClearsAllBits()
        {
            BitSet bits = new(64);
            bits.SetAll();
            bits.ClearAll();

            Assert.AreEqual(0, bits.CountSetBits());
            Assert.IsTrue(bits.None());
        }

        [Test]
        public void TryAndWorks()
        {
            BitSet bits1 = new(8);
            BitSet bits2 = new(8);

            bits1.TrySet(0);
            bits1.TrySet(1);
            bits2.TrySet(1);
            bits2.TrySet(2);

            bits1.TryAnd(bits2);

            Assert.AreEqual(1, bits1.CountSetBits());
            Assert.IsTrue(bits1.TryGet(1, out bool value));
            Assert.IsTrue(value);
        }

        [Test]
        public void TryOrWorks()
        {
            BitSet bits1 = new(8);
            BitSet bits2 = new(8);

            bits1.TrySet(0);
            bits2.TrySet(1);

            bits1.TryOr(bits2);

            Assert.AreEqual(2, bits1.CountSetBits());
        }

        [Test]
        public void LeftShiftWorks()
        {
            BitSet bits = new(8);
            bits.TrySet(0);
            bits.TrySet(1);

            bits.LeftShift(2);

            Assert.IsTrue(bits.TryGet(2, out bool v1) && v1);
            Assert.IsTrue(bits.TryGet(3, out bool v2) && v2);
            Assert.IsFalse(bits.TryGet(0, out bool v3) || v3);
        }

        [Test]
        public void RightShiftWorks()
        {
            BitSet bits = new(8);
            bits.TrySet(5);
            bits.TrySet(6);

            bits.RightShift(2);

            Assert.IsTrue(bits.TryGet(3, out bool v1) && v1);
            Assert.IsTrue(bits.TryGet(4, out bool v2) && v2);
            Assert.IsFalse(bits.TryGet(5, out bool v3) || v3);
        }

        [Test]
        public void ResizeWorks()
        {
            BitSet bits = new(10);
            bits.TrySet(5);

            bits.Resize(20);

            Assert.AreEqual(20, bits.Capacity);
            Assert.IsTrue(bits.TryGet(5, out bool value));
            Assert.IsTrue(value);
        }

        [Test]
        public void EnumerationYieldsAllBits()
        {
            BitSet bits = new(5);
            bits.TrySet(1);
            bits.TrySet(3);

            List<bool> values = new();
            foreach (bool bit in bits)
            {
                values.Add(bit);
            }

            Assert.AreEqual(5, values.Count);
            Assert.IsFalse(values[0]);
            Assert.IsTrue(values[1]);
            Assert.IsFalse(values[2]);
            Assert.IsTrue(values[3]);
            Assert.IsFalse(values[4]);
        }
    }

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

    public sealed class SpatialHashTests
    {
        [Test]
        public void InsertAndQueryWorks()
        {
            SpatialHash2D<string> hash = new(1.0f);
            hash.Insert(new Vector2(0.5f, 0.5f), "a");
            hash.Insert(new Vector2(1.5f, 1.5f), "b");

            List<string> results = new();
            hash.Query(new Vector2(0, 0), 2.0f, results);

            Assert.GreaterOrEqual(results.Count, 1);
            CollectionAssert.Contains(results, "a");
        }

        [Test]
        public void RemoveWorks()
        {
            SpatialHash2D<string> hash = new(1.0f);
            hash.Insert(new Vector2(0.5f, 0.5f), "a");

            Assert.IsTrue(hash.Remove(new Vector2(0.5f, 0.5f), "a"));

            List<string> results = new();
            hash.Query(new Vector2(0, 0), 2.0f, results);

            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void QueryRectWorks()
        {
            SpatialHash2D<string> hash = new(1.0f);
            hash.Insert(new Vector2(0.5f, 0.5f), "a");
            hash.Insert(new Vector2(5.0f, 5.0f), "b");

            List<string> results = new();
            hash.QueryRect(new Vector2(0, 0), new Vector2(2, 2), results);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("a", results[0]);
        }

        [Test]
        public void SpatialHash3DWorks()
        {
            SpatialHash3D<string> hash = new(1.0f);
            hash.Insert(new Vector3(0.5f, 0.5f, 0.5f), "a");

            List<string> results = new();
            hash.Query(new Vector3(0, 0, 0), 2.0f, results);

            Assert.GreaterOrEqual(results.Count, 1);
            CollectionAssert.Contains(results, "a");
        }
    }
}
