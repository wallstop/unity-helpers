namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with explicit Top groupPlacement. Expected: Always renders at top regardless of global setting.
    /// </summary>
    public sealed class WButtonGroupPlacementTopTarget : ScriptableObject
    {
        [WButton("Top Placement", groupName: "TopGroup", groupPlacement: WButtonGroupPlacement.Top)]
        public void TopPlacement() { }
    }
}
