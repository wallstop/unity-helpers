namespace WallstopStudios.UnityHelpers.Editor
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using Core.Extension;
    using Object = UnityEngine.Object;

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

        [MenuItem("Tools/Wallstop Studios/Unity Helpers/Fit Texture Size", priority = -1)]
        public static void EnsureSizes()
        {
            _ = DisplayWizard<FitTextureSizeWizard>("Fit Texture Size", "Run");
        }

        private void OnEnable()
        {
            if (textureSourcePaths is { Count: > 0 })
            {
                return;
            }

            Object defaultFolder = AssetDatabase.LoadAssetAtPath<Object>("Assets/Sprites");
            if (defaultFolder != null)
            {
                textureSourcePaths ??= new List<Object>();
                textureSourcePaths.Add(defaultFolder);
            }
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
            List<TextureImporter> updatedImporters = new();
            AssetDatabase.StartAssetEditing();
            try
            {
                float totalAssets = textures.Count;
                for (int i = 0; i < textures.Count; i++)
                {
                    Texture2D texture = textures[i];
                    string assetPath = AssetDatabase.GetAssetPath(texture);
                    float progress = (i + 1) / totalAssets;
                    EditorUtility.DisplayProgressBar(
                        "Fitting Texture Size",
                        $"Checking: {Path.GetFileName(assetPath)} ({i + 1}/{textures.Count})",
                        progress
                    );
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
                        updatedImporters.Add(textureImporter);
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

                foreach (TextureImporter importer in updatedImporters)
                {
                    importer.SaveAndReimport();
                }

                AssetDatabase.StopAssetEditing();
                if (changedCount != 0)
                {
                    this.Log($"Updated {changedCount} textures.");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                else
                {
                    this.Log($"No textures updated.");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
#endif
}
