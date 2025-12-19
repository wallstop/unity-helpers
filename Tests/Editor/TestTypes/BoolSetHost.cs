namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    internal sealed class BoolSetHost : ScriptableObject
    {
        public SerializableHashSet<bool> set = new();
    }
}
