#if UNITY_EDITOR
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for single-element arrays.
    /// </summary>
    internal sealed class SingleElementTarget : ScriptableObject
    {
        [WGroup("Single", "Single Elements")]
        public List<int> singleItemList = new() { 42 };

        [WGroupEnd("Single")]
        public int[] singleItemArray = new[] { 99 };
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif
