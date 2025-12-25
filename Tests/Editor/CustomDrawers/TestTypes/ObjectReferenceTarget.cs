#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using UnityEngine;

    /// <summary>
    /// A ScriptableObject with an object reference field for testing simple property detection.
    /// </summary>
    internal sealed class ObjectReferenceTarget : ScriptableObject
    {
        public Object objectRef;
    }
}
#endif
