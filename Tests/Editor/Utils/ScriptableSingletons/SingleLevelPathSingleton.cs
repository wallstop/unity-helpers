#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

    [ExcludeFromSingletonCreation]
    [ScriptableSingletonPath("SingleLevel")]
    internal sealed class SingleLevelPathSingleton
        : ScriptableObjectSingleton<SingleLevelPathSingleton>
    {
        public bool flag = true;
    }
}
#endif
