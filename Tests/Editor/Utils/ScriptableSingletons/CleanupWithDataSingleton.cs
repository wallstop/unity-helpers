// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

    [ScriptableSingletonPath("DuplicateCleanupTests")]
    [AllowDuplicateCleanup]
    internal sealed class CleanupWithDataSingleton
        : ScriptableObjectSingleton<CleanupWithDataSingleton>
    {
        [SerializeField]
        public int TestValue;

        [SerializeField]
        public string TestString;
    }
}
#endif
