// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target where all buttons in a group use the same explicit placement.
    /// This should NOT generate any placement conflict warnings.
    /// </summary>
    public sealed class WButtonAllSameExplicitPlacementTarget : ScriptableObject
    {
        [WButton("Button A", groupName: "TopGroup", groupPlacement: WButtonGroupPlacement.Top)]
        public void ButtonA() { }

        [WButton("Button B", groupName: "TopGroup", groupPlacement: WButtonGroupPlacement.Top)]
        public void ButtonB() { }

        [WButton("Button C", groupName: "TopGroup", groupPlacement: WButtonGroupPlacement.Top)]
        public void ButtonC() { }
    }
}
