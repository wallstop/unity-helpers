#if UNITY_INCLUDE_TESTS
namespace WallstopStudios.UnityHelpers.Tests.Editor.CustomDrawers
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Host ScriptableObject with default settings inline editor (UseSettings mode) for testing.
    /// </summary>
    internal sealed class DefaultSettingsInlineEditorHost : ScriptableObject
    {
        [WInLineEditor]
        public InlineEditorTarget collapsedTarget;
    }
}
#endif
