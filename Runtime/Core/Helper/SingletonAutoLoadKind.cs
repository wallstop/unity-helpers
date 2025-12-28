// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Helper
{
    /// <summary>
    /// Defines the singleton categories that can participate in auto-loading.
    /// </summary>
    public enum SingletonAutoLoadKind : byte
    {
        [System.Obsolete("Default uninitialized value - should never be used")]
        Unknown = 0,
        Runtime = 1,
        ScriptableObject = 2,
    }
}
