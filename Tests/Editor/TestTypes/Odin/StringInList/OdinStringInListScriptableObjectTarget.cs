// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.StringInList
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for StringInList attribute on SerializedScriptableObject with inline string options.
    /// </summary>
    internal sealed class OdinStringInListScriptableObjectTarget : SerializedScriptableObject
    {
        [StringInList("Easy", "Normal", "Hard")]
        public string selectedDifficulty;
    }
#endif
}
