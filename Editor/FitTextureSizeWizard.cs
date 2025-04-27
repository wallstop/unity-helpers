namespace WallstopStudios.UnityHelpers.Editor
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Extension;

    public enum FitMode
    {
        GrowAndShrink = 0,
        GrowOnly = 1,
        ShrinkOnly = 2,
    }

    public sealed class FitTextureSizeWizard : ScriptableWizard
    {
        public FitMode fitMode = FitMode.GrowAndShrink;
        public List<Texture2D> textures = new();

        public List<Object> textureSourcePaths = new();

        [MenuItem("Tools/Unity Helpers/Fit Texture Size", priority = -1)]
        public static void EnsureSizes()
        {
            _ = DisplayWizard<FitTextureSizeWizard>("Fit Texture Size", "Run");
        }

        private void OnWizardCreate()
        {
            textures ??= new List<Texture2D>();
            textureSourcePaths ??= new List<Object>();
            HashSet<string> texturePaths = new();
            foreach (
                string assetPath in textureSourcePaths
                    .Select(AssetDatabase.GetAssetPath)
                    .Where(assetPath => !string.IsNullOrWhiteSpace(assetPath))
            )
            {
                _ = texturePaths.Add(assetPath);
            }

            if (!textures.Any() && !texturePaths.Any())
            {
                texturePaths.Add("Assets");
            }

            if (texturePaths.Any())
            {
                foreach (
                    string assetGuid in AssetDatabase.FindAssets(
                        "t:texture2D",
                        texturePaths.ToArray()
                    )
                )
                {
                    string path = AssetDatabase.GUIDToAssetPath(assetGuid);
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }

                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (texture != null)
                    {
                        textures.Add(texture);
                    }
                }
            }

            textures = textures.Distinct().OrderBy(texture => texture.name).ToList();
            if (textures.Count <= 0)
            {
                this.Log($"Failed to find any texture paths.");
                return;
            }

            int changedCount = 0;
            foreach (Texture2D texture in textures)
            {
                string assetPath = AssetDatabase.GetAssetPath(texture);
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    continue;
                }

                TextureImporter textureImporter =
                    AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (textureImporter == null)
                {
                    continue;
                }
                textureImporter.GetSourceTextureWidthAndHeight(out int width, out int height);

                float size = Mathf.Max(width, height);
                int textureSize = textureImporter.maxTextureSize;
                int originalTextureSize = textureSize;
                bool changed = false;
                if (fitMode is FitMode.GrowAndShrink or FitMode.GrowOnly)
                {
                    while (textureSize < size)
                    {
                        changed = true;
                        textureSize <<= 1;
                    }
                }

                if (fitMode is FitMode.GrowAndShrink or FitMode.ShrinkOnly)
                {
                    while (0 < textureSize && size <= (textureSize >> 1))
                    {
                        changed = true;
                        textureSize >>= 1;
                    }
                }

                textureImporter.maxTextureSize = textureSize;

                if (changed)
                {
                    ++changedCount;
                    textureImporter.SaveAndReimport();
                    if (textureImporter.maxTextureSize != textureSize)
                    {
                        this.LogError(
                            $"Failed to update {texture.name}, need texture size {textureSize} but got {textureImporter.maxTextureSize}. Path: '{assetPath}'."
                        );
                        if (originalTextureSize != textureImporter.maxTextureSize)
                        {
                            --changedCount;
                        }
                    }
                }
            }

            if (changedCount != 0)
            {
                this.Log($"Updated {changedCount} textures.");
                AssetDatabase.Refresh();
            }
            else
            {
                this.Log($"No textures updated.");
            }
        }
    }
}
