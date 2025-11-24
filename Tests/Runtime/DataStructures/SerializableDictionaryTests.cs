namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using Serializer = WallstopStudios.UnityHelpers.Core.Serialization.Serializer;

    public sealed class SerializableDictionaryTests
    {
        [Test]
        public void AddRetrieveAndEnumerateValues()
        {
            SerializableDictionary<string, int> dictionary = new() { { "alpha", 1 } };
            dictionary["beta"] = 2;

            Assert.AreEqual(2, dictionary.Count);
            Assert.IsTrue(dictionary.ContainsKey("alpha"));
            Assert.IsTrue(dictionary.ContainsKey("beta"));

            Assert.AreEqual(1, dictionary["alpha"]);
            Assert.AreEqual(2, dictionary["beta"]);

            Assert.IsTrue(dictionary.TryGetValue("alpha", out int retrievedValue));
            Assert.AreEqual(1, retrievedValue);

            int keyedCount = 0;
            foreach (KeyValuePair<string, int> pair in dictionary)
            {
                keyedCount++;
                Assert.IsTrue(dictionary.ContainsKey(pair.Key));
                Assert.AreEqual(dictionary[pair.Key], pair.Value);
            }

            Assert.AreEqual(dictionary.Count, keyedCount);
        }

        [Test]
        public void ToDictionaryReturnsIndependentCopy()
        {
            SerializableDictionary<string, int> dictionary = new()
            {
                { "alpha", 1 },
                { "beta", 2 },
            };

            Dictionary<string, int> copy = dictionary.ToDictionary();

            Assert.AreEqual(dictionary.Count, copy.Count);
            Assert.AreEqual(1, copy["alpha"]);
            Assert.AreEqual(2, copy["beta"]);

            copy["gamma"] = 3;
            Assert.IsFalse(dictionary.ContainsKey("gamma"));

            dictionary["alpha"] = 10;
            Assert.AreEqual(1, copy["alpha"]);
        }

        [Test]
        public void CopyFromReplacesExistingContent()
        {
            SerializableDictionary<int, string> dictionary = new() { { 1, "one" } };

            Dictionary<int, string> source = new() { [2] = "two", [3] = "three" };

            dictionary.CopyFrom(source);

            Assert.AreEqual(2, dictionary.Count);
            Assert.IsFalse(dictionary.ContainsKey(1));
            Assert.AreEqual("two", dictionary[2]);
            Assert.AreEqual("three", dictionary[3]);
        }

        [Test]
        public void TryAddWhenKeyExistsDoesNotClearSerializedArrays()
        {
            SerializableDictionary<int, string> dictionary = new() { { 1, "one" } };

            dictionary.OnBeforeSerialize();

            int[] serializedKeys = dictionary.SerializedKeys;
            string[] serializedValues = dictionary.SerializedValues;

            Assert.IsNotNull(serializedKeys, "Serialized keys should be produced on demand.");
            Assert.IsNotNull(serializedValues, "Serialized values should be produced on demand.");

            bool added = dictionary.TryAdd(1, "duplicate");

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
            Assert.AreEqual("one", dictionary[1]);
        }

        [Test]
        public void IndexerUpdateClearsSerializedArrays()
        {
            SerializableDictionary<int, string> dictionary = new() { { 1, "one" }, { 2, "two" } };

            dictionary.OnBeforeSerialize();

            Assert.IsNotNull(dictionary.SerializedKeys, "Keys cache should exist before update.");
            Assert.IsNotNull(
                dictionary.SerializedValues,
                "Values cache should exist before update."
            );

            dictionary[2] = "second";

            Assert.IsNull(dictionary.SerializedKeys, "Indexer mutations must clear cached keys.");
            Assert.IsNull(
                dictionary.SerializedValues,
                "Indexer mutations must clear cached values."
            );
            Assert.AreEqual("second", dictionary[2]);
        }

        [Test]
        public void RemoveOutputsPreviousValueAndClearsSerializedArrays()
        {
            SerializableDictionary<int, string> dictionary = new() { { 1, "one" }, { 2, "two" } };

            dictionary.OnBeforeSerialize();

            Assert.IsNotNull(dictionary.SerializedKeys);
            Assert.IsNotNull(dictionary.SerializedValues);

            bool removed = dictionary.Remove(2, out string removedValue);

            Assert.IsTrue(removed);
            Assert.AreEqual("two", removedValue);
            Assert.IsNull(dictionary.SerializedKeys, "Removal should clear cached keys.");
            Assert.IsNull(dictionary.SerializedValues, "Removal should clear cached values.");
            Assert.IsFalse(dictionary.ContainsKey(2));
        }

        [Test]
        public void ProtoSerializationPreservesSerializationArraysWhenNoDuplicatesExist()
        {
            SerializableDictionary<int, string> dictionary = new() { { 1, "one" }, { 3, "three" } };

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

            SerializableDictionary<int, string> roundTripped = Serializer.ProtoDeserialize<
                SerializableDictionary<int, string>
            >(payload);

            Assert.AreEqual(2, roundTripped.Count);
            Assert.AreEqual("one", roundTripped[1]);
            Assert.AreEqual("three", roundTripped[3]);
        }

        [Test]
        public void SerializationCallbacksRoundTripEntries()
        {
            SerializableDictionary<int, string> original = new()
            {
                { 10, "ten" },
                { 20, "twenty" },
            };

            original.OnBeforeSerialize();

            int[] serializedKeys = original.SerializedKeys;
            string[] serializedValues = original.SerializedValues;

            Assert.IsNotNull(serializedKeys);
            Assert.IsNotNull(serializedValues);
            Assert.AreEqual(original.Count, serializedKeys.Length);
            Assert.AreEqual(serializedKeys.Length, serializedValues.Length);

            int[] keysCopy = new int[serializedKeys.Length];
            Array.Copy(serializedKeys, keysCopy, serializedKeys.Length);

            string[] valuesCopy = new string[serializedValues.Length];
            Array.Copy(serializedValues, valuesCopy, serializedValues.Length);

            SerializableDictionary<int, string> roundTripped = new()
            {
                _keys = keysCopy,
                _values = valuesCopy,
            };

            roundTripped.OnAfterDeserialize();

            Assert.AreEqual(original.Count, roundTripped.Count);
            Assert.AreEqual("ten", roundTripped[10]);
            Assert.AreEqual("twenty", roundTripped[20]);
            Assert.IsTrue(roundTripped.TryGetValue(10, out string tenValue));
            Assert.AreEqual("ten", tenValue);

            roundTripped.Clear();
            Assert.AreEqual(0, roundTripped.Count);
        }

        [Test]
        public void CacheBackedDictionarySerializesValues()
        {
            SerializableDictionary<int, int, IntCache> original = new() { { 2, 200 }, { 5, 500 } };

            original.OnBeforeSerialize();

            int[] serializedKeys = original.SerializedKeys;
            IntCache[] serializedValues = original.SerializedValues;

            Assert.IsNotNull(serializedKeys);
            Assert.IsNotNull(serializedValues);
            Assert.AreEqual(original.Count, serializedKeys.Length);
            Assert.AreEqual(serializedKeys.Length, serializedValues.Length);

            SerializableDictionary<int, int, IntCache> roundTripped = new();

            int[] keysCopy = new int[serializedKeys.Length];
            Array.Copy(serializedKeys, keysCopy, serializedKeys.Length);

            IntCache[] valuesCopy = new IntCache[serializedValues.Length];
            for (int index = 0; index < serializedValues.Length; index++)
            {
                IntCache cache = new() { Data = serializedValues[index].Data };
                valuesCopy[index] = cache;
            }

            roundTripped._keys = keysCopy;
            roundTripped._values = valuesCopy;

            roundTripped.OnAfterDeserialize();

            Assert.AreEqual(original.Count, roundTripped.Count);
            Assert.AreEqual(200, roundTripped[2]);
            Assert.AreEqual(500, roundTripped[5]);
        }

        [Test]
        public void EnumeratorIsValueType()
        {
            SerializableDictionary<int, string> dictionary = new() { { 1, "one" } };
            using SerializableDictionaryBase<int, string, string>.Enumerator enumerator =
                dictionary.GetEnumerator();

            Assert.IsTrue(
                typeof(SerializableDictionaryBase<int, string, string>.Enumerator).IsValueType
            );
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(new KeyValuePair<int, string>(1, "one"), enumerator.Current);
        }

        [Test]
        public void DuplicateKeysRemainSerializedForInspector()
        {
            SerializableDictionary<int, string> dictionary = new();

            int[] serializedKeys = { 1, 1 };
            string[] serializedValues = { "first", "second" };
            dictionary._keys = serializedKeys;
            dictionary._values = serializedValues;

            dictionary.OnAfterDeserialize();

            Assert.AreEqual(1, dictionary.Count);
            Assert.AreEqual("second", dictionary[1]);

            int[] storedKeys = dictionary.SerializedKeys;
            string[] storedValues = dictionary.SerializedValues;
            bool preserveFlag = dictionary.PreserveSerializedEntries;

            Assert.IsNotNull(storedKeys, "Serialized keys were unexpectedly cleared.");
            Assert.IsNotNull(storedValues, "Serialized values were unexpectedly cleared.");
            Assert.AreEqual(2, storedKeys.Length);
            Assert.AreEqual(2, storedValues.Length);
            Assert.IsTrue(preserveFlag, "Preserve flag should remain true while duplicates exist.");

            dictionary.OnBeforeSerialize();

            int[] roundTripKeys = dictionary.SerializedKeys;
            string[] roundTripValues = dictionary.SerializedValues;
            bool roundTripPreserve = dictionary.PreserveSerializedEntries;

            Assert.IsNotNull(roundTripKeys, "Round-trip keys should stay populated.");
            Assert.IsNotNull(roundTripValues, "Round-trip values should stay populated.");
            Assert.AreEqual(2, roundTripKeys.Length);
            Assert.AreEqual(2, roundTripValues.Length);
            Assert.AreEqual("first", roundTripValues[0]);
            Assert.AreEqual("second", roundTripValues[1]);
            Assert.IsTrue(
                roundTripPreserve,
                "Preserve flag should remain true after serialization skip."
            );
        }

        [Test]
        public void NullKeysAreSkippedDuringDeserialization()
        {
            SerializableDictionary<string, string> dictionary = new();

            string[] serializedKeys = { null, "valid" };
            string[] serializedValues = { "ignored", "retained" };
            dictionary._keys = serializedKeys;
            dictionary._values = serializedValues;

            LogAssert.Expect(
                LogType.Error,
                "SerializableDictionary<System.String, System.String> skipped serialized entry at index 0 because the key reference was null."
            );

            dictionary.OnAfterDeserialize();

            Assert.AreEqual(1, dictionary.Count);
            Assert.IsTrue(dictionary.ContainsKey("valid"));
            Assert.AreEqual("retained", dictionary["valid"]);

            string[] storedKeys = dictionary._keys;
            string[] storedValues = dictionary._values;
            bool preserveFlag = dictionary.PreserveSerializedEntries;

            Assert.IsNotNull(
                storedKeys,
                "Serialized keys should be preserved when null keys exist."
            );
            CollectionAssert.AreEqual(serializedKeys, storedKeys);
            if (storedValues != null)
            {
                CollectionAssert.AreEqual(serializedValues, storedValues);
            }
            Assert.IsTrue(preserveFlag, "Null keys should force serialized cache preservation.");

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void NullValuesArePreservedDuringDeserialization()
        {
            SerializableDictionary<string, string> dictionary = new();

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

            Assert.IsNull(
                storedKeys,
                "Serialized keys should be cleared when only values are null."
            );
            Assert.IsNull(
                storedValues,
                "Serialized values should be cleared when only values are null."
            );
            Assert.IsFalse(
                preserveFlag,
                "Null values should not force serialized cache preservation."
            );

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void DuplicateSerializedKeysPreserveInspectorCache()
        {
            SerializableDictionary<string, string> dictionary = new();

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
        public void UnityObjectKeysWithNullEntriesPreserveSerializedArrays()
        {
            SerializableDictionary<DummyAsset, string> dictionary = new();

            DummyAsset valid = ScriptableObject.CreateInstance<DummyAsset>();
            DummyAsset[] serializedKeys = { null, valid };
            string[] serializedValues = { "omit", "keep" };
            dictionary._keys = serializedKeys;
            dictionary._values = serializedValues;

            string expectedMessage =
                $"SerializableDictionary<{typeof(DummyAsset).FullName}, {typeof(string).FullName}> skipped serialized entry at index 0 because the key reference was null.";
            LogAssert.Expect(LogType.Error, expectedMessage);

            dictionary.OnAfterDeserialize();

            Assert.AreEqual(1, dictionary.Count);
            Assert.IsTrue(dictionary.ContainsKey(valid));
            Assert.AreEqual("keep", dictionary[valid]);
            Assert.AreSame(serializedKeys, dictionary.SerializedKeys);
            Assert.AreSame(serializedValues, dictionary.SerializedValues);
            Assert.IsTrue(dictionary.PreserveSerializedEntries);

            ScriptableObject.DestroyImmediate(valid);
        }

        [Test]
        public void ComparerCollisionsPreserveSerializedCache()
        {
            SerializableDictionary<CaseInsensitiveDictionaryKey, string> dictionary = new();

            CaseInsensitiveDictionaryKey loud = new("ALPHA");
            CaseInsensitiveDictionaryKey quiet = new("alpha");
            dictionary._keys = new[] { loud, quiet };
            dictionary._values = new[] { "upper", "lower" };

            dictionary.OnAfterDeserialize();

            Assert.AreEqual(1, dictionary.Count);
            Assert.AreEqual("lower", dictionary[quiet]);

            CaseInsensitiveDictionaryKey[] cachedKeys = dictionary.SerializedKeys;
            string[] cachedValues = dictionary.SerializedValues;

            Assert.IsNotNull(cachedKeys);
            Assert.IsNotNull(cachedValues);
            Assert.AreSame(loud, cachedKeys[0]);
            Assert.AreSame(quiet, cachedKeys[1]);
            CollectionAssert.AreEqual(new[] { "upper", "lower" }, cachedValues);
            Assert.IsTrue(dictionary.PreserveSerializedEntries);
        }

        [Test]
        public void JsonRoundTripClearsCacheAndRebuildsSnapshot()
        {
            SerializableDictionary<int, string> original = new()
            {
                { 3, "three" },
                { 1, "one" },
                { 2, "two" },
            };

            string json = Serializer.JsonStringify(original);
            SerializableDictionary<int, string> roundTrip = Serializer.JsonDeserialize<
                SerializableDictionary<int, string>
            >(json);

            Assert.AreEqual(original.Count, roundTrip.Count);
            Assert.IsNull(roundTrip.SerializedKeys);
            Assert.IsNull(roundTrip.SerializedValues);
            Assert.IsFalse(roundTrip.PreserveSerializedEntries);

            roundTrip.OnBeforeSerialize();

            int[] rebuiltKeys = roundTrip.SerializedKeys;
            string[] rebuiltValues = roundTrip.SerializedValues;

            Assert.IsNotNull(rebuiltKeys);
            Assert.IsNotNull(rebuiltValues);
            Dictionary<int, string> snapshot = new();
            for (int index = 0; index < rebuiltKeys.Length; index++)
            {
                snapshot[rebuiltKeys[index]] = rebuiltValues[index];
            }

            foreach (KeyValuePair<int, string> pair in original)
            {
                Assert.IsTrue(snapshot.ContainsKey(pair.Key));
                Assert.AreEqual(pair.Value, snapshot[pair.Key]);
            }
        }

        [Test]
        public void MismatchedSerializedArraysAreDiscarded()
        {
            SerializableDictionary<int, string> dictionary = new()
            {
                _keys = new[] { 1, 2 },
                _values = new[] { "one" },
            };

            dictionary.OnAfterDeserialize();

            Assert.AreEqual(0, dictionary.Count);
            Assert.IsNull(dictionary.SerializedKeys);
            Assert.IsNull(dictionary.SerializedValues);
            Assert.IsFalse(dictionary.PreserveSerializedEntries);
        }

        [Test]
        public void EditorAfterDeserializeSuppressesWarnings()
        {
            SerializableDictionary<string, string> dictionary = new();

            string[] keys = { null, "valid" };
            string[] values = { "ignored", "retained" };
            dictionary._keys = keys;
            dictionary._values = values;

            SerializableDictionaryBase editorSync = dictionary;
            editorSync.EditorAfterDeserialize();

            Assert.AreEqual(1, dictionary.Count);
            Assert.IsTrue(dictionary.ContainsKey("valid"));
            Assert.AreEqual("retained", dictionary["valid"]);

            string[] storedKeys = dictionary.SerializedKeys;
            string[] storedValues = dictionary.SerializedValues;
            CollectionAssert.AreEqual(keys, storedKeys);
            CollectionAssert.AreEqual(values, storedValues);

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnBeforeSerializeSkipsRebuildWhenPreservingSerializedEntries()
        {
            SerializableDictionary<string, string> dictionary = new();
            string[] serializedKeys = { "dup", "dup" };
            string[] serializedValues = { "first", "second" };
            dictionary._keys = serializedKeys;
            dictionary._values = serializedValues;

            dictionary.OnAfterDeserialize();
            Assert.IsTrue(dictionary.PreserveSerializedEntries);

            dictionary.OnBeforeSerialize();

            Assert.AreSame(serializedKeys, dictionary.SerializedKeys);
            Assert.AreSame(serializedValues, dictionary.SerializedValues);
            Assert.IsTrue(dictionary.PreserveSerializedEntries);
        }

        [Test]
        public void DictionaryMutationsClearPreservedSerializedEntries()
        {
            SerializableDictionary<int, string> dictionary = new();

            int[] serializedKeys = { 3, 3 };
            string[] serializedValues = { "old", "new" };
            dictionary._keys = serializedKeys;
            dictionary._values = serializedValues;

            dictionary.OnAfterDeserialize();
            Assert.IsTrue(dictionary.PreserveSerializedEntries);

            dictionary.Add(4, "fresh");

            bool preserveAfterAdd = dictionary.PreserveSerializedEntries;
            int[] storedKeysAfterAdd = dictionary.SerializedKeys;
            string[] storedValuesAfterAdd = dictionary.SerializedValues;

            Assert.IsFalse(
                preserveAfterAdd,
                "Preserve flag should clear after dictionary mutation."
            );
            Assert.IsNull(
                storedKeysAfterAdd,
                "Serialized keys should reset after dictionary mutation."
            );
            Assert.IsNull(
                storedValuesAfterAdd,
                "Serialized values should reset after dictionary mutation."
            );
        }

        [Serializable]
        private sealed class IntCache : SerializableDictionary.Cache<int> { }

        private sealed class DummyAsset : ScriptableObject { }

        private sealed class CaseInsensitiveDictionaryKey : IEquatable<CaseInsensitiveDictionaryKey>
        {
            public CaseInsensitiveDictionaryKey(string token)
            {
                Token = token;
            }

            public string Token { get; }

            public bool Equals(CaseInsensitiveDictionaryKey other)
            {
                if (other == null)
                {
                    return false;
                }

                return string.Equals(Token, other.Token, StringComparison.OrdinalIgnoreCase);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as CaseInsensitiveDictionaryKey);
            }

            public override int GetHashCode()
            {
                if (Token == null)
                {
                    return 0;
                }

                return StringComparer.OrdinalIgnoreCase.GetHashCode(Token);
            }
        }
    }
}
