namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NUnit.Framework;
    using ProtoBuf;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure;

    public sealed class ProtobufSerializationTests
    {
        private static T SerializeDeserialize<T>(T original)
        {
            using MemoryStream stream = new MemoryStream();
            Serializer.Serialize(stream, original);
            stream.Position = 0;
            return Serializer.Deserialize<T>(stream);
        }

        [Test]
        public void CyclicBuffer_SerializesAndDeserializes()
        {
            CyclicBuffer<int> original = new CyclicBuffer<int>(5);
            original.Add(1);
            original.Add(2);
            original.Add(3);

            CyclicBuffer<int> deserialized = SerializeDeserialize(original);

            Assert.AreEqual(original.Count, deserialized.Count);
            Assert.AreEqual(original.Capacity, deserialized.Capacity);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.AreEqual(original[i], deserialized[i]);
            }
        }

        [Test]
        public void CyclicBuffer_EmptyBuffer_SerializesAndDeserializes()
        {
            CyclicBuffer<string> original = new CyclicBuffer<string>(10);

            CyclicBuffer<string> deserialized = SerializeDeserialize(original);

            Assert.AreEqual(0, deserialized.Count);
            Assert.AreEqual(10, deserialized.Capacity);
        }

        [Test]
        public void CyclicBuffer_FullBuffer_SerializesAndDeserializes()
        {
            CyclicBuffer<int> original = new CyclicBuffer<int>(3);
            original.Add(1);
            original.Add(2);
            original.Add(3);
            original.Add(4); // This should wrap and replace 1

            CyclicBuffer<int> deserialized = SerializeDeserialize(original);

            Assert.AreEqual(original.Count, deserialized.Count);
            Assert.AreEqual(original.Capacity, deserialized.Capacity);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.AreEqual(original[i], deserialized[i]);
            }
        }

        [Test]
        public void Heap_MinHeap_SerializesAndDeserializes()
        {
            Heap<int> original = Heap<int>.CreateMinHeap();
            original.Add(5);
            original.Add(2);
            original.Add(8);
            original.Add(1);

            Heap<int> deserialized = SerializeDeserialize(original);

            Assert.AreEqual(original.Count, deserialized.Count);
            List<int> originalValues = new List<int>();
            List<int> deserializedValues = new List<int>();

            while (original.TryPop(out int val))
            {
                originalValues.Add(val);
            }
            while (deserialized.TryPop(out int val))
            {
                deserializedValues.Add(val);
            }

            CollectionAssert.AreEqual(originalValues, deserializedValues);
        }

        [Test]
        public void Heap_EmptyHeap_SerializesAndDeserializes()
        {
            Heap<int> original = new Heap<int>();

            Heap<int> deserialized = SerializeDeserialize(original);

            Assert.AreEqual(0, deserialized.Count);
            Assert.IsTrue(deserialized.IsEmpty);
        }

        [Test]
        public void BitSet_SerializesAndDeserializes()
        {
            BitSet original = new BitSet(100);
            original.TrySet(0);
            original.TrySet(15);
            original.TrySet(31);
            original.TrySet(63);
            original.TrySet(99);

            BitSet deserialized = SerializeDeserialize(original);

            Assert.AreEqual(original.Capacity, deserialized.Capacity);
            for (int i = 0; i < original.Capacity; i++)
            {
                original.TryGet(i, out bool origVal);
                deserialized.TryGet(i, out bool deserVal);
                Assert.AreEqual(origVal, deserVal, $"Bit {i} mismatch");
            }
        }

        [Test]
        public void BitSet_AllSet_SerializesAndDeserializes()
        {
            BitSet original = new BitSet(64);
            original.SetAll();

            BitSet deserialized = SerializeDeserialize(original);

            Assert.IsTrue(deserialized.All());
            Assert.AreEqual(64, deserialized.CountSetBits());
        }

        [Test]
        public void Deque_SerializesAndDeserializes()
        {
            Deque<string> original = new Deque<string>();
            original.PushBack("first");
            original.PushBack("second");
            original.PushFront("zero");

            Deque<string> deserialized = SerializeDeserialize(original);

            Assert.AreEqual(original.Count, deserialized.Count);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.AreEqual(original[i], deserialized[i]);
            }
        }

        [Test]
        public void Deque_AfterOperations_SerializesAndDeserializes()
        {
            Deque<int> original = new Deque<int>();
            original.PushBack(1);
            original.PushBack(2);
            original.PushFront(0);
            original.TryPopFront(out _);

            Deque<int> deserialized = SerializeDeserialize(original);

            Assert.AreEqual(original.Count, deserialized.Count);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.AreEqual(original[i], deserialized[i]);
            }
        }

        [Test]
        public void DisjointSet_SerializesAndDeserializes()
        {
            DisjointSet original = new DisjointSet(10);
            original.TryUnion(0, 1);
            original.TryUnion(2, 3);
            original.TryUnion(1, 3);

            DisjointSet deserialized = SerializeDeserialize(original);

            Assert.AreEqual(original.Count, deserialized.Count);
            Assert.AreEqual(original.SetCount, deserialized.SetCount);

            for (int i = 0; i < original.Count; i++)
            {
                for (int j = i + 1; j < original.Count; j++)
                {
                    original.TryIsConnected(i, j, out bool origConnected);
                    deserialized.TryIsConnected(i, j, out bool deserConnected);
                    Assert.AreEqual(
                        origConnected,
                        deserConnected,
                        $"Connection between {i} and {j} mismatch"
                    );
                }
            }
        }

        [Test]
        public void DisjointSetGeneric_SerializesAndDeserializes()
        {
            List<string> elements = new List<string> { "a", "b", "c", "d", "e" };
            DisjointSet<string> original = new DisjointSet<string>(elements);
            original.TryUnion("a", "b");
            original.TryUnion("c", "d");

            DisjointSet<string> deserialized = SerializeDeserialize(original);

            Assert.AreEqual(original.Count, deserialized.Count);
            Assert.AreEqual(original.SetCount, deserialized.SetCount);

            foreach (string elem1 in elements)
            {
                foreach (string elem2 in elements)
                {
                    original.TryIsConnected(elem1, elem2, out bool origConnected);
                    deserialized.TryIsConnected(elem1, elem2, out bool deserConnected);
                    Assert.AreEqual(
                        origConnected,
                        deserConnected,
                        $"Connection between {elem1} and {elem2} mismatch"
                    );
                }
            }
        }

        [Test]
        public void PriorityQueue_SerializesAndDeserializes()
        {
            PriorityQueue<int> original = PriorityQueue<int>.CreateMin();
            original.Enqueue(5);
            original.Enqueue(2);
            original.Enqueue(8);
            original.Enqueue(1);

            PriorityQueue<int> deserialized = SerializeDeserialize(original);

            Assert.AreEqual(original.Count, deserialized.Count);

            List<int> originalValues = new List<int>();
            List<int> deserializedValues = new List<int>();

            while (original.TryDequeue(out int val))
            {
                originalValues.Add(val);
            }
            while (deserialized.TryDequeue(out int val))
            {
                deserializedValues.Add(val);
            }

            CollectionAssert.AreEqual(originalValues, deserializedValues);
        }

        [Test]
        public void SparseSet_SerializesAndDeserializes()
        {
            SparseSet original = new SparseSet(100);
            original.TryAdd(0);
            original.TryAdd(15);
            original.TryAdd(50);
            original.TryAdd(99);

            SparseSet deserialized = SerializeDeserialize(original);

            Assert.AreEqual(original.Count, deserialized.Count);
            Assert.AreEqual(original.Capacity, deserialized.Capacity);

            for (int i = 0; i < original.Capacity; i++)
            {
                Assert.AreEqual(
                    original.Contains(i),
                    deserialized.Contains(i),
                    $"Element {i} containment mismatch"
                );
            }
        }

        [Test]
        public void SparseSetGeneric_SerializesAndDeserializes()
        {
            SparseSet<string> original = new SparseSet<string>(10);
            original.TryAdd("apple");
            original.TryAdd("banana");
            original.TryAdd("cherry");

            SparseSet<string> deserialized = SerializeDeserialize(original);

            Assert.AreEqual(original.Count, deserialized.Count);
            Assert.IsTrue(deserialized.Contains("apple"));
            Assert.IsTrue(deserialized.Contains("banana"));
            Assert.IsTrue(deserialized.Contains("cherry"));
            Assert.IsFalse(deserialized.Contains("date"));
        }

        [Test]
        public void SpatialHash2D_SerializesAndDeserializes()
        {
            SpatialHash2D<int> original = new SpatialHash2D<int>(1.0f);
            original.Insert(new Vector2(0.5f, 0.5f), 1);
            original.Insert(new Vector2(1.5f, 1.5f), 2);
            original.Insert(new Vector2(0.6f, 0.6f), 3);

            SpatialHash2D<int> deserialized = SerializeDeserialize(original);

            Assert.AreEqual(original.CellSize, deserialized.CellSize);
            Assert.AreEqual(original.CellCount, deserialized.CellCount);

            List<int> originalResults = new List<int>();
            List<int> deserializedResults = new List<int>();

            original.Query(new Vector2(0.5f, 0.5f), 0.5f, originalResults);
            deserialized.Query(new Vector2(0.5f, 0.5f), 0.5f, deserializedResults);

            originalResults.Sort();
            deserializedResults.Sort();
            CollectionAssert.AreEqual(originalResults, deserializedResults);
        }

        [Test]
        public void SpatialHash3D_SerializesAndDeserializes()
        {
            SpatialHash3D<string> original = new SpatialHash3D<string>(2.0f);
            original.Insert(new Vector3(1, 1, 1), "a");
            original.Insert(new Vector3(5, 5, 5), "b");
            original.Insert(new Vector3(1.5f, 1.5f, 1.5f), "c");

            SpatialHash3D<string> deserialized = SerializeDeserialize(original);

            Assert.AreEqual(original.CellSize, deserialized.CellSize);
            Assert.AreEqual(original.CellCount, deserialized.CellCount);

            List<string> originalResults = new List<string>();
            List<string> deserializedResults = new List<string>();

            original.Query(new Vector3(1, 1, 1), 1.0f, originalResults);
            deserialized.Query(new Vector3(1, 1, 1), 1.0f, deserializedResults);

            originalResults.Sort();
            deserializedResults.Sort();
            CollectionAssert.AreEqual(originalResults, deserializedResults);
        }
    }
}
