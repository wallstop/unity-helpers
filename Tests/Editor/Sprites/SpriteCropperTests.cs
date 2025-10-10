namespace WallstopStudios.UnityHelpers.Tests.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using System.IO;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;

    public sealed class SpriteCropperTests
    {
        private const string Root = "Assets/Temp/SpriteCropperTests";

        [SetUp]
        public void SetUp()
        {
            EnsureFolder(Root);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset("Assets/Temp");
            AssetDatabase.Refresh();
        }

        [Test]
        public void CropsTransparentMarginsAndPreservesPivot()
        {
            string src = Path.Combine(Root, "src.png").Replace('\\', '/');
            // 16x16 with an opaque 10x10 square starting at (3,3)
            CreatePngWithOpaqueRect(src, 16, 16, 3, 3, 10, 10, Color.white);
            AssetDatabase.Refresh();

            // Ensure sprite importer single + readable
            TextureImporter imp = AssetImporter.GetAtPath(src) as TextureImporter;
            Assert.IsNotNull(imp);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePivot = new Vector2(0.5f, 0.5f); // center
            imp.isReadable = true;
            imp.SaveAndReimport();

            var window =
                ScriptableObject.CreateInstance<WallstopStudios.UnityHelpers.Editor.Sprites.SpriteCropper>();
            window._overwriteOriginals = true;
            window._inputDirectories = new System.Collections.Generic.List<UnityEngine.Object>
            {
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Root),
            };
            window.FindFilesToProcess();
            window.ProcessFoundSprites();

            AssetDatabase.Refresh();

            // Source should be overwritten and cropped to 10x10
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(src);
            Assert.IsNotNull(tex);
            Assert.That(tex.width, Is.EqualTo(10));
            Assert.That(tex.height, Is.EqualTo(10));

            // Pivot should remain effectively center after crop
            imp = AssetImporter.GetAtPath(src) as TextureImporter;
            Assert.IsNotNull(imp);
            Assert.That(imp.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
            Vector2 pivot = imp.spritePivot;
            Assert.That(pivot.x, Is.InRange(0.49f, 0.51f));
            Assert.That(pivot.y, Is.InRange(0.49f, 0.51f));
        }

        [Test]
        public void WritesToOutputDirectoryWhenNotOverwriting()
        {
            string src = Path.Combine(Root, "src2.png").Replace('\\', '/');
            string outDir = Path.Combine(Root, "Out").Replace('\\', '/');
            EnsureFolder(outDir);
            CreatePngWithOpaqueRect(src, 8, 8, 2, 2, 4, 4, Color.green);
            AssetDatabase.Refresh();

            TextureImporter imp = AssetImporter.GetAtPath(src) as TextureImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.isReadable = true;
            imp.SaveAndReimport();

            var window =
                ScriptableObject.CreateInstance<WallstopStudios.UnityHelpers.Editor.Sprites.SpriteCropper>();
            window._overwriteOriginals = false;
            window._outputDirectory = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(outDir);
            window._inputDirectories = new System.Collections.Generic.List<UnityEngine.Object>
            {
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Root),
            };
            window.FindFilesToProcess();
            window.ProcessFoundSprites();

            AssetDatabase.Refresh();

            string dst = Path.Combine(outDir, "Cropped_src2.png").Replace('\\', '/');
            Assert.That(File.Exists(RelToFull(dst)), Is.True, "Cropped output should exist");

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(dst);
            Assert.IsNotNull(tex);
            Assert.That(tex.width, Is.EqualTo(4));
            Assert.That(tex.height, Is.EqualTo(4));
        }

        private static void CreatePngWithOpaqueRect(
            string relPath,
            int w,
            int h,
            int rectX,
            int rectY,
            int rectW,
            int rectH,
            Color color
        )
        {
            string dir = Path.GetDirectoryName(relPath).Replace('\\', '/');
            EnsureFolder(dir);
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                alphaIsTransparency = true,
            };
            var pix = new Color[w * h];
            for (int y = 0; y < h; ++y)
            for (int x = 0; x < w; ++x)
            {
                bool inRect = x >= rectX && x < rectX + rectW && y >= rectY && y < rectY + rectH;
                pix[y * w + x] = inRect ? color : new Color(0f, 0f, 0f, 0f);
            }
            t.SetPixels(pix);
            t.Apply();
            File.WriteAllBytes(RelToFull(relPath), t.EncodeToPNG());
        }

        private static void EnsureFolder(string relPath)
        {
            string[] parts = relPath.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(cur, parts[i]);
                }
                cur = next;
            }
        }

        private static string RelToFull(string rel)
        {
            return Path.Combine(
                    Application.dataPath.Substring(
                        0,
                        Application.dataPath.Length - "Assets".Length
                    ),
                    rel
                )
                .Replace('\\', '/');
        }
    }
#endif
}
