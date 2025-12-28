// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for deeply nested groups (3 levels).
    /// </summary>
    internal sealed class ThreeLevelGroupsTarget : ScriptableObject
    {
        [WGroup("Level1", "Level 1")]
        public int level1Field;

        [WGroup("Level2", "Level 2", parentGroup: "Level1")]
        public int level2Field;

        [WGroup("Level3", "Level 3", parentGroup: "Level2")]
        public List<int> level3List = new();

        [WGroupEnd("Level3")]
        public int level3EndField;

        [WGroupEnd("Level2")]
        public int level2EndField;

        [WGroupEnd("Level1")]
        public int level1EndField;
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif
