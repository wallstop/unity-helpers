#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

    [ScriptableSingletonPath("Tests/CreatorPath")]
    internal sealed class CreatorPathSingleton : ScriptableObjectSingleton<CreatorPathSingleton> { }
}
#endif
