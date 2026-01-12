// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Host ScriptableObject for testing compact mode with custom inspector heights.
    /// </summary>
    internal sealed class CompactCustomHeightHost : ScriptableObject
    {
        /// <summary>
        /// Compact mode with custom height of 180.
        /// </summary>
        [WInLineEditor(
            mode: WInLineEditorMode.AlwaysExpanded,
            inspectorHeight: 180f,
            drawObjectField: false,
            drawHeader: false
        )]
        public InlineEditorTarget fixedHeightCompact;

        /// <summary>
        /// Compact mode with custom height and preview enabled.
        /// </summary>
        [WInLineEditor(
            mode: WInLineEditorMode.AlwaysExpanded,
            inspectorHeight: 200f,
            drawObjectField: false,
            drawHeader: false,
            drawPreview: true,
            previewHeight: 64f
        )]
        public InlineEditorTarget compactWithPreview;

        /// <summary>
        /// Compact mode with custom height and scrolling disabled.
        /// </summary>
        [WInLineEditor(
            mode: WInLineEditorMode.AlwaysExpanded,
            inspectorHeight: 180f,
            drawObjectField: false,
            drawHeader: false,
            enableScrolling: false
        )]
        public InlineEditorTarget compactNoScroll;
    }
}
#endif
