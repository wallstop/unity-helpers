namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
    using UnityEngine;

    /// <summary>
    /// Test host for mixed scalar and dictionary field testing.
    /// </summary>
    public sealed class MixedFieldsDictionaryHost : ScriptableObject
    {
        public int scalarValue;
        public IntStringDictionary dictionary = new();
    }
}
