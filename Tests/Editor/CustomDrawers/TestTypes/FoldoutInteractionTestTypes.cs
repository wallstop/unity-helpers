// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    [Serializable]
    public sealed class FoldoutInteractionTestDictionary : SerializableDictionary<string, int> { }

    [Serializable]
    public sealed class FoldoutInteractionTestSet : SerializableHashSet<int> { }

    public sealed class FoldoutInteractionDictionaryHost : ScriptableObject
    {
        public FoldoutInteractionTestDictionary dictionary = new();
    }

    public sealed class FoldoutInteractionSetHost : ScriptableObject
    {
        public FoldoutInteractionTestSet set = new();
    }
}
