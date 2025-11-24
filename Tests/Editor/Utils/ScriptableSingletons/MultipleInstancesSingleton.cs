#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Utils;

    internal sealed class MultipleInstancesSingleton
        : ScriptableObjectSingleton<MultipleInstancesSingleton>
    {
        public int instanceId;
    }
}
#endif
