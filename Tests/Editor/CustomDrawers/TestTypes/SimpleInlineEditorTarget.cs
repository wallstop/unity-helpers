#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using UnityEngine;

    /// <summary>
    /// A simple ScriptableObject with basic fields for testing simple property detection.
    /// </summary>
    internal sealed class SimpleInlineEditorTarget : ScriptableObject
    {
        public int number;
        public string description;
    }
}
#endif
