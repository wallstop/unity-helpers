#if UNITY_EDITOR
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for complex combined scenarios.
    /// </summary>
    internal sealed class ComplexCombinedTarget : ScriptableObject
    {
        [WGroup("Outer", "Outer Container")]
        public int outerSimple;

        public List<int> outerList = new();

        [WGroup("MiddleNested", "Middle Nested", parentGroup: "Outer")]
        public NestedData middleData = new();

        public List<NestedData> middleNestedList = new();

        [WGroup("Deepest", "Deepest Level", parentGroup: "MiddleNested")]
        public int[] deepestArray = Array.Empty<int>();

        [WGroupEnd("Deepest")]
        public int deepestEnd;

        [WGroupEnd("MiddleNested")]
        public int middleEnd;

        [WGroupEnd("Outer")]
        public int outerEnd;
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif
