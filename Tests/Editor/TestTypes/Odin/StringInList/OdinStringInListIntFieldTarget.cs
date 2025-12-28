// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.StringInList
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target for StringInList attribute with int field type (index selection).
    /// </summary>
    internal sealed class OdinStringInListIntFieldTarget : SerializedScriptableObject
    {
        [StringInList("First", "Second", "Third", "Fourth")]
        public int selectedIndex;
    }
#endif
}
