namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.InLineEditor
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WInLineEditor with AlwaysExpanded mode.
    /// </summary>
    internal sealed class OdinInlineEditorAlwaysExpandedTarget : SerializedScriptableObject
    {
        [WInLineEditor(WInLineEditorMode.AlwaysExpanded)]
        public OdinReferencedScriptableObject alwaysExpandedReference;
    }
#endif
}
