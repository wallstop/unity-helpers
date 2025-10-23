namespace WallstopStudios.UnityHelpers.Tests.Editor.Attributes
{
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Tags;
    using WallstopStudios.UnityHelpers.Tests.Editor.Utils;

    [TestFixture]
    public sealed class WShowIfPropertyDrawerTests : CommonTestBase
    {
        private static readonly FieldInfo AttributeField = typeof(PropertyDrawer).GetField(
            "m_Attribute",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        [Test]
        public void BoolConditionHidesFieldWhenFalse()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = new(container);
            serializedObject.Update();

            SerializedProperty dependentProperty = serializedObject.FindProperty(
                nameof(TestContainer.boolDependent)
            );
            Assert.NotNull(dependentProperty);

            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(nameof(TestContainer.boolCondition))
            );

            container.boolCondition = false;
            serializedObject.Update();
            float hiddenHeight = drawer.GetPropertyHeight(
                dependentProperty,
                new GUIContent("boolDependent")
            );
            Assert.That(hiddenHeight, Is.Zero);

            container.boolCondition = true;
            serializedObject.Update();
            float shownHeight = drawer.GetPropertyHeight(
                dependentProperty,
                new GUIContent("boolDependent")
            );
            Assert.That(shownHeight, Is.GreaterThan(0f));
        }

        [Test]
        public void EnumConditionMatchesExpectedValue()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = new(container);
            serializedObject.Update();

            SerializedProperty dependentProperty = serializedObject.FindProperty(
                nameof(TestContainer.durationDependent)
            );
            Assert.NotNull(dependentProperty);

            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(
                    nameof(TestContainer.durationType),
                    expectedValues: new object[] { ModifierDurationType.Duration }
                )
            );

            container.durationType = ModifierDurationType.Instant;
            serializedObject.Update();
            float hiddenHeight = drawer.GetPropertyHeight(dependentProperty, GUIContent.none);
            Assert.That(hiddenHeight, Is.Zero);

            container.durationType = ModifierDurationType.Duration;
            serializedObject.Update();
            float shownHeight = drawer.GetPropertyHeight(dependentProperty, GUIContent.none);
            Assert.That(shownHeight, Is.GreaterThan(0f));
        }

        [Test]
        public void EnumConditionHonorsInverseFlag()
        {
            TestContainer container = CreateScriptableObject<TestContainer>();
            SerializedObject serializedObject = new(container);
            serializedObject.Update();

            SerializedProperty dependentProperty = serializedObject.FindProperty(
                nameof(TestContainer.inverseDependent)
            );
            Assert.NotNull(dependentProperty);

            WShowIfPropertyDrawer drawer = CreateDrawer(
                new WShowIfAttribute(
                    nameof(TestContainer.durationType),
                    inverse: true,
                    expectedValues: new object[] { ModifierDurationType.Instant }
                )
            );

            container.durationType = ModifierDurationType.Instant;
            serializedObject.Update();
            float hiddenHeight = drawer.GetPropertyHeight(dependentProperty, GUIContent.none);
            Assert.That(hiddenHeight, Is.Zero);

            container.durationType = ModifierDurationType.Infinite;
            serializedObject.Update();
            float shownHeight = drawer.GetPropertyHeight(dependentProperty, GUIContent.none);
            Assert.That(shownHeight, Is.GreaterThan(0f));
        }

        private static WShowIfPropertyDrawer CreateDrawer(WShowIfAttribute attribute)
        {
            WShowIfPropertyDrawer drawer = new();
            Assert.NotNull(AttributeField);
            AttributeField.SetValue(drawer, attribute);
            return drawer;
        }

        private sealed class TestContainer : ScriptableObject
        {
            public bool boolCondition;

            [WShowIf(nameof(boolCondition))]
            public int boolDependent;

            public ModifierDurationType durationType = ModifierDurationType.Instant;

            [WShowIf(
                nameof(durationType),
                expectedValues: new object[] { ModifierDurationType.Duration }
            )]
            public int durationDependent;

            [WShowIf(
                nameof(durationType),
                inverse: true,
                expectedValues: new object[] { ModifierDurationType.Instant }
            )]
            public int inverseDependent;
        }
    }
}
