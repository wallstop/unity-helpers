namespace WallstopStudios.UnityHelpers.Tests.Helper
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Sprites;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    public sealed class SpriteSettingsApplierAdditionalTests : CommonTestBase
    {
        private const string TestFolder = "Assets/TempSpriteApplierAdditional";
        private string _assetPath;

        [SetUp]
        public void Setup()
        {
            if (Application.isPlaying)
            {
                Assert.Ignore("AssetDatabase access requires edit mode.");
            }
            if (!AssetDatabase.IsValidFolder(TestFolder))
            {
                AssetDatabase.CreateFolder("Assets", "TempSpriteApplierAdditional");
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

        private string CreatePng(string name, bool asSprite)
        {
            Texture2D tex = Track(new Texture2D(4, 4, TextureFormat.RGBA32, false));
            byte[] png = tex.EncodeToPNG();
            string path = Path.Combine(TestFolder, name + ".png");
            File.WriteAllBytes(path, png);
            AssetDatabase.ImportAsset(path);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(ti != null, "Importer not found for asset path: " + path);
            if (asSprite)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.SaveAndReimport();
            }
            return path;
        }

        [Test]
        public void DetectsChangeForNameContainsWithPriority()
        {
            string path = CreatePng("ui_button", asSprite: true);
            _assetPath = path;

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
                    priority = 10,
                    applyFilterMode = true,
                    filterMode = FilterMode.Bilinear,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, prepared);
            Assert.IsTrue(
                willChange,
                "Expected detection when matching profile has apply flags. Path=" + path
            );

            bool changed = SpriteSettingsApplierAPI.TryUpdateTextureSettings(
                path,
                prepared,
                out TextureImporter importer
            );
            Assert.IsTrue(
                changed,
                "Expected TryUpdateTextureSettings to apply settings. Path=" + path
            );
            Assert.IsTrue(importer != null, "Expected non-null importer after change");
            importer.SaveAndReimport();
            Assert.AreEqual(
                FilterMode.Bilinear,
                importer.filterMode,
                "Expected higher-priority filter mode to win"
            );
        }

        [Test]
        public void DetectsChangeByExtensionAndEnforcesTextureType()
        {
            string path = CreatePng("any_name", asSprite: false);
            _assetPath = path;

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Extension,
                    matchPattern = ".png",
                    priority = 5,
                    applyTextureType = true,
                    textureType = TextureImporterType.Sprite,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, prepared);
            Assert.IsTrue(willChange, "Expected change detection by extension for path: " + path);

            bool changed = SpriteSettingsApplierAPI.TryUpdateTextureSettings(
                path,
                prepared,
                out TextureImporter importer
            );
            Assert.IsTrue(changed, "Expected importer to be updated for path: " + path);
            Assert.IsTrue(importer != null, "Importer was null after update for path: " + path);
            importer.SaveAndReimport();
            Assert.AreEqual(
                TextureImporterType.Sprite,
                importer.textureType,
                "Expected texture type to be enforced"
            );
        }

        [Test]
        public void DetectsChangeWithBackslashPath()
        {
            string fwd = CreatePng("named_for_backslash", asSprite: true);
            _assetPath = fwd;
            string back = fwd.Replace('/', '\\');

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.NameContains,
                    matchPattern = "backslash",
                    priority = 3,
                    applyFilterMode = true,
                    filterMode = FilterMode.Trilinear,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(back, prepared);
            Assert.IsTrue(willChange, "Expected detection for Windows-style path: " + back);
        }
    }
#endif
}
