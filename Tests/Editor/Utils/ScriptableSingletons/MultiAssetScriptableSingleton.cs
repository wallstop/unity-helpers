#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Utils;

    internal sealed class MultiAssetScriptableSingleton
        : ScriptableObjectSingleton<MultiAssetScriptableSingleton>
    {
        public string id = "unset";
    }
}
#endif
