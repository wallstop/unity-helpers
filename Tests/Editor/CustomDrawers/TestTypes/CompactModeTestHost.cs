// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Host ScriptableObject with compact inline editor and explicit foldout states for testing.
    /// Tests various combinations of drawObjectField=false with different modes.
    /// </summary>
    internal sealed class CompactModeTestHost : ScriptableObject
    {
        /// <summary>
        /// Compact mode with foldout collapsed - shows label with picker and header.
        /// </summary>
        [WInLineEditor(
            mode: WInLineEditorMode.FoldoutCollapsed,
            drawObjectField: false,
            drawHeader: true
        )]
        public InlineEditorTarget foldoutCollapsedCompact;

        /// <summary>
        /// Compact mode with foldout expanded - shows label with picker, header, and body.
        /// </summary>
        [WInLineEditor(
            mode: WInLineEditorMode.FoldoutExpanded,
            drawObjectField: false,
            drawHeader: true
        )]
        public InlineEditorTarget foldoutExpandedCompact;

        /// <summary>
        /// Compact mode always expanded - shows label with picker and body (no header).
        /// </summary>
        [WInLineEditor(
            mode: WInLineEditorMode.AlwaysExpanded,
            drawObjectField: false,
            drawHeader: false
        )]
        public InlineEditorTarget alwaysExpandedCompact;

        /// <summary>
        /// Compact mode always expanded with header - shows label with picker, header, and body.
        /// </summary>
        [WInLineEditor(
            mode: WInLineEditorMode.AlwaysExpanded,
            drawObjectField: false,
            drawHeader: true
        )]
        public InlineEditorTarget alwaysExpandedWithHeaderCompact;

        /// <summary>
        /// Compact mode that uses settings - behavior determined by UnityHelpersSettings.
        /// </summary>
        [WInLineEditor(
            mode: WInLineEditorMode.UseSettings,
            drawObjectField: false,
            drawHeader: true
        )]
        public InlineEditorTarget useSettingsCompact;
    }
}
#endif
