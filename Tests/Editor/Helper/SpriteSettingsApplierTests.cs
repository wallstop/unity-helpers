namespace WallstopStudios.UnityHelpers.Tests.Editor.Helper
{
    using System.Collections.Generic;
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Sprites;
    using WallstopStudios.UnityHelpers.Tests.Editor.Utils;

    [TestFixture]
    public sealed class SpriteSettingsApplierTests : CommonTestBase
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
        public override void TearDown()
        {
            base.TearDown();
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
            Texture2D tex = Track(new Texture2D(4, 4, TextureFormat.RGBA32, mipChain: false));
            byte[] png = tex.EncodeToPNG();
            string path = Path.Combine(TestFolder, "ui_button.png");
            File.WriteAllBytes(path, png);
            AssetDatabase.ImportAsset(path);

            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(ti != null);
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

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, prepared);
            Assert.IsTrue(willChange, $"Expected change detection for path: {path}");

            bool changed = SpriteSettingsApplierAPI.TryUpdateTextureSettings(
                path,
                prepared,
                out TextureImporter importer
            );
            Assert.IsTrue(
                changed,
                $"Expected TryUpdateTextureSettings to apply settings. Path={path}"
            );
            Assert.IsTrue(importer != null, $"Importer was null for path: {path}");
            importer.SaveAndReimport();

            // Verify final filter mode is from higher priority profile
            Assert.AreEqual(
                FilterMode.Bilinear,
                importer.filterMode,
                $"Expected higher-priority Bilinear filter; actual={importer.filterMode}"
            );
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
            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool changed = SpriteSettingsApplierAPI.TryUpdateTextureSettings(
                path,
                prepared,
                out TextureImporter importer
            );
            Assert.IsTrue(
                changed,
                $"Expected TryUpdateTextureSettings to update importer for path: {path}"
            );
            Assert.IsTrue(importer != null, $"Importer was null for path: {path}");
            importer.SaveAndReimport();
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
        }
    }
}
