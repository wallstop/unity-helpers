namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with mixed placements across multiple groups. Expected: Each group renders in its designated placement.
    /// </summary>
    public sealed class WButtonMixedPlacementGroupsTarget : ScriptableObject
    {
        [WButton(
            "Top Group Button 1",
            groupName: "TopGroup",
            groupPlacement: WButtonGroupPlacement.Top
        )]
        public void TopGroupButton1() { }

        [WButton(
            "Top Group Button 2",
            groupName: "TopGroup",
            groupPlacement: WButtonGroupPlacement.Top
        )]
        public void TopGroupButton2() { }

        [WButton(
            "Bottom Group Button 1",
            groupName: "BottomGroup",
            groupPlacement: WButtonGroupPlacement.Bottom
        )]
        public void BottomGroupButton1() { }

        [WButton(
            "Bottom Group Button 2",
            groupName: "BottomGroup",
            groupPlacement: WButtonGroupPlacement.Bottom
        )]
        public void BottomGroupButton2() { }

        [WButton("Default Group Button", groupName: "DefaultGroup")]
        public void DefaultGroupButton() { }
    }
}
