// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target combining groupPriority, groupPlacement, and drawOrder.
    /// Expected:
    /// - Groups sorted by groupPriority
    /// - Within each group, buttons sorted by drawOrder
    /// - Placement determined by groupPlacement
    /// </summary>
    public sealed class WButtonComplexGroupingTarget : ScriptableObject
    {
        [WButton(
            "High Priority Top A",
            drawOrder: 1,
            groupName: "HighPriorityTop",
            groupPriority: 0,
            groupPlacement: WButtonGroupPlacement.Top
        )]
        public void HighPriorityTopA() { }

        [WButton(
            "High Priority Top B",
            drawOrder: 0,
            groupName: "HighPriorityTop",
            groupPriority: 0,
            groupPlacement: WButtonGroupPlacement.Top
        )]
        public void HighPriorityTopB() { }

        [WButton(
            "Low Priority Top A",
            drawOrder: 1,
            groupName: "LowPriorityTop",
            groupPriority: 10,
            groupPlacement: WButtonGroupPlacement.Top
        )]
        public void LowPriorityTopA() { }

        [WButton(
            "Low Priority Top B",
            drawOrder: 0,
            groupName: "LowPriorityTop",
            groupPriority: 10,
            groupPlacement: WButtonGroupPlacement.Top
        )]
        public void LowPriorityTopB() { }

        [WButton(
            "Bottom Group Button",
            groupName: "BottomGroup",
            groupPriority: 5,
            groupPlacement: WButtonGroupPlacement.Bottom
        )]
        public void BottomGroupButton() { }
    }
}
