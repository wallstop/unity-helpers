namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers
{
    using System;
    using System.Collections;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using UnityEngine.UIElements;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Tests.EditorFramework;
    using Object = UnityEngine.Object;

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
            OverrideViewWidth(() => 2000f);
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

        [Test]
        public void ExpandRectIgnoresNestingShrinkWhenGroupPaddingActive()
        {
            Rect probeRect = new Rect(36f, 12f, 220f, 64f);
            OverrideViewWidth(() => 820f, () => BuildVisibleRect(probeRect, 820f));

            Rect expandedWithoutPadding = WInLineEditorPropertyDrawer.ExpandRectForTesting(
                probeRect,
                out float widthWithoutPadding,
                out _
            );

            float horizontalPadding = GroupGUIWidthUtility.CalculateHorizontalPadding(
                EditorStyles.helpBox,
                out float leftPadding,
                out float rightPadding
            );

            Rect expandedWithPadding;
            using (
                GroupGUIWidthUtility.PushContentPadding(
                    horizontalPadding,
                    leftPadding,
                    rightPadding
                )
            )
            {
                expandedWithPadding = WInLineEditorPropertyDrawer.ExpandRectForTesting(
                    probeRect,
                    out float widthWithPadding,
                    out _
                );

                Assert.That(widthWithPadding, Is.EqualTo(widthWithoutPadding).Within(0.001f));
            }

            Assert.That(
                expandedWithPadding.width,
                Is.EqualTo(expandedWithoutPadding.width).Within(0.001f)
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
            Rect visibleRect = BuildVisibleRect(rect, viewWidth);
            OverrideViewWidth(() => viewWidth, () => visibleRect);

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
            float preserveTolerance = InlineWidthTolerance + 0.5f;
            float expectedInlineWidth = ResolveExpectedInlineWidth(rect);
            float widthDelta = Mathf.Abs(info.InlineRect.width - expectedInlineWidth);
            Assert.That(widthDelta, Is.LessThanOrEqualTo(preserveTolerance));
            float expectedContentWidth = CalculateExpectedInspectorWidth(info);
            float contentWidthDelta = Mathf.Abs(info.InspectorRect.width - expectedContentWidth);
            Assert.That(contentWidthDelta, Is.LessThanOrEqualTo(preserveTolerance));
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
            const float extraWidth = 360f;
            float viewWidth =
                rect.x
                + rect.width
                + extraWidth
                + WInLineEditorPropertyDrawer.InlineInspectorRightPadding;
            Rect visibleRect = BuildVisibleRect(rect, viewWidth);
            OverrideViewWidth(() => viewWidth, () => visibleRect);

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

            Assert.That(info.InlineRect.x, Is.EqualTo(rect.x).Within(0.1f));
            float widthTolerance = InlineWidthTolerance + 0.5f;
            float expectedWidth = ResolveExpectedInlineWidth(rect);
            float expectedComparisonDelta = Mathf.Abs(info.InlineRect.width - expectedWidth);
            Assert.That(expectedComparisonDelta, Is.LessThanOrEqualTo(widthTolerance));
            float minimumGain = rect.width + extraWidth - widthTolerance;
            Assert.That(info.InlineRect.width, Is.GreaterThanOrEqualTo(minimumGain));

            float expectedContentWidth = CalculateExpectedInspectorWidth(info);
            float inspectorWidthDelta = Mathf.Abs(info.InspectorRect.width - expectedContentWidth);
            Assert.That(inspectorWidthDelta, Is.LessThanOrEqualTo(widthTolerance));
            Assert.That(info.UsesHorizontalScroll, Is.False);
            expectedContentWidth = CalculateExpectedInspectorWidth(info);
            float contentWidthDelta = Mathf.Abs(info.InspectorContentWidth - expectedContentWidth);
            Assert.That(contentWidthDelta, Is.LessThanOrEqualTo(widthTolerance));
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
            Rect visibleRect = BuildVisibleRect(rect, constrainedViewWidth);
            OverrideViewWidth(() => constrainedViewWidth, () => visibleRect);

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
            Assert.That(info.InlineRect.height, Is.EqualTo(expectedInlineHeight).Within(0.1f));
            float expectedInspectorHeight = new WInLineEditorAttribute().inspectorHeight;
            Assert.That(
                info.InspectorRect.height,
                Is.EqualTo(expectedInspectorHeight).Within(0.1f)
            );
            float keepTolerance = InlineWidthTolerance + 0.5f;
            float expectedContentWidth = CalculateExpectedInspectorWidth(info);
            float inspectorWidthDelta = Mathf.Abs(info.InspectorRect.width - expectedContentWidth);
            Assert.That(inspectorWidthDelta, Is.LessThanOrEqualTo(keepTolerance));
            Assert.That(info.UsesHorizontalScroll, Is.True);
            float preservedWidthDelta = Mathf.Abs(info.InlineRect.width - rect.width);
            Assert.That(preservedWidthDelta, Is.LessThanOrEqualTo(keepTolerance));
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
            OverrideViewWidth(() => constrainedViewWidth);

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
            float inspectorContentHeight =
                info.InspectorContentHeight > 0.5f
                    ? info.InspectorContentHeight
                    : new WInLineEditorAttribute().inspectorHeight;
            float maxVerticalScroll = Mathf.Max(
                0f,
                inspectorContentHeight - info.InspectorRect.height
            );
            float expectedScroll = Mathf.Min(64f, maxVerticalScroll);
            Assert.That(info.ScrollPosition.y, Is.EqualTo(expectedScroll).Within(0.1f));
        }

        [UnityTest]
        public IEnumerator OnGUIHorizontalScrollDoesNotShiftInspectorOrigin()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            float height = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect rect = new Rect(24f, 12f, 280f, height);
            OverrideViewWidth(() => rect.x + rect.width - 60f);

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
            float expectedOffset =
                EditorGUIUtility.singleLineHeight + WInLineEditorPropertyDrawer.InlineHeaderSpacing;
            Assert.That(
                info.InspectorRect.y,
                Is.EqualTo(info.ContentRect.y + expectedOffset).Within(0.1f)
            );
        }

        [UnityTest]
        public IEnumerator GetPropertyHeightDropsScrollbarPaddingAfterRelease()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            float simulatedViewWidth = 320f;
            OverrideViewWidth(() => simulatedViewWidth);

            float narrowHeight = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect narrowRect = new Rect(24f, 12f, 280f, narrowHeight);
            yield return TestIMGUIExecutor.Run(() =>
                drawer.OnGUI(narrowRect, _inlineProperty, label)
            );
            float heightWithScrollbar = drawer.GetPropertyHeight(_inlineProperty, label);

            simulatedViewWidth = 1800f;
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
            float reserveDrop = heightWithScrollbar - heightWithoutScrollbar;
            Assert.That(
                reserveDrop,
                Is.GreaterThanOrEqualTo(
                    WInLineEditorPropertyDrawer.InlineHorizontalScrollbarHeight - 0.5f
                )
            );
            Assert.That(
                WInLineEditorPropertyDrawer.HasHorizontalScrollbarReservationForTesting(sessionKey),
                Is.False
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
            OverrideViewWidth(() => rect.x + rect.width);

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
        }

        [UnityTest]
        public IEnumerator OnGUIHonorsMinimumWidthEvenIfInlineRectIsNotVisiblyClipped()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            float constrainedViewWidth = 420f;
            OverrideViewWidth(() => constrainedViewWidth);
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
        public IEnumerator OnGUIHorizontalScrollOffsetIgnoresInternalScrollViewState()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            float height = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect rect = new Rect(24f, 12f, 260f, height);
            OverrideViewWidth(() => rect.x + rect.width - 120f);

            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(rect, _inlineProperty, label);
            });

            Assert.IsTrue(
                WInLineEditorPropertyDrawer.SetHorizontalScrollOffsetForTesting(sessionKey, 160f)
            );
            Assert.IsTrue(
                WInLineEditorPropertyDrawer.SetScrollPositionForTesting(
                    sessionKey,
                    new Vector2(45f, 32f)
                )
            );

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
            Assert.That(info.HorizontalScrollOffset, Is.EqualTo(160f).Within(0.1f));
            Assert.That(info.ScrollPosition.x, Is.EqualTo(0f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator OnGUIHorizontalScrollOffsetRemainsStableInsideGroupPadding()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            float rectWidth = 260f;
            Rect baseRect = new Rect(32f, 16f, rectWidth, 0f);
            OverrideViewWidth(() => baseRect.x + baseRect.width - 110f);

            float totalPadding = GroupGUIWidthUtility.CalculateHorizontalPadding(
                EditorStyles.helpBox,
                out float leftPadding,
                out float rightPadding
            );

            float height;
            using (GroupGUIWidthUtility.PushContentPadding(totalPadding, leftPadding, rightPadding))
            {
                height = drawer.GetPropertyHeight(_inlineProperty, label);
            }

            Rect rect = new Rect(baseRect.x, baseRect.y, baseRect.width, height);

            yield return TestIMGUIExecutor.Run(() =>
            {
                using (
                    GroupGUIWidthUtility.PushContentPadding(totalPadding, leftPadding, rightPadding)
                )
                {
                    drawer.OnGUI(rect, _inlineProperty, label);
                }
            });

            Assert.IsTrue(
                WInLineEditorPropertyDrawer.SetHorizontalScrollOffsetForTesting(sessionKey, 140f)
            );

            yield return TestIMGUIExecutor.Run(() =>
            {
                using (
                    GroupGUIWidthUtility.PushContentPadding(totalPadding, leftPadding, rightPadding)
                )
                {
                    drawer.OnGUI(rect, _inlineProperty, label);
                }
            });

            Assert.That(
                WInLineEditorPropertyDrawer.TryGetImGuiStateInfo(
                    sessionKey,
                    out WInLineEditorPropertyDrawer.InlineInspectorImGuiStateInfo info
                ),
                Is.True
            );
            Assert.That(info.UsesHorizontalScroll, Is.True);
            Assert.That(info.HorizontalScrollOffset, Is.EqualTo(140f).Within(0.1f));
        }

        [UnityTest]
        public IEnumerator OnGUIVerticalScrollOffsetPersistsInsideGroup()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            Rect baseRect = new Rect(30f, 18f, 280f, 0f);
            OverrideViewWidth(() => baseRect.x + baseRect.width - 40f);

            float horizontalPadding = GroupGUIWidthUtility.CalculateHorizontalPadding(
                EditorStyles.helpBox,
                out float leftPadding,
                out float rightPadding
            );

            float height;
            using (
                GroupGUIWidthUtility.PushContentPadding(
                    horizontalPadding,
                    leftPadding,
                    rightPadding
                )
            )
            {
                height = drawer.GetPropertyHeight(_inlineProperty, label);
            }

            Rect rect = new Rect(baseRect.x, baseRect.y, baseRect.width, height);

            yield return TestIMGUIExecutor.Run(() =>
            {
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        leftPadding,
                        rightPadding
                    )
                )
                {
                    drawer.OnGUI(rect, _inlineProperty, label);
                }
            });

            Assert.That(
                WInLineEditorPropertyDrawer.TryGetImGuiStateInfo(
                    sessionKey,
                    out WInLineEditorPropertyDrawer.InlineInspectorImGuiStateInfo info
                ),
                Is.True
            );
            Assert.That(info.UsesVerticalScroll, Is.True);

            Assert.IsTrue(
                WInLineEditorPropertyDrawer.SetScrollPositionForTesting(
                    sessionKey,
                    new Vector2(0f, 140f)
                )
            );

            yield return TestIMGUIExecutor.Run(() =>
            {
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        leftPadding,
                        rightPadding
                    )
                )
                {
                    drawer.OnGUI(rect, _inlineProperty, label);
                }
            });

            Assert.That(
                WInLineEditorPropertyDrawer.TryGetImGuiStateInfo(sessionKey, out info),
                Is.True
            );
            Assert.That(info.UsesVerticalScroll, Is.True);
            Assert.That(info.ScrollPosition.y, Is.EqualTo(140f).Within(0.1f));
        }

        [UnityTest]
        public IEnumerator OnGUIHorizontalScrollOffsetPersistsAcrossLayoutWidthFlips()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            float narrowViewWidth = 320f;
            float rectWidth = 260f;
            OverrideViewWidth(() => narrowViewWidth);

            float height = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect rect = new Rect(30f, 18f, rectWidth, height);

            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(rect, _inlineProperty, label);
            });

            Assert.IsTrue(
                WInLineEditorPropertyDrawer.SetHorizontalScrollOffsetForTesting(sessionKey, 90f)
            );

            float wideViewWidth = 1200f;
            OverrideViewWidth(() => wideViewWidth);
            drawer.GetPropertyHeight(_inlineProperty, label);

            OverrideViewWidth(() => narrowViewWidth);
            height = drawer.GetPropertyHeight(_inlineProperty, label);
            rect.height = height;

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
            Assert.That(info.HorizontalScrollOffset, Is.EqualTo(90f).Within(0.1f));
        }

        [UnityTest]
        public IEnumerator OnGUILayoutPassReusesCommittedInlineWidth()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            float currentViewWidth = 360f;
            OverrideViewWidth(() => currentViewWidth);

            float height = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect rect = new Rect(28f, 16f, 260f, height);

            yield return TestIMGUIExecutor.Run(() => drawer.OnGUI(rect, _inlineProperty, label));

            Assert.That(
                WInLineEditorPropertyDrawer.TryGetImGuiStateInfo(
                    sessionKey,
                    out WInLineEditorPropertyDrawer.InlineInspectorImGuiStateInfo initialInfo
                ),
                Is.True
            );
            float initialInlineWidth = initialInfo.InlineRect.width;

            float layoutMeasuredWidth = -1f;
            float repaintMeasuredWidth = -1f;
            currentViewWidth = 1480f;
            height = drawer.GetPropertyHeight(_inlineProperty, label);
            rect.height = height;

            yield return TestIMGUIExecutor.Run(() =>
            {
                drawer.OnGUI(rect, _inlineProperty, label);
                if (
                    !WInLineEditorPropertyDrawer.TryGetImGuiStateInfo(
                        sessionKey,
                        out WInLineEditorPropertyDrawer.InlineInspectorImGuiStateInfo info
                    )
                )
                {
                    return;
                }

                if (Event.current.type == EventType.Layout)
                {
                    layoutMeasuredWidth = info.InlineRect.width;
                }
                else if (Event.current.type == EventType.Repaint)
                {
                    repaintMeasuredWidth = info.InlineRect.width;
                }
            });

            float preserveTolerance = InlineWidthTolerance + 0.5f;
            Assert.That(layoutMeasuredWidth, Is.GreaterThan(0f));
            Assert.That(
                layoutMeasuredWidth,
                Is.EqualTo(initialInlineWidth).Within(preserveTolerance)
            );
            Assert.That(
                repaintMeasuredWidth,
                Is.GreaterThan(layoutMeasuredWidth + InlineWidthTolerance)
            );
        }

        [UnityTest]
        public IEnumerator HorizontalScrollbarReservationReleasesOnlyAfterStableRepaints()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            float narrowViewWidth = 320f;
            OverrideViewWidth(() => narrowViewWidth);

            float height = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect rect = new Rect(30f, 18f, 260f, height);

            yield return TestIMGUIExecutor.Run(() => drawer.OnGUI(rect, _inlineProperty, label));
            Assert.That(
                WInLineEditorPropertyDrawer.HasHorizontalScrollbarReservationForTesting(sessionKey),
                Is.True
            );

            float wideViewWidth = 1600f;
            OverrideViewWidth(() => wideViewWidth);
            int releaseThreshold =
                WInLineEditorPropertyDrawer.InlineHorizontalScrollbarReleaseRepaintThreshold;
            int iterationsToHold = Math.Max(0, releaseThreshold - 1);

            for (int i = 0; i < iterationsToHold; i++)
            {
                float wideHeight = drawer.GetPropertyHeight(_inlineProperty, label);
                Rect wideRect = new Rect(rect.x, rect.y, rect.width, wideHeight);
                yield return TestIMGUIExecutor.Run(() =>
                    drawer.OnGUI(wideRect, _inlineProperty, label)
                );

                Assert.That(
                    WInLineEditorPropertyDrawer.HasHorizontalScrollbarReservationForTesting(
                        sessionKey
                    ),
                    Is.True,
                    $"Reservation released too early on iteration {i}."
                );
            }

            float finalHeight = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect finalRect = new Rect(rect.x, rect.y, rect.width, finalHeight);
            yield return TestIMGUIExecutor.Run(() =>
                drawer.OnGUI(finalRect, _inlineProperty, label)
            );

            Assert.That(
                WInLineEditorPropertyDrawer.HasHorizontalScrollbarReservationForTesting(sessionKey),
                Is.False
            );
        }

        [UnityTest]
        public IEnumerator DiagnosticsRecordingCapturesLayoutSamples()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            bool originalRecording = WInLineEditorDiagnostics.RecordingEnabled;
            bool originalConsole = WInLineEditorDiagnostics.ConsoleLoggingEnabled;
            WInLineEditorDiagnostics.ClearAllDiagnostics();
            WInLineEditorDiagnostics.RecordingEnabled = true;
            WInLineEditorDiagnostics.ConsoleLoggingEnabled = false;

            try
            {
                WInLineEditorPropertyDrawer drawer = new();
                GUIContent label = new GUIContent(_inlineProperty.displayName);
                float viewWidth = 420f;
                OverrideViewWidth(() => viewWidth);

                float height = drawer.GetPropertyHeight(_inlineProperty, label);
                Rect rect = new Rect(30f, 20f, 280f, height);

                yield return TestIMGUIExecutor.Run(() =>
                {
                    drawer.OnGUI(rect, _inlineProperty, label);
                });

                Assert.That(
                    WInLineEditorDiagnostics.TryGetLayoutSamples(sessionKey, out var samples),
                    Is.True
                );
                Assert.That(samples.Length, Is.GreaterThan(0));
                Assert.That(samples[^1].InlineRectWidth, Is.GreaterThan(0f));
                Assert.That(samples[^1].ResolvedViewWidth, Is.GreaterThan(0f));
                Assert.That(samples[^1].VisibleRectWidth, Is.GreaterThan(0f));

                Assert.That(
                    WInLineEditorDiagnostics.TryGetReservationSamples(
                        sessionKey,
                        out var reservations
                    ),
                    Is.True
                );
                Assert.That(reservations.Length, Is.GreaterThan(0));
            }
            finally
            {
                WInLineEditorDiagnostics.ClearAllDiagnostics();
                WInLineEditorDiagnostics.RecordingEnabled = originalRecording;
                WInLineEditorDiagnostics.ConsoleLoggingEnabled = originalConsole;
            }
        }

        [UnityTest]
        public IEnumerator OnGUIHorizontalScrollOffsetPersistsWhenScrollbarsToggleInGroup()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            float narrowViewWidth = 340f;
            float wideViewWidth = 1020f;
            float rectWidth = 260f;
            Rect rect = new Rect(32f, 18f, rectWidth, 0f);

            float horizontalPadding = GroupGUIWidthUtility.CalculateHorizontalPadding(
                EditorStyles.helpBox,
                out float leftPadding,
                out float rightPadding
            );

            OverrideViewWidth(() => narrowViewWidth, () => BuildVisibleRect(rect, narrowViewWidth));

            float height;
            using (
                GroupGUIWidthUtility.PushContentPadding(
                    horizontalPadding,
                    leftPadding,
                    rightPadding
                )
            )
            {
                height = drawer.GetPropertyHeight(_inlineProperty, label);
            }
            rect.height = height;

            yield return TestIMGUIExecutor.Run(() =>
            {
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        leftPadding,
                        rightPadding
                    )
                )
                {
                    drawer.OnGUI(rect, _inlineProperty, label);
                }
            });

            Assert.IsTrue(
                WInLineEditorPropertyDrawer.SetHorizontalScrollOffsetForTesting(sessionKey, 120f)
            );

            using (
                GroupGUIWidthUtility.PushContentPadding(
                    horizontalPadding,
                    leftPadding,
                    rightPadding
                )
            )
            {
                height = drawer.GetPropertyHeight(_inlineProperty, label);
            }
            rect.height = height;

            yield return TestIMGUIExecutor.Run(() =>
            {
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        leftPadding,
                        rightPadding
                    )
                )
                {
                    drawer.OnGUI(rect, _inlineProperty, label);
                }
            });

            Assert.That(
                WInLineEditorPropertyDrawer.TryGetImGuiStateInfo(
                    sessionKey,
                    out WInLineEditorPropertyDrawer.InlineInspectorImGuiStateInfo info
                ),
                Is.True
            );
            Assert.That(info.UsesHorizontalScroll, Is.True);
            Assert.That(info.HorizontalScrollOffset, Is.EqualTo(120f).Within(0.1f));

            OverrideViewWidth(() => wideViewWidth, () => BuildVisibleRect(rect, wideViewWidth));

            using (
                GroupGUIWidthUtility.PushContentPadding(
                    horizontalPadding,
                    leftPadding,
                    rightPadding
                )
            )
            {
                height = drawer.GetPropertyHeight(_inlineProperty, label);
            }
            rect.height = height;

            yield return TestIMGUIExecutor.Run(() =>
            {
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        leftPadding,
                        rightPadding
                    )
                )
                {
                    drawer.OnGUI(rect, _inlineProperty, label);
                }
            });

            Assert.That(
                WInLineEditorPropertyDrawer.TryGetImGuiStateInfo(sessionKey, out info),
                Is.True
            );
            Assert.That(info.UsesHorizontalScroll, Is.False);
            Assert.That(info.HorizontalScrollOffset, Is.EqualTo(120f).Within(0.1f));

            OverrideViewWidth(() => narrowViewWidth, () => BuildVisibleRect(rect, narrowViewWidth));

            using (
                GroupGUIWidthUtility.PushContentPadding(
                    horizontalPadding,
                    leftPadding,
                    rightPadding
                )
            )
            {
                height = drawer.GetPropertyHeight(_inlineProperty, label);
            }
            rect.height = height;

            yield return TestIMGUIExecutor.Run(() =>
            {
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        leftPadding,
                        rightPadding
                    )
                )
                {
                    drawer.OnGUI(rect, _inlineProperty, label);
                }
            });

            Assert.That(
                WInLineEditorPropertyDrawer.TryGetImGuiStateInfo(sessionKey, out info),
                Is.True
            );
            Assert.That(info.UsesHorizontalScroll, Is.True);
            Assert.That(info.HorizontalScrollOffset, Is.EqualTo(120f).Within(0.1f));
        }

        [UnityTest]
        public IEnumerator OnGUIHorizontalScrollContentWidthPersistsWhileOffsetPositive()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            float initialViewWidth = 360f;
            Rect rect = new Rect(26f, 18f, initialViewWidth - 48f, 0f);

            float horizontalPadding = GroupGUIWidthUtility.CalculateHorizontalPadding(
                EditorStyles.helpBox,
                out float leftPadding,
                out float rightPadding
            );
            OverrideViewWidth(
                () => initialViewWidth,
                () => BuildVisibleRect(rect, initialViewWidth)
            );

            float height;
            using (
                GroupGUIWidthUtility.PushContentPadding(
                    horizontalPadding,
                    leftPadding,
                    rightPadding
                )
            )
            {
                height = drawer.GetPropertyHeight(_inlineProperty, label);
            }
            rect.height = height;

            yield return TestIMGUIExecutor.Run(() =>
            {
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        leftPadding,
                        rightPadding
                    )
                )
                {
                    drawer.OnGUI(rect, _inlineProperty, label);
                }
            });

            Assert.That(
                WInLineEditorPropertyDrawer.TryGetImGuiStateInfo(
                    sessionKey,
                    out WInLineEditorPropertyDrawer.InlineInspectorImGuiStateInfo info
                ),
                Is.True
            );
            Assert.That(info.UsesHorizontalScroll, Is.True);
            float firstWidth = info.InspectorContentWidth;
            Assert.Greater(firstWidth, info.InspectorRect.width);

            Assert.IsTrue(
                WInLineEditorPropertyDrawer.SetHorizontalScrollOffsetForTesting(sessionKey, 90f)
            );

            float narrowerViewWidth = 300f;
            OverrideViewWidth(
                () => narrowerViewWidth,
                () => BuildVisibleRect(rect, narrowerViewWidth)
            );

            using (
                GroupGUIWidthUtility.PushContentPadding(
                    horizontalPadding,
                    leftPadding,
                    rightPadding
                )
            )
            {
                height = drawer.GetPropertyHeight(_inlineProperty, label);
            }
            rect.height = height;

            yield return TestIMGUIExecutor.Run(() =>
            {
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        leftPadding,
                        rightPadding
                    )
                )
                {
                    drawer.OnGUI(rect, _inlineProperty, label);
                }
            });

            Assert.That(
                WInLineEditorPropertyDrawer.TryGetImGuiStateInfo(sessionKey, out info),
                Is.True
            );
            Assert.That(info.UsesHorizontalScroll, Is.True);
            Assert.That(info.InspectorContentWidth, Is.GreaterThanOrEqualTo(firstWidth));
            Assert.That(info.HorizontalScrollOffset, Is.EqualTo(90f).Within(0.1f));
            Assert.That(
                WInLineEditorPropertyDrawer.HasHorizontalScrollbarReservationForTesting(sessionKey),
                Is.True
            );
        }

        [UnityTest]
        public IEnumerator OnGUIHorizontalScrollOffsetReachedMaxDoesNotBounceInsideGroup()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            float constrainedWidth = 320f;
            Rect rect = new Rect(28f, 14f, constrainedWidth, 0f);

            float horizontalPadding = GroupGUIWidthUtility.CalculateHorizontalPadding(
                EditorStyles.helpBox,
                out float leftPadding,
                out float rightPadding
            );

            OverrideViewWidth(
                () => constrainedWidth,
                () => BuildVisibleRect(rect, constrainedWidth)
            );

            float height;
            using (
                GroupGUIWidthUtility.PushContentPadding(
                    horizontalPadding,
                    leftPadding,
                    rightPadding
                )
            )
            {
                height = drawer.GetPropertyHeight(_inlineProperty, label);
            }
            rect.height = height;

            yield return TestIMGUIExecutor.Run(() =>
            {
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        leftPadding,
                        rightPadding
                    )
                )
                {
                    drawer.OnGUI(rect, _inlineProperty, label);
                }
            });

            Assert.That(
                WInLineEditorPropertyDrawer.TryGetImGuiStateInfo(
                    sessionKey,
                    out WInLineEditorPropertyDrawer.InlineInspectorImGuiStateInfo info
                ),
                Is.True
            );
            Assert.That(info.UsesHorizontalScroll, Is.True);
            float expectedMax = Mathf.Max(
                0f,
                info.InspectorContentWidth - info.EffectiveViewportWidth
            );
            Assert.Greater(expectedMax, 10f, "Expected horizontal scroll range to be significant.");

            Assert.IsTrue(
                WInLineEditorPropertyDrawer.SetHorizontalScrollOffsetForTesting(
                    sessionKey,
                    expectedMax
                )
            );

            yield return TestIMGUIExecutor.Run(() =>
            {
                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        horizontalPadding,
                        leftPadding,
                        rightPadding
                    )
                )
                {
                    drawer.OnGUI(rect, _inlineProperty, label);
                }
            });

            Assert.That(
                WInLineEditorPropertyDrawer.TryGetImGuiStateInfo(sessionKey, out info),
                Is.True
            );
            Assert.That(info.UsesHorizontalScroll, Is.True);
            Assert.That(info.HorizontalScrollOffset, Is.EqualTo(expectedMax).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator OnGUIUsesExternalVerticalScrollbarForTallInspectors()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            WInLineEditorPropertyDrawer drawer = new();
            GUIContent label = new GUIContent(_inlineProperty.displayName);
            float height = drawer.GetPropertyHeight(_inlineProperty, label);
            Rect rect = new Rect(24f, 12f, 280f, height);
            OverrideViewWidth(() => rect.x + rect.width);

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
            Assert.That(info.UsesVerticalScroll, Is.True);
            Assert.That(info.InspectorContentHeight, Is.GreaterThan(info.InspectorRect.height));
        }

        [UnityTest]
        public IEnumerator OnGUIClampsWidthWhenVisibleRectIsNarrower()
        {
            string sessionKey = GetSessionKey(_inlineProperty);
            OverrideViewWidth(() => 520f);
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

        private static void OverrideViewWidth(
            Func<float> viewWidthResolver,
            Func<Rect> visibleRectResolver = null
        )
        {
            if (viewWidthResolver == null)
            {
                return;
            }

            WInLineEditorPropertyDrawer.SetViewWidthResolver(viewWidthResolver);
            if (visibleRectResolver != null)
            {
                WInLineEditorPropertyDrawer.SetVisibleRectResolver(visibleRectResolver);
            }
            else
            {
                WInLineEditorPropertyDrawer.SetVisibleRectResolver(() =>
                {
                    float width = viewWidthResolver();
                    return new Rect(0f, 0f, Mathf.Max(0f, width), 4096f);
                });
            }
        }

        private static float ResolveExpectedInlineWidth(Rect rect)
        {
            Rect probeRect = new Rect(rect.x, rect.y, rect.width, 10f);
            Rect expandedRect = WInLineEditorPropertyDrawer.ExpandRectForTesting(
                probeRect,
                out _,
                out _
            );
            return expandedRect.width;
        }

        private const float InlineWidthTolerance =
            WInLineEditorPropertyDrawer.InlineGroupEdgePadding
            + WInLineEditorPropertyDrawer.InlineGroupNestingPadding;

        private static Rect BuildVisibleRect(Rect rect, float viewWidth)
        {
            float width = Mathf.Max(0f, viewWidth);
            return new Rect(
                rect.x - WInLineEditorPropertyDrawer.InlineGroupEdgePadding,
                0f,
                width,
                4096f
            );
        }

        private static string GetSessionKey(SerializedProperty property)
        {
            return $"WInLineEditor:{property.serializedObject.targetObject.GetInstanceID()}:{property.propertyPath}";
        }

        private static float CalculateExpectedInspectorWidth(
            WInLineEditorPropertyDrawer.InlineInspectorImGuiStateInfo info
        )
        {
            float width =
                info.InlineRect.width
                - (
                    2f
                    * (
                        WInLineEditorPropertyDrawer.InlineBorderThickness
                        + WInLineEditorPropertyDrawer.InlinePadding
                    )
                );
            if (info.UsesVerticalScroll)
            {
                width -= WInLineEditorPropertyDrawer.InlineVerticalScrollbarWidth;
            }

            return width;
        }

        [CreateAssetMenu]
        private sealed class TestData : ScriptableObject
        {
            public int value = 10;
            public string text = "Inline";
            public Vector4 vector = new(1f, 2f, 3f, 4f);
            public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            public Color color = Color.cyan;
            public int[] numbers = new int[8];

            [TextArea]
            public string description =
                "Line 1 for scrolling\nLine 2 for scrolling\nLine 3 for scrolling";
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
