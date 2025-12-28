// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WGroup layout testing.
    /// </summary>
    public sealed class WGroupLayoutTestTarget : ScriptableObject
    {
        [WGroup("Group A", displayName: "Alpha Group")]
        public int fieldA1;

        [WGroup("Group A")]
        public int fieldA2;

        [WGroup("Group B", displayName: "Beta Group")]
        public int fieldB1;

        [WGroup("Group B")]
        public int fieldB2;

        public int ungroupedField;

        [WGroup("Group C")]
        public int fieldC1;
    }
}
#endif
