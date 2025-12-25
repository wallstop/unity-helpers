namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    internal sealed class NullableContainer : ScriptableObject
    {
        public SerializableNullable<int> integerValue;
    }
}
