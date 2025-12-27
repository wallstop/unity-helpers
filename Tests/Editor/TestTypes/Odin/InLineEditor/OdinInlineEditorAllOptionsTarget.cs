namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.InLineEditor
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WInLineEditor with all options combined.
    /// </summary>
    internal sealed class OdinInlineEditorAllOptionsTarget : SerializedScriptableObject
    {
        [WInLineEditor(
            mode: WInLineEditorMode.FoldoutExpanded,
            inspectorHeight: 250f,
            drawPreview: true,
            previewHeight: 96f,
            drawObjectField: true,
            drawHeader: true,
            enableScrolling: true
        )]
        public OdinReferencedScriptableObject allOptionsReference;
    }
#endif
}
