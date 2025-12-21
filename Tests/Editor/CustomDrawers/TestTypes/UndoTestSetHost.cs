namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    [Serializable]
    public sealed class UndoIntHashSetDictionary : SerializableHashSet<int> { }

    public sealed class UndoTestSetHost : ScriptableObject
    {
        public UndoIntHashSetDictionary set = new();
    }
}
