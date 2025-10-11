namespace WallstopStudios.UnityHelpers.Tests.Serialization
{
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure;
    using WallstopStudios.UnityHelpers.Core.Serialization;

    public sealed class ImmutableBitSetJsonTests
    {
        [Test]
        public void ImmutableBitSetRoundTrips()
        {
            BitSet mutable = new(10);
            mutable.TrySet(2);
            mutable.TrySet(9);
            ImmutableBitSet original = mutable.ToImmutable();
            string json = Serializer.JsonStringify(original);
            ImmutableBitSet deserialized = Serializer.JsonDeserialize<ImmutableBitSet>(json);
            Assert.AreEqual(original.Capacity, deserialized.Capacity, "Capacity should match");
            for (int i = 0; i < original.Capacity; i++)
            {
                Assert.AreEqual(original[i], deserialized[i], $"Bit {i} should match");
            }
        }
    }
}
