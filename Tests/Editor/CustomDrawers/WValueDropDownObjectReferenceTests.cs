// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
#if UNITY_EDITOR
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers.Base;
    using WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;

    /// <summary>
    /// Comprehensive tests for WValueDropDown ObjectReference (UnityEngine.Object) support.
    /// </summary>
    [TestFixture]
    [NUnit.Framework.Category("Slow")]
    [NUnit.Framework.Category("Integration")]
    public sealed class WValueDropDownObjectReferenceTests : CommonTestBase
    {
        private WValueDropDownTestScriptableObject _testObject1;
        private WValueDropDownTestScriptableObject _testObject2;
        private WValueDropDownTestScriptableObject _testObject3;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            _testObject1 = CreateScriptableObject<WValueDropDownTestScriptableObject>();
            _testObject1.name = "TestObject1";
            _testObject1.displayValue = "First Object";
            _testObject1.identifier = 1;

            _testObject2 = CreateScriptableObject<WValueDropDownTestScriptableObject>();
            _testObject2.name = "TestObject2";
            _testObject2.displayValue = "Second Object";
            _testObject2.identifier = 2;

            _testObject3 = CreateScriptableObject<WValueDropDownTestScriptableObject>();
            _testObject3.name = "TestObject3";
            _testObject3.displayValue = "Third Object";
            _testObject3.identifier = 3;
        }

        [TearDown]
        public override void TearDown()
        {
            WValueDropDownObjectReferenceSource.Clear();
            base.TearDown();
        }

        [Test]
        public void ApplyOptionUpdatesObjectReferenceSerializedProperty()
        {
            WValueDropDownObjectReferenceAsset asset =
                CreateScriptableObject<WValueDropDownObjectReferenceAsset>();
            asset.availableOptions.Add(_testObject1);
            asset.availableOptions.Add(_testObject2);
            asset.availableOptions.Add(_testObject3);

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownObjectReferenceAsset.selectedObject)
            );
            Assert.IsNotNull(property, "Failed to locate selectedObject property.");

            WValueDropDownDrawer.ApplyOption(property, _testObject2);
            serializedObject.ApplyModifiedProperties();

            Assert.That(asset.selectedObject, Is.SameAs(_testObject2));
        }

        [Test]
        public void ApplyOptionSetsNullWhenOptionIsNull()
        {
            WValueDropDownObjectReferenceAsset asset =
                CreateScriptableObject<WValueDropDownObjectReferenceAsset>();
            asset.selectedObject = _testObject1;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownObjectReferenceAsset.selectedObject)
            );
            Assert.IsNotNull(property, "Failed to locate selectedObject property.");

            WValueDropDownDrawer.ApplyOption(property, null);
            serializedObject.ApplyModifiedProperties();

            Assert.That(asset.selectedObject, Is.Null);
        }

        [Test]
        public void InstanceMethodProviderReturnsObjectReferenceValues()
        {
            WValueDropDownObjectReferenceAsset asset =
                CreateScriptableObject<WValueDropDownObjectReferenceAsset>();
            asset.availableOptions.Add(_testObject1);
            asset.availableOptions.Add(_testObject2);

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownObjectReferenceAsset.selectedObject)
            );
            Assert.IsNotNull(property, "Failed to locate selectedObject property.");

            WValueDropDownAttribute attribute =
                PropertyDrawerTestHelper.GetAttributeFromProperty<WValueDropDownAttribute>(
                    property
                );
            Assert.IsNotNull(attribute, "Failed to retrieve attribute.");

            object[] options = attribute.GetOptions(asset);
            Assert.That(options.Length, Is.EqualTo(2));
            Assert.That(options[0], Is.SameAs(_testObject1));
            Assert.That(options[1], Is.SameAs(_testObject2));
        }

        [Test]
        public void StaticMethodProviderReturnsObjectReferenceValues()
        {
            WValueDropDownTestScriptableObject[] staticObjects = new[]
            {
                _testObject1,
                _testObject2,
                _testObject3,
            };
            WValueDropDownObjectReferenceSource.SetStaticScriptableObjects(staticObjects);

            WValueDropDownObjectReferenceAsset asset =
                CreateScriptableObject<WValueDropDownObjectReferenceAsset>();

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownObjectReferenceAsset.staticSelectedObject)
            );
            Assert.IsNotNull(property, "Failed to locate staticSelectedObject property.");

            WValueDropDownAttribute attribute =
                PropertyDrawerTestHelper.GetAttributeFromProperty<WValueDropDownAttribute>(
                    property
                );
            Assert.IsNotNull(attribute, "Failed to retrieve attribute.");

            object[] options = attribute.GetOptions(asset);
            Assert.That(options.Length, Is.EqualTo(3));
            Assert.That(options[0], Is.SameAs(_testObject1));
            Assert.That(options[1], Is.SameAs(_testObject2));
            Assert.That(options[2], Is.SameAs(_testObject3));
        }

        [Test]
        public void CreatePropertyGUIWithEmptyObjectReferenceOptionsReturnsHelpBox()
        {
            WValueDropDownEmptyObjectReferenceAsset asset =
                CreateScriptableObject<WValueDropDownEmptyObjectReferenceAsset>();

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownEmptyObjectReferenceAsset.emptySelection)
            );
            Assert.IsNotNull(property, "Failed to locate emptySelection property.");

            WValueDropDownDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(
                drawer,
                new WValueDropDownAttribute(
                    typeof(WValueDropDownObjectReferenceSource),
                    nameof(WValueDropDownObjectReferenceSource.GetEmptyScriptableObjects)
                )
            );
            VisualElement element = drawer.CreatePropertyGUI(property);
            Assert.IsInstanceOf<HelpBox>(element);
        }

        [Test]
        public void SelectorUpdatesObjectReferenceSerializedProperty()
        {
            WValueDropDownObjectReferenceAsset asset =
                CreateScriptableObject<WValueDropDownObjectReferenceAsset>();
            asset.availableOptions.Add(_testObject1);
            asset.availableOptions.Add(_testObject2);
            asset.availableOptions.Add(_testObject3);
            asset.selectedObject = _testObject1;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownObjectReferenceAsset.selectedObject)
            );
            Assert.IsNotNull(property, "Failed to locate selectedObject property.");

            WValueDropDownDrawer drawer = new();
            WValueDropDownAttribute attribute = new(
                nameof(WValueDropDownObjectReferenceAsset.GetAvailableScriptableObjects),
                typeof(WValueDropDownTestScriptableObject)
            );
            PropertyDrawerTestHelper.AssignAttribute(drawer, attribute);
            VisualElement element = drawer.CreatePropertyGUI(property);
            Assert.IsInstanceOf<BaseField<string>>(element);

            BaseField<string> selector = (BaseField<string>)element;
            DropdownField dropdown = selector.Q<DropdownField>();
            Assert.IsNotNull(dropdown, "Dropdown field was not created.");
            Assert.That(dropdown.value, Is.EqualTo("TestObject1"));

            WDropDownSelectorBase<string> dropDownSelector =
                selector as WDropDownSelectorBase<string>;
            Assert.IsNotNull(
                dropDownSelector,
                "Expected selector to derive from WDropDownSelectorBase<string>."
            );
            dropDownSelector.ApplySelection(2);
            serializedObject.Update();
            Assert.That(asset.selectedObject, Is.SameAs(_testObject3));
        }

        [Test]
        public void ObjectReferenceDropdownDisplaysObjectNames()
        {
            WValueDropDownObjectReferenceAsset asset =
                CreateScriptableObject<WValueDropDownObjectReferenceAsset>();
            asset.availableOptions.Add(_testObject1);
            asset.availableOptions.Add(_testObject2);
            asset.selectedObject = _testObject2;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownObjectReferenceAsset.selectedObject)
            );
            Assert.IsNotNull(property, "Failed to locate selectedObject property.");

            WValueDropDownDrawer drawer = new();
            WValueDropDownAttribute attribute = new(
                nameof(WValueDropDownObjectReferenceAsset.GetAvailableScriptableObjects),
                typeof(WValueDropDownTestScriptableObject)
            );
            PropertyDrawerTestHelper.AssignAttribute(drawer, attribute);
            VisualElement element = drawer.CreatePropertyGUI(property);
            Assert.IsInstanceOf<BaseField<string>>(element);

            BaseField<string> selector = (BaseField<string>)element;
            DropdownField dropdown = selector.Q<DropdownField>();
            Assert.IsNotNull(dropdown, "Dropdown field was not created.");

            Assert.That(dropdown.value, Is.EqualTo("TestObject2"));
        }

        [Test]
        public void NullObjectReferenceDisplaysNoneInDropdown()
        {
            WValueDropDownNullableObjectReferenceAsset asset =
                CreateScriptableObject<WValueDropDownNullableObjectReferenceAsset>();
            asset.availableOptions.Add(_testObject1);
            asset.selectedObjectOrNull = null;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownNullableObjectReferenceAsset.selectedObjectOrNull)
            );
            Assert.IsNotNull(property, "Failed to locate selectedObjectOrNull property.");

            WValueDropDownDrawer drawer = new();
            WValueDropDownAttribute attribute = new(
                nameof(WValueDropDownNullableObjectReferenceAsset.GetOptionsIncludingNull),
                typeof(WValueDropDownTestScriptableObject)
            );
            PropertyDrawerTestHelper.AssignAttribute(drawer, attribute);
            VisualElement element = drawer.CreatePropertyGUI(property);
            Assert.IsInstanceOf<BaseField<string>>(element);

            BaseField<string> selector = (BaseField<string>)element;
            DropdownField dropdown = selector.Q<DropdownField>();
            Assert.IsNotNull(dropdown, "Dropdown field was not created.");

            Assert.That(dropdown.value, Is.EqualTo("(null)"));
        }

        [Test]
        public void GenericUnityObjectFieldIsSupported()
        {
            WValueDropDownGenericObjectAsset asset =
                CreateScriptableObject<WValueDropDownGenericObjectAsset>();
            asset.availableObjects.Add(_testObject1);
            asset.availableObjects.Add(_testObject2);
            asset.selectedObject = _testObject1;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownGenericObjectAsset.selectedObject)
            );
            Assert.IsNotNull(property, "Failed to locate selectedObject property.");

            WValueDropDownDrawer drawer = new();
            WValueDropDownAttribute attribute = new(
                nameof(WValueDropDownGenericObjectAsset.GetAvailableObjects),
                typeof(UnityEngine.Object)
            );
            PropertyDrawerTestHelper.AssignAttribute(drawer, attribute);
            VisualElement element = drawer.CreatePropertyGUI(property);
            Assert.IsInstanceOf<BaseField<string>>(element);

            BaseField<string> selector = (BaseField<string>)element;
            DropdownField dropdown = selector.Q<DropdownField>();
            Assert.IsNotNull(dropdown, "Dropdown field was not created.");

            WDropDownSelectorBase<string> dropDownSelector =
                selector as WDropDownSelectorBase<string>;
            Assert.IsNotNull(
                dropDownSelector,
                "Expected selector to derive from WDropDownSelectorBase<string>."
            );
            dropDownSelector.ApplySelection(1);
            serializedObject.Update();
            Assert.That(asset.selectedObject, Is.SameAs(_testObject2));
        }

        [Test]
        public void ObjectReferencePropertyTypeIsSupported()
        {
            WValueDropDownObjectReferenceAsset asset =
                CreateScriptableObject<WValueDropDownObjectReferenceAsset>();

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownObjectReferenceAsset.selectedObject)
            );
            Assert.IsNotNull(property, "Failed to locate selectedObject property.");
            Assert.That(property.propertyType, Is.EqualTo(SerializedPropertyType.ObjectReference));
        }

        [Test]
        public void MaterialObjectReferenceFieldIsSupported()
        {
            Material material1 = new(Shader.Find("Standard"));
            Material material2 = new(Shader.Find("Standard"));
            material1.name = "TestMaterial1";
            material2.name = "TestMaterial2";
            Track(material1);
            Track(material2);

            WValueDropDownObjectReferenceSource.SetStaticMaterials(new[] { material1, material2 });

            WValueDropDownMaterialAsset asset =
                CreateScriptableObject<WValueDropDownMaterialAsset>();
            asset.selectedMaterial = null;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownMaterialAsset.selectedMaterial)
            );
            Assert.IsNotNull(property, "Failed to locate selectedMaterial property.");

            WValueDropDownDrawer.ApplyOption(property, material1);
            serializedObject.ApplyModifiedProperties();

            Assert.That(asset.selectedMaterial, Is.SameAs(material1));
        }

        [Test]
        public void ObjectReferenceMatchingUsesReferenceEquality()
        {
            WValueDropDownObjectReferenceAsset asset =
                CreateScriptableObject<WValueDropDownObjectReferenceAsset>();

            WValueDropDownTestScriptableObject duplicate =
                CreateScriptableObject<WValueDropDownTestScriptableObject>();
            duplicate.name = "TestObject1";
            duplicate.displayValue = "First Object";
            duplicate.identifier = 1;

            asset.availableOptions.Add(_testObject1);
            asset.availableOptions.Add(duplicate);
            asset.selectedObject = duplicate;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownObjectReferenceAsset.selectedObject)
            );
            Assert.IsNotNull(property, "Failed to locate selectedObject property.");

            WValueDropDownDrawer drawer = new();
            WValueDropDownAttribute attribute = new(
                nameof(WValueDropDownObjectReferenceAsset.GetAvailableScriptableObjects),
                typeof(WValueDropDownTestScriptableObject)
            );
            PropertyDrawerTestHelper.AssignAttribute(drawer, attribute);
            VisualElement element = drawer.CreatePropertyGUI(property);
            Assert.IsInstanceOf<BaseField<string>>(element);

            BaseField<string> selector = (BaseField<string>)element;
            DropdownField dropdown = selector.Q<DropdownField>();
            Assert.IsNotNull(dropdown, "Dropdown field was not created.");

            Assert.That(
                dropdown.value,
                Is.EqualTo("TestObject1"),
                "Should match the duplicate (second item) by reference"
            );
        }

        [Test]
        public void ChangingSelectionUpdatesObjectReference()
        {
            WValueDropDownObjectReferenceAsset asset =
                CreateScriptableObject<WValueDropDownObjectReferenceAsset>();
            asset.availableOptions.Add(_testObject1);
            asset.availableOptions.Add(_testObject2);
            asset.availableOptions.Add(_testObject3);
            asset.selectedObject = _testObject1;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownObjectReferenceAsset.selectedObject)
            );

            Assert.That(asset.selectedObject, Is.SameAs(_testObject1));

            WValueDropDownDrawer.ApplyOption(property, _testObject2);
            serializedObject.ApplyModifiedProperties();
            Assert.That(asset.selectedObject, Is.SameAs(_testObject2));

            WValueDropDownDrawer.ApplyOption(property, _testObject3);
            serializedObject.ApplyModifiedProperties();
            Assert.That(asset.selectedObject, Is.SameAs(_testObject3));

            WValueDropDownDrawer.ApplyOption(property, _testObject1);
            serializedObject.ApplyModifiedProperties();
            Assert.That(asset.selectedObject, Is.SameAs(_testObject1));
        }

        [Test]
        public void ObjectReferenceWithoutNameUsesTypeName()
        {
            WValueDropDownTestScriptableObject unnamedObject =
                CreateScriptableObject<WValueDropDownTestScriptableObject>();
            unnamedObject.name = string.Empty;

            WValueDropDownObjectReferenceAsset asset =
                CreateScriptableObject<WValueDropDownObjectReferenceAsset>();
            asset.availableOptions.Add(unnamedObject);
            asset.selectedObject = unnamedObject;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownObjectReferenceAsset.selectedObject)
            );
            Assert.IsNotNull(property, "Failed to locate selectedObject property.");

            WValueDropDownDrawer drawer = new();
            WValueDropDownAttribute attribute = new(
                nameof(WValueDropDownObjectReferenceAsset.GetAvailableScriptableObjects),
                typeof(WValueDropDownTestScriptableObject)
            );
            PropertyDrawerTestHelper.AssignAttribute(drawer, attribute);
            VisualElement element = drawer.CreatePropertyGUI(property);
            Assert.IsInstanceOf<BaseField<string>>(element);

            BaseField<string> selector = (BaseField<string>)element;
            DropdownField dropdown = selector.Q<DropdownField>();
            Assert.IsNotNull(dropdown, "Dropdown field was not created.");

            Assert.That(
                dropdown.value,
                Is.EqualTo(nameof(WValueDropDownTestScriptableObject)),
                "Unnamed object should display type name"
            );
        }

        [Test]
        public void ApplyOptionIgnoresNonUnityObjectForObjectReferenceProperty()
        {
            WValueDropDownObjectReferenceAsset asset =
                CreateScriptableObject<WValueDropDownObjectReferenceAsset>();
            asset.selectedObject = _testObject1;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownObjectReferenceAsset.selectedObject)
            );
            Assert.IsNotNull(property, "Failed to locate selectedObject property.");

            WValueDropDownDrawer.ApplyOption(property, "not a unity object");
            serializedObject.ApplyModifiedProperties();

            Assert.That(
                asset.selectedObject,
                Is.SameAs(_testObject1),
                "Should not change when applying non-UnityObject value"
            );
        }

        [Test]
        public void MultipleObjectReferenceFieldsWorkIndependently()
        {
            WValueDropDownTestScriptableObject[] staticObjects = new[]
            {
                _testObject1,
                _testObject2,
            };
            WValueDropDownObjectReferenceSource.SetStaticScriptableObjects(staticObjects);

            WValueDropDownObjectReferenceAsset asset =
                CreateScriptableObject<WValueDropDownObjectReferenceAsset>();
            asset.availableOptions.Add(_testObject3);
            asset.selectedObject = null;
            asset.staticSelectedObject = null;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty instanceProperty = serializedObject.FindProperty(
                nameof(WValueDropDownObjectReferenceAsset.selectedObject)
            );
            SerializedProperty staticProperty = serializedObject.FindProperty(
                nameof(WValueDropDownObjectReferenceAsset.staticSelectedObject)
            );

            WValueDropDownDrawer.ApplyOption(instanceProperty, _testObject3);
            WValueDropDownDrawer.ApplyOption(staticProperty, _testObject1);
            serializedObject.ApplyModifiedProperties();

            Assert.That(asset.selectedObject, Is.SameAs(_testObject3));
            Assert.That(asset.staticSelectedObject, Is.SameAs(_testObject1));
        }

        [Test]
        public void ProviderTypeAndMethodNameAreStoredForObjectReferenceProvider()
        {
            WValueDropDownAttribute attribute = new(
                typeof(WValueDropDownObjectReferenceSource),
                nameof(WValueDropDownObjectReferenceSource.GetStaticScriptableObjects)
            );
            Assert.That(
                attribute.ProviderType,
                Is.EqualTo(typeof(WValueDropDownObjectReferenceSource))
            );
            Assert.That(
                attribute.ProviderMethodName,
                Is.EqualTo(nameof(WValueDropDownObjectReferenceSource.GetStaticScriptableObjects))
            );
        }

        [Test]
        public void InstanceMethodSetsProviderMethodNameForObjectReference()
        {
            WValueDropDownAttribute attribute = new(
                nameof(WValueDropDownObjectReferenceAsset.GetAvailableScriptableObjects),
                typeof(WValueDropDownTestScriptableObject)
            );
            Assert.That(attribute.ProviderType, Is.Null);
            Assert.That(
                attribute.ProviderMethodName,
                Is.EqualTo(nameof(WValueDropDownObjectReferenceAsset.GetAvailableScriptableObjects))
            );
            Assert.That(attribute.RequiresInstanceContext, Is.True);
        }
    }
#endif
}
