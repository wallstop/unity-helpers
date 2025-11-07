namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using NUnit.Framework;
    using ProtoBuf;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    public sealed class SerializableTypeTests
    {
        [Test]
        public void DefaultWrapperBehavesAsEmpty()
        {
            SerializableType serializable = default;

            Assert.IsTrue(serializable.IsEmpty);
            Assert.IsNull(serializable.Value);
            Assert.IsNull((Type)serializable);
            Assert.IsTrue(serializable.Equals(null));
            Assert.IsTrue(serializable.EqualsType(null));
            Assert.AreEqual(string.Empty, serializable.AssemblyQualifiedName);
            Assert.AreEqual(0, serializable.GetHashCode());
        }

        [Test]
        public void AssigningTypePersistsAssemblyQualifiedName()
        {
            SerializableType serializable = SerializableType.FromType(typeof(List<int>));

            Assert.IsFalse(serializable.IsEmpty);
            Assert.AreEqual(typeof(List<int>), serializable.Value);
            Assert.AreEqual(
                typeof(List<int>).AssemblyQualifiedName,
                serializable.AssemblyQualifiedName
            );
            Assert.IsTrue(serializable.EqualsType(typeof(List<int>)));
            Type extracted = serializable;
            Assert.AreEqual(typeof(List<int>), extracted);
            Assert.IsFalse(serializable.Equals(null));
            Assert.IsFalse(serializable.EqualsType(null));
        }

        [Test]
        public void EqualityOperatorsMatchSystemType()
        {
            SerializableType first = SerializableType.FromType(typeof(int));
            SerializableType second = SerializableType.FromType(typeof(int));
            SerializableType different = SerializableType.FromType(typeof(float));

            Assert.IsTrue(first.EqualsType(typeof(int)));
            Assert.IsFalse(first.EqualsType(typeof(float)));
            Assert.IsFalse(first.Equals(different));
        }

        [Test]
        public void ImplicitConversionsRoundTripType()
        {
            SerializableType serializable = SerializableType.FromType(typeof(string));
            Type extracted = serializable;

            Assert.AreEqual(typeof(string), extracted);
            SerializableType reconstructed = SerializableType.FromType(extracted);
            Assert.AreEqual(serializable, reconstructed);
            Assert.AreEqual(
                serializable.AssemblyQualifiedName,
                reconstructed.AssemblyQualifiedName
            );
        }

        [Test]
        public void FromTypeHandlesNull()
        {
            SerializableType none = SerializableType.FromType(null);
            SerializableType implicitNone = SerializableType.FromType(null);

            Assert.IsTrue(none.IsEmpty);
            Assert.IsNull(none.Value);
            Assert.AreEqual(string.Empty, none.AssemblyQualifiedName);
            Assert.IsTrue(implicitNone.IsEmpty);
            Assert.IsNull(implicitNone.Value);
            Assert.AreEqual(string.Empty, implicitNone.AssemblyQualifiedName);
            Assert.IsTrue(implicitNone.Equals(null));
            Assert.IsTrue(implicitNone.EqualsType(null));
            Assert.IsTrue(none.Equals(default(SerializableType)));
            Assert.IsTrue(default(SerializableType).Equals(none));
            Assert.IsTrue(none.EqualsType(null));
            Assert.IsNull((Type)none);
        }

        [Test]
        public void DisplayNameIncludesAssemblyAndType()
        {
            SerializableType serializable = new(typeof(Dictionary<string, int>));
            string displayName = serializable.DisplayName;

            Assert.IsTrue(displayName.Contains("Dictionary", StringComparison.Ordinal));
            Assert.IsTrue(
                displayName.Contains(
                    typeof(Dictionary<string, int>).Assembly.GetName().Name,
                    StringComparison.Ordinal
                )
            );
        }

        [Test]
        public void JsonSerializationRoundTripsType()
        {
            SerializableType serializable = new(typeof(Dictionary<string, int>));
            string json = JsonSerializer.Serialize(serializable);

            SerializableType roundTripped = JsonSerializer.Deserialize<SerializableType>(json);
            Assert.IsNotNull(roundTripped.Value);
            Assert.AreEqual(typeof(Dictionary<string, int>), roundTripped.Value);

            string nullJson = JsonSerializer.Serialize(default(SerializableType));
            Assert.AreEqual("null", nullJson);

            SerializableType nullRoundTrip = JsonSerializer.Deserialize<SerializableType>("null");
            Assert.IsTrue(nullRoundTrip.IsEmpty);
            Assert.IsNull(nullRoundTrip.Value);
        }

        [Test]
        public void ProtoSerializationRoundTripsType()
        {
            SerializableType serializable = new(typeof(List<float>));
            using MemoryStream stream = new();
            Serializer.Serialize(stream, serializable);
            stream.Position = 0;

            SerializableType roundTripped = Serializer.Deserialize<SerializableType>(stream);
            Assert.AreEqual(typeof(List<float>), roundTripped.Value);

            stream.SetLength(0);
            stream.Position = 0;
            SerializableType empty = default;
            Serializer.Serialize(stream, empty);
            stream.Position = 0;

            SerializableType roundTrippedEmpty = Serializer.Deserialize<SerializableType>(stream);
            Assert.IsTrue(roundTrippedEmpty.IsEmpty);
            Assert.IsNull(roundTrippedEmpty.Value);
        }

        [Test]
        public void CatalogContainsSerializableType()
        {
            string[] names = SerializableTypeCatalog.GetAssemblyQualifiedNames();
            string expected = typeof(SerializableType).AssemblyQualifiedName;

            CollectionAssert.Contains(names, expected);

            IReadOnlyList<SerializableTypeCatalog.SerializableTypeDescriptor> filtered =
                SerializableTypeCatalog.GetFilteredDescriptors("SerializableType");

            bool found = false;
            for (int index = 0; index < filtered.Count; index++)
            {
                SerializableTypeCatalog.SerializableTypeDescriptor descriptor = filtered[index];
                if (descriptor.Type == typeof(SerializableType))
                {
                    found = true;
                    break;
                }
            }

            Assert.IsTrue(found, "SerializableType was not found in the filtered descriptor list.");
        }

        [Test]
        public void TryGetValueFailsForUnknownType()
        {
            SerializableType unresolved = SerializableType.FromSerializedName(
                "Missing.Type, MissingAssembly"
            );

            bool resolved = unresolved.TryGetValue(out Type resolvedType);
            Assert.IsFalse(resolved);
            Assert.IsNull(resolvedType);
            Assert.IsNull(unresolved.Value);
            Assert.IsFalse(unresolved.IsEmpty);
        }
    }
}
