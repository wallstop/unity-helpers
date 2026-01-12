// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target where buttons have multiple different explicit priorities.
    /// This SHOULD generate a priority conflict warning.
    /// </summary>
    public sealed class WButtonMultipleExplicitPriorityConflictTarget : ScriptableObject
    {
        [WButton("Button A", groupName: "ConflictGroup", groupPriority: 0)]
        public void ButtonA() { }

        [WButton("Button B", groupName: "ConflictGroup", groupPriority: 10)]
        public void ButtonB() { }

        [WButton("Button C", groupName: "ConflictGroup")]
        public void ButtonC() { }
    }
}
