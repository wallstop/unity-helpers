namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Utils;
    using Serializer = WallstopStudios.UnityHelpers.Core.Serialization.Serializer;

    public sealed class SerializableHashSetTests
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

    public sealed class SerializableSortedSetTests
    {
        [Test]
        public void SortedHashSetEnumeratesInComparerOrder()
        {
            SerializableSortedSet<int> set = new() { 5, 1, 3 };

            int[] expected = { 1, 3, 5 };
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
        public void ToSortedSetReturnsIndependentCopy()
        {
            SerializableSortedSet<int> set = new();
            bool addedFive = set.Add(5);
            bool addedTwo = set.Add(2);

            Assert.IsTrue(addedFive);
            Assert.IsTrue(addedTwo);

            SortedSet<int> copy = set.ToSortedSet();

            Assert.AreEqual(set.Count, copy.Count);

            bool firstAssigned = false;
            int firstValue = 0;
            foreach (int value in copy)
            {
                if (!firstAssigned)
                {
                    firstValue = value;
                    firstAssigned = true;
                }
            }

            Assert.IsTrue(firstAssigned);
            Assert.AreEqual(2, firstValue);

            bool copyAdded = copy.Add(10);
            Assert.IsTrue(copyAdded);
            Assert.IsFalse(set.Contains(10));

            bool setAdded = set.Add(12);
            Assert.IsTrue(setAdded);
            Assert.IsFalse(copy.Contains(12));
        }

        [Test]
        public void SortedHashSetProtoRoundTripRetainsOrdering()
        {
            SerializableSortedSet<string> original = new(new[] { "kiwi", "apple", "mango" });

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
            SerializableSortedSet<int> original = new() { 42, -5, 99 };

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
            SerializableSortedSet<int> set = new() { _items = new[] { 1, 1, 2 } };
            set.OnAfterDeserialize();

            int[] cached = set.SerializedItems;
            Assert.IsNotNull(cached, "Duplicate entries must keep serialized cache for inspector.");

            CollectionAssert.AreEqual(new[] { 1, 1, 2 }, cached);
            Assert.AreEqual(2, set.Count);
            Assert.IsTrue(set.Contains(1));
            Assert.IsTrue(set.Contains(2));
        }

        [Test]
        public void NullSerializedItemsResetStateDuringDeserialization()
        {
            SerializableHashSet<string> set = new() { "alpha" };
            set._items = null;

            set.OnAfterDeserialize();

            Assert.AreEqual(0, set.Count);
            Assert.IsFalse(set.PreserveSerializedEntries);
        }

        [Test]
        public void SortedSetNullEntriesAreSkippedDuringDeserialization()
        {
            SerializableSortedSet<ScriptableSample> set = new();
            ScriptableSample valid = ScriptableObject.CreateInstance<ScriptableSample>();
            set._items = new[] { null, valid };

            LogAssert.Expect(
                LogType.Error,
                "SerializableSet<WallstopStudios.UnityHelpers.Tests.DataStructures.SerializableSortedSetTests+ScriptableSample> skipped serialized entry at index 0 because the value reference was null."
            );

            set.OnAfterDeserialize();

            Assert.AreEqual(1, set.Count);
            Assert.IsTrue(set.Contains(valid));

            object cached = set._items;
            Assert.IsNotNull(cached, "Serialized cache should be preserved for inspector review.");
            ScriptableSample[] cachedValues = (ScriptableSample[])cached;
            CollectionAssert.AreEqual(new[] { null, valid }, cachedValues);

            LogAssert.NoUnexpectedReceived();

            ScriptableObject.DestroyImmediate(valid);
        }

        [Test]
        public void UnityDeserializationRestoresSortOrderFromUnsortedSerializedItems()
        {
            SerializableSortedSet<string> set = new();
            string[] unsorted = { "delta", "alpha", "charlie" };
            set._items = unsorted;

            set.OnAfterDeserialize();

            string[] enumeration = set.ToArray();
            CollectionAssert.AreEqual(
                new[] { "alpha", "charlie", "delta" },
                enumeration,
                "SortedSet enumeration should follow comparer order after deserialization."
            );

            Assert.IsNull(
                set._items,
                "Serialized cache should be released when no null entries or duplicates remain."
            );
        }

        [Test]
        public void UnitySerializationRebuildsSortedSetCacheAfterDeserialization()
        {
            SerializableSortedSet<string> set = new();
            string[] serializedItems = { "delta", "alpha", "charlie" };
            set._items = serializedItems;

            set.OnAfterDeserialize();

            Assert.IsNull(
                set._items,
                "Deserialization should release serialized cache when entries can be reconstructed."
            );

            set.OnBeforeSerialize();

            string[] rebuiltItems = set._items;

            Assert.IsNotNull(rebuiltItems);
            CollectionAssert.AreEqual(
                new[] { "alpha", "charlie", "delta" },
                rebuiltItems,
                "Serialization should rebuild sorted cache after cache was cleared."
            );
        }

        [Test]
        public void SortedSetInspectorReportsSortingSupport()
        {
            SerializableSortedSet<int> set = new();
            ISerializableSetInspector inspector = set;

            Assert.IsTrue(inspector.SupportsSorting);
        }

        [Test]
        public void JsonRoundTripClearsCacheAndRebuildsSortedSetSnapshot()
        {
            SerializableSortedSet<int> original = new(new[] { 5, 1, 3 });

            string json = Serializer.JsonStringify(original);
            SerializableSortedSet<int> roundTrip = Serializer.JsonDeserialize<
                SerializableSortedSet<int>
            >(json);

            Assert.AreEqual(3, roundTrip.Count);
            Assert.IsNull(roundTrip.SerializedItems);
            Assert.IsFalse(roundTrip.PreserveSerializedEntries);

            roundTrip.OnBeforeSerialize();

            CollectionAssert.AreEqual(new[] { 1, 3, 5 }, roundTrip.SerializedItems);
        }

        private sealed class SortedSample : IComparable<SortedSample>, IComparable
        {
            public SortedSample(string token)
            {
                Token = token;
            }

            public string Token { get; }

            public override bool Equals(object candidate)
            {
                if (candidate is SortedSample other)
                {
                    return string.Equals(Token, other.Token, StringComparison.Ordinal);
                }

                return false;
            }

            public override int GetHashCode()
            {
                if (Token == null)
                {
                    return 0;
                }

                return Token.GetHashCode(StringComparison.Ordinal);
            }

            public int CompareTo(SortedSample other)
            {
                if (other == null)
                {
                    return 1;
                }

                return string.CompareOrdinal(Token, other.Token);
            }

            int IComparable.CompareTo(object obj)
            {
                if (obj is SortedSample other)
                {
                    return CompareTo(other);
                }

                return -1;
            }
        }

        private sealed class ScriptableSample
            : ScriptableObject,
                IComparable<ScriptableSample>,
                IComparable
        {
            public int CompareTo(ScriptableSample other)
            {
                if (other == null)
                {
                    return 1;
                }

                return UnityObjectNameComparer<ScriptableSample>.Instance.Compare(this, other);
            }

            public int CompareTo(object obj)
            {
                if (obj is ScriptableSample other)
                {
                    return CompareTo(other);
                }

                return -1;
            }
        }

        [Test]
        public void EnumeratorIsValueTypeAndMaintainsSortOrder()
        {
            SerializableSortedSet<int> set = new() { 4, 1, 9 };

            using SortedSet<int>.Enumerator enumerator = set.GetEnumerator();
            Assert.IsTrue(enumerator.GetType().IsValueType);

            List<int> values = new();
            while (enumerator.MoveNext())
            {
                values.Add(enumerator.Current);
            }

            CollectionAssert.AreEqual(new[] { 1, 4, 9 }, values);
        }

        [Test]
        public void EnumeratorThrowsAfterMutationLikeSortedSet()
        {
            SerializableSortedSet<int> set = new() { 2, 5 };

            using SortedSet<int>.Enumerator enumerator = set.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());

            set.Add(7);

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Test]
        public void TryGetValueReturnsStoredReferenceForMatchingComparable()
        {
            SerializableSortedSet<SortedSample> set = new();

            SortedSample stored = new("delta");
            set.Add(stored);

            SortedSample probe = new("delta");

            bool found = set.TryGetValue(probe, out SortedSample resolved);
            Assert.IsTrue(found);
            Assert.AreSame(stored, resolved);
        }

        [Test]
        public void MinAndMaxMatchSystemSortedSet()
        {
            SerializableSortedSet<int> serializable = new() { 8, -1, 13 };

            SortedSet<int> baseline = new() { 8, -1, 13 };

            Assert.AreEqual(baseline.Min, serializable.Min);
            Assert.AreEqual(baseline.Max, serializable.Max);
        }

        [Test]
        public void ReverseMatchesSystemSortedSet()
        {
            SerializableSortedSet<int> serializable = new() { 10, 4, 6 };

            SortedSet<int> baseline = new() { 10, 4, 6 };

            int[] serializableReverse = serializable.Reverse().ToArray();
            int[] baselineReverse = baseline.Reverse().ToArray();

            CollectionAssert.AreEqual(baselineReverse, serializableReverse);
        }

        [Test]
        public void ViewBetweenMatchesSystemSortedSet()
        {
            SerializableSortedSet<int> serializable = new() { 1, 3, 5, 7 };

            SortedSet<int> baseline = new() { 1, 3, 5, 7 };

            SortedSet<int> serializableView = serializable.GetViewBetween(2, 6);
            SortedSet<int> baselineView = baseline.GetViewBetween(2, 6);

            CollectionAssert.AreEqual(baselineView.ToArray(), serializableView.ToArray());
        }

        [Test]
        public void ProtoSerializationProducesIndependentCopy()
        {
            SerializableSortedSet<int> original = new() { 3, 1, 5 };

            byte[] payload = Serializer.ProtoSerialize(original);

            original.Add(7);

            SerializableSortedSet<int> roundTrip = Serializer.ProtoDeserialize<
                SerializableSortedSet<int>
            >(payload);

            Assert.IsFalse(original.SetEquals(roundTrip));
            Assert.IsTrue(original.Contains(7));
            Assert.IsFalse(roundTrip.Contains(7));

            int[] expectedOrder = { 1, 3, 5 };
            CollectionAssert.AreEqual(expectedOrder, roundTrip.ToArray());
        }

        [Test]
        public void JsonSerializationProducesIndependentCopy()
        {
            SerializableSortedSet<string> original = new() { "bravo", "alpha", "charlie" };

            string json = Serializer.JsonStringify(original);

            original.Add("delta");

            SerializableSortedSet<string> roundTrip = Serializer.JsonDeserialize<
                SerializableSortedSet<string>
            >(json);

            Assert.IsFalse(original.SetEquals(roundTrip));
            Assert.IsTrue(original.Contains("delta"));
            Assert.IsFalse(roundTrip.Contains("delta"));

            string[] expected = { "alpha", "bravo", "charlie" };
            CollectionAssert.AreEqual(expected, roundTrip.ToArray());
        }
    }
}
