namespace WallstopStudios.UnityHelpers.Tests.Utils
{
#if UNITY_EDITOR
    using System;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Editor.Utils.WGroup;

    [TestFixture]
    public sealed class WGroupIndentationTests
    {
        private const float ExpectedFoldoutOffsetInspector = 9f;
        private const float ExpectedFoldoutOffsetSettings = 0f;
        private int _originalIndentLevel;

        [SetUp]
        public void SetUp()
        {
            _originalIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            GroupGUIWidthUtility.ResetForTests();
            WGroupHeaderVisualUtility.IsSettingsContext = false;
        }

        [TearDown]
        public void TearDown()
        {
            EditorGUI.indentLevel = _originalIndentLevel;
            GroupGUIWidthUtility.ResetForTests();
            WGroupHeaderVisualUtility.IsSettingsContext = false;
        }

        [Test]
        public void GetContentRectHorizontalPaddingIsMinimal()
        {
            Rect baseRect = new(0f, 0f, 400f, 24f);

            Rect contentRect = WGroupHeaderVisualUtility.GetContentRect(baseRect, 0f, 0f);

            float horizontalPaddingPerSide = (baseRect.width - contentRect.width) / 2f;

            Assert.That(
                horizontalPaddingPerSide,
                Is.LessThanOrEqualTo(5f),
                "Horizontal padding per side should be minimal (<=5px) to prevent excessive indentation."
            );
        }

        [Test]
        public void InspectorContextFoldoutContentRectAdds9PixelOffset()
        {
            Rect baseRect = new(0f, 0f, 400f, 24f);

            Rect nonFoldoutRect = WGroupHeaderVisualUtility.GetContentRect(baseRect, 0f, 0f);
            Rect foldoutRect = WGroupHeaderVisualUtility.GetContentRect(baseRect, 0f, 0f, true);

            float additionalFoldoutOffset = foldoutRect.xMin - nonFoldoutRect.xMin;

            Assert.That(
                additionalFoldoutOffset,
                Is.EqualTo(ExpectedFoldoutOffsetInspector).Within(0.0001f),
                "In Inspector context, foldout content rect should add 9px offset "
                    + "to ensure the arrow is visually contained within the header background."
            );
        }

        [Test]
        public void SettingsContextFoldoutContentRectAddsZeroOffset()
        {
            WGroupHeaderVisualUtility.IsSettingsContext = true;
            Rect baseRect = new(0f, 0f, 400f, 24f);

            Rect nonFoldoutRect = WGroupHeaderVisualUtility.GetContentRect(baseRect, 0f, 0f);
            Rect foldoutRect = WGroupHeaderVisualUtility.GetContentRect(baseRect, 0f, 0f, true);

            float additionalFoldoutOffset = foldoutRect.xMin - nonFoldoutRect.xMin;

            Assert.That(
                additionalFoldoutOffset,
                Is.EqualTo(ExpectedFoldoutOffsetSettings).Within(0.0001f),
                "In Settings context, foldout content rect should add 0px offset "
                    + "to prevent excessive indentation."
            );
        }

        [Test]
        public void InspectorContextTotalFoldoutLeftPaddingIsCorrect()
        {
            Color textColor = new(0.5f, 0.5f, 0.5f, 1f);
            GUIStyle foldoutStyle = WGroupStyles.GetFoldoutStyle(textColor);
            Rect baseRect = new(0f, 0f, 400f, 24f);
            Rect foldoutRect = WGroupHeaderVisualUtility.GetContentRect(baseRect, 0f, 0f, true);

            float rectOffset = foldoutRect.xMin - baseRect.xMin;
            float stylePadding = foldoutStyle.padding.left;
            float totalLeftPadding = rectOffset + stylePadding;

            Assert.That(
                totalLeftPadding,
                Is.EqualTo(3f + 9f + 16f).Within(1f),
                $"In Inspector context, total left padding should be ~28px (3+9+16). "
                    + $"Rect offset: {rectOffset:F1}px, Style padding: {stylePadding}px."
            );
        }

        [Test]
        public void SettingsContextTotalFoldoutLeftPaddingIsCorrect()
        {
            WGroupHeaderVisualUtility.IsSettingsContext = true;
            Color textColor = new(0.5f, 0.5f, 0.5f, 1f);
            GUIStyle foldoutStyle = WGroupStyles.GetFoldoutStyle(textColor);
            Rect baseRect = new(0f, 0f, 400f, 24f);
            Rect foldoutRect = WGroupHeaderVisualUtility.GetContentRect(baseRect, 0f, 0f, true);

            float rectOffset = foldoutRect.xMin - baseRect.xMin;
            float stylePadding = foldoutStyle.padding.left;
            float totalLeftPadding = rectOffset + stylePadding;

            Assert.That(
                totalLeftPadding,
                Is.EqualTo(3f + 0f + 16f).Within(1f),
                $"In Settings context, total left padding should be ~19px (3+0+16). "
                    + $"Rect offset: {rectOffset:F1}px, Style padding: {stylePadding}px."
            );
        }

