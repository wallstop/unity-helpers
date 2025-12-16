namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with default groupPlacement (UseGlobalSetting). Expected: Renders based on global setting.
    /// </summary>
    public sealed class WButtonGroupPlacementDefaultTarget : ScriptableObject
    {
        [WButton("Default Placement", groupName: "DefaultGroup")]
        public void DefaultPlacement() { }
    }
}
