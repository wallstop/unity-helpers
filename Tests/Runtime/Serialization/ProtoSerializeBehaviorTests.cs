namespace WallstopStudios.UnityHelpers.Tests.Serialization
{
    using NUnit.Framework;
    using ProtoBuf;
    using Serializer = WallstopStudios.UnityHelpers.Core.Serialization.Serializer;

    [TestFixture]
    public sealed class ProtoSerializeBehaviorTests
    {
        [ProtoContract]
        [ProtoInclude(100, typeof(DerivedMsg))]
        private class BaseMsg
        {
            [ProtoMember(1)]
            public int A { get; set; }
        }

        [ProtoContract]
        private class DerivedMsg : BaseMsg
        {
            [ProtoMember(2)]
            public string B { get; set; }
        }

        [Test]
        public void GenericBasePrefersRuntimeTypeWhenDifferent()
        {
            DerivedMsg original = new() { A = 123, B = "hello" };
            byte[] data = Serializer.ProtoSerialize<BaseMsg>(original);
            DerivedMsg round = Serializer.ProtoDeserialize<DerivedMsg>(data);

            Assert.IsNotNull(round, "Deserialized instance should not be null");
            Assert.AreEqual(123, round.A, "Base field A should match");
            Assert.AreEqual("hello", round.B, "Derived field B should be preserved");
        }

        [Test]
        public void GenericBasePreservesDerivedFieldsWithForce()
        {
            DerivedMsg original = new() { A = 7, B = "world" };
            byte[] data = Serializer.ProtoSerialize<BaseMsg>(original, forceRuntimeType: true);

            DerivedMsg round = Serializer.ProtoDeserialize<DerivedMsg>(data);

            Assert.IsNotNull(round, "Deserialized instance should not be null");
            Assert.AreEqual(7, round.A, "Base field A should match");
            Assert.AreEqual("world", round.B, "Derived field B should be preserved");
        }

        [Test]
        public void ObjectDeclaredUsesRuntimeTypeByDefault()
        {
            object original = new DerivedMsg { A = 42, B = "obj" };
            byte[] data = Serializer.ProtoSerialize<object>(original);
            DerivedMsg round = Serializer.ProtoDeserialize<DerivedMsg>(data);

            Assert.IsNotNull(round, "Deserialized instance should not be null");
            Assert.AreEqual(42, round.A, "Base field A should match");
            Assert.AreEqual("obj", round.B, "Derived field B should be preserved");
        }
    }
}
