// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    /// <summary>
    /// Test host for indentation behavior tests on SerializableDictionary.
    /// </summary>
    public sealed class IndentationTestDictionaryHost : ScriptableObject
    {
        public IndentationTestIntStringDictionary dictionary = new();
    }

    [Serializable]
    public sealed class IndentationTestIntStringDictionary : SerializableDictionary<int, string> { }
}
