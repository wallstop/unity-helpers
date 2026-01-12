// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for explicit auto-include count testing.
    /// Unlike WGroupAutoIncludeTestTarget which uses UseGlobalAutoInclude,
    /// this target explicitly specifies autoIncludeCount which should override
    /// any global settings.
    /// </summary>
    /// <remarks>
    /// Field layout:
    /// - explicitGroupFirst: [WGroup] with explicit autoIncludeCount: 2
    /// - captured1: Unattributed (will be auto-included due to explicit count)
    /// - captured2: Unattributed (will be auto-included due to explicit count)
    /// - notCaptured: Unattributed (should NOT be included - beyond explicit count of 2)
    /// </remarks>
    public sealed class WGroupExplicitAutoIncludeTestTarget : ScriptableObject
    {
        /// <summary>
        /// First field with explicit autoIncludeCount: 2.
        /// This should always capture exactly 2 subsequent fields regardless of global settings.
        /// </summary>
        [WGroup("Explicit Group", autoIncludeCount: 2)]
        public int explicitGroupFirst;

        public int captured1;
        public int captured2;
        public int notCaptured;
    }
}
#endif
