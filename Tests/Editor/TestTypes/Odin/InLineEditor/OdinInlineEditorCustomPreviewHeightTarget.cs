namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.InLineEditor
{
#if UNITY_EDITOR && ODIN_INSPECTOR

    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WInLineEditor with custom preview height.
    /// </summary>
    internal sealed class OdinInlineEditorCustomPreviewHeightTarget : SerializedScriptableObject
    {
        [WInLineEditor(drawPreview: true, previewHeight: 128f)]
        public OdinReferencedScriptableObject customPreviewHeightReference;
    }

#endif
}
