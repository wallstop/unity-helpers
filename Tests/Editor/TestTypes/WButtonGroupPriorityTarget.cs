namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with groupPriority controlling order within same placement. Expected: Groups render in priority order (lower first).
    /// </summary>
    public sealed class WButtonGroupPriorityTarget : ScriptableObject
    {
        [WButton(
            "Priority 10",
            groupName: "Group10",
            groupPriority: 10,
            groupPlacement: WButtonGroupPlacement.Top
        )]
        public void Priority10() { }

        [WButton(
            "Priority 5",
            groupName: "Group5",
            groupPriority: 5,
            groupPlacement: WButtonGroupPlacement.Top
        )]
        public void Priority5() { }

        [WButton(
            "Priority 0",
            groupName: "Group0",
            groupPriority: 0,
            groupPlacement: WButtonGroupPlacement.Top
        )]
        public void Priority0() { }

        [WButton(
            "No Priority",
            groupName: "GroupNoPriority",
            groupPlacement: WButtonGroupPlacement.Top
        )]
        public void NoPriority() { }
    }
}
