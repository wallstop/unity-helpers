namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using Extension;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Sprite and texture utilities for editor workflows.
    /// </summary>
    public static class SpriteHelpers
    {
        /// <summary>
        /// Ensures a Texture2D asset is marked as readable (Editor only). No-ops in player.
        /// </summary>
        /// <remarks>
        /// Useful for analysis or runtime generation workflows that require raw texture data.
        /// </remarks>
        public static void MakeReadable(this Texture2D texture)
        {
            if (texture == null || texture.isReadable)
            {
                return;
            }

#if UNITY_EDITOR
            string assetPath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(assetPath))
            {
                texture.LogError($"Failed to get asset path.");
                return;
            }

            TextureImporter tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter == null)
            {
                texture.LogError($"Failed to get texture importer.");
                return;
            }

            if (!tImporter.isReadable)
            {
                tImporter.isReadable = true;
                EditorUtility.SetDirty(tImporter);
                tImporter.SaveAndReimport();
                EditorUtility.SetDirty(texture);
            }
#endif
        }
    }
}
