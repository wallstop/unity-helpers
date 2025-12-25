namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    internal sealed class ObjectSetHost : ScriptableObject
    {
        public SerializableHashSet<TestData> set = new();
    }
}
