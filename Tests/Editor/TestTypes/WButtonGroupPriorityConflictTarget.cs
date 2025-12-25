namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with conflicting groupPriority values in the same group. Expected: Uses first declared button's priority (0) and generates warning.
    /// </summary>
    public sealed class WButtonGroupPriorityConflictTarget : ScriptableObject
    {
        [WButton(
            "First Button Priority 0",
            groupName: "ConflictGroup",
            groupPriority: 0,
            groupPlacement: WButtonGroupPlacement.Top
        )]
        public void FirstButtonPriority0() { }

        [WButton(
            "Second Button Priority 10",
            groupName: "ConflictGroup",
            groupPriority: 10,
            groupPlacement: WButtonGroupPlacement.Top
        )]
        public void SecondButtonPriority10() { }
    }
}
