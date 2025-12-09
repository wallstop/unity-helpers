#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.WButton
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Utils.WButton;

    public sealed class WButtonGuiHeaderTests
    {
        [SetUp]
        public void SetUp()
        {
            WButtonGUI.ClearGroupDataForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            WButtonGUI.ClearGroupDataForTesting();
        }

        [Test]
        public void BuildGroupHeaderNoGroupingSuffixWhenSingleGroup()
        {
            WButtonGUI.ClearGroupDataForTesting();

            // Draw order >= -1 is top placement
            GUIContent header = WButtonGUI.BuildGroupHeader(-1);
            Assert.That(header.text, Is.EqualTo(WButtonStyles.TopGroupLabel.text));
        }

        [Test]
        public void BuildGroupHeaderAppendsDrawOrderWhenMultipleGroups()
        {
            Dictionary<int, int> counts = new() { { -1, 3 }, { -5, 2 } };
            WButtonGUI.SetGroupCountsForTesting(counts);

            GUIContent topHeader = WButtonGUI.BuildGroupHeader(-1);
            GUIContent bottomHeader = WButtonGUI.BuildGroupHeader(-5);

            Assert.That(topHeader.text, Is.EqualTo($"{WButtonStyles.TopGroupLabel.text} (-1)"));
            Assert.That(
                bottomHeader.text,
                Is.EqualTo($"{WButtonStyles.BottomGroupLabel.text} (-5)")
            );
        }

        [Test]
        public void BuildGroupHeaderUsesCustomGroupNameWhenProvided()
        {
            Dictionary<int, int> counts = new() { { -1, 2 }, { -4, 1 } };
            WButtonGUI.SetGroupCountsForTesting(counts);

            Dictionary<int, string> names = new() { { -4, "Networking" } };
            WButtonGUI.SetGroupNamesForTesting(names);

            GUIContent custom = WButtonGUI.BuildGroupHeader(-4);
            GUIContent defaultHeader = WButtonGUI.BuildGroupHeader(-1);

            Assert.That(custom.text, Is.EqualTo("Networking"));
            Assert.That(defaultHeader.text, Is.EqualTo($"{WButtonStyles.TopGroupLabel.text} (-1)"));
        }
    }
}
#endif
