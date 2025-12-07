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
