namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;

    [TestFixture]
    public sealed class WInLineEditorPropertyDrawerTests
    {
        private TestHolder holder;
        private TestData data;
        private SerializedObject serializedHolder;
        private SerializedProperty inlineProperty;

        [SetUp]
        public void SetUp()
        {
            holder = ScriptableObject.CreateInstance<TestHolder>();
            data = ScriptableObject.CreateInstance<TestData>();

            holder.inlineData = data;
            holder.collapsedData = data;
            holder.headerOnlyData = data;

            serializedHolder = new SerializedObject(holder);
            inlineProperty = serializedHolder.FindProperty(nameof(TestHolder.inlineData));
        }

        [TearDown]
        public void TearDown()
        {
            if (serializedHolder != null)
            {
                SessionState.EraseBool(
                    GetSessionKey(serializedHolder.FindProperty(nameof(TestHolder.inlineData)))
                );
                SessionState.EraseBool(
                    GetSessionKey(serializedHolder.FindProperty(nameof(TestHolder.collapsedData)))
                );
                SessionState.EraseBool(
                    GetSessionKey(serializedHolder.FindProperty(nameof(TestHolder.headerOnlyData)))
                );
                serializedHolder = null;
            }

            if (holder != null)
            {
                ScriptableObject.DestroyImmediate(holder);
                holder = null;
            }

            if (data != null)
            {
                ScriptableObject.DestroyImmediate(data);
                data = null;
            }
        }

        [Test]
        public void CreatePropertyGUIDrawObjectFieldEnabledAddsObjectField()
        {
            WInLineEditorPropertyDrawer drawer = new();

            VisualElement root = drawer.CreatePropertyGUI(inlineProperty);
            root.Bind(serializedHolder);

            ObjectField field = root.Query<ObjectField>()
                .Where(x => x.bindingPath == inlineProperty.propertyPath)
                .First();
            Assert.That(field, Is.Not.Null);
            Assert.That(field.objectType, Is.EqualTo(typeof(TestData)));
        }

        [Test]
        public void CreatePropertyGUIModeFoldoutCollapsedStartsCollapsed()
        {
            SerializedProperty property = serializedHolder.FindProperty(
                nameof(TestHolder.collapsedData)
            );
            SessionState.EraseBool(GetSessionKey(property));

            WInLineEditorPropertyDrawer drawer = new();
            VisualElement root = drawer.CreatePropertyGUI(property);
            root.Bind(serializedHolder);

            Foldout foldout = root.Q<Foldout>();
            Assert.That(foldout, Is.Not.Null);
            Assert.That(foldout.value, Is.False);
        }

        [Test]
        public void NullReferenceKeepsFieldEnabledAndFoldoutCollapsed()
        {
            SerializedProperty property = serializedHolder.FindProperty(
                nameof(TestHolder.collapsedData)
            );
            SessionState.EraseBool(GetSessionKey(property));
            property.objectReferenceValue = null;
            serializedHolder.ApplyModifiedProperties();
            serializedHolder.Update();

            WInLineEditorPropertyDrawer drawer = new();
            VisualElement root = drawer.CreatePropertyGUI(property);
            root.Bind(serializedHolder);

            ObjectField field = root.Query<ObjectField>()
                .Where(x => x.bindingPath == property.propertyPath)
                .First();
            Assert.That(field, Is.Not.Null);
            Assert.That(field.enabledInHierarchy, Is.True);

            Foldout foldout = root.Q<Foldout>();
            Assert.That(foldout, Is.Not.Null);
            Toggle toggle = foldout.Q<Toggle>();
            Assert.That(toggle, Is.Not.Null);
            toggle.value = true;
            Assert.That(foldout.value, Is.False);
        }

        [Test]
        public void CreatePropertyGUIDrawObjectFieldDisabledHasNoObjectField()
        {
            SerializedProperty property = serializedHolder.FindProperty(
                nameof(TestHolder.headerOnlyData)
            );

            WInLineEditorPropertyDrawer drawer = new();
            VisualElement root = drawer.CreatePropertyGUI(property);
            root.Bind(serializedHolder);

            ObjectField field = root.Query<ObjectField>()
                .Where(x => x.bindingPath == property.propertyPath)
                .First();
            Assert.That(field, Is.Null);
        }

        [Test]
        public void GetPropertyHeightReturnsDefaultHeight()
        {
            WInLineEditorPropertyDrawer drawer = new();
            float expected = EditorGUI.GetPropertyHeight(inlineProperty, true);
            Assert.That(
                drawer.GetPropertyHeight(inlineProperty, GUIContent.none),
                Is.EqualTo(expected)
            );
        }

        private static string GetSessionKey(SerializedProperty property)
        {
            return $"WInLineEditor:{property.serializedObject.targetObject.GetInstanceID()}:{property.propertyPath}";
        }

        [CreateAssetMenu]
        private sealed class TestData : ScriptableObject
        {
            public int value = 10;
            public string text = "Inline";
        }

        private sealed class TestHolder : ScriptableObject
        {
            [WInLineEditor]
            public TestData inlineData;

            [WInLineEditor(WInLineEditorMode.FoldoutCollapsed)]
            public TestData collapsedData;

            [WInLineEditor(drawObjectField: false)]
            public TestData headerOnlyData;
        }
    }
}
