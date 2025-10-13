namespace WallstopStudios.UnityHelpers.Tests.Editor.Sprites
{
#if UNITY_EDITOR
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Sprites;
    using WallstopStudios.UnityHelpers.Tests.Editor.Utils;
    using Object = UnityEngine.Object;

    public sealed class SpriteCropperAdditionalTests : CommonTestBase
    {
        private const string Root = "Assets/Temp/SpriteCropperAdditionalTests";

        [SetUp]
        public void SetUp()
        {
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
        public void AppliesPaddingAndAdjustsPivotCorrectly()
        {
            string src = (Root + "/pad_src.png").Replace('\\', '/');
            // 20x20, opaque 10x10 at (5,5)
            CreatePngWithOpaqueRect(src, 20, 20, 5, 5, 10, 10, Color.white);
            AssetDatabase.Refresh();

            TextureImporter imp = AssetImporter.GetAtPath(src) as TextureImporter;
            Assert.IsTrue(imp != null);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.isReadable = true;
            imp.spritePivot = new Vector2(0.5f, 0.5f); // original center at (10,10)
            imp.SaveAndReimport();

            SpriteCropper window = Track(ScriptableObject.CreateInstance<SpriteCropper>());
            window._overwriteOriginals = false;
            window._inputDirectories = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };
            window._leftPadding = 2;
            window._bottomPadding = 3;
            window._rightPadding = 1;
            window._topPadding = 0;

            window.FindFilesToProcess();
            window.ProcessFoundSprites();

            AssetDatabase.Refresh();

            string dst = (Root + "/Cropped_pad_src.png").Replace('\\', '/');
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(dst);
            Assert.IsTrue(tex != null);
            // Expected size: (10 + 2 + 1) x (10 + 3 + 0) = 13x13
            Assert.That(tex.width, Is.EqualTo(13));
            Assert.That(tex.height, Is.EqualTo(13));

            TextureImporter newImp = AssetImporter.GetAtPath(dst) as TextureImporter;
            Assert.IsTrue(newImp != null);
            Vector2 pivot = newImp.spritePivot;
            // Expected pivot in pixels = (10-3, 10-2) = (7,8) → normalized (7/13, 8/13)
            Assert.That(pivot.x, Is.InRange(7f / 13f - 1e-3f, 7f / 13f + 1e-3f));
            Assert.That(pivot.y, Is.InRange(8f / 13f - 1e-3f, 8f / 13f + 1e-3f));
        }

        [Test]
        public void SkipsWhenOnlyNecessaryAndNoTrimNeeded()
        {
            string src = (Root + "/full_opaque.png").Replace('\\', '/');
            // Entirely opaque 8x8
            CreatePngFilled(src, 8, 8, Color.white);
            AssetDatabase.Refresh();

            TextureImporter imp = AssetImporter.GetAtPath(src) as TextureImporter;
            Assert.IsTrue(imp != null);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.isReadable = true;
            imp.SaveAndReimport();

            SpriteCropper window = Track(ScriptableObject.CreateInstance<SpriteCropper>());
            window._overwriteOriginals = false;
            window._onlyNecessary = true;
            window._inputDirectories = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };
            window.FindFilesToProcess();
            window.ProcessFoundSprites();

            AssetDatabase.Refresh();

            string dst = (Root + "/Cropped_full_opaque.png").Replace('\\', '/');
            Assert.That(
                File.Exists(RelToFull(dst)),
                Is.False,
                "Should not write cropped file when unnecessary"
            );
        }

        [Test]
        public void RestoresOriginalReadabilityWhenWritingToOutput()
        {
            string src = (Root + "/readable_toggle.png").Replace('\\', '/');
            CreatePngWithOpaqueRect(src, 10, 10, 2, 2, 6, 6, Color.white);
            AssetDatabase.Refresh();

            TextureImporter imp = AssetImporter.GetAtPath(src) as TextureImporter;
            Assert.IsTrue(imp != null);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.isReadable = false; // start unreadable
            imp.SaveAndReimport();

            SpriteCropper window = Track(ScriptableObject.CreateInstance<SpriteCropper>());
            window._overwriteOriginals = false; // write Cropped_*
            window._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(Root);
            window._inputDirectories = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };
            // Mirror source readability
            window.FindFilesToProcess();
            window.ProcessFoundSprites();
            AssetDatabase.Refresh();

            // Original should be restored to unreadable
            imp = AssetImporter.GetAtPath(src) as TextureImporter;
            Assert.IsTrue(imp != null);
            Assert.That(imp.isReadable, Is.False);

            string dst = (Root + "/Cropped_readable_toggle.png").Replace('\\', '/');
            TextureImporter newImp = AssetImporter.GetAtPath(dst) as TextureImporter;
            Assert.IsTrue(newImp != null);
            Assert.That(
                newImp.isReadable,
                Is.False,
                "MirrorSource should copy original readability"
            );

            // Now force output readability to Readable without reflection
            window = Track(ScriptableObject.CreateInstance<SpriteCropper>());
            window._overwriteOriginals = false;
            window._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(Root);
            window._inputDirectories = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };
            window._outputReadability = SpriteCropper.OutputReadability.Readable;

            window.FindFilesToProcess();
            window.ProcessFoundSprites();
            AssetDatabase.Refresh();

            string dst2 = (Root + "/Cropped_readable_toggle.png").Replace('\\', '/');
            newImp = AssetImporter.GetAtPath(dst2) as TextureImporter;
            Assert.IsTrue(newImp != null);
            Assert.That(newImp.isReadable, Is.True);
        }

        [Test]
        public void ProducesOneByOneForFullyTransparentImage()
        {
            string src = (Root + "/all_transparent.png").Replace('\\', '/');
            CreateTransparentPng(src, 12, 9);
            AssetDatabase.Refresh();

            TextureImporter imp = AssetImporter.GetAtPath(src) as TextureImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.isReadable = true;
            imp.SaveAndReimport();

            SpriteCropper window = Track(ScriptableObject.CreateInstance<SpriteCropper>());
            window._overwriteOriginals = false;
            window._inputDirectories = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };
            window.FindFilesToProcess();
            window.ProcessFoundSprites();

            AssetDatabase.Refresh();
            string dst = (Root + "/Cropped_all_transparent.png").Replace('\\', '/');
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(dst);
            Assert.IsTrue(tex != null);
            Assert.That(tex.width, Is.EqualTo(1));
            Assert.That(tex.height, Is.EqualTo(1));

            TextureImporter newImp = AssetImporter.GetAtPath(dst) as TextureImporter;
            Assert.IsTrue(newImp != null);
            Vector2 pivot = newImp.spritePivot;
            Assert.That(pivot.x, Is.InRange(0.49f, 0.51f));
            Assert.That(pivot.y, Is.InRange(0.49f, 0.51f));
        }

        [Test]
        public void SkipsMultipleSpriteTextures()
        {
            string src = (Root + "/multi.png").Replace('\\', '/');
            CreatePngWithOpaqueRect(src, 16, 16, 4, 4, 8, 8, Color.white);
            AssetDatabase.Refresh();

            TextureImporter imp = AssetImporter.GetAtPath(src) as TextureImporter;
            Assert.IsTrue(imp != null);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Multiple;
            imp.SaveAndReimport();

            SpriteCropper window = Track(ScriptableObject.CreateInstance<SpriteCropper>());
            window._overwriteOriginals = false;
            window._inputDirectories = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };
            window.FindFilesToProcess();
            window.ProcessFoundSprites();
            AssetDatabase.Refresh();

            // Should not create Cropped_* and should not overwrite
            string dst = (Root + "/Cropped_multi.png").Replace('\\', '/');
            Assert.That(File.Exists(RelToFull(dst)), Is.False);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(src);
            Assert.IsTrue(tex != null);
            Assert.That(tex.width, Is.EqualTo(16));
            Assert.That(tex.height, Is.EqualTo(16));
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
            EnsureFolder(Path.GetDirectoryName(relPath).Replace('\\', '/'));
            Texture2D t = new(w, h, TextureFormat.RGBA32, false) { alphaIsTransparency = true };
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

        private static void CreatePngFilled(string relPath, int w, int h, Color c)
        {
            EnsureFolder(Path.GetDirectoryName(relPath).Replace('\\', '/'));
            Texture2D t = new(w, h, TextureFormat.RGBA32, false) { alphaIsTransparency = true };
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = c;
            }

            t.SetPixels(pix);
            t.Apply();
            File.WriteAllBytes(RelToFull(relPath), t.EncodeToPNG());
        }

        private static void CreateTransparentPng(string relPath, int w, int h)
        {
            EnsureFolder(Path.GetDirectoryName(relPath).Replace('\\', '/'));
            Texture2D t = new(w, h, TextureFormat.RGBA32, false) { alphaIsTransparency = true };
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = new Color(0f, 0f, 0f, 0f);
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
