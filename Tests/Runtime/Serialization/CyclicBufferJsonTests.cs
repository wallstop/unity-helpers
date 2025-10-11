namespace WallstopStudios.UnityHelpers.Tests.Serialization
{
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure;
    using WallstopStudios.UnityHelpers.Core.Serialization;

    public sealed class CyclicBufferJsonTests
    {
        [Test]
        public void CyclicBufferRoundTripsWithCapacity()
        {
            CyclicBuffer<int> original = new(5);
            for (int i = 1; i <= 7; i++)
            {
                original.Add(i);
            }
            string json = Serializer.JsonStringify(original);
            CyclicBuffer<int> deserialized = Serializer.JsonDeserialize<CyclicBuffer<int>>(json);
            Assert.AreEqual(
                original.Capacity,
                deserialized.Capacity,
                "Capacity should be preserved"
            );
            Assert.AreEqual(original.Count, deserialized.Count, "Count should be preserved");
            int idx = 0;
            foreach (int v in original)
            {
                Assert.AreEqual(v, deserialized[idx], $"Element {idx} should match");
                idx++;
            }
        }

        [Test]
        public void CyclicBufferRoundTripsWithFastOptions()
        {
            CyclicBuffer<int> original = new(3);
            original.Add(1);
            original.Add(2);
            original.Add(3);
            original.Add(4);
            var options = Serializer.CreateFastJsonOptions();
            string json = Serializer.JsonStringify(original, options);
            CyclicBuffer<int> deserialized = Serializer.JsonDeserialize<CyclicBuffer<int>>(
                json,
                null,
                options
            );
            Assert.AreEqual(
                original.Capacity,
                deserialized.Capacity,
                "Capacity should match under fast options"
            );
            Assert.AreEqual(
                original.Count,
                deserialized.Count,
                "Count should match under fast options"
            );
            int idx = 0;
            foreach (int v in original)
            {
                Assert.AreEqual(
                    v,
                    deserialized[idx],
                    $"Element {idx} should match under fast options"
                );
                idx++;
            }
        }
    }
}
