namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    public sealed class SetPageTestHost : ScriptableObject
    {
        public SetPageTestHashSet hashSet = new();
    }

    [Serializable]
    public sealed class SetPageTestHashSet : SerializableHashSet<int> { }
}
