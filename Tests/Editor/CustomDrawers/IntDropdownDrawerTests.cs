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
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using PropertyAttribute = UnityEngine.PropertyAttribute;

    [TestFixture]
    public sealed class IntDropdownDrawerTests : CommonTestBase
    {
        [Test]
        public void CreatePropertyGUIWithoutOptionsReturnsHelpBox()
        {
            IntDropdownNoOptionsAsset asset = CreateScriptableObject<IntDropdownNoOptionsAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(IntDropdownNoOptionsAsset.unspecified)
            );
            Assert.IsNotNull(property, "Failed to locate int property.");

            IntDropdownDrawer drawer = new();
            AssignAttribute(
                drawer,
                new IntDropDownAttribute(
                    typeof(IntDropdownEmptySource),
                    nameof(IntDropdownEmptySource.GetEmptyOptions)
                )
            );
            VisualElement element = drawer.CreatePropertyGUI(property);
            Assert.IsInstanceOf<HelpBox>(element);
        }

        [Test]
        public void CreatePropertyGUIWithOptionsReturnsSelectorField()
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
            AssignAttribute(drawer, new IntDropDownAttribute(5, 10, 15));
            VisualElement element = drawer.CreatePropertyGUI(property);
            Assert.IsInstanceOf<BaseField<int>>(element);

            BaseField<int> selector = (BaseField<int>)element;
            DropdownField dropdown = selector.Q<DropdownField>();
            Assert.IsNotNull(dropdown, "Dropdown field was not created.");
            Assert.That(dropdown.value, Is.EqualTo("10"));
        }

        [Test]
        public void CreatePropertyGUILargeOptionsReturnsPopupSelector()
        {
            IntDropdownLargeOptionsAsset asset =
                CreateScriptableObject<IntDropdownLargeOptionsAsset>();
            asset.selection = 100;
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(IntDropdownLargeOptionsAsset.selection)
            );
            Assert.IsNotNull(property, "Failed to locate selection property.");

            IntDropdownDrawer drawer = new();
            AssignAttribute(
                drawer,
                new IntDropDownAttribute(
                    typeof(IntDropdownLargeSource),
                    nameof(IntDropdownLargeSource.GetLargeOptions)
                )
            );
            VisualElement element = drawer.CreatePropertyGUI(property);

            // Large options should use popup selector (BaseField<int>)
            Assert.IsInstanceOf<BaseField<int>>(element);
        }

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
            AssignAttribute(drawer, new IntDropDownAttribute(5, 10, 15));
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
            AssignAttribute(drawer, new IntDropDownAttribute(5, 10, 15));
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

            IntDropDownAttribute attribute = new(
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
            IntDropDownAttribute attribute = new(
                nameof(IntDropdownInstanceMethodAsset.GetDynamicValues)
            );
            int[] options = attribute.GetOptions(null);
            Assert.That(options.Length, Is.EqualTo(0));
        }

        [Test]
        public void StaticMethodProviderReturnsValues()
        {
            IntDropDownAttribute attribute = new(
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
            IntDropDownAttribute attribute = new(1, 2, 3, 4, 5);
            int[] options = attribute.Options;

            Assert.That(options.Length, Is.EqualTo(5));
            Assert.That(options[0], Is.EqualTo(1));
            Assert.That(options[4], Is.EqualTo(5));
        }

        [Test]
        public void BackingAttributeIsWValueDropDown()
        {
            IntDropDownAttribute attribute = new(10, 20, 30);
            WValueDropDownAttribute backingAttribute = attribute.BackingAttribute;
            Assert.IsNotNull(backingAttribute);
            Assert.That(backingAttribute.ValueType, Is.EqualTo(typeof(int)));
        }

        [Test]
        public void RequiresInstanceContextIsTrueForInstanceProvider()
        {
            IntDropDownAttribute attribute = new(
                nameof(IntDropdownInstanceMethodAsset.GetDynamicValues)
            );
            Assert.That(attribute.RequiresInstanceContext, Is.True);
        }

        [Test]
        public void RequiresInstanceContextIsFalseForInlineList()
        {
            IntDropDownAttribute attribute = new(1, 2, 3);
            Assert.That(attribute.RequiresInstanceContext, Is.False);
        }

        [Test]
        public void RequiresInstanceContextIsFalseForStaticProvider()
        {
            IntDropDownAttribute attribute = new(
                typeof(IntDropdownSource),
                nameof(IntDropdownSource.GetStaticOptions)
            );
            Assert.That(attribute.RequiresInstanceContext, Is.False);
        }

        [Test]
        public void TypeAndMethodConstructorWithInstanceMethodReturnsContextValues()
        {
            // This tests the fix for instance methods with the (Type, string) constructor
            IntDropdownInstanceMethodAsset asset =
                CreateScriptableObject<IntDropdownInstanceMethodAsset>();
            asset.dynamicValues.AddRange(new[] { 50, 100, 150 });

            IntDropDownAttribute attribute = new(
                typeof(IntDropdownInstanceMethodAsset),
                nameof(IntDropdownInstanceMethodAsset.GetDynamicValues)
            );
            int[] options = attribute.GetOptions(asset);

            Assert.That(options.Length, Is.EqualTo(3));
            Assert.That(options[0], Is.EqualTo(50));
            Assert.That(options[1], Is.EqualTo(100));
            Assert.That(options[2], Is.EqualTo(150));
        }

        [Test]
        public void TypeAndMethodConstructorWithInstanceMethodAndNoContextReturnsEmpty()
        {
            // When no context is provided, instance method should return empty
            IntDropDownAttribute attribute = new(
                typeof(IntDropdownInstanceMethodAsset),
                nameof(IntDropdownInstanceMethodAsset.GetDynamicValues)
            );
            int[] options = attribute.Options;

            Assert.That(options.Length, Is.EqualTo(0));
        }

        [Test]
        public void TypeAndMethodConstructorRequiresInstanceContextForInstanceMethod()
        {
            // When using (Type, string) constructor with an instance method,
            // RequiresInstanceContext should be true
            IntDropDownAttribute attribute = new(
                typeof(IntDropdownInstanceMethodAsset),
                nameof(IntDropdownInstanceMethodAsset.GetDynamicValues)
            );
            Assert.That(attribute.RequiresInstanceContext, Is.True);
        }

        [Test]
        public void LargeOptionsListUsesPageSizeThreshold()
        {
            // Verify the attribute can retrieve options from large source
            IntDropDownAttribute attribute = new(
                typeof(IntDropdownLargeSource),
                nameof(IntDropdownLargeSource.GetLargeOptions)
            );
            int[] options = attribute.Options;

            Assert.That(options, Is.Not.Null);
            Assert.That(options.Length, Is.EqualTo(50));
            Assert.That(options[0], Is.EqualTo(10));
            Assert.That(options[49], Is.EqualTo(500));
        }

        [UnityTest]
        public IEnumerator OnGUIHandlesLargeOptionsListWithoutException()
        {
            IntDropdownLargeOptionsAsset asset =
                CreateScriptableObject<IntDropdownLargeOptionsAsset>();
            asset.selection = 100;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(IntDropdownLargeOptionsAsset.selection)
            );
            Assert.IsNotNull(property, "Failed to locate selection property.");

            IntDropdownDrawer drawer = new();
            IntDropDownAttribute attribute = new(
                typeof(IntDropdownLargeSource),
                nameof(IntDropdownLargeSource.GetLargeOptions)
            );
            AssignAttribute(drawer, attribute);
            Rect position = new(0f, 0f, 400f, EditorGUIUtility.singleLineHeight);
            GUIContent label = new("Large Selection");

            // Should not throw when rendering with large options (popup button path)
            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(position, property, label);
            });

            // Value should remain unchanged since we didn't simulate a click
            serializedObject.ApplyModifiedProperties();
            Assert.That(asset.selection, Is.EqualTo(100));
        }

        [Test]
        public void CreatePropertyGUIShowsErrorForStringFieldWithIntDropdown()
        {
            IntDropdownTypeMismatchAsset asset =
                CreateScriptableObject<IntDropdownTypeMismatchAsset>();

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(IntDropdownTypeMismatchAsset.stringFieldWithIntDropdown)
            );
            Assert.IsNotNull(property, "Failed to locate string field property.");

            IntDropdownDrawer drawer = new();
            AssignAttribute(drawer, new IntDropDownAttribute(1, 2, 3));
            VisualElement element = drawer.CreatePropertyGUI(property);

            Assert.IsInstanceOf<HelpBox>(element, "Expected HelpBox for type mismatch");
            HelpBox helpBox = (HelpBox)element;
            Assert.That(
                helpBox.text,
                Does.Contain("Type mismatch"),
                "Error message should indicate type mismatch"
            );
            Assert.That(
                helpBox.text,
                Does.Contain("string"),
                "Error message should mention the actual type"
            );
            Assert.That(
                helpBox.text,
                Does.Contain("int"),
                "Error message should mention the expected type"
            );
        }

        [Test]
        public void CreatePropertyGUIShowsErrorForFloatFieldWithIntDropdown()
        {
            IntDropdownTypeMismatchAsset asset =
                CreateScriptableObject<IntDropdownTypeMismatchAsset>();

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(IntDropdownTypeMismatchAsset.floatFieldWithIntDropdown)
            );
            Assert.IsNotNull(property, "Failed to locate float field property.");

            IntDropdownDrawer drawer = new();
            AssignAttribute(drawer, new IntDropDownAttribute(1, 2, 3));
            VisualElement element = drawer.CreatePropertyGUI(property);

            Assert.IsInstanceOf<HelpBox>(element, "Expected HelpBox for type mismatch");
            HelpBox helpBox = (HelpBox)element;
            Assert.That(
                helpBox.text,
                Does.Contain("Type mismatch"),
                "Error message should indicate type mismatch"
            );
            Assert.That(
                helpBox.text,
                Does.Contain("float"),
                "Error message should mention the actual type"
            );
        }

        [TestCase(
            nameof(IntDropdownTypeMismatchAsset.stringFieldWithIntDropdown),
            "string",
            Description = "String field should show type mismatch"
        )]
        [TestCase(
            nameof(IntDropdownTypeMismatchAsset.floatFieldWithIntDropdown),
            "float",
            Description = "Float field should show type mismatch"
        )]
        [TestCase(
            nameof(IntDropdownTypeMismatchAsset.boolFieldWithIntDropdown),
            "bool",
            Description = "Bool field should show type mismatch"
        )]
        public void IntDropdownTypeMismatchShowsErrorForVariousTypes(
            string fieldName,
            string expectedTypeName
        )
        {
            IntDropdownTypeMismatchAsset asset =
                CreateScriptableObject<IntDropdownTypeMismatchAsset>();

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(fieldName);
            Assert.IsNotNull(property, $"Failed to locate {fieldName} property.");

            IntDropdownDrawer drawer = new();
            AssignAttribute(drawer, new IntDropDownAttribute(1, 2, 3));
            VisualElement element = drawer.CreatePropertyGUI(property);

            Assert.IsInstanceOf<HelpBox>(
                element,
                $"Expected HelpBox for {fieldName} type mismatch"
            );
            HelpBox helpBox = (HelpBox)element;
            Assert.That(
                helpBox.text,
                Does.Contain("Type mismatch"),
                $"Error message for {fieldName} should indicate type mismatch"
            );
            Assert.That(
                helpBox.text,
                Does.Contain(expectedTypeName).IgnoreCase,
                $"Error message for {fieldName} should mention '{expectedTypeName}'"
            );
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
