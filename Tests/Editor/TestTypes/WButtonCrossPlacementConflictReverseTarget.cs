// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with conflicting draw orders where first declared has drawOrder -5.
    /// Expected: Uses first declared drawOrder (-5), but placement is determined by
    /// groupPlacement (defaults to UseGlobalSetting, which defers to globalPlacementIsTop).
    /// </summary>
    public sealed class WButtonCrossPlacementConflictReverseTarget : ScriptableObject
    {
        [WButton("Bottom First", drawOrder: -5, groupName: "CrossPlacement")]
        public void BottomFirst() { }

        [WButton("Top Second", drawOrder: 0, groupName: "CrossPlacement")]
        public void TopSecond() { }
    }
}
