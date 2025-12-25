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
