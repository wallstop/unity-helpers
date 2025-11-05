namespace WallstopStudios.UnityHelpers.Tests.Serialization
{
    using System.Collections.Generic;
    using System.Text.Json;
    using NUnit.Framework;
    using ProtoBuf;
    using WallstopStudios.UnityHelpers.Core.DataStructure;
    using WallstopStudios.UnityHelpers.Core.Serialization;
    using Serializer = WallstopStudios.UnityHelpers.Core.Serialization.Serializer;

    public sealed class SerializableDictionaryJsonTests
    {
        [Test]
        public void SerializableDictionaryRoundTripsJson()
        {
            SerializableDictionary<string, int> original = new SerializableDictionary<string, int>
            {
                { "alpha", 1 },
                { "beta", 2 },
                { "gamma", 3 },
            };

            string json = Serializer.JsonStringify(original);
            SerializableDictionary<string, int> deserialized = Serializer.JsonDeserialize<
                SerializableDictionary<string, int>
            >(json);

            Assert.AreEqual(original.Count, deserialized.Count);
            foreach (KeyValuePair<string, int> pair in original)
            {
                Assert.IsTrue(deserialized.TryGetValue(pair.Key, out int value), pair.Key);
                Assert.AreEqual(pair.Value, value, pair.Key);
            }
        }

        [Test]
        public void SerializableDictionaryRoundTripsJsonWithFastOptions()
        {
            SerializableDictionary<int, string> original = new SerializableDictionary<int, string>
            {
                { 1, "one" },
                { 2, "two" },
                { 3, "three" },
            };

            JsonSerializerOptions options = Serializer.CreateFastJsonOptions();
            string json = Serializer.JsonStringify(original, options);
            SerializableDictionary<int, string> deserialized = Serializer.JsonDeserialize<
                SerializableDictionary<int, string>
            >(json, null, options);

            Assert.AreEqual(original.Count, deserialized.Count);
            foreach (KeyValuePair<int, string> pair in original)
            {
                Assert.IsTrue(deserialized.TryGetValue(pair.Key, out string value));
                Assert.AreEqual(pair.Value, value);
            }
        }

        [Test]
        public void SerializableDictionaryCacheRoundTripsJson()
        {
            SerializableDictionary<
                string,
                SerializablePayload,
                SerializableDictionary.Cache<SerializablePayload>
            > original =
                new SerializableDictionary<
                    string,
                    SerializablePayload,
                    SerializableDictionary.Cache<SerializablePayload>
                >();

            original.Add("primary", new SerializablePayload { Id = 1, Name = "Primary" });
            original.Add("secondary", new SerializablePayload { Id = 2, Name = "Secondary" });

            JsonSerializerOptions options = Serializer.CreatePrettyJsonOptions();
            string json = Serializer.JsonStringify(original, options);
            SerializableDictionary<
                string,
                SerializablePayload,
                SerializableDictionary.Cache<SerializablePayload>
            > deserialized = Serializer.JsonDeserialize<
                SerializableDictionary<
                    string,
                    SerializablePayload,
                    SerializableDictionary.Cache<SerializablePayload>
                >
            >(json, null, options);

            Assert.AreEqual(original.Count, deserialized.Count);
            foreach (KeyValuePair<string, SerializablePayload> pair in original)
            {
                Assert.IsTrue(deserialized.TryGetValue(pair.Key, out SerializablePayload value));
                Assert.AreEqual(pair.Value.Id, value.Id);
                Assert.AreEqual(pair.Value.Name, value.Name);
            }
        }

        [ProtoContract]
        private sealed class SerializablePayload
        {
            [ProtoMember(1)]
            public int Id { get; set; }

            [ProtoMember(2)]
            public string Name { get; set; }
        }
    }
}
