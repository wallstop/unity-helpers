// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.SharedEnums
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    /// <summary>
    /// Minimal two-option enum for edge case testing.
    /// </summary>
    public enum SmallTestEnum
    {
        OptionA,
        OptionB,
    }
#endif
}
