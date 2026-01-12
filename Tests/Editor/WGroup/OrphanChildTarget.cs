// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for orphan child with non-existent parent group.
    /// </summary>
    public sealed class OrphanChildTarget : ScriptableObject
    {
        [WGroup("child", "Child", parentGroup: "nonExistent")]
        [WGroupEnd("child")]
        public string childField;
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif
