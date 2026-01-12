// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for zero auto-include count via attribute.
    /// </summary>
    public sealed class WGroupZeroAutoIncludeTestTarget : ScriptableObject
    {
        /// <summary>
        /// First field with explicit autoIncludeCount: 0.
        /// This should capture no subsequent fields regardless of global settings.
        /// </summary>
        [WGroup("Zero Group", autoIncludeCount: 0)]
        public int zeroGroupFirst;

        public int notCaptured1;
        public int notCaptured2;
    }
}
#endif
