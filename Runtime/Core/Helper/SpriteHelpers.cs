namespace UnityHelpers.Core.Helper
{
    using Extension;
    using UnityEditor;
    using UnityEngine;
    using Utils;

    public static class SpriteHelpers
    {
        public static void MakeReadable(this Texture2D texture)
        {
            if (texture.isReadable)
            {
                return;
            }

#if UNITY_EDITOR
            string assetPath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(assetPath))
            {
                texture.LogError("Failed to get asset path.");
                return;
            }

            TextureImporter tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter == null)
            {
                texture.LogError("Failed to get texture importer.");
                return;
            }

            tImporter.isReadable = true;
            EditorUtility.SetDirty(tImporter);
            tImporter.SaveAndReimport();
            EditorUtility.SetDirty(texture);
#endif
        }

        public static void SetSpritePivot(string fullSpritePath, Vector2 pivot)
        {
#if UNITY_EDITOR
            SetSpritePivot(AssetImporter.GetAtPath(fullSpritePath) as TextureImporter, pivot);
#endif
        }

        public static void SetSpritePivot(Sprite sprite, Vector2 pivot)
        {
#if UNITY_EDITOR
            SetSpritePivot(
                AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as TextureImporter,
                pivot
            );
#endif
        }

#if UNITY_EDITOR
        public static void SetSpritePivot(TextureImporter textureImporter, Vector2 pivot)
        {
            if (textureImporter == null)
            {
                return;
            }

            TextureImporterSettings textureImportSettings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(textureImportSettings);
            textureImportSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            textureImportSettings.wrapMode = TextureWrapMode.Clamp;
            textureImportSettings.filterMode = FilterMode.Trilinear;
            textureImporter.SetTextureSettings(textureImportSettings);

            TextureImporterPlatformSettings importerSettings = new TextureImporterPlatformSettings
            {
                resizeAlgorithm = TextureResizeAlgorithm.Bilinear,
                maxTextureSize = SetTextureImportData.RegularTextureSize,
                textureCompression = TextureImporterCompression.Compressed,
                format = TextureImporterFormat.Automatic,
            };

            textureImporter.SetPlatformTextureSettings(importerSettings);
            textureImporter.isReadable = true;
            textureImporter.spritePivot = pivot;
            textureImporter.SaveAndReimport();
        }
#endif
    }
}
