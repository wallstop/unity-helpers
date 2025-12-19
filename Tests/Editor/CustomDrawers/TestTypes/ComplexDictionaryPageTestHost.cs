namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    public sealed class ComplexDictionaryPageTestHost : ScriptableObject
    {
        public ComplexDictionaryPageTestDictionary dictionary = new();
    }

    [Serializable]
    public sealed class ComplexDictionaryPageTestDictionary
        : SerializableDictionary<int, ComplexDictionaryPageTestValue> { }

    [Serializable]
    public sealed class ComplexDictionaryPageTestValue
    {
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.black;
        public string label = string.Empty;
    }
}
