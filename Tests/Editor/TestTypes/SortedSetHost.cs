namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    internal sealed class SortedSetHost : ScriptableObject
    {
        public SerializableSortedSet<int> set = new();
    }
}
