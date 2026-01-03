// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Host ScriptableObject with compact inline editor that is always expanded (no header, no object field).
    /// This represents the most compact configuration where only the inline inspector is visible.
    /// </summary>
    internal sealed class CompactAlwaysExpandedHost : ScriptableObject
    {
        [WInLineEditor(
            mode: WInLineEditorMode.AlwaysExpanded,
            drawObjectField: false,
            drawHeader: false
        )]
        public InlineEditorTarget compactExpandedTarget;
    }
}
#endif
