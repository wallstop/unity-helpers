namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    internal sealed class SetScalarAfterHost : ScriptableObject
    {
        public SerializableHashSet<int> set = new();
        public int trailingScalar;
    }
}
