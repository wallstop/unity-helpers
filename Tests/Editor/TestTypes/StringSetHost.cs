namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    internal sealed class StringSetHost : ScriptableObject
    {
        public SerializableHashSet<string> set = new();
    }
}
