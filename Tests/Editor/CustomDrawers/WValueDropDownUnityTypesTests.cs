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
    /// Tests for WValueDropDown with Unity built-in types (Vector2, Vector3, Color, Rect, etc.)
    /// to verify the generic reflection-based approach works for any serializable type.
    /// </summary>
    [TestFixture]
    [NUnit.Framework.Category("Slow")]
    [NUnit.Framework.Category("Integration")]
    public sealed class WValueDropDownUnityTypesTests : CommonTestBase
    {
        [TearDown]
        public override void TearDown()
        {
            WValueDropDownUnityTypesSource.Clear();
            base.TearDown();
        }

        [Test]
        public void ApplyOptionUpdatesVector2SerializedProperty()
        {
            WValueDropDownUnityTypesAsset asset =
                CreateScriptableObject<WValueDropDownUnityTypesAsset>();
            Vector2 target = new Vector2(5f, 10f);
            asset.vector2Options.Add(Vector2.zero);
            asset.vector2Options.Add(target);
            asset.selectedVector2 = Vector2.zero;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownUnityTypesAsset.selectedVector2)
            );
            Assert.IsNotNull(property, "Failed to locate Vector2 property.");

            WValueDropDownDrawer.ApplyOption(property, target);
            serializedObject.ApplyModifiedProperties();

            Assert.That(asset.selectedVector2, Is.EqualTo(target));
        }

        [Test]
        public void ApplyOptionUpdatesVector3SerializedProperty()
        {
            WValueDropDownUnityTypesAsset asset =
                CreateScriptableObject<WValueDropDownUnityTypesAsset>();
            Vector3 target = new Vector3(1f, 2f, 3f);
            asset.vector3Options.Add(Vector3.zero);
            asset.vector3Options.Add(target);
            asset.selectedVector3 = Vector3.zero;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownUnityTypesAsset.selectedVector3)
            );
            Assert.IsNotNull(property, "Failed to locate Vector3 property.");

            WValueDropDownDrawer.ApplyOption(property, target);
            serializedObject.ApplyModifiedProperties();

            Assert.That(asset.selectedVector3, Is.EqualTo(target));
        }

        [Test]
        public void ApplyOptionUpdatesColorSerializedProperty()
        {
            WValueDropDownUnityTypesAsset asset =
                CreateScriptableObject<WValueDropDownUnityTypesAsset>();
            Color target = Color.red;
            asset.colorOptions.Add(Color.white);
            asset.colorOptions.Add(target);
            asset.selectedColor = Color.white;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownUnityTypesAsset.selectedColor)
            );
            Assert.IsNotNull(property, "Failed to locate Color property.");

            WValueDropDownDrawer.ApplyOption(property, target);
            serializedObject.ApplyModifiedProperties();

            Assert.That(asset.selectedColor, Is.EqualTo(target));
        }

        [Test]
        public void ApplyOptionUpdatesRectSerializedProperty()
        {
            WValueDropDownUnityTypesAsset asset =
                CreateScriptableObject<WValueDropDownUnityTypesAsset>();
            Rect target = new Rect(10, 20, 100, 200);
            asset.rectOptions.Add(Rect.zero);
            asset.rectOptions.Add(target);
            asset.selectedRect = Rect.zero;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownUnityTypesAsset.selectedRect)
            );
            Assert.IsNotNull(property, "Failed to locate Rect property.");

            WValueDropDownDrawer.ApplyOption(property, target);
            serializedObject.ApplyModifiedProperties();

            Assert.That(asset.selectedRect, Is.EqualTo(target));
        }

        [Test]
        public void ApplyOptionUpdatesVector2IntSerializedProperty()
        {
            WValueDropDownUnityTypesAsset asset =
                CreateScriptableObject<WValueDropDownUnityTypesAsset>();
            Vector2Int target = new Vector2Int(5, 10);
            asset.vector2IntOptions.Add(Vector2Int.zero);
            asset.vector2IntOptions.Add(target);
            asset.selectedVector2Int = Vector2Int.zero;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownUnityTypesAsset.selectedVector2Int)
            );
            Assert.IsNotNull(property, "Failed to locate Vector2Int property.");

            WValueDropDownDrawer.ApplyOption(property, target);
            serializedObject.ApplyModifiedProperties();

            Assert.That(asset.selectedVector2Int, Is.EqualTo(target));
        }

        [Test]
        public void ApplyOptionUpdatesBoundsSerializedProperty()
        {
            WValueDropDownUnityTypesAsset asset =
                CreateScriptableObject<WValueDropDownUnityTypesAsset>();
            Bounds target = new Bounds(Vector3.one, Vector3.one * 2);
            asset.boundsOptions.Add(new Bounds());
            asset.boundsOptions.Add(target);
            asset.selectedBounds = new Bounds();

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownUnityTypesAsset.selectedBounds)
            );
            Assert.IsNotNull(property, "Failed to locate Bounds property.");

            WValueDropDownDrawer.ApplyOption(property, target);
            serializedObject.ApplyModifiedProperties();

            Assert.That(asset.selectedBounds, Is.EqualTo(target));
        }

        [Test]
        public void SelectorUpdatesVector2SerializedProperty()
        {
            WValueDropDownUnityTypesAsset asset =
                CreateScriptableObject<WValueDropDownUnityTypesAsset>();
            Vector2 v1 = new Vector2(1f, 1f);
            Vector2 v2 = new Vector2(2f, 2f);
            Vector2 v3 = new Vector2(3f, 3f);
            asset.vector2Options.Add(v1);
            asset.vector2Options.Add(v2);
            asset.vector2Options.Add(v3);
            asset.selectedVector2 = v1;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownUnityTypesAsset.selectedVector2)
            );
            Assert.IsNotNull(property, "Failed to locate Vector2 property.");

            WValueDropDownDrawer drawer = new();
            WValueDropDownAttribute attribute = new(
                nameof(WValueDropDownUnityTypesAsset.GetVector2Options),
                typeof(Vector2)
            );
            PropertyDrawerTestHelper.AssignAttribute(drawer, attribute);
            VisualElement element = drawer.CreatePropertyGUI(property);
            Assert.IsInstanceOf<BaseField<string>>(element);

            BaseField<string> selector = (BaseField<string>)element;
            WDropDownSelectorBase<string> dropDownSelector =
                selector as WDropDownSelectorBase<string>;
            Assert.IsNotNull(
                dropDownSelector,
                "Expected selector to derive from WDropDownSelectorBase<string>."
            );
            dropDownSelector.ApplySelection(2);
            serializedObject.Update();
            Assert.That(asset.selectedVector2, Is.EqualTo(v3));
        }

        [Test]
        public void SelectorUpdatesColorSerializedProperty()
        {
            WValueDropDownUnityTypesAsset asset =
                CreateScriptableObject<WValueDropDownUnityTypesAsset>();
            asset.colorOptions.Add(Color.red);
            asset.colorOptions.Add(Color.green);
            asset.colorOptions.Add(Color.blue);
            asset.selectedColor = Color.red;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownUnityTypesAsset.selectedColor)
            );
            Assert.IsNotNull(property, "Failed to locate Color property.");

            WValueDropDownDrawer drawer = new();
            WValueDropDownAttribute attribute = new(
                nameof(WValueDropDownUnityTypesAsset.GetColorOptions),
                typeof(Color)
            );
            PropertyDrawerTestHelper.AssignAttribute(drawer, attribute);
            VisualElement element = drawer.CreatePropertyGUI(property);
            Assert.IsInstanceOf<BaseField<string>>(element);

            BaseField<string> selector = (BaseField<string>)element;
            WDropDownSelectorBase<string> dropDownSelector =
                selector as WDropDownSelectorBase<string>;
            Assert.IsNotNull(
                dropDownSelector,
                "Expected selector to derive from WDropDownSelectorBase<string>."
            );
            dropDownSelector.ApplySelection(1);
            serializedObject.Update();
            Assert.That(asset.selectedColor, Is.EqualTo(Color.green));
        }

        [Test]
        public void Vector2PropertyTypeIsSupported()
        {
            WValueDropDownUnityTypesAsset asset =
                CreateScriptableObject<WValueDropDownUnityTypesAsset>();

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownUnityTypesAsset.selectedVector2)
            );
            Assert.IsNotNull(property, "Failed to locate Vector2 property.");
            Assert.That(property.propertyType, Is.EqualTo(SerializedPropertyType.Vector2));
        }

        [Test]
        public void Vector3PropertyTypeIsSupported()
        {
            WValueDropDownUnityTypesAsset asset =
                CreateScriptableObject<WValueDropDownUnityTypesAsset>();

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownUnityTypesAsset.selectedVector3)
            );
            Assert.IsNotNull(property, "Failed to locate Vector3 property.");
            Assert.That(property.propertyType, Is.EqualTo(SerializedPropertyType.Vector3));
        }

        [Test]
        public void ColorPropertyTypeIsSupported()
        {
            WValueDropDownUnityTypesAsset asset =
                CreateScriptableObject<WValueDropDownUnityTypesAsset>();

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownUnityTypesAsset.selectedColor)
            );
            Assert.IsNotNull(property, "Failed to locate Color property.");
            Assert.That(property.propertyType, Is.EqualTo(SerializedPropertyType.Color));
        }

        [Test]
        public void RectPropertyTypeIsSupported()
        {
            WValueDropDownUnityTypesAsset asset =
                CreateScriptableObject<WValueDropDownUnityTypesAsset>();

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownUnityTypesAsset.selectedRect)
            );
            Assert.IsNotNull(property, "Failed to locate Rect property.");
            Assert.That(property.propertyType, Is.EqualTo(SerializedPropertyType.Rect));
        }

        [Test]
        public void BoundsPropertyTypeIsSupported()
        {
            WValueDropDownUnityTypesAsset asset =
                CreateScriptableObject<WValueDropDownUnityTypesAsset>();

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownUnityTypesAsset.selectedBounds)
            );
            Assert.IsNotNull(property, "Failed to locate Bounds property.");
            Assert.That(property.propertyType, Is.EqualTo(SerializedPropertyType.Bounds));
        }

        [Test]
        public void ChangingVector2SelectionUpdatesProperty()
        {
            WValueDropDownUnityTypesAsset asset =
                CreateScriptableObject<WValueDropDownUnityTypesAsset>();
            Vector2 v1 = new Vector2(1f, 1f);
            Vector2 v2 = new Vector2(2f, 2f);
            Vector2 v3 = new Vector2(3f, 3f);
            asset.vector2Options.Add(v1);
            asset.vector2Options.Add(v2);
            asset.vector2Options.Add(v3);
            asset.selectedVector2 = v1;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownUnityTypesAsset.selectedVector2)
            );

            Assert.That(asset.selectedVector2, Is.EqualTo(v1));

            WValueDropDownDrawer.ApplyOption(property, v2);
            serializedObject.ApplyModifiedProperties();
            Assert.That(asset.selectedVector2, Is.EqualTo(v2));

            WValueDropDownDrawer.ApplyOption(property, v3);
            serializedObject.ApplyModifiedProperties();
            Assert.That(asset.selectedVector2, Is.EqualTo(v3));

            WValueDropDownDrawer.ApplyOption(property, v1);
            serializedObject.ApplyModifiedProperties();
            Assert.That(asset.selectedVector2, Is.EqualTo(v1));
        }

        [Test]
        public void InstanceMethodProviderReturnsVector2Values()
        {
            WValueDropDownUnityTypesAsset asset =
                CreateScriptableObject<WValueDropDownUnityTypesAsset>();
            Vector2 v1 = new Vector2(1f, 1f);
            Vector2 v2 = new Vector2(2f, 2f);
            asset.vector2Options.Add(v1);
            asset.vector2Options.Add(v2);

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownUnityTypesAsset.selectedVector2)
            );
            Assert.IsNotNull(property, "Failed to locate Vector2 property.");

            WValueDropDownAttribute attribute =
                PropertyDrawerTestHelper.GetAttributeFromProperty<WValueDropDownAttribute>(
                    property
                );
            Assert.IsNotNull(attribute, "Failed to retrieve attribute.");

            object[] options = attribute.GetOptions(asset);
            Assert.That(options.Length, Is.EqualTo(2));
            Assert.That(options[0], Is.EqualTo(v1));
            Assert.That(options[1], Is.EqualTo(v2));
        }

        [Test]
        public void Vector2DropdownDisplaysFormattedValues()
        {
            WValueDropDownUnityTypesAsset asset =
                CreateScriptableObject<WValueDropDownUnityTypesAsset>();
            Vector2 v1 = new Vector2(1f, 2f);
            asset.vector2Options.Add(v1);
            asset.selectedVector2 = v1;

            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WValueDropDownUnityTypesAsset.selectedVector2)
            );
            Assert.IsNotNull(property, "Failed to locate Vector2 property.");

            WValueDropDownDrawer drawer = new();
            WValueDropDownAttribute attribute = new(
                nameof(WValueDropDownUnityTypesAsset.GetVector2Options),
                typeof(Vector2)
            );
            PropertyDrawerTestHelper.AssignAttribute(drawer, attribute);
            VisualElement element = drawer.CreatePropertyGUI(property);
            Assert.IsInstanceOf<BaseField<string>>(element);

            BaseField<string> selector = (BaseField<string>)element;
            DropdownField dropdown = selector.Q<DropdownField>();
            Assert.IsNotNull(dropdown, "Dropdown field was not created.");

            Assert.That(
                dropdown.value,
                Does.Contain("1").And.Contain("2"),
                "Dropdown should display Vector2 components"
            );
        }
    }
#endif
}
