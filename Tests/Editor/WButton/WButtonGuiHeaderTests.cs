#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.WButton
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Utils.WButton;

    public sealed class WButtonGuiHeaderTests
    {
        [Test]
        public void BuildGroupHeader_NoGroupingSuffixWhenSingleGroup()
        {
            Dictionary<int, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();
            groupCounts.Clear();
            Dictionary<int, string> groupNames = WButtonGUI.GetGroupNamesForTesting();
            groupNames.Clear();

            GUIContent header = WButtonGUI.BuildGroupHeader(-1);
            Assert.That(header.text, Is.EqualTo(WButtonStyles.TopGroupLabel.text));
        }

        [Test]
        public void BuildGroupHeader_AppendsDrawOrderWhenMultipleGroups()
        {
            Dictionary<int, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();
            groupCounts.Clear();
            groupCounts[-1] = 3;
            groupCounts[-5] = 2;
            Dictionary<int, string> groupNames = WButtonGUI.GetGroupNamesForTesting();
            groupNames.Clear();

            GUIContent topHeader = WButtonGUI.BuildGroupHeader(-1);
            GUIContent bottomHeader = WButtonGUI.BuildGroupHeader(-5);

            Assert.That(topHeader.text, Is.EqualTo($"{WButtonStyles.TopGroupLabel.text} (-1)"));
            Assert.That(
                bottomHeader.text,
                Is.EqualTo($"{WButtonStyles.BottomGroupLabel.text} (-5)")
            );
        }

        [Test]
        public void BuildGroupHeader_UsesCustomGroupNameWhenProvided()
        {
            Dictionary<int, int> groupCounts = WButtonGUI.GetGroupCountsForTesting();
            groupCounts.Clear();
            groupCounts[-1] = 2;
            groupCounts[-4] = 1;
            Dictionary<int, string> groupNames = WButtonGUI.GetGroupNamesForTesting();
            groupNames.Clear();
            groupNames[-4] = "Networking";

            GUIContent custom = WButtonGUI.BuildGroupHeader(-4);
            GUIContent defaultHeader = WButtonGUI.BuildGroupHeader(-1);

            Assert.That(custom.text, Is.EqualTo("Networking"));
            Assert.That(defaultHeader.text, Is.EqualTo($"{WButtonStyles.TopGroupLabel.text} (-1)"));
        }
    }
}
#endif
