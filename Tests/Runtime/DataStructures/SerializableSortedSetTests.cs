namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Core.Serialization;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class SerializableSortedSetTests : CommonTestBase
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
        public void CustomComparerDuplicatesPreserveSerializedItems()
        {
            SerializableSortedSet<CaseInsensitiveString> set = new();
            CaseInsensitiveString[] serializedItems = { new("ALPHA"), new("alpha"), new("bravo") };
            set._items = serializedItems;

            set.OnAfterDeserialize();

            Assert.AreEqual(2, set.Count);
            Assert.IsTrue(set.Contains(new CaseInsensitiveString("alpha")));
            Assert.IsTrue(set.Contains(new CaseInsensitiveString("BRAVO")));
            Assert.AreSame(
                serializedItems,
                set.SerializedItems,
                "Comparer-driven duplicates must keep serialized cache."
            );
            Assert.IsTrue(set.PreserveSerializedEntries);
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
            ScriptableSample valid = Track(ScriptableObject.CreateInstance<ScriptableSample>());
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
        }

        [Test]
        public void EditorAfterDeserializeSuppressesNullWarnings()
        {
            SerializableSortedSet<ScriptableSample> set = new();
            set._items = new ScriptableSample[] { null };

            ISerializableSetEditorSync editorSync = set;
            editorSync.EditorAfterDeserialize();

            Assert.AreEqual(0, set.Count);
            Assert.AreSame(set._items, set.SerializedItems);
            Assert.IsTrue(set.PreserveSerializedEntries);
            LogAssert.NoUnexpectedReceived();
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
        public void InspectorSnapshotWithDuplicatesPreservesCache()
        {
            SerializableSortedSet<int> set = new();
            ISerializableSetInspector inspector = set;
            int[] snapshot = { 2, 2, 3 };

            inspector.SetSerializedItemsSnapshot(snapshot, preserveSerializedEntries: true);
            set.OnAfterDeserialize();

            Assert.AreEqual(2, set.Count);
            Assert.IsTrue(set.Contains(2));
            Assert.IsTrue(set.Contains(3));
            CollectionAssert.AreEqual(snapshot, set.SerializedItems);
            Assert.IsTrue(set.PreserveSerializedEntries);
        }

        [Test]
        public void InspectorSnapshotWithoutPreserveClearsCache()
        {
            SerializableSortedSet<int> set = new();
            ISerializableSetInspector inspector = set;
            int[] snapshot = { 4, 5 };

            inspector.SetSerializedItemsSnapshot(snapshot, preserveSerializedEntries: false);
            set.OnAfterDeserialize();

            Assert.AreEqual(2, set.Count);
            Assert.IsFalse(set.PreserveSerializedEntries);
            Assert.IsNull(set.SerializedItems);
            CollectionAssert.AreEquivalent(snapshot, set.ToArray());
        }

        [Test]
        public void InspectorSynchronizeSerializedStateStoresOrderedSnapshot()
        {
            SerializableSortedSet<int> set = new() { 5, 1, 3 };
            ISerializableSetInspector inspector = set;

            inspector.SynchronizeSerializedState();

            Array snapshot = inspector.GetSerializedItemsSnapshot();
            CollectionAssert.AreEqual(new[] { 1, 3, 5 }, snapshot);
            Assert.IsFalse(set.PreserveSerializedEntries);
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

        private sealed class CaseInsensitiveString : IComparable<CaseInsensitiveString>, IComparable
        {
            public CaseInsensitiveString(string value)
            {
                Value = value;
            }

            public string Value { get; }

            public int CompareTo(CaseInsensitiveString other)
            {
                return other == null
                    ? 1
                    : string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
            }

            int IComparable.CompareTo(object obj)
            {
                if (ReferenceEquals(this, obj))
                {
                    return 0;
                }

                if (obj is CaseInsensitiveString candidate)
                {
                    return CompareTo(candidate);
                }

                return 1;
            }

            public override bool Equals(object obj)
            {
                if (obj is CaseInsensitiveString other)
                {
                    return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return Value == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
            }

            public override string ToString()
            {
                return Value ?? string.Empty;
            }
        }
    }
}
