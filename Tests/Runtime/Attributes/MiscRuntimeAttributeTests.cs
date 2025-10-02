namespace WallstopStudios.UnityHelpers.Tests.Attributes
{
    using System;
    using System.Reflection;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using PropertyAttribute = UnityEngine.PropertyAttribute;

    [TestFixture]
    public sealed class MiscRuntimeAttributeTests
    {
        [Test]
        public void EnumDisplayNameAttributeStoresProvidedName()
        {
            EnumDisplayNameAttribute attribute = new("Pretty Name");
            Assert.AreEqual("Pretty Name", attribute.DisplayName);
        }

        [Test]
        public void IntDropdownAttributeExposesOptions()
        {
            IntDropdownAttribute attribute = new(1, 2, 3);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, attribute.Options);
        }

        [Test]
        public void ScriptableSingletonPathNullBecomesEmptyString()
        {
            ScriptableSingletonPathAttribute attribute = new(null);
            Assert.AreEqual(string.Empty, attribute.resourcesPath);
        }

        [Test]
        public void WShowIfAttributeCopiesExpectedValues()
        {
            object[] input = { 1, "two" };
            WShowIfAttribute attribute = new("flag", expectedValues: input);
            CollectionAssert.AreEqual(input, attribute.expectedValues);

            input[0] = 5;
            Assert.AreNotEqual(input[0], attribute.expectedValues[0]);
        }

        [Test]
        public void DxReadOnlyAttributeDerivesFromPropertyAttribute()
        {
            Assert.IsInstanceOf<PropertyAttribute>(new DxReadOnlyAttribute());
        }

        [Test]
        public void KSerializableAttributesAreDiscoverableViaReflection()
        {
            Type type = typeof(SerializableTarget);
            Assert.IsNotNull(Attribute.GetCustomAttribute(type, typeof(KSerializableAttribute)));

            FieldInfo field = type.GetField(nameof(SerializableTarget.Included));
            Assert.IsNotNull(Attribute.GetCustomAttribute(field, typeof(KSerializableAttribute)));

            FieldInfo ignored = type.GetField(nameof(SerializableTarget.Ignored));
            Assert.IsNotNull(
                Attribute.GetCustomAttribute(ignored, typeof(KNonSerializableAttribute))
            );
        }
    }

    [KSerializable]
    internal sealed class SerializableTarget
    {
        [KSerializable]
        public int Included;

        [KNonSerializable]
        public int Ignored;
    }
}
