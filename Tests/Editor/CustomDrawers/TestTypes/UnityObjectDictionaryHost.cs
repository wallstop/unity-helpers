namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using UnityEngine;

    /// <summary>
    /// Test host for Unity Object key dictionary operations.
    /// </summary>
    public sealed class UnityObjectDictionaryHost : ScriptableObject
    {
        public GameObjectStringDictionary dictionary = new();
    }
}
