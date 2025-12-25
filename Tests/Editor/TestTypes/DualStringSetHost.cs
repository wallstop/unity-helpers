namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    internal sealed class DualStringSetHost : ScriptableObject
    {
        public SerializableHashSet<string> firstSet = new();
        public SerializableHashSet<string> secondSet = new();
    }
}
