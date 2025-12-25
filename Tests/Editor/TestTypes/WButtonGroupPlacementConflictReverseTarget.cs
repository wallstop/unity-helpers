namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with conflicting groupPlacement values where first is Bottom. Expected: Uses first declared button's placement (Bottom) and generates warning.
    /// </summary>
    public sealed class WButtonGroupPlacementConflictReverseTarget : ScriptableObject
    {
        [WButton(
            "First Button Bottom",
            groupName: "ConflictGroup",
            groupPlacement: WButtonGroupPlacement.Bottom
        )]
        public void FirstButtonBottom() { }

        [WButton(
            "Second Button Top",
            groupName: "ConflictGroup",
            groupPlacement: WButtonGroupPlacement.Top
        )]
        public void SecondButtonTop() { }
    }
}
