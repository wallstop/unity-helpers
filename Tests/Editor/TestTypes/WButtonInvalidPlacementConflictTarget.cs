// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with an invalid first-declared groupPlacement value in a named group.
    /// Expected: Draw path remains stable, canonical placement remains first declared,
    /// and conflict warnings include both explicit values.
    /// </summary>
    public sealed class WButtonInvalidPlacementConflictTarget : ScriptableObject
    {
        [WButton(
            "First Button Invalid Placement",
            groupName: "InvalidPlacementGroup",
            groupPlacement: (WButtonGroupPlacement)999
        )]
        public void FirstButtonInvalidPlacement() { }

        [WButton(
            "Second Button Top",
            groupName: "InvalidPlacementGroup",
            groupPlacement: WButtonGroupPlacement.Top
        )]
        public void SecondButtonTop() { }
    }
}
