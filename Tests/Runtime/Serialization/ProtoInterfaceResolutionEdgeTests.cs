namespace WallstopStudios.UnityHelpers.Tests.Serialization
{
    using NUnit.Framework;
    using ProtoBuf;
    using Serializer = WallstopStudios.UnityHelpers.Core.Serialization.Serializer;

    [TestFixture]
    public sealed class ProtoInterfaceResolutionEdgeTests
    {
        public interface IWidget { }

        [ProtoContract]
        private sealed class Widget : IWidget
        {
            [ProtoMember(1)]
            public int Id { get; set; }

            [ProtoMember(2)]
            public string Label { get; set; }
        }

        [Test]
        public void SingleImplementationRequiresRegistration()
        {
            IWidget original = new Widget { Id = 3, Label = "ok" };
            byte[] data = Serializer.ProtoSerialize<IWidget>(original);

            Assert.Throws<ProtoException>(
                () => Serializer.ProtoDeserialize<IWidget>(data),
                "Deserializing interface even with a single implementation should require registration"
            );

            // After registration, it should succeed
            Serializer.RegisterProtobufRoot<IWidget, Widget>();
            IWidget round = Serializer.ProtoDeserialize<IWidget>(data);
            Assert.IsNotNull(round, "Deserialized instance should not be null");
            Assert.IsInstanceOf<Widget>(round);
            Widget w = (Widget)round;
            Assert.AreEqual(3, w.Id);
            Assert.AreEqual("ok", w.Label);
        }

        [ProtoContract]
        private abstract class AbstractBase
        {
            [ProtoMember(1)]
            public int Common { get; set; }
        }

        [ProtoContract]
        private sealed class DerivedA : AbstractBase
        {
            [ProtoMember(2)]
            public string ExtraA { get; set; }
        }

        [ProtoContract]
        private sealed class DerivedB : AbstractBase
        {
            [ProtoMember(2)]
            public string ExtraB { get; set; }
        }

        [Test]
        public void AbstractBaseWithoutRegistrationThrows()
        {
            AbstractBase original = new DerivedA { Common = 9, ExtraA = "x" };
            byte[] data = Serializer.ProtoSerialize<AbstractBase>(original, forceRuntimeType: true);

            Assert.Throws<ProtoException>(
                () => Serializer.ProtoDeserialize<AbstractBase>(data),
                "Deserializing abstract base with multiple derived types should require registration"
            );
        }
    }
}
