// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using UnityEngine;

    /// <summary>
    /// A simple ScriptableObject used as the inline editor target in tests.
    /// Contains a single serialized field for testing basic inline inspector functionality.
    /// </summary>
    internal sealed class InlineEditorTarget : ScriptableObject
    {
        public int sampleValue;
    }
}
#endif
