#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using UnityEngine;

    /// <summary>
    /// A ScriptableObject with a Color field for testing simple property detection.
    /// </summary>
    internal sealed class ColorTarget : ScriptableObject
    {
        public Color color;
    }
}
#endif
