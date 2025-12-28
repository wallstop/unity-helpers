// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.Odin.EnumToggleButtons
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.SharedEnums;

    /// <summary>
    /// Test target for WEnumToggleButtons with regular (non-flags) enum on SerializedScriptableObject.
    /// </summary>
    internal sealed class OdinEnumToggleButtonsRegularTarget : SerializedScriptableObject
    {
        [WEnumToggleButtons]
        public SimpleTestEnum enumValue;
    }
#endif
}
