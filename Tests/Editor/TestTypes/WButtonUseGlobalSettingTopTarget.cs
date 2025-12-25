namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for verifying UseGlobalSetting behavior with global Top.
    /// </summary>
    public sealed class WButtonUseGlobalSettingTopTarget : ScriptableObject
    {
        [WButton(
            "Explicit UseGlobalSetting",
            groupName: "GlobalGroup",
            groupPlacement: WButtonGroupPlacement.UseGlobalSetting
        )]
        public void ExplicitUseGlobalSetting() { }
    }
}
