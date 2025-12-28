// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Runtime.TestTypes.Odin.EnumToggleButtons
{
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Tests.Runtime.TestTypes.SharedEnums;

    /// <summary>
    /// Test target for WEnumToggleButtons on SerializedMonoBehaviour with both regular and flags enums.
    /// </summary>
    public sealed class OdinEnumToggleButtonsMonoBehaviour : SerializedMonoBehaviour
    {
        [WEnumToggleButtons]
        public SimpleTestEnum enumValue;

        [WEnumToggleButtons]
        public TestFlagsEnum flags;
    }
#endif
}
