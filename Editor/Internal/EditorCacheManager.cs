namespace WallstopStudios.UnityHelpers.Editor.Internal
{
#if UNITY_EDITOR
    using UnityEditor;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Editor.Extensions;
    using WallstopStudios.UnityHelpers.Editor.Utils.WButton;
    using WallstopStudios.UnityHelpers.Editor.Utils.WGroup;

    [InitializeOnLoad]
    internal static class EditorCacheManager
    {
        static EditorCacheManager()
        {
            ClearAllCaches();
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            ClearAllCaches();
        }

        internal static void ClearAllCaches()
        {
            WGroupLayoutBuilder.ClearCache();
            WButtonGUI.ClearContextCache();
            SerializedPropertyExtensions.ClearCache();
            WEnumToggleButtonsDrawer.ClearCache();
        }
    }
#endif
}
