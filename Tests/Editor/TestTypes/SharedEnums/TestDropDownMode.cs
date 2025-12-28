// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.SharedEnums
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    /// <summary>
    /// Test enum for value dropdown mode selection tests.
    /// </summary>
    public enum TestDropDownMode
    {
        ModeA = 0,
        ModeB = 1,
        ModeC = 2,
    }
#endif
}
