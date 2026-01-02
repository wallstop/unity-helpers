// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Helper
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Sprites;
    using WallstopStudios.UnityHelpers.Tests.Core;

    public sealed class SpriteSettingsApplierAdditionalTests : CommonTestBase
    {
        private const string TestFolder = "Assets/TempSpriteApplierAdditional";
        private string _assetPath;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
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

            // Set initial filter mode to Point (different from what the higher-priority profile wants)
            // Unity's default is Bilinear, so we need to explicitly set a different value
            // to ensure WillTextureSettingsChange detects a change
            TextureImporter initialImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(initialImporter != null, "Initial importer not found");
            initialImporter.filterMode = FilterMode.Point;
            initialImporter.SaveAndReimport();

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
                "Expected detection when matching profile has apply flags and initial filter mode differs. Path="
                    + path
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

        private static IEnumerable<TestCaseData> FilterModeMatchingCases()
        {
            FilterMode[] allModes = (FilterMode[])Enum.GetValues(typeof(FilterMode));
            for (int i = 0; i < allModes.Length; i++)
            {
                FilterMode spriteMode = allModes[i];
                for (int j = 0; j < allModes.Length; j++)
                {
                    FilterMode configMode = allModes[j];
                    bool expectChange = spriteMode != configMode;
                    string resultSuffix = expectChange ? "ReturnsTrue" : "ReturnsFalse";
                    if (spriteMode == configMode)
                    {
                        yield return new TestCaseData(spriteMode, configMode, expectChange).SetName(
                            "FilterMode.Match." + spriteMode + "." + resultSuffix
                        );
                    }
                    else
                    {
                        yield return new TestCaseData(spriteMode, configMode, expectChange).SetName(
                            "FilterMode.Differ."
                                + spriteMode
                                + "To"
                                + configMode
                                + "."
                                + resultSuffix
                        );
                    }
                }
            }
        }

        [Test]
        [TestCaseSource(nameof(FilterModeMatchingCases))]
        public void WillTextureSettingsChangeDetectsFilterModeCorrectly(
            FilterMode spriteFilterMode,
            FilterMode configuredFilterMode,
            bool expectedResult
        )
        {
            string path = CreatePng("filtermode_test", asSprite: true);
            _assetPath = path;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importer != null, "Importer not found");
            importer.filterMode = spriteFilterMode;
            importer.SaveAndReimport();

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = configuredFilterMode,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, prepared);
            Assert.AreEqual(
                expectedResult,
                willChange,
                $"FilterMode sprite={spriteFilterMode} config={configuredFilterMode}"
            );
        }

        private static IEnumerable<TestCaseData> WrapModeMatchingCases()
        {
            yield return new TestCaseData(
                TextureWrapMode.Clamp,
                TextureWrapMode.Clamp,
                false
            ).SetName("WrapMode.Match.Clamp.ReturnsFalse");
            yield return new TestCaseData(
                TextureWrapMode.Repeat,
                TextureWrapMode.Repeat,
                false
            ).SetName("WrapMode.Match.Repeat.ReturnsFalse");
            yield return new TestCaseData(
                TextureWrapMode.Mirror,
                TextureWrapMode.Mirror,
                false
            ).SetName("WrapMode.Match.Mirror.ReturnsFalse");
            yield return new TestCaseData(
                TextureWrapMode.MirrorOnce,
                TextureWrapMode.MirrorOnce,
                false
            ).SetName("WrapMode.Match.MirrorOnce.ReturnsFalse");
            yield return new TestCaseData(
                TextureWrapMode.Clamp,
                TextureWrapMode.Repeat,
                true
            ).SetName("WrapMode.Differ.ClampToRepeat.ReturnsTrue");
            yield return new TestCaseData(
                TextureWrapMode.Repeat,
                TextureWrapMode.Clamp,
                true
            ).SetName("WrapMode.Differ.RepeatToClamp.ReturnsTrue");
            yield return new TestCaseData(
                TextureWrapMode.Clamp,
                TextureWrapMode.Mirror,
                true
            ).SetName("WrapMode.Differ.ClampToMirror.ReturnsTrue");
            yield return new TestCaseData(
                TextureWrapMode.Mirror,
                TextureWrapMode.MirrorOnce,
                true
            ).SetName("WrapMode.Differ.MirrorToMirrorOnce.ReturnsTrue");
        }

        [Test]
        [TestCaseSource(nameof(WrapModeMatchingCases))]
        public void WillTextureSettingsChangeDetectsWrapModeCorrectly(
            TextureWrapMode spriteWrapMode,
            TextureWrapMode configuredWrapMode,
            bool expectedResult
        )
        {
            string path = CreatePng("wrapmode_test", asSprite: true);
            _assetPath = path;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importer != null, "Importer not found");
            importer.wrapMode = spriteWrapMode;
            importer.SaveAndReimport();

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyWrapMode = true,
                    wrapMode = configuredWrapMode,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, prepared);
            Assert.AreEqual(
                expectedResult,
                willChange,
                $"WrapMode sprite={spriteWrapMode} config={configuredWrapMode}"
            );
        }

        private static IEnumerable<TestCaseData> PixelsPerUnitMatchingCases()
        {
            yield return new TestCaseData(100, 100, false).SetName(
                "PPU.Match.Default100.ReturnsFalse"
            );
            yield return new TestCaseData(32, 32, false).SetName("PPU.Match.32.ReturnsFalse");
            yield return new TestCaseData(16, 16, false).SetName("PPU.Match.16.ReturnsFalse");
            yield return new TestCaseData(256, 256, false).SetName("PPU.Match.256.ReturnsFalse");
            yield return new TestCaseData(1, 1, false).SetName("PPU.Match.Minimum1.ReturnsFalse");
            yield return new TestCaseData(100, 32, true).SetName("PPU.Differ.100To32.ReturnsTrue");
            yield return new TestCaseData(32, 100, true).SetName("PPU.Differ.32To100.ReturnsTrue");
            yield return new TestCaseData(100, 1, true).SetName("PPU.Differ.100To1.ReturnsTrue");
            yield return new TestCaseData(16, 256, true).SetName("PPU.Differ.16To256.ReturnsTrue");
        }

        [Test]
        [TestCaseSource(nameof(PixelsPerUnitMatchingCases))]
        public void WillTextureSettingsChangeDetectsPpuCorrectly(
            int spritePpu,
            int configuredPpu,
            bool expectedResult
        )
        {
            string path = CreatePng("ppu_test", asSprite: true);
            _assetPath = path;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importer != null, "Importer not found");
            importer.spritePixelsPerUnit = spritePpu;
            importer.SaveAndReimport();

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyPixelsPerUnit = true,
                    pixelsPerUnit = configuredPpu,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, prepared);
            Assert.AreEqual(
                expectedResult,
                willChange,
                $"PPU sprite={spritePpu} config={configuredPpu}"
            );
        }

        private static IEnumerable<TestCaseData> CompressionMatchingCases()
        {
            yield return new TestCaseData(
                TextureImporterCompression.Uncompressed,
                TextureImporterCompression.Uncompressed,
                false
            ).SetName("Compression.Match.Uncompressed.ReturnsFalse");
            yield return new TestCaseData(
                TextureImporterCompression.Compressed,
                TextureImporterCompression.Compressed,
                false
            ).SetName("Compression.Match.Compressed.ReturnsFalse");
            yield return new TestCaseData(
                TextureImporterCompression.CompressedHQ,
                TextureImporterCompression.CompressedHQ,
                false
            ).SetName("Compression.Match.CompressedHQ.ReturnsFalse");
            yield return new TestCaseData(
                TextureImporterCompression.CompressedLQ,
                TextureImporterCompression.CompressedLQ,
                false
            ).SetName("Compression.Match.CompressedLQ.ReturnsFalse");
            yield return new TestCaseData(
                TextureImporterCompression.Uncompressed,
                TextureImporterCompression.Compressed,
                true
            ).SetName("Compression.Differ.UncompressedToCompressed.ReturnsTrue");
            yield return new TestCaseData(
                TextureImporterCompression.Compressed,
                TextureImporterCompression.Uncompressed,
                true
            ).SetName("Compression.Differ.CompressedToUncompressed.ReturnsTrue");
            yield return new TestCaseData(
                TextureImporterCompression.Compressed,
                TextureImporterCompression.CompressedHQ,
                true
            ).SetName("Compression.Differ.CompressedToHQ.ReturnsTrue");
            yield return new TestCaseData(
                TextureImporterCompression.CompressedHQ,
                TextureImporterCompression.CompressedLQ,
                true
            ).SetName("Compression.Differ.HQToLQ.ReturnsTrue");
        }

        [Test]
        [TestCaseSource(nameof(CompressionMatchingCases))]
        public void WillTextureSettingsChangeDetectsCompressionCorrectly(
            TextureImporterCompression spriteCompression,
            TextureImporterCompression configuredCompression,
            bool expectedResult
        )
        {
            string path = CreatePng("compression_test", asSprite: true);
            _assetPath = path;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importer != null, "Importer not found");
            importer.textureCompression = spriteCompression;
            importer.SaveAndReimport();

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyCompression = true,
                    compressionLevel = configuredCompression,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, prepared);
            Assert.AreEqual(
                expectedResult,
                willChange,
                $"Compression sprite={spriteCompression} config={configuredCompression}"
            );
        }

        private static IEnumerable<TestCaseData> MipMapMatchingCases()
        {
            yield return new TestCaseData(true, true, false).SetName(
                "MipMaps.Match.Enabled.ReturnsFalse"
            );
            yield return new TestCaseData(false, false, false).SetName(
                "MipMaps.Match.Disabled.ReturnsFalse"
            );
            yield return new TestCaseData(true, false, true).SetName(
                "MipMaps.Differ.EnabledToDisabled.ReturnsTrue"
            );
            yield return new TestCaseData(false, true, true).SetName(
                "MipMaps.Differ.DisabledToEnabled.ReturnsTrue"
            );
        }

        [Test]
        [TestCaseSource(nameof(MipMapMatchingCases))]
        public void WillTextureSettingsChangeDetectsMipMapsCorrectly(
            bool spriteMipMaps,
            bool configuredMipMaps,
            bool expectedResult
        )
        {
            string path = CreatePng("mipmaps_test", asSprite: true);
            _assetPath = path;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importer != null, "Importer not found");
            importer.mipmapEnabled = spriteMipMaps;
            importer.SaveAndReimport();

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyGenerateMipMaps = true,
                    generateMipMaps = configuredMipMaps,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, prepared);
            Assert.AreEqual(
                expectedResult,
                willChange,
                $"MipMaps sprite={spriteMipMaps} config={configuredMipMaps}"
            );
        }

        private static IEnumerable<TestCaseData> CrunchCompressionMatchingCases()
        {
            yield return new TestCaseData(true, true, false).SetName(
                "Crunch.Match.Enabled.ReturnsFalse"
            );
            yield return new TestCaseData(false, false, false).SetName(
                "Crunch.Match.Disabled.ReturnsFalse"
            );
            yield return new TestCaseData(true, false, true).SetName(
                "Crunch.Differ.EnabledToDisabled.ReturnsTrue"
            );
            yield return new TestCaseData(false, true, true).SetName(
                "Crunch.Differ.DisabledToEnabled.ReturnsTrue"
            );
        }

        [Test]
        [TestCaseSource(nameof(CrunchCompressionMatchingCases))]
        public void WillTextureSettingsChangeDetectsCrunchCompressionCorrectly(
            bool spriteCrunch,
            bool configuredCrunch,
            bool expectedResult
        )
        {
            string path = CreatePng("crunch_test", asSprite: true);
            _assetPath = path;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importer != null, "Importer not found");
            importer.crunchedCompression = spriteCrunch;
            importer.SaveAndReimport();

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyCrunchCompression = true,
                    useCrunchCompression = configuredCrunch,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, prepared);
            Assert.AreEqual(
                expectedResult,
                willChange,
                $"CrunchCompression sprite={spriteCrunch} config={configuredCrunch}"
            );
        }

        private static IEnumerable<TestCaseData> ReadWriteMatchingCases()
        {
            yield return new TestCaseData(true, true, false).SetName(
                "ReadWrite.Match.Enabled.ReturnsFalse"
            );
            yield return new TestCaseData(false, false, false).SetName(
                "ReadWrite.Match.Disabled.ReturnsFalse"
            );
            yield return new TestCaseData(true, false, true).SetName(
                "ReadWrite.Differ.EnabledToDisabled.ReturnsTrue"
            );
            yield return new TestCaseData(false, true, true).SetName(
                "ReadWrite.Differ.DisabledToEnabled.ReturnsTrue"
            );
        }

        [Test]
        [TestCaseSource(nameof(ReadWriteMatchingCases))]
        public void WillTextureSettingsChangeDetectsReadWriteCorrectly(
            bool spriteReadWrite,
            bool configuredReadWrite,
            bool expectedResult
        )
        {
            string path = CreatePng("readwrite_test", asSprite: true);
            _assetPath = path;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importer != null, "Importer not found");
            importer.isReadable = spriteReadWrite;
            importer.SaveAndReimport();

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyReadWriteEnabled = true,
                    readWriteEnabled = configuredReadWrite,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, prepared);
            Assert.AreEqual(
                expectedResult,
                willChange,
                $"ReadWrite sprite={spriteReadWrite} config={configuredReadWrite}"
            );
        }

        private static IEnumerable<TestCaseData> AlphaTransparencyMatchingCases()
        {
            yield return new TestCaseData(true, true, false).SetName(
                "AlphaTransparency.Match.Enabled.ReturnsFalse"
            );
            yield return new TestCaseData(false, false, false).SetName(
                "AlphaTransparency.Match.Disabled.ReturnsFalse"
            );
            yield return new TestCaseData(true, false, true).SetName(
                "AlphaTransparency.Differ.EnabledToDisabled.ReturnsTrue"
            );
            yield return new TestCaseData(false, true, true).SetName(
                "AlphaTransparency.Differ.DisabledToEnabled.ReturnsTrue"
            );
        }

        [Test]
        [TestCaseSource(nameof(AlphaTransparencyMatchingCases))]
        public void WillTextureSettingsChangeDetectsAlphaTransparencyCorrectly(
            bool spriteAlpha,
            bool configuredAlpha,
            bool expectedResult
        )
        {
            string path = CreatePng("alpha_test", asSprite: true);
            _assetPath = path;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importer != null, "Importer not found");
            importer.alphaIsTransparency = spriteAlpha;
            importer.SaveAndReimport();

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyAlphaIsTransparency = true,
                    alphaIsTransparency = configuredAlpha,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, prepared);
            Assert.AreEqual(
                expectedResult,
                willChange,
                $"AlphaTransparency sprite={spriteAlpha} config={configuredAlpha}"
            );
        }

        private static IEnumerable<TestCaseData> SpriteModeMatchingCases()
        {
            yield return new TestCaseData(
                SpriteImportMode.Single,
                SpriteImportMode.Single,
                false
            ).SetName("SpriteMode.Match.Single.ReturnsFalse");
            yield return new TestCaseData(
                SpriteImportMode.Multiple,
                SpriteImportMode.Multiple,
                false
            ).SetName("SpriteMode.Match.Multiple.ReturnsFalse");
            yield return new TestCaseData(
                SpriteImportMode.Polygon,
                SpriteImportMode.Polygon,
                false
            ).SetName("SpriteMode.Match.Polygon.ReturnsFalse");
            yield return new TestCaseData(
                SpriteImportMode.Single,
                SpriteImportMode.Multiple,
                true
            ).SetName("SpriteMode.Differ.SingleToMultiple.ReturnsTrue");
            yield return new TestCaseData(
                SpriteImportMode.Multiple,
                SpriteImportMode.Single,
                true
            ).SetName("SpriteMode.Differ.MultipleToSingle.ReturnsTrue");
            yield return new TestCaseData(
                SpriteImportMode.Single,
                SpriteImportMode.Polygon,
                true
            ).SetName("SpriteMode.Differ.SingleToPolygon.ReturnsTrue");
            yield return new TestCaseData(
                SpriteImportMode.Polygon,
                SpriteImportMode.Multiple,
                true
            ).SetName("SpriteMode.Differ.PolygonToMultiple.ReturnsTrue");
        }

        [Test]
        [TestCaseSource(nameof(SpriteModeMatchingCases))]
        public void WillTextureSettingsChangeDetectsSpriteModeCorrectly(
            SpriteImportMode spriteSpriteMode,
            SpriteImportMode configuredSpriteMode,
            bool expectedResult
        )
        {
            string path = CreatePng("spritemode_test", asSprite: true);
            _assetPath = path;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importer != null, "Importer not found");
            importer.spriteImportMode = spriteSpriteMode;
            importer.SaveAndReimport();

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applySpriteMode = true,
                    spriteMode = configuredSpriteMode,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, prepared);
            Assert.AreEqual(
                expectedResult,
                willChange,
                $"SpriteMode sprite={spriteSpriteMode} config={configuredSpriteMode}"
            );
        }

        private static IEnumerable<TestCaseData> TextureTypeMatchingCases()
        {
            yield return new TestCaseData(
                TextureImporterType.Sprite,
                TextureImporterType.Sprite,
                false
            ).SetName("TextureType.Match.Sprite.ReturnsFalse");
            yield return new TestCaseData(
                TextureImporterType.Default,
                TextureImporterType.Default,
                false
            ).SetName("TextureType.Match.Default.ReturnsFalse");
            yield return new TestCaseData(
                TextureImporterType.NormalMap,
                TextureImporterType.NormalMap,
                false
            ).SetName("TextureType.Match.NormalMap.ReturnsFalse");
            yield return new TestCaseData(
                TextureImporterType.GUI,
                TextureImporterType.GUI,
                false
            ).SetName("TextureType.Match.GUI.ReturnsFalse");
            yield return new TestCaseData(
                TextureImporterType.Sprite,
                TextureImporterType.Default,
                true
            ).SetName("TextureType.Differ.SpriteToDefault.ReturnsTrue");
            yield return new TestCaseData(
                TextureImporterType.Default,
                TextureImporterType.Sprite,
                true
            ).SetName("TextureType.Differ.DefaultToSprite.ReturnsTrue");
            yield return new TestCaseData(
                TextureImporterType.Sprite,
                TextureImporterType.NormalMap,
                true
            ).SetName("TextureType.Differ.SpriteToNormalMap.ReturnsTrue");
            yield return new TestCaseData(
                TextureImporterType.Sprite,
                TextureImporterType.GUI,
                true
            ).SetName("TextureType.Differ.SpriteToGUI.ReturnsTrue");
        }

        [Test]
        [TestCaseSource(nameof(TextureTypeMatchingCases))]
        public void WillTextureSettingsChangeDetectsTextureTypeCorrectly(
            TextureImporterType spriteTextureType,
            TextureImporterType configuredTextureType,
            bool expectedResult
        )
        {
            string path = CreatePng("texturetype_test", asSprite: false);
            _assetPath = path;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importer != null, "Importer not found");
            importer.textureType = spriteTextureType;
            importer.SaveAndReimport();

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyTextureType = true,
                    textureType = configuredTextureType,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, prepared);
            Assert.AreEqual(
                expectedResult,
                willChange,
                $"TextureType sprite={spriteTextureType} config={configuredTextureType}"
            );
        }

        private static IEnumerable<TestCaseData> ExtrudeEdgesMatchingCases()
        {
            yield return new TestCaseData((uint)0, (uint)0, false).SetName(
                "ExtrudeEdges.Match.Zero.ReturnsFalse"
            );
            yield return new TestCaseData((uint)1, (uint)1, false).SetName(
                "ExtrudeEdges.Match.One.ReturnsFalse"
            );
            yield return new TestCaseData((uint)16, (uint)16, false).SetName(
                "ExtrudeEdges.Match.16.ReturnsFalse"
            );
            yield return new TestCaseData((uint)32, (uint)32, false).SetName(
                "ExtrudeEdges.Match.Max32.ReturnsFalse"
            );
            yield return new TestCaseData((uint)0, (uint)1, true).SetName(
                "ExtrudeEdges.Differ.ZeroTo1.ReturnsTrue"
            );
            yield return new TestCaseData((uint)1, (uint)0, true).SetName(
                "ExtrudeEdges.Differ.1ToZero.ReturnsTrue"
            );
            yield return new TestCaseData((uint)1, (uint)16, true).SetName(
                "ExtrudeEdges.Differ.1To16.ReturnsTrue"
            );
            yield return new TestCaseData((uint)16, (uint)32, true).SetName(
                "ExtrudeEdges.Differ.16To32.ReturnsTrue"
            );
        }

        [Test]
        [TestCaseSource(nameof(ExtrudeEdgesMatchingCases))]
        public void WillTextureSettingsChangeDetectsExtrudeEdgesCorrectly(
            uint spriteExtrude,
            uint configuredExtrude,
            bool expectedResult
        )
        {
            string path = CreatePng("extrude_test", asSprite: true);
            _assetPath = path;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importer != null, "Importer not found");
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteExtrude = spriteExtrude;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyExtrudeEdges = true,
                    extrudeEdges = configuredExtrude,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, prepared);
            Assert.AreEqual(
                expectedResult,
                willChange,
                $"ExtrudeEdges sprite={spriteExtrude} config={configuredExtrude}"
            );
        }

        [Test]
        public void WillTextureSettingsChangeReturnsFalseWhenAllSettingsMatch()
        {
            string path = CreatePng("all_match_test", asSprite: true);
            _assetPath = path;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importer != null, "Importer not found");
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false;
            importer.crunchedCompression = false;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.SaveAndReimport();

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Point,
                    applyWrapMode = true,
                    wrapMode = TextureWrapMode.Clamp,
                    applyPixelsPerUnit = true,
                    pixelsPerUnit = 100,
                    applyGenerateMipMaps = true,
                    generateMipMaps = false,
                    applyCrunchCompression = true,
                    useCrunchCompression = false,
                    applyCompression = true,
                    compressionLevel = TextureImporterCompression.Compressed,
                    applyAlphaIsTransparency = true,
                    alphaIsTransparency = true,
                    applyReadWriteEnabled = true,
                    readWriteEnabled = false,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, prepared);
            Assert.IsFalse(
                willChange,
                "Expected no change when all configured settings match sprite settings"
            );
        }

        [Test]
        public void TryUpdateTextureSettingsReturnsFalseWhenAllSettingsMatch()
        {
            string path = CreatePng("try_update_all_match", asSprite: true);
            _assetPath = path;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importer != null, "Importer not found");
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.spritePixelsPerUnit = 32;
            importer.SaveAndReimport();

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Bilinear,
                    applyWrapMode = true,
                    wrapMode = TextureWrapMode.Repeat,
                    applyPixelsPerUnit = true,
                    pixelsPerUnit = 32,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool changed = SpriteSettingsApplierAPI.TryUpdateTextureSettings(
                path,
                prepared,
                out TextureImporter outImporter
            );
            Assert.IsFalse(
                changed,
                "Expected no change when all configured settings match sprite settings"
            );
            Assert.IsTrue(outImporter != null, "Importer should still be returned");
        }

        [Test]
        public void WillTextureSettingsChangeReturnsTrueWhenOnlyOneSettingDiffers()
        {
            string path = CreatePng("one_differs_test", asSprite: true);
            _assetPath = path;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importer != null, "Importer not found");
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 100;
            importer.SaveAndReimport();

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Point,
                    applyWrapMode = true,
                    wrapMode = TextureWrapMode.Clamp,
                    applyPixelsPerUnit = true,
                    pixelsPerUnit = 64,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, prepared);
            Assert.IsTrue(
                willChange,
                "Expected change when at least one configured setting differs from sprite"
            );
        }

        [Test]
        public void TryUpdateTextureSettingsReturnsTrueWhenOnlyOneSettingDiffers()
        {
            string path = CreatePng("try_update_one_differs", asSprite: true);
            _assetPath = path;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importer != null, "Importer not found");
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.spritePixelsPerUnit = 32;
            importer.SaveAndReimport();

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Bilinear,
                    applyWrapMode = true,
                    wrapMode = TextureWrapMode.Clamp,
                    applyPixelsPerUnit = true,
                    pixelsPerUnit = 32,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool changed = SpriteSettingsApplierAPI.TryUpdateTextureSettings(
                path,
                prepared,
                out TextureImporter outImporter
            );
            Assert.IsTrue(
                changed,
                "Expected change when at least one configured setting differs from sprite"
            );
            Assert.IsTrue(outImporter != null, "Importer should be returned");
        }

        [Test]
        public void WillTextureSettingsChangeReturnsFalseWhenNoApplyFlagsEnabled()
        {
            string path = CreatePng("no_flags_test", asSprite: true);
            _assetPath = path;

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyFilterMode = false,
                    filterMode = FilterMode.Trilinear,
                    applyWrapMode = false,
                    wrapMode = TextureWrapMode.Mirror,
                    applyPixelsPerUnit = false,
                    pixelsPerUnit = 999,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, prepared);
            Assert.IsFalse(
                willChange,
                "Expected no change when no apply flags are enabled, regardless of configured values"
            );
        }

        [Test]
        public void TryUpdateTextureSettingsReturnsFalseWhenNoApplyFlagsEnabled()
        {
            string path = CreatePng("try_update_no_flags", asSprite: true);
            _assetPath = path;

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyFilterMode = false,
                    filterMode = FilterMode.Trilinear,
                    applyWrapMode = false,
                    wrapMode = TextureWrapMode.Mirror,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool changed = SpriteSettingsApplierAPI.TryUpdateTextureSettings(
                path,
                prepared,
                out TextureImporter outImporter
            );
            Assert.IsFalse(
                changed,
                "Expected no change when no apply flags are enabled, regardless of configured values"
            );
            Assert.IsTrue(outImporter != null, "Importer should still be returned");
        }

        [Test]
        public void WillTextureSettingsChangeReturnsFalseWhenNoProfileMatches()
        {
            string path = CreatePng("nomatch_test", asSprite: true);
            _assetPath = path;

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.NameContains,
                    matchPattern = "this_does_not_exist",
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Trilinear,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, prepared);
            Assert.IsFalse(willChange, "Expected no change when no profile matches the asset");
        }

        [Test]
        public void TryUpdateTextureSettingsReturnsFalseWhenNoProfileMatches()
        {
            string path = CreatePng("try_update_nomatch", asSprite: true);
            _assetPath = path;

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.NameContains,
                    matchPattern = "this_does_not_exist",
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Trilinear,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool changed = SpriteSettingsApplierAPI.TryUpdateTextureSettings(
                path,
                prepared,
                out TextureImporter outImporter
            );
            Assert.IsFalse(changed, "Expected no change when no profile matches the asset");
            // The importer is still returned even when no profile matches,
            // allowing the caller to use it for other purposes if needed
            Assert.IsTrue(
                outImporter != null,
                "Importer should still be returned even when no profile matches"
            );
        }

        [Test]
        public void WillTextureSettingsChangeReturnsFalseForNullPath()
        {
            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Trilinear,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(null, prepared);
            Assert.IsFalse(willChange, "Expected no change for null path");
        }

        [Test]
        public void WillTextureSettingsChangeReturnsFalseForEmptyPath()
        {
            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Trilinear,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(
                string.Empty,
                prepared
            );
            Assert.IsFalse(willChange, "Expected no change for empty path");
        }

        [Test]
        public void WillTextureSettingsChangeReturnsFalseForNonExistentPath()
        {
            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Trilinear,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(
                "Assets/NonExistent/fake.png",
                prepared
            );
            Assert.IsFalse(willChange, "Expected no change for non-existent path");
        }

        [Test]
        public void WillTextureSettingsChangeReturnsFalseForNullPreparedProfiles()
        {
            string path = CreatePng("null_profiles_test", asSprite: true);
            _assetPath = path;

            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, null);
            Assert.IsFalse(willChange, "Expected no change for null prepared profiles");
        }

        [Test]
        public void WillTextureSettingsChangeReturnsFalseForEmptyPreparedProfiles()
        {
            string path = CreatePng("empty_profiles_test", asSprite: true);
            _assetPath = path;

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(new List<SpriteSettings>());
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, prepared);
            Assert.IsFalse(willChange, "Expected no change for empty prepared profiles list");
        }

        [Test]
        public void TryUpdateTextureSettingsReturnsFalseForNullPath()
        {
            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Trilinear,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool changed = SpriteSettingsApplierAPI.TryUpdateTextureSettings(
                null,
                prepared,
                out TextureImporter outImporter
            );
            Assert.IsFalse(changed, "Expected no change for null path");
            Assert.IsTrue(outImporter == null, "Importer should be null for null path");
        }

        [Test]
        public void TryUpdateTextureSettingsReturnsFalseForEmptyPath()
        {
            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Trilinear,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool changed = SpriteSettingsApplierAPI.TryUpdateTextureSettings(
                string.Empty,
                prepared,
                out TextureImporter outImporter
            );
            Assert.IsFalse(changed, "Expected no change for empty path");
            Assert.IsTrue(outImporter == null, "Importer should be null for empty path");
        }

        [Test]
        public void TryUpdateTextureSettingsAppliesChangesCorrectly()
        {
            string path = CreatePng("apply_changes_test", asSprite: true);
            _assetPath = path;

            TextureImporter importerBefore = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importerBefore != null, "Importer not found");
            importerBefore.filterMode = FilterMode.Point;
            importerBefore.wrapMode = TextureWrapMode.Clamp;
            importerBefore.spritePixelsPerUnit = 100;
            importerBefore.SaveAndReimport();

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Trilinear,
                    applyWrapMode = true,
                    wrapMode = TextureWrapMode.Mirror,
                    applyPixelsPerUnit = true,
                    pixelsPerUnit = 64,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool changed = SpriteSettingsApplierAPI.TryUpdateTextureSettings(
                path,
                prepared,
                out TextureImporter outImporter
            );
            Assert.IsTrue(changed, "Expected changes to be applied");
            Assert.IsTrue(outImporter != null, "Importer should be returned");
            outImporter.SaveAndReimport();

            TextureImporter importerAfter = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importerAfter != null, "Importer after not found");
            Assert.AreEqual(
                FilterMode.Trilinear,
                importerAfter.filterMode,
                "FilterMode should be updated"
            );
            Assert.AreEqual(
                TextureWrapMode.Mirror,
                importerAfter.wrapMode,
                "WrapMode should be updated"
            );
            Assert.AreEqual(64, importerAfter.spritePixelsPerUnit, "PPU should be updated");
        }

        private static IEnumerable<TestCaseData> PivotMatchingCases()
        {
            yield return new TestCaseData(
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                false
            ).SetName("Pivot.Match.Center.ReturnsFalse");
            yield return new TestCaseData(new Vector2(0f, 0f), new Vector2(0f, 0f), false).SetName(
                "Pivot.Match.BottomLeft.ReturnsFalse"
            );
            yield return new TestCaseData(new Vector2(1f, 1f), new Vector2(1f, 1f), false).SetName(
                "Pivot.Match.TopRight.ReturnsFalse"
            );
            yield return new TestCaseData(
                new Vector2(0.25f, 0.75f),
                new Vector2(0.25f, 0.75f),
                false
            ).SetName("Pivot.Match.Custom.ReturnsFalse");
            yield return new TestCaseData(
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                false
            ).SetName("Pivot.Match.MiddleLeft.ReturnsFalse");
            yield return new TestCaseData(
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                false
            ).SetName("Pivot.Match.MiddleRight.ReturnsFalse");
            yield return new TestCaseData(
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                false
            ).SetName("Pivot.Match.BottomCenter.ReturnsFalse");
            yield return new TestCaseData(
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                false
            ).SetName("Pivot.Match.TopCenter.ReturnsFalse");
            yield return new TestCaseData(
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f),
                true
            ).SetName("Pivot.Differ.CenterToBottomLeft.ReturnsTrue");
            yield return new TestCaseData(new Vector2(0f, 0f), new Vector2(1f, 1f), true).SetName(
                "Pivot.Differ.BottomLeftToTopRight.ReturnsTrue"
            );
            yield return new TestCaseData(
                new Vector2(0.5f, 0.5f),
                new Vector2(0.25f, 0.75f),
                true
            ).SetName("Pivot.Differ.CenterToCustom.ReturnsTrue");
            yield return new TestCaseData(
                new Vector2(0.3f, 0.7f),
                new Vector2(0.7f, 0.3f),
                true
            ).SetName("Pivot.Differ.CustomToCustom.ReturnsTrue");
            yield return new TestCaseData(
                new Vector2(0f, 0.5f),
                new Vector2(1f, 0.5f),
                true
            ).SetName("Pivot.Differ.MiddleLeftToMiddleRight.ReturnsTrue");
            yield return new TestCaseData(
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 1f),
                true
            ).SetName("Pivot.Differ.BottomCenterToTopCenter.ReturnsTrue");
        }

        [Test]
        [TestCaseSource(nameof(PivotMatchingCases))]
        public void WillTextureSettingsChangeDetectsPivotCorrectly(
            Vector2 spritePivot,
            Vector2 configuredPivot,
            bool expectedResult
        )
        {
            string path = CreatePng("pivot_test", asSprite: true);
            _assetPath = path;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importer != null, "Importer not found");
            importer.spritePivot = spritePivot;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = spritePivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyPivot = true,
                    pivot = configuredPivot,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, prepared);
            Assert.AreEqual(
                expectedResult,
                willChange,
                $"Pivot sprite={spritePivot} config={configuredPivot}"
            );
        }

        [Test]
        public void WillTextureSettingsChangeDetectsPivotAlignmentChange()
        {
            string path = CreatePng("pivot_alignment_test", asSprite: true);
            _assetPath = path;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importer != null, "Importer not found");
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyPivot = true,
                    pivot = new Vector2(0.5f, 0.5f),
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool willChange = SpriteSettingsApplierAPI.WillTextureSettingsChange(path, prepared);
            Assert.IsTrue(
                willChange,
                "Expected change when sprite alignment is not Custom, even if pivot matches"
            );
        }

        [Test]
        public void TryUpdateTextureSettingsAppliesPivotCorrectly()
        {
            string path = CreatePng("pivot_apply_test", asSprite: true);
            _assetPath = path;

            TextureImporter importerBefore = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importerBefore != null, "Importer not found");
            importerBefore.spritePivot = new Vector2(0.5f, 0.5f);
            TextureImporterSettings settingsBefore = new TextureImporterSettings();
            importerBefore.ReadTextureSettings(settingsBefore);
            settingsBefore.spriteAlignment = (int)SpriteAlignment.Custom;
            settingsBefore.spritePivot = new Vector2(0.5f, 0.5f);
            importerBefore.SetTextureSettings(settingsBefore);
            importerBefore.SaveAndReimport();

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyPivot = true,
                    pivot = new Vector2(0.25f, 0.75f),
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            bool changed = SpriteSettingsApplierAPI.TryUpdateTextureSettings(
                path,
                prepared,
                out TextureImporter outImporter
            );
            Assert.IsTrue(changed, "Expected changes to be applied");
            Assert.IsTrue(outImporter != null, "Importer should be returned");
            outImporter.SaveAndReimport();

            TextureImporter importerAfter = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importerAfter != null, "Importer after not found");
            Assert.AreEqual(
                new Vector2(0.25f, 0.75f),
                importerAfter.spritePivot,
                "Pivot should be updated"
            );

            TextureImporterSettings settingsAfter = new TextureImporterSettings();
            importerAfter.ReadTextureSettings(settingsAfter);
            Assert.AreEqual(
                (int)SpriteAlignment.Custom,
                settingsAfter.spriteAlignment,
                "Alignment should be set to Custom"
            );
        }

        [Test]
        public void WillTextureSettingsChangeConsistentWithTryUpdate()
        {
            string path = CreatePng("consistency_test", asSprite: true);
            _assetPath = path;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importer != null, "Importer not found");
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();

            List<SpriteSettings> matchingProfiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Point,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> preparedMatch =
                SpriteSettingsApplierAPI.PrepareProfiles(matchingProfiles);
            bool willChangeMatch = SpriteSettingsApplierAPI.WillTextureSettingsChange(
                path,
                preparedMatch
            );
            bool changedMatch = SpriteSettingsApplierAPI.TryUpdateTextureSettings(
                path,
                preparedMatch,
                out TextureImporter _
            );
            Assert.AreEqual(
                willChangeMatch,
                changedMatch,
                "WillTextureSettingsChange and TryUpdateTextureSettings should agree when settings match"
            );

            List<SpriteSettings> differingProfiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Bilinear,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> preparedDiffer =
                SpriteSettingsApplierAPI.PrepareProfiles(differingProfiles);
            bool willChangeDiffer = SpriteSettingsApplierAPI.WillTextureSettingsChange(
                path,
                preparedDiffer
            );
            bool changedDiffer = SpriteSettingsApplierAPI.TryUpdateTextureSettings(
                path,
                preparedDiffer,
                out TextureImporter _
            );
            Assert.AreEqual(
                willChangeDiffer,
                changedDiffer,
                "WillTextureSettingsChange and TryUpdateTextureSettings should agree when settings differ"
            );
        }

        [Test]
        public void WillTextureSettingsChangeBufferReuseWorks()
        {
            string path1 = CreatePng("buffer_reuse_1", asSprite: true);
            _assetPath = path1;

            TextureImporter importer1 = AssetImporter.GetAtPath(path1) as TextureImporter;
            Assert.IsTrue(importer1 != null, "Importer not found for first asset");
            importer1.filterMode = FilterMode.Point;
            importer1.SaveAndReimport();

            string path2 = null;
            try
            {
                path2 = Path.Combine(TestFolder, "buffer_reuse_2.png");
                Texture2D tex2 = Track(new Texture2D(4, 4, TextureFormat.RGBA32, false));
                byte[] png2 = tex2.EncodeToPNG();
                File.WriteAllBytes(path2, png2);
                AssetDatabase.ImportAsset(path2);
                TextureImporter importer2 = AssetImporter.GetAtPath(path2) as TextureImporter;
                Assert.IsTrue(importer2 != null, "Importer not found for second asset");
                importer2.textureType = TextureImporterType.Sprite;
                importer2.filterMode = FilterMode.Bilinear;
                importer2.SaveAndReimport();

                List<SpriteSettings> profiles = new()
                {
                    new SpriteSettings
                    {
                        matchBy = SpriteSettings.MatchMode.Any,
                        priority = 1,
                        applyFilterMode = true,
                        filterMode = FilterMode.Point,
                    },
                };

                List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                    SpriteSettingsApplierAPI.PrepareProfiles(profiles);

                TextureImporterSettings sharedBuffer = new TextureImporterSettings();
                bool willChange1 = SpriteSettingsApplierAPI.WillTextureSettingsChange(
                    path1,
                    prepared,
                    sharedBuffer
                );
                Assert.IsFalse(
                    willChange1,
                    "First asset should not need change (already Point filter mode)"
                );

                bool willChange2 = SpriteSettingsApplierAPI.WillTextureSettingsChange(
                    path2,
                    prepared,
                    sharedBuffer
                );
                Assert.IsTrue(willChange2, "Second asset should need change (Bilinear to Point)");

                bool willChange1Again = SpriteSettingsApplierAPI.WillTextureSettingsChange(
                    path1,
                    prepared,
                    sharedBuffer
                );
                Assert.AreEqual(
                    willChange1,
                    willChange1Again,
                    "Buffer reuse should not affect results for first asset on second call"
                );
            }
            finally
            {
                if (path2 != null)
                {
                    AssetDatabase.DeleteAsset(path2);
                }
            }
        }

        [Test]
        public void TryUpdateTextureSettingsBufferReuseWorks()
        {
            string path1 = CreatePng("try_buffer_reuse_1", asSprite: true);
            _assetPath = path1;

            TextureImporter importer1 = AssetImporter.GetAtPath(path1) as TextureImporter;
            Assert.IsTrue(importer1 != null, "Importer not found for first asset");
            importer1.filterMode = FilterMode.Point;
            importer1.wrapMode = TextureWrapMode.Clamp;
            importer1.SaveAndReimport();

            string path2 = null;
            try
            {
                path2 = Path.Combine(TestFolder, "try_buffer_reuse_2.png");
                Texture2D tex2 = Track(new Texture2D(4, 4, TextureFormat.RGBA32, false));
                byte[] png2 = tex2.EncodeToPNG();
                File.WriteAllBytes(path2, png2);
                AssetDatabase.ImportAsset(path2);
                TextureImporter importer2 = AssetImporter.GetAtPath(path2) as TextureImporter;
                Assert.IsTrue(importer2 != null, "Importer not found for second asset");
                importer2.textureType = TextureImporterType.Sprite;
                importer2.filterMode = FilterMode.Bilinear;
                importer2.wrapMode = TextureWrapMode.Repeat;
                importer2.SaveAndReimport();

                List<SpriteSettings> profiles = new()
                {
                    new SpriteSettings
                    {
                        matchBy = SpriteSettings.MatchMode.Any,
                        priority = 1,
                        applyFilterMode = true,
                        filterMode = FilterMode.Point,
                        applyWrapMode = true,
                        wrapMode = TextureWrapMode.Clamp,
                    },
                };

                List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                    SpriteSettingsApplierAPI.PrepareProfiles(profiles);

                TextureImporterSettings sharedBuffer = new TextureImporterSettings();
                bool changed1 = SpriteSettingsApplierAPI.TryUpdateTextureSettings(
                    path1,
                    prepared,
                    out TextureImporter outImporter1,
                    sharedBuffer
                );
                Assert.IsFalse(changed1, "First asset should not change (settings already match)");
                Assert.IsTrue(outImporter1 != null, "Importer should be returned for first asset");

                bool changed2 = SpriteSettingsApplierAPI.TryUpdateTextureSettings(
                    path2,
                    prepared,
                    out TextureImporter outImporter2,
                    sharedBuffer
                );
                Assert.IsTrue(changed2, "Second asset should change");
                Assert.IsTrue(outImporter2 != null, "Importer should be returned for second asset");
                outImporter2.SaveAndReimport();

                TextureImporter verifyImporter2 = AssetImporter.GetAtPath(path2) as TextureImporter;
                Assert.IsTrue(verifyImporter2 != null, "Verify importer not found");
                Assert.AreEqual(
                    FilterMode.Point,
                    verifyImporter2.filterMode,
                    "Second asset filter mode should be updated"
                );
                Assert.AreEqual(
                    TextureWrapMode.Clamp,
                    verifyImporter2.wrapMode,
                    "Second asset wrap mode should be updated"
                );
            }
            finally
            {
                if (path2 != null)
                {
                    AssetDatabase.DeleteAsset(path2);
                }
            }
        }

        [Test]
        public void TryUpdateTextureSettingsUpdatesBufferCorrectly()
        {
            string path = CreatePng("buffer_update_test", asSprite: true);
            _assetPath = path;

            TextureImporter importerBefore = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importerBefore != null, "Importer not found");
            importerBefore.spritePixelsPerUnit = 100;
            importerBefore.filterMode = FilterMode.Point;
            importerBefore.wrapMode = TextureWrapMode.Clamp;
            importerBefore.SaveAndReimport();

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyPixelsPerUnit = true,
                    pixelsPerUnit = 64,
                    applyFilterMode = true,
                    filterMode = FilterMode.Bilinear,
                    applyWrapMode = true,
                    wrapMode = TextureWrapMode.Repeat,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);

            TextureImporterSettings buffer = new TextureImporterSettings();
            bool changed = SpriteSettingsApplierAPI.TryUpdateTextureSettings(
                path,
                prepared,
                out TextureImporter outImporter,
                buffer
            );
            Assert.IsTrue(changed, "Expected changes to be applied");
            Assert.IsTrue(outImporter != null, "Importer should be returned");

            Assert.AreEqual(
                64,
                buffer.spritePixelsPerUnit,
                "Buffer should contain updated PPU value"
            );
            Assert.AreEqual(
                FilterMode.Bilinear,
                buffer.filterMode,
                "Buffer should contain updated filter mode"
            );
            Assert.AreEqual(
                TextureWrapMode.Repeat,
                buffer.wrapMode,
                "Buffer should contain updated wrap mode"
            );
        }

        [Test]
        public void SamePriorityProfilesUseFirstMatchInList()
        {
            string path = CreatePng("tie_breaker_test", asSprite: true);
            _assetPath = path;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importer != null, "Importer not found for path: " + path);
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.NameContains,
                    matchPattern = "tie",
                    priority = 5,
                    applyFilterMode = true,
                    filterMode = FilterMode.Bilinear,
                },
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.NameContains,
                    matchPattern = "breaker",
                    priority = 5,
                    applyFilterMode = true,
                    filterMode = FilterMode.Trilinear,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            SpriteSettings matched = SpriteSettingsApplierAPI.FindMatchingSettings(path, prepared);

            Assert.IsTrue(
                matched != null,
                "Expected a matching profile to be found for path: " + path
            );
            Assert.AreEqual(
                FilterMode.Bilinear,
                matched.filterMode,
                "Expected first profile with same priority to win. "
                    + "Matched filter mode was "
                    + matched.filterMode
                    + " but expected Bilinear"
            );
        }

        [Test]
        public void SamePriorityProfilesSecondMatchedFirstInListWins()
        {
            string path = CreatePng("priority_order_test", asSprite: true);
            _assetPath = path;

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.NameContains,
                    matchPattern = "nonexistent",
                    priority = 10,
                    applyFilterMode = true,
                    filterMode = FilterMode.Trilinear,
                },
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.NameContains,
                    matchPattern = "order",
                    priority = 5,
                    applyFilterMode = true,
                    filterMode = FilterMode.Bilinear,
                },
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.NameContains,
                    matchPattern = "priority",
                    priority = 5,
                    applyFilterMode = true,
                    filterMode = FilterMode.Point,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            SpriteSettings matched = SpriteSettingsApplierAPI.FindMatchingSettings(path, prepared);

            Assert.IsTrue(matched != null, "Expected a matching profile for path: " + path);
            Assert.AreEqual(
                FilterMode.Bilinear,
                matched.filterMode,
                "With same priority, first matching profile in list (order) should win. "
                    + "Actual filter mode: "
                    + matched.filterMode
            );
        }

        private static IEnumerable<TestCaseData> NullEmptyMatchPatternCases()
        {
            yield return new TestCaseData(SpriteSettings.MatchMode.Any, null, true).SetName(
                "MatchPattern.Null.Any.MatchesAnyFile"
            );

            yield return new TestCaseData(SpriteSettings.MatchMode.Any, "", true).SetName(
                "MatchPattern.Empty.Any.MatchesAnyFile"
            );

            yield return new TestCaseData(SpriteSettings.MatchMode.Any, "   ", true).SetName(
                "MatchPattern.Whitespace.Any.MatchesAnyFile"
            );

            yield return new TestCaseData(
                SpriteSettings.MatchMode.NameContains,
                null,
                false
            ).SetName("MatchPattern.Null.NameContains.NoMatch");

            yield return new TestCaseData(SpriteSettings.MatchMode.NameContains, "", false).SetName(
                "MatchPattern.Empty.NameContains.NoMatch"
            );

            yield return new TestCaseData(
                SpriteSettings.MatchMode.NameContains,
                "   ",
                false
            ).SetName("MatchPattern.Whitespace.NameContains.NoMatch");

            yield return new TestCaseData(
                SpriteSettings.MatchMode.PathContains,
                null,
                false
            ).SetName("MatchPattern.Null.PathContains.NoMatch");

            yield return new TestCaseData(SpriteSettings.MatchMode.PathContains, "", false).SetName(
                "MatchPattern.Empty.PathContains.NoMatch"
            );

            yield return new TestCaseData(SpriteSettings.MatchMode.Extension, null, false).SetName(
                "MatchPattern.Null.Extension.NoMatch"
            );

            yield return new TestCaseData(SpriteSettings.MatchMode.Extension, "", false).SetName(
                "MatchPattern.Empty.Extension.NoMatch"
            );

            yield return new TestCaseData(SpriteSettings.MatchMode.Regex, null, false).SetName(
                "MatchPattern.Null.Regex.NoMatch"
            );

            yield return new TestCaseData(SpriteSettings.MatchMode.Regex, "", false).SetName(
                "MatchPattern.Empty.Regex.NoMatch"
            );
        }

        [Test]
        [TestCaseSource(nameof(NullEmptyMatchPatternCases))]
        public void NullOrEmptyMatchPatternBehavesCorrectly(
            SpriteSettings.MatchMode matchMode,
            string matchPattern,
            bool expectedMatch
        )
        {
            string path = CreatePng("pattern_test", asSprite: true);
            _assetPath = path;

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = matchMode,
                    matchPattern = matchPattern,
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Trilinear,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            SpriteSettings matched = SpriteSettingsApplierAPI.FindMatchingSettings(path, prepared);

            bool didMatch = matched != null;
            Assert.AreEqual(
                expectedMatch,
                didMatch,
                "MatchMode="
                    + matchMode
                    + ", Pattern="
                    + (matchPattern ?? "null")
                    + ". Expected match="
                    + expectedMatch
                    + ", actual match="
                    + didMatch
            );
        }

        private static IEnumerable<TestCaseData> CaseSensitivityCases()
        {
            yield return new TestCaseData(
                SpriteSettings.MatchMode.NameContains,
                "CASE",
                true
            ).SetName("CaseSensitivity.NameContains.UpperCase.Matches");

            yield return new TestCaseData(
                SpriteSettings.MatchMode.NameContains,
                "case",
                true
            ).SetName("CaseSensitivity.NameContains.LowerCase.Matches");

            yield return new TestCaseData(
                SpriteSettings.MatchMode.NameContains,
                "CaSe",
                true
            ).SetName("CaseSensitivity.NameContains.MixedCase.Matches");

            yield return new TestCaseData(
                SpriteSettings.MatchMode.NameContains,
                "SENSITIVE",
                true
            ).SetName("CaseSensitivity.NameContains.UpperSensitive.Matches");

            yield return new TestCaseData(
                SpriteSettings.MatchMode.PathContains,
                "TEMPSPR",
                true
            ).SetName("CaseSensitivity.PathContains.UpperCase.Matches");

            yield return new TestCaseData(
                SpriteSettings.MatchMode.PathContains,
                "tempspr",
                true
            ).SetName("CaseSensitivity.PathContains.LowerCase.Matches");

            yield return new TestCaseData(
                SpriteSettings.MatchMode.PathContains,
                "TeMpSpR",
                true
            ).SetName("CaseSensitivity.PathContains.MixedCase.Matches");

            yield return new TestCaseData(
                SpriteSettings.MatchMode.PathContains,
                "ADDITIONAL",
                true
            ).SetName("CaseSensitivity.PathContains.UpperAdditional.Matches");
        }

        [Test]
        [TestCaseSource(nameof(CaseSensitivityCases))]
        public void MatchModeIsCaseInsensitive(
            SpriteSettings.MatchMode matchMode,
            string matchPattern,
            bool expectedMatch
        )
        {
            string path = CreatePng("case_sensitive_test", asSprite: true);
            _assetPath = path;

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = matchMode,
                    matchPattern = matchPattern,
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Trilinear,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            SpriteSettings matched = SpriteSettingsApplierAPI.FindMatchingSettings(path, prepared);

            bool didMatch = matched != null;
            Assert.AreEqual(
                expectedMatch,
                didMatch,
                "MatchMode="
                    + matchMode
                    + ", Pattern='"
                    + matchPattern
                    + "', Path='"
                    + path
                    + "'. "
                    + "Expected match="
                    + expectedMatch
                    + ", actual match="
                    + didMatch
                    + ". "
                    + "Pattern matching should be case-insensitive."
            );
        }

        [Test]
        public void NameContainsCaseInsensitiveWithActualCaseVariation()
        {
            string path = CreatePng("MixedCASEname", asSprite: true);
            _assetPath = path;

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.NameContains,
                    matchPattern = "mixedcasename",
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Point,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            SpriteSettings matched = SpriteSettingsApplierAPI.FindMatchingSettings(path, prepared);

            Assert.IsTrue(
                matched != null,
                "NameContains should match case-insensitively. "
                    + "File name has 'MixedCASEname', pattern is 'mixedcasename'. "
                    + "Path: "
                    + path
            );
        }

        [Test]
        public void PathContainsCaseInsensitiveWithActualCaseVariation()
        {
            string path = CreatePng("pathtest", asSprite: true);
            _assetPath = path;

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.PathContains,
                    matchPattern = "TEMPSPRITEAPPLIERADDITIONAL",
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Point,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            SpriteSettings matched = SpriteSettingsApplierAPI.FindMatchingSettings(path, prepared);

            Assert.IsTrue(
                matched != null,
                "PathContains should match case-insensitively. "
                    + "Path contains 'TempSpriteApplierAdditional', pattern is 'TEMPSPRITEAPPLIERADDITIONAL'. "
                    + "Path: "
                    + path
            );
        }

        [Test]
        public void ExtensionMatchIsCaseInsensitive()
        {
            string path = CreatePng("extension_case_test", asSprite: true);
            _assetPath = path;

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Extension,
                    matchPattern = ".PNG",
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Trilinear,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            SpriteSettings matched = SpriteSettingsApplierAPI.FindMatchingSettings(path, prepared);

            Assert.IsTrue(
                matched != null,
                "Extension matching should be case-insensitive. "
                    + "File has '.png' extension, pattern is '.PNG'. "
                    + "Path: "
                    + path
            );
        }

        [Test]
        public void ExtensionMatchWithoutDotPrefix()
        {
            string path = CreatePng("extension_no_dot_test", asSprite: true);
            _assetPath = path;

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Extension,
                    matchPattern = "png",
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Trilinear,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            SpriteSettings matched = SpriteSettingsApplierAPI.FindMatchingSettings(path, prepared);

            Assert.IsTrue(
                matched != null,
                "Extension matching should work without leading dot. "
                    + "File has '.png' extension, pattern is 'png' (no dot). "
                    + "Path: "
                    + path
            );
        }

        [Test]
        public void HigherPriorityWinsOverListOrder()
        {
            string path = CreatePng("priority_wins", asSprite: true);
            _assetPath = path;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importer != null, "Importer not found for path: " + path);
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.NameContains,
                    matchPattern = "priority",
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Point,
                },
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.NameContains,
                    matchPattern = "wins",
                    priority = 100,
                    applyFilterMode = true,
                    filterMode = FilterMode.Trilinear,
                },
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.NameContains,
                    matchPattern = "priority_wins",
                    priority = 50,
                    applyFilterMode = true,
                    filterMode = FilterMode.Bilinear,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            SpriteSettings matched = SpriteSettingsApplierAPI.FindMatchingSettings(path, prepared);

            Assert.IsTrue(matched != null, "Expected a matching profile for path: " + path);
            Assert.AreEqual(
                FilterMode.Trilinear,
                matched.filterMode,
                "Profile with highest priority (100) should win regardless of list order. "
                    + "Expected Trilinear, got "
                    + matched.filterMode
                    + ". "
                    + "Priorities were: 1, 100, 50"
            );
        }

        [Test]
        public void FindMatchingSettingsWithNullPreparedListReturnsNull()
        {
            string path = CreatePng("null_prepared", asSprite: true);
            _assetPath = path;

            SpriteSettings matched = SpriteSettingsApplierAPI.FindMatchingSettings(path, null);

            Assert.IsTrue(
                matched == null,
                "FindMatchingSettings should return null when prepared list is null"
            );
        }

        [Test]
        public void FindMatchingSettingsWithEmptyPreparedListReturnsNull()
        {
            string path = CreatePng("empty_prepared", asSprite: true);
            _assetPath = path;

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(new List<SpriteSettings>());
            SpriteSettings matched = SpriteSettingsApplierAPI.FindMatchingSettings(path, prepared);

            Assert.IsTrue(
                matched == null,
                "FindMatchingSettings should return null when prepared list is empty"
            );
        }

        [Test]
        public void FindMatchingSettingsWithNullPathReturnsNull()
        {
            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings { matchBy = SpriteSettings.MatchMode.Any, priority = 1 },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            SpriteSettings matched = SpriteSettingsApplierAPI.FindMatchingSettings(null, prepared);

            Assert.IsTrue(matched == null, "FindMatchingSettings should return null for null path");
        }

        [Test]
        public void RegexMatchIsCaseInsensitive()
        {
            string path = CreatePng("regex_case_test", asSprite: true);
            _assetPath = path;

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Regex,
                    matchPattern = "REGEX_CASE",
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Trilinear,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            SpriteSettings matched = SpriteSettingsApplierAPI.FindMatchingSettings(path, prepared);

            Assert.IsTrue(
                matched != null,
                "Regex matching should be case-insensitive. "
                    + "File name has 'regex_case', pattern is 'REGEX_CASE'. "
                    + "Path: "
                    + path
            );
        }

        [Test]
        public void InvalidRegexPatternDoesNotMatch()
        {
            string path = CreatePng("invalid_regex_test", asSprite: true);
            _assetPath = path;

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Regex,
                    matchPattern = "[invalid(regex",
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Trilinear,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            SpriteSettings matched = SpriteSettingsApplierAPI.FindMatchingSettings(path, prepared);

            Assert.IsTrue(
                matched == null,
                "Invalid regex pattern should not match. "
                    + "Pattern '[invalid(regex' is malformed. "
                    + "Path: "
                    + path
            );
        }

        [Test]
        public void PrepareProfilesSkipsNullEntries()
        {
            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Point,
                },
                null,
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.Any,
                    priority = 2,
                    applyFilterMode = true,
                    filterMode = FilterMode.Bilinear,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);

            Assert.AreEqual(
                2,
                prepared.Count,
                "PrepareProfiles should skip null entries. "
                    + "Input had 3 items (2 valid + 1 null), expected 2 prepared profiles."
            );
        }

        [Test]
        public void PrepareProfilesWithNullListReturnsEmptyList()
        {
            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(null);

            Assert.IsTrue(prepared != null, "PrepareProfiles should never return null");
            Assert.AreEqual(
                0,
                prepared.Count,
                "PrepareProfiles with null input should return empty list"
            );
        }

        private static IEnumerable<TestCaseData> NegativePriorityCases()
        {
            yield return new TestCaseData(-1, 0, 0).SetName("Priority.NegativeVsZero.ZeroWins");
            yield return new TestCaseData(-100, -50, -50).SetName(
                "Priority.TwoNegatives.HigherNegativeWins"
            );
            yield return new TestCaseData(int.MinValue, 0, 0).SetName(
                "Priority.MinValueVsZero.ZeroWins"
            );
            yield return new TestCaseData(int.MinValue, int.MaxValue, int.MaxValue).SetName(
                "Priority.MinValueVsMaxValue.MaxValueWins"
            );
        }

        [Test]
        [TestCaseSource(nameof(NegativePriorityCases))]
        public void NegativePriorityHandledCorrectly(
            int priority1,
            int priority2,
            int expectedWinningPriority
        )
        {
            string path = CreatePng("negative_priority_test", asSprite: true);
            _assetPath = path;

            List<SpriteSettings> profiles = new()
            {
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.NameContains,
                    matchPattern = "negative",
                    priority = priority1,
                    applyFilterMode = true,
                    filterMode = FilterMode.Point,
                },
                new SpriteSettings
                {
                    matchBy = SpriteSettings.MatchMode.NameContains,
                    matchPattern = "priority",
                    priority = priority2,
                    applyFilterMode = true,
                    filterMode = FilterMode.Bilinear,
                },
            };

            List<SpriteSettingsApplierAPI.PreparedProfile> prepared =
                SpriteSettingsApplierAPI.PrepareProfiles(profiles);
            SpriteSettings matched = SpriteSettingsApplierAPI.FindMatchingSettings(path, prepared);

            Assert.IsTrue(matched != null, "Expected a match for path: " + path);
            Assert.AreEqual(
                expectedWinningPriority,
                matched.priority,
                "Priority comparison failed. "
                    + "Profile priorities: "
                    + priority1
                    + ", "
                    + priority2
                    + ". "
                    + "Expected winning priority: "
                    + expectedWinningPriority
                    + ", "
                    + "actual: "
                    + matched.priority
            );
        }
    }
#endif
}
