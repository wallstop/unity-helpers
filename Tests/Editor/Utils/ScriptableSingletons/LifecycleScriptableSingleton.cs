// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

    [ExcludeFromSingletonCreation]
    internal sealed class LifecycleScriptableSingleton
        : ScriptableObjectSingleton<LifecycleScriptableSingleton>
    {
        public static int ClearedCount;

        protected override void OnInstanceCleared()
        {
            base.OnInstanceCleared();
            ClearedCount++;
        }
    }
}
#endif
