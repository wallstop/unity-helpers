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
        private TestHolder _holder;
        private TestData _data;
        private SerializedObject _serializedHolder;
        private SerializedProperty _inlineProperty;

        [SetUp]
        public void SetUp()
        {
            _holder = ScriptableObject.CreateInstance<TestHolder>();
            _data = ScriptableObject.CreateInstance<TestData>();

            _holder.inlineData = _data;
            _holder.collapsedData = _data;
            _holder.headerOnlyData = _data;

            _serializedHolder = new SerializedObject(_holder);
            _inlineProperty = _serializedHolder.FindProperty(nameof(TestHolder.inlineData));
        }

        [TearDown]
        public void TearDown()
        {
            if (_serializedHolder != null)
            {
                SessionState.EraseBool(
                    GetSessionKey(_serializedHolder.FindProperty(nameof(TestHolder.inlineData)))
                );
                SessionState.EraseBool(
                    GetSessionKey(_serializedHolder.FindProperty(nameof(TestHolder.collapsedData)))
                );
                SessionState.EraseBool(
                    GetSessionKey(_serializedHolder.FindProperty(nameof(TestHolder.headerOnlyData)))
                );
                _serializedHolder = null;
            }

            if (_holder != null)
            {
                Object.DestroyImmediate(_holder);
                _holder = null;
            }

            if (_data != null)
            {
                Object.DestroyImmediate(_data);
                _data = null;
            }
        }

        [Test]
        public void CreatePropertyGUIDrawObjectFieldEnabledAddsObjectField()
        {
            WInLineEditorPropertyDrawer drawer = new();

            VisualElement root = drawer.CreatePropertyGUI(_inlineProperty);
            root.Bind(_serializedHolder);

            ObjectField field = root.Query<ObjectField>()
                .Where(x => x.bindingPath == _inlineProperty.propertyPath)
                .First();
            Assert.That(field, Is.Not.Null);
            Assert.That(field.objectType, Is.EqualTo(typeof(TestData)));
        }

        [Test]
        public void CreatePropertyGUIModeFoldoutCollapsedStartsCollapsed()
        {
            SerializedProperty property = _serializedHolder.FindProperty(
                nameof(TestHolder.collapsedData)
            );
            SessionState.EraseBool(GetSessionKey(property));

            WInLineEditorPropertyDrawer drawer = new();
            VisualElement root = drawer.CreatePropertyGUI(property);
            root.Bind(_serializedHolder);

            Foldout foldout = root.Q<Foldout>();
            Assert.That(foldout, Is.Not.Null);
            Assert.That(foldout.value, Is.False);
        }

        [Test]
        public void NullReferenceKeepsFieldEnabledAndFoldoutCollapsed()
        {
            SerializedProperty property = _serializedHolder.FindProperty(
                nameof(TestHolder.collapsedData)
            );
            SessionState.EraseBool(GetSessionKey(property));
            property.objectReferenceValue = null;
            _serializedHolder.ApplyModifiedProperties();
            _serializedHolder.Update();

            WInLineEditorPropertyDrawer drawer = new();
            VisualElement root = drawer.CreatePropertyGUI(property);
            root.Bind(_serializedHolder);

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
            SerializedProperty property = _serializedHolder.FindProperty(
                nameof(TestHolder.headerOnlyData)
            );

            WInLineEditorPropertyDrawer drawer = new();
            VisualElement root = drawer.CreatePropertyGUI(property);
            root.Bind(_serializedHolder);

            ObjectField field = root.Query<ObjectField>()
                .Where(x => x.bindingPath == property.propertyPath)
                .First();
            Assert.That(field, Is.Null);
        }

        [Test]
        public void GetPropertyHeightReturnsDefaultHeight()
        {
            WInLineEditorPropertyDrawer drawer = new();
            float expected = EditorGUI.GetPropertyHeight(_inlineProperty, true);
            Assert.That(
                drawer.GetPropertyHeight(_inlineProperty, GUIContent.none),
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
