#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

    [ExcludeFromSingletonCreation]
    internal sealed class EmptyPathSingleton : ScriptableObjectSingleton<EmptyPathSingleton>
    {
        public bool flag = true;
    }
}
#endif
