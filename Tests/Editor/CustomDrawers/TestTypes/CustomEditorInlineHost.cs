#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Host ScriptableObject with an inline editor for a target that has a custom editor.
    /// </summary>
    internal sealed class CustomEditorInlineHost : ScriptableObject
    {
        [WInLineEditor(WInLineEditorMode.FoldoutCollapsed)]
        public SimpleCustomEditorTarget customTarget;
    }
}
#endif
