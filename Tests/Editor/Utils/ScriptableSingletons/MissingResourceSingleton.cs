#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Utils;

    internal sealed class MissingResourceSingleton
        : ScriptableObjectSingleton<MissingResourceSingleton>
    {
        public string note = "missing";
    }
}
#endif
