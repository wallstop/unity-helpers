#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Utils
{
    using WallstopStudios.UnityHelpers.Utils;

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