        [Test]
        public void TotalLabelLeftPaddingIsReasonable()
        {
            Color textColor = new(0.5f, 0.5f, 0.5f, 1f);
            GUIStyle labelStyle = WGroupStyles.GetHeaderLabelStyle(textColor);
            Rect baseRect = new(0f, 0f, 400f, 24f);
            Rect contentRect = WGroupHeaderVisualUtility.GetContentRect(baseRect, 0f, 0f);

            float rectOffset = contentRect.xMin - baseRect.xMin;
            float stylePadding = labelStyle.padding.left;
            float totalLeftPadding = rectOffset + stylePadding;

            Assert.That(
                totalLeftPadding,
                Is.LessThanOrEqualTo(10f),
                $"Total left padding for label text ({totalLeftPadding:F1}px) should be <= 10px. "
                    + $"Rect offset: {rectOffset:F1}px, Style padding: {stylePadding}px."
            );
        }

        [Test]
        public void InspectorContextFoldoutAndLabelPaddingDifferenceMatchesArrowSpace()
        {
            Color textColor = new(0.5f, 0.5f, 0.5f, 1f);
            GUIStyle foldoutStyle = WGroupStyles.GetFoldoutStyle(textColor);
            GUIStyle labelStyle = WGroupStyles.GetHeaderLabelStyle(textColor);
            Rect baseRect = new(0f, 0f, 400f, 24f);

            Rect foldoutRect = WGroupHeaderVisualUtility.GetContentRect(baseRect, 0f, 0f, true);
            Rect labelRect = WGroupHeaderVisualUtility.GetContentRect(baseRect, 0f, 0f);

            float foldoutRectOffset = foldoutRect.xMin - baseRect.xMin;
            float labelRectOffset = labelRect.xMin - baseRect.xMin;
            float rectOffsetDifference = foldoutRectOffset - labelRectOffset;

            float stylePaddingDifference = foldoutStyle.padding.left - labelStyle.padding.left;
            float totalDifference = rectOffsetDifference + stylePaddingDifference;

            Assert.That(
                totalDifference,
                Is.EqualTo(9f + 12f).Within(2f),
                $"In Inspector context, total padding difference should be ~21px (9px offset + 12px style). "
                    + $"Rect offset diff: {rectOffsetDifference:F1}px, Style padding diff: {stylePaddingDifference}px."
            );
        }

        [Test]
        public void SettingsContextFoldoutAndLabelPaddingDifferenceIsStyleOnly()
        {
            WGroupHeaderVisualUtility.IsSettingsContext = true;
            Color textColor = new(0.5f, 0.5f, 0.5f, 1f);
            GUIStyle foldoutStyle = WGroupStyles.GetFoldoutStyle(textColor);
            GUIStyle labelStyle = WGroupStyles.GetHeaderLabelStyle(textColor);
            Rect baseRect = new(0f, 0f, 400f, 24f);

            Rect foldoutRect = WGroupHeaderVisualUtility.GetContentRect(baseRect, 0f, 0f, true);
            Rect labelRect = WGroupHeaderVisualUtility.GetContentRect(baseRect, 0f, 0f);

            float foldoutRectOffset = foldoutRect.xMin - baseRect.xMin;
            float labelRectOffset = labelRect.xMin - baseRect.xMin;
            float rectOffsetDifference = foldoutRectOffset - labelRectOffset;

            float stylePaddingDifference = foldoutStyle.padding.left - labelStyle.padding.left;
            float totalDifference = rectOffsetDifference + stylePaddingDifference;

            Assert.That(
                rectOffsetDifference,
                Is.EqualTo(0f).Within(0.0001f),
                "In Settings context, rect offset difference should be 0px."
            );
            Assert.That(
                totalDifference,
                Is.EqualTo(stylePaddingDifference).Within(0.0001f),
                $"In Settings context, total difference should equal style padding difference only (~12px). "
                    + $"Actual: {totalDifference:F1}px."
            );
        }

        [Test]
        public void GroupContentPaddingCanBePushedAndPopped()
        {
            float originalPadding = GroupGUIWidthUtility.CurrentHorizontalPadding;
            float testPadding = 10f;

            using (GroupGUIWidthUtility.PushContentPadding(testPadding))
            {
                Assert.That(
                    GroupGUIWidthUtility.CurrentHorizontalPadding,
                    Is.EqualTo(originalPadding + testPadding).Within(0.0001f)
                );
            }

            Assert.That(
                GroupGUIWidthUtility.CurrentHorizontalPadding,
                Is.EqualTo(originalPadding).Within(0.0001f)
            );
        }

