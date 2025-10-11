namespace WallstopStudios.UnityHelpers.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Utils;
    using Object = UnityEngine.Object;

    /// <summary>
    /// ScriptableWizard to apply texture importer settings (readability, mipmaps, wrap/filter,
    /// compression, platform resize algorithm/format/size) to individual textures or recursively
    /// to all textures under selected directories.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Usage: open via menu, pick explicit textures and/or directories, set which options to apply
    /// (e.g., toggle <c>applyWrapMode</c>), and press Set to write importer changes.
    /// </para>
    /// <para>
    /// Pros: fast bulk consistency; platform settings in one place.
    /// Caveats: triggers reimports; ensure file extension filters list (<c>spriteFileExtensions</c>)
    /// is accurate for your project; destructive to importer state—commit to VCS.
    /// </para>
    /// </remarks>
    public sealed class TextureSettingsApplier : ScriptableWizard
    {
        public bool applyReadOnly;
        public bool isReadOnly;
        public bool applyMipMaps;
        public bool generateMipMaps;
        public bool applyWrapMode;

        [WShowIf(nameof(applyWrapMode))]
        public TextureWrapMode wrapMode = TextureWrapMode.Clamp;

        public bool applyFilterMode;

        [WShowIf(nameof(applyFilterMode))]
        public FilterMode filterMode = FilterMode.Trilinear;

        public TextureImporterCompression compression = TextureImporterCompression.CompressedHQ;
        public bool useCrunchCompression = true;
        public TextureResizeAlgorithm textureResizeAlgorithm = TextureResizeAlgorithm.Bilinear;
        public int maxTextureSize = SetTextureImportData.MaxTextureSize;
        public TextureImporterFormat textureFormat = TextureImporterFormat.Automatic;
        public List<string> spriteFileExtensions = new() { ".png" };

        public List<Texture2D> textures = new();

        [Tooltip(
            "Drag a folder from Unity here to apply the configuration to all settings under it. No sprites are modified if no directories are provided."
        )]
        public List<Object> directories = new();

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/Texture Settings Applier")]
        public static void CreateAnimation()
        {
            _ = DisplayWizard<TextureSettingsApplier>("Texture Settings Directory Applier", "Set");
        }

        internal void OnWizardCreate()
        {
            // Build extension filter (normalize to dot-prefix)
            HashSet<string> allowedExtensions = new(StringComparer.OrdinalIgnoreCase);
            foreach (string extRaw in spriteFileExtensions ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(extRaw))
                {
                    continue;
                }
                string ext = extRaw.StartsWith(".") ? extRaw : "." + extRaw;
                _ = allowedExtensions.Add(ext);
            }

            // Collect folder asset paths from selections
            List<string> folderAssetPaths = new();
            foreach (Object directory in directories ?? Enumerable.Empty<Object>())
            {
                if (directory == null)
                {
                    continue;
                }
                string assetPath = AssetDatabase.GetAssetPath(directory);
                if (!string.IsNullOrWhiteSpace(assetPath) && AssetDatabase.IsValidFolder(assetPath))
                {
                    folderAssetPaths.Add(assetPath);
                }
            }

            // Canonical path set for deduplication
            HashSet<string> uniqueAssetPaths = new(StringComparer.OrdinalIgnoreCase);

            // From folders: use AssetDatabase.FindAssets (fast, robust)
            if (folderAssetPaths.Count > 0)
            {
                using PooledResource<string[]> folderLease = WallstopFastArrayPool<string>.Get(
                    folderAssetPaths.Count,
                    out string[] folders
                );
                for (int i = 0; i < folderAssetPaths.Count; i++)
                {
                    folders[i] = folderAssetPaths[i];
                }
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", folders);
                for (int i = 0; i < guids.Length; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (string.IsNullOrWhiteSpace(assetPath))
                    {
                        continue;
                    }
                    string ext = Path.GetExtension(assetPath);
                    if (allowedExtensions.Count > 0 && !allowedExtensions.Contains(ext))
                    {
                        continue;
                    }
                    _ = uniqueAssetPaths.Add(assetPath);
                }
            }

            // From explicit textures
            foreach (
                Texture2D texture in textures?.Where(t => t != null).Distinct()
                    ?? Enumerable.Empty<Texture2D>()
            )
            {
                string assetPath = AssetDatabase.GetAssetPath(texture);
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    continue;
                }
                string ext = Path.GetExtension(assetPath);
                if (allowedExtensions.Count > 0 && !allowedExtensions.Contains(ext))
                {
                    continue;
                }
                _ = uniqueAssetPaths.Add(assetPath);
            }

            // Prepare config for API (preserve wizard semantics)
            TextureSettingsApplierAPI.Config config = new()
            {
                applyReadWriteEnabled = applyReadOnly,
                readWriteEnabled = !isReadOnly,
                applyMipMaps = applyMipMaps,
                generateMipMaps = generateMipMaps,
                applyWrapMode = applyWrapMode,
                wrapMode = wrapMode,
                applyFilterMode = applyFilterMode,
                filterMode = filterMode,
                // For parity with previous wizard behavior, apply platform settings
                applyPlatformResizeAlgorithm = true,
                platformResizeAlgorithm = textureResizeAlgorithm,
                applyPlatformMaxTextureSize = true,
                platformMaxTextureSize = maxTextureSize,
                applyPlatformFormat = true,
                platformFormat = textureFormat,
                applyPlatformCompression = true,
                platformCompression = compression,
                applyPlatformCrunchCompression = true,
                platformUseCrunchCompression = useCrunchCompression,
                // Importer-level compression flags are exposed but not toggled by wizard UI;
                // leave them off to avoid conflicting signals.
                applyCompression = false,
                applyCrunchCompression = false,
            };

            // Batch-apply with progress and cancellation, reimport only changed importers at the end
            int textureCount = 0;
            using (
                Buffers<TextureImporter>.List.Get(
                    out System.Collections.Generic.List<TextureImporter> changedImporters
                )
            )
            {
                TextureImporterSettings buffer = new();
                AssetDatabase.StartAssetEditing();
                try
                {
                    int i = 0;
                    int total = uniqueAssetPaths.Count;
                    double lastUpdateTime = EditorApplication.timeSinceStartup;
                    foreach (string path in uniqueAssetPaths)
                    {
                        double now = EditorApplication.timeSinceStartup;
                        bool shouldUpdate =
                            i == 0 || i == total - 1 || i % 50 == 0 || now - lastUpdateTime > 0.2;
                        if (
                            shouldUpdate
                            && Utils.EditorUi.CancelableProgress(
                                "Applying Texture Settings",
                                $"Processing '{System.IO.Path.GetFileName(path)}' ({i + 1}/{total})",
                                total == 0 ? 1f : (float)(i + 1) / total
                            )
                        )
                        {
                            // canceled
                            break;
                        }
                        if (shouldUpdate)
                        {
                            lastUpdateTime = now;
                        }

                        if (
                            TextureSettingsApplierAPI.TryUpdateTextureSettings(
                                path,
                                in config,
                                out TextureImporter importer,
                                buffer
                            )
                        )
                        {
                            if (importer != null)
                            {
                                changedImporters.Add(importer);
                                ++textureCount;
                            }
                        }
                        i++;
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                    Utils.EditorUi.ClearProgress();
                    for (int j = 0; j < changedImporters.Count; j++)
                    {
                        changedImporters[j].SaveAndReimport();
                    }
                }
            }

            this.Log($"Processed {textureCount} textures.");
            if (textureCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                this.Log($"Asset database saved and refreshed.");
            }
            else
            {
                this.Log($"No textures required changes.");
            }
        }
    }
#endif
}
