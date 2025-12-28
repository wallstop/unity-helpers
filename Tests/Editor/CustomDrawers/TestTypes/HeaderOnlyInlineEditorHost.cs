// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Host ScriptableObject with inline editor that shows header only (no object field) for testing.
    /// </summary>
    internal sealed class HeaderOnlyInlineEditorHost : ScriptableObject
    {
        [WInLineEditor(mode: WInLineEditorMode.FoldoutCollapsed, drawObjectField: false)]
        public InlineEditorTarget collapsedTarget;
    }
}
#endif
