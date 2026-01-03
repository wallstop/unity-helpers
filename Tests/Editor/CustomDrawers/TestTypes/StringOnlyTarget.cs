// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using UnityEngine;

    /// <summary>
    /// A ScriptableObject with only a string field for testing simple property detection.
    /// </summary>
    internal sealed class StringOnlyTarget : ScriptableObject
    {
        public string text;
    }
}
#endif
