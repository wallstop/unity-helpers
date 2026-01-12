// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for mixed nested and non-nested groups.
    /// </summary>
    public sealed class MixedNestingTarget : ScriptableObject
    {
        [WGroup("standalone", "Standalone")]
        public string standaloneField;

        [WGroupEnd("standalone")]
        [WGroup("parent", "Parent")]
        public string parentField;

        [WGroup("nested", "Nested", parentGroup: "parent")]
        public string nestedField;

        [WGroupEnd("nested")]
        [WGroupEnd("parent")]
        public string ungrouped;
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif
