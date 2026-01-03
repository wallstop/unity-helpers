// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target that reproduces the user's exact issue:
    /// Two buttons with the same groupName but different drawOrder values.
    /// Before the fix: These would render in separate groups (or not at all).
    /// After the fix: These should render in the same "Setup" group, using drawOrder -21 (first declared).
    /// </summary>
    public sealed class WButtonConflictingDrawOrderTarget : ScriptableObject
    {
        // Setup group - appears above properties
        [WButton("Initialize Level", drawOrder: -21, groupName: "Setup")]
        public void Initialize()
        {
            Debug.Log("Level initialized!");
        }

        [WButton("Validate Configuration", drawOrder: -2, groupName: "Setup")]
        public void ValidateConfig()
        {
            Debug.Log("Configuration valid!");
        }
    }
}
