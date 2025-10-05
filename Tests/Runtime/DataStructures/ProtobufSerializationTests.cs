namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure;
    using Serializer = WallstopStudios.UnityHelpers.Core.Serialization.Serializer;

    public sealed class ProtobufSerializationTests
    {
        private static T SerializeDeserialize<T>(T original)
        {
            byte[] serialized = Serializer.ProtoSerialize(original);
            return Serializer.ProtoDeserialize<T>(serialized);
        }

        private static ImmutableBitSet SerializeDeserializeImmutable(ImmutableBitSet original)
        {
            byte[] serialized = Serializer.ProtoSerialize(original);
            ImmutableBitSet deserialized = Serializer.ProtoDeserialize<ImmutableBitSet>(serialized);
            return deserialized;
        }

        [Test]
        public void CyclicBufferSerializesAndDeserializes()
        {
            CyclicBuffer<int> original = new(5) { 1, 2, 3 };

            CyclicBuffer<int> deserialized = SerializeDeserialize(original);

            Assert.AreEqual(original.Count, deserialized.Count);
            Assert.AreEqual(original.Capacity, deserialized.Capacity);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.AreEqual(original[i], deserialized[i]);
            }
        }

        [Test]
        public void CyclicBufferEmptyBufferSerializesAndDeserializes()
        {
            CyclicBuffer<string> original = new(10);

            CyclicBuffer<string> deserialized = SerializeDeserialize(original);

            Assert.AreEqual(0, deserialized.Count);
            Assert.AreEqual(10, deserialized.Capacity);
        }

        [Test]
        public void CyclicBufferFullBufferSerializesAndDeserializes()
        {
            CyclicBuffer<int> original = new(3)
            {
                1,
                2,
                3,
                4, // This should wrap and replace 1
            };

            CyclicBuffer<int> deserialized = SerializeDeserialize(original);

            Assert.AreEqual(original.Count, deserialized.Count);
            Assert.AreEqual(original.Capacity, deserialized.Capacity);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.AreEqual(original[i], deserialized[i]);
            }
        }

        [Test]
        public void CyclicBufferReferenceTypeVacatedSlotsSerializesAndDeserializes()
        {
            CyclicBuffer<string> original = new(5) { "alpha", "beta", "gamma" };
            Assert.IsTrue(original.TryPopFront(out _));

            CyclicBuffer<string> deserialized = SerializeDeserialize(original);

            Assert.AreEqual(original.Count, deserialized.Count);
            Assert.AreEqual(original.Capacity, deserialized.Capacity);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.AreEqual(original[i], deserialized[i]);
            }
        }

        [Test]
        public void BitSetSerializesAndDeserializes()
        {
            BitSet original = new(100);
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
        public void BitSetAllSetSerializesAndDeserializes()
        {
            BitSet original = new(64);
            original.SetAll();
            Assert.IsTrue(original.All());

            BitSet deserialized = SerializeDeserialize(original);

            Assert.IsTrue(deserialized.All());
            Assert.AreEqual(64, deserialized.CountSetBits());
        }

        [Test]
        public void ImmutableBitSetSerializesAndDeserializes()
        {
            BitSet mutableBitSet = new(100);
            mutableBitSet.TrySet(0);
            mutableBitSet.TrySet(15);
            mutableBitSet.TrySet(31);
            mutableBitSet.TrySet(63);
            mutableBitSet.TrySet(99);
            ImmutableBitSet original = mutableBitSet.ToImmutable();

            ImmutableBitSet deserialized = SerializeDeserializeImmutable(original);

            Assert.AreEqual(original.Capacity, deserialized.Capacity);
            for (int i = 0; i < original.Capacity; i++)
            {
                original.TryGet(i, out bool origVal);
                deserialized.TryGet(i, out bool deserVal);
                Assert.AreEqual(origVal, deserVal, $"Bit {i} mismatch");
            }
        }

        [Test]
        public void ImmutableBitSetAllSetSerializesAndDeserializes()
        {
            BitSet mutableBitSet = new(64);
            mutableBitSet.SetAll();
            ImmutableBitSet original = mutableBitSet.ToImmutable();
            Assert.IsTrue(original.All());

            ImmutableBitSet deserialized = SerializeDeserializeImmutable(original);

            Assert.IsTrue(deserialized.All());
            Assert.AreEqual(64, deserialized.CountSetBits());
        }

        [Test]
        public void ImmutableBitSetEmptySerializesAndDeserializes()
        {
            BitSet mutableBitSet = new(128);
            ImmutableBitSet original = mutableBitSet.ToImmutable();
            Assert.IsTrue(original.None());

            ImmutableBitSet deserialized = SerializeDeserializeImmutable(original);

            Assert.AreEqual(original.Capacity, deserialized.Capacity);
            Assert.IsTrue(deserialized.None());
            Assert.AreEqual(0, deserialized.CountSetBits());
        }

        [Test]
        public void ImmutableBitSetSingleBitSerializesAndDeserializes()
        {
            BitSet mutableBitSet = new(200);
            mutableBitSet.TrySet(123);
            ImmutableBitSet original = mutableBitSet.ToImmutable();

            ImmutableBitSet deserialized = SerializeDeserializeImmutable(original);

            Assert.AreEqual(original.Capacity, deserialized.Capacity);
            Assert.AreEqual(1, deserialized.CountSetBits());
            Assert.IsTrue(deserialized[123]);
            for (int i = 0; i < original.Capacity; i++)
            {
                if (i != 123)
                {
                    Assert.IsFalse(deserialized[i], $"Bit {i} should be false");
                }
            }
        }

        [Test]
        public void ImmutableBitSetMultipleWordsSerializesAndDeserializes()
        {
            BitSet mutableBitSet = new(300);
            // Set bits across multiple 64-bit words
            mutableBitSet.TrySet(0); // First word
            mutableBitSet.TrySet(63); // End of first word
            mutableBitSet.TrySet(64); // Start of second word
            mutableBitSet.TrySet(127); // End of second word
            mutableBitSet.TrySet(128); // Start of third word
            mutableBitSet.TrySet(255); // End of fourth word
            mutableBitSet.TrySet(299); // Near end
            ImmutableBitSet original = mutableBitSet.ToImmutable();

            ImmutableBitSet deserialized = SerializeDeserializeImmutable(original);

            Assert.AreEqual(original.Capacity, deserialized.Capacity);
            Assert.AreEqual(original.CountSetBits(), deserialized.CountSetBits());
            for (int i = 0; i < original.Capacity; i++)
            {
                original.TryGet(i, out bool origVal);
                deserialized.TryGet(i, out bool deserVal);
                Assert.AreEqual(origVal, deserVal, $"Bit {i} mismatch");
            }
        }

        [Test]
        public void DequeSerializesAndDeserializes()
        {
            Deque<string> original = new(Deque<string>.DefaultCapacity);
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
        public void DequeAfterOperationsSerializesAndDeserializes()
        {
            Deque<int> original = new(Deque<int>.DefaultCapacity);
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
        public void SparseSetSerializesAndDeserializes()
        {
            SparseSet original = new(100);
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
    }
}
