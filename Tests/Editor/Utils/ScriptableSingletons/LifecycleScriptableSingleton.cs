#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Utils;

    [ExcludeFromSingletonCreation]
    internal sealed class LifecycleScriptableSingleton
        : ScriptableObjectSingleton<LifecycleScriptableSingleton>
    {
        public static int DisableCount;

        private void OnDisable()
        {
            DisableCount++;
        }
    }
}
#endif
