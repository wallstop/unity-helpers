// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.SharedEnums
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    /// <summary>
    /// Simple three-option enum for basic toggle button tests.
    /// </summary>
    public enum SimpleTestEnum
    {
        OptionA,
        OptionB,
        OptionC,
    }
#endif
}
