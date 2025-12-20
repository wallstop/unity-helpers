namespace WallstopStudios.UnityHelpers.Tests.Utils
{
#if UNITY_EDITOR
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Utils.WGroup;

    [TestFixture]
    public sealed class WGroupHeaderVisualUtilityTests
    {
        private const float ExpectedHorizontalContentPadding = 3f;
        private const float ExpectedFoldoutOffsetInspector = 9f;
        private const float ExpectedFoldoutOffsetSettings = 0f;

        [SetUp]
        public void SetUp()
        {
            WGroupHeaderVisualUtility.IsSettingsContext = false;
        }

        [TearDown]
        public void TearDown()
        {
            WGroupHeaderVisualUtility.IsSettingsContext = false;
        }

        [Test]
        public void InspectorContextGetContentRectAppliesFoldoutOffsetForVisualContainment()
        {
            Rect headerRect = new(0f, 0f, 200f, 24f);

            Rect withoutFoldout = WGroupHeaderVisualUtility.GetContentRect(headerRect, 0f, 0f);
            Rect withFoldout = WGroupHeaderVisualUtility.GetContentRect(headerRect, 0f, 0f, true);

            Assert.That(
                withFoldout.xMin,
                Is.GreaterThan(withoutFoldout.xMin),
                "In Inspector context, foldout content rect should have additional left offset "
                    + "to ensure the arrow is visually contained within the header background."
            );
            Assert.That(withFoldout.xMax, Is.EqualTo(withoutFoldout.xMax).Within(0.0001f));
        }

        [Test]
        public void InspectorContextFoldoutOffsetIs9Pixels()
        {
            Rect headerRect = new(20f, 10f, 300f, 30f);

            Rect withoutFoldout = WGroupHeaderVisualUtility.GetContentRect(headerRect, 0f, 0f);
            Rect withFoldout = WGroupHeaderVisualUtility.GetContentRect(headerRect, 0f, 0f, true);

            float actualOffset = withFoldout.xMin - withoutFoldout.xMin;

            Assert.That(
                actualOffset,
                Is.EqualTo(ExpectedFoldoutOffsetInspector).Within(0.0001f),
                "In Inspector context, foldout offset should be 9px for proper visual containment."
            );
        }

        [Test]
        public void SettingsContextFoldoutOffsetIsZero()
        {
            WGroupHeaderVisualUtility.IsSettingsContext = true;
            Rect headerRect = new(20f, 10f, 300f, 30f);

            Rect withoutFoldout = WGroupHeaderVisualUtility.GetContentRect(headerRect, 0f, 0f);
            Rect withFoldout = WGroupHeaderVisualUtility.GetContentRect(headerRect, 0f, 0f, true);

            float actualOffset = withFoldout.xMin - withoutFoldout.xMin;

            Assert.That(
                actualOffset,
                Is.EqualTo(ExpectedFoldoutOffsetSettings).Within(0.0001f),
                "In Settings context, foldout offset should be 0px to prevent excessive indentation."
            );
        }

        [Test]
        public void SettingsContextFoldoutAndNonFoldoutHaveSameXMin()
        {
            WGroupHeaderVisualUtility.IsSettingsContext = true;
            Rect headerRect = new(0f, 0f, 200f, 24f);

            Rect withoutFoldout = WGroupHeaderVisualUtility.GetContentRect(headerRect, 0f, 0f);
            Rect withFoldout = WGroupHeaderVisualUtility.GetContentRect(headerRect, 0f, 0f, true);

            Assert.That(
                withFoldout.xMin,
                Is.EqualTo(withoutFoldout.xMin).Within(0.0001f),
                "In Settings context, foldout and non-foldout should have same xMin."
            );
        }

        [Test]
        public void SettingsContextScopeSetsIsSettingsContextToTrue()
        {
            Assert.That(
                WGroupHeaderVisualUtility.IsSettingsContext,
                Is.False,
                "Should start in Inspector context."
            );

            using (new WGroupHeaderVisualUtility.SettingsContextScope())
            {
                Assert.That(
                    WGroupHeaderVisualUtility.IsSettingsContext,
                    Is.True,
                    "Should be Settings context inside scope."
                );
            }

            Assert.That(
                WGroupHeaderVisualUtility.IsSettingsContext,
                Is.False,
                "Should return to Inspector context after scope."
            );
        }

        [Test]
        public void SettingsContextScopeRestoresPreviousValue()
        {
            WGroupHeaderVisualUtility.IsSettingsContext = true;

            using (new WGroupHeaderVisualUtility.SettingsContextScope())
            {
                Assert.That(WGroupHeaderVisualUtility.IsSettingsContext, Is.True);
            }

            Assert.That(
                WGroupHeaderVisualUtility.IsSettingsContext,
                Is.True,
                "Should restore previous value (true) after scope."
            );
        }

        [Test]
        public void SettingsContextScopeNestedScopesWorkCorrectly()
        {
            Assert.That(WGroupHeaderVisualUtility.IsSettingsContext, Is.False);

            using (new WGroupHeaderVisualUtility.SettingsContextScope())
            {
                Assert.That(WGroupHeaderVisualUtility.IsSettingsContext, Is.True);

                using (new WGroupHeaderVisualUtility.SettingsContextScope())
                {
                    Assert.That(WGroupHeaderVisualUtility.IsSettingsContext, Is.True);
                }

                Assert.That(
                    WGroupHeaderVisualUtility.IsSettingsContext,
                    Is.True,
                    "Should still be Settings context after inner scope."
                );
            }

            Assert.That(
                WGroupHeaderVisualUtility.IsSettingsContext,
                Is.False,
                "Should return to Inspector context after all scopes."
            );
        }

        [Test]
        public void GetContentRectAppliesHorizontalPaddingFromBothSides()
        {
            Rect headerRect = new(10f, 5f, 200f, 24f);

            Rect contentRect = WGroupHeaderVisualUtility.GetContentRect(headerRect, 0f, 0f);

            float expectedXMin = headerRect.xMin + ExpectedHorizontalContentPadding;
            float expectedXMax = headerRect.xMax - ExpectedHorizontalContentPadding;

            Assert.That(contentRect.xMin, Is.EqualTo(expectedXMin).Within(0.0001f));
            Assert.That(contentRect.xMax, Is.EqualTo(expectedXMax).Within(0.0001f));
        }

        [Test]
        public void InspectorContextFoldoutWidthIsLessThanNonFoldout()
        {
            Rect headerRect = new(20f, 10f, 300f, 30f);

            Rect withoutFoldout = WGroupHeaderVisualUtility.GetContentRect(headerRect, 0f, 0f);
            Rect withFoldout = WGroupHeaderVisualUtility.GetContentRect(headerRect, 0f, 0f, true);

            Assert.That(
                withFoldout.width,
                Is.LessThan(withoutFoldout.width),
                "In Inspector context, foldout width should be less due to additional left offset."
            );
            Assert.That(
                withoutFoldout.width - withFoldout.width,
                Is.EqualTo(ExpectedFoldoutOffsetInspector).Within(0.0001f)
            );
        }

        [Test]
        public void SettingsContextFoldoutWidthEqualToNonFoldout()
        {
            WGroupHeaderVisualUtility.IsSettingsContext = true;
            Rect headerRect = new(20f, 10f, 300f, 30f);

            Rect withoutFoldout = WGroupHeaderVisualUtility.GetContentRect(headerRect, 0f, 0f);
            Rect withFoldout = WGroupHeaderVisualUtility.GetContentRect(headerRect, 0f, 0f, true);

            Assert.That(
                withFoldout.width,
                Is.EqualTo(withoutFoldout.width).Within(0.0001f),
                "In Settings context, foldout width should equal non-foldout width."
            );
        }

        [Test]
        public void GetContentRectPreservesReasonableWidthForSmallRects()
        {
            Rect smallRect = new(0f, 0f, 50f, 24f);

            Rect contentRect = WGroupHeaderVisualUtility.GetContentRect(smallRect, 0f, 0f);

            Assert.That(
                contentRect.width,
                Is.GreaterThan(0f),
                "Content rect should have positive width."
            );
            Assert.That(
                contentRect.width,
                Is.EqualTo(smallRect.width - ExpectedHorizontalContentPadding * 2f).Within(0.0001f)
            );
        }

        [Test]
        public void GetContentRectHandlesZeroWidthRect()
        {
            Rect zeroWidthRect = new(0f, 0f, 0f, 24f);

            Rect contentRect = WGroupHeaderVisualUtility.GetContentRect(zeroWidthRect, 0f, 0f);

            Assert.That(
                contentRect,
                Is.EqualTo(zeroWidthRect),
                "Zero-width rect should be returned unchanged."
            );
        }

        [Test]
        public void GetContentRectHandlesZeroHeightRect()
        {
            Rect zeroHeightRect = new(0f, 0f, 200f, 0f);

            Rect contentRect = WGroupHeaderVisualUtility.GetContentRect(zeroHeightRect, 0f, 0f);

            Assert.That(
                contentRect,
                Is.EqualTo(zeroHeightRect),
                "Zero-height rect should be returned unchanged."
            );
        }

        [Test]
        public void GetContentRectAppliesVerticalPadding()
        {
            Rect headerRect = new(0f, 0f, 200f, 30f);
            float topPadding = 2f;
            float bottomPadding = 3f;

            Rect contentRect = WGroupHeaderVisualUtility.GetContentRect(
                headerRect,
                topPadding,
                bottomPadding
            );

            float expectedYMin = headerRect.yMin + 1f + topPadding;
            float expectedYMax = headerRect.yMax - 3f - bottomPadding;

            Assert.That(contentRect.yMin, Is.EqualTo(expectedYMin).Within(0.0001f));
            Assert.That(contentRect.yMax, Is.EqualTo(expectedYMax).Within(0.0001f));
        }

        [Test]
        public void GetContentRectClampsToValidRangeWhenPaddingExceedsRect()
        {
            Rect tinyRect = new(100f, 50f, 4f, 4f);

            Rect contentRect = WGroupHeaderVisualUtility.GetContentRect(tinyRect, 10f, 10f);

            Assert.That(contentRect.width, Is.GreaterThanOrEqualTo(0f));
            Assert.That(contentRect.height, Is.GreaterThanOrEqualTo(0f));
            Assert.That(contentRect.xMin, Is.LessThanOrEqualTo(contentRect.xMax));
            Assert.That(contentRect.yMin, Is.LessThanOrEqualTo(contentRect.yMax));
        }

        [Test]
        public void FoldoutStyleHasConsistentLeftPaddingForArrowSpace()
        {
            Color textColor = new(0.5f, 0.5f, 0.5f, 1f);
            GUIStyle foldoutStyle = WGroupStyles.GetFoldoutStyle(textColor);

            Assert.That(
                foldoutStyle.padding.left,
                Is.GreaterThanOrEqualTo(12),
                "Foldout style should have sufficient left padding for the arrow icon."
            );
        }

        [Test]
        public void HeaderLabelStyleHasMinimalLeftPadding()
        {
            Color textColor = new(0.5f, 0.5f, 0.5f, 1f);
            GUIStyle labelStyle = WGroupStyles.GetHeaderLabelStyle(textColor);

            Assert.That(
                labelStyle.padding.left,
                Is.LessThanOrEqualTo(8),
                "Label style should have minimal left padding since no arrow space is needed."
            );
        }

        [Test]
        public void FoldoutAndLabelStylesHaveDifferentLeftPaddingForArrowSpace()
        {
            Color textColor = new(0.3f, 0.6f, 0.9f, 1f);
            GUIStyle foldoutStyle = WGroupStyles.GetFoldoutStyle(textColor);
            GUIStyle labelStyle = WGroupStyles.GetHeaderLabelStyle(textColor);

            int paddingDifference = foldoutStyle.padding.left - labelStyle.padding.left;

            Assert.That(
                paddingDifference,
                Is.GreaterThanOrEqualTo(8),
                "Foldout style should have more left padding than label style to accommodate the arrow."
            );
        }

        [Test]
        public void GetHeaderHeightUsesTallestStyleLineHeight()
        {
            Color textColor = new(0.25f, 0.5f, 0.75f, 1f);

            GUIStyle foldoutStyle = WGroupStyles.GetFoldoutStyle(textColor);
            GUIStyle headerStyle = WGroupStyles.GetHeaderLabelStyle(textColor);

            float foldoutLineHeight = Mathf.Max(
                EditorGUIUtility.singleLineHeight,
                foldoutStyle.lineHeight
            );
            float headerLineHeight = Mathf.Max(
                EditorGUIUtility.singleLineHeight,
                headerStyle.lineHeight
            );
            float expectedHeight =
                Mathf.Max(foldoutLineHeight, headerLineHeight) + WGroupStyles.HeaderVerticalPadding;

            Assert.That(
                WGroupStyles.GetHeaderHeight(textColor),
                Is.EqualTo(expectedHeight).Within(0.0001f)
            );
        }

        [Test]
        public void GetHeaderHeightGrowsWhenFoldoutStyleExpands()
        {
            Color textColor = new(0.6f, 0.2f, 0.8f, 1f);
            GUIStyle foldoutStyle = WGroupStyles.GetFoldoutStyle(textColor);
            GUIStyle headerStyle = WGroupStyles.GetHeaderLabelStyle(textColor);

            float baselineHeight = WGroupStyles.GetHeaderHeight(textColor);

            int originalFontSize = foldoutStyle.fontSize;
            int newFontSize = originalFontSize <= 0 ? 18 : originalFontSize + 12;

            try
            {
                foldoutStyle.fontSize = newFontSize;

                float foldoutLineHeight = Mathf.Max(
                    EditorGUIUtility.singleLineHeight,
                    foldoutStyle.lineHeight
                );
                float headerLineHeight = Mathf.Max(
                    EditorGUIUtility.singleLineHeight,
                    headerStyle.lineHeight
                );
                float expectedHeight =
                    Mathf.Max(foldoutLineHeight, headerLineHeight)
                    + WGroupStyles.HeaderVerticalPadding;

                float updatedHeight = WGroupStyles.GetHeaderHeight(textColor);

                Assert.That(updatedHeight, Is.EqualTo(expectedHeight).Within(0.0001f));
                Assert.That(updatedHeight, Is.GreaterThan(baselineHeight + 0.0001f));
            }
            finally
            {
                foldoutStyle.fontSize = originalFontSize;
            }
        }
    }
#endif
}
