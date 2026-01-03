// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Host ScriptableObject with inline editor that has scrolling disabled for testing.
    /// </summary>
    internal sealed class NoScrollInlineEditorHost : ScriptableObject
    {
        [WInLineEditor(WInLineEditorMode.FoldoutCollapsed, 400f, false, 64f, true, true, false)]
        public InlineEditorTarget collapsedTarget;
    }
}
#endif
