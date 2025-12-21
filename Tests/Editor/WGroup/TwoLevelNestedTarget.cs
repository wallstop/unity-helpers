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
        [WGroup("outer", "Character")]
        public string characterName;

        [WGroup("inner", "Stats", parentGroup: "outer")]
        public int level;

        [WGroupEnd("inner")]
        public int experience;

        [WGroupEnd("outer")]
        public string faction;
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif
