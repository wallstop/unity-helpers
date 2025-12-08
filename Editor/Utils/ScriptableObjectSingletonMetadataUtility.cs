namespace WallstopStudios.UnityHelpers.Editor.Utils
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Utils;
    using Object = UnityEngine.Object;

    internal static class ScriptableObjectSingletonMetadataUtility
    {
        internal static ScriptableObjectSingletonMetadata LoadOrCreateMetadataAsset()
        {
            // Try loading from current path first
            ScriptableObjectSingletonMetadata metadata =
                AssetDatabase.LoadAssetAtPath<ScriptableObjectSingletonMetadata>(
                    ScriptableObjectSingletonMetadata.AssetPath
                );
            if (metadata != null)
            {
                return metadata;
            }

            // Check for legacy path and migrate if found
            ScriptableObjectSingletonMetadata legacyMetadata =
                AssetDatabase.LoadAssetAtPath<ScriptableObjectSingletonMetadata>(
                    ScriptableObjectSingletonMetadata.LegacyAssetPath
                );
            if (legacyMetadata != null)
            {
                // Skip migration during test runs to avoid Unity's internal modal dialogs
                // unless explicitly allowed
                if (
                    EditorUi.Suppress
                    && !ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression
                )
                {
                    return legacyMetadata;
                }
                return MigrateLegacyMetadata(legacyMetadata);
            }

            // Skip creating new assets during test runs to avoid Unity's internal modal dialogs
            // when asset operations fail, unless explicitly allowed.
            if (
                EditorUi.Suppress
                && !ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression
            )
            {
                return null;
            }

            // Create new asset at current path
            if (!EnsureResourcesFolder())
            {
                Debug.LogWarning(
                    "ScriptableObjectSingletonMetadataUtility: Could not ensure Resources folder exists. Skipping metadata asset creation."
                );
                return null;
            }

            ScriptableObjectSingletonMetadata created =
                ScriptableObject.CreateInstance<ScriptableObjectSingletonMetadata>();
            bool editingInterrupted = TryStopAssetEditing();
            try
            {
                AssetDatabase.CreateAsset(created, ScriptableObjectSingletonMetadata.AssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(ScriptableObjectSingletonMetadata.AssetPath);
                return created;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"ScriptableObjectSingletonMetadataUtility: Failed to create metadata asset: {ex.Message}"
                );
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
                return null;
            }
            finally
            {
                if (editingInterrupted)
                {
                    AssetDatabase.StartAssetEditing();
                }
            }
        }

        private static ScriptableObjectSingletonMetadata MigrateLegacyMetadata(
            ScriptableObjectSingletonMetadata legacyMetadata
        )
        {
            bool editingInterrupted = TryStopAssetEditing();
            try
            {
                if (!EnsureResourcesFolder())
                {
                    Debug.LogWarning(
                        "ScriptableObjectSingletonMetadataUtility: Could not ensure Resources folder exists. Keeping legacy metadata asset."
                    );
                    return legacyMetadata;
                }

                string legacyPath = ScriptableObjectSingletonMetadata.LegacyAssetPath;
                string targetPath = ScriptableObjectSingletonMetadata.AssetPath;

                string moveResult = AssetDatabase.MoveAsset(legacyPath, targetPath);
                if (string.IsNullOrEmpty(moveResult))
                {
                    // Move succeeded - try to delete empty parent folders
                    TryDeleteEmptyParentFolders(legacyPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    return AssetDatabase.LoadAssetAtPath<ScriptableObjectSingletonMetadata>(
                        targetPath
                    );
                }

                // Move failed - create new and copy data
                Debug.LogWarning(
                    $"Failed to move ScriptableObjectSingletonMetadata from {legacyPath} to {targetPath}: {moveResult}. Creating new asset."
                );

                ScriptableObjectSingletonMetadata created =
                    ScriptableObject.CreateInstance<ScriptableObjectSingletonMetadata>();
                try
                {
                    AssetDatabase.CreateAsset(created, targetPath);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"ScriptableObjectSingletonMetadataUtility: Failed to create new metadata asset during migration: {ex.Message}. Keeping legacy metadata."
                    );
                    if (created != null)
                    {
                        Object.DestroyImmediate(created);
                    }
                    return legacyMetadata;
                }

                // Delete legacy asset
                if (AssetDatabase.DeleteAsset(legacyPath))
                {
                    TryDeleteEmptyParentFolders(legacyPath);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return created;
            }
            finally
            {
                if (editingInterrupted)
                {
                    AssetDatabase.StartAssetEditing();
                }
            }
        }

        private static void TryDeleteEmptyParentFolders(string assetPath)
        {
            try
            {
                string folder = Path.GetDirectoryName(assetPath);
                if (string.IsNullOrWhiteSpace(folder))
                {
                    return;
                }

                folder = folder.SanitizePath();
                while (
                    !string.IsNullOrWhiteSpace(folder)
                    && !string.Equals(folder, "Assets", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(
                        folder,
                        "Assets/Resources",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    if (!AssetDatabase.IsValidFolder(folder))
                    {
                        folder = Path.GetDirectoryName(folder);
                        if (folder != null)
                        {
                            folder = folder.SanitizePath();
                        }
                        continue;
                    }

                    string[] contents = AssetDatabase.FindAssets(string.Empty, new[] { folder });
                    if (contents == null || contents.Length == 0)
                    {
                        if (AssetDatabase.DeleteAsset(folder))
                        {
                            string parent = Path.GetDirectoryName(folder);
                            if (parent != null)
                            {
                                folder = parent.SanitizePath();
                            }
                            else
                            {
                                break;
                            }
                            continue;
                        }
                    }
                    break;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to clean up empty folders after migration: {e.Message}");
            }
        }

        internal static void UpdateEntry(
            Type type,
            string resourcesLoadPath,
            string resourcesPath,
            string assetGuid
        )
        {
            // Skip creating new metadata asset during test runs to avoid Unity's internal modal dialogs
            // when asset operations fail, unless explicitly allowed. Only update if the asset already exists.
            if (
                EditorUi.Suppress
                && !ScriptableObjectSingletonCreator.AllowAssetCreationDuringSuppression
            )
            {
                ScriptableObjectSingletonMetadata existing =
                    AssetDatabase.LoadAssetAtPath<ScriptableObjectSingletonMetadata>(
                        ScriptableObjectSingletonMetadata.AssetPath
                    );
                if (existing == null)
                {
                    // Also check legacy path
                    existing = AssetDatabase.LoadAssetAtPath<ScriptableObjectSingletonMetadata>(
                        ScriptableObjectSingletonMetadata.LegacyAssetPath
                    );
                }
                if (existing == null)
                {
                    return;
                }
            }

            ScriptableObjectSingletonMetadata metadata = LoadOrCreateMetadataAsset();
            if (metadata == null)
            {
                return;
            }

            ScriptableObjectSingletonMetadata.Entry entry = new()
            {
                assemblyQualifiedTypeName = type.AssemblyQualifiedName,
                resourcesLoadPath = resourcesLoadPath,
                resourcesPath = resourcesPath,
                assetGuid = assetGuid,
            };
            metadata.SetOrUpdateEntry(entry);
            EditorUtility.SetDirty(metadata);
        }

        /// <summary>
        /// Removes metadata entries that point to non-existent assets.
        /// This cleans up stale entries that may have been left behind when assets were deleted.
        /// </summary>
        /// <returns>The number of stale entries removed.</returns>
        internal static int CleanupStaleEntries()
        {
            ScriptableObjectSingletonMetadata metadata =
                AssetDatabase.LoadAssetAtPath<ScriptableObjectSingletonMetadata>(
                    ScriptableObjectSingletonMetadata.AssetPath
                );
            if (metadata == null)
            {
                // Also check legacy path
                metadata = AssetDatabase.LoadAssetAtPath<ScriptableObjectSingletonMetadata>(
                    ScriptableObjectSingletonMetadata.LegacyAssetPath
                );
            }

            if (metadata == null)
            {
                return 0;
            }

            IReadOnlyList<ScriptableObjectSingletonMetadata.Entry> entries =
                metadata.GetAllEntries();
            if (entries == null || entries.Count == 0)
            {
                return 0;
            }

            using PooledResource<List<string>> staleEntryResource = Buffers<string>.List.Get(
                out List<string> staleEntries
            );
            foreach (ScriptableObjectSingletonMetadata.Entry entry in entries)
            {
                if (string.IsNullOrEmpty(entry.resourcesLoadPath))
                {
                    continue;
                }

                // Check if the asset actually exists
                string assetPath = $"Assets/Resources/{entry.resourcesLoadPath}.asset";
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                if (asset == null)
                {
                    // Also verify by GUID if available
                    if (!string.IsNullOrEmpty(entry.assetGuid))
                    {
                        string guidPath = AssetDatabase.GUIDToAssetPath(entry.assetGuid);
                        if (!string.IsNullOrEmpty(guidPath))
                        {
                            asset = AssetDatabase.LoadAssetAtPath<Object>(guidPath);
                        }
                    }

                    if (asset == null)
                    {
                        staleEntries.Add(entry.assemblyQualifiedTypeName);
                    }
                }
            }

            if (staleEntries.Count == 0)
            {
                return 0;
            }

            foreach (string typeName in staleEntries)
            {
                metadata.RemoveEntry(typeName);
            }

            EditorUtility.SetDirty(metadata);
            return staleEntries.Count;
        }

        private static bool EnsureResourcesFolder()
        {
            string assetPath = ScriptableObjectSingletonMetadata.AssetPath;
            string directory = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(directory))
            {
                return false;
            }

            directory = directory.SanitizePath();

            // First, ensure the folder exists on disk. This prevents Unity's internal
            // "Moving file failed" modal dialog when CreateAsset tries to move a temp file
            // to a destination folder that doesn't exist.
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (!string.IsNullOrEmpty(projectRoot))
            {
                string absoluteDirectory = Path.Combine(projectRoot, directory);
                try
                {
                    if (!Directory.Exists(absoluteDirectory))
                    {
                        Directory.CreateDirectory(absoluteDirectory);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"ScriptableObjectSingletonMetadataUtility: Failed to create directory on disk '{absoluteDirectory}': {ex.Message}"
                    );
                    return false;
                }
            }

            if (AssetDatabase.IsValidFolder(directory))
            {
                return true;
            }

            // Stop any asset editing batch to ensure folder creation is immediate
            bool wasEditing = TryStopAssetEditing();

            try
            {
                string[] segments = directory.Split('/', StringSplitOptions.RemoveEmptyEntries);
                string current = segments[0];
                for (int i = 1; i < segments.Length; i++)
                {
                    string next = $"{current}/{segments[i]}";
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        string result = AssetDatabase.CreateFolder(current, segments[i]);
                        if (string.IsNullOrEmpty(result))
                        {
                            Debug.LogError(
                                $"ScriptableObjectSingletonMetadataUtility: Failed to create folder '{next}'"
                            );
                            return false;
                        }
                    }
                    current = next;
                }

                // Note: Do NOT call AssetDatabase.Refresh here - it causes issues during
                // Unity initialization and can trigger "Unable to import newly created asset" errors.
                // The folders are immediately available after CreateFolder.
                return true;
            }
            finally
            {
                if (wasEditing)
                {
                    AssetDatabase.StartAssetEditing();
                }
            }
        }

        /// <summary>
        /// Internal counter to track StartAssetEditing calls.
        /// Unity's AssetDatabase doesn't expose this, so we track it ourselves.
        /// </summary>
        private static int _assetEditingDepth = 0;

        /// <summary>
        /// Tries to stop asset editing if we're in a batch operation.
        /// Returns true if we were editing and successfully stopped.
        /// </summary>
        /// <remarks>
        /// Unity's StopAssetEditing will throw an assertion error if called without
        /// a corresponding StartAssetEditing. We avoid calling it unconditionally
        /// to prevent these assertion errors.
        /// </remarks>
        private static bool TryStopAssetEditing()
        {
            // Only try to stop if we know we started editing
            if (_assetEditingDepth <= 0)
            {
                return false;
            }

            try
            {
                AssetDatabase.StopAssetEditing();
                _assetEditingDepth--;
                return true;
            }
            catch (InvalidOperationException)
            {
                // Reset depth if Unity says we're not in a batch
                _assetEditingDepth = 0;
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Starts asset editing batch operation and tracks the depth.
        /// </summary>
        internal static void StartAssetEditingTracked()
        {
            AssetDatabase.StartAssetEditing();
            _assetEditingDepth++;
        }

        /// <summary>
        /// Resets the asset editing depth counter. Use with caution, only for test cleanup.
        /// </summary>
        internal static void ResetAssetEditingDepthForTesting()
        {
            _assetEditingDepth = 0;
        }
    }
#endif
}
