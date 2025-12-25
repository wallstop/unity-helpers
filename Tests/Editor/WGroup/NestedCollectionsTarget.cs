#if UNITY_EDITOR
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for nested arrays (arrays of arrays, Lists of Lists).
    /// </summary>
    internal sealed class NestedCollectionsTarget : ScriptableObject
    {
        [WGroup("NestedCollections", "Nested Collections")]
        public List<List<int>> listOfLists = new();

        [WGroupEnd("NestedCollections")]
        public int[] simpleArray = Array.Empty<int>();
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif
