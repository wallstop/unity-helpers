// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    /// <summary>
    /// Test host for undo/redo operations on SerializableDictionary.
    /// </summary>
    public sealed class UndoTestDictionaryHost : ScriptableObject
    {
        public UndoTestIntStringDictionary dictionary = new();
    }

    [Serializable]
    public sealed class UndoTestIntStringDictionary : SerializableDictionary<int, string> { }
}