        [Test]
        public void NestedGroupPaddingAccumulatesCorrectly()
        {
            float originalPadding = GroupGUIWidthUtility.CurrentHorizontalPadding;
            float firstPadding = 8f;
            float secondPadding = 6f;

            using (GroupGUIWidthUtility.PushContentPadding(firstPadding))
            {
                Assert.That(
                    GroupGUIWidthUtility.CurrentHorizontalPadding,
                    Is.EqualTo(originalPadding + firstPadding).Within(0.0001f)
                );

                using (GroupGUIWidthUtility.PushContentPadding(secondPadding))
                {
                    Assert.That(
                        GroupGUIWidthUtility.CurrentHorizontalPadding,
                        Is.EqualTo(originalPadding + firstPadding + secondPadding).Within(0.0001f)
                    );
                }

                Assert.That(
                    GroupGUIWidthUtility.CurrentHorizontalPadding,
                    Is.EqualTo(originalPadding + firstPadding).Within(0.0001f)
                );
            }

            Assert.That(
                GroupGUIWidthUtility.CurrentHorizontalPadding,
                Is.EqualTo(originalPadding).Within(0.0001f)
            );
        }

        [Test]
        public void ScopeDepthTracksNestedGroups()
        {
            int originalDepth = GroupGUIWidthUtility.CurrentScopeDepth;

            using (GroupGUIWidthUtility.PushContentPadding(5f))
            {
                Assert.That(GroupGUIWidthUtility.CurrentScopeDepth, Is.EqualTo(originalDepth + 1));

                using (GroupGUIWidthUtility.PushContentPadding(5f))
                {
                    Assert.That(
                        GroupGUIWidthUtility.CurrentScopeDepth,
                        Is.EqualTo(originalDepth + 2)
                    );

                    using (GroupGUIWidthUtility.PushContentPadding(5f))
                    {
                        Assert.That(
                            GroupGUIWidthUtility.CurrentScopeDepth,
                            Is.EqualTo(originalDepth + 3)
                        );
                    }

                    Assert.That(
                        GroupGUIWidthUtility.CurrentScopeDepth,
                        Is.EqualTo(originalDepth + 2)
                    );
                }

                Assert.That(GroupGUIWidthUtility.CurrentScopeDepth, Is.EqualTo(originalDepth + 1));
            }

            Assert.That(GroupGUIWidthUtility.CurrentScopeDepth, Is.EqualTo(originalDepth));
        }

        [Test]
        public void LeftAndRightPaddingCanBeSpecifiedSeparately()
        {
            float leftPadding = 10f;
            float rightPadding = 5f;
            float totalPadding = leftPadding + rightPadding;

            using (GroupGUIWidthUtility.PushContentPadding(totalPadding, leftPadding, rightPadding))
            {
                Assert.That(
                    GroupGUIWidthUtility.CurrentLeftPadding,
                    Is.EqualTo(leftPadding).Within(0.0001f)
                );
                Assert.That(
                    GroupGUIWidthUtility.CurrentRightPadding,
                    Is.EqualTo(rightPadding).Within(0.0001f)
                );
            }
        }

        [Test]
        public void ApplyCurrentPaddingAdjustsRectCorrectly()
        {
            Rect originalRect = new(50f, 25f, 300f, 100f);
            float leftPadding = 12f;
            float rightPadding = 8f;

            using (
                GroupGUIWidthUtility.PushContentPadding(
                    leftPadding + rightPadding,
                    leftPadding,
                    rightPadding
                )
            )
            {
                Rect adjustedRect = GroupGUIWidthUtility.ApplyCurrentPadding(originalRect);

                Assert.That(
                    adjustedRect.xMin,
                    Is.EqualTo(originalRect.xMin + leftPadding).Within(0.0001f)
                );
                Assert.That(
                    adjustedRect.xMax,
                    Is.EqualTo(originalRect.xMax - rightPadding).Within(0.0001f)
                );
                Assert.That(
                    adjustedRect.width,
                    Is.EqualTo(originalRect.width - leftPadding - rightPadding).Within(0.0001f)
                );
            }
        }

        [Test]
        public void ApplyCurrentPaddingWithNoPaddingReturnsOriginalRect()
        {
            Rect originalRect = new(10f, 20f, 200f, 50f);

            Rect adjustedRect = GroupGUIWidthUtility.ApplyCurrentPadding(originalRect);

            Assert.That(adjustedRect, Is.EqualTo(originalRect));
        }

