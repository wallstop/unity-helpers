// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Editor.AssetProcessors
{
    using System;
    using UnityEditor;

    internal sealed class LlmArtifactCleaner : AssetPostprocessor
    {
        private const string PackagePathPrefix = "Packages/com.wallstop-studios.unity-helpers/";
        private const string LlmPrefix = "_llm_";

        private static readonly string[] BlockedSegments = { LlmPrefix };

        private static bool _isDeleting;

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
        {
            DeleteBlockedAssets(importedAssets);
            DeleteBlockedAssets(movedAssets);
        }

        internal static void DeleteBlockedAssets(string[] assetPaths)
        {
            if (assetPaths == null || assetPaths.Length == 0)
            {
                return;
            }

            if (_isDeleting)
            {
                return;
            }

            for (int i = 0; i < assetPaths.Length; i++)
            {
                string assetPath = assetPaths[i];
                if (!ShouldDelete(assetPath))
                {
                    continue;
                }

                DeleteAsset(assetPath);
            }
        }

        internal static bool ShouldDelete(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            if (!assetPath.StartsWith(PackagePathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            for (int i = 0; i < BlockedSegments.Length; i++)
            {
                string segment = BlockedSegments[i];
                int index = assetPath.IndexOf(segment, StringComparison.Ordinal);
                while (index >= 0)
                {
                    // Check that _llm_ is not preceded by an underscore (to avoid matching __llm__)
                    bool validPrefix = index == 0 || assetPath[index - 1] != '_';
                    // Check that _llm_ is not followed by an additional underscore immediately after
                    int afterIndex = index + segment.Length;
                    bool validSuffix =
                        afterIndex >= assetPath.Length || assetPath[afterIndex] != '_';

                    if (validPrefix && validSuffix)
                    {
                        return true;
                    }

                    // Continue searching for next occurrence
                    index = assetPath.IndexOf(segment, index + 1, StringComparison.Ordinal);
                }
            }

            return false;
        }

        private static void DeleteAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            if (_isDeleting)
            {
                return;
            }

            _isDeleting = true;
            try
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
            finally
            {
                _isDeleting = false;
            }
        }
    }
}
