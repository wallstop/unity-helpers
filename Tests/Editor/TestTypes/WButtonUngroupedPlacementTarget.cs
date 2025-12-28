// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with ungrouped buttons (no groupName). Expected: groupPriority and groupPlacement are ignored for ungrouped buttons.
    /// </summary>
    public sealed class WButtonUngroupedPlacementTarget : ScriptableObject
    {
        [WButton("Ungrouped Top", groupPlacement: WButtonGroupPlacement.Top)]
        public void UngroupedTop() { }

        [WButton("Ungrouped Bottom", groupPlacement: WButtonGroupPlacement.Bottom)]
        public void UngroupedBottom() { }

        [WButton("Ungrouped Default")]
        public void UngroupedDefault() { }
    }
}
