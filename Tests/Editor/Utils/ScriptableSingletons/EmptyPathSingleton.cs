#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Utils;

    internal sealed class EmptyPathSingleton : ScriptableObjectSingleton<EmptyPathSingleton>
    {
        public bool flag = true;
    }
}
#endif
