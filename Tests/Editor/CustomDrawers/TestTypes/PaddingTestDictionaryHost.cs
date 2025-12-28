// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    /// <summary>
    /// Test host for pending entry section padding resolution tests.
    /// </summary>
    public sealed class PaddingTestDictionaryHost : ScriptableObject
    {
        public PaddingTestIntStringDictionary dictionary = new();
    }

    [Serializable]
    public sealed class PaddingTestIntStringDictionary : SerializableDictionary<int, string> { }
}
