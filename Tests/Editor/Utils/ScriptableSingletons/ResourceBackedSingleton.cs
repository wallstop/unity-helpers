#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Utils;

    internal sealed class ResourceBackedSingleton : ScriptableObjectSingleton<ResourceBackedSingleton>
    {
        public string payload = "resource";
    }
}
#endif
