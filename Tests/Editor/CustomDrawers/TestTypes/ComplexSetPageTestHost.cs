namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    public sealed class ComplexSetPageTestHost : ScriptableObject
    {
        public ComplexSetPageTestHashSet hashSet = new();
    }

    [Serializable]
    public sealed class ComplexSetPageTestHashSet : SerializableHashSet<ComplexSetPageTestValue> { }

    [Serializable]
    public sealed class ComplexSetPageTestValue
    {
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.black;
        public string label = string.Empty;
    }
}
