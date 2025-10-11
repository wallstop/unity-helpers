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

    public sealed class SpritePivotAdjusterAdditionalTests : CommonTestBase
    {
        private const string Root = "Assets/Temp/SpritePivotAdjusterAdditionalTests";

        [SetUp]
        public void SetUp()
        {
            EnsureFolder(Root);
            SpritePivotAdjuster.SuppressUserPrompts = true;
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            AssetDatabase.DeleteAsset("Assets/Temp");
            AssetDatabase.Refresh();
            SpritePivotAdjuster.SuppressUserPrompts = false;
        }

        [Test]
        public void RespectsAlphaCutoffWhenComputingPivot()
        {
            string src = (Root + "/alpha_bias.png").Replace('\\', '/');
            // 20x20 with a faint (alpha=0.2) 4x4 block at bottom-left and a solid 4x4 at top-right
            CreateDualAlphaPattern(src, 20, 20);
            AssetDatabase.Refresh();

            TextureImporter imp = AssetImporter.GetAtPath(src) as TextureImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.isReadable = true;
            imp.spritePivot = new Vector2(0.5f, 0.5f);
            imp.SaveAndReimport();

            SpritePivotAdjuster window = Track(
                ScriptableObject.CreateInstance<SpritePivotAdjuster>()
            );
            window._directoryPaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };
            window._skipUnchanged = false;
            window.FindFilesToProcess();

            // Low cutoff: include faint alpha → pivot pulled toward bottom-left somewhat
            window._alphaCutoff = 0.1f;
            window._forceReimport = true;
            window.AdjustPivotsInDirectory(false);
            AssetDatabase.Refresh();
            imp = AssetImporter.GetAtPath(src) as TextureImporter;
            Vector2 pivotLow = imp.spritePivot;

            // High cutoff: ignore faint alpha → pivot biased to top-right block only
            window = Track(ScriptableObject.CreateInstance<SpritePivotAdjuster>());
            window._directoryPaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };
            window._skipUnchanged = false;
            window._alphaCutoff = 0.6f;
            window._forceReimport = true;
            window.FindFilesToProcess();
            window.AdjustPivotsInDirectory(false);
            AssetDatabase.Refresh();
            imp = AssetImporter.GetAtPath(src) as TextureImporter;
            Vector2 pivotHigh = imp.spritePivot;

            // Expect the low-cutoff pivot to be less than the high-cutoff (more bottom-left influence)
            Assert.That(pivotLow.x, Is.LessThan(pivotHigh.x));
            Assert.That(pivotLow.y, Is.LessThan(pivotHigh.y));
        }

        [Test]
        public void SkipsWhenTextureNotReadable()
        {
            string src = (Root + "/nonreadable.png").Replace('\\', '/');
            CreateOpaqueLShape(src, 10, 10);
            AssetDatabase.Refresh();

            TextureImporter imp = AssetImporter.GetAtPath(src) as TextureImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.isReadable = false;
            imp.spritePivot = new Vector2(0.5f, 0.5f);
            imp.SaveAndReimport();

            Vector2 before = imp.spritePivot;

            SpritePivotAdjuster window = Track(
                ScriptableObject.CreateInstance<SpritePivotAdjuster>()
            );
            window._directoryPaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };
            window._skipUnchanged = false;
            window._forceReimport = true;
            window.FindFilesToProcess();
            window.AdjustPivotsInDirectory(false);
            AssetDatabase.Refresh();

            imp = AssetImporter.GetAtPath(src) as TextureImporter;
            Vector2 after = imp.spritePivot;
            Assert.That(after, Is.EqualTo(before));
        }

        [Test]
        public void SkipsMultipleSpriteTextures()
        {
            string src = (Root + "/multi.png").Replace('\\', '/');
            CreateOpaqueLShape(src, 12, 12);
            AssetDatabase.Refresh();

            TextureImporter imp = AssetImporter.GetAtPath(src) as TextureImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Multiple;
            imp.isReadable = true;
            imp.spritePivot = new Vector2(0.3f, 0.7f);
            imp.SaveAndReimport();

            Vector2 before = imp.spritePivot;

            SpritePivotAdjuster window = Track(
                ScriptableObject.CreateInstance<SpritePivotAdjuster>()
            );
            window._directoryPaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };
            window._skipUnchanged = false;
            window._forceReimport = true;
            window.FindFilesToProcess();
            window.AdjustPivotsInDirectory(false);
            AssetDatabase.Refresh();

            imp = AssetImporter.GetAtPath(src) as TextureImporter;
            Vector2 after = imp.spritePivot;
            Assert.That(after, Is.EqualTo(before));
        }

        private static void CreateDualAlphaPattern(string relPath, int w, int h)
        {
            EnsureFolder(Path.GetDirectoryName(relPath).Replace('\\', '/'));
            Texture2D t = new(w, h, TextureFormat.RGBA32, false) { alphaIsTransparency = true };
            Color[] pix = new Color[w * h];
            for (int y = 0; y < h; ++y)
            for (int x = 0; x < w; ++x)
            {
                // faint 4x4 at (0..3,0..3), alpha=0.2
                bool faint = x < 4 && y < 4;
                // solid 4x4 at top-right (w-4..w-1, h-4..h-1)
                bool solid = x >= w - 4 && y >= h - 4;
                if (solid)
                {
                    pix[y * w + x] = new Color(1, 1, 1, 1);
                }
                else if (faint)
                {
                    pix[y * w + x] = new Color(1, 1, 1, 0.2f);
                }
                else
                {
                    pix[y * w + x] = new Color(0, 0, 0, 0);
                }
            }
            t.SetPixels(pix);
            t.Apply();
            File.WriteAllBytes(RelToFull(relPath), t.EncodeToPNG());
        }

        private static void CreateOpaqueLShape(string relPath, int w, int h)
        {
            EnsureFolder(Path.GetDirectoryName(relPath).Replace('\\', '/'));
            Texture2D t = new(w, h, TextureFormat.RGBA32, false) { alphaIsTransparency = true };
            Color[] pix = new Color[w * h];
            for (int y = 0; y < h; ++y)
            for (int x = 0; x < w; ++x)
            {
                bool opaque = (y == 0) || (x == 0);
                pix[y * w + x] = opaque ? Color.white : new Color(0, 0, 0, 0);
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
