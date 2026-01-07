// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Serialization
{
    using System;
    using NUnit.Framework;
    using ProtoBuf;
    using Serializer = WallstopStudios.UnityHelpers.Core.Serialization.Serializer;

    [TestFixture]
    [NUnit.Framework.Category("Fast")]
    public sealed class ProtoRootRegistrationTests
    {
        public interface IAnimal { }

        [ProtoContract]
        private sealed class Dog : IAnimal
        {
            [ProtoMember(1)]
            public int Age { get; set; }

            [ProtoMember(2)]
            public string Name { get; set; }
        }

        [ProtoContract]
        private sealed class Cat : IAnimal
        {
            [ProtoMember(1)]
            public int Lives { get; set; }

            [ProtoMember(2)]
            public string Color { get; set; }
        }

        private sealed class NoContractAnimal : IAnimal { }

        [Test]
        public void MultipleImplementationsRequireRegistration()
        {
            IAnimal original = new Dog { Age = 5, Name = "Rex" };

            byte[] data = Serializer.ProtoSerialize(original);
            Serializer.RegisterProtobufRoot<IAnimal, Dog>();

            IAnimal round = Serializer.ProtoDeserialize<IAnimal>(data);

            Assert.IsNotNull(round, "Deserialized instance should not be null");
            Assert.IsInstanceOf<Dog>(round, "Expected registered root type to be used");
            Dog dog = (Dog)round;
            Assert.AreEqual(5, dog.Age, "Age should match");
            Assert.AreEqual("Rex", dog.Name, "Name should match");
        }

        [Test]
        public void RegisteringInvalidRootMissingContractThrows()
        {
            Assert.Throws<ArgumentException>(
                () => Serializer.RegisterProtobufRoot(typeof(IAnimal), typeof(NoContractAnimal)),
                "Missing [ProtoContract] should throw"
            );
        }

        [Test]
        public void RegisteringIncompatibleRootThrows()
        {
            Assert.Throws<ArgumentException>(
                () => Serializer.RegisterProtobufRoot(typeof(IAnimal), typeof(string)),
                "Incompatible root should throw"
            );
        }

        [Test]
        public void ConflictingRegistrationThrows()
        {
            Serializer.RegisterProtobufRoot<IAnimal, Dog>();
            Assert.Throws<InvalidOperationException>(
                () => Serializer.RegisterProtobufRoot<IAnimal, Cat>(),
                "Conflicting root registration should throw"
            );
        }
    }
}
