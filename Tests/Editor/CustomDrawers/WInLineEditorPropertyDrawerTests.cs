namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEditor;
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
            WInLineEditorPropertyDrawer.ResetViewWidthResolver();
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
            WInLineEditorPropertyDrawer.ResetViewWidthResolver();
        }

        [Test]
        public void CreatePropertyGUIFallsBackToIMGUI()
        {
            WInLineEditorPropertyDrawer drawer = new();
            VisualElement element = drawer.CreatePropertyGUI(_inlineProperty);
            Assert.That(element, Is.Null);
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
            Assert.That(info.HasSerializedObject, Is.True);
            Assert.That(string.IsNullOrEmpty(info.ErrorMessage), Is.True);
        }

        [UnityTest]
        public IEnumerator OnGUINullReferenceClearsSerializedState()
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
            Assert.That(info.HasSerializedObject, Is.False);
        }

        [UnityTest]
        public IEnumerator OnGUIPreservesIndentWhenFullWidthAlreadyAvailable()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            float height = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect rect = new Rect(40f, 20f, 320f, height);
            float viewWidth =
                rect.x + rect.width + WInLineEditorPropertyDrawer.InlineInspectorRightPadding;
            WInLineEditorPropertyDrawer.SetViewWidthResolver(() => viewWidth);

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

            Assert.That(info.InlineRect.x, Is.EqualTo(rect.x).Within(0.1f));
            Assert.That(info.InlineRect.width, Is.EqualTo(rect.width).Within(0.1f));
            float expectedContentWidth =
                info.InlineRect.width
                - (
                    2f
                    * (
                        WInLineEditorPropertyDrawer.InlineBorderThickness
                        + WInLineEditorPropertyDrawer.InlinePadding
                    )
                );
            Assert.That(info.InspectorRect.width, Is.EqualTo(expectedContentWidth).Within(0.1f));
        }

        [UnityTest]
        public IEnumerator OnGUIExpandsInlineRectWhenViewWidthExceedsParent()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            float height = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect rect = new Rect(40f, 10f, 220f, height);
            float extraWidth = 180f;
            float viewWidth =
                rect.x
                + rect.width
                + extraWidth
                + WInLineEditorPropertyDrawer.InlineInspectorRightPadding;
            WInLineEditorPropertyDrawer.SetViewWidthResolver(() => viewWidth);

            int originalIndent = EditorGUI.indentLevel;
            bool executed = false;
            try
            {
                EditorGUI.indentLevel = 2;
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

            float expectedWidth = rect.width + extraWidth;
            Assert.That(info.InlineRect.x, Is.EqualTo(rect.x).Within(0.1f));
            Assert.That(info.InlineRect.width, Is.EqualTo(expectedWidth).Within(0.1f));

            float expectedContentWidth =
                info.InlineRect.width
                - (
                    2f
                    * (
                        WInLineEditorPropertyDrawer.InlineBorderThickness
                        + WInLineEditorPropertyDrawer.InlinePadding
                    )
                );
            Assert.That(info.InspectorRect.width, Is.EqualTo(expectedContentWidth).Within(0.1f));
        }

        [UnityTest]
        public IEnumerator OnGUIKeepsProvidedWidthWhenViewWidthIsSmaller()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            float height = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect rect = new Rect(24f, 12f, 280f, height);
            float constrainedViewWidth = rect.x + rect.width - 40f; // make available width smaller than rect
            WInLineEditorPropertyDrawer.SetViewWidthResolver(() => constrainedViewWidth);

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

            Assert.That(info.InlineRect.width, Is.EqualTo(rect.width).Within(0.1f));
            float expectedContentWidth =
                info.InlineRect.width
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
