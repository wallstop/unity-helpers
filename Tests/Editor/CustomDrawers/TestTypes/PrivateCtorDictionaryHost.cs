namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using UnityEngine;

    /// <summary>
    /// Test host for private constructor key/value dictionary operations.
    /// </summary>
    public sealed class PrivateCtorDictionaryHost : ScriptableObject
    {
        public PrivateCtorDictionary dictionary = new();
    }
}
