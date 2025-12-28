// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes.SharedEnums
{
#if UNITY_EDITOR && ODIN_INSPECTOR
    /// <summary>
    /// General-purpose test enum for read-only attribute tests.
    /// </summary>
    public enum OdinTestEnum
    {
        Value1,
        Value2,
        Value3,
    }
#endif
}
