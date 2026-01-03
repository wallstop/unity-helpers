// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

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
