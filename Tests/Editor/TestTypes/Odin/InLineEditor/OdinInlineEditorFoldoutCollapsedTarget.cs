namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.InLineEditor
{
#if UNITY_EDITOR && ODIN_INSPECTOR

    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WInLineEditor with FoldoutCollapsed mode.
    /// </summary>
    internal sealed class OdinInlineEditorFoldoutCollapsedTarget : SerializedScriptableObject
    {
        [WInLineEditor(WInLineEditorMode.FoldoutCollapsed)]
        public OdinReferencedScriptableObject foldoutCollapsedReference;
    }

#endif
}
