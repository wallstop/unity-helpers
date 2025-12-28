// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for empty arrays within WGroups.
    /// </summary>
    internal sealed class EmptyCollectionsTarget : ScriptableObject
    {
        [WGroup("Empty", "Empty Collections")]
        public List<int> emptyList = new();

        public int[] emptyArray = Array.Empty<int>();

        [WGroupEnd("Empty")]
        public List<NestedData> emptyNestedList = new();
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif
