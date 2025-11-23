namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;
    using Serializer = WallstopStudios.UnityHelpers.Core.Serialization.Serializer;

    public sealed class SerializableHashSetTests : CommonTestBase
    {
        [Test]
        public void AddContainsAndEnumerateValues()
        {
            SerializableHashSet<string> set = new();
            bool addedAlpha = set.Add("alpha");
            bool addedBeta = set.Add("beta");
            bool duplicateAlpha = set.Add("alpha");

            Assert.IsTrue(addedAlpha);
            Assert.IsTrue(addedBeta);
            Assert.IsFalse(duplicateAlpha);
            Assert.IsTrue(set.Contains("alpha"));
            Assert.IsTrue(set.Contains("beta"));
            Assert.IsFalse(set.Contains("gamma"));

            List<string> values = new();
            foreach (string value in set)
            {
                values.Add(value);
            }

            Assert.AreEqual(2, values.Count);
            Assert.Contains("alpha", values);
            Assert.Contains("beta", values);
        }

        [Test]
        public void ToHashSetReturnsIndependentCopy()
        {
            SerializableHashSet<int> set = new();
            bool addedOne = set.Add(1);
            bool addedTwo = set.Add(2);

            Assert.IsTrue(addedOne);
            Assert.IsTrue(addedTwo);

            HashSet<int> copy = set.ToHashSet();

            Assert.AreEqual(set.Count, copy.Count);
            Assert.IsTrue(copy.Contains(1));
            Assert.IsTrue(copy.Contains(2));

            bool copyAddedThree = copy.Add(3);
            Assert.IsTrue(copyAddedThree);
            Assert.IsFalse(set.Contains(3));

            bool setAddedFour = set.Add(4);
            Assert.IsTrue(setAddedFour);
            Assert.IsFalse(copy.Contains(4));
        }

        [Test]
        public void NullEntriesAreSkippedDuringDeserialization()
        {
            SerializableHashSet<string> set = new();
            string[] source = { null, "valid" };
            set._items = source;

            LogAssert.Expect(
                LogType.Error,
                "SerializableSet<System.String> skipped serialized entry at index 0 because the value reference was null."
            );

            set.OnAfterDeserialize();

            Assert.AreEqual(1, set.Count);
            Assert.IsTrue(set.Contains("valid"));

            string[] stored = set._items;
            bool preserve = set.PreserveSerializedEntries;

            Assert.IsNotNull(stored, "Serialized items should remain when null entries exist.");
            CollectionAssert.AreEqual(source, stored);
            Assert.IsTrue(preserve, "Null entries should preserve serialized cache.");

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void UnitySerializationPreservesDuplicateEntriesInBackingArray()
        {
            SerializableHashSet<int> set = new();
            int[] duplicateSource = { 1, 1, 2 };
            set._items = duplicateSource;

            set.OnAfterDeserialize();

            object preservedItems = set._items;
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
        public void ComparerDuplicatesPreserveSerializedItems()
        {
            SerializableHashSet<string> set = new(StringComparer.OrdinalIgnoreCase);
            string[] serializedItems = { "ALPHA", "alpha", "beta" };
            set._items = serializedItems;

            set.OnAfterDeserialize();

            Assert.AreEqual(2, set.Count);
            Assert.IsTrue(set.Contains("alpha"));
            Assert.IsTrue(set.Contains("beta"));

            Assert.AreSame(
                serializedItems,
                set.SerializedItems,
                "Comparer-driven duplicates must keep serialized cache for inspector review."
            );
            Assert.IsTrue(set.PreserveSerializedEntries);
        }

        [Test]
        public void UnitySerializationClearsCacheAfterMutation()
        {
            SerializableHashSet<int> set = new() { 5, 10 };
            set.OnBeforeSerialize();

            int[] serializedBefore = set._items;
            Assert.IsNotNull(serializedBefore);
            CollectionAssert.AreEquivalent(new[] { 5, 10 }, serializedBefore);

            set.OnAfterDeserialize();
            object cachedAfterDeserialize = set._items;
            Assert.IsNull(cachedAfterDeserialize, "No duplicates should clear serialized cache.");

            bool addedNew = set.Add(20);
            Assert.IsTrue(addedNew);

            set.OnBeforeSerialize();
            int[] serializedAfterMutation = set._items;
            Assert.IsNotNull(serializedAfterMutation);
            CollectionAssert.AreEquivalent(new[] { 5, 10, 20 }, serializedAfterMutation);
        }

        [Test]
        public void ProtoSerializationRoundTripsValues()
        {
            SerializableHashSet<int> original = new(new[] { 1, 3, 5 });
            byte[] payload = Serializer.ProtoSerialize(original);

            object cachedItems = original._items;
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
            SerializableHashSet<string> original = new(new[] { "delta", "alpha", "gamma" });

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
        public void InspectorSnapshotRoundTripsSerializedItems()
        {
            SerializableHashSet<int> set = new();
            ISerializableSetInspector inspector = set;
            int[] snapshot = { 5, 10, 15 };

            inspector.SetSerializedItemsSnapshot(snapshot, preserveSerializedEntries: true);

            Array stored = inspector.GetSerializedItemsSnapshot();
            CollectionAssert.AreEqual(snapshot, stored);
            Assert.IsTrue(set.PreserveSerializedEntries);

            set.OnAfterDeserialize();

            Assert.AreEqual(3, set.Count);
            Assert.IsTrue(set.Contains(5));
            Assert.IsTrue(set.Contains(10));
            Assert.IsTrue(set.Contains(15));
        }

        [Test]
        public void JsonRoundTripClearsCacheAndRebuildsHashSetSnapshot()
        {
            SerializableHashSet<int> original = new(new[] { 9, 4, 7 });

            string json = Serializer.JsonStringify(original);
            SerializableHashSet<int> roundTrip = Serializer.JsonDeserialize<
                SerializableHashSet<int>
            >(json);

            Assert.AreEqual(original.Count, roundTrip.Count);
            Assert.IsNull(roundTrip.SerializedItems);
            Assert.IsFalse(roundTrip.PreserveSerializedEntries);

            roundTrip.OnBeforeSerialize();

            int[] rebuiltItems = roundTrip.SerializedItems;
            Assert.IsNotNull(rebuiltItems);
            CollectionAssert.AreEquivalent(new[] { 4, 7, 9 }, rebuiltItems);
        }

        [Test]
        public void UnionWithPopulatesSetWithoutDuplicates()
        {
            SerializableHashSet<int> set = new(new[] { 1, 3, 5 });
            set.UnionWith(new[] { 3, 5, 7, 9 });

            Assert.AreEqual(5, set.Count);
            int[] expected = { 1, 3, 5, 7, 9 };
            foreach (int value in expected)
            {
                Assert.IsTrue(set.Contains(value), $"Expected value {value} to be present.");
            }
        }

        private sealed class SampleValue
        {
            public SampleValue(string identifier)
            {
                Identifier = identifier;
            }

            public string Identifier { get; }

            public override bool Equals(object candidate)
            {
                if (candidate is SampleValue other)
                {
                    return string.Equals(Identifier, other.Identifier, StringComparison.Ordinal);
                }

                return false;
            }

            public override int GetHashCode()
            {
                if (Identifier == null)
                {
                    return 0;
                }

                return Identifier.GetHashCode(StringComparison.Ordinal);
            }
        }

        [Test]
        public void EnumeratorIsValueTypeAndSupportsForEach()
        {
            SerializableHashSet<int> set = new(new[] { 1, 2, 3 });
            using HashSet<int>.Enumerator enumerator = set.GetEnumerator();

            Assert.IsTrue(enumerator.GetType().IsValueType);

            List<int> values = new();
            while (enumerator.MoveNext())
            {
                values.Add(enumerator.Current);
            }

            Assert.AreEqual(set.Count, values.Count);
            foreach (int value in values)
            {
                Assert.IsTrue(set.Contains(value));
            }
        }

        [Test]
        public void EnumeratorThrowsAfterMutationLikeHashSet()
        {
            SerializableHashSet<int> set = new(new[] { 1, 2, 3 });
            using HashSet<int>.Enumerator enumerator = set.GetEnumerator();

            Assert.IsTrue(enumerator.MoveNext());
            set.Add(4);

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Test]
        public void TryGetValueReturnsStoredInstance()
        {
            SerializableHashSet<SampleValue> set = new();
            SampleValue stored = new("gamma");
            set.Add(stored);

            SampleValue probe = new("gamma");
            bool found = set.TryGetValue(probe, out SampleValue resolved);

            Assert.IsTrue(found);
            Assert.AreSame(stored, resolved);
        }

        [Test]
        public void CopyToWithCountMatchesSystemHashSet()
        {
            SerializableHashSet<int> serializable = new(new[] { 5, 7, 11, 13, 17 });

            HashSet<int> baseline = new(new[] { 5, 7, 11, 13, 17 });

            int[] serializableTarget = new int[8];
            int[] baselineTarget = new int[8];

            serializable.CopyTo(serializableTarget, 1, 4);
            baseline.CopyTo(baselineTarget, 1, 4);

            CollectionAssert.AreEqual(baselineTarget, serializableTarget);
        }

        [Test]
        public void SetOperationsMatchSystemHashSet()
        {
            SerializableHashSet<int> serializable = new(new[] { 1, 3, 5, 7 });
            HashSet<int> baseline = new(new[] { 1, 3, 5, 7 });

            int[] unionSource = { 5, 6, 9 };
            serializable.UnionWith(unionSource);
            baseline.UnionWith(unionSource);

            int[] exceptSource = { 1, 9 };
            serializable.ExceptWith(exceptSource);
            baseline.ExceptWith(exceptSource);

            int[] symmetricSource = { 3, 4, 6 };
            serializable.SymmetricExceptWith(symmetricSource);
            baseline.SymmetricExceptWith(symmetricSource);

            int[] intersectSource = { 4, 5, 6 };
            serializable.IntersectWith(intersectSource);
            baseline.IntersectWith(intersectSource);

            Assert.IsTrue(serializable.SetEquals(baseline));
            Assert.AreEqual(baseline.Count, serializable.Count);
        }

        [Test]
        public void ProtoSerializationProducesIndependentCopy()
        {
            SerializableHashSet<int> original = new(new[] { 2, 4, 6 });

            byte[] payload = Serializer.ProtoSerialize(original);

            original.Add(8);

            SerializableHashSet<int> roundTrip = Serializer.ProtoDeserialize<
                SerializableHashSet<int>
            >(payload);

            Assert.IsFalse(original.SetEquals(roundTrip));
            Assert.IsTrue(original.Contains(8));
            Assert.IsFalse(roundTrip.Contains(8));

            roundTrip.Add(10);
            Assert.IsFalse(original.Contains(10));
        }

        [Test]
        public void JsonSerializationProducesIndependentCopy()
        {
            SerializableHashSet<string> original = new(new[] { "alpha", "beta", "delta" });

            string json = Serializer.JsonStringify(original);

            original.Add("epsilon");

            SerializableHashSet<string> roundTrip = Serializer.JsonDeserialize<
                SerializableHashSet<string>
            >(json);

            Assert.IsFalse(original.SetEquals(roundTrip));
            Assert.IsTrue(original.Contains("epsilon"));
            Assert.IsFalse(roundTrip.Contains("epsilon"));

            roundTrip.Add("omega");
            Assert.IsFalse(original.Contains("omega"));
        }
    }
}
