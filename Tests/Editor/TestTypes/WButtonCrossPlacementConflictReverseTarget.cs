namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with conflicting draw orders where first is bottom, second is top.
    /// Expected: Uses first declared (-5) which is bottom placement.
    /// </summary>
    public sealed class WButtonCrossPlacementConflictReverseTarget : ScriptableObject
    {
        [WButton("Bottom First", drawOrder: -5, groupName: "CrossPlacement")]
        public void BottomFirst() { }

        [WButton("Top Second", drawOrder: 0, groupName: "CrossPlacement")]
        public void TopSecond() { }
    }
}
