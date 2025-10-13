namespace WallstopStudios.UnityHelpers.Tests.Editor.Sprites
{
#if UNITY_EDITOR
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Sprites;
    using WallstopStudios.UnityHelpers.Tests.Editor.Utils;

    public sealed class SpritePivotAdjusterTests : CommonTestBase
    {
        private const string Root = "Assets/Temp/SpritePivotAdjusterTests";

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
        public void AdjustsPivotToAlphaWeightedCenter()
        {
            string path = Path.Combine(Root, "pivot.png").Replace('\\', '/');
            // 10x10 image, opaque L-shape to bias center toward bottom-left
            CreateAsymmetricAlpha(path, 10, 10);
            AssetDatabase.Refresh();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
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
            window._alphaCutoff = 0.01f;
            window._skipUnchanged = false;
            window._forceReimport = true;
            window.FindFilesToProcess();
            window.AdjustPivotsInDirectory(false);

            AssetDatabase.Refresh();

            // Expect pivot biased toward bottom-left (< 0.5)
            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Vector2 pivot = imp.spritePivot;
            Assert.That(pivot.x, Is.LessThan(0.5f));
            Assert.That(pivot.y, Is.LessThan(0.5f));
        }

        private static void CreateAsymmetricAlpha(string relPath, int w, int h)
        {
            string dir = Path.GetDirectoryName(relPath).Replace('\\', '/');
            EnsureFolder(dir);
            Texture2D t = new(w, h, TextureFormat.RGBA32, false) { alphaIsTransparency = true };
            Color[] pix = new Color[w * h];
            // Make an L-shape: full bottom row and full left column opaque; rest transparent
            for (int y = 0; y < h; ++y)
            for (int x = 0; x < w; ++x)
            {
                bool opaque = (y == 0) || (x == 0);
                pix[y * w + x] = opaque ? Color.white : new Color(0f, 0f, 0f, 0f);
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
