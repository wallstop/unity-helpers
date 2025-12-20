namespace WallstopStudios.UnityHelpers.Tests.Settings
{
#if UNITY_EDITOR
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using WallstopStudios.UnityHelpers.Editor.Utils.WGroup;

    [TestFixture]
    public sealed class WGroupSettingsIndentationTests
    {
        private int _originalIndentLevel;

        [SetUp]
        public void SetUp()
        {
            _originalIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            GroupGUIWidthUtility.ResetForTests();
            WGroupHeaderVisualUtility.IsSettingsContext = true;
        }

        [TearDown]
        public void TearDown()
        {
            EditorGUI.indentLevel = _originalIndentLevel;
            GroupGUIWidthUtility.ResetForTests();
            WGroupHeaderVisualUtility.IsSettingsContext = false;
        }

        [Test]
        public void SettingsContextHasZeroBaseIndent()
        {
            Assert.That(
                EditorGUI.indentLevel,
                Is.EqualTo(0),
                "Settings context should start with zero indent level."
            );
        }

        [Test]
        public void SettingsContextFlagIsSetInSetUp()
        {
            Assert.That(
                WGroupHeaderVisualUtility.IsSettingsContext,
                Is.True,
                "IsSettingsContext should be true in these tests."
            );
        }

        [Test]
        public void WGroupHeaderContentRectHasMinimalOffset()
        {
            Rect headerRect = new(0f, 0f, 400f, 24f);

            Rect contentRect = WGroupHeaderVisualUtility.GetContentRect(headerRect, 0f, 0f, true);

            float xOffset = contentRect.xMin - headerRect.xMin;

            Assert.That(
                xOffset,
                Is.EqualTo(3f).Within(0.0001f),
                $"In Settings context, header content rect xMin offset ({xOffset:F1}px) should be 3px "
                    + "(horizontal padding only, no foldout offset)."
            );
        }

        [Test]
        public void FoldoutContentRectHasZeroAdditionalOffset()
        {
            Rect headerRect = new(0f, 0f, 400f, 24f);

            Rect nonFoldoutRect = WGroupHeaderVisualUtility.GetContentRect(headerRect, 0f, 0f);
            Rect foldoutRect = WGroupHeaderVisualUtility.GetContentRect(headerRect, 0f, 0f, true);

            float additionalOffset = foldoutRect.xMin - nonFoldoutRect.xMin;

            Assert.That(
                additionalOffset,
                Is.EqualTo(0f).Within(0.0001f),
                $"In Settings context, foldout rect should have 0px additional offset ({additionalOffset:F1}px) "
                    + "to prevent excessive indentation."
            );
        }

        [Test]
        public void HelpBoxPaddingIsReasonableForSettings()
        {
            GUIStyle helpBox = EditorStyles.helpBox;

            float padding = GroupGUIWidthUtility.CalculateHorizontalPadding(
                helpBox,
                out float leftPadding,
                out float rightPadding
            );

            Assert.That(
                leftPadding,
                Is.LessThanOrEqualTo(10f),
                $"HelpBox left padding ({leftPadding:F1}px) should be reasonable."
            );
            Assert.That(
                rightPadding,
                Is.LessThanOrEqualTo(10f),
                $"HelpBox right padding ({rightPadding:F1}px) should be reasonable."
            );
        }

        [Test]
        public void TotalWGroupLeftOffsetIsAcceptable()
        {
            Rect headerRect = new(0f, 0f, 400f, 24f);
            Color textColor = new(0.5f, 0.5f, 0.5f, 1f);
            GUIStyle foldoutStyle = WGroupStyles.GetFoldoutStyle(textColor);

            Rect foldoutRect = WGroupHeaderVisualUtility.GetContentRect(headerRect, 0f, 0f, true);

            float helpBoxPadding = GroupGUIWidthUtility.CalculateHorizontalPadding(
                EditorStyles.helpBox,
                out float helpBoxLeft,
                out _
            );
            float contentRectOffset = foldoutRect.xMin - headerRect.xMin;
            float stylePadding = foldoutStyle.padding.left;
            float totalOffset = helpBoxLeft + contentRectOffset + stylePadding;

            Assert.That(
                totalOffset,
                Is.LessThanOrEqualTo(30f),
                $"Total left offset ({totalOffset:F1}px) should be <= 30px. "
                    + $"HelpBox: {helpBoxLeft:F1}px, ContentRect: {contentRectOffset:F1}px, Style: {stylePadding}px."
            );
        }

        [Test]
        public void WGroupIndentLevelIncrementIsOneInsideGroup()
        {
            int baseIndent = EditorGUI.indentLevel;

            EditorGUI.indentLevel++;
            int groupIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel--;

            Assert.That(
                groupIndent,
                Is.EqualTo(baseIndent + 1),
                "WGroup content should increase indent by exactly 1."
            );
        }

        [Test]
        public void IndentCompensationReducesToBaseLevel()
        {
            EditorGUI.indentLevel = 1;
            int observedIndent = -1;

            GroupGUIIndentUtility.ExecuteWithIndentCompensation(() =>
            {
                observedIndent = EditorGUI.indentLevel;
            });

            Assert.That(
                observedIndent,
                Is.EqualTo(0),
                "Indent compensation should reduce indent back to base level (0 in Settings)."
            );
        }

        [Test]
        public void NestedWGroupsMaintainCorrectIndentation()
        {
            int[] observedIndents = new int[3];

            EditorGUI.indentLevel++;
            observedIndents[0] = EditorGUI.indentLevel;

            EditorGUI.indentLevel++;
            observedIndents[1] = EditorGUI.indentLevel;

            EditorGUI.indentLevel++;
            observedIndents[2] = EditorGUI.indentLevel;

            EditorGUI.indentLevel -= 3;

            Assert.That(observedIndents[0], Is.EqualTo(1), "First nested group indent.");
            Assert.That(observedIndents[1], Is.EqualTo(2), "Second nested group indent.");
            Assert.That(observedIndents[2], Is.EqualTo(3), "Third nested group indent.");
            Assert.That(
                EditorGUI.indentLevel,
                Is.EqualTo(0),
                "Should return to base indent after exiting all groups."
            );
        }

        [Test]
        public void SettingsWGroupPaddingDoesNotAccumulateExcessively()
        {
            float padding = 8f;
            float initialPadding = GroupGUIWidthUtility.CurrentHorizontalPadding;

            using (GroupGUIWidthUtility.PushContentPadding(padding))
            {
                Assert.That(
                    GroupGUIWidthUtility.CurrentHorizontalPadding,
                    Is.EqualTo(initialPadding + padding).Within(0.0001f)
                );

                using (GroupGUIWidthUtility.PushContentPadding(padding))
                {
                    Assert.That(
                        GroupGUIWidthUtility.CurrentHorizontalPadding,
                        Is.EqualTo(initialPadding + padding * 2).Within(0.0001f)
                    );
                }
            }

            Assert.That(
                GroupGUIWidthUtility.CurrentHorizontalPadding,
                Is.EqualTo(initialPadding).Within(0.0001f)
            );
        }

        [Test]
        public void HeaderRectWidthIsPreservedAfterContentRectTransform()
        {
            Rect headerRect = new(50f, 25f, 300f, 24f);

            Rect contentRect = WGroupHeaderVisualUtility.GetContentRect(headerRect, 1f, 1f, true);

            float widthReduction = headerRect.width - contentRect.width;

            Assert.That(
                widthReduction,
                Is.LessThanOrEqualTo(10f),
                $"Width reduction ({widthReduction:F1}px) should be <= 10px (minimal horizontal padding)."
            );
        }

        [Test]
        public void FoldoutRectMaintainsClickableWidth()
        {
            Rect headerRect = new(0f, 0f, 200f, 24f);

            Rect foldoutRect = WGroupHeaderVisualUtility.GetContentRect(headerRect, 1f, 1f, true);

            Assert.That(
                foldoutRect.width,
                Is.GreaterThanOrEqualTo(180f),
                $"Foldout rect width ({foldoutRect.width:F1}px) should be >= 180px for adequate click area."
            );
        }

        [Test]
        public void FoldoutContentRectHasSameWidthAsLabel()
        {
            Rect headerRect = new(0f, 0f, 400f, 24f);

            Rect foldoutRect = WGroupHeaderVisualUtility.GetContentRect(headerRect, 1f, 1f, true);
            Rect labelRect = WGroupHeaderVisualUtility.GetContentRect(headerRect, 1f, 1f);

            float widthDifference = labelRect.width - foldoutRect.width;

            Assert.That(
                widthDifference,
                Is.EqualTo(0f).Within(0.0001f),
                $"In Settings context, foldout rect should have same width as label rect (diff: {widthDifference:F1}px) "
                    + "since there's no additional foldout offset."
            );
        }

        [Test]
        public void HeaderStylesProduceConsistentHeightsAcrossColors()
        {
            Color color1 = new(0.2f, 0.4f, 0.6f, 1f);
            Color color2 = new(0.8f, 0.6f, 0.4f, 1f);

            float height1 = WGroupStyles.GetHeaderHeight(color1);
            float height2 = WGroupStyles.GetHeaderHeight(color2);

            Assert.That(
                height1,
                Is.EqualTo(height2).Within(0.0001f),
                "Header heights should be consistent regardless of text color."
            );
        }

        [Test]
        public void WideLabelWidthDoesNotAffectHeaderIndentation()
        {
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            Rect headerRect = new(0f, 0f, 400f, 24f);

            try
            {
                EditorGUIUtility.labelWidth = 260f;

                Rect contentRect = WGroupHeaderVisualUtility.GetContentRect(
                    headerRect,
                    0f,
                    0f,
                    true
                );
                float xOffset = contentRect.xMin - headerRect.xMin;

                Assert.That(
                    xOffset,
                    Is.LessThanOrEqualTo(5f),
                    $"Header content rect offset ({xOffset:F1}px) should not be affected by label width setting."
                );
            }
            finally
            {
                EditorGUIUtility.labelWidth = originalLabelWidth;
            }
        }
    }
#endif
}
