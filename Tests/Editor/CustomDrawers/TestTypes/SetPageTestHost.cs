// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using System;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;

    public sealed class SetPageTestHost : ScriptableObject
    {
        public SetPageTestHashSet hashSet = new();
    }

    [Serializable]
    public sealed class SetPageTestHashSet : SerializableHashSet<int> { }
}
