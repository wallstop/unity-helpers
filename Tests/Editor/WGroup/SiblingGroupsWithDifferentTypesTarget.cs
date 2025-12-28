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
    /// Test target for multiple sibling groups with different field types.
    /// </summary>
    internal sealed class SiblingGroupsWithDifferentTypesTarget : ScriptableObject
    {
        [WGroup(
            "GroupA",
            "Group A - Primitives",
            autoIncludeCount: WGroupAttribute.InfiniteAutoInclude
        )]
        public int intA;

        [WGroup("GroupA"), WGroupEnd("GroupA")]
        public float floatA;

        [WGroup(
            "GroupB",
            "Group B - Collections",
            autoIncludeCount: WGroupAttribute.InfiniteAutoInclude
        )]
        public List<int> listB = new();

        [WGroup("GroupB"), WGroupEnd("GroupB")]
        public int[] arrayB = Array.Empty<int>();

        [WGroup("GroupC", "Group C - Nested")]
        public NestedData nestedC = new();

        [WGroupEnd("GroupC")]
        public int afterAllGroups;
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif
