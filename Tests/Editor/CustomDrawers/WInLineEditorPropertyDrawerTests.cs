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
        private SerializedProperty _noMinWidthProperty;

        [SetUp]
        public void SetUp()
        {
            WInLineEditorPropertyDrawer.ResetImGuiStateCacheForTesting();
            WInLineEditorPropertyDrawer.ResetViewWidthResolver();
            WInLineEditorPropertyDrawer.ResetVisibleRectResolver();
            _holder = ScriptableObject.CreateInstance<TestHolder>();
            _data = ScriptableObject.CreateInstance<TestData>();

            _holder.inlineData = _data;
            _holder.collapsedData = _data;
            _holder.headerOnlyData = _data;
            _holder.noMinWidthData = _data;

            _serializedHolder = new SerializedObject(_holder);
            _inlineProperty = _serializedHolder.FindProperty(nameof(TestHolder.inlineData));
            _noMinWidthProperty = _serializedHolder.FindProperty(nameof(TestHolder.noMinWidthData));
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
                SessionState.EraseBool(
                    GetSessionKey(_serializedHolder.FindProperty(nameof(TestHolder.noMinWidthData)))
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
            WInLineEditorPropertyDrawer.ResetVisibleRectResolver();
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
            Assert.That(info.UsesHorizontalScroll, Is.True);
            Assert.That(info.InspectorContentWidth, Is.GreaterThan(info.InspectorRect.width));
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
            Assert.That(info.UsesHorizontalScroll, Is.False);
            Assert.That(
                info.InspectorContentWidth,
                Is.EqualTo(info.InspectorRect.width).Within(0.1f)
            );
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

            yield return TestIMGUIExecutor.Run(() => drawer.OnGUI(rect, _inlineProperty, label));

            float paddedHeight = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect paddedRect = new Rect(rect.x, rect.y, rect.width, paddedHeight);

            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(paddedRect, _inlineProperty, label);
            });

            Assert.That(
                WInLineEditorPropertyDrawer.TryGetImGuiStateInfo(
                    sessionKey,
                    out WInLineEditorPropertyDrawer.InlineInspectorImGuiStateInfo info
                ),
                Is.True
            );

            float expectedInlineHeight =
                WInLineEditorPropertyDrawer.GetInlineContainerHeightForTesting(
                    new WInLineEditorAttribute()
                ) + WInLineEditorPropertyDrawer.InlineHorizontalScrollbarHeight;
            Assert.That(info.InlineRect.width, Is.EqualTo(rect.width).Within(0.1f));
            Assert.That(info.InlineRect.height, Is.EqualTo(expectedInlineHeight).Within(0.1f));
            float expectedInspectorHeight = new WInLineEditorAttribute().inspectorHeight;
            Assert.That(
                info.InspectorRect.height,
                Is.EqualTo(expectedInspectorHeight).Within(0.1f)
            );
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
            Assert.That(info.UsesHorizontalScroll, Is.True);
            Assert.That(info.InspectorContentWidth, Is.GreaterThan(info.InspectorRect.width));
        }

        [UnityTest]
        public IEnumerator OnGUIHorizontalScrollPreservesVerticalPosition()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            float height = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect rect = new Rect(24f, 12f, 260f, height);
            float constrainedViewWidth = rect.x + rect.width - 60f;
            WInLineEditorPropertyDrawer.SetViewWidthResolver(() => constrainedViewWidth);

            yield return TestIMGUIExecutor.Run(() => drawer.OnGUI(rect, _inlineProperty, label));

            float paddedHeight = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect paddedRect = new Rect(rect.x, rect.y, rect.width, paddedHeight);

            Assert.IsTrue(
                WInLineEditorPropertyDrawer.SetScrollPositionForTesting(
                    sessionKey,
                    new Vector2(0f, 64f)
                )
            );

            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(paddedRect, _inlineProperty, label);
            });

            Assert.That(
                WInLineEditorPropertyDrawer.TryGetImGuiStateInfo(
                    sessionKey,
                    out WInLineEditorPropertyDrawer.InlineInspectorImGuiStateInfo info
                ),
                Is.True
            );
            Assert.That(info.UsesHorizontalScroll, Is.True);
            Assert.That(info.ScrollPosition.y, Is.EqualTo(64f).Within(0.1f));
        }

        [UnityTest]
        public IEnumerator OnGUIHorizontalScrollDoesNotShiftInspectorOrigin()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            float height = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect rect = new Rect(24f, 12f, 280f, height);
            WInLineEditorPropertyDrawer.SetViewWidthResolver(() => rect.x + rect.width - 60f);

            yield return TestIMGUIExecutor.Run(() => drawer.OnGUI(rect, _inlineProperty, label));

            float paddedHeight = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect paddedRect = new Rect(rect.x, rect.y, rect.width, paddedHeight);
            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(paddedRect, _inlineProperty, label);
            });

            Assert.That(
                WInLineEditorPropertyDrawer.TryGetImGuiStateInfo(
                    sessionKey,
                    out WInLineEditorPropertyDrawer.InlineInspectorImGuiStateInfo info
                ),
                Is.True
            );

            Assert.That(info.UsesHorizontalScroll, Is.True);
            Assert.That(info.InspectorRect.y, Is.EqualTo(info.ContentRect.y).Within(0.1f));
        }

        [UnityTest]
        public IEnumerator GetPropertyHeightDropsScrollbarPaddingAfterRelease()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);

            WInLineEditorPropertyDrawer.SetViewWidthResolver(() => 320f);
            float narrowHeight = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect narrowRect = new Rect(24f, 12f, 280f, narrowHeight);
            yield return TestIMGUIExecutor.Run(() =>
                drawer.OnGUI(narrowRect, _inlineProperty, label)
            );
            float heightWithScrollbar = drawer.GetPropertyHeight(_inlineProperty, label);

            WInLineEditorPropertyDrawer.SetViewWidthResolver(() => 1800f);
            float wideHeight = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect wideRect = new Rect(24f, 12f, 720f, wideHeight);
            yield return TestIMGUIExecutor.Run(() =>
                drawer.OnGUI(wideRect, _inlineProperty, label)
            );
            float heightWithoutScrollbar = drawer.GetPropertyHeight(_inlineProperty, label);

            Assert.That(
                WInLineEditorPropertyDrawer.TryGetImGuiStateInfo(
                    sessionKey,
                    out WInLineEditorPropertyDrawer.InlineInspectorImGuiStateInfo info
                ),
                Is.True
            );

            Assert.That(
                info.UsesHorizontalScroll,
                Is.False,
                $"Horizontal scroll still active after wide layout. Inline width: {info.InlineRect.width}, content width: {info.InspectorContentWidth}"
            );
            float diff = heightWithScrollbar - heightWithoutScrollbar;
            Assert.That(
                diff,
                Is.EqualTo(WInLineEditorPropertyDrawer.InlineHorizontalScrollbarHeight).Within(0.5f)
            );
        }

        [UnityTest]
        public IEnumerator OnGUIDoesNotForceHorizontalScrollWhenMinimumWidthDisabled()
        {
            string sessionKey = GetSessionKey(_noMinWidthProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_noMinWidthProperty.displayName);
            float height = drawer.GetPropertyHeight(_noMinWidthProperty, label);
            Rect rect = new Rect(24f, 12f, 260f, height);
            WInLineEditorPropertyDrawer.SetViewWidthResolver(() => rect.x + rect.width);

            bool executed = false;
            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(rect, _noMinWidthProperty, label);
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

            Assert.That(info.UsesHorizontalScroll, Is.False);
            Assert.That(
                info.InspectorContentWidth,
                Is.EqualTo(info.InspectorRect.width).Within(0.1f)
            );
        }

        [UnityTest]
        public IEnumerator OnGUIHonorsMinimumWidthEvenIfInlineRectIsNotVisiblyClipped()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            float constrainedViewWidth = 420f;
            WInLineEditorPropertyDrawer.SetViewWidthResolver(() => constrainedViewWidth);
            WInLineEditorPropertyDrawer.SetVisibleRectResolver(() =>
                new Rect(0f, 0f, constrainedViewWidth, 800f)
            );

            float height = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect rect = new Rect(24f, 12f, constrainedViewWidth - 48f, height);

            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(rect, _inlineProperty, label);
            });

            Assert.That(
                WInLineEditorPropertyDrawer.TryGetImGuiStateInfo(
                    sessionKey,
                    out WInLineEditorPropertyDrawer.InlineInspectorImGuiStateInfo info
                ),
                Is.True
            );
            Assert.That(info.UsesHorizontalScroll, Is.True);
            Assert.That(info.InspectorContentWidth, Is.GreaterThan(info.InspectorRect.width));
        }

        [UnityTest]
        public IEnumerator OnGUIClampsWidthWhenVisibleRectIsNarrower()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer.SetViewWidthResolver(() => 520f);
            WInLineEditorPropertyDrawer.SetVisibleRectResolver(() => new Rect(0f, 0f, 260f, 800f));

            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            float height = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect rect = new Rect(40f, 10f, 180f, height);

            yield return TestIMGUIExecutor.Run(() => drawer.OnGUI(rect, _inlineProperty, label));

            float paddedHeight = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect paddedRect = new Rect(rect.x, rect.y, rect.width, paddedHeight);

            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(paddedRect, _inlineProperty, label);
            });

            Assert.That(
                WInLineEditorPropertyDrawer.TryGetImGuiStateInfo(
                    sessionKey,
                    out WInLineEditorPropertyDrawer.InlineInspectorImGuiStateInfo info
                ),
                Is.True
            );

            float maxWidth = 260f - rect.x;
            Assert.That(info.InlineRect.width, Is.LessThanOrEqualTo(maxWidth));
            Assert.That(info.InlineRect.width, Is.GreaterThanOrEqualTo(rect.width - 0.1f));
            Assert.That(info.UsesHorizontalScroll, Is.True);
            Assert.That(info.InspectorContentWidth, Is.GreaterThan(info.InspectorRect.width));
            Assert.That(info.HorizontalScrollOffset, Is.EqualTo(0f).Within(0.001f));
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

            [WInLineEditor(minInspectorWidth: 0f)]
            public TestData noMinWidthData;
        }
    }
}
