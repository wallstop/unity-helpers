#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using System;
    using UnityEngine;

    /// <summary>
    /// A ScriptableObject with a nested serializable class for testing complex property detection.
    /// </summary>
    internal sealed class NestedClassTarget : ScriptableObject
    {
        [Serializable]
        public class NestedData
        {
            public int value;
        }

        public NestedData nested;
    }
}
#endif
