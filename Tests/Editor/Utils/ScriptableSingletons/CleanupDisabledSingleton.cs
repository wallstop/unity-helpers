#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

    [ScriptableSingletonPath("DuplicateCleanupTests")]
    internal sealed class CleanupDisabledSingleton
        : ScriptableObjectSingleton<CleanupDisabledSingleton> { }
}
#endif
