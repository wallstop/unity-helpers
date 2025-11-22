namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using Serializer = WallstopStudios.UnityHelpers.Core.Serialization.Serializer;

    public sealed class SerializableSortedDictionaryTests
    {
        [Test]
        public void EntriesEnumerateInAscendingOrder()
        {
            SerializableSortedDictionary<int, string> dictionary = new()
            {
                { 3, "three" },
                { 1, "one" },
                { 2, "two" },
            };

            int[] expectedKeys = { 1, 2, 3 };
            int index = 0;
            foreach (KeyValuePair<int, string> pair in dictionary)
            {
                Assert.Less(index, expectedKeys.Length);
                Assert.AreEqual(expectedKeys[index], pair.Key);
                index++;
            }

            Assert.AreEqual(expectedKeys.Length, index);
        }

        [Test]
        public void CopyFromOrdersSourceDictionary()
        {
            SerializableSortedDictionary<int, string> dictionary = new();
            Dictionary<int, string> source = new()
            {
                { 7, "seven" },
                { 4, "four" },
                { 5, "five" },
            };

            dictionary.CopyFrom(source);

            int[] expectedKeys = { 4, 5, 7 };
            int index = 0;
            foreach (KeyValuePair<int, string> pair in dictionary)
            {
                Assert.Less(index, expectedKeys.Length);
                Assert.AreEqual(expectedKeys[index], pair.Key);
                index++;
            }

            Assert.AreEqual(expectedKeys.Length, index);
        }

        [Test]
        public void ToSortedDictionaryReturnsIndependentCopy()
        {
            SerializableSortedDictionary<int, string> dictionary = new()
            {
                { 2, "two" },
                { 1, "one" },
            };

            SortedDictionary<int, string> copy = dictionary.ToSortedDictionary();

            Assert.AreEqual(dictionary.Count, copy.Count);
            Assert.AreEqual("two", copy[2]);

            bool firstAssigned = false;
            KeyValuePair<int, string> firstPair = default;
            foreach (KeyValuePair<int, string> pair in copy)
            {
                if (!firstAssigned)
                {
                    firstPair = pair;
                    firstAssigned = true;
                }
            }

            Assert.IsTrue(firstAssigned);
            Assert.AreEqual(1, firstPair.Key);
            Assert.AreEqual("one", firstPair.Value);

            copy.Add(3, "three");
            Assert.IsFalse(dictionary.ContainsKey(3));

            dictionary[2] = "two-updated";
            Assert.AreEqual("two", copy[2]);
        }

        [Test]
        public void TryAddWhenKeyExistsDoesNotInvalidateCache()
        {
            SerializableSortedDictionary<int, string> dictionary = new() { { 5, "five" } };

            dictionary.OnBeforeSerialize();

            int[] serializedKeys = dictionary.SerializedKeys;
            string[] serializedValues = dictionary.SerializedValues;

            Assert.IsNotNull(serializedKeys, "Serialized keys should be generated.");
            Assert.IsNotNull(serializedValues, "Serialized values should be generated.");
            Assert.IsFalse(dictionary.SerializationArraysDirty);

            bool added = dictionary.TryAdd(5, "duplicate");

            Assert.IsFalse(added);
            Assert.AreSame(
                serializedKeys,
                dictionary.SerializedKeys,
                "Failed TryAdd must not clear the cached keys."
            );
            Assert.AreSame(
                serializedValues,
                dictionary.SerializedValues,
                "Failed TryAdd must not clear the cached values."
            );
            Assert.IsFalse(
                dictionary.SerializationArraysDirty,
                "Failed TryAdd must not mark arrays dirty."
            );
            Assert.AreEqual("five", dictionary[5]);
        }

        [Test]
        public void IndexerUpdateClearsSerializationArrays()
        {
            SerializableSortedDictionary<int, string> dictionary = new() { { 7, "seven" } };

            dictionary.OnBeforeSerialize();

            Assert.IsNotNull(dictionary.SerializedKeys);
            Assert.IsNotNull(dictionary.SerializedValues);
            Assert.IsFalse(dictionary.SerializationArraysDirty);

            dictionary[7] = "updated";

            Assert.IsNull(dictionary.SerializedKeys, "Indexer mutations must clear cached keys.");
            Assert.IsNull(
                dictionary.SerializedValues,
                "Indexer mutations must clear cached values."
            );
            Assert.IsTrue(
                dictionary.SerializationArraysDirty,
                "Indexer mutations must mark arrays dirty."
            );
            Assert.AreEqual("updated", dictionary[7]);
        }

        [Test]
        public void OnBeforeSerializeSkipsRebuildWhenCacheFresh()
        {
            SerializableSortedDictionary<int, string> dictionary = new()
            {
                { 2, "two" },
                { 4, "four" },
            };

            dictionary.OnBeforeSerialize();

            int[] initialKeys = dictionary.SerializedKeys;
            string[] initialValues = dictionary.SerializedValues;

            Assert.IsNotNull(initialKeys);
            Assert.IsNotNull(initialValues);
            Assert.IsFalse(dictionary.SerializationArraysDirty);

            dictionary.OnBeforeSerialize();

            Assert.AreSame(initialKeys, dictionary.SerializedKeys);
            Assert.AreSame(initialValues, dictionary.SerializedValues);
            Assert.IsFalse(dictionary.SerializationArraysDirty);
        }

        [Test]
        public void CopyFromNullThrowsArgumentNullException()
        {
            SerializableSortedDictionary<int, string> dictionary = new();

            Assert.Throws<ArgumentNullException>(() => dictionary.CopyFrom(null));
        }

        [Test]
        public void ProtoSerializationPreservesTemporaryArraysWhenNoDuplicatesExist()
        {
            SerializableSortedDictionary<int, string> dictionary = new()
            {
                { 1, "one" },
                { 3, "three" },
            };

            dictionary.OnBeforeSerialize();

            int[] serializedKeysBefore = dictionary.SerializedKeys;
            string[] serializedValuesBefore = dictionary.SerializedValues;
            Assert.IsNotNull(serializedKeysBefore);
            Assert.IsNotNull(serializedValuesBefore);

            byte[] payload = Serializer.ProtoSerialize(dictionary);

            int[] serializedKeysAfter = dictionary.SerializedKeys;
            string[] serializedValuesAfter = dictionary.SerializedValues;

            Assert.AreSame(
                serializedKeysBefore,
                serializedKeysAfter,
                "Proto serialization should not replace the serialized keys array."
            );
            Assert.AreSame(
                serializedValuesBefore,
                serializedValuesAfter,
                "Proto serialization should not replace the serialized values array."
            );
            Assert.AreEqual(2, serializedKeysAfter.Length);
            Assert.AreEqual(2, serializedValuesAfter.Length);
            Assert.AreEqual("one", dictionary[1]);
            Assert.AreEqual("three", dictionary[3]);

            SerializableSortedDictionary<int, string> roundTripped = Serializer.ProtoDeserialize<
                SerializableSortedDictionary<int, string>
            >(payload);

            int[] expectedKeys = { 1, 3 };
            int index = 0;
            foreach (KeyValuePair<int, string> pair in roundTripped)
            {
                Assert.Less(index, expectedKeys.Length);
                Assert.AreEqual(expectedKeys[index], pair.Key);
                index++;
            }

            Assert.AreEqual(expectedKeys.Length, index);
        }

        [Test]
        public void OnBeforeSerializeProducesSortedArrays()
        {
            SerializableSortedDictionary<int, string> dictionary = new()
            {
                { 10, "ten" },
                { 2, "two" },
                { 7, "seven" },
            };

            dictionary.OnBeforeSerialize();

            int[] serializedKeys = dictionary.SerializedKeys;
            string[] serializedValues = dictionary.SerializedValues;

            int[] expectedKeys = { 2, 7, 10 };
            string[] expectedValues = { "two", "seven", "ten" };

            CollectionAssert.AreEqual(expectedKeys, serializedKeys);
            CollectionAssert.AreEqual(expectedValues, serializedValues);
        }

        [Test]
        public void OnAfterDeserializeSortsEntries()
        {
            SerializableSortedDictionary<int, string> dictionary = new();

            int[] unsortedKeys = { 5, 1, 3 };
            string[] unsortedValues = { "five", "one", "three" };
            dictionary._keys = unsortedKeys;
            dictionary._values = unsortedValues;

            dictionary.OnAfterDeserialize();

            int[] expectedKeys = { 1, 3, 5 };
            string[] expectedValues = { "one", "three", "five" };

            int index = 0;
            foreach (KeyValuePair<int, string> pair in dictionary)
            {
                Assert.Less(index, expectedKeys.Length);
                Assert.AreEqual(expectedKeys[index], pair.Key);
                Assert.AreEqual(expectedValues[index], pair.Value);
                index++;
            }

            Assert.AreEqual(expectedKeys.Length, index);
        }

        [Test]
        public void NullKeysAreSkippedDuringDeserialization()
        {
            SerializableSortedDictionary<string, string> dictionary = new();

            string[] serializedKeys = { null, "valid" };
            string[] serializedValues = { "ignored", "retained" };
            dictionary._keys = serializedKeys;
            dictionary._values = serializedValues;

            LogAssert.Expect(
                LogType.Error,
                "SerializableSortedDictionary<System.String, System.String> skipped serialized entry at index 0 because the key reference was null."
            );

            dictionary.OnAfterDeserialize();

            Assert.AreEqual(1, dictionary.Count);
            Assert.IsTrue(dictionary.ContainsKey("valid"));
            Assert.AreEqual("retained", dictionary["valid"]);

            string[] storedKeys = dictionary._keys;
            string[] storedValues = dictionary._values;
            bool preserveFlag = dictionary.PreserveSerializedEntries;

            Assert.IsNotNull(storedKeys);
            CollectionAssert.AreEqual(serializedKeys, storedKeys);
            if (storedValues != null)
            {
                CollectionAssert.AreEqual(serializedValues, storedValues);
            }

            Assert.IsTrue(preserveFlag);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void NullValuesArePreservedDuringDeserialization()
        {
            SerializableSortedDictionary<string, string> dictionary = new();

            string[] serializedKeys = { "skip", "keep" };
            string[] serializedValues = { null, "retained" };
            dictionary._keys = serializedKeys;
            dictionary._values = serializedValues;

            dictionary.OnAfterDeserialize();

            Assert.AreEqual(2, dictionary.Count);
            Assert.IsTrue(dictionary.ContainsKey("keep"));
            Assert.AreEqual("retained", dictionary["keep"]);
            Assert.IsTrue(dictionary.ContainsKey("skip"));
            Assert.IsNull(dictionary["skip"]);

            string[] storedKeys = dictionary._keys;
            string[] storedValues = dictionary._values;
            bool preserveFlag = dictionary.PreserveSerializedEntries;

            Assert.IsNull(storedKeys);
            Assert.IsNull(storedValues);
            Assert.IsFalse(preserveFlag);
        }

        [Test]
        public void DuplicateSerializedKeysPreserveInspectorCache()
        {
            SerializableSortedDictionary<string, string> dictionary = new();

            string[] serializedKeys = { "dup", "dup" };
            string[] serializedValues = { "first", "second" };
            dictionary._keys = serializedKeys;
            dictionary._values = serializedValues;

            dictionary.OnAfterDeserialize();

            Assert.AreEqual(1, dictionary.Count);
            Assert.AreEqual("second", dictionary["dup"]);

            Assert.AreSame(serializedKeys, dictionary.SerializedKeys);
            Assert.AreSame(serializedValues, dictionary.SerializedValues);
            CollectionAssert.AreEqual(new[] { "dup", "dup" }, dictionary.SerializedKeys);
            CollectionAssert.AreEqual(new[] { "first", "second" }, dictionary.SerializedValues);
            Assert.IsTrue(
                dictionary.PreserveSerializedEntries,
                "Duplicate keys should preserve serialized arrays for inspector review."
            );
        }

        [Test]
        public void ComparerCollisionsPreserveSerializedCache()
        {
            SerializableSortedDictionary<CaseInsensitiveKey, string> dictionary = new();

            CaseInsensitiveKey loud = new("ALPHA");
            CaseInsensitiveKey quiet = new("alpha");
            dictionary._keys = new[] { loud, quiet };
            dictionary._values = new[] { "upper", "lower" };

            dictionary.OnAfterDeserialize();

            Assert.AreEqual(1, dictionary.Count);
            Assert.AreEqual("lower", dictionary[quiet]);

            CaseInsensitiveKey[] cachedKeys = dictionary.SerializedKeys;
            string[] cachedValues = dictionary.SerializedValues;

            Assert.IsNotNull(cachedKeys);
            Assert.IsNotNull(cachedValues);
            Assert.AreSame(dictionary._keys, cachedKeys);
            Assert.AreSame(dictionary._values, cachedValues);
            Assert.AreSame(loud, cachedKeys[0]);
            Assert.AreSame(quiet, cachedKeys[1]);
            CollectionAssert.AreEqual(new[] { "upper", "lower" }, cachedValues);
            Assert.IsTrue(dictionary.PreserveSerializedEntries);
        }

        [Test]
        public void UnitySerializationRebuildsSortedDictionaryCacheAfterDeserialization()
        {
            SerializableSortedDictionary<string, int> dictionary = new();

            string[] serializedKeys = { "delta", "alpha", "charlie" };
            int[] serializedValues = { 3, 1, 2 };
            dictionary._keys = serializedKeys;
            dictionary._values = serializedValues;

            dictionary.OnAfterDeserialize();

            Assert.IsNull(
                dictionary._keys,
                "Deserialization should drop serialized keys when entries can be restored without loss."
            );
            Assert.IsNull(
                dictionary._values,
                "Deserialization should drop serialized values when entries can be restored without loss."
            );

            dictionary.OnBeforeSerialize();

            string[] rebuiltKeys = dictionary._keys;
            int[] rebuiltValues = dictionary._values;

            Assert.IsNotNull(rebuiltKeys);
            Assert.IsNotNull(rebuiltValues);
            CollectionAssert.AreEqual(
                new[] { "alpha", "charlie", "delta" },
                rebuiltKeys,
                "Serialized keys should be rewritten in sorted order."
            );
            CollectionAssert.AreEqual(
                new[] { 1, 2, 3 },
                rebuiltValues,
                "Serialized values should stay aligned with sorted keys."
            );
        }

        [Test]
        public void JsonRoundTripClearsCacheAndRebuildsSortedSnapshot()
        {
            SerializableSortedDictionary<int, string> original = new()
            {
                { 3, "three" },
                { 1, "one" },
                { 2, "two" },
            };

            string json = Serializer.JsonStringify(original);
            SerializableSortedDictionary<int, string> roundTrip = Serializer.JsonDeserialize<
                SerializableSortedDictionary<int, string>
            >(json);

            Assert.AreEqual(3, roundTrip.Count);
            Assert.IsNull(roundTrip.SerializedKeys);
            Assert.IsNull(roundTrip.SerializedValues);
            Assert.IsFalse(roundTrip.PreserveSerializedEntries);

            roundTrip.OnBeforeSerialize();

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, roundTrip.SerializedKeys);
            CollectionAssert.AreEqual(new[] { "one", "two", "three" }, roundTrip.SerializedValues);
        }

        [Test]
        public void MismatchedSerializedArraysAreDiscardedAndMarkCacheDirty()
        {
            SerializableSortedDictionary<string, string> dictionary = new()
            {
                _keys = new[] { "alpha" },
                _values = new[] { "one", "extra" },
            };

            dictionary.OnAfterDeserialize();

            Assert.AreEqual(0, dictionary.Count);
            Assert.IsNull(dictionary.SerializedKeys);
            Assert.IsNull(dictionary.SerializedValues);
            Assert.IsTrue(
                dictionary.SerializationArraysDirty,
                "Cache should be marked dirty when serialized arrays are invalid."
            );
        }

        [Test]
        public void ProtoSerializationRoundTrips()
        {
            SerializableSortedDictionary<int, string> original = new()
            {
                { 4, "four" },
                { 2, "two" },
                { 9, "nine" },
            };

            byte[] data = Serializer.ProtoSerialize(original);
            SerializableSortedDictionary<int, string> deserialized = Serializer.ProtoDeserialize<
                SerializableSortedDictionary<int, string>
            >(data);

            int[] expectedKeys = { 2, 4, 9 };
            int index = 0;
            foreach (KeyValuePair<int, string> pair in deserialized)
            {
                Assert.Less(index, expectedKeys.Length);
                Assert.AreEqual(expectedKeys[index], pair.Key);
                index++;
            }

            Assert.AreEqual(expectedKeys.Length, index);
        }

        private sealed class CaseInsensitiveKey : IComparable<CaseInsensitiveKey>, IComparable
        {
            public CaseInsensitiveKey(string token)
            {
                Token = token;
            }

            public string Token { get; }

            public int CompareTo(CaseInsensitiveKey other)
            {
                return other == null
                    ? 1
                    : string.Compare(Token, other.Token, StringComparison.OrdinalIgnoreCase);
            }

            int IComparable.CompareTo(object obj)
            {
                if (ReferenceEquals(this, obj))
                {
                    return 0;
                }

                if (obj is CaseInsensitiveKey candidate)
                {
                    return CompareTo(candidate);
                }

                return 1;
            }

            public override bool Equals(object obj)
            {
                if (obj is CaseInsensitiveKey other)
                {
                    return string.Equals(Token, other.Token, StringComparison.OrdinalIgnoreCase);
                }

                return false;
            }

            public override int GetHashCode()
            {
                if (Token == null)
                {
                    return 0;
                }

                return StringComparer.OrdinalIgnoreCase.GetHashCode(Token);
            }

            public override string ToString()
            {
                return Token ?? string.Empty;
            }
        }
    }
}
