#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using UnityEngine;

    /// <summary>
    /// A ScriptableObject with an array field for testing complex property detection.
    /// </summary>
    internal sealed class ArrayInlineEditorTarget : ScriptableObject
    {
        public int[] values = new int[2];
    }
}
#endif
