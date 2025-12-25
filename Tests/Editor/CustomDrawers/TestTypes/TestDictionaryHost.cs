namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using UnityEngine;

    /// <summary>
    /// Test host for basic int-string dictionary operations.
    /// </summary>
    public sealed class TestDictionaryHost : ScriptableObject
    {
        public IntStringDictionary dictionary = new();
    }
}
