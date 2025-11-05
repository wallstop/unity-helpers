namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure;

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

        [Serializable]
        private sealed class IntCache : SerializableDictionary.Cache<int> { }
    }
}
