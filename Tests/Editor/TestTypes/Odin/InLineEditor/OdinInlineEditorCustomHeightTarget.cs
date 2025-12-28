// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.InLineEditor
{
#if UNITY_EDITOR && ODIN_INSPECTOR

    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for WInLineEditor with custom inspector height.
    /// </summary>
    internal sealed class OdinInlineEditorCustomHeightTarget : SerializedScriptableObject
    {
        [WInLineEditor(inspectorHeight: 300f)]
        public OdinReferencedScriptableObject customHeightReference;
    }

#endif
}
