// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for circular reference detection between groups.
    /// </summary>
    public sealed class CircularReferenceTarget : ScriptableObject
    {
        [WGroup("groupA", "Group A", parentGroup: "groupB")]
        [WGroupEnd("groupA")]
        public string fieldA;

        [WGroup("groupB", "Group B", parentGroup: "groupA")]
        [WGroupEnd("groupB")]
        public string fieldB;
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif
