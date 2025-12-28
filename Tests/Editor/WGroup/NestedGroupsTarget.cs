// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for groups within groups (nested WGroups).
    /// </summary>
    internal sealed class NestedGroupsTarget : ScriptableObject
    {
        [WGroup("Outer", "Outer Group")]
        public int outerField1;

        [WGroup("Inner", "Inner Group", parentGroup: "Outer")]
        public List<int> innerList = new();

        [WGroupEnd("Inner")]
        public int innerField2;

        [WGroupEnd("Outer")]
        public int outerField2;

        public int ungroupedField;
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif
