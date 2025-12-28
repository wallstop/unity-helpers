// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for basic two-level nested groups.
    /// </summary>
    public sealed class TwoLevelNestedTarget : ScriptableObject
    {
        [WGroup("outer", "Character", autoIncludeCount: WGroupAttribute.InfiniteAutoInclude)]
        public string characterName;

        [WGroup(
            "inner",
            "Stats",
            parentGroup: "outer",
            autoIncludeCount: WGroupAttribute.InfiniteAutoInclude
        )]
        public int level;

        [WGroup("inner"), WGroupEnd("inner")]
        public int experience;

        [WGroup("outer"), WGroupEnd("outer")]
        public string faction;
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif
