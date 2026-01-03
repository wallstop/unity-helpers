// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    [Serializable]
    public sealed class IndentationTestIntHashSet : SerializableHashSet<int> { }

    public sealed class IndentationTestSetHost : ScriptableObject
    {
        public IndentationTestIntHashSet set = new();
    }
}
