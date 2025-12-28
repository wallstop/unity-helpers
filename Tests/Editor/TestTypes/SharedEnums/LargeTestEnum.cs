// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.SharedEnums
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    /// <summary>
    /// Large ten-option enum for pagination and scrolling tests.
    /// </summary>
    public enum LargeTestEnum
    {
        Item1,
        Item2,
        Item3,
        Item4,
        Item5,
        Item6,
        Item7,
        Item8,
        Item9,
        Item10,
    }
#endif
}
