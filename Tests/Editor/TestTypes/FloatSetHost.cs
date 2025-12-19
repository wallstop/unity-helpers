namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    internal sealed class FloatSetHost : ScriptableObject
    {
        public SerializableHashSet<float> set = new();
    }
}
