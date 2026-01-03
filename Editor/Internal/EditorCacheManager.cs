// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Editor.Internal
{
#if UNITY_EDITOR
    using UnityEditor;
    using WallstopStudios.UnityHelpers.Editor.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers;
    using WallstopStudios.UnityHelpers.Editor.CustomDrawers.Utils;
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

        /// <summary>
        /// Clears all editor caches. This method is called automatically on domain reload
        /// via <see cref="InitializeOnLoadAttribute"/> and <see cref="InitializeOnLoadMethodAttribute"/>
        /// to ensure all cached state is properly reset when Unity reloads the domain
        /// (after script compilation, entering/exiting play mode, etc.).
        /// </summary>
        internal static void ClearAllCaches()
        {
            EditorCacheHelper.ClearAllCaches();
            WGroupLayoutBuilder.ClearCache();
            WButtonGUI.ClearContextCache();
            SerializedPropertyExtensions.ClearCache();
            WEnumToggleButtonsDrawer.ClearCache();
            WShowIfPropertyDrawer.ClearCache();
            InLineEditorShared.ClearCache();
#if ODIN_INSPECTOR
            WEnumToggleButtonsOdinDrawer.ClearCache();
            WShowIfOdinDrawer.ClearCache();
            IntDropDownOdinDrawer.ClearCache();
#endif
        }
    }
#endif
}
