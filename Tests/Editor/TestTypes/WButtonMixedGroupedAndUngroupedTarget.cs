// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with a mix of grouped buttons (with conflicts) and ungrouped buttons.
    /// Expected: Grouped buttons merge by group name, ungrouped buttons remain separate by draw order.
    /// </summary>
    public sealed class WButtonMixedGroupedAndUngroupedTarget : ScriptableObject
    {
        [WButton("Setup Init", drawOrder: -10, groupName: "Setup")]
        public void SetupInit() { }

        [WButton("Ungrouped Top", drawOrder: 0)]
        public void UngroupedTop() { }

        [WButton("Setup Validate", drawOrder: 5, groupName: "Setup")]
        public void SetupValidate() { }

        [WButton("Ungrouped Bottom", drawOrder: -5)]
        public void UngroupedBottom() { }
    }
}
