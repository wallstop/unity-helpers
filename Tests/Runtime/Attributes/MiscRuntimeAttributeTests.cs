namespace WallstopStudios.UnityHelpers.Tests.Attributes
{
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
        public void IntDropDownAttributeExposesOptions()
        {
            IntDropDownAttribute attribute = new(1, 2, 3);
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
        public void WShowIfAttributeExposesComparisonMode()
        {
            WShowIfAttribute attribute = new("flag", WShowIfComparison.GreaterThan, 5);
            Assert.AreEqual(WShowIfComparison.GreaterThan, attribute.comparison);
        }

        [Test]
        public void WShowIfAttributeComparisonConstructorWithoutValuesSetsMode()
        {
            WShowIfAttribute attribute = new("flag", WShowIfComparison.IsNull);
            Assert.AreEqual(WShowIfComparison.IsNull, attribute.comparison);
            Assert.IsEmpty(attribute.expectedValues);
        }

        [Test]
        public void WShowIfAttributeDefaultsComparisonToEqual()
        {
            WShowIfAttribute attribute = new("flag");
            Assert.AreEqual(WShowIfComparison.Equal, attribute.comparison);
        }

        [Test]
        public void WShowIfAttributeParamsConstructorCopiesValues()
        {
            WShowIfAttribute attribute = new("flag", 1, 2, 3);
            CollectionAssert.AreEqual(new object[] { 1, 2, 3 }, attribute.expectedValues);
        }

        [Test]
        public void WReadOnlyAttributeDerivesFromPropertyAttribute()
        {
            Assert.IsInstanceOf<PropertyAttribute>(new WReadOnlyAttribute());
        }
    }
}
