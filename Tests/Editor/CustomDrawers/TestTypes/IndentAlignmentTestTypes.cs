// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    [Serializable]
    public sealed class IndentAlignmentTestStringIntDictionary
        : SerializableDictionary<string, int> { }

    [Serializable]
    public sealed class IndentAlignmentTestIntSet : SerializableHashSet<int> { }

    public sealed class IndentAlignmentSimpleDictionaryHost : ScriptableObject
    {
        public IndentAlignmentTestStringIntDictionary dictionary = new();
    }

    public sealed class IndentAlignmentSimpleSetHost : ScriptableObject
    {
        public IndentAlignmentTestIntSet set = new();
    }

    public sealed class IndentAlignmentWGroupDictionaryHost : ScriptableObject
    {
        [WGroup("TestGroup", displayName: "Test Group", collapsible: true, autoIncludeCount: 1)]
        public IndentAlignmentTestStringIntDictionary dictionary = new();
    }

    public sealed class IndentAlignmentWGroupSetHost : ScriptableObject
    {
        [WGroup("TestGroup", displayName: "Test Group", collapsible: true, autoIncludeCount: 1)]
        public IndentAlignmentTestIntSet set = new();
    }
}
