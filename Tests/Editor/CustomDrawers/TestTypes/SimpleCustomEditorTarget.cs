#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using UnityEngine;

    /// <summary>
    /// A ScriptableObject that has a custom editor associated with it for testing.
    /// </summary>
    internal sealed class SimpleCustomEditorTarget : ScriptableObject
    {
        public bool toggle;
        public int number;
    }
}
#endif
