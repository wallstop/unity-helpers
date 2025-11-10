#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.WButton
{
    using System.Collections.Generic;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.WButton;

    public sealed class WButtonGuiHeaderTests
    {
        private static readonly FieldInfo GroupCountsField = typeof(WButtonGUI).GetField(
            "GroupCounts",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        private static readonly MethodInfo BuildGroupHeaderMethod = typeof(WButtonGUI).GetMethod(
            "BuildGroupHeader",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        [Test]
        public void BuildGroupHeader_NoGroupingSuffixWhenSingleGroup()
        {
            Dictionary<int, int> groupCounts = GetGroupCounts();
            groupCounts.Clear();

            GUIContent header = InvokeBuildGroupHeader(-1);
            Assert.That(header.text, Is.EqualTo(WButtonStyles.TopGroupLabel.text));
        }

        [Test]
        public void BuildGroupHeader_AppendsDrawOrderWhenMultipleGroups()
        {
            Dictionary<int, int> groupCounts = GetGroupCounts();
            groupCounts.Clear();
            groupCounts[-1] = 3;
            groupCounts[-5] = 2;

            GUIContent topHeader = InvokeBuildGroupHeader(-1);
            GUIContent bottomHeader = InvokeBuildGroupHeader(-5);

            Assert.That(topHeader.text, Is.EqualTo($"{WButtonStyles.TopGroupLabel.text} (-1)"));
            Assert.That(
                bottomHeader.text,
                Is.EqualTo($"{WButtonStyles.BottomGroupLabel.text} (-5)")
            );
        }

        private static Dictionary<int, int> GetGroupCounts()
        {
            return (Dictionary<int, int>)GroupCountsField.GetValue(null);
        }

        private static GUIContent InvokeBuildGroupHeader(int drawOrder)
        {
            return (GUIContent)BuildGroupHeaderMethod.Invoke(null, new object[] { drawOrder });
        }
    }
}
#endif
