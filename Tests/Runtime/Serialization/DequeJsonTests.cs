namespace WallstopStudios.UnityHelpers.Tests.Serialization
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure;
    using WallstopStudios.UnityHelpers.Core.Serialization;

    public sealed class DequeJsonTests
    {
        [Test]
        public void DequeOfIntsRoundTrips()
        {
            Deque<int> original = new(4);
            original.PushBack(1);
            original.PushBack(2);
            original.PushFront(0);
            string json = Serializer.JsonStringify(original);
            Deque<int> deserialized = Serializer.JsonDeserialize<Deque<int>>(json);
            Assert.AreEqual(
                original.Count,
                deserialized.Count,
                "Count should match after round-trip"
            );
            for (int i = 0; i < original.Count; i++)
            {
                Assert.AreEqual(original[i], deserialized[i], $"Element at {i} should match");
            }
        }

        [Test]
        public void DequeOfVectorsRoundTrips()
        {
            List<Vector2> list = new() { new Vector2(1, 2), new Vector2(3, 4), new Vector2(-5, 6) };
            Deque<Vector2> original = new(list);
            string json = Serializer.JsonStringify(original);
            Deque<Vector2> deserialized = Serializer.JsonDeserialize<Deque<Vector2>>(json);
            Assert.AreEqual(original.Count, deserialized.Count, "Count should match for vectors");
            for (int i = 0; i < original.Count; i++)
            {
                Assert.AreEqual(original[i], deserialized[i], $"Vector at {i} should match");
            }
        }

        [Test]
        public void DequeFastOptionsRoundTrips()
        {
            Deque<int> original = new(2);
            original.PushBack(10);
            original.PushFront(5);
            var options = Serializer.CreateFastJsonOptions();
            string json = Serializer.JsonStringify(original, options);
            Deque<int> deserialized = Serializer.JsonDeserialize<Deque<int>>(json, null, options);
            Assert.AreEqual(
                original.Count,
                deserialized.Count,
                "Count should match under fast options"
            );
            for (int i = 0; i < original.Count; i++)
            {
                Assert.AreEqual(
                    original[i],
                    deserialized[i],
                    $"Element {i} should match under fast options"
                );
            }
        }
    }
}
