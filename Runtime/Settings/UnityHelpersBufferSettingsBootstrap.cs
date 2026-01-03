// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Settings
{
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    /// <summary>
    /// Applies the configured buffer defaults automatically when the domain or player starts.
    /// </summary>
    internal static class UnityHelpersBufferSettingsBootstrap
    {
#if UNITY_EDITOR
        private static bool _editorAppliedThisDomain;

        [InitializeOnLoadMethod]
        private static void ApplyInEditor()
        {
            ApplyIfConfigured(fromEditorInit: true);
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void ApplyInRuntime()
        {
            ApplyIfConfigured(fromEditorInit: false);
        }

        private static void ApplyIfConfigured(bool fromEditorInit)
        {
#if UNITY_EDITOR
            if (fromEditorInit)
            {
                _editorAppliedThisDomain = true;
            }
            else if (_editorAppliedThisDomain)
            {
                // RuntimeInitializeOnLoadMethod also runs in the editor; avoid double applying.
                _editorAppliedThisDomain = false;
                return;
            }
#endif

            UnityHelpersBufferSettingsAsset asset = Resources.Load<UnityHelpersBufferSettingsAsset>(
                UnityHelpersBufferSettingsAsset.ResourcePath
            );

            if (asset == null || !asset.ApplyOnLoad)
            {
                return;
            }

            asset.ApplyToBuffers();
        }
    }
}
