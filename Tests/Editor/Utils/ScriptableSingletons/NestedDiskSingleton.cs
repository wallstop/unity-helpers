#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

    [ScriptableSingletonPath("Tests/Nested/DeepPath")]
    internal sealed class NestedDiskSingleton : ScriptableObjectSingleton<NestedDiskSingleton> { }
}
#endif
