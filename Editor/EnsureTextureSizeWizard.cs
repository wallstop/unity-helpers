namespace UnityHelpers.Editor
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Extension;
    using UnityEditor;
    using UnityEngine;

    public sealed class EnsureTextureSizeWizard : ScriptableWizard
    {
        public List<Texture2D> textures = new();

        public List<Object> textureSourcePaths = new();

        [MenuItem("Tools/Unity Helpers/Ensure Texture Size")]
        public static void EnsureSizes()
        {
            _ = DisplayWizard<EnsureTextureSizeWizard>("Ensure Texture Size", "Run");
        }

        private void OnWizardCreate()
        {
            textures ??= new List<Texture2D>();
            textureSourcePaths ??= new List<Object>();
            HashSet<string> texturePath = new();
            foreach (Object textureSource in textureSourcePaths)
            {
                string assetPath = AssetDatabase.GetAssetPath(textureSource);
                if (!string.IsNullOrWhiteSpace(assetPath))
                {
                    _ = texturePath.Add(assetPath);
                }
            }

            if (texturePath.Any())
            {
                foreach (
                    string assetGuid in AssetDatabase.FindAssets(
                        "t:texture2D",
                        texturePath.ToArray()
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
                this.Log("Failed to find any texture paths.");
                return;
            }

            int changedCount = 0;
            foreach (Texture2D inputTexture in textures)
            {
                Texture2D texture = inputTexture;
                string assetPath = AssetDatabase.GetAssetPath(texture);
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    continue;
                }

                TextureImporter tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (tImporter == null)
                {
                    continue;
                }
                tImporter.GetSourceTextureWidthAndHeight(out int width, out int height);

                float size = Mathf.Max(width, height);
                int textureSize = tImporter.maxTextureSize;
                bool changed = false;
                while (textureSize < size)
                {
                    changed = true;
                    textureSize <<= 1;
                }
                tImporter.maxTextureSize = textureSize;

                if (changed)
                {
                    changedCount++;
                    tImporter.SaveAndReimport();
                }
            }

            if (changedCount != 0)
            {
                this.Log($"Updated {changedCount} textures.");
                AssetDatabase.Refresh();
            }
            else
            {
                this.Log("No textures updated.");
            }
        }
    }
}
