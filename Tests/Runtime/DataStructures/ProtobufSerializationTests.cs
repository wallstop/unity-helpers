namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Core.Math;
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

        [Test]
        public void FastVector2IntSerializesAndDeserializes()
        {
            FastVector2Int original = new(42, -17);

            FastVector2Int deserialized = SerializeDeserialize(original);

            Assert.AreEqual(original.x, deserialized.x);
            Assert.AreEqual(original.y, deserialized.y);
            Assert.AreEqual(original.GetHashCode(), deserialized.GetHashCode());
            Assert.IsTrue(original.Equals(deserialized));
        }

        [Test]
        public void FastVector2IntZeroSerializesAndDeserializes()
        {
            FastVector2Int original = new(0, 0);

            FastVector2Int deserialized = SerializeDeserialize(original);

            Assert.AreEqual(0, deserialized.x);
            Assert.AreEqual(0, deserialized.y);
            Assert.IsTrue(original.Equals(deserialized));
        }

        [Test]
        public void FastVector2IntNegativeValuesSerializeAndDeserialize()
        {
            FastVector2Int original = new(-1000, -2000);

            FastVector2Int deserialized = SerializeDeserialize(original);

            Assert.AreEqual(-1000, deserialized.x);
            Assert.AreEqual(-2000, deserialized.y);
            Assert.IsTrue(original.Equals(deserialized));
        }

        [Test]
        public void FastVector2IntLargeValuesSerializeAndDeserialize()
        {
            FastVector2Int original = new(int.MaxValue, int.MinValue);

            FastVector2Int deserialized = SerializeDeserialize(original);

            Assert.AreEqual(int.MaxValue, deserialized.x);
            Assert.AreEqual(int.MinValue, deserialized.y);
            Assert.IsTrue(original.Equals(deserialized));
        }

        [Test]
        public void FastVector3IntSerializesAndDeserializes()
        {
            FastVector3Int original = new(123, -456, 789);

            FastVector3Int deserialized = SerializeDeserialize(original);

            Assert.AreEqual(original.x, deserialized.x);
            Assert.AreEqual(original.y, deserialized.y);
            Assert.AreEqual(original.z, deserialized.z);
            Assert.AreEqual(original.GetHashCode(), deserialized.GetHashCode());
            Assert.IsTrue(original.Equals(deserialized));
        }

        [Test]
        public void FastVector3IntZeroSerializesAndDeserializes()
        {
            FastVector3Int original = FastVector3Int.zero;

            FastVector3Int deserialized = SerializeDeserialize(original);

            Assert.AreEqual(0, deserialized.x);
            Assert.AreEqual(0, deserialized.y);
            Assert.AreEqual(0, deserialized.z);
            Assert.IsTrue(original.Equals(deserialized));
        }

        [Test]
        public void FastVector3IntNegativeValuesSerializeAndDeserialize()
        {
            FastVector3Int original = new(-100, -200, -300);

            FastVector3Int deserialized = SerializeDeserialize(original);

            Assert.AreEqual(-100, deserialized.x);
            Assert.AreEqual(-200, deserialized.y);
            Assert.AreEqual(-300, deserialized.z);
            Assert.IsTrue(original.Equals(deserialized));
        }

        [Test]
        public void FastVector3IntLargeValuesSerializeAndDeserialize()
        {
            FastVector3Int original = new(int.MaxValue, 0, int.MinValue);

            FastVector3Int deserialized = SerializeDeserialize(original);

            Assert.AreEqual(int.MaxValue, deserialized.x);
            Assert.AreEqual(0, deserialized.y);
            Assert.AreEqual(int.MinValue, deserialized.z);
            Assert.IsTrue(original.Equals(deserialized));
        }

        [Test]
        public void FastVector3IntTwoParameterConstructorSerializesAndDeserializes()
        {
            FastVector3Int original = new(50, 75);

            FastVector3Int deserialized = SerializeDeserialize(original);

            Assert.AreEqual(50, deserialized.x);
            Assert.AreEqual(75, deserialized.y);
            Assert.AreEqual(0, deserialized.z);
            Assert.IsTrue(original.Equals(deserialized));
        }

        [Test]
        public void ParabolaSerializesAndDeserializes()
        {
            Parabola original = new(maxHeight: 10f, length: 20f);

            Parabola deserialized = SerializeDeserialize(original);

            Assert.AreEqual(original.Length, deserialized.Length, 0.0001f);
            Assert.AreEqual(original.MaxHeight, deserialized.MaxHeight, 0.0001f);
            Assert.AreEqual(original.A, deserialized.A, 0.0001f);
            Assert.AreEqual(original.B, deserialized.B, 0.0001f);
            Assert.IsTrue(original.Equals(deserialized));
        }

        [Test]
        public void ParabolaWithDifferentValuesSerializesAndDeserializes()
        {
            Parabola original = new(maxHeight: 15.5f, length: 30.25f);

            Parabola deserialized = SerializeDeserialize(original);

            Assert.AreEqual(15.5f, deserialized.MaxHeight, 0.0001f);
            Assert.AreEqual(30.25f, deserialized.Length, 0.0001f);
            Assert.IsTrue(original.Equals(deserialized));
        }

        [Test]
        public void ParabolaSmallValuesSerializeAndDeserialize()
        {
            Parabola original = new(maxHeight: 0.001f, length: 0.002f);

            Parabola deserialized = SerializeDeserialize(original);

            Assert.AreEqual(0.001f, deserialized.MaxHeight, 0.000001f);
            Assert.AreEqual(0.002f, deserialized.Length, 0.000001f);
            Assert.IsTrue(original.Equals(deserialized));
        }

        [Test]
        public void ParabolaLargeValuesSerializeAndDeserialize()
        {
            Parabola original = new(maxHeight: 10000f, length: 50000f);

            Parabola deserialized = SerializeDeserialize(original);

            Assert.AreEqual(10000f, deserialized.MaxHeight, 1f);
            Assert.AreEqual(50000f, deserialized.Length, 1f);
            Assert.IsTrue(original.Equals(deserialized));
        }
    }
}
