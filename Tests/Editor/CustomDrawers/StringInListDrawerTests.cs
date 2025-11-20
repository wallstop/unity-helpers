namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using NUnit.Framework;
    using System.Reflection;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Tests.Utils;
    using PropertyAttribute = UnityEngine.PropertyAttribute;

    [TestFixture]
    public sealed class StringInListDrawerTests : CommonTestBase
    {
        [Test]
        public void CreatePropertyGUIWithoutOptionsReturnsHelpBox()
        {
            NoOptionsAsset asset = CreateScriptableObject<NoOptionsAsset>();
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(NoOptionsAsset.unspecified)
            );
            Assert.IsNotNull(property, "Failed to locate string property.");

            StringInListDrawer drawer = new StringInListDrawer();
            AssignAttribute(drawer, new StringInListAttribute());
            VisualElement element = drawer.CreatePropertyGUI(property);
            Assert.IsInstanceOf<HelpBox>(element);
        }

        [Test]
        public void SelectorUpdatesStringSerializedProperty()
        {
            StringOptionsAsset asset = CreateScriptableObject<StringOptionsAsset>();
            asset.state = "Run";
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(StringOptionsAsset.state)
            );
            Assert.IsNotNull(property, "Failed to locate state property.");

            StringInListDrawer drawer = new StringInListDrawer();
            AssignAttribute(drawer, new StringInListAttribute("Idle", "Run", "Jump"));
            VisualElement element = drawer.CreatePropertyGUI(property);
            Assert.IsInstanceOf<BaseField<string>>(element);

            BaseField<string> selector = (BaseField<string>)element;
            DropdownField dropdown = selector.Q<DropdownField>();
            Assert.IsNotNull(dropdown, "Dropdown field was not created.");
            Assert.That(dropdown.value, Is.EqualTo("Run"));

            InvokeApplySelection(selector, 2);
            serializedObject.Update();
            Assert.That(asset.state, Is.EqualTo("Jump"));
        }

        [Test]
        public void SelectorWritesSelectedIndexToIntegerProperty()
        {
            IntegerOptionsAsset asset = CreateScriptableObject<IntegerOptionsAsset>();
            asset.selection = 0;
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(IntegerOptionsAsset.selection)
            );
            Assert.IsNotNull(property, "Failed to locate integer-backed dropdown.");

            StringInListDrawer drawer = new StringInListDrawer();
            AssignAttribute(drawer, new StringInListAttribute("Low", "Medium", "High"));
            VisualElement element = drawer.CreatePropertyGUI(property);
            DropdownField dropdown = element.Q<DropdownField>();
            Assert.IsNotNull(dropdown, "Dropdown field was not created.");

            InvokeApplySelection((BaseField<string>)element, 2);
            serializedObject.Update();
            Assert.That(asset.selection, Is.EqualTo(2));
        }

        [Serializable]
        private sealed class NoOptionsAsset : ScriptableObject
        {
            [StringInList]
            public string unspecified = string.Empty;
        }

        [Serializable]
        private sealed class StringOptionsAsset : ScriptableObject
        {
            [StringInList("Idle", "Run", "Jump")]
            public string state = "Idle";
        }

        [Serializable]
        private sealed class IntegerOptionsAsset : ScriptableObject
        {
            [StringInList("Low", "Medium", "High")]
            public int selection;
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

        private static void InvokeApplySelection(BaseField<string> selector, int optionIndex)
        {
            MethodInfo method = selector
                .GetType()
                .GetMethod("ApplySelection", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Unable to locate ApplySelection on selector.");
            method.Invoke(selector, new object[] { optionIndex });
        }
    }
#endif
}
