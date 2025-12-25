namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target where all buttons in a group use default placement (UseGlobalSetting).
    /// This should NOT generate any placement conflict warnings.
    /// </summary>
    public sealed class WButtonAllDefaultPlacementTarget : ScriptableObject
    {
        [WButton("Button A", groupName: "DefaultGroup")]
        public void ButtonA() { }

        [WButton("Button B", groupName: "DefaultGroup")]
        public void ButtonB() { }

        [WButton("Button C", groupName: "DefaultGroup")]
        public void ButtonC() { }
    }
}
