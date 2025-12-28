// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

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
