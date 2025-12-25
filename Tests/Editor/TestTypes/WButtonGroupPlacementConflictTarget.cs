namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with conflicting groupPlacement values in the same group. Expected: Uses first declared button's placement (Top) and generates warning.
    /// </summary>
    public sealed class WButtonGroupPlacementConflictTarget : ScriptableObject
    {
        [WButton(
            "First Button Top",
            groupName: "ConflictGroup",
            groupPlacement: WButtonGroupPlacement.Top
        )]
        public void FirstButtonTop() { }

        [WButton(
            "Second Button Bottom",
            groupName: "ConflictGroup",
            groupPlacement: WButtonGroupPlacement.Bottom
        )]
        public void SecondButtonBottom() { }
    }
}
