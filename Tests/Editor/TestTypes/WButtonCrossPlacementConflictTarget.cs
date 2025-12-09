namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with conflicting draw orders that cross the top/bottom threshold.
    /// Draw order >= -1 is top placement, < -1 is bottom placement.
    /// Expected: Uses first declared (0) which is top placement.
    /// </summary>
    public sealed class WButtonCrossPlacementConflictTarget : ScriptableObject
    {
        [WButton("Top Placement", drawOrder: 0, groupName: "CrossPlacement")]
        public void TopPlacement() { }

        [WButton("Bottom Placement", drawOrder: -5, groupName: "CrossPlacement")]
        public void BottomPlacement() { }
    }
}
