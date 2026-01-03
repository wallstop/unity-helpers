// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

    [ExcludeFromSingletonCreation]
    [ScriptableSingletonPath("Missing/Subfolder")]
    internal sealed class WrongPathFallbackSingleton
        : ScriptableObjectSingleton<WrongPathFallbackSingleton>
    {
        public string payload = "fallback";
    }
}
#endif
