namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    internal sealed class MixedFieldsSetHost : ScriptableObject
    {
        public int scalarValue;
        public SerializableHashSet<int> set = new();
    }
}
