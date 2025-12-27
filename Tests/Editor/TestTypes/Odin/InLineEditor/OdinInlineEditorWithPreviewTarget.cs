namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.InLineEditor
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WInLineEditor with preview enabled.
    /// </summary>
    internal sealed class OdinInlineEditorWithPreviewTarget : SerializedScriptableObject
    {
        [WInLineEditor(drawPreview: true)]
        public OdinReferencedScriptableObject previewReference;
    }
#endif
}
