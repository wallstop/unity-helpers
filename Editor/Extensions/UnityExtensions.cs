namespace WallstopStudios.UnityHelpers.Editor.Extensions
{
#if UNITY_EDITOR
    using System.Linq;
    using System.Reflection;
    using Core.Helper;
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
            object obj =
                method != null ? ReflectionHelpers.InvokeStaticMethod(method, spriteAtlas) : null;
            return obj is not Texture2D[] { Length: > 0 } textures
                ? null
                : textures.Where(Objects.NotNull).FirstOrDefault();
        }
    }
#endif
}
