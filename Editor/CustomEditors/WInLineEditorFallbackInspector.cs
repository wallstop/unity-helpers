namespace WallstopStudios.UnityHelpers.Editor.CustomEditors
{
#if UNITY_EDITOR
    using UnityEditor;

    /// <summary>
    /// Default inspector applied when no higher-priority custom inspector exists.
    /// Ensures WInLineEditor fields receive custom full-width rendering.
    /// </summary>
    [CustomEditor(typeof(UnityEngine.Object), editorForChildClasses: true)]
    [CanEditMultipleObjects]
    internal sealed class WInLineEditorFallbackInspector : WInLineEditorInspectorBase { }
#endif
}
