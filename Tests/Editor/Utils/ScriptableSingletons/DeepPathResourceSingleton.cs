#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

    [ExcludeFromSingletonCreation]
    [ScriptableSingletonPath("Deep/Nested/Singletons")]
    internal sealed class DeepPathResourceSingleton
        : ScriptableObjectSingleton<DeepPathResourceSingleton>
    {
        public string payload = "deep";
    }
}
#endif
