#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using UnityEngine;

    /// <summary>
    /// A ScriptableObject with various numeric fields for testing simple property detection.
    /// </summary>
    internal sealed class NumericTypesTarget : ScriptableObject
    {
        public int intValue;
        public float floatValue;
        public double doubleValue;
        public long longValue;
    }
}
#endif
