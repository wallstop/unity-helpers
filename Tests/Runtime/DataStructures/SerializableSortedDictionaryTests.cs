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
