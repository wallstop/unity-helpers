namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.InLineEditor
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WInLineEditor on SerializedScriptableObject.
    /// </summary>
    internal sealed class OdinInlineEditorScriptableObjectTarget : SerializedScriptableObject
    {
        [WInLineEditor]
        public OdinReferencedScriptableObject referencedObject;
    }
#endif
}
