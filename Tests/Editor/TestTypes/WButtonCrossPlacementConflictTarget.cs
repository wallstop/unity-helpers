// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with conflicting draw orders within the same group.
    /// First declared has drawOrder 0, second has drawOrder -5.
    /// Expected: Uses first declared drawOrder (0), but placement is determined by
    /// groupPlacement (defaults to UseGlobalSetting, which defers to globalPlacementIsTop).
    /// </summary>
    public sealed class WButtonCrossPlacementConflictTarget : ScriptableObject
    {
        [WButton("Top Placement", drawOrder: 0, groupName: "CrossPlacement")]
        public void TopPlacement() { }

        [WButton("Bottom Placement", drawOrder: -5, groupName: "CrossPlacement")]
        public void BottomPlacement() { }
    }
}
