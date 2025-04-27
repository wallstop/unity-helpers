namespace WallstopStudios.UnityHelpers.Editor.DataVisualizer
{
#if UNITY_EDITOR
    using System.IO; // For Path operations
    using UnityEditor;
    using UnityEngine;
    using UnityHelpers.Core.DataVisualizer; // Namespace of BaseDataObject
    using UnityHelpers.Editor; // Namespace of your DataVisualizer window

    public class DataVisualizerAssetProcessor : AssetPostprocessor
    {
        // This method is called by Unity after assets have been imported, deleted, or moved.
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
        {
            bool needsRefresh = false;

            // Check Deleted Assets
            foreach (string path in deletedAssets)
            {
                // Check if a BaseDataObject asset file might have been deleted
                if (path.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase))
                {
                    // We don't know the type for sure without loading, but any .asset deletion
                    // *could* be relevant. A simple check is often sufficient.
                    // More complex: check if path was inside known data folders.
                    Debug.Log($"[Asset Processor] Detected deletion: {path}");
                    needsRefresh = true;
                    break; // One relevant change is enough to trigger refresh
                }
                // Optional: Check if a directory relevant to settings was deleted
                // var settings = DataVisualizer.LoadOrCreateSettings(); // Need settings access
                // if (settings != null && path.Equals(settings.DataFolderPath, System.StringComparison.OrdinalIgnoreCase)) {
                //     needsRefresh = true;
                //     break;
                // }
            }

            // Check Imported/Updated Assets (includes newly created)
            if (!needsRefresh) // Only check if not already flagged
            {
                foreach (string path in importedAssets)
                {
                    // Check if it's an asset file that *is* a BaseDataObject
                    if (path.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase))
                    {
                        // Try loading to check the type
                        // Doing LoadAssetAtPath here can be slow if many assets change,
                        // but it's the most reliable way to check the type.
                        var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                        if (asset is BaseDataObject)
                        {
                            Debug.Log(
                                $"[Asset Processor] Detected import/update of BaseDataObject: {path}"
                            );
                            needsRefresh = true;
                            break;
                        }
                        // Also refresh if our settings file itself was imported/changed
                        if (asset is DataVisualizerSettings)
                        {
                            Debug.Log(
                                $"[Asset Processor] Detected import/update of Settings: {path}"
                            );
                            needsRefresh = true;
                            break;
                        }
                    }
                }
            }

            // Check Moved Assets (Renaming is treated as a move)
            if (!needsRefresh) // Only check if not already flagged
            {
                for (int i = 0; i < movedAssets.Length; i++)
                {
                    string newPath = movedAssets[i];
                    string oldPath = movedFromAssetPaths[i];

                    // Check if a BaseDataObject asset file might have been moved/renamed
                    if (
                        newPath.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase)
                        || oldPath.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        // Similar to imports, we might need to load to confirm type if unsure.
                        // If the type doesn't change on move, just checking extension might be enough.
                        // Let's assume any .asset move could be relevant for simplicity.
                        Debug.Log(
                            $"[Asset Processor] Detected move/rename: {oldPath} -> {newPath}"
                        );
                        needsRefresh = true;
                        break;
                    }
                    // Optional: Check if a directory relevant to settings was moved/renamed
                }
            }

            // --- If any relevant change occurred, signal the window ---
            if (needsRefresh)
            {
                // Use delayCall to ensure this runs on the main thread after post-processing
                EditorApplication.delayCall += DataVisualizer.SignalRefresh;
                Debug.Log("[Asset Processor] Refresh signal scheduled.");
            }
        }
    }
#endif
}
