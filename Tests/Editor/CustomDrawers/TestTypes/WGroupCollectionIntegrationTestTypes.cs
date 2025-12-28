// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    [Serializable]
    public sealed class IntegrationTestStringIntDictionary : SerializableDictionary<string, int> { }

    [Serializable]
    public sealed class IntegrationTestIntSet : SerializableHashSet<int> { }

    [Serializable]
    public sealed class IntegrationTestSortedStringIntDictionary
        : SerializableSortedDictionary<string, int> { }

    [Serializable]
    public sealed class IntegrationTestSortedIntSet : SerializableSortedSet<int> { }

    public sealed class IntegrationTestWGroupDictionaryHost : ScriptableObject
    {
        [WGroup("TestGroup", displayName: "Test Group", collapsible: true, autoIncludeCount: 1)]
        public IntegrationTestStringIntDictionary dictionary = new();
    }

    public sealed class IntegrationTestWGroupSetHost : ScriptableObject
    {
        [WGroup("TestGroup", displayName: "Test Group", collapsible: true, autoIncludeCount: 1)]
        public IntegrationTestIntSet set = new();
    }

    public sealed class IntegrationTestMultiWGroupHost : ScriptableObject
    {
        [WGroup("OuterGroup", displayName: "Outer Group", collapsible: true, autoIncludeCount: 3)]
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        public int outerField;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

        [WGroup("InnerGroup", displayName: "Inner Group", collapsible: true, autoIncludeCount: 1)]
        public IntegrationTestStringIntDictionary nestedDictionary = new();

        [WGroupEnd("InnerGroup")]
        public IntegrationTestIntSet nestedSet = new();

        [WGroupEnd("OuterGroup")]
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        public int outerEndField;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
    }

    public sealed class IntegrationTestNonCollapsibleWGroupHost : ScriptableObject
    {
        [WGroup(
            "StaticGroup",
            displayName: "Static Group",
            collapsible: false,
            autoIncludeCount: 1
        )]
        public IntegrationTestStringIntDictionary dictionary = new();
    }

    public sealed class IntegrationTestWGroupSortedDictionaryHost : ScriptableObject
    {
        [WGroup("SortedGroup", displayName: "Sorted Group", collapsible: true, autoIncludeCount: 1)]
        public IntegrationTestSortedStringIntDictionary sortedDictionary = new();
    }

    public sealed class IntegrationTestWGroupSortedSetHost : ScriptableObject
    {
        [WGroup("SortedGroup", displayName: "Sorted Group", collapsible: true, autoIncludeCount: 1)]
        public IntegrationTestSortedIntSet sortedSet = new();
    }
}
