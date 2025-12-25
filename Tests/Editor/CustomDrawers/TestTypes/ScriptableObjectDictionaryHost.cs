namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using UnityEngine;

    /// <summary>
    /// Test host for ScriptableObject value dictionary operations.
    /// </summary>
    public sealed class ScriptableObjectDictionaryHost : ScriptableObject
    {
        public StringScriptableDictionary dictionary = new();
    }
}
