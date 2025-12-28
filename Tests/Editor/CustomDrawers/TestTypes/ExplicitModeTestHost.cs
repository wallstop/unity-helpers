// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Host ScriptableObject with fields for each explicit inline editor mode for testing.
    /// </summary>
    internal sealed class ExplicitModeTestHost : ScriptableObject
    {
        [WInLineEditor(WInLineEditorMode.FoldoutCollapsed)]
        public InlineEditorTarget foldoutCollapsedTarget;

        [WInLineEditor(WInLineEditorMode.FoldoutExpanded)]
        public InlineEditorTarget foldoutExpandedTarget;

        [WInLineEditor(WInLineEditorMode.AlwaysExpanded)]
        public InlineEditorTarget alwaysExpandedTarget;
    }
}
#endif
