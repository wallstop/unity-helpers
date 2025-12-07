#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

    [ExcludeFromSingletonCreation]
    [ScriptableSingletonPath("CustomPath")]
    internal sealed class CustomPathSingleton : ScriptableObjectSingleton<CustomPathSingleton>
    {
        public string customData = "test";
    }
}
#endif
