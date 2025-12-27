namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.InLineEditor
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WInLineEditor with nested inline editors.
    /// </summary>
    internal sealed class OdinInlineEditorNestedTarget : SerializedScriptableObject
    {
        [WInLineEditor]
        public OdinInlineEditorScriptableObjectTarget nestedReference;
    }
#endif
}
