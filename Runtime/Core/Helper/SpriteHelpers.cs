namespace UnityHelpers.Core.Helper
{
    using Extension;
    using UnityEditor;
    using UnityEngine;

    public static class SpriteHelpers
    {
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
