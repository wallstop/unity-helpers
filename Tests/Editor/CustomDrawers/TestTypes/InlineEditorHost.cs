// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Host ScriptableObject with a foldout-collapsed inline editor field for testing.
    /// </summary>
    internal sealed class InlineEditorHost : ScriptableObject
    {
        [WInLineEditor(WInLineEditorMode.FoldoutCollapsed)]
        public InlineEditorTarget collapsedTarget;
    }
}
#endif
