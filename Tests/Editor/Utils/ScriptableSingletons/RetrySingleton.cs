#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

    [ScriptableSingletonPath("CreatorTests/Retry")]
    internal sealed class RetrySingleton : ScriptableObjectSingleton<RetrySingleton> { }
}
#endif
