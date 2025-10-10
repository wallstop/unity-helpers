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

        private void OnWizardCreate()
        {
            HashSet<string> uniqueDirectories = new();
            foreach (Object directory in directories)
            {
                string assetPath = AssetDatabase.GetAssetPath(directory);
                if (Directory.Exists(assetPath))
                {
                    _ = uniqueDirectories.Add(assetPath);
                }
            }

            int textureCount = 0;
            HashSet<string> processedPaths = new();
            foreach (
                Texture2D texture in textures
                    ?.Distinct()
                    .OrderBy(texture => texture != null ? texture.name : string.Empty)
                    ?? Enumerable.Empty<Texture2D>()
            )
            {
                if (texture == null)
                {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath(texture);
                if (
                    processedPaths.Add(Application.dataPath + assetPath)
                    && TryUpdateTextureSettings(assetPath)
                )
                {
                    ++textureCount;
                }
            }

            Queue<string> directoriesToCheck = new(uniqueDirectories);
            while (directoriesToCheck.TryDequeue(out string directoryPath))
            {
                foreach (string fullFilePath in Directory.EnumerateFiles(directoryPath))
                {
                    if (!spriteFileExtensions.Contains(Path.GetExtension(fullFilePath)))
                    {
                        continue;
                    }

                    int index = fullFilePath.LastIndexOf(
                        directoryPath,
                        StringComparison.OrdinalIgnoreCase
                    );
                    if (index < 0)
                    {
                        continue;
                    }

                    string filePath = fullFilePath.Substring(index);
                    if (processedPaths.Add(fullFilePath) && TryUpdateTextureSettings(filePath))
                    {
                        ++textureCount;
                    }
                }

                foreach (string subDirectory in Directory.EnumerateDirectories(directoryPath))
                {
                    int index = subDirectory.LastIndexOf(
                        directoryPath,
                        StringComparison.OrdinalIgnoreCase
                    );
                    if (index < 0)
                    {
                        continue;
                    }

                    directoriesToCheck.Enqueue(subDirectory.Substring(index));
                }
            }

            this.Log($"Processed {textureCount} textures.");
            if (0 < textureCount)
            {
                AssetDatabase.Refresh();
            }
        }

        private bool TryUpdateTextureSettings(string filePath)
        {
            TextureImporter textureImporter = AssetImporter.GetAtPath(filePath) as TextureImporter;
            if (textureImporter == null)
            {
                return false;
            }

            textureImporter.SetPlatformTextureSettings(
                new TextureImporterPlatformSettings
                {
                    resizeAlgorithm = textureResizeAlgorithm,
                    maxTextureSize = maxTextureSize,
                    format = textureFormat,
                    textureCompression = compression,
                    crunchedCompression = useCrunchCompression,
                }
            );
            if (applyReadOnly)
            {
                textureImporter.isReadable = !isReadOnly;
            }

            if (applyMipMaps)
            {
                textureImporter.mipmapEnabled = generateMipMaps;
            }

            if (applyWrapMode)
            {
                textureImporter.wrapMode = wrapMode;
            }

            if (applyFilterMode)
            {
                textureImporter.filterMode = filterMode;
            }

            textureImporter.SaveAndReimport();
            return true;
        }
    }
#endif
}
