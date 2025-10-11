namespace WallstopStudios.UnityHelpers.Tests.Editor.Sprites
{
#if UNITY_EDITOR
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Sprites;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    public sealed class TextureSettingsApplierAPITests : CommonTestBase
    {
        private const string Root = "Assets/Temp/TextureSettingsApplierAPITests";

        [SetUp]
        public void SetUp()
        {
            EnsureFolder(Root);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            if (AssetDatabase.IsValidFolder("Assets/Temp"))
            {
                AssetDatabase.DeleteAsset("Assets/Temp");
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void AppliesDefaultPlatformSettingsViaAPI()
        {
            string texPath = (Root + "/api_tex.png").Replace('\\', '/');
            CreatePng(texPath, 16, 16, Color.white);
            AssetDatabase.Refresh();

            var config = new TextureSettingsApplierAPI.Config
            {
                applyPlatformMaxTextureSize = true,
                platformMaxTextureSize = 64,
                applyWrapMode = true,
                wrapMode = TextureWrapMode.Clamp,
                applyFilterMode = true,
                filterMode = FilterMode.Bilinear,
            };

            bool will = TextureSettingsApplierAPI.WillTextureSettingsChange(texPath, in config);
            Assert.IsTrue(will, "Expected WillTextureSettingsChange to detect differences");

            bool changed = TextureSettingsApplierAPI.TryUpdateTextureSettings(
                texPath,
                in config,
                out TextureImporter importer
            );
            Assert.IsTrue(changed, "Expected API to apply changes");
            Assert.IsNotNull(importer);
            importer.SaveAndReimport();

            TextureImporterPlatformSettings ps = importer.GetDefaultPlatformTextureSettings();
            Assert.AreEqual(64, ps.maxTextureSize);
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode);
            Assert.AreEqual(FilterMode.Bilinear, importer.filterMode);
        }

        [Test]
        public void AppliesNamedPlatformOverrideStandalone()
        {
            string texPath = (Root + "/api_tex_platform.png").Replace('\\', '/');
            CreatePng(texPath, 32, 32, Color.white);
            AssetDatabase.Refresh();

            var platform = new TextureSettingsApplierAPI.PlatformOverride
            {
                name = "Standalone",
                applyMaxTextureSize = true,
                maxTextureSize = 128,
            };
            var config = new TextureSettingsApplierAPI.Config
            {
                platformOverrides = new[] { platform },
            };

            bool changed = TextureSettingsApplierAPI.TryUpdateTextureSettings(
                texPath,
                in config,
                out TextureImporter importer
            );
            Assert.IsTrue(changed, "Expected override to apply");
            Assert.IsNotNull(importer);
            importer.SaveAndReimport();

            TextureImporterPlatformSettings ops = importer.GetPlatformTextureSettings("Standalone");
            Assert.AreEqual(128, ops.maxTextureSize);
            Assert.IsTrue(ops.overridden);
        }

        private static void EnsureFolder(string relPath)
        {
            if (AssetDatabase.IsValidFolder(relPath))
            {
                return;
            }
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
            EnsureFolder(Path.GetDirectoryName(relPath).Replace('\\', '/'));
            Texture2D t = new(w, h, TextureFormat.RGBA32, false);
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = c;
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
                .Replace('\\', '/');
        }
    }
#endif
}
