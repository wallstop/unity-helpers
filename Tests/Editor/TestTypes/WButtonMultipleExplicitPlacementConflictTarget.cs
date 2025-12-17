namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target where buttons have multiple different explicit placements.
    /// This SHOULD generate a placement conflict warning.
    /// </summary>
    public sealed class WButtonMultipleExplicitPlacementConflictTarget : ScriptableObject
    {
        [WButton("Button A", groupName: "ConflictGroup", groupPlacement: WButtonGroupPlacement.Top)]
        public void ButtonA() { }

        [WButton(
            "Button B",
            groupName: "ConflictGroup",
            groupPlacement: WButtonGroupPlacement.Bottom
        )]
        public void ButtonB() { }

        [WButton("Button C", groupName: "ConflictGroup")]
        public void ButtonC() { }
    }
}
