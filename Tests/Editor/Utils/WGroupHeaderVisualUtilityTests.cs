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
        [Test]
        public void GetContentRectShiftsWhenFoldoutSpaceRequested()
        {
            Rect headerRect = new(0f, 0f, 200f, 24f);

            Rect withoutFoldout = WGroupHeaderVisualUtility.GetContentRect(headerRect, 0f, 0f);

            Rect withFoldout = WGroupHeaderVisualUtility.GetContentRect(headerRect, 0f, 0f, true);

            Assert.That(withFoldout.xMin, Is.GreaterThan(withoutFoldout.xMin));
            Assert.That(withFoldout.xMax, Is.EqualTo(withoutFoldout.xMax).Within(0.0001f));
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
