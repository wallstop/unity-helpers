namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text.Json;
    using NUnit.Framework;
    using ProtoBuf;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.DataStructure;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    public sealed class SerializableNullableTests
    {
        [Test]
        public void HasValueMatchesSystemNullable()
        {
            SerializableNullable<int> wrapped = new(5);
            Assert.IsTrue(wrapped.HasValue);
            Assert.AreEqual(5, wrapped.Value);
            Assert.AreEqual(5, wrapped.GetValueOrDefault());

            int? systemNullable = wrapped;
            Assert.IsTrue(systemNullable.HasValue);
            Assert.AreEqual(5, systemNullable.Value);
        }

        [Test]
        public void ClearAndSetValueTogglePresence()
        {
            SerializableNullable<int> wrapped = default;
            Assert.IsFalse(wrapped.HasValue);

            wrapped.SetValue(9);
            Assert.IsTrue(wrapped.HasValue);
            Assert.AreEqual(9, wrapped.Value);

            wrapped.Clear();
            Assert.IsFalse(wrapped.HasValue);
            Assert.AreEqual(0, wrapped.GetValueOrDefault());
        }

        [Test]
        public void EqualityBehavesLikeNullable()
        {
            SerializableNullable<int> empty = default;
            SerializableNullable<int> five = new(5);
            int? systemFive = 5;

            Assert.IsTrue(empty == null);
            Assert.IsTrue(null == empty);
            Assert.IsTrue(five == systemFive);
            Assert.IsTrue(systemFive == five);
            Assert.IsTrue(five == 5);
            Assert.IsTrue(5 == five);
            Assert.IsTrue(five.Equals(systemFive));
            Assert.IsTrue(five.Equals(5));
            Assert.IsFalse(five == null);
            Assert.IsTrue(five != null);

            SerializableNullable<int> otherFive = new(5);
            Assert.IsTrue(five == otherFive);
            Assert.IsFalse(five != otherFive);
        }

        [Test]
        public void TryGetValueFollowsPresence()
        {
            SerializableNullable<int> empty = default;
            bool emptyResult = empty.TryGetValue(out int emptyValue);
            Assert.IsFalse(emptyResult);
            Assert.AreEqual(0, emptyValue);

            SerializableNullable<int> wrapped = new(12);
            bool hasValue = wrapped.TryGetValue(out int actualValue);
            Assert.IsTrue(hasValue);
            Assert.AreEqual(12, actualValue);
        }

        [Test]
        public void JsonSerializationRoundTrips()
        {
            SerializableNullable<int> wrapped = new(42);
            string jsonValue = JsonSerializer.Serialize(wrapped);
            Assert.AreEqual("42", jsonValue);

            SerializableNullable<int> deserialized = JsonSerializer.Deserialize<
                SerializableNullable<int>
            >("42");
            Assert.IsTrue(deserialized.HasValue);
            Assert.AreEqual(42, deserialized.Value);

            string jsonNull = JsonSerializer.Serialize(default(SerializableNullable<int>));
            Assert.AreEqual("null", jsonNull);

            SerializableNullable<int> deserializedNull = JsonSerializer.Deserialize<
                SerializableNullable<int>
            >("null");
            Assert.IsFalse(deserializedNull.HasValue);
        }

        [Test]
        public void ProtoSerializationRoundTrips()
        {
            SerializableNullable<int> wrapped = new(1234);
            using MemoryStream stream = new();
            Serializer.Serialize(stream, wrapped);
            stream.Position = 0;
            SerializableNullable<int> roundTripped = Serializer.Deserialize<
                SerializableNullable<int>
            >(stream);

            Assert.IsTrue(roundTripped.HasValue);
            Assert.AreEqual(1234, roundTripped.Value);

            stream.SetLength(0);
            stream.Position = 0;
            SerializableNullable<int> empty = default;
            Serializer.Serialize(stream, empty);
            stream.Position = 0;
            SerializableNullable<int> roundTrippedEmpty = Serializer.Deserialize<
                SerializableNullable<int>
            >(stream);
            Assert.IsFalse(roundTrippedEmpty.HasValue);
        }

        [Test]
        public void ImplicitConversionsAreSymmetric()
        {
            int? systemNullable = 77;
            SerializableNullable<int> wrapped = systemNullable;
            Assert.IsTrue(wrapped.HasValue);
            Assert.AreEqual(77, wrapped.Value);

            int? convertedBack = wrapped;
            Assert.IsTrue(convertedBack.HasValue);
            Assert.AreEqual(77, convertedBack.Value);

            SerializableNullable<int> fromValue = 88;
            Assert.IsTrue(fromValue.HasValue);
            Assert.AreEqual(88, fromValue.Value);
        }

        [Test]
        public void ValueFieldIncludesWShowIfAttribute()
        {
            Type type = typeof(SerializableNullable<int>);
            string fieldName = SerializableNullable<int>.SerializedPropertyNames.Value;
            FieldInfo valueField = type.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.IsNotNull(valueField);

            object[] attributes = valueField.GetCustomAttributes(
                typeof(WShowIfAttribute),
                inherit: false
            );
            Assert.IsNotEmpty(attributes);

            WShowIfAttribute attribute = (WShowIfAttribute)attributes[0];
            Assert.AreEqual("_hasValue", attribute.conditionField);
            Assert.IsFalse(attribute.inverse);
        }
    }
}
