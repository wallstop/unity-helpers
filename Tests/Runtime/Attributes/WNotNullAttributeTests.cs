namespace WallstopStudios.UnityHelpers.Tests.Attributes
{
    using System;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Tests.Core;

    [TestFixture]
    public sealed class WNotNullAttributeTests : CommonTestBase
    {
        [Test]
        public void CheckForNullsThrowsWhenAnnotatedFieldIsNull()
        {
            WNotNullHolder holder = new();
            Assert.Throws<ArgumentNullException>(() => holder.CheckForNulls());

            holder.reference = new object();
            Assert.DoesNotThrow(() => holder.CheckForNulls());
        }

        [Test]
        public void CheckForNullsThrowsWhenUnityObjectFieldIsNull()
        {
            WNotNullUnityObjectHolder holder = new();
            Assert.Throws<ArgumentNullException>(() => holder.CheckForNulls());

            holder.unityObject = NewGameObject("TestObject");
            Assert.DoesNotThrow(() => holder.CheckForNulls());
        }

        [Test]
        public void CheckForNullsThrowsForDestroyedUnityObject()
        {
            WNotNullUnityObjectHolder holder = new();
            GameObject gameObject = NewGameObject("DestroyedObject");
            holder.unityObject = gameObject;
            Assert.DoesNotThrow(() => holder.CheckForNulls());

            UnityEngine.Object.DestroyImmediate(gameObject);
            _trackedObjects.Remove(gameObject);
            Assert.Throws<ArgumentNullException>(() => holder.CheckForNulls());
        }

        [Test]
        public void CheckForNullsThrowsForMultipleNullFields()
        {
            WNotNullMultipleHolder holder = new();
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                holder.CheckForNulls()
            );
            Assert.That(exception.ParamName, Is.EqualTo(nameof(WNotNullMultipleHolder.firstField)));
        }

        [Test]
        public void CheckForNullsDoesNotThrowWhenAllFieldsAssigned()
        {
            WNotNullMultipleHolder holder = new()
            {
                firstField = new object(),
                secondField = "test",
                thirdField = NewGameObject("TestObject"),
            };
            Assert.DoesNotThrow(() => holder.CheckForNulls());
        }

        [Test]
        public void AttributeDefaultConstructorSetsWarningMessageType()
        {
            WNotNullAttribute attribute = new();
            Assert.That(attribute.MessageType, Is.EqualTo(WNotNullMessageType.Warning));
            Assert.That(attribute.CustomMessage, Is.Null);
        }

        [Test]
        public void AttributeMessageTypeConstructorSetsCorrectType()
        {
            WNotNullAttribute warningAttribute = new(WNotNullMessageType.Warning);
            Assert.That(warningAttribute.MessageType, Is.EqualTo(WNotNullMessageType.Warning));
            Assert.That(warningAttribute.CustomMessage, Is.Null);

            WNotNullAttribute errorAttribute = new(WNotNullMessageType.Error);
            Assert.That(errorAttribute.MessageType, Is.EqualTo(WNotNullMessageType.Error));
            Assert.That(errorAttribute.CustomMessage, Is.Null);
        }

        [Test]
        public void AttributeCustomMessageConstructorSetsMessage()
        {
            string customMessage = "Custom validation message";
            WNotNullAttribute attribute = new(customMessage);
            Assert.That(attribute.CustomMessage, Is.EqualTo(customMessage));
            Assert.That(attribute.MessageType, Is.EqualTo(WNotNullMessageType.Warning));
        }

        [Test]
        public void AttributeFullConstructorSetsBothProperties()
        {
            string customMessage = "Error validation message";
            WNotNullAttribute attribute = new(WNotNullMessageType.Error, customMessage);
            Assert.That(attribute.CustomMessage, Is.EqualTo(customMessage));
            Assert.That(attribute.MessageType, Is.EqualTo(WNotNullMessageType.Error));
        }

        [Test]
        public void AttributeWithWarningTypeAndCustomMessage()
        {
            string customMessage = "Warning: This field should be assigned";
            WNotNullAttribute attribute = new(WNotNullMessageType.Warning, customMessage);
            Assert.That(attribute.CustomMessage, Is.EqualTo(customMessage));
            Assert.That(attribute.MessageType, Is.EqualTo(WNotNullMessageType.Warning));
        }

        [Test]
        public void AttributeWithEmptyStringCustomMessage()
        {
            WNotNullAttribute attribute = new(string.Empty);
            Assert.That(attribute.CustomMessage, Is.EqualTo(string.Empty));
            Assert.That(attribute.MessageType, Is.EqualTo(WNotNullMessageType.Warning));
        }

        [Test]
        public void AttributeWithNullCustomMessage()
        {
            WNotNullAttribute attribute = new((string)null);
            Assert.That(attribute.CustomMessage, Is.Null);
            Assert.That(attribute.MessageType, Is.EqualTo(WNotNullMessageType.Warning));
        }

        [Test]
        public void MessageTypeEnumHasExpectedValues()
        {
            Assert.That((int)WNotNullMessageType.Warning, Is.EqualTo(0));
            Assert.That((int)WNotNullMessageType.Error, Is.EqualTo(1));
        }

        [Test]
        public void CheckForNullsWithMixedAttributeConfigurations()
        {
            WNotNullMixedConfigHolder holder = new();
            Assert.Throws<ArgumentNullException>(() => holder.CheckForNulls());

            holder.warningField = new object();
            Assert.Throws<ArgumentNullException>(() => holder.CheckForNulls());

            holder.errorField = new object();
            Assert.Throws<ArgumentNullException>(() => holder.CheckForNulls());

            holder.customMessageField = new object();
            Assert.DoesNotThrow(() => holder.CheckForNulls());
        }
    }

    internal sealed class WNotNullHolder
    {
        [WNotNull]
        public object reference;
    }

    internal sealed class WNotNullUnityObjectHolder
    {
        [WNotNull]
        public UnityEngine.Object unityObject;
    }

    internal sealed class WNotNullMultipleHolder
    {
        [WNotNull]
        public object firstField;

        [WNotNull]
        public string secondField;

        [WNotNull]
        public GameObject thirdField;
    }

    internal sealed class WNotNullMixedConfigHolder
    {
        [WNotNull(WNotNullMessageType.Warning)]
        public object warningField;

        [WNotNull(WNotNullMessageType.Error)]
        public object errorField;

        [WNotNull("Custom error message")]
        public object customMessageField;
    }
}
