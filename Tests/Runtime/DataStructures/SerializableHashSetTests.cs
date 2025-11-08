namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using Serializer = WallstopStudios.UnityHelpers.Core.Serialization.Serializer;

    public sealed class SerializableHashSetTests
    {
        [Test]
        public void AddContainsAndEnumerateValues()
        {
            SerializableHashSet<string> set = new SerializableHashSet<string>();
            bool addedAlpha = set.Add("alpha");
            bool addedBeta = set.Add("beta");
            bool duplicateAlpha = set.Add("alpha");

            Assert.IsTrue(addedAlpha);
            Assert.IsTrue(addedBeta);
            Assert.IsFalse(duplicateAlpha);
            Assert.IsTrue(set.Contains("alpha"));
            Assert.IsTrue(set.Contains("beta"));
            Assert.IsFalse(set.Contains("gamma"));

            List<string> values = new List<string>();
            foreach (string value in set)
            {
                values.Add(value);
            }

            Assert.AreEqual(2, values.Count);
            Assert.Contains("alpha", values);
            Assert.Contains("beta", values);
        }

        [Test]
        public void UnitySerializationPreservesDuplicateEntriesInBackingArray()
        {
            SerializableHashSet<int> set = new SerializableHashSet<int>();
            FieldInfo itemsField = typeof(SerializableHashSet<int>).GetField(
                "_items",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.IsNotNull(itemsField, "Unable to access serialized items field.");

            int[] duplicateSource = new int[] { 1, 1, 2 };
            itemsField.SetValue(set, duplicateSource);

            set.OnAfterDeserialize();

            object preservedItems = itemsField.GetValue(set);
            Assert.IsNotNull(preservedItems, "Duplicate entries should keep serialized cache.");
            Assert.AreSame(
                duplicateSource,
                preservedItems,
                "Duplicate cache should not be replaced."
            );
            Assert.AreEqual(2, set.Count);
            Assert.IsTrue(set.Contains(1));
            Assert.IsTrue(set.Contains(2));
        }

        [Test]
        public void UnitySerializationClearsCacheAfterMutation()
        {
            SerializableHashSet<int> set = new SerializableHashSet<int>();
            FieldInfo itemsField = typeof(SerializableHashSet<int>).GetField(
                "_items",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.IsNotNull(itemsField, "Unable to access serialized items field.");

            set.Add(5);
            set.Add(10);
            set.OnBeforeSerialize();

            int[] serializedBefore = (int[])itemsField.GetValue(set);
            Assert.IsNotNull(serializedBefore);
            CollectionAssert.AreEquivalent(new int[] { 5, 10 }, serializedBefore);

            set.OnAfterDeserialize();
            object cachedAfterDeserialize = itemsField.GetValue(set);
            Assert.IsNull(cachedAfterDeserialize, "No duplicates should clear serialized cache.");

            bool addedNew = set.Add(20);
            Assert.IsTrue(addedNew);

            set.OnBeforeSerialize();
            int[] serializedAfterMutation = (int[])itemsField.GetValue(set);
            Assert.IsNotNull(serializedAfterMutation);
            CollectionAssert.AreEquivalent(new int[] { 5, 10, 20 }, serializedAfterMutation);
        }

        [Test]
        public void ProtoSerializationRoundTripsValues()
        {
            SerializableHashSet<int> original = new SerializableHashSet<int>(new int[] { 1, 3, 5 });
            FieldInfo itemsField = typeof(SerializableHashSet<int>).GetField(
                "_items",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.IsNotNull(itemsField);

            byte[] payload = Serializer.ProtoSerialize(original);

            object cachedItems = itemsField.GetValue(original);
            Assert.IsNull(
                cachedItems,
                "Proto serialization should release cached arrays when no duplicates exist."
            );

            SerializableHashSet<int> roundTrip = Serializer.ProtoDeserialize<
                SerializableHashSet<int>
            >(payload);

            Assert.AreEqual(original.Count, roundTrip.Count);
            foreach (int value in original)
            {
                Assert.IsTrue(
                    roundTrip.Contains(value),
                    $"Missing value {value} after Proto round-trip."
                );
            }
        }

        [Test]
        public void JsonSerializationRoundTripsValues()
        {
            SerializableHashSet<string> original = new SerializableHashSet<string>(
                new string[] { "delta", "alpha", "gamma" }
            );

            string json = Serializer.JsonStringify(original);
            SerializableHashSet<string> roundTrip = Serializer.JsonDeserialize<
                SerializableHashSet<string>
            >(json);

            Assert.AreEqual(original.Count, roundTrip.Count);
            foreach (string token in original)
            {
                Assert.IsTrue(roundTrip.Contains(token), token);
            }
        }

        [Test]
        public void UnionWithPopulatesSetWithoutDuplicates()
        {
            SerializableHashSet<int> set = new SerializableHashSet<int>(new int[] { 1, 3, 5 });
            set.UnionWith(new int[] { 3, 5, 7, 9 });

            Assert.AreEqual(5, set.Count);
            int[] expected = new int[] { 1, 3, 5, 7, 9 };
            foreach (int value in expected)
            {
                Assert.IsTrue(set.Contains(value), $"Expected value {value} to be present.");
            }
        }
    }

    public sealed class SerializableSortedSetTests
    {
        [Test]
        public void SortedHashSetEnumeratesInComparerOrder()
        {
            SerializableSortedSet<int> set = new SerializableSortedSet<int>();
            set.Add(5);
            set.Add(1);
            set.Add(3);

            int[] expected = new int[] { 1, 3, 5 };
            int index = 0;
            foreach (int value in set)
            {
                Assert.Less(index, expected.Length);
                Assert.AreEqual(expected[index], value);
                index++;
            }

            Assert.AreEqual(expected.Length, index);
        }

        [Test]
        public void SortedHashSetProtoRoundTripRetainsOrdering()
        {
            SerializableSortedSet<string> original = new SerializableSortedSet<string>(
                new string[] { "kiwi", "apple", "mango" }
            );

            byte[] payload = Serializer.ProtoSerialize(original);
            SerializableSortedSet<string> roundTrip = Serializer.ProtoDeserialize<
                SerializableSortedSet<string>
            >(payload);

            string[] expected = roundTrip.ToArray();
            Array.Sort(expected, StringComparer.Ordinal);

            int index = 0;
            foreach (string value in roundTrip)
            {
                Assert.Less(index, expected.Length);
                Assert.AreEqual(expected[index], value);
                index++;
            }

            Assert.AreEqual(expected.Length, index);
        }

        [Test]
        public void SortedHashSetJsonRoundTripRetainsOrdering()
        {
            SerializableSortedSet<int> original = new SerializableSortedSet<int>();
            original.Add(42);
            original.Add(-5);
            original.Add(99);

            string json = Serializer.JsonStringify(original);
            SerializableSortedSet<int> roundTrip = Serializer.JsonDeserialize<
                SerializableSortedSet<int>
            >(json);

            int[] expected = roundTrip.ToArray();
            Array.Sort(expected);

            int index = 0;
            foreach (int value in roundTrip)
            {
                Assert.Less(index, expected.Length);
                Assert.AreEqual(expected[index], value);
                index++;
            }

            Assert.AreEqual(expected.Length, index);
        }

        [Test]
        public void UnityDeserializationPreservesDuplicateSerializedEntries()
        {
            SerializableSortedSet<int> set = new SerializableSortedSet<int>();
            FieldInfo itemsField = typeof(SerializableSortedSet<int>).GetField(
                "_items",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.IsNotNull(itemsField);

            itemsField.SetValue(set, new int[] { 1, 1, 2 });
            set.OnAfterDeserialize();

            object cached = itemsField.GetValue(set);
            Assert.IsNotNull(cached, "Duplicate entries must keep serialized cache for inspector.");

            int[] cachedValues = (int[])cached;
            CollectionAssert.AreEqual(new int[] { 1, 1, 2 }, cachedValues);
            Assert.AreEqual(2, set.Count);
            Assert.IsTrue(set.Contains(1));
            Assert.IsTrue(set.Contains(2));
        }
    }
}
