namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with explicit Bottom groupPlacement. Expected: Always renders at bottom regardless of global setting.
    /// </summary>
    public sealed class WButtonGroupPlacementBottomTarget : ScriptableObject
    {
        [WButton(
            "Bottom Placement",
            groupName: "BottomGroup",
            groupPlacement: WButtonGroupPlacement.Bottom
        )]
        public void BottomPlacement() { }
    }
}
