// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target where all buttons in a group use the same explicit priority.
    /// This should NOT generate any priority conflict warnings.
    /// </summary>
    public sealed class WButtonAllSameExplicitPriorityTarget : ScriptableObject
    {
        [WButton("Button A", groupName: "PriorityGroup", groupPriority: 5)]
        public void ButtonA() { }

        [WButton("Button B", groupName: "PriorityGroup", groupPriority: 5)]
        public void ButtonB() { }

        [WButton("Button C", groupName: "PriorityGroup", groupPriority: 5)]
        public void ButtonC() { }
    }
}
