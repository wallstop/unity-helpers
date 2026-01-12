// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Utils
{
#if UNITY_EDITOR
    using NUnit.Framework;
    using UnityEditor;
    using WallstopStudios.UnityHelpers.Editor.Utils;

    [TestFixture]
    [NUnit.Framework.Category("Fast")]
    public sealed class GroupGUIIndentUtilityTests
    {
        [Test]
        public void ExecuteWithIndentCompensationReducesIndentByOne()
        {
            int original = 4;
            int observed = -1;
            int restored = -1;

            int previousIndent = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = original;
                GroupGUIIndentUtility.ExecuteWithIndentCompensation(() =>
                {
                    observed = EditorGUI.indentLevel;
                });
                restored = EditorGUI.indentLevel;
            }
            finally
            {
                EditorGUI.indentLevel = previousIndent;
            }

            Assert.AreEqual(
                original - 1,
                observed,
                "Indent level should be reduced inside action."
            );
            Assert.AreEqual(original, restored, "Indent level should be restored.");
        }

        [Test]
        public void ExecuteWithIndentCompensationPreventsNegativeIndent()
        {
            int previousIndent = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;
                int observed = -1;
                GroupGUIIndentUtility.ExecuteWithIndentCompensation(() =>
                    observed = EditorGUI.indentLevel
                );
                Assert.AreEqual(0, observed, "Indent should not go below zero.");
            }
            finally
            {
                EditorGUI.indentLevel = previousIndent;
            }
        }
    }
#endif
}
