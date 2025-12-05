#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using UnityEngine;

    /// <summary>
    /// A ScriptableObject with an AnimationCurve field for testing complex property detection.
    /// </summary>
    internal sealed class AnimationCurveTarget : ScriptableObject
    {
        public AnimationCurve curve;
    }
}
#endif
