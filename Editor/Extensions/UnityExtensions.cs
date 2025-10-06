namespace WallstopStudios.UnityHelpers.Editor.Extensions
{
#if UNITY_EDITOR
    using System.Reflection;
    using UnityEditor.U2D;
    using UnityEngine;
    using UnityEngine.U2D;

    /// <summary>
    /// Editor-only extension methods for Unity types.
    /// </summary>
    public static class UnityExtensions
    {
        /// <summary>
        /// Retrieves the preview texture from a SpriteAtlas using reflection.
        /// </summary>
        /// <param name="spriteAtlas">The SpriteAtlas to get the preview texture from.</param>
        /// <returns>The first non-null preview texture, or null if none are found.</returns>
        /// <remarks>
        /// This method uses reflection to access the internal GetPreviewTextures method from Unity's SpriteAtlasExtensions.
        /// The method is intended for editor use only and should not be used at runtime.
        /// Null handling: Returns null if the sprite atlas is null or has no preview textures.
        /// Thread-safe: No. Must be called from the main Unity thread.
        /// Performance: Uses reflection, which is relatively slow. Cache the result if used frequently.
        /// </remarks>
        public static Texture2D GetPreviewTexture(this SpriteAtlas spriteAtlas)
        {
            MethodInfo method = typeof(SpriteAtlasExtensions).GetMethod(
                "GetPreviewTextures",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public
            );
            object obj = method?.Invoke(null, new object[] { spriteAtlas });
            if (obj is not Texture2D[] { Length: > 0 } textures)
            {
                return null;
            }

            foreach (Texture2D texture in textures)
            {
                if (texture != null)
                {
                    return texture;
                }
            }

            return null;
        }
    }
#endif
}
