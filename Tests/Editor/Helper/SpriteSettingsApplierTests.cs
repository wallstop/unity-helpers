namespace WallstopStudios.UnityHelpers.Tests.Tests.Editor.Helper
{
    using System.Collections.Generic;
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Sprites;

    [TestFixture]
    public sealed class SpriteSettingsApplierTests
        : WallstopStudios.UnityHelpers.Tests.CommonTestBase
    {
        private const string TestFolder = "Assets/TempSpriteApplierTests";
        private string _assetPath;

        [SetUp]
        public void Setup()
        {
            if (Application.isPlaying)
            {
                return;
            }
            if (!AssetDatabase.IsValidFolder(TestFolder))
            {
                AssetDatabase.CreateFolder("Assets", "TempSpriteApplierTests");
            }
        }

        [TearDown]
        public void Teardown()
        {
            if (Application.isPlaying)
            {
                return;
            }
            if (!string.IsNullOrEmpty(_assetPath) && File.Exists(_assetPath))
            {
                AssetDatabase.DeleteAsset(_assetPath);
                _assetPath = null;
            }
            if (AssetDatabase.IsValidFolder(TestFolder))
            {
                AssetDatabase.DeleteAsset(TestFolder);
            }
            AssetDatabase.Refresh();
        }

        private string CreateTempTexture(bool asSprite = false)
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, mipChain: false);
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            string path = Path.Combine(TestFolder, "ui_button.png");
            File.WriteAllBytes(path, png);
            AssetDatabase.ImportAsset(path);

            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(ti);
            if (asSprite)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.SaveAndReimport();
            }
            return path;
        }

        [Test]
        public void AppliesProfileByNameContainsWithPriority()
        {
            if (Application.isPlaying)
            {
                Assert.Ignore("AssetDatabase access requires edit mode.");
            }
            string path = CreateTempTexture(asSprite: true);
            _assetPath = path;

            // lower priority sets FilterMode.Point; higher sets Bilinear
            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.NameContains,
                    matchPattern = "ui_",
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Point,
                },
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.NameContains,
                    matchPattern = "button",
                    priority = 5,
                    applyFilterMode = true,
                    filterMode = FilterMode.Bilinear,
                },
            };

            var prepared = SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, prepared);
            Assert.IsTrue(willChange, "Expected changes to be detected");

            bool changed = SpriteSettingsApplierAPI.TryUpdateTextureSettings(
                path,
                prepared,
                out TextureImporter importer
            );
            Assert.IsTrue(changed);
            Assert.IsNotNull(importer);
            importer.SaveAndReimport();

            // Verify final filter mode is from higher priority profile
            Assert.AreEqual(FilterMode.Bilinear, importer.filterMode);
        }

        [Test]
        public void EnforcesTextureTypeWhenConfigured()
        {
            if (Application.isPlaying)
            {
                Assert.Ignore("AssetDatabase access requires edit mode.");
            }
            string path = CreateTempTexture(asSprite: false);
            _assetPath = path;

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Extension,
                    matchPattern = ".png",
                    priority = 10,
                    applyTextureType = true,
                    textureType = TextureImporterType.Sprite,
                },
            };
            var prepared = SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool changed = SpriteSettingsApplierAPI.TryUpdateTextureSettings(
                path,
                prepared,
                out TextureImporter importer
            );
            Assert.IsTrue(changed);
            Assert.IsNotNull(importer);
            importer.SaveAndReimport();
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
        }
    }
}
