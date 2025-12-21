namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    [Serializable]
    public sealed class PaddingTestIntHashSet : SerializableHashSet<int> { }

    public sealed class PaddingTestSetHost : ScriptableObject
    {
        public PaddingTestIntHashSet set = new();
    }
}
