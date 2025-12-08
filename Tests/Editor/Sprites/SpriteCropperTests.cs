namespace WallstopStudios.UnityHelpers.Tests.Sprites
{
#if UNITY_EDITOR
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.Sprites;
    using WallstopStudios.UnityHelpers.Tests.Core;

    public sealed class SpriteCropperTests : CommonTestBase
    {
        private const string Root = "Assets/Temp/SpriteCropperTests";

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            EnsureFolder(Root);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            AssetDatabase.DeleteAsset("Assets/Temp");
            AssetDatabase.Refresh();
        }

        [Test]
        public void CropsTransparentMarginsAndPreservesPivot()
        {
            string src = Path.Combine(Root, "src.png").SanitizePath();
            // 16x16 with an opaque 10x10 square starting at (3,3)
            CreatePngWithOpaqueRect(src, 16, 16, 3, 3, 10, 10, Color.white);
            AssetDatabase.Refresh();

            // Ensure sprite importer single + readable
            TextureImporter imp = AssetImporter.GetAtPath(src) as TextureImporter;
            Assert.IsTrue(imp != null);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePivot = new Vector2(0.5f, 0.5f); // center
            imp.isReadable = true;
            imp.SaveAndReimport();

            SpriteCropper window = Track(ScriptableObject.CreateInstance<SpriteCropper>());
            window._overwriteOriginals = true;
            window._inputDirectories = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };
            window.FindFilesToProcess();
            window.ProcessFoundSprites();

            AssetDatabase.Refresh();

            // Source should be overwritten and cropped to 10x10
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(src);
            Assert.IsTrue(tex != null);
            Assert.That(tex.width, Is.EqualTo(10));
            Assert.That(tex.height, Is.EqualTo(10));

            // Pivot should remain effectively center after crop
            imp = AssetImporter.GetAtPath(src) as TextureImporter;
            Assert.IsTrue(imp != null);
            Assert.That(imp.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
            Vector2 pivot = imp.spritePivot;
            Assert.That(pivot.x, Is.InRange(0.49f, 0.51f));
            Assert.That(pivot.y, Is.InRange(0.49f, 0.51f));
        }

        [Test]
        public void WritesToOutputDirectoryWhenNotOverwriting()
        {
            string src = Path.Combine(Root, "src2.png").SanitizePath();
            string outDir = Path.Combine(Root, "Out").SanitizePath();
            EnsureFolder(outDir);
            CreatePngWithOpaqueRect(src, 8, 8, 2, 2, 4, 4, Color.green);
            AssetDatabase.Refresh();

            TextureImporter imp = AssetImporter.GetAtPath(src) as TextureImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.isReadable = true;
            imp.SaveAndReimport();

            SpriteCropper window = Track(ScriptableObject.CreateInstance<SpriteCropper>());
            window._overwriteOriginals = false;
            window._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(outDir);
            window._inputDirectories = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };
            window.FindFilesToProcess();
            window.ProcessFoundSprites();

            AssetDatabase.Refresh();

            string dst = Path.Combine(outDir, "Cropped_src2.png").SanitizePath();
            Assert.That(File.Exists(RelToFull(dst)), Is.True, "Cropped output should exist");

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(dst);
            Assert.IsTrue(tex != null);
            Assert.That(tex.width, Is.EqualTo(4));
            Assert.That(tex.height, Is.EqualTo(4));
        }

        private void CreatePngWithOpaqueRect(
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
            string dir = Path.GetDirectoryName(relPath)?.SanitizePath();
            EnsureFolder(dir);
            Texture2D t = Track(
                new Texture2D(w, h, TextureFormat.RGBA32, false) { alphaIsTransparency = true }
            );
            Color[] pix = new Color[w * h];
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

        private static string RelToFull(string rel)
        {
            return Path.Combine(
                    Application.dataPath.Substring(
                        0,
                        Application.dataPath.Length - "Assets".Length
                    ),
                    rel
                )
                .SanitizePath();
        }
    }
#endif
}
