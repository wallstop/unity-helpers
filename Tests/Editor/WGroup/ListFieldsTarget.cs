// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace WallstopStudios.UnityHelpers.Tests.WGroup
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for List fields within WGroups - the main bug case.
    /// </summary>
    internal sealed class ListFieldsTarget : ScriptableObject
    {
        [WGroup("Lists", "List Types", autoIncludeCount: WGroupAttribute.InfiniteAutoInclude)]
        public List<int> intList = new();

        public List<string> stringList = new();

        [WGroup("Lists"), WGroupEnd("Lists")]
        public List<float> floatList = new();

        public int afterListField;
    }
}
#pragma warning restore CS0414 // Field is assigned but its value is never used
#endif
