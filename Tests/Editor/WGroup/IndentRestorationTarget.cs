#if UNITY_EDITOR
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for verifying indent restoration after drawing.
    /// </summary>
    internal sealed class IndentRestorationTarget : ScriptableObject
    {
        public int beforeGroup;

        [WGroup("Middle", "Middle Group", autoIncludeCount: WGroupAttribute.InfiniteAutoInclude)]
        public List<int> middleList = new();

        [WGroup("Middle"), WGroupEnd("Middle")]
        public NestedData middleNested = new();

        public int afterGroup;
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif
