#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEditor;
    using WallstopStudios.UnityHelpers.Editor.Settings;
    using WallstopStudios.UnityHelpers.Editor.Utils.WGroup;
    using WallstopStudios.UnityHelpers.Tests.Core;

    /// <summary>
    /// Tests for verifying hasVisibleChildren detection for different property types
    /// within WGroups. This is critical for proper indent handling.
    /// </summary>
    [TestFixture]
    public sealed class WGroupHasVisibleChildrenTests : CommonTestBase
    {
        private UnityHelpersSettings.WGroupAutoIncludeConfiguration _previousConfiguration;
        private int _originalIndentLevel;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            WGroupLayoutBuilder.ClearCache();
            _originalIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            _previousConfiguration = UnityHelpersSettings.GetWGroupAutoIncludeConfiguration();
            UnityHelpersSettings.SetWGroupAutoIncludeConfigurationForTests(
                UnityHelpersSettings.WGroupAutoIncludeMode.None,
                0
            );
        }

        [TearDown]
        public override void TearDown()
        {
            WGroupLayoutBuilder.ClearCache();
            EditorGUI.indentLevel = _originalIndentLevel;
            UnityHelpersSettings.SetWGroupAutoIncludeConfigurationForTests(
                _previousConfiguration.Mode,
                _previousConfiguration.RowCount
            );
            base.TearDown();
        }

        [Test]
        public void IntFieldDoesNotHaveVisibleChildren()
        {
            SimpleFieldsTarget target = CreateScriptableObject<SimpleFieldsTarget>();
            using SerializedObject serializedObject = new(target);

            SerializedProperty property = serializedObject.FindProperty("intField");

            Assert.That(
                property.hasVisibleChildren,
                Is.False,
                "int field should not have visible children."
            );
        }

        [Test]
        public void FloatFieldDoesNotHaveVisibleChildren()
        {
            SimpleFieldsTarget target = CreateScriptableObject<SimpleFieldsTarget>();
            using SerializedObject serializedObject = new(target);

            SerializedProperty property = serializedObject.FindProperty("floatField");

            Assert.That(
                property.hasVisibleChildren,
                Is.False,
                "float field should not have visible children."
            );
        }

        [Test]
        public void StringFieldDoesNotHaveVisibleChildren()
        {
            SimpleFieldsTarget target = CreateScriptableObject<SimpleFieldsTarget>();
            using SerializedObject serializedObject = new(target);

            SerializedProperty property = serializedObject.FindProperty("stringField");

            Assert.That(
                property.hasVisibleChildren,
                Is.False,
                "string field should not have visible children."
            );
        }

        [Test]
        public void BoolFieldDoesNotHaveVisibleChildren()
        {
            SimpleFieldsTarget target = CreateScriptableObject<SimpleFieldsTarget>();
            using SerializedObject serializedObject = new(target);

            SerializedProperty property = serializedObject.FindProperty("boolField");

            Assert.That(
                property.hasVisibleChildren,
                Is.False,
                "bool field should not have visible children."
            );
        }

        [Test]
        public void EmptyListHasVisibleChildren()
        {
            ListFieldsTarget target = CreateScriptableObject<ListFieldsTarget>();
            target.intList = new List<int>();
            using SerializedObject serializedObject = new(target);

            SerializedProperty property = serializedObject.FindProperty("intList");

            Assert.That(
                property.hasVisibleChildren,
                Is.True,
                "Empty list should have visible children (size field)."
            );
        }

        [Test]
        public void PopulatedListHasVisibleChildren()
        {
            ListFieldsTarget target = CreateScriptableObject<ListFieldsTarget>();
            target.intList = new List<int> { 1, 2, 3, 4, 5 };
            using SerializedObject serializedObject = new(target);

            SerializedProperty property = serializedObject.FindProperty("intList");

            Assert.That(
                property.hasVisibleChildren,
                Is.True,
                "Populated list should have visible children."
            );
        }

        [Test]
        public void SingleElementListHasVisibleChildren()
        {
            SingleElementTarget target = CreateScriptableObject<SingleElementTarget>();
            using SerializedObject serializedObject = new(target);

            SerializedProperty property = serializedObject.FindProperty("singleItemList");

            Assert.That(
                property.hasVisibleChildren,
                Is.True,
                "Single element list should have visible children."
            );
        }

        [Test]
        public void EmptyArrayHasVisibleChildren()
        {
            ArrayFieldsTarget target = CreateScriptableObject<ArrayFieldsTarget>();
            target.intArray = Array.Empty<int>();
            using SerializedObject serializedObject = new(target);

            SerializedProperty property = serializedObject.FindProperty("intArray");

            Assert.That(
                property.hasVisibleChildren,
                Is.True,
                "Empty array should have visible children (size field)."
            );
        }

        [Test]
        public void PopulatedArrayHasVisibleChildren()
        {
            ArrayFieldsTarget target = CreateScriptableObject<ArrayFieldsTarget>();
            target.intArray = new[] { 1, 2, 3 };
            using SerializedObject serializedObject = new(target);

            SerializedProperty property = serializedObject.FindProperty("intArray");

            Assert.That(
                property.hasVisibleChildren,
                Is.True,
                "Populated array should have visible children."
            );
        }

        [Test]
        public void SingleElementArrayHasVisibleChildren()
        {
            SingleElementTarget target = CreateScriptableObject<SingleElementTarget>();
            using SerializedObject serializedObject = new(target);

            SerializedProperty property = serializedObject.FindProperty("singleItemArray");

            Assert.That(
                property.hasVisibleChildren,
                Is.True,
                "Single element array should have visible children."
            );
        }

        [Test]
        public void StringListHasVisibleChildren()
        {
            ListFieldsTarget target = CreateScriptableObject<ListFieldsTarget>();
            target.stringList = new List<string> { "a", "b", "c" };
            using SerializedObject serializedObject = new(target);

            SerializedProperty property = serializedObject.FindProperty("stringList");

            Assert.That(
                property.hasVisibleChildren,
                Is.True,
                "String list should have visible children."
            );
        }

        [Test]
        public void FloatArrayHasVisibleChildren()
        {
            ArrayFieldsTarget target = CreateScriptableObject<ArrayFieldsTarget>();
            target.floatArray = new[] { 1.0f, 2.0f };
            using SerializedObject serializedObject = new(target);

            SerializedProperty property = serializedObject.FindProperty("floatArray");

            Assert.That(
                property.hasVisibleChildren,
                Is.True,
                "Float array should have visible children."
            );
        }

        [Test]
        public void SerializableClassWithFieldsHasVisibleChildren()
        {
            SerializableClassTarget target = CreateScriptableObject<SerializableClassTarget>();
            target.nestedData = new NestedData { value = 10, name = "Test" };
            using SerializedObject serializedObject = new(target);

            SerializedProperty property = serializedObject.FindProperty("nestedData");

            Assert.That(
                property.hasVisibleChildren,
                Is.True,
                "Serializable class should have visible children."
            );
        }

        [Test]
        public void EmptySerializableClassHasVisibleChildren()
        {
            SerializableClassTarget target = CreateScriptableObject<SerializableClassTarget>();
            target.nestedData = new NestedData();
            using SerializedObject serializedObject = new(target);

            SerializedProperty property = serializedObject.FindProperty("nestedData");

            Assert.That(
                property.hasVisibleChildren,
                Is.True,
                "Empty serializable class still has its fields as visible children."
            );
        }

        [Test]
        public void DeepNestedSerializableClassHasVisibleChildren()
        {
            DeepNestingTarget target = CreateScriptableObject<DeepNestingTarget>();
            target.deepData = new DeepNestedData
            {
                depth = 5,
                child = new NestedData { value = 10 },
            };
            using SerializedObject serializedObject = new(target);

            SerializedProperty property = serializedObject.FindProperty("deepData");

            Assert.That(
                property.hasVisibleChildren,
                Is.True,
                "Deep nested serializable class should have visible children."
            );
        }

        [Test]
        public void NestedClassChildFieldHasVisibleChildren()
        {
            DeepNestingTarget target = CreateScriptableObject<DeepNestingTarget>();
            using SerializedObject serializedObject = new(target);

            SerializedProperty deepDataProperty = serializedObject.FindProperty("deepData");
            SerializedProperty childProperty = deepDataProperty.FindPropertyRelative("child");

            Assert.That(
                childProperty.hasVisibleChildren,
                Is.True,
                "Child field of deep nested class should have visible children."
            );
        }

        [Test]
        public void ListOfSerializableClassesHasVisibleChildren()
        {
            WrappedNestedListTarget target = CreateScriptableObject<WrappedNestedListTarget>();
            target.wrappedLists = new List<IntListWrapper>
            {
                new IntListWrapper
                {
                    values = new List<int> { 1, 2 },
                },
            };
            using SerializedObject serializedObject = new(target);

            SerializedProperty property = serializedObject.FindProperty("wrappedLists");

            Assert.That(
                property.hasVisibleChildren,
                Is.True,
                "List of serializable classes should have visible children."
            );
        }

        [Test]
        public void EmptyListOfSerializableClassesHasVisibleChildren()
        {
            WrappedNestedListTarget target = CreateScriptableObject<WrappedNestedListTarget>();
            target.wrappedLists = new List<IntListWrapper>();
            using SerializedObject serializedObject = new(target);

            SerializedProperty property = serializedObject.FindProperty("wrappedLists");

            Assert.That(
                property.hasVisibleChildren,
                Is.True,
                "Empty list of serializable classes should still have visible children (size)."
            );
        }

        [Test]
        public void ListElementHasVisibleChildren()
        {
            WrappedNestedListTarget target = CreateScriptableObject<WrappedNestedListTarget>();
            target.wrappedLists = new List<IntListWrapper>
            {
                new IntListWrapper
                {
                    values = new List<int> { 1, 2 },
                },
            };
            using SerializedObject serializedObject = new(target);

            SerializedProperty listProperty = serializedObject.FindProperty("wrappedLists");
            SerializedProperty elementProperty = listProperty.GetArrayElementAtIndex(0);

            Assert.That(
                elementProperty.hasVisibleChildren,
                Is.True,
                "List element (serializable class) should have visible children."
            );
        }

        [Test]
        public void ListInsideSerializableClassHasVisibleChildren()
        {
            SerializableClassTarget target = CreateScriptableObject<SerializableClassTarget>();
            target.nestedData = new NestedData
            {
                numbers = new List<int> { 1, 2, 3 },
            };
            using SerializedObject serializedObject = new(target);

            SerializedProperty nestedProperty = serializedObject.FindProperty("nestedData");
            SerializedProperty numbersProperty = nestedProperty.FindPropertyRelative("numbers");

            Assert.That(
                numbersProperty.hasVisibleChildren,
                Is.True,
                "List inside serializable class should have visible children."
            );
        }

        [Test]
        public void MixedFieldsShowCorrectHasVisibleChildrenStatus()
        {
            MixedFieldsTarget target = CreateScriptableObject<MixedFieldsTarget>();
            target.simpleInt = 42;
            target.listField = new List<int> { 1, 2 };
            target.simpleString = "test";
            target.nestedField = new NestedData { value = 10 };
            target.simpleFloat = 3.14f;
            using SerializedObject serializedObject = new(target);

            Assert.That(
                serializedObject.FindProperty("simpleInt").hasVisibleChildren,
                Is.False,
                "simpleInt should not have visible children."
            );
            Assert.That(
                serializedObject.FindProperty("listField").hasVisibleChildren,
                Is.True,
                "listField should have visible children."
            );
            Assert.That(
                serializedObject.FindProperty("simpleString").hasVisibleChildren,
                Is.False,
                "simpleString should not have visible children."
            );
            Assert.That(
                serializedObject.FindProperty("nestedField").hasVisibleChildren,
                Is.True,
                "nestedField should have visible children."
            );
            Assert.That(
                serializedObject.FindProperty("simpleFloat").hasVisibleChildren,
                Is.False,
                "simpleFloat should not have visible children."
            );
        }

        [Test]
        public void AllCollectionTypesInEmptyCollectionsTargetHaveVisibleChildren()
        {
            EmptyCollectionsTarget target = CreateScriptableObject<EmptyCollectionsTarget>();
            using SerializedObject serializedObject = new(target);

            Assert.That(
                serializedObject.FindProperty("emptyList").hasVisibleChildren,
                Is.True,
                "emptyList should have visible children."
            );
            Assert.That(
                serializedObject.FindProperty("emptyArray").hasVisibleChildren,
                Is.True,
                "emptyArray should have visible children."
            );
            Assert.That(
                serializedObject.FindProperty("emptyNestedList").hasVisibleChildren,
                Is.True,
                "emptyNestedList should have visible children."
            );
        }

        [Test]
        public void NestedGroupsListFieldHasVisibleChildren()
        {
            NestedGroupsTarget target = CreateScriptableObject<NestedGroupsTarget>();
            target.innerList = new List<int> { 1, 2, 3 };
            using SerializedObject serializedObject = new(target);

            SerializedProperty innerListProperty = serializedObject.FindProperty("innerList");

            Assert.That(
                innerListProperty.hasVisibleChildren,
                Is.True,
                "List in nested group should have visible children."
            );
        }

        [Test]
        public void ThreeLevelGroupsListFieldHasVisibleChildren()
        {
            ThreeLevelGroupsTarget target = CreateScriptableObject<ThreeLevelGroupsTarget>();
            target.level3List = new List<int> { 1, 2 };
            using SerializedObject serializedObject = new(target);

            SerializedProperty level3ListProperty = serializedObject.FindProperty("level3List");

            Assert.That(
                level3ListProperty.hasVisibleChildren,
                Is.True,
                "List at third nesting level should have visible children."
            );
        }

        [Test]
        public void ComplexCombinedTargetArrayHasVisibleChildren()
        {
            ComplexCombinedTarget target = CreateScriptableObject<ComplexCombinedTarget>();
            target.deepestArray = new[] { 1, 2, 3 };
            using SerializedObject serializedObject = new(target);

            SerializedProperty deepestArrayProperty = serializedObject.FindProperty("deepestArray");

            Assert.That(
                deepestArrayProperty.hasVisibleChildren,
                Is.True,
                "Array in deeply nested group structure should have visible children."
            );
        }

        [Test]
        public void ComplexCombinedTargetNestedListHasVisibleChildren()
        {
            ComplexCombinedTarget target = CreateScriptableObject<ComplexCombinedTarget>();
            target.middleNestedList = new List<NestedData>
            {
                new NestedData { value = 1 },
                new NestedData { value = 2 },
            };
            using SerializedObject serializedObject = new(target);

            SerializedProperty middleNestedListProperty = serializedObject.FindProperty(
                "middleNestedList"
            );

            Assert.That(
                middleNestedListProperty.hasVisibleChildren,
                Is.True,
                "List of NestedData in complex combined target should have visible children."
            );
        }
    }
}
#endif
