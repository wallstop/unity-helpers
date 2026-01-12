// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for declaration order testing.
    /// </summary>
    public sealed class WGroupDeclarationOrderTestTarget : ScriptableObject
    {
        [WGroup("First")]
        public int first1;

        [WGroup("Second")]
        public int second1;

        [WGroup("Third")]
        public int third1;

        [WGroup("First")]
        public int first2;

        [WGroup("Second")]
        public int second2;
    }
}
#endif
