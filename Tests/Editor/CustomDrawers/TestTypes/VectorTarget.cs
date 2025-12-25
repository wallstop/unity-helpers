#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using UnityEngine;

    /// <summary>
    /// A ScriptableObject with Vector fields for testing simple property detection.
    /// </summary>
    internal sealed class VectorTarget : ScriptableObject
    {
        public Vector2 vec2;
        public Vector3 vec3;
        public Vector4 vec4;
    }
}
#endif
