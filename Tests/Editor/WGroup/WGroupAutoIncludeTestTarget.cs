// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for auto-include testing with global settings control.
    /// Uses the default UseGlobalAutoInclude so global settings can control behavior.
    /// </summary>
    /// <remarks>
    /// Field layout:
    /// - autoGroupFirst: Explicit [WGroup] with default auto-include (uses global setting)
    /// - autoIncluded1: Unattributed (can be auto-included based on global setting)
    /// - autoIncluded2: Unattributed (can be auto-included based on global setting)
    /// - notAutoIncluded: Unattributed (may or may not be auto-included based on global setting)
    /// </remarks>
    public sealed class WGroupAutoIncludeTestTarget : ScriptableObject
    {
        /// <summary>
        /// First field in the Auto Group. Uses default autoIncludeCount (UseGlobalAutoInclude)
        /// so the global WGroupAutoIncludeConfiguration controls how many subsequent fields are captured.
        /// </summary>
        [WGroup("Auto Group")]
        public int autoGroupFirst;

        public int autoIncluded1;
        public int autoIncluded2;
        public int notAutoIncluded;
    }
}
#endif
