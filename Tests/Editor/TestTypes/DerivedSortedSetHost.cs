// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    [Serializable]
    internal sealed class CustomSortedSet : SerializableSortedSet<int> { }

    internal sealed class DerivedSortedSetHost : ScriptableObject
    {
        public CustomSortedSet set = new();
    }
}
