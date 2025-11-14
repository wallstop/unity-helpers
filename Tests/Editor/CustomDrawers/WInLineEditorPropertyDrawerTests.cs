namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.TestTools;
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Tests.EditorFramework;

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
            WInLineEditorPropertyDrawer.ResetImGuiStateCacheForTesting();
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

            WInLineEditorPropertyDrawer.ResetImGuiStateCacheForTesting();
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
        public void GetPropertyHeightIncludesInlineInspectorWhenExpanded()
        {
            WInLineEditorPropertyDrawer drawer = new();
            float foldoutHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float objectHeight = EditorGUI.GetPropertyHeight(
                _inlineProperty,
                GUIContent.none,
                true
            );
            float inlineHeight = WInLineEditorPropertyDrawer.GetInlineContainerHeightForTesting(
                new WInLineEditorAttribute()
            );
            float expected = foldoutHeight + spacing + objectHeight + spacing + inlineHeight;
            Assert.That(
                drawer.GetPropertyHeight(
                    _inlineProperty,
                    new GUIContent(_inlineProperty.displayName)
                ),
                Is.EqualTo(expected)
            );
        }

        [Test]
        public void GetPropertyHeightFoldoutCollapsedOmitsInlineInspector()
        {
            SerializedProperty property = _serializedHolder.FindProperty(
                nameof(TestHolder.collapsedData)
            );
            SessionState.EraseBool(GetSessionKey(property));

            WInLineEditorPropertyDrawer drawer = new();
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float objectHeight = EditorGUI.GetPropertyHeight(property, GUIContent.none, true);
            float expected = lineHeight + spacing + objectHeight;

            float actual = drawer.GetPropertyHeight(property, new GUIContent(property.displayName));
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void GetPropertyHeightNullReferenceOmitsInlineInspector()
        {
            SerializedProperty property = _serializedHolder.FindProperty(
                nameof(TestHolder.inlineData)
            );
            property.objectReferenceValue = null;
            _serializedHolder.ApplyModifiedProperties();
            _serializedHolder.Update();

            WInLineEditorPropertyDrawer drawer = new();
            float expected = EditorGUI.GetPropertyHeight(
                property,
                new GUIContent(property.displayName),
                true
            );

            Assert.That(
                drawer.GetPropertyHeight(property, new GUIContent(property.displayName)),
                Is.EqualTo(expected)
            );
        }

        [UnityTest]
        public IEnumerator OnGUIWithReferenceBuildsInlineInspectorState()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            float height = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect rect = new Rect(0f, 0f, 400f, height);

            bool executed = false;
            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(rect, _inlineProperty, label);
                executed = true;
            });
            Assert.IsTrue(executed);

            Assert.That(
                WInLineEditorPropertyDrawer.TryGetImGuiStateInfo(
                    sessionKey,
                    out WInLineEditorPropertyDrawer.InlineInspectorImGuiStateInfo info
                ),
                Is.True
            );
            Assert.That(info.Target, Is.EqualTo(_data));
            Assert.That(info.HasEditor, Is.True);
            Assert.That(string.IsNullOrEmpty(info.ErrorMessage), Is.True);
        }

        [UnityTest]
        public IEnumerator OnGUINullReferenceDisposesCachedEditor()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            float height = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect rect = new Rect(0f, 0f, 400f, height);

            yield return TestIMGUIExecutor.Run(() => drawer.OnGUI(rect, _inlineProperty, label));

            _inlineProperty.objectReferenceValue = null;
            _serializedHolder.ApplyModifiedProperties();
            _serializedHolder.Update();

            float collapsedHeight = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect collapsedRect = new Rect(0f, 0f, 400f, collapsedHeight);
            yield return TestIMGUIExecutor.Run(() =>
                drawer.OnGUI(collapsedRect, _inlineProperty, label)
            );

            Assert.That(
                WInLineEditorPropertyDrawer.TryGetImGuiStateInfo(
                    sessionKey,
                    out WInLineEditorPropertyDrawer.InlineInspectorImGuiStateInfo info
                ),
                Is.True
            );
            Assert.That(info.Target, Is.Null);
            Assert.That(info.HasEditor, Is.False);
        }

        [UnityTest]
        public IEnumerator OnGUIRespectsProvidedRectWidthWhenIndented()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            float height = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect rect = new Rect(40f, 20f, 320f, height);

            int originalIndent = EditorGUI.indentLevel;
            bool executed = false;
            try
            {
                EditorGUI.indentLevel = 3;
                yield return TestIMGUIExecutor.Run(() =>
                {
                    drawer.OnGUI(rect, _inlineProperty, label);
                    executed = true;
                });
            }
            finally
            {
                EditorGUI.indentLevel = originalIndent;
            }

            Assert.IsTrue(executed);
            Assert.That(
                WInLineEditorPropertyDrawer.TryGetImGuiStateInfo(
                    sessionKey,
                    out WInLineEditorPropertyDrawer.InlineInspectorImGuiStateInfo info
                ),
                Is.True
            );

            Assert.That(info.InlineRect.width, Is.EqualTo(rect.width).Within(0.1f));
            float expectedContentWidth =
                rect.width
                - (
                    2f
                    * (
                        WInLineEditorPropertyDrawer.InlineBorderThickness
                        + WInLineEditorPropertyDrawer.InlinePadding
                    )
                );
            Assert.That(info.InspectorRect.width, Is.EqualTo(expectedContentWidth).Within(0.1f));
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
