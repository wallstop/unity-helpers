namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.InLineEditor
{
#if UNITY_EDITOR && ODIN_INSPECTOR

    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WInLineEditor with object field disabled.
    /// </summary>
    internal sealed class OdinInlineEditorNoObjectFieldTarget : SerializedScriptableObject
    {
        [WInLineEditor(drawObjectField: false)]
        public OdinReferencedScriptableObject noObjectFieldReference;
    }

#endif
}
