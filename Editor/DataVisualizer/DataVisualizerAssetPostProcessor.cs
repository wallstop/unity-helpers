namespace WallstopStudios.UnityHelpers.Editor.DataVisualizer
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;
    using UnityHelpers.Core.DataVisualizer;

    public sealed class DataVisualizerAssetProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
        {
            bool needsRefresh = false;

            foreach (string path in deletedAssets)
            {
                if (!path.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                needsRefresh = true;
                break;
            }

            if (!needsRefresh)
            {
                foreach (string path in importedAssets)
                {
                    if (!path.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    if (asset is BaseDataObject or DataVisualizerSettings)
                    {
                        needsRefresh = true;
                        break;
                    }
                }
            }

            if (!needsRefresh)
            {
                for (int i = 0; i < movedAssets.Length; i++)
                {
                    string newPath = movedAssets[i];
                    string oldPath = movedFromAssetPaths[i];

                    if (
                        newPath.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase)
                        || oldPath.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        needsRefresh = true;
                        break;
                    }
                }
            }

            if (needsRefresh)
            {
                EditorApplication.delayCall += DataVisualizer.SignalRefresh;
            }
        }
    }
#endif
}
