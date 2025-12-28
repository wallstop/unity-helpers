// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;

    public sealed class VisualRegressionSetHost : ScriptableObject
    {
        public SerializableHashSet<DrawerVisualRegressionSetValue> set = new();
    }
}
