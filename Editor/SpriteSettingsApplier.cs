﻿namespace UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Core.Attributes;
    using Core.Extension;
    using Core.Helper;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [Serializable]
    public sealed class SpriteSettings
    {
        public bool applyPixelsPerUnit;

        [WShowIf(nameof(applyPixelsPerUnit))]
        public int pixelsPerUnit = 100;

        public bool applyPivot;

        [WShowIf(nameof(applyPivot))]
        public Vector2 pivot = new(0.5f, 0.5f);

        public bool applySpriteMode;

        [WShowIf(nameof(applySpriteMode))]
        public SpriteImportMode spriteMode = SpriteImportMode.Single;

        public bool applyGenerateMipMaps;

        [WShowIf(nameof(applyGenerateMipMaps))]
        public bool generateMipMaps;

        public bool applyAlphaIsTransparency;

        [WShowIf(nameof(applyAlphaIsTransparency))]
        public bool alphaIsTransparency = true;

        public bool applyReadWriteEnabled;

        [WShowIf(nameof(applyReadWriteEnabled))]
        public bool readWriteEnabled = true;

        public bool applyExtrudeEdges;

        [WShowIf(nameof(applyExtrudeEdges))]
        [Range(0, 32)]
        public uint extrudeEdges = 1;

        public bool applyWrapMode;

        [WShowIf(nameof(applyWrapMode))]
        public TextureWrapMode wrapMode = TextureWrapMode.Clamp;

        public bool applyFilterMode;

        [WShowIf(nameof(applyFilterMode))]
        public FilterMode filterMode = FilterMode.Point;

        public string name = string.Empty;
    }

    public sealed class SpriteSettingsApplier : ScriptableWizard
    {
        public List<Sprite> sprites = new();
        public List<string> spriteFileExtensions = new() { ".png" };

        [Tooltip(
            "Drag various sprite settings here, where the name property matches a sprite asset name. The first settings with an empty or matching name will be applied to each and every sprite."
        )]
        public List<SpriteSettings> spriteSettings = new() { new SpriteSettings() };

        [Tooltip(
            "Drag a folder from Unity here to apply the configuration to all settings under it. No sprites are modified if no directories are provided."
        )]
        public List<Object> directories = new();

        [MenuItem("Tools/Unity Helpers/Sprite Settings Applier", priority = -2)]
        public static void CreateAnimation()
        {
            _ = DisplayWizard<SpriteSettingsApplier>("Sprite Settings Directory Applier", "Set");
        }

        private void OnWizardCreate()
        {
            HashSet<string> uniqueDirectories = new();
            foreach (
                string assetPath in directories
                    .Where(Objects.NotNull)
                    .Select(AssetDatabase.GetAssetPath)
                    .Where(assetPath => !string.IsNullOrWhiteSpace(assetPath))
            )
            {
                if (Directory.Exists(assetPath))
                {
                    _ = uniqueDirectories.Add(assetPath);
                }
            }

            HashSet<string> processedSpritePaths = new();
            Queue<string> directoriesToCheck = new(uniqueDirectories);
            int spriteCount = 0;
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
                    if (
                        processedSpritePaths.Add(fullFilePath) && TryUpdateTextureSettings(filePath)
                    )
                    {
                        ++spriteCount;
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

            foreach (
                string filePath in sprites
                    .Where(Objects.NotNull)
                    .Select(AssetDatabase.GetAssetPath)
                    .Where(Objects.NotNull)
            )
            {
                if (
                    processedSpritePaths.Add(Application.dataPath + filePath)
                    && TryUpdateTextureSettings(filePath)
                )
                {
                    ++spriteCount;
                }
            }

            this.Log("Processed {0} sprites.", spriteCount);
            if (0 < spriteCount)
            {
                AssetDatabase.Refresh();
            }
        }

        private bool TryUpdateTextureSettings(string filePath)
        {
            bool changed = false;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return changed;
            }

            TextureImporter textureImporter = AssetImporter.GetAtPath(filePath) as TextureImporter;
            if (textureImporter == null)
            {
                return changed;
            }

            SpriteSettings spriteData = spriteSettings.Find(settings =>
                string.IsNullOrWhiteSpace(settings.name) || filePath.Contains(settings.name)
            );
            if (spriteData == null)
            {
                return changed;
            }

            if (spriteData.applyPixelsPerUnit)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                changed |= textureImporter.spritePixelsPerUnit != spriteData.pixelsPerUnit;
                textureImporter.spritePixelsPerUnit = spriteData.pixelsPerUnit;
            }

            if (spriteData.applyPivot)
            {
                changed |= textureImporter.spritePivot != spriteData.pivot;
                textureImporter.spritePivot = spriteData.pivot;
            }

            if (spriteData.applyGenerateMipMaps)
            {
                changed |= textureImporter.mipmapEnabled != spriteData.generateMipMaps;
                textureImporter.mipmapEnabled = spriteData.generateMipMaps;
            }

            bool changedSettings = false;
            TextureImporterSettings settings = new();
            textureImporter.ReadTextureSettings(settings);
            if (spriteData.applyPivot)
            {
                changedSettings |= settings.spriteAlignment != (int)SpriteAlignment.Custom;
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
            }

            if (spriteData.applyAlphaIsTransparency)
            {
                changedSettings |= settings.alphaIsTransparency != spriteData.alphaIsTransparency;
                settings.alphaIsTransparency = spriteData.alphaIsTransparency;
            }

            if (spriteData.applyReadWriteEnabled)
            {
                changedSettings |= settings.readable != spriteData.readWriteEnabled;
                settings.readable = spriteData.readWriteEnabled;
            }

            if (spriteData.applySpriteMode)
            {
                changedSettings |= settings.spriteMode != (int)spriteData.spriteMode;
                settings.spriteMode = (int)spriteData.spriteMode;
            }

            if (spriteData.applyExtrudeEdges)
            {
                changedSettings |= settings.spriteExtrude != spriteData.extrudeEdges;
                settings.spriteExtrude = spriteData.extrudeEdges;
            }

            if (spriteData.applyWrapMode)
            {
                changedSettings |= settings.wrapMode != spriteData.wrapMode;
                settings.wrapMode = spriteData.wrapMode;
            }

            if (spriteData.applyFilterMode)
            {
                changedSettings |= settings.filterMode != spriteData.filterMode;
                settings.filterMode = spriteData.filterMode;
            }

            if (changedSettings)
            {
                textureImporter.SetTextureSettings(settings);
            }
            changed |= changedSettings;
            if (changed)
            {
                textureImporter.SaveAndReimport();
            }

            return changed;
        }
    }
#endif
}
