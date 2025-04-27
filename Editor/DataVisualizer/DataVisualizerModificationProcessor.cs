namespace WallstopStudios.UnityHelpers.Editor.DataVisualizer
{
    using UnityEditor;
    using UnityEngine;
    using UnityHelpers.Core.DataVisualizer; // Your namespace for BaseDataObject
    using UnityHelpers.Editor; // Your namespace for DataVisualizer & Settings

    // This script hooks into the asset saving process
    public class DataVisualizerModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        // Flag to prevent scheduling multiple refreshes during a single save operation
        private static bool s_refreshSignaledThisSave = false;

        // This method is called by Unity before assets are saved to disk.
        // It receives an array of asset paths that are about to be saved.
        static string[] OnWillSaveAssets(string[] paths)
        {
            bool needsRefresh = false;
            DataVisualizer openWindow = null; // Cache the window lookup

            foreach (string path in paths)
            {
                // Quick check: only interested in .asset files for our ScriptableObjects
                if (path.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase))
                {
                    // Check the type without fully loading if possible using GetMainAssetTypeAtPath
                    System.Type assetType = AssetDatabase.GetMainAssetTypeAtPath(path);

                    if (assetType != null)
                    {
                        // Check if the saved asset is a BaseDataObject or our Settings
                        if (
                            typeof(BaseDataObject).IsAssignableFrom(assetType)
                            || typeof(DataVisualizerSettings).IsAssignableFrom(assetType)
                        )
                        {
                            // Only bother signaling if the window is actually open
                            if (openWindow == null)
                            {
                                openWindow = EditorWindow.GetWindow<DataVisualizer>(
                                    false,
                                    null,
                                    false
                                );
                            }

                            if (openWindow != null)
                            {
                                Debug.Log(
                                    $"[Modification Processor] Detected save relevant to DataVisualizer: {path}"
                                );
                                needsRefresh = true;
                                break; // One relevant asset is enough to trigger refresh
                            }
                        }
                    }
                }
            }

            // If a relevant asset was saved AND the window is open AND we haven't already signaled...
            if (needsRefresh && openWindow != null && !s_refreshSignaledThisSave)
            {
                // Schedule the refresh to happen *after* the save operation completes
                // using delayCall ensures it runs on the main thread's next update cycle.
                EditorApplication.delayCall += DataVisualizer.SignalRefresh;
                s_refreshSignaledThisSave = true; // Mark that we've signaled for this save event

                // Schedule another delegate to reset the flag shortly after, allowing
                // subsequent unrelated save operations to trigger a refresh again.
                EditorApplication.delayCall += ResetSignalFlag;

                Debug.Log(
                    "[Modification Processor] DataVisualizer refresh signal scheduled via delayCall."
                );
            }

            // IMPORTANT: Must return the original paths array (or a modified one if you want to prevent saving)
            return paths;
        }

        // Resets the flag after the delayCall queue has likely been processed for the current save
        private static void ResetSignalFlag()
        {
            s_refreshSignaledThisSave = false;
            // Debug.Log("[Modification Processor] Refresh signal flag reset.");
        }
    }
}
