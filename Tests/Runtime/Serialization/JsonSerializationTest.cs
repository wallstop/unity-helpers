namespace UnityHelpers.Tests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;
    using Core.Extension;
    using Core.Random;
    using Core.Serialization;
    using NUnit.Framework;

    [DataContract]
    public sealed class TestDataObject
    {
        public string field;
        public int Property { get; set; }

        [JsonPropertyName("DifferentPropertyName")]
        public float NamedProperty { get; set; }

        public Dictionary<string, bool> DictionaryProperty { get; set; } = new();

        public List<int> ListProperty { get; set; } = new();
    }

    public class JsonSerializationTest
    {
        [Test]
        public void SerializationWorks()
        {
            IRandom random = PRNG.Instance;
            TestDataObject input = new()
            {
                field = Guid.NewGuid().ToString(),
                Property = random.Next(),
                NamedProperty = random.NextFloat(),
            };

            int dictionaryProperties = random.Next(4, 10);
            for (int i = 0; i < dictionaryProperties; ++i)
            {
                input.DictionaryProperty[Guid.NewGuid().ToString()] = random.NextBool();
            }

            int listProperties = random.Next(4, 10);
            for (int i = 0; i < listProperties; ++i)
            {
                input.ListProperty.Add(random.Next());
            }

            string json = input.ToJson();
            Assert.IsTrue(
                json.Contains("DifferentPropertyName"),
                $"DifferentPropertyName failed to serialize! JSON: {json}"
            );

            TestDataObject deserialized = Serializer.JsonDeserialize<TestDataObject>(json);
            Assert.AreEqual(
                input.field,
                deserialized.field,
                $"Unexpected {nameof(deserialized.field)}! JSON: {json}"
            );
            Assert.AreEqual(
                input.Property,
                deserialized.Property,
                $"Unexpected {nameof(deserialized.Property)}! JSON: {json}"
            );
            Assert.AreEqual(
                input.NamedProperty,
                deserialized.NamedProperty,
                $"Unexpected {nameof(deserialized.NamedProperty)}! JSON: {json}"
            );
            Assert.IsTrue(
                input.DictionaryProperty.ContentEquals(deserialized.DictionaryProperty),
                $"Unexpected {nameof(deserialized.DictionaryProperty)}! JSON: {json}"
            );
            Assert.IsTrue(
                input.ListProperty.SequenceEqual(deserialized.ListProperty),
                $"Unexpected {nameof(deserialized.ListProperty)}! JSON: {json}"
            );
        }
    }
}
