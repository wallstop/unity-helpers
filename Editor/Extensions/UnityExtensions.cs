namespace WallstopStudios.UnityHelpers.Editor.Extensions
{
#if UNITY_EDITOR
    using System.Reflection;
    using UnityEditor.U2D;
    using UnityEngine;
    using UnityEngine.U2D;

    public static class UnityExtensions
    {
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
