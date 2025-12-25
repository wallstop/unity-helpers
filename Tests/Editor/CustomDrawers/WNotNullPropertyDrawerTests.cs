namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using System.Collections;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.TestTools;
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes;
    using WallstopStudios.UnityHelpers.Tests.EditorFramework;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;

    [TestFixture]
    public sealed class WNotNullPropertyDrawerTests : CommonTestBase
    {
        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            WNotNullPropertyDrawer.ClearHeightCache();
        }

        [Test]
        public void IsPropertyNullReturnsTrueForNullObjectReference()
        {
            WNotNullObjectReferenceTestAsset asset =
                CreateScriptableObject<WNotNullObjectReferenceTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullObjectReferenceTestAsset.requiredGameObject)
            );
            Assert.IsNotNull(property, "Failed to locate requiredGameObject property.");
            Assert.IsTrue(WNotNullPropertyDrawer.IsPropertyNull(property));
        }

        [Test]
        public void IsPropertyNullReturnsFalseForAssignedObjectReference()
        {
            WNotNullObjectReferenceTestAsset asset =
                CreateScriptableObject<WNotNullObjectReferenceTestAsset>();
            asset.requiredGameObject = NewGameObject("TestObject");
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullObjectReferenceTestAsset.requiredGameObject)
            );
            Assert.IsNotNull(property, "Failed to locate requiredGameObject property.");
            Assert.IsFalse(WNotNullPropertyDrawer.IsPropertyNull(property));
        }

        [Test]
        public void IsPropertyNullReturnsTrueForNullTransformReference()
        {
            WNotNullObjectReferenceTestAsset asset =
                CreateScriptableObject<WNotNullObjectReferenceTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullObjectReferenceTestAsset.requiredTransform)
            );
            Assert.IsNotNull(property, "Failed to locate requiredTransform property.");
            Assert.IsTrue(WNotNullPropertyDrawer.IsPropertyNull(property));
        }

        [Test]
        public void IsPropertyNullReturnsFalseForAssignedTransformReference()
        {
            WNotNullObjectReferenceTestAsset asset =
                CreateScriptableObject<WNotNullObjectReferenceTestAsset>();
            asset.requiredTransform = NewGameObject("TestTransform").transform;
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullObjectReferenceTestAsset.requiredTransform)
            );
            Assert.IsNotNull(property, "Failed to locate requiredTransform property.");
            Assert.IsFalse(WNotNullPropertyDrawer.IsPropertyNull(property));
        }

        [Test]
        public void IsPropertyNullReturnsTrueForEmptyString()
        {
            WNotNullStringTestAsset asset = CreateScriptableObject<WNotNullStringTestAsset>();
            asset.requiredString = string.Empty;
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullStringTestAsset.requiredString)
            );
            Assert.IsNotNull(property, "Failed to locate requiredString property.");
            Assert.IsTrue(WNotNullPropertyDrawer.IsPropertyNull(property));
        }

        [Test]
        public void IsPropertyNullReturnsTrueForNullString()
        {
            WNotNullStringTestAsset asset = CreateScriptableObject<WNotNullStringTestAsset>();
            asset.requiredString = null;
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullStringTestAsset.requiredString)
            );
            Assert.IsNotNull(property, "Failed to locate requiredString property.");
            Assert.IsTrue(WNotNullPropertyDrawer.IsPropertyNull(property));
        }

        [Test]
        public void IsPropertyNullReturnsFalseForNonEmptyString()
        {
            WNotNullStringTestAsset asset = CreateScriptableObject<WNotNullStringTestAsset>();
            asset.requiredString = "ValidValue";
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullStringTestAsset.requiredString)
            );
            Assert.IsNotNull(property, "Failed to locate requiredString property.");
            Assert.IsFalse(WNotNullPropertyDrawer.IsPropertyNull(property));
        }

        [Test]
        public void CreatePropertyGUIReturnsContainerWithHelpBoxForNullField()
        {
            WNotNullObjectReferenceTestAsset asset =
                CreateScriptableObject<WNotNullObjectReferenceTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullObjectReferenceTestAsset.requiredGameObject)
            );
            Assert.IsNotNull(property, "Failed to locate requiredGameObject property.");

            WNotNullPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(drawer, new WNotNullAttribute());
            VisualElement element = drawer.CreatePropertyGUI(property);

            Assert.IsNotNull(element, "CreatePropertyGUI should return a non-null element.");
            HelpBox helpBox = element.Q<HelpBox>();
            Assert.IsNotNull(helpBox, "Container should contain a HelpBox.");
            Assert.That(helpBox.style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        [Test]
        public void CreatePropertyGUIHidesHelpBoxForAssignedField()
        {
            WNotNullObjectReferenceTestAsset asset =
                CreateScriptableObject<WNotNullObjectReferenceTestAsset>();
            asset.requiredGameObject = NewGameObject("TestObject");
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullObjectReferenceTestAsset.requiredGameObject)
            );
            Assert.IsNotNull(property, "Failed to locate requiredGameObject property.");

            WNotNullPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(drawer, new WNotNullAttribute());
            VisualElement element = drawer.CreatePropertyGUI(property);

            Assert.IsNotNull(element, "CreatePropertyGUI should return a non-null element.");
            HelpBox helpBox = element.Q<HelpBox>();
            Assert.IsNotNull(helpBox, "Container should contain a HelpBox.");
            Assert.That(helpBox.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [Test]
        public void CreatePropertyGUIIncludesPropertyField()
        {
            WNotNullObjectReferenceTestAsset asset =
                CreateScriptableObject<WNotNullObjectReferenceTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullObjectReferenceTestAsset.requiredGameObject)
            );
            Assert.IsNotNull(property, "Failed to locate requiredGameObject property.");

            WNotNullPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(drawer, new WNotNullAttribute());
            VisualElement element = drawer.CreatePropertyGUI(property);

            PropertyField propertyField = element.Q<PropertyField>();
            Assert.IsNotNull(propertyField, "Container should include a PropertyField.");
        }

        [Test]
        public void CreatePropertyGUIUsesWarningMessageTypeByDefault()
        {
            WNotNullMessageTypeTestAsset asset =
                CreateScriptableObject<WNotNullMessageTypeTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullMessageTypeTestAsset.defaultField)
            );
            Assert.IsNotNull(property, "Failed to locate defaultField property.");

            WNotNullPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(drawer, new WNotNullAttribute());
            VisualElement element = drawer.CreatePropertyGUI(property);

            HelpBox helpBox = element.Q<HelpBox>();
            Assert.IsNotNull(helpBox, "Container should contain a HelpBox.");
            Assert.That(helpBox.messageType, Is.EqualTo(HelpBoxMessageType.Warning));
        }

        [Test]
        public void CreatePropertyGUIUsesErrorMessageTypeWhenSpecified()
        {
            WNotNullMessageTypeTestAsset asset =
                CreateScriptableObject<WNotNullMessageTypeTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullMessageTypeTestAsset.errorField)
            );
            Assert.IsNotNull(property, "Failed to locate errorField property.");

            WNotNullPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(
                drawer,
                new WNotNullAttribute(WNotNullMessageType.Error)
            );
            VisualElement element = drawer.CreatePropertyGUI(property);

            HelpBox helpBox = element.Q<HelpBox>();
            Assert.IsNotNull(helpBox, "Container should contain a HelpBox.");
            Assert.That(helpBox.messageType, Is.EqualTo(HelpBoxMessageType.Error));
        }

        [Test]
        public void CreatePropertyGUIUsesWarningMessageTypeWhenSpecified()
        {
            WNotNullMessageTypeTestAsset asset =
                CreateScriptableObject<WNotNullMessageTypeTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullMessageTypeTestAsset.warningField)
            );
            Assert.IsNotNull(property, "Failed to locate warningField property.");

            WNotNullPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(
                drawer,
                new WNotNullAttribute(WNotNullMessageType.Warning)
            );
            VisualElement element = drawer.CreatePropertyGUI(property);

            HelpBox helpBox = element.Q<HelpBox>();
            Assert.IsNotNull(helpBox, "Container should contain a HelpBox.");
            Assert.That(helpBox.messageType, Is.EqualTo(HelpBoxMessageType.Warning));
        }

        [Test]
        public void CreatePropertyGUIUsesCustomMessageWhenProvided()
        {
            WNotNullCustomMessageTestAsset asset =
                CreateScriptableObject<WNotNullCustomMessageTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullCustomMessageTestAsset.playerPrefab)
            );
            Assert.IsNotNull(property, "Failed to locate playerPrefab property.");

            string customMessage = "Player prefab is required for spawning";
            WNotNullPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(drawer, new WNotNullAttribute(customMessage));
            VisualElement element = drawer.CreatePropertyGUI(property);

            HelpBox helpBox = element.Q<HelpBox>();
            Assert.IsNotNull(helpBox, "Container should contain a HelpBox.");
            Assert.That(helpBox.text, Is.EqualTo(customMessage));
        }

        [Test]
        public void CreatePropertyGUIUsesCustomMessageWithErrorType()
        {
            WNotNullCustomMessageTestAsset asset =
                CreateScriptableObject<WNotNullCustomMessageTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullCustomMessageTestAsset.audioSource)
            );
            Assert.IsNotNull(property, "Failed to locate audioSource property.");

            string customMessage = "Audio source must be assigned for sound effects";
            WNotNullPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(
                drawer,
                new WNotNullAttribute(WNotNullMessageType.Error, customMessage)
            );
            VisualElement element = drawer.CreatePropertyGUI(property);

            HelpBox helpBox = element.Q<HelpBox>();
            Assert.IsNotNull(helpBox, "Container should contain a HelpBox.");
            Assert.That(helpBox.text, Is.EqualTo(customMessage));
            Assert.That(helpBox.messageType, Is.EqualTo(HelpBoxMessageType.Error));
        }

        [Test]
        public void CreatePropertyGUIGeneratesDefaultMessageForFieldName()
        {
            WNotNullObjectReferenceTestAsset asset =
                CreateScriptableObject<WNotNullObjectReferenceTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullObjectReferenceTestAsset.requiredGameObject)
            );
            Assert.IsNotNull(property, "Failed to locate requiredGameObject property.");

            WNotNullPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(drawer, new WNotNullAttribute());
            VisualElement element = drawer.CreatePropertyGUI(property);

            HelpBox helpBox = element.Q<HelpBox>();
            Assert.IsNotNull(helpBox, "Container should contain a HelpBox.");
            Assert.That(helpBox.text, Does.Contain("Required Game Object"));
        }

        [Test]
        public void GetPropertyHeightIsGreaterWhenFieldIsNull()
        {
            WNotNullObjectReferenceTestAsset asset =
                CreateScriptableObject<WNotNullObjectReferenceTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty nullProperty = serializedObject.FindProperty(
                nameof(WNotNullObjectReferenceTestAsset.requiredGameObject)
            );
            Assert.IsNotNull(nullProperty, "Failed to locate requiredGameObject property.");

            WNotNullPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(drawer, new WNotNullAttribute());
            GUIContent label = new("Test");

            float nullHeight = drawer.GetPropertyHeight(nullProperty, label);

            asset.requiredGameObject = NewGameObject("TestObject");
            serializedObject.Update();

            SerializedProperty assignedProperty = serializedObject.FindProperty(
                nameof(WNotNullObjectReferenceTestAsset.requiredGameObject)
            );
            float assignedHeight = drawer.GetPropertyHeight(assignedProperty, label);

            Assert.That(
                nullHeight,
                Is.GreaterThan(assignedHeight),
                "Height should be greater when field is null to accommodate help box."
            );
        }

        [UnityTest]
        public IEnumerator OnGUIRendersHelpBoxForNullField()
        {
            WNotNullObjectReferenceTestAsset asset =
                CreateScriptableObject<WNotNullObjectReferenceTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullObjectReferenceTestAsset.requiredGameObject)
            );
            Assert.IsNotNull(property, "Failed to locate requiredGameObject property.");

            WNotNullPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(drawer, new WNotNullAttribute());
            GUIContent label = new("Required Game Object");
            float height = drawer.GetPropertyHeight(property, label);
            Rect position = new(0f, 0f, 400f, height);

            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(position, property, label);
            });
        }

        [UnityTest]
        public IEnumerator OnGUIRendersPropertyFieldForAssignedField()
        {
            WNotNullObjectReferenceTestAsset asset =
                CreateScriptableObject<WNotNullObjectReferenceTestAsset>();
            asset.requiredGameObject = NewGameObject("TestObject");
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullObjectReferenceTestAsset.requiredGameObject)
            );
            Assert.IsNotNull(property, "Failed to locate requiredGameObject property.");

            WNotNullPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(drawer, new WNotNullAttribute());
            GUIContent label = new("Required Game Object");
            float height = drawer.GetPropertyHeight(property, label);
            Rect position = new(0f, 0f, 400f, height);

            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(position, property, label);
            });
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

            WNotNullAttribute errorAttribute = new(WNotNullMessageType.Error);
            Assert.That(errorAttribute.MessageType, Is.EqualTo(WNotNullMessageType.Error));
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
        public void IsPropertyNullReturnsFalseForIntProperty()
        {
            WNotNullMixedFieldsTestAsset asset =
                CreateScriptableObject<WNotNullMixedFieldsTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullMixedFieldsTestAsset.nonDecoratedIntField)
            );
            Assert.IsNotNull(property, "Failed to locate nonDecoratedIntField property.");
            Assert.IsFalse(WNotNullPropertyDrawer.IsPropertyNull(property));
        }

        [Test]
        public void IsPropertyNullReturnsTrueForNullSprite()
        {
            WNotNullMixedFieldsTestAsset asset =
                CreateScriptableObject<WNotNullMixedFieldsTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullMixedFieldsTestAsset.nullableSprite)
            );
            Assert.IsNotNull(property, "Failed to locate nullableSprite property.");
            Assert.IsTrue(WNotNullPropertyDrawer.IsPropertyNull(property));
        }

        [Test]
        public void IsPropertyNullReturnsTrueForNullAudioClip()
        {
            WNotNullMixedFieldsTestAsset asset =
                CreateScriptableObject<WNotNullMixedFieldsTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullMixedFieldsTestAsset.nullableAudioClip)
            );
            Assert.IsNotNull(property, "Failed to locate nullableAudioClip property.");
            Assert.IsTrue(WNotNullPropertyDrawer.IsPropertyNull(property));
        }

        [Test]
        public void IsPropertyNullReturnsTrueForNullScriptableObject()
        {
            WNotNullObjectReferenceTestAsset asset =
                CreateScriptableObject<WNotNullObjectReferenceTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullObjectReferenceTestAsset.requiredScriptableObject)
            );
            Assert.IsNotNull(property, "Failed to locate requiredScriptableObject property.");
            Assert.IsTrue(WNotNullPropertyDrawer.IsPropertyNull(property));
        }

        [Test]
        public void IsPropertyNullReturnsFalseForAssignedScriptableObject()
        {
            WNotNullObjectReferenceTestAsset asset =
                CreateScriptableObject<WNotNullObjectReferenceTestAsset>();
            asset.requiredScriptableObject = CreateScriptableObject<ScriptableObject>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullObjectReferenceTestAsset.requiredScriptableObject)
            );
            Assert.IsNotNull(property, "Failed to locate requiredScriptableObject property.");
            Assert.IsFalse(WNotNullPropertyDrawer.IsPropertyNull(property));
        }

        [Test]
        public void IsPropertyNullReturnsTrueForNullMaterial()
        {
            WNotNullObjectReferenceTestAsset asset =
                CreateScriptableObject<WNotNullObjectReferenceTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullObjectReferenceTestAsset.requiredMaterial)
            );
            Assert.IsNotNull(property, "Failed to locate requiredMaterial property.");
            Assert.IsTrue(WNotNullPropertyDrawer.IsPropertyNull(property));
        }

        [Test]
        public void ClearHeightCacheClearsCache()
        {
            WNotNullObjectReferenceTestAsset asset =
                CreateScriptableObject<WNotNullObjectReferenceTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(WNotNullObjectReferenceTestAsset.requiredGameObject)
            );

            WNotNullPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(drawer, new WNotNullAttribute());
            GUIContent label = new("Test");

            drawer.GetPropertyHeight(property, label);

            WNotNullPropertyDrawer.ClearHeightCache();

            drawer.GetPropertyHeight(property, label);
        }
    }
#endif
}
