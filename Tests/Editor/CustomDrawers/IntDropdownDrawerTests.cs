namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
#if UNITY_EDITOR
    using System.Collections;
    using NUnit.Framework;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes;
    using WallstopStudios.UnityHelpers.Tests.EditorFramework;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using PropertyAttribute = UnityEngine.PropertyAttribute;

    [TestFixture]
    public sealed class IntDropdownDrawerTests : CommonTestBase
    {
        [UnityTest]
        public IEnumerator OnGUIClampsValuesOutsideConfiguredOptions()
        {
            IntDropdownTestAsset asset = CreateScriptableObject<IntDropdownTestAsset>();
            asset.missingValue = 999;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(IntDropdownTestAsset.missingValue)
            );
            Assert.IsNotNull(property, "Failed to locate missing value property.");

            IntDropdownDrawer drawer = new();
            AssignAttribute(drawer, new IntDropdownAttribute(5, 10, 15));
            Rect position = new(0f, 0f, 240f, EditorGUIUtility.singleLineHeight);
            GUIContent label = new("Missing");

            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(position, property, label);
            });

            serializedObject.ApplyModifiedProperties();
            Assert.That(asset.missingValue, Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator OnGUILeavesValidSelectionUnchanged()
        {
            IntDropdownTestAsset asset = CreateScriptableObject<IntDropdownTestAsset>();
            asset.validValue = 10;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(IntDropdownTestAsset.validValue)
            );
            Assert.IsNotNull(property, "Failed to locate valid value property.");

            IntDropdownDrawer drawer = new();
            AssignAttribute(drawer, new IntDropdownAttribute(5, 10, 15));
            Rect position = new(0f, 0f, 240f, EditorGUIUtility.singleLineHeight);
            GUIContent label = new("Valid");

            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(position, property, label);
            });

            serializedObject.ApplyModifiedProperties();
            Assert.That(asset.validValue, Is.EqualTo(10));
        }

        [Test]
        public void InstanceMethodProviderReturnsValues()
        {
            IntDropdownInstanceMethodAsset asset =
                CreateScriptableObject<IntDropdownInstanceMethodAsset>();
            asset.dynamicValues.AddRange(new[] { 100, 200, 300 });

            IntDropdownAttribute attribute = new(
                nameof(IntDropdownInstanceMethodAsset.GetDynamicValues)
            );
            int[] options = attribute.GetOptions(asset);

            Assert.That(options.Length, Is.EqualTo(3));
            Assert.That(options[0], Is.EqualTo(100));
            Assert.That(options[1], Is.EqualTo(200));
            Assert.That(options[2], Is.EqualTo(300));
        }

        [Test]
        public void InstanceMethodProviderWithNoContextReturnsEmpty()
        {
            IntDropdownAttribute attribute = new(
                nameof(IntDropdownInstanceMethodAsset.GetDynamicValues)
            );
            int[] options = attribute.GetOptions(null);
            Assert.That(options.Length, Is.EqualTo(0));
        }

        [Test]
        public void StaticMethodProviderReturnsValues()
        {
            IntDropdownAttribute attribute = new(
                typeof(IntDropdownSource),
                nameof(IntDropdownSource.GetStaticOptions)
            );
            int[] options = attribute.Options;

            Assert.That(options.Length, Is.EqualTo(3));
            Assert.That(options[0], Is.EqualTo(100));
            Assert.That(options[1], Is.EqualTo(200));
            Assert.That(options[2], Is.EqualTo(300));
        }

        [Test]
        public void InlineOptionsReturnsCorrectValues()
        {
            IntDropdownAttribute attribute = new(1, 2, 3, 4, 5);
            int[] options = attribute.Options;

            Assert.That(options.Length, Is.EqualTo(5));
            Assert.That(options[0], Is.EqualTo(1));
            Assert.That(options[4], Is.EqualTo(5));
        }

        [Test]
        public void BackingAttributeIsWValueDropDown()
        {
            IntDropdownAttribute attribute = new(10, 20, 30);
            WValueDropDownAttribute backingAttribute = attribute.BackingAttribute;
            Assert.IsNotNull(backingAttribute);
            Assert.That(backingAttribute.ValueType, Is.EqualTo(typeof(int)));
        }

        [Test]
        public void RequiresInstanceContextIsTrueForInstanceProvider()
        {
            IntDropdownAttribute attribute = new(
                nameof(IntDropdownInstanceMethodAsset.GetDynamicValues)
            );
            Assert.That(attribute.RequiresInstanceContext, Is.True);
        }

        [Test]
        public void RequiresInstanceContextIsFalseForInlineList()
        {
            IntDropdownAttribute attribute = new(1, 2, 3);
            Assert.That(attribute.RequiresInstanceContext, Is.False);
        }

        [Test]
        public void RequiresInstanceContextIsFalseForStaticProvider()
        {
            IntDropdownAttribute attribute = new(
                typeof(IntDropdownSource),
                nameof(IntDropdownSource.GetStaticOptions)
            );
            Assert.That(attribute.RequiresInstanceContext, Is.False);
        }

        private static void AssignAttribute(PropertyDrawer drawer, PropertyAttribute attribute)
        {
            FieldInfo attributeField = typeof(PropertyDrawer).GetField(
                "m_Attribute",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            Assert.IsNotNull(attributeField, "Unable to locate PropertyDrawer.m_Attribute.");
            attributeField.SetValue(drawer, attribute);
        }
    }
#endif
}
