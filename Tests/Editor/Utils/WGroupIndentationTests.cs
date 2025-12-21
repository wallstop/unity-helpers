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
                    Is.EqualTo(originalPadding).Within(0.0001f),
                    "Zero padding should not change horizontal padding total."
                );
                Assert.That(
                    GroupGUIWidthUtility.CurrentScopeDepth,
                    Is.EqualTo(originalDepth),
                    "Zero padding should not increment scope depth since there is no visual WGroup context."
                );
            }
        }

        [TestCase(0f, 0f, 0f, 0, Description = "All zeros - no scope depth increment")]
        [TestCase(0f, 0f, 0.001f, 1, Description = "Tiny right padding - increments scope depth")]
        [TestCase(0f, 0.001f, 0f, 1, Description = "Tiny left padding - increments scope depth")]
        [TestCase(
            0.001f,
            0f,
            0f,
            1,
            Description = "Tiny horizontal padding - increments scope depth"
        )]
        [TestCase(1f, 0f, 0f, 1, Description = "1px horizontal padding - increments scope depth")]
        [TestCase(0f, 1f, 1f, 1, Description = "1px left and right - increments scope depth")]
        [TestCase(10f, 5f, 5f, 1, Description = "Typical padding values - increments scope depth")]
        public void PaddingScopeDepthBehaviorWithVariousValues(
            float horizontalPadding,
            float leftPadding,
            float rightPadding,
            int expectedDepthIncrement
        )
        {
            GroupGUIWidthUtility.ResetForTests();
            int originalDepth = GroupGUIWidthUtility.CurrentScopeDepth;

            using (
                GroupGUIWidthUtility.PushContentPadding(
                    horizontalPadding,
                    leftPadding,
                    rightPadding
                )
            )
            {
                int actualDepth = GroupGUIWidthUtility.CurrentScopeDepth;
                Assert.That(
                    actualDepth,
                    Is.EqualTo(originalDepth + expectedDepthIncrement),
                    $"Scope depth should be {originalDepth + expectedDepthIncrement} with padding "
                        + $"(h={horizontalPadding}, l={leftPadding}, r={rightPadding}). Actual: {actualDepth}"
                );
            }

            // Verify scope is properly restored after disposal
            Assert.That(
                GroupGUIWidthUtility.CurrentScopeDepth,
                Is.EqualTo(originalDepth),
                "Scope depth should be restored after disposing padding scope."
            );
        }

        [Test]
        public void NestedZeroPaddingScopesDoNotIncrementDepth()
        {
            GroupGUIWidthUtility.ResetForTests();
            int originalDepth = GroupGUIWidthUtility.CurrentScopeDepth;

            using (GroupGUIWidthUtility.PushContentPadding(0f, 0f, 0f))
            {
                Assert.That(
                    GroupGUIWidthUtility.CurrentScopeDepth,
                    Is.EqualTo(originalDepth),
                    "First zero padding scope should not increment depth."
                );

                using (GroupGUIWidthUtility.PushContentPadding(0f, 0f, 0f))
                {
                    Assert.That(
                        GroupGUIWidthUtility.CurrentScopeDepth,
                        Is.EqualTo(originalDepth),
                        "Nested zero padding scope should not increment depth."
                    );
                }

                Assert.That(
                    GroupGUIWidthUtility.CurrentScopeDepth,
                    Is.EqualTo(originalDepth),
                    "Depth should remain unchanged after nested scope disposal."
                );
            }

            Assert.That(
                GroupGUIWidthUtility.CurrentScopeDepth,
                Is.EqualTo(originalDepth),
                "Depth should be restored after all scopes disposed."
            );
        }

        [Test]
        public void MixedZeroAndNonZeroPaddingScopesTrackCorrectly()
        {
            GroupGUIWidthUtility.ResetForTests();
            int originalDepth = GroupGUIWidthUtility.CurrentScopeDepth;

            using (GroupGUIWidthUtility.PushContentPadding(10f, 5f, 5f))
            {
                Assert.That(
                    GroupGUIWidthUtility.CurrentScopeDepth,
                    Is.EqualTo(originalDepth + 1),
                    "Non-zero padding should increment depth."
                );

                using (GroupGUIWidthUtility.PushContentPadding(0f, 0f, 0f))
                {
                    Assert.That(
                        GroupGUIWidthUtility.CurrentScopeDepth,
                        Is.EqualTo(originalDepth + 1),
                        "Zero padding nested inside non-zero should not change depth."
                    );

                    using (GroupGUIWidthUtility.PushContentPadding(5f, 2f, 3f))
                    {
                        Assert.That(
                            GroupGUIWidthUtility.CurrentScopeDepth,
                            Is.EqualTo(originalDepth + 2),
                            "Another non-zero padding should increment depth."
                        );
                    }

                    Assert.That(
                        GroupGUIWidthUtility.CurrentScopeDepth,
                        Is.EqualTo(originalDepth + 1),
                        "Depth should decrement after innermost non-zero scope disposal."
                    );
                }

                Assert.That(
                    GroupGUIWidthUtility.CurrentScopeDepth,
                    Is.EqualTo(originalDepth + 1),
                    "Depth should remain after zero scope disposal."
                );
            }

            Assert.That(
                GroupGUIWidthUtility.CurrentScopeDepth,
                Is.EqualTo(originalDepth),
                "Depth should be restored after all scopes disposed."
            );
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

        [TestCase(10f, 5f, 0f)]
        [TestCase(24f, 10f, 0f)]
        [TestCase(4f, 4f, 0f)]
        [TestCase(16f, 8f, 100f)]
        [TestCase(24f, 10f, 50f)]
        public void ApplyCurrentPaddingAdjustsRectConsistentlyWithVariousPaddingsAndOffsets(
            float leftPadding,
            float rightPadding,
            float rectX
        )
        {
            Rect originalRect = new(rectX, 0f, 300f, 100f);
            float totalPadding = leftPadding + rightPadding;

            using (GroupGUIWidthUtility.PushContentPadding(totalPadding, leftPadding, rightPadding))
            {
                Rect adjustedRect = GroupGUIWidthUtility.ApplyCurrentPadding(originalRect);

                Assert.That(
                    adjustedRect.xMin,
                    Is.EqualTo(originalRect.xMin + leftPadding).Within(0.0001f),
                    $"Adjusted xMin should be original xMin ({originalRect.xMin}) + left padding ({leftPadding})"
                );
                Assert.That(
                    adjustedRect.xMax,
                    Is.EqualTo(originalRect.xMax - rightPadding).Within(0.0001f),
                    $"Adjusted xMax should be original xMax ({originalRect.xMax}) - right padding ({rightPadding})"
                );
                Assert.That(
                    adjustedRect.width,
                    Is.EqualTo(originalRect.width - totalPadding).Within(0.0001f),
                    $"Adjusted width should be original width ({originalRect.width}) - total padding ({totalPadding})"
                );
            }
        }

        [TestCase(10f, 5f)]
        [TestCase(24f, 10f)]
        [TestCase(4f, 4f)]
        [TestCase(16f, 8f)]
        public void PaddingShouldNotBeAppliedMultipleTimesToSameRect(
            float leftPadding,
            float rightPadding
        )
        {
            Rect originalRect = new(0f, 0f, 300f, 100f);
            float totalPadding = leftPadding + rightPadding;

            using (GroupGUIWidthUtility.PushContentPadding(totalPadding, leftPadding, rightPadding))
            {
                Rect firstAdjustment = GroupGUIWidthUtility.ApplyCurrentPadding(originalRect);
                Rect secondAdjustment = GroupGUIWidthUtility.ApplyCurrentPadding(firstAdjustment);

                float totalLeftShift = secondAdjustment.xMin - originalRect.xMin;
                float totalWidthReduction = originalRect.width - secondAdjustment.width;

                // This test documents the current behavior where double-application
                // results in double the shift. Tests that pass pre-adjusted rects
                // to drawers that also apply padding will see this issue.
                Assert.That(
                    totalLeftShift,
                    Is.EqualTo(leftPadding * 2f).Within(0.0001f),
                    $"Double-applying padding results in 2x left shift. "
                        + $"Single application = {leftPadding}, Double = {totalLeftShift}. "
                        + "Tests must NOT pass pre-adjusted rects to drawers that apply padding internally."
                );
                Assert.That(
                    totalWidthReduction,
                    Is.EqualTo(totalPadding * 2f).Within(0.0001f),
                    $"Double-applying padding results in 2x width reduction. "
                        + $"Single application = {totalPadding}, Double = {totalWidthReduction}."
                );
            }
        }

        [Test]
        public void NestedPaddingScopesAccumulateCorrectly()
        {
            Rect originalRect = new(0f, 0f, 400f, 100f);
            float outerLeft = 10f;
            float outerRight = 5f;
            float innerLeft = 8f;
            float innerRight = 4f;

            using (
                GroupGUIWidthUtility.PushContentPadding(
                    outerLeft + outerRight,
                    outerLeft,
                    outerRight
                )
            )
            {
                float afterOuterLeft = GroupGUIWidthUtility.CurrentLeftPadding;
                float afterOuterRight = GroupGUIWidthUtility.CurrentRightPadding;

                using (
                    GroupGUIWidthUtility.PushContentPadding(
                        innerLeft + innerRight,
                        innerLeft,
                        innerRight
                    )
                )
                {
                    float totalLeft = GroupGUIWidthUtility.CurrentLeftPadding;
                    float totalRight = GroupGUIWidthUtility.CurrentRightPadding;

                    Assert.That(
                        totalLeft,
                        Is.EqualTo(outerLeft + innerLeft).Within(0.0001f),
                        "Nested padding scopes should accumulate left padding"
                    );
                    Assert.That(
                        totalRight,
                        Is.EqualTo(outerRight + innerRight).Within(0.0001f),
                        "Nested padding scopes should accumulate right padding"
                    );

                    Rect adjustedRect = GroupGUIWidthUtility.ApplyCurrentPadding(originalRect);
                    Assert.That(
                        adjustedRect.xMin,
                        Is.EqualTo(originalRect.xMin + totalLeft).Within(0.0001f),
                        "ApplyCurrentPadding should use accumulated left padding"
                    );
                }

                Assert.That(
                    GroupGUIWidthUtility.CurrentLeftPadding,
                    Is.EqualTo(afterOuterLeft).Within(0.0001f),
                    "After inner scope disposed, left padding should return to outer value"
                );
                Assert.That(
                    GroupGUIWidthUtility.CurrentRightPadding,
                    Is.EqualTo(afterOuterRight).Within(0.0001f),
                    "After inner scope disposed, right padding should return to outer value"
                );
            }

            Assert.That(
                GroupGUIWidthUtility.CurrentLeftPadding,
                Is.EqualTo(0f).Within(0.0001f),
                "After all scopes disposed, left padding should be zero"
            );
            Assert.That(
                GroupGUIWidthUtility.CurrentRightPadding,
                Is.EqualTo(0f).Within(0.0001f),
                "After all scopes disposed, right padding should be zero"
            );
        }
    }
#endif
}
