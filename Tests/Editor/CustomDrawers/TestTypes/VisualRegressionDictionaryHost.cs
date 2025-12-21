namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure.Adapters;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;

    public sealed class VisualRegressionDictionaryHost : ScriptableObject
    {
        public SerializableDictionary<
            DrawerVisualRegressionKey,
            DrawerVisualRegressionDictionaryValue
        > dictionary = new();
    }
}
