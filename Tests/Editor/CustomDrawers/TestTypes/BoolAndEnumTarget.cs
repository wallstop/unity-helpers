// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// A ScriptableObject with bool and enum fields for testing simple property detection.
    /// </summary>
    internal sealed class BoolAndEnumTarget : ScriptableObject
    {
        public bool boolValue;
        public WInLineEditorMode enumValue;
    }
}
#endif
