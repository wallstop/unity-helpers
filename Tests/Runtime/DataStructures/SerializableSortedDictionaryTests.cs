namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using Serializer = WallstopStudios.UnityHelpers.Core.Serialization.Serializer;

    public sealed class SerializableSortedDictionaryTests
    {
        private sealed class DescendingComparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                if (x == y)
                {
                    return 0;
                }

                return x > y ? -1 : 1;
            }
        }

        [Test]
        public void EntriesEnumerateInAscendingOrder()
        {
            SerializableSortedDictionary<int, string> dictionary =
                new SerializableSortedDictionary<int, string>();
            dictionary.Add(3, "three");
            dictionary.Add(1, "one");
            dictionary.Add(2, "two");

            int[] expectedKeys = new int[] { 1, 2, 3 };
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
            SerializableSortedDictionary<int, string> dictionary =
                new SerializableSortedDictionary<int, string>();
            Dictionary<int, string> source = new Dictionary<int, string>
            {
                { 7, "seven" },
                { 4, "four" },
                { 5, "five" },
            };

            dictionary.CopyFrom(source);

            int[] expectedKeys = new int[] { 4, 5, 7 };
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
        public void TryAddWhenKeyExistsDoesNotInvalidateCache()
        {
            SerializableSortedDictionary<int, string> dictionary =
                new SerializableSortedDictionary<int, string>();
            dictionary.Add(5, "five");

            Type baseType = typeof(SerializableSortedDictionary<int, string>).BaseType;
            Assert.IsNotNull(baseType, "Base type lookup failed.");

            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo keysField = baseType.GetField("_keys", flags);
            FieldInfo valuesField = baseType.GetField("_values", flags);
            FieldInfo arraysDirtyField = baseType.GetField("_arraysDirty", flags);
            Assert.IsNotNull(keysField, "Serialized keys field was not found.");
            Assert.IsNotNull(valuesField, "Serialized values field was not found.");
            Assert.IsNotNull(arraysDirtyField, "Array dirty flag field was not found.");

            dictionary.OnBeforeSerialize();

            object serializedKeys = keysField.GetValue(dictionary);
            object serializedValues = valuesField.GetValue(dictionary);

            Assert.IsNotNull(serializedKeys, "Serialized keys should be generated.");
            Assert.IsNotNull(serializedValues, "Serialized values should be generated.");
            Assert.IsFalse((bool)arraysDirtyField.GetValue(dictionary));

            bool added = dictionary.TryAdd(5, "duplicate");

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
            Assert.IsFalse(
                (bool)arraysDirtyField.GetValue(dictionary),
                "Failed TryAdd must not mark arrays dirty."
            );
            Assert.AreEqual("five", dictionary[5]);
        }

        [Test]
        public void IndexerUpdateClearsSerializationArrays()
        {
            SerializableSortedDictionary<int, string> dictionary =
                new SerializableSortedDictionary<int, string>();
            dictionary.Add(7, "seven");

            Type baseType = typeof(SerializableSortedDictionary<int, string>).BaseType;
            Assert.IsNotNull(baseType, "Base type lookup failed.");

            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo keysField = baseType.GetField("_keys", flags);
            FieldInfo valuesField = baseType.GetField("_values", flags);
            FieldInfo arraysDirtyField = baseType.GetField("_arraysDirty", flags);
            Assert.IsNotNull(keysField, "Serialized keys field was not found.");
            Assert.IsNotNull(valuesField, "Serialized values field was not found.");
            Assert.IsNotNull(arraysDirtyField, "Array dirty flag field was not found.");

            dictionary.OnBeforeSerialize();

            Assert.IsNotNull(keysField.GetValue(dictionary));
            Assert.IsNotNull(valuesField.GetValue(dictionary));
            Assert.IsFalse((bool)arraysDirtyField.GetValue(dictionary));

            dictionary[7] = "updated";

            Assert.IsNull(
                keysField.GetValue(dictionary),
                "Indexer mutations must clear cached keys."
            );
            Assert.IsNull(
                valuesField.GetValue(dictionary),
                "Indexer mutations must clear cached values."
            );
            Assert.IsTrue(
                (bool)arraysDirtyField.GetValue(dictionary),
                "Indexer mutations must mark arrays dirty."
            );
            Assert.AreEqual("updated", dictionary[7]);
        }

        [Test]
        public void OnBeforeSerializeSkipsRebuildWhenCacheFresh()
        {
            SerializableSortedDictionary<int, string> dictionary =
                new SerializableSortedDictionary<int, string>();
            dictionary.Add(2, "two");
            dictionary.Add(4, "four");

            Type baseType = typeof(SerializableSortedDictionary<int, string>).BaseType;
            Assert.IsNotNull(baseType, "Base type lookup failed.");

            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo keysField = baseType.GetField("_keys", flags);
            FieldInfo valuesField = baseType.GetField("_values", flags);
            FieldInfo arraysDirtyField = baseType.GetField("_arraysDirty", flags);
            Assert.IsNotNull(keysField, "Serialized keys field was not found.");
            Assert.IsNotNull(valuesField, "Serialized values field was not found.");
            Assert.IsNotNull(arraysDirtyField, "Array dirty flag field was not found.");

            dictionary.OnBeforeSerialize();

            object initialKeys = keysField.GetValue(dictionary);
            object initialValues = valuesField.GetValue(dictionary);

            Assert.IsNotNull(initialKeys);
            Assert.IsNotNull(initialValues);
            Assert.IsFalse((bool)arraysDirtyField.GetValue(dictionary));

            dictionary.OnBeforeSerialize();

            Assert.AreSame(initialKeys, keysField.GetValue(dictionary));
            Assert.AreSame(initialValues, valuesField.GetValue(dictionary));
            Assert.IsFalse((bool)arraysDirtyField.GetValue(dictionary));
        }

        [Test]
        public void CopyFromNullThrowsArgumentNullException()
        {
            SerializableSortedDictionary<int, string> dictionary =
                new SerializableSortedDictionary<int, string>();

            Assert.Throws<ArgumentNullException>(() => dictionary.CopyFrom(null));
        }

        [Test]
        public void ProtoSerializationPreservesTemporaryArraysWhenNoDuplicatesExist()
        {
            SerializableSortedDictionary<int, string> dictionary =
                new SerializableSortedDictionary<int, string>();
            dictionary.Add(1, "one");
            dictionary.Add(3, "three");

            Type baseType = typeof(SerializableSortedDictionary<int, string>).BaseType;
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

            SerializableSortedDictionary<int, string> roundTripped = Serializer.ProtoDeserialize<
                SerializableSortedDictionary<int, string>
            >(payload);

            int[] expectedKeys = new int[] { 1, 3 };
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
            SerializableSortedDictionary<int, string> dictionary =
                new SerializableSortedDictionary<int, string>();
            dictionary.Add(10, "ten");
            dictionary.Add(2, "two");
            dictionary.Add(7, "seven");

            dictionary.OnBeforeSerialize();

            Type baseType = typeof(SerializableSortedDictionary<int, string>).BaseType;
            Assert.IsNotNull(baseType);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo keysField = baseType.GetField("_keys", flags);
            FieldInfo valuesField = baseType.GetField("_values", flags);
            Assert.IsNotNull(keysField);
            Assert.IsNotNull(valuesField);

            int[] serializedKeys = (int[])keysField.GetValue(dictionary);
            string[] serializedValues = (string[])valuesField.GetValue(dictionary);

            int[] expectedKeys = new int[] { 2, 7, 10 };
            string[] expectedValues = new string[] { "two", "seven", "ten" };

            CollectionAssert.AreEqual(expectedKeys, serializedKeys);
            CollectionAssert.AreEqual(expectedValues, serializedValues);
        }

        [Test]
        public void OnAfterDeserializeSortsEntries()
        {
            SerializableSortedDictionary<int, string> dictionary =
                new SerializableSortedDictionary<int, string>();
            Type baseType = typeof(SerializableSortedDictionary<int, string>).BaseType;
            Assert.IsNotNull(baseType);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo keysField = baseType.GetField("_keys", flags);
            FieldInfo valuesField = baseType.GetField("_values", flags);
            Assert.IsNotNull(keysField);
            Assert.IsNotNull(valuesField);

            int[] unsortedKeys = new int[] { 5, 1, 3 };
            string[] unsortedValues = new string[] { "five", "one", "three" };
            keysField.SetValue(dictionary, unsortedKeys);
            valuesField.SetValue(dictionary, unsortedValues);

            dictionary.OnAfterDeserialize();

            int[] expectedKeys = new int[] { 1, 3, 5 };
            string[] expectedValues = new string[] { "one", "three", "five" };

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
        public void ProtoSerializationRoundTrips()
        {
            SerializableSortedDictionary<int, string> original =
                new SerializableSortedDictionary<int, string>();
            original.Add(4, "four");
            original.Add(2, "two");
            original.Add(9, "nine");

            byte[] data = Serializer.ProtoSerialize(original);
            SerializableSortedDictionary<int, string> deserialized = Serializer.ProtoDeserialize<
                SerializableSortedDictionary<int, string>
            >(data);

            int[] expectedKeys = new int[] { 2, 4, 9 };
            int index = 0;
            foreach (KeyValuePair<int, string> pair in deserialized)
            {
                Assert.Less(index, expectedKeys.Length);
                Assert.AreEqual(expectedKeys[index], pair.Key);
                index++;
            }

            Assert.AreEqual(expectedKeys.Length, index);
        }

        [Test]
        public void CustomComparerControlsOrdering()
        {
            SerializableSortedDictionary<int, string> dictionary = new SerializableSortedDictionary<
                int,
                string
            >(new DescendingComparer());
            dictionary.Add(1, "one");
            dictionary.Add(3, "three");
            dictionary.Add(2, "two");

            Assert.IsInstanceOf<DescendingComparer>(dictionary.Comparer);

            int[] expectedKeys = new int[] { 3, 2, 1 };
            int index = 0;
            foreach (KeyValuePair<int, string> pair in dictionary)
            {
                Assert.Less(index, expectedKeys.Length);
                Assert.AreEqual(expectedKeys[index], pair.Key);
                index++;
            }

            Assert.AreEqual(expectedKeys.Length, index);
        }
    }
}
