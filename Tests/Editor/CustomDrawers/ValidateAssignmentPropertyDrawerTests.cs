namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
#if UNITY_EDITOR
    using System.Collections;
    using System.Collections.Generic;
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
    public sealed class ValidateAssignmentPropertyDrawerTests : CommonTestBase
    {
        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            ValidateAssignmentPropertyDrawer.ClearHeightCache();
        }

        [Test]
        public void IsPropertyInvalidReturnsTrueForNullObjectReference()
        {
            ValidateAssignmentObjectReferenceTestAsset asset =
                CreateScriptableObject<ValidateAssignmentObjectReferenceTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentObjectReferenceTestAsset.requiredGameObject)
            );
            Assert.IsNotNull(property, "Failed to locate requiredGameObject property.");
            Assert.IsTrue(ValidateAssignmentPropertyDrawer.IsPropertyInvalid(property));
        }

        [Test]
        public void IsPropertyInvalidReturnsFalseForAssignedObjectReference()
        {
            ValidateAssignmentObjectReferenceTestAsset asset =
                CreateScriptableObject<ValidateAssignmentObjectReferenceTestAsset>();
            asset.requiredGameObject = NewGameObject("TestObject");
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentObjectReferenceTestAsset.requiredGameObject)
            );
            Assert.IsNotNull(property, "Failed to locate requiredGameObject property.");
            Assert.IsFalse(ValidateAssignmentPropertyDrawer.IsPropertyInvalid(property));
        }

        [Test]
        public void IsPropertyInvalidReturnsTrueForNullTransformReference()
        {
            ValidateAssignmentObjectReferenceTestAsset asset =
                CreateScriptableObject<ValidateAssignmentObjectReferenceTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentObjectReferenceTestAsset.requiredTransform)
            );
            Assert.IsNotNull(property, "Failed to locate requiredTransform property.");
            Assert.IsTrue(ValidateAssignmentPropertyDrawer.IsPropertyInvalid(property));
        }

        [Test]
        public void IsPropertyInvalidReturnsFalseForAssignedTransformReference()
        {
            ValidateAssignmentObjectReferenceTestAsset asset =
                CreateScriptableObject<ValidateAssignmentObjectReferenceTestAsset>();
            asset.requiredTransform = NewGameObject("TestTransform").transform;
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentObjectReferenceTestAsset.requiredTransform)
            );
            Assert.IsNotNull(property, "Failed to locate requiredTransform property.");
            Assert.IsFalse(ValidateAssignmentPropertyDrawer.IsPropertyInvalid(property));
        }

        [Test]
        public void IsPropertyInvalidReturnsTrueForEmptyString()
        {
            ValidateAssignmentStringTestAsset asset =
                CreateScriptableObject<ValidateAssignmentStringTestAsset>();
            asset.requiredString = string.Empty;
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentStringTestAsset.requiredString)
            );
            Assert.IsNotNull(property, "Failed to locate requiredString property.");
            Assert.IsTrue(ValidateAssignmentPropertyDrawer.IsPropertyInvalid(property));
        }

        [Test]
        public void IsPropertyInvalidReturnsTrueForWhitespaceString()
        {
            ValidateAssignmentStringTestAsset asset =
                CreateScriptableObject<ValidateAssignmentStringTestAsset>();
            asset.requiredString = "   ";
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentStringTestAsset.requiredString)
            );
            Assert.IsNotNull(property, "Failed to locate requiredString property.");
            Assert.IsTrue(ValidateAssignmentPropertyDrawer.IsPropertyInvalid(property));
        }

        [Test]
        public void IsPropertyInvalidReturnsTrueForNullString()
        {
            ValidateAssignmentStringTestAsset asset =
                CreateScriptableObject<ValidateAssignmentStringTestAsset>();
            asset.requiredString = null;
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentStringTestAsset.requiredString)
            );
            Assert.IsNotNull(property, "Failed to locate requiredString property.");
            Assert.IsTrue(ValidateAssignmentPropertyDrawer.IsPropertyInvalid(property));
        }

        [Test]
        public void IsPropertyInvalidReturnsFalseForNonEmptyString()
        {
            ValidateAssignmentStringTestAsset asset =
                CreateScriptableObject<ValidateAssignmentStringTestAsset>();
            asset.requiredString = "ValidValue";
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentStringTestAsset.requiredString)
            );
            Assert.IsNotNull(property, "Failed to locate requiredString property.");
            Assert.IsFalse(ValidateAssignmentPropertyDrawer.IsPropertyInvalid(property));
        }

        [Test]
        public void IsPropertyInvalidReturnsTrueForEmptyList()
        {
            ValidateAssignmentCollectionTestAsset asset =
                CreateScriptableObject<ValidateAssignmentCollectionTestAsset>();
            asset.requiredList.Clear();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentCollectionTestAsset.requiredList)
            );
            Assert.IsNotNull(property, "Failed to locate requiredList property.");
            Assert.IsTrue(ValidateAssignmentPropertyDrawer.IsPropertyInvalid(property));
        }

        [Test]
        public void IsPropertyInvalidReturnsFalseForNonEmptyList()
        {
            ValidateAssignmentCollectionTestAsset asset =
                CreateScriptableObject<ValidateAssignmentCollectionTestAsset>();
            asset.requiredList.Add(42);
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentCollectionTestAsset.requiredList)
            );
            Assert.IsNotNull(property, "Failed to locate requiredList property.");
            Assert.IsFalse(ValidateAssignmentPropertyDrawer.IsPropertyInvalid(property));
        }

        [Test]
        public void IsPropertyInvalidReturnsTrueForEmptyArray()
        {
            ValidateAssignmentCollectionTestAsset asset =
                CreateScriptableObject<ValidateAssignmentCollectionTestAsset>();
            asset.requiredArray = new int[0];
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentCollectionTestAsset.requiredArray)
            );
            Assert.IsNotNull(property, "Failed to locate requiredArray property.");
            Assert.IsTrue(ValidateAssignmentPropertyDrawer.IsPropertyInvalid(property));
        }

        [Test]
        public void IsPropertyInvalidReturnsFalseForNonEmptyArray()
        {
            ValidateAssignmentCollectionTestAsset asset =
                CreateScriptableObject<ValidateAssignmentCollectionTestAsset>();
            asset.requiredArray = new[] { 1, 2, 3 };
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentCollectionTestAsset.requiredArray)
            );
            Assert.IsNotNull(property, "Failed to locate requiredArray property.");
            Assert.IsFalse(ValidateAssignmentPropertyDrawer.IsPropertyInvalid(property));
        }

        [Test]
        public void CreatePropertyGUIReturnsContainerWithHelpBoxForInvalidField()
        {
            ValidateAssignmentObjectReferenceTestAsset asset =
                CreateScriptableObject<ValidateAssignmentObjectReferenceTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentObjectReferenceTestAsset.requiredGameObject)
            );
            Assert.IsNotNull(property, "Failed to locate requiredGameObject property.");

            ValidateAssignmentPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(drawer, new ValidateAssignmentAttribute());
            VisualElement element = drawer.CreatePropertyGUI(property);

            Assert.IsNotNull(element, "CreatePropertyGUI should return a non-null element.");
            HelpBox helpBox = element.Q<HelpBox>();
            Assert.IsNotNull(helpBox, "Container should contain a HelpBox.");
            Assert.That(helpBox.style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        [Test]
        public void CreatePropertyGUIHidesHelpBoxForValidField()
        {
            ValidateAssignmentObjectReferenceTestAsset asset =
                CreateScriptableObject<ValidateAssignmentObjectReferenceTestAsset>();
            asset.requiredGameObject = NewGameObject("TestObject");
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentObjectReferenceTestAsset.requiredGameObject)
            );
            Assert.IsNotNull(property, "Failed to locate requiredGameObject property.");

            ValidateAssignmentPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(drawer, new ValidateAssignmentAttribute());
            VisualElement element = drawer.CreatePropertyGUI(property);

            Assert.IsNotNull(element, "CreatePropertyGUI should return a non-null element.");
            HelpBox helpBox = element.Q<HelpBox>();
            Assert.IsNotNull(helpBox, "Container should contain a HelpBox.");
            Assert.That(helpBox.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [Test]
        public void CreatePropertyGUIIncludesPropertyField()
        {
            ValidateAssignmentObjectReferenceTestAsset asset =
                CreateScriptableObject<ValidateAssignmentObjectReferenceTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentObjectReferenceTestAsset.requiredGameObject)
            );
            Assert.IsNotNull(property, "Failed to locate requiredGameObject property.");

            ValidateAssignmentPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(drawer, new ValidateAssignmentAttribute());
            VisualElement element = drawer.CreatePropertyGUI(property);

            PropertyField propertyField = element.Q<PropertyField>();
            Assert.IsNotNull(propertyField, "Container should include a PropertyField.");
        }

        [Test]
        public void CreatePropertyGUIUsesWarningMessageTypeByDefault()
        {
            ValidateAssignmentMessageTypeTestAsset asset =
                CreateScriptableObject<ValidateAssignmentMessageTypeTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentMessageTypeTestAsset.defaultField)
            );
            Assert.IsNotNull(property, "Failed to locate defaultField property.");

            ValidateAssignmentPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(drawer, new ValidateAssignmentAttribute());
            VisualElement element = drawer.CreatePropertyGUI(property);

            HelpBox helpBox = element.Q<HelpBox>();
            Assert.IsNotNull(helpBox, "Container should contain a HelpBox.");
            Assert.That(helpBox.messageType, Is.EqualTo(HelpBoxMessageType.Warning));
        }

        [Test]
        public void CreatePropertyGUIUsesErrorMessageTypeWhenSpecified()
        {
            ValidateAssignmentMessageTypeTestAsset asset =
                CreateScriptableObject<ValidateAssignmentMessageTypeTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentMessageTypeTestAsset.errorField)
            );
            Assert.IsNotNull(property, "Failed to locate errorField property.");

            ValidateAssignmentPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(
                drawer,
                new ValidateAssignmentAttribute(ValidateAssignmentMessageType.Error)
            );
            VisualElement element = drawer.CreatePropertyGUI(property);

            HelpBox helpBox = element.Q<HelpBox>();
            Assert.IsNotNull(helpBox, "Container should contain a HelpBox.");
            Assert.That(helpBox.messageType, Is.EqualTo(HelpBoxMessageType.Error));
        }

        [Test]
        public void CreatePropertyGUIUsesWarningMessageTypeWhenSpecified()
        {
            ValidateAssignmentMessageTypeTestAsset asset =
                CreateScriptableObject<ValidateAssignmentMessageTypeTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentMessageTypeTestAsset.warningField)
            );
            Assert.IsNotNull(property, "Failed to locate warningField property.");

            ValidateAssignmentPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(
                drawer,
                new ValidateAssignmentAttribute(ValidateAssignmentMessageType.Warning)
            );
            VisualElement element = drawer.CreatePropertyGUI(property);

            HelpBox helpBox = element.Q<HelpBox>();
            Assert.IsNotNull(helpBox, "Container should contain a HelpBox.");
            Assert.That(helpBox.messageType, Is.EqualTo(HelpBoxMessageType.Warning));
        }

        [Test]
        public void CreatePropertyGUIUsesCustomMessageWhenProvided()
        {
            ValidateAssignmentCustomMessageTestAsset asset =
                CreateScriptableObject<ValidateAssignmentCustomMessageTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentCustomMessageTestAsset.playerPrefab)
            );
            Assert.IsNotNull(property, "Failed to locate playerPrefab property.");

            string customMessage = "Player prefab is required for spawning";
            ValidateAssignmentPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(
                drawer,
                new ValidateAssignmentAttribute(customMessage)
            );
            VisualElement element = drawer.CreatePropertyGUI(property);

            HelpBox helpBox = element.Q<HelpBox>();
            Assert.IsNotNull(helpBox, "Container should contain a HelpBox.");
            Assert.That(helpBox.text, Is.EqualTo(customMessage));
        }

        [Test]
        public void CreatePropertyGUIUsesCustomMessageWithErrorType()
        {
            ValidateAssignmentCustomMessageTestAsset asset =
                CreateScriptableObject<ValidateAssignmentCustomMessageTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentCustomMessageTestAsset.audioSource)
            );
            Assert.IsNotNull(property, "Failed to locate audioSource property.");

            string customMessage = "Audio source must be assigned for sound effects";
            ValidateAssignmentPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(
                drawer,
                new ValidateAssignmentAttribute(ValidateAssignmentMessageType.Error, customMessage)
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
            ValidateAssignmentObjectReferenceTestAsset asset =
                CreateScriptableObject<ValidateAssignmentObjectReferenceTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentObjectReferenceTestAsset.requiredGameObject)
            );
            Assert.IsNotNull(property, "Failed to locate requiredGameObject property.");

            ValidateAssignmentPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(drawer, new ValidateAssignmentAttribute());
            VisualElement element = drawer.CreatePropertyGUI(property);

            HelpBox helpBox = element.Q<HelpBox>();
            Assert.IsNotNull(helpBox, "Container should contain a HelpBox.");
            Assert.That(helpBox.text, Does.Contain("Required Game Object"));
        }

        [Test]
        public void GetPropertyHeightIsGreaterWhenFieldIsInvalid()
        {
            ValidateAssignmentObjectReferenceTestAsset asset =
                CreateScriptableObject<ValidateAssignmentObjectReferenceTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty invalidProperty = serializedObject.FindProperty(
                nameof(ValidateAssignmentObjectReferenceTestAsset.requiredGameObject)
            );
            Assert.IsNotNull(invalidProperty, "Failed to locate requiredGameObject property.");

            ValidateAssignmentPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(drawer, new ValidateAssignmentAttribute());
            GUIContent label = new("Test");

            float invalidHeight = drawer.GetPropertyHeight(invalidProperty, label);

            asset.requiredGameObject = NewGameObject("TestObject");
            serializedObject.Update();

            SerializedProperty validProperty = serializedObject.FindProperty(
                nameof(ValidateAssignmentObjectReferenceTestAsset.requiredGameObject)
            );
            float validHeight = drawer.GetPropertyHeight(validProperty, label);

            Assert.That(
                invalidHeight,
                Is.GreaterThan(validHeight),
                "Height should be greater when field is invalid to accommodate help box."
            );
        }

        [UnityTest]
        public IEnumerator OnGUIRendersHelpBoxForInvalidField()
        {
            ValidateAssignmentObjectReferenceTestAsset asset =
                CreateScriptableObject<ValidateAssignmentObjectReferenceTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentObjectReferenceTestAsset.requiredGameObject)
            );
            Assert.IsNotNull(property, "Failed to locate requiredGameObject property.");

            ValidateAssignmentPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(drawer, new ValidateAssignmentAttribute());
            GUIContent label = new("Required Game Object");
            float height = drawer.GetPropertyHeight(property, label);
            Rect position = new(0f, 0f, 400f, height);

            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(position, property, label);
            });
        }

        [UnityTest]
        public IEnumerator OnGUIRendersPropertyFieldForValidField()
        {
            ValidateAssignmentObjectReferenceTestAsset asset =
                CreateScriptableObject<ValidateAssignmentObjectReferenceTestAsset>();
            asset.requiredGameObject = NewGameObject("TestObject");
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentObjectReferenceTestAsset.requiredGameObject)
            );
            Assert.IsNotNull(property, "Failed to locate requiredGameObject property.");

            ValidateAssignmentPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(drawer, new ValidateAssignmentAttribute());
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
            ValidateAssignmentAttribute attribute = new();
            Assert.That(attribute.MessageType, Is.EqualTo(ValidateAssignmentMessageType.Warning));
            Assert.That(attribute.CustomMessage, Is.Null);
        }

        [Test]
        public void AttributeMessageTypeConstructorSetsCorrectType()
        {
            ValidateAssignmentAttribute warningAttribute = new(
                ValidateAssignmentMessageType.Warning
            );
            Assert.That(
                warningAttribute.MessageType,
                Is.EqualTo(ValidateAssignmentMessageType.Warning)
            );

            ValidateAssignmentAttribute errorAttribute = new(ValidateAssignmentMessageType.Error);
            Assert.That(
                errorAttribute.MessageType,
                Is.EqualTo(ValidateAssignmentMessageType.Error)
            );
        }

        [Test]
        public void AttributeCustomMessageConstructorSetsMessage()
        {
            string customMessage = "Custom validation message";
            ValidateAssignmentAttribute attribute = new(customMessage);
            Assert.That(attribute.CustomMessage, Is.EqualTo(customMessage));
            Assert.That(attribute.MessageType, Is.EqualTo(ValidateAssignmentMessageType.Warning));
        }

        [Test]
        public void AttributeFullConstructorSetsBothProperties()
        {
            string customMessage = "Error validation message";
            ValidateAssignmentAttribute attribute = new(
                ValidateAssignmentMessageType.Error,
                customMessage
            );
            Assert.That(attribute.CustomMessage, Is.EqualTo(customMessage));
            Assert.That(attribute.MessageType, Is.EqualTo(ValidateAssignmentMessageType.Error));
        }

        [Test]
        public void IsPropertyInvalidReturnsFalseForIntProperty()
        {
            ValidateAssignmentMixedFieldsTestAsset asset =
                CreateScriptableObject<ValidateAssignmentMixedFieldsTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentMixedFieldsTestAsset.nonDecoratedIntField)
            );
            Assert.IsNotNull(property, "Failed to locate nonDecoratedIntField property.");
            Assert.IsFalse(ValidateAssignmentPropertyDrawer.IsPropertyInvalid(property));
        }

        [Test]
        public void IsPropertyInvalidReturnsTrueForNullSprite()
        {
            ValidateAssignmentMixedFieldsTestAsset asset =
                CreateScriptableObject<ValidateAssignmentMixedFieldsTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentMixedFieldsTestAsset.nullableSprite)
            );
            Assert.IsNotNull(property, "Failed to locate nullableSprite property.");
            Assert.IsTrue(ValidateAssignmentPropertyDrawer.IsPropertyInvalid(property));
        }

        [Test]
        public void IsPropertyInvalidReturnsTrueForNullScriptableObject()
        {
            ValidateAssignmentObjectReferenceTestAsset asset =
                CreateScriptableObject<ValidateAssignmentObjectReferenceTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentObjectReferenceTestAsset.requiredScriptableObject)
            );
            Assert.IsNotNull(property, "Failed to locate requiredScriptableObject property.");
            Assert.IsTrue(ValidateAssignmentPropertyDrawer.IsPropertyInvalid(property));
        }

        [Test]
        public void IsPropertyInvalidReturnsFalseForAssignedScriptableObject()
        {
            ValidateAssignmentObjectReferenceTestAsset asset =
                CreateScriptableObject<ValidateAssignmentObjectReferenceTestAsset>();
            asset.requiredScriptableObject = CreateScriptableObject<ScriptableObject>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentObjectReferenceTestAsset.requiredScriptableObject)
            );
            Assert.IsNotNull(property, "Failed to locate requiredScriptableObject property.");
            Assert.IsFalse(ValidateAssignmentPropertyDrawer.IsPropertyInvalid(property));
        }

        [Test]
        public void IsPropertyInvalidReturnsTrueForNullMaterial()
        {
            ValidateAssignmentObjectReferenceTestAsset asset =
                CreateScriptableObject<ValidateAssignmentObjectReferenceTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentObjectReferenceTestAsset.requiredMaterial)
            );
            Assert.IsNotNull(property, "Failed to locate requiredMaterial property.");
            Assert.IsTrue(ValidateAssignmentPropertyDrawer.IsPropertyInvalid(property));
        }

        [Test]
        public void ClearHeightCacheClearsCache()
        {
            ValidateAssignmentObjectReferenceTestAsset asset =
                CreateScriptableObject<ValidateAssignmentObjectReferenceTestAsset>();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentObjectReferenceTestAsset.requiredGameObject)
            );

            ValidateAssignmentPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(drawer, new ValidateAssignmentAttribute());
            GUIContent label = new("Test");

            drawer.GetPropertyHeight(property, label);

            ValidateAssignmentPropertyDrawer.ClearHeightCache();

            drawer.GetPropertyHeight(property, label);
        }

        [Test]
        public void CreatePropertyGUIShowsHelpBoxForEmptyList()
        {
            ValidateAssignmentCollectionTestAsset asset =
                CreateScriptableObject<ValidateAssignmentCollectionTestAsset>();
            asset.requiredList.Clear();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentCollectionTestAsset.requiredList)
            );
            Assert.IsNotNull(property, "Failed to locate requiredList property.");

            ValidateAssignmentPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(drawer, new ValidateAssignmentAttribute());
            VisualElement element = drawer.CreatePropertyGUI(property);

            HelpBox helpBox = element.Q<HelpBox>();
            Assert.IsNotNull(helpBox, "Container should contain a HelpBox.");
            Assert.That(helpBox.style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        [Test]
        public void CreatePropertyGUIHidesHelpBoxForNonEmptyList()
        {
            ValidateAssignmentCollectionTestAsset asset =
                CreateScriptableObject<ValidateAssignmentCollectionTestAsset>();
            asset.requiredList.Add(42);
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentCollectionTestAsset.requiredList)
            );
            Assert.IsNotNull(property, "Failed to locate requiredList property.");

            ValidateAssignmentPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(drawer, new ValidateAssignmentAttribute());
            VisualElement element = drawer.CreatePropertyGUI(property);

            HelpBox helpBox = element.Q<HelpBox>();
            Assert.IsNotNull(helpBox, "Container should contain a HelpBox.");
            Assert.That(helpBox.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [Test]
        public void CreatePropertyGUIUsesErrorMessageTypeForCollectionWithCustomMessage()
        {
            ValidateAssignmentCollectionTestAsset asset =
                CreateScriptableObject<ValidateAssignmentCollectionTestAsset>();
            asset.spawnPoints.Clear();
            using SerializedObject serializedObject = new(asset);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(
                nameof(ValidateAssignmentCollectionTestAsset.spawnPoints)
            );
            Assert.IsNotNull(property, "Failed to locate spawnPoints property.");

            string customMessage = "Spawn points cannot be empty";
            ValidateAssignmentPropertyDrawer drawer = new();
            PropertyDrawerTestHelper.AssignAttribute(
                drawer,
                new ValidateAssignmentAttribute(ValidateAssignmentMessageType.Error, customMessage)
            );
            VisualElement element = drawer.CreatePropertyGUI(property);

            HelpBox helpBox = element.Q<HelpBox>();
            Assert.IsNotNull(helpBox, "Container should contain a HelpBox.");
            Assert.That(helpBox.text, Is.EqualTo(customMessage));
            Assert.That(helpBox.messageType, Is.EqualTo(HelpBoxMessageType.Error));
        }
    }
#endif
}
