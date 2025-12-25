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
