namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
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
        public void NullEntriesAreSkippedDuringDeserialization()
        {
            SerializableHashSet<string> set = new SerializableHashSet<string>();
            FieldInfo itemsField = typeof(SerializableHashSet<string>).GetField(
                "_items",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Type baseType = typeof(SerializableHashSet<string>).BaseType;
            Assert.IsNotNull(baseType, "Base type lookup failed.");
            FieldInfo preserveField = baseType.GetField(
                "_preserveSerializedEntries",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            Assert.IsNotNull(itemsField, "Unable to access serialized items field.");
            Assert.IsNotNull(preserveField, "Unable to access preserve flag field.");

            string[] source = new string[] { null, "valid" };
            itemsField.SetValue(set, source);

            set.OnAfterDeserialize();

            Assert.AreEqual(1, set.Count);
            Assert.IsTrue(set.Contains("valid"));

            string[] stored = (string[])itemsField.GetValue(set);
            bool preserve = (bool)preserveField.GetValue(set);

            Assert.IsNotNull(stored, "Serialized items should remain when null entries exist.");
            CollectionAssert.AreEqual(source, stored);
            Assert.IsTrue(preserve, "Null entries should preserve serialized cache.");

            LogAssert.NoUnexpectedReceived();
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
            SerializableHashSet<int> set = new SerializableHashSet<int>(new int[] { 1, 2, 3 });
            HashSet<int>.Enumerator enumerator = set.GetEnumerator();

            Assert.IsTrue(enumerator.GetType().IsValueType);

            List<int> values = new List<int>();
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
            SerializableHashSet<int> set = new SerializableHashSet<int>(new int[] { 1, 2, 3 });
            HashSet<int>.Enumerator enumerator = set.GetEnumerator();

            Assert.IsTrue(enumerator.MoveNext());
            set.Add(4);

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Test]
        public void TryGetValueReturnsStoredInstance()
        {
            SerializableHashSet<SampleValue> set = new SerializableHashSet<SampleValue>();
            SampleValue stored = new SampleValue("gamma");
            set.Add(stored);

            SampleValue probe = new SampleValue("gamma");
            bool found = set.TryGetValue(probe, out SampleValue resolved);

            Assert.IsTrue(found);
            Assert.AreSame(stored, resolved);
        }

        [Test]
        public void CopyToWithCountMatchesSystemHashSet()
        {
            SerializableHashSet<int> serializable = new SerializableHashSet<int>(
                new int[] { 5, 7, 11, 13, 17 }
            );

            HashSet<int> baseline = new HashSet<int>(new int[] { 5, 7, 11, 13, 17 });

            int[] serializableTarget = new int[8];
            int[] baselineTarget = new int[8];

            serializable.CopyTo(serializableTarget, 1, 4);
            baseline.CopyTo(baselineTarget, 1, 4);

            CollectionAssert.AreEqual(baselineTarget, serializableTarget);
        }

        [Test]
        public void SetOperationsMatchSystemHashSet()
        {
            SerializableHashSet<int> serializable = new SerializableHashSet<int>(
                new int[] { 1, 3, 5, 7 }
            );
            HashSet<int> baseline = new HashSet<int>(new int[] { 1, 3, 5, 7 });

            int[] unionSource = new int[] { 5, 6, 9 };
            serializable.UnionWith(unionSource);
            baseline.UnionWith(unionSource);

            int[] exceptSource = new int[] { 1, 9 };
            serializable.ExceptWith(exceptSource);
            baseline.ExceptWith(exceptSource);

            int[] symmetricSource = new int[] { 3, 4, 6 };
            serializable.SymmetricExceptWith(symmetricSource);
            baseline.SymmetricExceptWith(symmetricSource);

            int[] intersectSource = new int[] { 4, 5, 6 };
            serializable.IntersectWith(intersectSource);
            baseline.IntersectWith(intersectSource);

            Assert.IsTrue(serializable.SetEquals(baseline));
            Assert.AreEqual(baseline.Count, serializable.Count);
        }

        [Test]
        public void ProtoSerializationProducesIndependentCopy()
        {
            SerializableHashSet<int> original = new SerializableHashSet<int>(new int[] { 2, 4, 6 });

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
            SerializableHashSet<string> original = new SerializableHashSet<string>(
                new string[] { "alpha", "beta", "delta" }
            );

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

        [Test]
        public void UnityDeserializationRestoresSortOrderFromUnsortedSerializedItems()
        {
            SerializableSortedSet<string> set = new SerializableSortedSet<string>();
            FieldInfo itemsField = typeof(SerializableSortedSet<string>).GetField(
                "_items",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.IsNotNull(itemsField, "Unable to access serialized items backing field.");

            string[] unsorted = new[] { "delta", "alpha", "charlie" };
            itemsField.SetValue(set, unsorted);

            set.OnAfterDeserialize();

            string[] enumeration = set.ToArray();
            CollectionAssert.AreEqual(
                new[] { "alpha", "charlie", "delta" },
                enumeration,
                "SortedSet enumeration should follow comparer order after deserialization."
            );

            string[] preservedItems = (string[])itemsField.GetValue(set);
            CollectionAssert.AreEqual(
                unsorted,
                preservedItems,
                "Serialized cache should retain the inspector ordering."
            );
        }

        private sealed class SortedSample
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
        }

        private sealed class SortedSampleComparer : IComparer<SortedSample>
        {
            public int Compare(SortedSample x, SortedSample y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }

                if (x == null)
                {
                    return -1;
                }

                if (y == null)
                {
                    return 1;
                }

                return string.Compare(x.Token, y.Token, StringComparison.Ordinal);
            }
        }

        [Test]
        public void EnumeratorIsValueTypeAndMaintainsSortOrder()
        {
            SerializableSortedSet<int> set = new SerializableSortedSet<int>();
            set.Add(4);
            set.Add(1);
            set.Add(9);

            SortedSet<int>.Enumerator enumerator = set.GetEnumerator();
            Assert.IsTrue(enumerator.GetType().IsValueType);

            List<int> values = new List<int>();
            while (enumerator.MoveNext())
            {
                values.Add(enumerator.Current);
            }

            CollectionAssert.AreEqual(new int[] { 1, 4, 9 }, values);
        }

        [Test]
        public void EnumeratorThrowsAfterMutationLikeSortedSet()
        {
            SerializableSortedSet<int> set = new SerializableSortedSet<int>();
            set.Add(2);
            set.Add(5);

            SortedSet<int>.Enumerator enumerator = set.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());

            set.Add(7);

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Test]
        public void TryGetValueReturnsStoredReferenceWithComparer()
        {
            SerializableSortedSet<SortedSample> set = new SerializableSortedSet<SortedSample>(
                new SortedSampleComparer()
            );

            SortedSample stored = new SortedSample("delta");
            set.Add(stored);

            SortedSample probe = new SortedSample("delta");

            bool found = set.TryGetValue(probe, out SortedSample resolved);
            Assert.IsTrue(found);
            Assert.AreSame(stored, resolved);
        }

        [Test]
        public void MinAndMaxMatchSystemSortedSet()
        {
            SerializableSortedSet<int> serializable = new SerializableSortedSet<int>();
            serializable.Add(8);
            serializable.Add(-1);
            serializable.Add(13);

            SortedSet<int> baseline = new SortedSet<int>();
            baseline.Add(8);
            baseline.Add(-1);
            baseline.Add(13);

            Assert.AreEqual(baseline.Min, serializable.Min);
            Assert.AreEqual(baseline.Max, serializable.Max);
        }

        [Test]
        public void ReverseMatchesSystemSortedSet()
        {
            SerializableSortedSet<int> serializable = new SerializableSortedSet<int>();
            serializable.Add(10);
            serializable.Add(4);
            serializable.Add(6);

            SortedSet<int> baseline = new SortedSet<int>();
            baseline.Add(10);
            baseline.Add(4);
            baseline.Add(6);

            int[] serializableReverse = serializable.Reverse().ToArray();
            int[] baselineReverse = baseline.Reverse().ToArray();

            CollectionAssert.AreEqual(baselineReverse, serializableReverse);
        }

        [Test]
        public void ViewBetweenMatchesSystemSortedSet()
        {
            SerializableSortedSet<int> serializable = new SerializableSortedSet<int>();
            serializable.Add(1);
            serializable.Add(3);
            serializable.Add(5);
            serializable.Add(7);

            SortedSet<int> baseline = new SortedSet<int>();
            baseline.Add(1);
            baseline.Add(3);
            baseline.Add(5);
            baseline.Add(7);

            SortedSet<int> serializableView = serializable.GetViewBetween(2, 6);
            SortedSet<int> baselineView = baseline.GetViewBetween(2, 6);

            CollectionAssert.AreEqual(baselineView.ToArray(), serializableView.ToArray());
        }

        [Test]
        public void ProtoSerializationProducesIndependentCopy()
        {
            SerializableSortedSet<int> original = new SerializableSortedSet<int>();
            original.Add(3);
            original.Add(1);
            original.Add(5);

            byte[] payload = Serializer.ProtoSerialize(original);

            original.Add(7);

            SerializableSortedSet<int> roundTrip = Serializer.ProtoDeserialize<
                SerializableSortedSet<int>
            >(payload);

            Assert.IsFalse(original.SetEquals(roundTrip));
            Assert.IsTrue(original.Contains(7));
            Assert.IsFalse(roundTrip.Contains(7));

            int[] expectedOrder = new int[] { 1, 3, 5 };
            CollectionAssert.AreEqual(expectedOrder, roundTrip.ToArray());
        }

        [Test]
        public void JsonSerializationProducesIndependentCopy()
        {
            SerializableSortedSet<string> original = new SerializableSortedSet<string>();
            original.Add("bravo");
            original.Add("alpha");
            original.Add("charlie");

            string json = Serializer.JsonStringify(original);

            original.Add("delta");

            SerializableSortedSet<string> roundTrip = Serializer.JsonDeserialize<
                SerializableSortedSet<string>
            >(json);

            Assert.IsFalse(original.SetEquals(roundTrip));
            Assert.IsTrue(original.Contains("delta"));
            Assert.IsFalse(roundTrip.Contains("delta"));

            string[] expected = new string[] { "alpha", "bravo", "charlie" };
            CollectionAssert.AreEqual(expected, roundTrip.ToArray());
        }
    }
}
