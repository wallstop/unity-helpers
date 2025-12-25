#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// A ScriptableObject with a List field for testing complex property detection.
    /// </summary>
    internal sealed class ListTarget : ScriptableObject
    {
        public List<int> list;
    }
}
#endif
