// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for infinite auto-include count via attribute (not global setting).
    /// </summary>
    public sealed class WGroupInfiniteAutoIncludeTestTarget : ScriptableObject
    {
        /// <summary>
        /// First field with explicit InfiniteAutoInclude (-1).
        /// This should capture all subsequent fields until end of type or WGroupEnd.
        /// </summary>
        [WGroup("Infinite Group", autoIncludeCount: WGroupAttribute.InfiniteAutoInclude)]
        public int infiniteGroupFirst;

        public int capturedA;
        public int capturedB;
        public int capturedC;
    }
}
#endif
