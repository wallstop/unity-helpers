namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.DataStructure;
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

            Type baseType = typeof(SerializableDictionary<int, string>).BaseType;
            Assert.IsNotNull(baseType, "Base type lookup failed.");

            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo keysField = baseType.GetField("_keys", flags);
            FieldInfo valuesField = baseType.GetField("_values", flags);
            Assert.IsNotNull(keysField, "Serialized keys field was not found.");
            Assert.IsNotNull(valuesField, "Serialized values field was not found.");

            dictionary.OnBeforeSerialize();

            object serializedKeys = keysField.GetValue(dictionary);
            object serializedValues = valuesField.GetValue(dictionary);

            Assert.IsNotNull(serializedKeys, "Serialized keys should be produced on demand.");
            Assert.IsNotNull(serializedValues, "Serialized values should be produced on demand.");

            bool added = dictionary.TryAdd(1, "duplicate");

            Assert.IsFalse(added);
            Assert.AreSame(
                serializedKeys,
                keysField.GetValue(dictionary),
                "Failed TryAdd must not clear the cached keys."
            );
            Assert.AreSame(
                serializedValues,
                valuesField.GetValue(dictionary),
                "Failed TryAdd must not clear the cached values."
            );
            Assert.AreEqual("one", dictionary[1]);
        }

        [Test]
        public void IndexerUpdateClearsSerializedArrays()
        {
            SerializableDictionary<int, string> dictionary = new() { { 1, "one" }, { 2, "two" } };

            Type baseType = typeof(SerializableDictionary<int, string>).BaseType;
            Assert.IsNotNull(baseType, "Base type lookup failed.");

            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo keysField = baseType.GetField("_keys", flags);
            FieldInfo valuesField = baseType.GetField("_values", flags);
            Assert.IsNotNull(keysField, "Serialized keys field was not found.");
            Assert.IsNotNull(valuesField, "Serialized values field was not found.");

            dictionary.OnBeforeSerialize();

            Assert.IsNotNull(
                keysField.GetValue(dictionary),
                "Keys cache should exist before update."
            );
            Assert.IsNotNull(
                valuesField.GetValue(dictionary),
                "Values cache should exist before update."
            );

            dictionary[2] = "second";

            Assert.IsNull(
                keysField.GetValue(dictionary),
                "Indexer mutations must clear cached keys."
            );
            Assert.IsNull(
                valuesField.GetValue(dictionary),
                "Indexer mutations must clear cached values."
            );
            Assert.AreEqual("second", dictionary[2]);
        }

        [Test]
        public void RemoveOutputsPreviousValueAndClearsSerializedArrays()
        {
            SerializableDictionary<int, string> dictionary = new() { { 1, "one" }, { 2, "two" } };

            Type baseType = typeof(SerializableDictionary<int, string>).BaseType;
            Assert.IsNotNull(baseType, "Base type lookup failed.");

            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo keysField = baseType.GetField("_keys", flags);
            FieldInfo valuesField = baseType.GetField("_values", flags);
            Assert.IsNotNull(keysField, "Serialized keys field was not found.");
            Assert.IsNotNull(valuesField, "Serialized values field was not found.");

            dictionary.OnBeforeSerialize();

            Assert.IsNotNull(keysField.GetValue(dictionary));
            Assert.IsNotNull(valuesField.GetValue(dictionary));

            bool removed = dictionary.Remove(2, out string removedValue);

            Assert.IsTrue(removed);
            Assert.AreEqual("two", removedValue);
            Assert.IsNull(keysField.GetValue(dictionary), "Removal should clear cached keys.");
            Assert.IsNull(valuesField.GetValue(dictionary), "Removal should clear cached values.");
            Assert.IsFalse(dictionary.ContainsKey(2));
        }

        [Test]
        public void ProtoSerializationPreservesSerializationArraysWhenNoDuplicatesExist()
        {
            SerializableDictionary<int, string> dictionary = new() { { 1, "one" }, { 3, "three" } };

            Type baseType = typeof(SerializableDictionary<int, string>).BaseType;
            Assert.IsNotNull(baseType, "Base type lookup failed.");

            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo keysField = baseType.GetField("_keys", flags);
            FieldInfo valuesField = baseType.GetField("_values", flags);
            Assert.IsNotNull(keysField, "Serialized keys field was not found.");
            Assert.IsNotNull(valuesField, "Serialized values field was not found.");

            dictionary.OnBeforeSerialize();

            int[] serializedKeysBefore = (int[])keysField.GetValue(dictionary);
            string[] serializedValuesBefore = (string[])valuesField.GetValue(dictionary);
            Assert.IsNotNull(serializedKeysBefore);
            Assert.IsNotNull(serializedValuesBefore);

            byte[] payload = Serializer.ProtoSerialize(dictionary);

            int[] serializedKeysAfter = (int[])keysField.GetValue(dictionary);
            string[] serializedValuesAfter = (string[])valuesField.GetValue(dictionary);

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

            Type baseType = typeof(SerializableDictionary<int, string>).BaseType;
            Assert.IsNotNull(baseType, "SerializableDictionary base type was not found.");

            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo keysField = baseType.GetField("_keys", flags);
            FieldInfo valuesField = baseType.GetField("_values", flags);
            Assert.IsNotNull(keysField, "Serialized keys field was not found.");
            Assert.IsNotNull(valuesField, "Serialized values field was not found.");

            int[] serializedKeys = (int[])keysField.GetValue(original);
            string[] serializedValues = (string[])valuesField.GetValue(original);

            Assert.IsNotNull(serializedKeys);
            Assert.IsNotNull(serializedValues);
            Assert.AreEqual(original.Count, serializedKeys.Length);
            Assert.AreEqual(serializedKeys.Length, serializedValues.Length);

            int[] keysCopy = new int[serializedKeys.Length];
            Array.Copy(serializedKeys, keysCopy, serializedKeys.Length);

            string[] valuesCopy = new string[serializedValues.Length];
            Array.Copy(serializedValues, valuesCopy, serializedValues.Length);

            SerializableDictionary<int, string> roundTripped = new();
            keysField.SetValue(roundTripped, keysCopy);
            valuesField.SetValue(roundTripped, valuesCopy);

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

            Type baseType = typeof(SerializableDictionary<int, int, IntCache>).BaseType;
            Assert.IsNotNull(baseType, "Cache-backed dictionary base type was not found.");

            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo keysField = baseType.GetField("_keys", flags);
            FieldInfo valuesField = baseType.GetField("_values", flags);
            Assert.IsNotNull(keysField, "Cache-backed serialized keys field was not found.");
            Assert.IsNotNull(valuesField, "Cache-backed serialized values field was not found.");

            int[] serializedKeys = (int[])keysField.GetValue(original);
            IntCache[] serializedValues = (IntCache[])valuesField.GetValue(original);

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

            keysField.SetValue(roundTripped, keysCopy);
            valuesField.SetValue(roundTripped, valuesCopy);

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

            Type baseType = typeof(SerializableDictionary<int, string>).BaseType;
            Assert.IsNotNull(baseType, "Base type lookup failed.");

            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo keysField = baseType.GetField("_keys", flags);
            FieldInfo valuesField = baseType.GetField("_values", flags);
            FieldInfo preserveField = baseType.GetField("_preserveSerializedEntries", flags);

            Assert.IsNotNull(keysField, "Keys backing field lookup failed.");
            Assert.IsNotNull(valuesField, "Values backing field lookup failed.");
            Assert.IsNotNull(preserveField, "Preserve flag field lookup failed.");

            int[] serializedKeys = new int[] { 1, 1 };
            string[] serializedValues = new string[] { "first", "second" };
            keysField.SetValue(dictionary, serializedKeys);
            valuesField.SetValue(dictionary, serializedValues);

            dictionary.OnAfterDeserialize();

            Assert.AreEqual(1, dictionary.Count);
            Assert.AreEqual("second", dictionary[1]);

            int[] storedKeys = (int[])keysField.GetValue(dictionary);
            string[] storedValues = (string[])valuesField.GetValue(dictionary);
            bool preserveFlag = (bool)preserveField.GetValue(dictionary);

            Assert.IsNotNull(storedKeys, "Serialized keys were unexpectedly cleared.");
            Assert.IsNotNull(storedValues, "Serialized values were unexpectedly cleared.");
            Assert.AreEqual(2, storedKeys.Length);
            Assert.AreEqual(2, storedValues.Length);
            Assert.IsTrue(preserveFlag, "Preserve flag should remain true while duplicates exist.");

            dictionary.OnBeforeSerialize();

            int[] roundTripKeys = (int[])keysField.GetValue(dictionary);
            string[] roundTripValues = (string[])valuesField.GetValue(dictionary);
            bool roundTripPreserve = (bool)preserveField.GetValue(dictionary);

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

            Type baseType = typeof(SerializableDictionary<string, string>).BaseType;
            Assert.IsNotNull(baseType, "Base type lookup failed.");

            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo keysField = baseType.GetField("_keys", flags);
            FieldInfo valuesField = baseType.GetField("_values", flags);
            FieldInfo preserveField = baseType.GetField("_preserveSerializedEntries", flags);

            Assert.IsNotNull(keysField, "Keys backing field lookup failed.");
            Assert.IsNotNull(valuesField, "Values backing field lookup failed.");
            Assert.IsNotNull(preserveField, "Preserve flag field lookup failed.");

            string[] serializedKeys = new string[] { null, "valid" };
            string[] serializedValues = new string[] { "ignored", "retained" };
            keysField.SetValue(dictionary, serializedKeys);
            valuesField.SetValue(dictionary, serializedValues);

            LogAssert.Expect(
                LogType.Error,
                new Regex("index 0.+key reference was null", RegexOptions.IgnoreCase)
            );

            dictionary.OnAfterDeserialize();

            Assert.AreEqual(1, dictionary.Count);
            Assert.IsTrue(dictionary.ContainsKey("valid"));
            Assert.AreEqual("retained", dictionary["valid"]);

            string[] storedKeys = (string[])keysField.GetValue(dictionary);
            string[] storedValues = (string[])valuesField.GetValue(dictionary);
            bool preserveFlag = (bool)preserveField.GetValue(dictionary);

            Assert.IsNotNull(
                storedKeys,
                "Serialized keys should be preserved when null keys exist."
            );
            Assert.IsNotNull(
                storedValues,
                "Serialized values should be preserved when null keys exist."
            );
            CollectionAssert.AreEqual(serializedKeys, storedKeys);
            CollectionAssert.AreEqual(serializedValues, storedValues);
            Assert.IsTrue(preserveFlag, "Null keys should force serialized cache preservation.");

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void NullValuesAreSkippedDuringDeserialization()
        {
            SerializableDictionary<string, string> dictionary = new();

            Type baseType = typeof(SerializableDictionary<string, string>).BaseType;
            Assert.IsNotNull(baseType, "Base type lookup failed.");

            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo keysField = baseType.GetField("_keys", flags);
            FieldInfo valuesField = baseType.GetField("_values", flags);
            FieldInfo preserveField = baseType.GetField("_preserveSerializedEntries", flags);

            Assert.IsNotNull(keysField, "Keys backing field lookup failed.");
            Assert.IsNotNull(valuesField, "Values backing field lookup failed.");
            Assert.IsNotNull(preserveField, "Preserve flag field lookup failed.");

            string[] serializedKeys = new string[] { "skip", "keep" };
            string[] serializedValues = new string[] { null, "retained" };
            keysField.SetValue(dictionary, serializedKeys);
            valuesField.SetValue(dictionary, serializedValues);

            LogAssert.Expect(
                LogType.Error,
                new Regex("index 0.+value reference was null", RegexOptions.IgnoreCase)
            );

            dictionary.OnAfterDeserialize();

            Assert.AreEqual(1, dictionary.Count);
            Assert.IsTrue(dictionary.ContainsKey("keep"));
            Assert.AreEqual("retained", dictionary["keep"]);

            string[] storedKeys = (string[])keysField.GetValue(dictionary);
            string[] storedValues = (string[])valuesField.GetValue(dictionary);
            bool preserveFlag = (bool)preserveField.GetValue(dictionary);

            Assert.IsNotNull(storedKeys, "Serialized keys should remain when null values exist.");
            Assert.IsNotNull(
                storedValues,
                "Serialized values should remain when null values exist."
            );
            CollectionAssert.AreEqual(serializedKeys, storedKeys);
            CollectionAssert.AreEqual(serializedValues, storedValues);
            Assert.IsTrue(preserveFlag, "Null values should force serialized cache preservation.");

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void EditorAfterDeserializeSuppressesWarnings()
        {
            SerializableDictionary<string, string> dictionary = new();

            Type baseType = typeof(SerializableDictionary<string, string>).BaseType;
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo keysField = baseType.GetField("_keys", flags);
            FieldInfo valuesField = baseType.GetField("_values", flags);
            Assert.IsNotNull(keysField);
            Assert.IsNotNull(valuesField);

            string[] keys = new string[] { null, "valid" };
            string[] values = new string[] { "ignored", "retained" };
            keysField.SetValue(dictionary, keys);
            valuesField.SetValue(dictionary, values);

            SerializableDictionaryBase editorSync = dictionary;
            editorSync.EditorAfterDeserialize();

            Assert.AreEqual(1, dictionary.Count);
            Assert.IsTrue(dictionary.ContainsKey("valid"));
            Assert.AreEqual("retained", dictionary["valid"]);

            string[] storedKeys = (string[])keysField.GetValue(dictionary);
            string[] storedValues = (string[])valuesField.GetValue(dictionary);
            CollectionAssert.AreEqual(keys, storedKeys);
            CollectionAssert.AreEqual(values, storedValues);

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void DictionaryMutationsClearPreservedSerializedEntries()
        {
            SerializableDictionary<int, string> dictionary = new();

            Type baseType = typeof(SerializableDictionary<int, string>).BaseType;
            Assert.IsNotNull(baseType, "Base type lookup failed.");

            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo keysField = baseType.GetField("_keys", flags);
            FieldInfo valuesField = baseType.GetField("_values", flags);
            FieldInfo preserveField = baseType.GetField("_preserveSerializedEntries", flags);

            Assert.IsNotNull(keysField, "Keys backing field lookup failed.");
            Assert.IsNotNull(valuesField, "Values backing field lookup failed.");
            Assert.IsNotNull(preserveField, "Preserve flag field lookup failed.");

            int[] serializedKeys = new int[] { 3, 3 };
            string[] serializedValues = new string[] { "old", "new" };
            keysField.SetValue(dictionary, serializedKeys);
            valuesField.SetValue(dictionary, serializedValues);

            dictionary.OnAfterDeserialize();
            Assert.IsTrue((bool)preserveField.GetValue(dictionary));

            dictionary.Add(4, "fresh");

            bool preserveAfterAdd = (bool)preserveField.GetValue(dictionary);
            int[] storedKeysAfterAdd = (int[])keysField.GetValue(dictionary);
            string[] storedValuesAfterAdd = (string[])valuesField.GetValue(dictionary);

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
    }
}
