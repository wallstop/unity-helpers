namespace WallstopStudios.UnityHelpers.Editor.DataVisualizer
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityHelpers.Core.DataVisualizer;

    public sealed class DataVisualizerModificationProcessor : AssetModificationProcessor
    {
        private static bool RefreshSignalThisSave;

        private static string[] OnWillSaveAssets(string[] paths)
        {
            bool needsRefresh = false;
            DataVisualizer openWindow = null;

            foreach (string path in paths)
            {
                if (!path.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                System.Type assetType = AssetDatabase.GetMainAssetTypeAtPath(path);

                if (assetType == null)
                {
                    continue;
                }

                bool isSettingsAsset = typeof(DataVisualizerSettings).IsAssignableFrom(assetType);
                bool isBDOAsset = typeof(BaseDataObject).IsAssignableFrom(assetType);

                if (isBDOAsset)
                { // Always refresh if a BaseDataObject is saved
                    needsRefresh = true;
                }
                else if (isSettingsAsset)
                {
                    // Only refresh from settings save IF the window is NOT using EditorPrefs
                    // (because if using EditorPrefs, saving settings file doesn't affect window state)
                    var settingsInstance = AssetDatabase.LoadAssetAtPath<DataVisualizerSettings>(
                        path
                    );
                    if (settingsInstance != null && !settingsInstance.UseEditorPrefsForState)
                    {
                        needsRefresh = true; // Settings file changed AND it's the active persistence method
                    }
                }

                if (needsRefresh)
                {
                    if (openWindow == null)
                    {
                        openWindow = EditorWindow.GetWindow<DataVisualizer>(false, null, false);
                    }

                    if (openWindow != null)
                    {
                        needsRefresh = true;
                        break;
                    }
                }
            }

            if (needsRefresh && openWindow != null && !RefreshSignalThisSave)
            {
                EditorApplication.delayCall += DataVisualizer.SignalRefresh;
                RefreshSignalThisSave = true;
                EditorApplication.delayCall += ResetSignalFlag;
            }
            return paths;
        }

        private static void ResetSignalFlag()
        {
            RefreshSignalThisSave = false;
        }
    }
#endif
}
