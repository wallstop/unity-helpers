namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    internal sealed class SortedStringSetHost : ScriptableObject
    {
        public SerializableSortedSet<string> set = new();
    }
}
