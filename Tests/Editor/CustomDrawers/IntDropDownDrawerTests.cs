// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
#if UNITY_EDITOR
    using System.Collections;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes;
    using WallstopStudios.UnityHelpers.Tests.EditorFramework;
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;
    using PropertyAttribute = UnityEngine.PropertyAttribute;

    [TestFixture]
    [NUnit.Framework.Category("Slow")]
    [NUnit.Framework.Category("Integration")]
    public sealed class IntDropDownDrawerTests : CommonTestBase
    {
        [Test]
        public void CreatePropertyGUIWithoutOptionsReturnsHelpBox()
        {
            IntDropDownNoOptionsAsset asset = CreateScriptableObject<IntDropDownNoOptionsAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(IntDropDownNoOptionsAsset.unspecified)
            );
            Assert.IsTrue(property != null, "Failed to locate int property.");

            IntDropDownDrawer drawer = new();
            AssignAttribute(
                drawer,
                new IntDropDownAttribute(
                    typeof(IntDropDownEmptySource),
                    nameof(IntDropDownEmptySource.GetEmptyOptions)
                )
            );
            VisualElement element = drawer.CreatePropertyGUI(property);
            Assert.IsInstanceOf<HelpBox>(element);
        }

        [Test]
        public void CreatePropertyGUIWithOptionsReturnsSelectorField()
        {
            IntDropDownTestAsset asset = CreateScriptableObject<IntDropDownTestAsset>();
            asset.validValue = 10;
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(IntDropDownTestAsset.validValue)
            );
            Assert.IsTrue(property != null, "Failed to locate valid value property.");

            IntDropDownDrawer drawer = new();
            AssignAttribute(drawer, new IntDropDownAttribute(5, 10, 15));
            VisualElement element = drawer.CreatePropertyGUI(property);
            Assert.IsInstanceOf<BaseField<int>>(element);

            BaseField<int> selector = (BaseField<int>)element;
            DropdownField dropdown = selector.Q<DropdownField>();
            Assert.IsTrue(dropdown != null, "DropDown field was not created.");
            Assert.That(dropdown.value, Is.EqualTo("10"));
        }

        [Test]
        public void CreatePropertyGUILargeOptionsReturnsPopupSelector()
        {
            IntDropDownLargeOptionsAsset asset =
                CreateScriptableObject<IntDropDownLargeOptionsAsset>();
            asset.selection = 100;
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(IntDropDownLargeOptionsAsset.selection)
            );
            Assert.IsTrue(property != null, "Failed to locate selection property.");

            IntDropDownDrawer drawer = new();
            AssignAttribute(
                drawer,
                new IntDropDownAttribute(
                    typeof(IntDropDownLargeSource),
                    nameof(IntDropDownLargeSource.GetLargeOptions)
                )
            );
            VisualElement element = drawer.CreatePropertyGUI(property);

            // Large options should use popup selector (BaseField<int>)
            Assert.IsInstanceOf<BaseField<int>>(element);
        }

        [UnityTest]
        public IEnumerator OnGUIClampsValuesOutsideConfiguredOptions()
        {
            IntDropDownTestAsset asset = CreateScriptableObject<IntDropDownTestAsset>();
            asset.missingValue = 999;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(IntDropDownTestAsset.missingValue)
            );
            Assert.IsTrue(property != null, "Failed to locate missing value property.");

            IntDropDownDrawer drawer = new();
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
            IntDropDownTestAsset asset = CreateScriptableObject<IntDropDownTestAsset>();
            asset.validValue = 10;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(IntDropDownTestAsset.validValue)
            );
            Assert.IsTrue(property != null, "Failed to locate valid value property.");

            IntDropDownDrawer drawer = new();
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
            IntDropDownInstanceMethodAsset asset =
                CreateScriptableObject<IntDropDownInstanceMethodAsset>();
            asset.dynamicValues.AddRange(new[] { 100, 200, 300 });

            IntDropDownAttribute attribute = new(
                nameof(IntDropDownInstanceMethodAsset.GetDynamicValues)
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
                nameof(IntDropDownInstanceMethodAsset.GetDynamicValues)
            );
            int[] options = attribute.GetOptions(null);
            Assert.That(options.Length, Is.EqualTo(0));
        }

        [Test]
        public void StaticMethodProviderReturnsValues()
        {
            IntDropDownAttribute attribute = new(
                typeof(IntDropDownSource),
                nameof(IntDropDownSource.GetStaticOptions)
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
            Assert.IsTrue(backingAttribute != null);
            Assert.That(backingAttribute.ValueType, Is.EqualTo(typeof(int)));
        }

        [Test]
        public void RequiresInstanceContextIsTrueForInstanceProvider()
        {
            IntDropDownAttribute attribute = new(
                nameof(IntDropDownInstanceMethodAsset.GetDynamicValues)
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
                typeof(IntDropDownSource),
                nameof(IntDropDownSource.GetStaticOptions)
            );
            Assert.That(attribute.RequiresInstanceContext, Is.False);
        }

        [Test]
        public void TypeAndMethodConstructorWithInstanceMethodReturnsContextValues()
        {
            // This tests the fix for instance methods with the (Type, string) constructor
            IntDropDownInstanceMethodAsset asset =
                CreateScriptableObject<IntDropDownInstanceMethodAsset>();
            asset.dynamicValues.AddRange(new[] { 50, 100, 150 });

            IntDropDownAttribute attribute = new(
                typeof(IntDropDownInstanceMethodAsset),
                nameof(IntDropDownInstanceMethodAsset.GetDynamicValues)
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
                typeof(IntDropDownInstanceMethodAsset),
                nameof(IntDropDownInstanceMethodAsset.GetDynamicValues)
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
                typeof(IntDropDownInstanceMethodAsset),
                nameof(IntDropDownInstanceMethodAsset.GetDynamicValues)
            );
            Assert.That(attribute.RequiresInstanceContext, Is.True);
        }

        [Test]
        public void LargeOptionsListUsesPageSizeThreshold()
        {
            // Verify the attribute can retrieve options from large source
            IntDropDownAttribute attribute = new(
                typeof(IntDropDownLargeSource),
                nameof(IntDropDownLargeSource.GetLargeOptions)
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
            IntDropDownLargeOptionsAsset asset =
                CreateScriptableObject<IntDropDownLargeOptionsAsset>();
            asset.selection = 100;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(IntDropDownLargeOptionsAsset.selection)
            );
            Assert.IsTrue(property != null, "Failed to locate selection property.");

            IntDropDownDrawer drawer = new();
            IntDropDownAttribute attribute = new(
                typeof(IntDropDownLargeSource),
                nameof(IntDropDownLargeSource.GetLargeOptions)
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
        public void CreatePropertyGUIShowsErrorForStringFieldWithIntDropDown()
        {
            IntDropDownTypeMismatchAsset asset =
                CreateScriptableObject<IntDropDownTypeMismatchAsset>();

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(IntDropDownTypeMismatchAsset.stringFieldWithIntDropDown)
            );
            Assert.IsTrue(property != null, "Failed to locate string field property.");

            IntDropDownDrawer drawer = new();
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
        public void CreatePropertyGUIShowsErrorForFloatFieldWithIntDropDown()
        {
            IntDropDownTypeMismatchAsset asset =
                CreateScriptableObject<IntDropDownTypeMismatchAsset>();

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(IntDropDownTypeMismatchAsset.floatFieldWithIntDropDown)
            );
            Assert.IsTrue(property != null, "Failed to locate float field property.");

            IntDropDownDrawer drawer = new();
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
            nameof(IntDropDownTypeMismatchAsset.stringFieldWithIntDropDown),
            "string",
            Description = "String field should show type mismatch"
        )]
        [TestCase(
            nameof(IntDropDownTypeMismatchAsset.floatFieldWithIntDropDown),
            "float",
            Description = "Float field should show type mismatch"
        )]
        [TestCase(
            nameof(IntDropDownTypeMismatchAsset.boolFieldWithIntDropDown),
            "bool",
            Description = "Bool field should show type mismatch"
        )]
        public void IntDropDownTypeMismatchShowsErrorForVariousTypes(
            string fieldName,
            string expectedTypeName
        )
        {
            IntDropDownTypeMismatchAsset asset =
                CreateScriptableObject<IntDropDownTypeMismatchAsset>();

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(fieldName);
            Assert.IsTrue(property != null, $"Failed to locate {fieldName} property.");

            IntDropDownDrawer drawer = new();
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
            PropertyDrawerTestHelper.AssignAttribute(drawer, attribute);
        }
    }
#endif
}
