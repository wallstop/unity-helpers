namespace UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Core.Extension;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [Serializable]
    public sealed class SpriteSettings
    {
        public int pixelsPerUnit = 100;
        public Vector2 pivot = new(0.5f, 0.5f);
        public SpriteImportMode spriteMode = SpriteImportMode.Single;
        public bool applyWrapMode = true;
        public TextureWrapMode wrapMode = TextureWrapMode.Clamp;
        public bool applyFilterMode = true;
        public FilterMode filterMode = FilterMode.Point;
        public string name;
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

        [MenuItem("Tools/Unity Helpers/Sprite Settings Applier")]
        public static void CreateAnimation()
        {
            _ = DisplayWizard<SpriteSettingsApplier>("Sprite Settings Directory Applier", "Set");
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

            HashSet<string> processedSpritePaths = new();
            Queue<string> directoriesToCheck = new(uniqueDirectories);
            int spriteCount = 0;
            while (0 < directoriesToCheck.Count)
            {
                string directoryPath = directoriesToCheck.Dequeue();
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

            foreach (Sprite sprite in sprites)
            {
                if (sprite == null)
                {
                    continue;
                }

                string filePath = AssetDatabase.GetAssetPath(sprite);
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
            TextureImporter textureImporter = AssetImporter.GetAtPath(filePath) as TextureImporter;
            if (textureImporter == null)
            {
                return false;
            }

            SpriteSettings spriteData = spriteSettings.FirstOrDefault(settings =>
                string.IsNullOrEmpty(settings.name) || filePath.Contains(settings.name)
            );
            if (spriteData == null)
            {
                return false;
            }

            textureImporter.spritePivot = spriteData.pivot;
            textureImporter.spritePixelsPerUnit = spriteData.pixelsPerUnit;

            TextureImporterSettings settings = new();
            textureImporter.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spriteMode = (int)spriteData.spriteMode;
            if (spriteData.applyWrapMode)
            {
                settings.wrapMode = spriteData.wrapMode;
            }

            if (spriteData.applyFilterMode)
            {
                settings.filterMode = spriteData.filterMode;
            }

            textureImporter.SetTextureSettings(settings);
            textureImporter.SaveAndReimport();
            return true;
        }
    }
#endif
}
