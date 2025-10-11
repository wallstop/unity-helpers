namespace WallstopStudios.UnityHelpers.Tests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using NUnit.Framework;
    using ProtoBuf;
    using WallstopStudios.UnityHelpers.Core.DataStructure;
    using WallstopStudios.UnityHelpers.Core.Serialization;
    using Serializer = WallstopStudios.UnityHelpers.Core.Serialization.Serializer;

    public sealed class ProtoSerializationTests
    {
        [ProtoContract]
        private sealed class SampleMessage
        {
            [ProtoMember(1)]
            public int Id { get; set; }

            [ProtoMember(2)]
            public string Name { get; set; }

            [ProtoMember(3)]
            public List<int> Values { get; set; }
        }

        [Test]
        public void ProtoSerializeRoundTripsComplexObject()
        {
            SampleMessage original = new()
            {
                Id = 5,
                Name = "Test",
                Values = new List<int> { 1, 2, 3 },
            };

            byte[] data = Serializer.ProtoSerialize(original);
            SampleMessage deserialized = Serializer.ProtoDeserialize<SampleMessage>(data);

            Assert.AreNotSame(original, deserialized);
            Assert.AreEqual(original.Id, deserialized.Id);
            Assert.AreEqual(original.Name, deserialized.Name);
            CollectionAssert.AreEqual(original.Values, deserialized.Values);
        }

        [Test]
        public void ProtoSerializeRoundTripsUsingSerializationTypeFacade()
        {
            SampleMessage original = new()
            {
                Id = 10,
                Name = "Facade",
                Values = new List<int> { 42 },
            };

            byte[] data = Serializer.Serialize(original, SerializationType.Protobuf);
            SampleMessage deserialized = Serializer.Deserialize<SampleMessage>(
                data,
                SerializationType.Protobuf
            );

            Assert.AreEqual(original.Id, deserialized.Id);
            Assert.AreEqual(original.Name, deserialized.Name);
            CollectionAssert.AreEqual(original.Values, deserialized.Values);
        }

        [Test]
        public void ProtoDeserializeWithExplicitTypeReturnsInstance()
        {
            SampleMessage original = new() { Id = 7, Name = "Explicit" };
            byte[] data = Serializer.ProtoSerialize(original);

            object boxed = Serializer.ProtoDeserialize<object>(data, typeof(SampleMessage));

            Assert.IsInstanceOf<SampleMessage>(boxed);
            SampleMessage message = (SampleMessage)boxed;
            Assert.AreEqual(original.Id, message.Id);
            Assert.AreEqual(original.Name, message.Name);
        }

        [Test]
        public void SerializeThrowsForUnsupportedSerializationType()
        {
            SampleMessage sample = new();

            Assert.Throws<InvalidEnumArgumentException>(() =>
                Serializer.Serialize(sample, (SerializationType)999)
            );
            Assert.Throws<InvalidEnumArgumentException>(() =>
                Serializer.Deserialize<SampleMessage>(Array.Empty<byte>(), (SerializationType)999)
            );
        }

        [Test]
        public void ProtoDeserializeHandlesEmpty()
        {
            SampleMessage message = Serializer.ProtoDeserialize<SampleMessage>(Array.Empty<byte>());
            Assert.IsNotNull(message);
            SampleMessage expected = new();
            Assert.AreEqual(expected.Id, message.Id);
            Assert.AreEqual(expected.Name, message.Name);
            Assert.AreEqual(expected.Values, message.Values);
        }

        [Test]
        public void ProtoDeserializeThrowsWhenDataNull()
        {
            Assert.Throws<ProtoException>(() => Serializer.ProtoDeserialize<SampleMessage>(null));
        }

        [Test]
        public void ProtoDeserializeThrowsWhenDataIsGarbage()
        {
            byte[] garbage = { 0xFF, 0x00, 0x01, 0x02, 0xAB, 0xCD };
            Assert.Throws<ProtoException>(() =>
                Serializer.ProtoDeserialize<SampleMessage>(garbage)
            );
        }

        [Test]
        public void ProtoDeserializeWithExplicitTypeThrowsWhenTypeNull()
        {
            SampleMessage original = new() { Id = 1, Name = "NullType" };
            byte[] data = Serializer.ProtoSerialize(original);
            Assert.Throws<ArgumentNullException>(() =>
                Serializer.ProtoDeserialize<object>(data, null)
            );
        }

        [Test]
        public void ProtoDeserializeWithExplicitTypeWhenDataEmpty()
        {
            object message = Serializer.ProtoDeserialize<object>(
                Array.Empty<byte>(),
                typeof(SampleMessage)
            );
            Assert.IsNotNull(message);
            Assert.IsInstanceOf<SampleMessage>(message);
            SampleMessage sample = (SampleMessage)message;
            SampleMessage expected = new();
            Assert.AreEqual(expected.Id, sample.Id);
            Assert.AreEqual(expected.Name, sample.Name);
            Assert.AreEqual(expected.Values, sample.Values);
        }

        [Test]
        public void ProtoSerializeRoundTripsImmutableBitSet()
        {
            BitSet mutable = new(96);
            mutable.TrySet(0);
            mutable.TrySet(15);
            mutable.TrySet(63);
            mutable.TrySet(64);
            mutable.TrySet(95);
            ImmutableBitSet original = mutable.ToImmutable();

            byte[] data = Serializer.ProtoSerialize(original);
            ImmutableBitSet clone = Serializer.ProtoDeserialize<ImmutableBitSet>(data);

            Assert.AreEqual(original.Capacity, clone.Capacity);
            for (int i = 0; i < original.Capacity; i++)
            {
                original.TryGet(i, out bool expected);
                clone.TryGet(i, out bool actual);
                Assert.AreEqual(expected, actual, $"Bit {i} mismatch");
            }
        }
    }
}
