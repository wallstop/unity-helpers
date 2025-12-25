#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

    [ExcludeFromSingletonCreation]
    internal sealed class MultipleInstancesSingleton
        : ScriptableObjectSingleton<MultipleInstancesSingleton>
    {
        public int instanceId;
    }
}
#endif
