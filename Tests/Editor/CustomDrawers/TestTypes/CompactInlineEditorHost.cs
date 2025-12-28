// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Host ScriptableObject with compact inline editor (no object field) for testing.
    /// The compact mode shows just a label with a small picker button, not a full object field.
    /// </summary>
    internal sealed class CompactInlineEditorHost : ScriptableObject
    {
        [WInLineEditor(
            mode: WInLineEditorMode.FoldoutCollapsed,
            drawObjectField: false,
            drawHeader: true
        )]
        public InlineEditorTarget compactTarget;
    }
}
#endif
