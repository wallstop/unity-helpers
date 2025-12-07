namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    internal sealed class HashSetHost : ScriptableObject
    {
        public SerializableHashSet<int> set = new();
    }
}
