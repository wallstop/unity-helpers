#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for auto-include testing.
    /// </summary>
    public sealed class WGroupAutoIncludeTestTarget : ScriptableObject
    {
        [WGroup("Auto Group", autoIncludeCount: 2)]
        public int autoGroupFirst;

        public int autoIncluded1;
        public int autoIncluded2;
        public int notAutoIncluded;
    }
}
#endif
