namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    internal sealed class GameObjectSetHost : ScriptableObject
    {
        public SerializableHashSet<GameObject> set = new();
    }
}
