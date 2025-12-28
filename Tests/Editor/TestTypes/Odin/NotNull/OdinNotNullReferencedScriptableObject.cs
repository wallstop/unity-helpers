// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.NotNull
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;

    /// <summary>
    /// A referenced ScriptableObject with string data for WNotNull tests with Odin Inspector.
    /// </summary>
    internal sealed class OdinNotNullReferencedScriptableObject : SerializedScriptableObject
    {
        public string data;
    }
#endif
}
