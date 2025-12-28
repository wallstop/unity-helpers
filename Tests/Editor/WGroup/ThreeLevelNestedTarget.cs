// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for deep three-level nesting.
    /// </summary>
    public sealed class ThreeLevelNestedTarget : ScriptableObject
    {
        [WGroup("level1", "Level 1")]
        public string field1;

        [WGroup("level2", "Level 2", parentGroup: "level1")]
        public string field2;

        [WGroup("level3", "Level 3", parentGroup: "level2")]
        public string field3;

        [WGroupEnd("level3")]
        [WGroupEnd("level2")]
        [WGroupEnd("level1")]
        public string field4;
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif
