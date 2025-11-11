namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using ProtoBuf;
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
        public void SerializableDictionarySerializesAndDeserializes()
        {
            SerializableDictionary<string, int> original = new()
            {
                { "alpha", 1 },
                { "beta", 2 },
                { "gamma", 3 },
            };

            byte[] data = Serializer.ProtoSerialize(original);
            SerializableDictionary<string, int> deserialized = Serializer.ProtoDeserialize<
                SerializableDictionary<string, int>
            >(data);

            Assert.AreEqual(original.Count, deserialized.Count);
            foreach (KeyValuePair<string, int> pair in original)
            {
                Assert.IsTrue(deserialized.TryGetValue(pair.Key, out int value), pair.Key);
                Assert.AreEqual(pair.Value, value, pair.Key);
            }
        }

        [Test]
        public void SerializableDictionaryCacheSerializesAndDeserializes()
        {
            SerializableDictionary<
                int,
                SerializablePayload,
                SerializableDictionary.Cache<SerializablePayload>
            > original = new()
            {
                {
                    1,
                    new SerializablePayload { Id = 1, Name = "Primary" }
                },
                {
                    2,
                    new SerializablePayload { Id = 2, Name = "Secondary" }
                },
            };

            byte[] data = Serializer.ProtoSerialize(original);
            SerializableDictionary<
                int,
                SerializablePayload,
                SerializableDictionary.Cache<SerializablePayload>
            > deserialized = Serializer.ProtoDeserialize<
                SerializableDictionary<
                    int,
                    SerializablePayload,
                    SerializableDictionary.Cache<SerializablePayload>
                >
            >(data);

            Assert.AreEqual(original.Count, deserialized.Count);
            foreach (KeyValuePair<int, SerializablePayload> pair in original)
            {
                Assert.IsTrue(deserialized.TryGetValue(pair.Key, out SerializablePayload value));
                Assert.AreEqual(pair.Value.Id, value.Id);
                Assert.AreEqual(pair.Value.Name, value.Name);
            }
        }

        [ProtoContract]
        private sealed class SerializablePayload
        {
            [ProtoMember(1)]
            public int Id { get; set; }

            [ProtoMember(2)]
            public string Name { get; set; }
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
        public void DequeEmptyPreservesCapacityOnSerialization()
        {
            const int capacity = 32;
            Deque<int> original = new(capacity);

            Deque<int> deserialized = SerializeDeserialize(original);

            Assert.AreEqual(0, deserialized.Count);
            Assert.AreEqual(capacity, deserialized.Capacity);
        }

        [Test]
        public void DequeWrapAroundStateSerializesAndDeserializes()
        {
            Deque<int> original = new(4);
            original.PushBack(1);
            original.PushBack(2);
            original.PushBack(3);
            // Force wrap by popping and pushing
            Assert.IsTrue(original.TryPopFront(out _)); // remove 1
            original.PushBack(4); // wrap occurs internally
            original.PushFront(0); // may trigger resize or wrap

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

        [Test]
        public void SerializableDictionarySupportsMultipleProtobufCycles()
        {
            SerializableDictionary<string, int> dictionary = new()
            {
                { "alpha", 1 },
                { "beta", 2 },
            };

            byte[] firstSnapshot = Serializer.ProtoSerialize(dictionary);

            dictionary["alpha"] = 10;
            dictionary.Add("gamma", 3);
            dictionary.Remove("beta");

            byte[] secondSnapshot = Serializer.ProtoSerialize(dictionary);

            dictionary.Clear();
            dictionary.Add("delta", 4);

            byte[] thirdSnapshot = Serializer.ProtoSerialize(dictionary);

            SerializableDictionary<string, int> firstRoundTrip = Serializer.ProtoDeserialize<
                SerializableDictionary<string, int>
            >(firstSnapshot);
            SerializableDictionary<string, int> secondRoundTrip = Serializer.ProtoDeserialize<
                SerializableDictionary<string, int>
            >(secondSnapshot);
            SerializableDictionary<string, int> thirdRoundTrip = Serializer.ProtoDeserialize<
                SerializableDictionary<string, int>
            >(thirdSnapshot);

            Assert.That(firstRoundTrip.Count, Is.EqualTo(2));
            Assert.That(firstRoundTrip["alpha"], Is.EqualTo(1));
            Assert.That(firstRoundTrip["beta"], Is.EqualTo(2));

            Assert.That(secondRoundTrip.Count, Is.EqualTo(2));
            Assert.IsTrue(secondRoundTrip.ContainsKey("alpha"));
            Assert.IsTrue(secondRoundTrip.ContainsKey("gamma"));
            Assert.IsFalse(secondRoundTrip.ContainsKey("beta"));
            Assert.That(secondRoundTrip["alpha"], Is.EqualTo(10));
            Assert.That(secondRoundTrip["gamma"], Is.EqualTo(3));

            Assert.That(thirdRoundTrip.Count, Is.EqualTo(1));
            Assert.That(thirdRoundTrip["delta"], Is.EqualTo(4));

            secondRoundTrip["alpha"] = 25;
            secondRoundTrip.Add("epsilon", 5);
            secondRoundTrip.Remove("gamma");

            byte[] fourthSnapshot = Serializer.ProtoSerialize(secondRoundTrip);
            SerializableDictionary<string, int> fourthRoundTrip = Serializer.ProtoDeserialize<
                SerializableDictionary<string, int>
            >(fourthSnapshot);

            Assert.That(fourthRoundTrip.Count, Is.EqualTo(2));
            Assert.That(fourthRoundTrip["alpha"], Is.EqualTo(25));
            Assert.That(fourthRoundTrip["epsilon"], Is.EqualTo(5));

            fourthRoundTrip.Clear();
            fourthRoundTrip.Add("zeta", 6);

            byte[] finalSnapshot = Serializer.ProtoSerialize(fourthRoundTrip);
            SerializableDictionary<string, int> finalRoundTrip = Serializer.ProtoDeserialize<
                SerializableDictionary<string, int>
            >(finalSnapshot);

            Assert.That(finalRoundTrip.Count, Is.EqualTo(1));
            Assert.That(finalRoundTrip["zeta"], Is.EqualTo(6));
        }

        [Test]
        public void CacheSerializableDictionaryHandlesMultipleProtobufMutations()
        {
            SerializableDictionary<
                int,
                SerializablePayload,
                SerializableDictionary.Cache<SerializablePayload>
            > dictionary = new()
            {
                {
                    1,
                    new SerializablePayload { Id = 1, Name = "First" }
                },
                {
                    2,
                    new SerializablePayload { Id = 2, Name = "Second" }
                },
            };

            byte[] firstSnapshot = Serializer.ProtoSerialize(dictionary);

            dictionary[1] = new SerializablePayload { Id = 11, Name = "First Updated" };
            dictionary.Remove(2);
            dictionary.Add(3, new SerializablePayload { Id = 3, Name = "Third" });

            byte[] secondSnapshot = Serializer.ProtoSerialize(dictionary);

            dictionary.Clear();
            dictionary.Add(4, new SerializablePayload { Id = 4, Name = "Fourth" });

            byte[] thirdSnapshot = Serializer.ProtoSerialize(dictionary);

            SerializableDictionary<
                int,
                SerializablePayload,
                SerializableDictionary.Cache<SerializablePayload>
            > firstRoundTrip = Serializer.ProtoDeserialize<
                SerializableDictionary<
                    int,
                    SerializablePayload,
                    SerializableDictionary.Cache<SerializablePayload>
                >
            >(firstSnapshot);

            SerializableDictionary<
                int,
                SerializablePayload,
                SerializableDictionary.Cache<SerializablePayload>
            > secondRoundTrip = Serializer.ProtoDeserialize<
                SerializableDictionary<
                    int,
                    SerializablePayload,
                    SerializableDictionary.Cache<SerializablePayload>
                >
            >(secondSnapshot);

            SerializableDictionary<
                int,
                SerializablePayload,
                SerializableDictionary.Cache<SerializablePayload>
            > thirdRoundTrip = Serializer.ProtoDeserialize<
                SerializableDictionary<
                    int,
                    SerializablePayload,
                    SerializableDictionary.Cache<SerializablePayload>
                >
            >(thirdSnapshot);

            Assert.That(firstRoundTrip.Count, Is.EqualTo(2));
            Assert.That(firstRoundTrip[1].Name, Is.EqualTo("First"));
            Assert.That(firstRoundTrip[2].Name, Is.EqualTo("Second"));

            Assert.That(secondRoundTrip.Count, Is.EqualTo(2));
            Assert.That(secondRoundTrip[1].Name, Is.EqualTo("First Updated"));
            Assert.That(secondRoundTrip[3].Name, Is.EqualTo("Third"));

            Assert.That(thirdRoundTrip.Count, Is.EqualTo(1));
            Assert.That(thirdRoundTrip[4].Name, Is.EqualTo("Fourth"));

            secondRoundTrip[3] = new SerializablePayload { Id = 30, Name = "Third Updated" };
            secondRoundTrip.Add(5, new SerializablePayload { Id = 5, Name = "Fifth" });

            byte[] laterSnapshot = Serializer.ProtoSerialize(secondRoundTrip);
            SerializableDictionary<
                int,
                SerializablePayload,
                SerializableDictionary.Cache<SerializablePayload>
            > laterRoundTrip = Serializer.ProtoDeserialize<
                SerializableDictionary<
                    int,
                    SerializablePayload,
                    SerializableDictionary.Cache<SerializablePayload>
                >
            >(laterSnapshot);

            Assert.That(laterRoundTrip.Count, Is.EqualTo(3));
            Assert.That(laterRoundTrip[1].Name, Is.EqualTo("First Updated"));
            Assert.That(laterRoundTrip[3].Name, Is.EqualTo("Third Updated"));
            Assert.That(laterRoundTrip[5].Name, Is.EqualTo("Fifth"));
        }
    }
}
