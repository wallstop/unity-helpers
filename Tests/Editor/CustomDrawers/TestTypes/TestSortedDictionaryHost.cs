// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    /// <summary>
    /// Test host for sorted dictionary operations.
    /// </summary>
    public sealed class TestSortedDictionaryHost : ScriptableObject
    {
        public SerializableSortedDictionary<int, string> dictionary = new();
    }
}