        [Test]
        public void IndentCompensationReducesIndentByOne()
        {
            EditorGUI.indentLevel = 3;
            int observedIndent = -1;

            GroupGUIIndentUtility.ExecuteWithIndentCompensation(() =>
            {
                observedIndent = EditorGUI.indentLevel;
            });

            Assert.That(observedIndent, Is.EqualTo(2));
            Assert.That(
                EditorGUI.indentLevel,
                Is.EqualTo(3),
                "Indent should be restored after action."
            );
        }

        [Test]
        public void IndentCompensationDoesNotGoNegative()
        {
            EditorGUI.indentLevel = 0;
            int observedIndent = -1;

            GroupGUIIndentUtility.ExecuteWithIndentCompensation(() =>
            {
                observedIndent = EditorGUI.indentLevel;
            });

            Assert.That(observedIndent, Is.EqualTo(0), "Indent should not go below zero.");
            Assert.That(EditorGUI.indentLevel, Is.EqualTo(0));
        }

        [Test]
        public void IndentCompensationRestoresIndentOnException()
        {
            EditorGUI.indentLevel = 5;

            try
            {
                GroupGUIIndentUtility.ExecuteWithIndentCompensation(() =>
                {
                    Assert.That(EditorGUI.indentLevel, Is.EqualTo(4));
                    throw new InvalidOperationException("Test exception");
                });
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            Assert.That(
                EditorGUI.indentLevel,
                Is.EqualTo(5),
                "Indent should be restored even after exception."
            );
        }

        [Test]
        public void CalculateHorizontalPaddingFromHelpBoxStyle()
        {
            GUIStyle helpBox = EditorStyles.helpBox;

            float totalPadding = GroupGUIWidthUtility.CalculateHorizontalPadding(
                helpBox,
                out float leftPadding,
                out float rightPadding
            );

            Assert.That(leftPadding, Is.GreaterThanOrEqualTo(0f));
            Assert.That(rightPadding, Is.GreaterThanOrEqualTo(0f));
            Assert.That(totalPadding, Is.EqualTo(leftPadding + rightPadding).Within(0.0001f));
        }

        [Test]
        public void CalculateHorizontalPaddingFromNullStyleReturnsZero()
        {
            float totalPadding = GroupGUIWidthUtility.CalculateHorizontalPadding(
                null,
                out float leftPadding,
                out float rightPadding
            );

            Assert.That(totalPadding, Is.EqualTo(0f));
            Assert.That(leftPadding, Is.EqualTo(0f));
            Assert.That(rightPadding, Is.EqualTo(0f));
        }

        [Test]
        public void ZeroPaddingScopeDoesNotAffectTotals()
        {
            float originalPadding = GroupGUIWidthUtility.CurrentHorizontalPadding;
            int originalDepth = GroupGUIWidthUtility.CurrentScopeDepth;

            using (GroupGUIWidthUtility.PushContentPadding(0f))
            {
                Assert.That(
                    GroupGUIWidthUtility.CurrentHorizontalPadding,
                    Is.EqualTo(originalPadding).Within(0.0001f)
                );
                Assert.That(
                    GroupGUIWidthUtility.CurrentScopeDepth,
                    Is.EqualTo(originalDepth + 1),
                    "Scope depth should still increment even with zero padding."
                );
            }
        }

        [Test]
        public void HeaderVerticalPaddingConstantsAreReasonable()
        {
            Assert.That(
                WGroupStyles.HeaderTopPadding,
                Is.InRange(0f, 5f),
                "Header top padding should be minimal."
            );
            Assert.That(
                WGroupStyles.HeaderBottomPadding,
                Is.InRange(0f, 5f),
                "Header bottom padding should be minimal."
            );
            Assert.That(
                WGroupStyles.HeaderVerticalPadding,
                Is.InRange(2f, 8f),
                "Header vertical padding should provide reasonable spacing."
            );
        }

        [Test]
        public void MultipleIndentCompensationCallsWorkCorrectly()
        {
            EditorGUI.indentLevel = 5;
            int firstObserved = -1;
            int secondObserved = -1;

            GroupGUIIndentUtility.ExecuteWithIndentCompensation(() =>
            {
                firstObserved = EditorGUI.indentLevel;
                GroupGUIIndentUtility.ExecuteWithIndentCompensation(() =>
                {
                    secondObserved = EditorGUI.indentLevel;
                });
                Assert.That(
                    EditorGUI.indentLevel,
                    Is.EqualTo(4),
                    "Outer indent should be restored after inner call."
                );
            });

            Assert.That(firstObserved, Is.EqualTo(4), "First call should reduce indent by 1.");
            Assert.That(
                secondObserved,
                Is.EqualTo(3),
                "Second call should reduce indent by 1 again."
            );
            Assert.That(
                EditorGUI.indentLevel,
                Is.EqualTo(5),
                "Original indent should be restored."
            );
        }
    }
#endif
}
