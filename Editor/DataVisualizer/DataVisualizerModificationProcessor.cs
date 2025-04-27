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

                if (
                    typeof(BaseDataObject).IsAssignableFrom(assetType)
                    || typeof(DataVisualizerSettings).IsAssignableFrom(assetType)
                )
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
