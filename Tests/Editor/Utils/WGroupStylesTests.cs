namespace WallstopStudios.UnityHelpers.Tests.Utils
{
#if UNITY_EDITOR
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Utils.WGroup;

    [TestFixture]
    public sealed class WGroupStylesTests
    {
        [Test]
        public void GetFoldoutStyleReturnsCachedStyleForSameColor()
        {
            Color textColor = new(0.2f, 0.4f, 0.6f, 1f);

            GUIStyle firstCall = WGroupStyles.GetFoldoutStyle(textColor);
            GUIStyle secondCall = WGroupStyles.GetFoldoutStyle(textColor);

            Assert.That(
                ReferenceEquals(firstCall, secondCall),
                Is.True,
                "Same color should return cached style instance."
            );
        }

        [Test]
        public void GetFoldoutStyleReturnsDistinctStylesForDifferentColors()
        {
            Color color1 = new(0.1f, 0.2f, 0.3f, 1f);
            Color color2 = new(0.4f, 0.5f, 0.6f, 1f);

            GUIStyle style1 = WGroupStyles.GetFoldoutStyle(color1);
            GUIStyle style2 = WGroupStyles.GetFoldoutStyle(color2);

            Assert.That(
                ReferenceEquals(style1, style2),
                Is.False,
                "Different colors should return different style instances."
            );
        }

        [Test]
        public void GetHeaderLabelStyleReturnsCachedStyleForSameColor()
        {
            Color textColor = new(0.7f, 0.8f, 0.9f, 1f);

            GUIStyle firstCall = WGroupStyles.GetHeaderLabelStyle(textColor);
            GUIStyle secondCall = WGroupStyles.GetHeaderLabelStyle(textColor);

            Assert.That(
                ReferenceEquals(firstCall, secondCall),
                Is.True,
                "Same color should return cached style instance."
            );
        }

        [Test]
        public void GetHeaderLabelStyleReturnsDistinctStylesForDifferentColors()
        {
            Color color1 = new(0.1f, 0.1f, 0.1f, 1f);
            Color color2 = new(0.9f, 0.9f, 0.9f, 1f);

            GUIStyle style1 = WGroupStyles.GetHeaderLabelStyle(color1);
            GUIStyle style2 = WGroupStyles.GetHeaderLabelStyle(color2);

            Assert.That(
                ReferenceEquals(style1, style2),
                Is.False,
                "Different colors should return different style instances."
            );
        }

        [Test]
        public void FoldoutStyleIsBold()
        {
            Color textColor = new(0.5f, 0.5f, 0.5f, 1f);
            GUIStyle style = WGroupStyles.GetFoldoutStyle(textColor);

            Assert.That(style.fontStyle, Is.EqualTo(FontStyle.Bold));
        }

        [Test]
        public void HeaderLabelStyleHasMiddleLeftAlignment()
        {
            Color textColor = new(0.5f, 0.5f, 0.5f, 1f);
            GUIStyle style = WGroupStyles.GetHeaderLabelStyle(textColor);

            Assert.That(style.alignment, Is.EqualTo(TextAnchor.MiddleLeft));
        }

        [Test]
        public void FoldoutStyleAppliesCorrectTextColor()
        {
            Color textColor = new(0.3f, 0.6f, 0.9f, 1f);
            GUIStyle style = WGroupStyles.GetFoldoutStyle(textColor);

            Assert.That(style.normal.textColor, Is.EqualTo(textColor));
            Assert.That(style.onNormal.textColor, Is.EqualTo(textColor));
            Assert.That(style.active.textColor, Is.EqualTo(textColor));
            Assert.That(style.onActive.textColor, Is.EqualTo(textColor));
            Assert.That(style.focused.textColor, Is.EqualTo(textColor));
            Assert.That(style.onFocused.textColor, Is.EqualTo(textColor));
        }

        [Test]
        public void HeaderLabelStyleAppliesCorrectTextColor()
        {
            Color textColor = new(0.9f, 0.6f, 0.3f, 1f);
            GUIStyle style = WGroupStyles.GetHeaderLabelStyle(textColor);

            Assert.That(style.normal.textColor, Is.EqualTo(textColor));
            Assert.That(style.active.textColor, Is.EqualTo(textColor));
            Assert.That(style.focused.textColor, Is.EqualTo(textColor));
        }

        [Test]
        public void FoldoutStyleHasReasonablePadding()
        {
            Color textColor = new(0.5f, 0.5f, 0.5f, 1f);
            GUIStyle style = WGroupStyles.GetFoldoutStyle(textColor);

            Assert.That(
                style.padding.left,
                Is.InRange(10, 20),
                "Left padding should accommodate foldout arrow."
            );
            Assert.That(
                style.padding.right,
                Is.InRange(2, 10),
                "Right padding should be minimal but present."
            );
            Assert.That(style.padding.top, Is.InRange(0, 6), "Top padding should be minimal.");
            Assert.That(
                style.padding.bottom,
                Is.InRange(0, 6),
                "Bottom padding should be minimal."
            );
        }

        [Test]
        public void HeaderLabelStyleHasMinimalPadding()
        {
            Color textColor = new(0.5f, 0.5f, 0.5f, 1f);
            GUIStyle style = WGroupStyles.GetHeaderLabelStyle(textColor);

            Assert.That(style.padding.left, Is.InRange(2, 8), "Left padding should be minimal.");
            Assert.That(style.padding.right, Is.InRange(2, 8), "Right padding should be minimal.");
        }

        [Test]
        public void GetHeaderHeightIsPositive()
        {
            Color textColor = new(0.5f, 0.5f, 0.5f, 1f);

            float height = WGroupStyles.GetHeaderHeight(textColor);

            Assert.That(height, Is.GreaterThan(0f));
        }

        [Test]
        public void GetHeaderHeightIsAtLeastSingleLineHeight()
        {
            Color textColor = new(0.5f, 0.5f, 0.5f, 1f);

            float height = WGroupStyles.GetHeaderHeight(textColor);

            Assert.That(
                height,
                Is.GreaterThanOrEqualTo(EditorGUIUtility.singleLineHeight),
                "Header height should be at least single line height."
            );
        }

        [Test]
        public void GetHeaderHeightIncludesVerticalPadding()
        {
            Color textColor = new(0.5f, 0.5f, 0.5f, 1f);
            GUIStyle foldoutStyle = WGroupStyles.GetFoldoutStyle(textColor);
            GUIStyle headerStyle = WGroupStyles.GetHeaderLabelStyle(textColor);

            float tallestLineHeight = Mathf.Max(
                Mathf.Max(EditorGUIUtility.singleLineHeight, foldoutStyle.lineHeight),
                Mathf.Max(EditorGUIUtility.singleLineHeight, headerStyle.lineHeight)
            );
            float expectedHeight = tallestLineHeight + WGroupStyles.HeaderVerticalPadding;

            float actualHeight = WGroupStyles.GetHeaderHeight(textColor);

            Assert.That(actualHeight, Is.EqualTo(expectedHeight).Within(0.0001f));
        }

        [Test]
        public void HeaderVerticalPaddingIsPositive()
        {
            Assert.That(
                WGroupStyles.HeaderVerticalPadding,
                Is.GreaterThan(0f),
                "Header vertical padding should be positive."
            );
        }

        [Test]
        public void HeaderPaddingConstantsAreAccessible()
        {
            Assert.That(
                WGroupStyles.HeaderTopPadding,
                Is.GreaterThanOrEqualTo(0f),
                "Header top padding should be non-negative."
            );
            Assert.That(
                WGroupStyles.HeaderBottomPadding,
                Is.GreaterThanOrEqualTo(0f),
                "Header bottom padding should be non-negative."
            );
        }

        [Test]
        public void FoldoutStyleLeftPaddingAccountsForArrowIcon()
        {
            Color textColor = new(0.5f, 0.5f, 0.5f, 1f);
            GUIStyle style = WGroupStyles.GetFoldoutStyle(textColor);

            Assert.That(
                style.padding.left,
                Is.GreaterThanOrEqualTo(12),
                "Foldout left padding should be >= 12px to account for the arrow icon (~12px wide)."
            );
        }

        [Test]
        public void StylesHandleEdgeCaseColors()
        {
            Color transparent = new(0f, 0f, 0f, 0f);
            Color fullAlpha = new(1f, 1f, 1f, 1f);
            Color negativeClamp = new(-0.5f, -0.5f, -0.5f, 1f);

            Assert.DoesNotThrow(() => WGroupStyles.GetFoldoutStyle(transparent));
            Assert.DoesNotThrow(() => WGroupStyles.GetFoldoutStyle(fullAlpha));
            Assert.DoesNotThrow(() => WGroupStyles.GetFoldoutStyle(negativeClamp));

            Assert.DoesNotThrow(() => WGroupStyles.GetHeaderLabelStyle(transparent));
            Assert.DoesNotThrow(() => WGroupStyles.GetHeaderLabelStyle(fullAlpha));
            Assert.DoesNotThrow(() => WGroupStyles.GetHeaderLabelStyle(negativeClamp));
        }

        [Test]
        public void FoldoutAndLabelStylesHaveConsistentRightPaddingRatio()
        {
            Color textColor = new(0.5f, 0.5f, 0.5f, 1f);
            GUIStyle foldoutStyle = WGroupStyles.GetFoldoutStyle(textColor);
            GUIStyle labelStyle = WGroupStyles.GetHeaderLabelStyle(textColor);

            float foldoutRightPadding = foldoutStyle.padding.right;
            float labelRightPadding = labelStyle.padding.right;

            float difference = Mathf.Abs(foldoutRightPadding - labelRightPadding);

            Assert.That(
                difference,
                Is.LessThanOrEqualTo(6f),
                "Right padding should be similar for both styles to maintain visual consistency."
            );
        }
    }
#endif
}
