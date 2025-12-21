namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using UnityEngine;

    /// <summary>
    /// Test host for dictionary with trailing scalar field.
    /// </summary>
    public sealed class DictionaryScalarAfterHost : ScriptableObject
    {
        public IntStringDictionary dictionary = new();
        public int trailingScalar;
    }
}
