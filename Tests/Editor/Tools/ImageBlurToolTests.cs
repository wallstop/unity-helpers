namespace WallstopStudios.UnityHelpers.Tests.Editor.Tools
{
#if UNITY_EDITOR
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;

    public sealed class ImageBlurToolTests
    {
        private const string TempRoot = "Assets/Temp/ImageBlurToolTests";

        [SetUp]
        public void SetUp()
        {
            EnsureFolder(TempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset("Assets/Temp");
            AssetDatabase.Refresh();
        }

        [Test]
        public void GenerateGaussianKernelIsNormalizedAndSymmetric()
        {
            float[] k = WallstopStudios.UnityHelpers.Editor.Tools.ImageBlurTool.KernelForTests(5);
            Assert.IsNotNull(k, "Kernel should not be null");
            Assert.That(k.Length, Is.EqualTo(11), "Kernel size should be 2r+1");

            float sum = k.Sum();
            Assert.That(Mathf.Abs(1f - sum) < 1e-3f, "Kernel should sum to 1");

            for (int i = 0; i < k.Length; i++)
            {
                Assert.That(
                    Mathf.Abs(k[i] - k[k.Length - 1 - i]) < 1e-6f,
                    $"Kernel must be symmetric at {i}"
                );
            }
        }

        [Test]
        public void CreateBlurredTextureSoftensHighContrastPixel()
        {
            Texture2D src = new Texture2D(5, 5, TextureFormat.RGBA32, false);
            Color[] pixels = Enumerable.Repeat(Color.black, 25).ToArray();
            pixels[12] = Color.white;
            src.SetPixels(pixels);
            src.Apply();

            Texture2D blurred =
                WallstopStudios.UnityHelpers.Editor.Tools.ImageBlurTool.BlurredForTests(src, 2);
            Assert.IsNotNull(blurred, "Blurred texture should be created");
            Assert.That(blurred.width, Is.EqualTo(5));
            Assert.That(blurred.height, Is.EqualTo(5));

            Color[] outPix = blurred.GetPixels();
            Assert.That(outPix[12].maxColorComponent < 1f, "Center intensity should reduce");
            Assert.That(outPix[11].maxColorComponent > 0f, "Neighbor should gain intensity");
            Assert.That(outPix[13].maxColorComponent > 0f, "Neighbor should gain intensity");
        }

        [Test]
        public void TrySyncDirectoryCollectsTextureAssets()
        {
            string texPath = Path.Combine(TempRoot, "tex.png").Replace('\\', '/');
            CreatePng(texPath, 8, 8, Color.red);

            AssetDatabase.Refresh();
            var folderObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(TempRoot);
            Assert.IsNotNull(folderObj, "Temp folder not found as asset");

            var list = new System.Collections.Generic.List<Texture2D>();
            WallstopStudios.UnityHelpers.Editor.Tools.ImageBlurTool.TrySyncDirectory(
                TempRoot,
                list
            );
            Assert.That(
                list.Count,
                Is.GreaterThanOrEqualTo(1),
                "Expected at least one texture collected"
            );
            Assert.IsTrue(
                list.Any(t => t != null && t.name == "tex"),
                "Texture tex should be present"
            );
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

        private static void CreatePng(string relPath, int w, int h, Color c)
        {
            string dir = Path.GetDirectoryName(relPath).Replace('\\', '/');
            EnsureFolder(dir);
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pix = Enumerable.Repeat(c, w * h).ToArray();
            t.SetPixels(pix);
            t.Apply();
            byte[] data = t.EncodeToPNG();
            File.WriteAllBytes(RelToFull(relPath), data);
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
