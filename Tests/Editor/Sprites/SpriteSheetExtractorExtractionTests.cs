// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.Sprites;
    using WallstopStudios.UnityHelpers.Editor.Utils;
    using Object = UnityEngine.Object;
    using PivotMode = UnityHelpers.Editor.Sprites.PivotMode;

    /// <summary>
    /// Tests for <see cref="SpriteSheetExtractor"/> that perform actual sprite extraction operations.
    /// These tests create assets, extract sprites, and verify the extracted results.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test fixture uses shared fixtures for common test scenarios and creates unique assets
    /// for tests that require specific configurations or need to verify extraction results without
    /// affecting other tests.
    /// </para>
    /// <para>
    /// Tests are marked with [Category("Slow")] and [Category("Integration")] because they
    /// perform full extraction operations including AssetDatabase modifications.
    /// </para>
    /// </remarks>
    [TestFixture]
    [NUnit.Framework.Category("Slow")]
    [NUnit.Framework.Category("Integration")]
    public sealed class SpriteSheetExtractorExtractionTests : SpriteSheetExtractorTestBase
    {
        private const string RootPath = "Assets/Temp/SpriteSheetExtractorExtractionTests";
        private const string OutputDirPath =
            "Assets/Temp/SpriteSheetExtractorExtractionTests/Output";
        private const string SharedDirPath =
            "Assets/Temp/SpriteSheetExtractorExtractionTests/Shared";

        /// <inheritdoc />
        protected override string Root => RootPath;

        /// <inheritdoc />
        protected override string OutputDir => OutputDirPath;

        /// <inheritdoc />
        protected override string SharedDir => SharedDirPath;

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            if (Application.isPlaying)
            {
                Assert.Ignore("AssetDatabase access requires edit mode.");
            }
            EnsureFolder(Root);
            EnsureFolder(OutputDir);
            SpriteSheetExtractor.SuppressUserPrompts = true;
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            if (Application.isPlaying)
            {
                return;
            }
            CleanupTrackedFoldersAndAssets();
        }

        public override void CommonOneTimeSetUp()
        {
            base.CommonOneTimeSetUp();
            DeferAssetCleanupToOneTimeTearDown = true;
            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                EnsureFolderStatic(Root);
                EnsureFolderStatic(OutputDir);
                EnsureFolderStatic(SharedDir);
                CreateAllSharedFixtures();
            }
        }

        [OneTimeTearDown]
        public override void OneTimeTearDown()
        {
            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                CleanupAllSharedFixtures();
            }

            CleanupDeferredAssetsAndFolders();
            base.OneTimeTearDown();
        }

        private string CreateUniqueSpriteSheet(
            string uniqueTestName,
            int width,
            int height,
            int gridColumns,
            int gridRows
        )
        {
            int cellWidth = width / gridColumns;
            int cellHeight = height / gridRows;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false) // UNH-SUPPRESS: Temporary texture for file writing, destroyed immediately after
            {
                alphaIsTransparency = true,
            };

            Color[] pixels = new Color[width * height];
            for (int row = 0; row < gridRows; row++)
            {
                for (int col = 0; col < gridColumns; col++)
                {
                    int spriteIndex = row * gridColumns + col;
                    float hue = (float)spriteIndex / (gridRows * gridColumns);
                    Color cellColor = Color.HSVToRGB(hue, 0.8f, 0.9f);

                    int startX = col * cellWidth;
                    int startY = row * cellHeight;

                    for (int y = startY; y < startY + cellHeight; y++)
                    {
                        for (int x = startX; x < startX + cellWidth; x++)
                        {
                            pixels[y * width + x] = cellColor;
                        }
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            string uniqueFolder = Path.Combine(Root, uniqueTestName).SanitizePath();
            EnsureFolder(uniqueFolder);

            string path = Path.Combine(uniqueFolder, uniqueTestName + ".png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);

            Object.DestroyImmediate(texture); // UNH-SUPPRESS: Cleanup temporary texture used only for file writing

            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return path;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            SpriteMetaData[] spritesheet = new SpriteMetaData[gridColumns * gridRows];
            for (int row = 0; row < gridRows; row++)
            {
                for (int col = 0; col < gridColumns; col++)
                {
                    int index = row * gridColumns + col;
                    spritesheet[index] = new SpriteMetaData
                    {
                        name = $"{uniqueTestName}_sprite_{index}",
                        rect = new Rect(col * cellWidth, row * cellHeight, cellWidth, cellHeight),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                        border = Vector4.zero,
                    };
                }
            }
            SetSpriteSheet(importer, spritesheet);
            importer.SaveAndReimport();

            return path;
        }

        private void CleanupExtractedFiles(string outputDirectory)
        {
            if (!AssetDatabase.IsValidFolder(outputDirectory))
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { outputDirectory });
            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (!string.IsNullOrEmpty(path))
                    {
                        AssetDatabase.DeleteAsset(path);
                    }
                }
            }
        }

        [Test]
        public void ExtractSelectedSpritesCreatesOutputFiles()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractCreatesOutput").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(extractor, Shared2x2Path);
            Assert.IsTrue(entry != null, "Should find shared_2x2 entry");

            extractor.SelectAll(entry);
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extractedGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { uniqueOutputDir }
            );

            Assert.That(
                extractedGuids.Length,
                Is.GreaterThan(0),
                "Should have created extracted files"
            );

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractedSpritesHaveCorrectDimensions()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractCorrectDimensions").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(extractor, Shared2x2Path);
            Assert.IsTrue(entry != null, "Should find shared_2x2 entry");

            extractor.SelectAll(entry);
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extractedGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { uniqueOutputDir }
            );
            Assert.That(extractedGuids.Length, Is.GreaterThan(0));

            string firstExtractedPath = AssetDatabase.GUIDToAssetPath(extractedGuids[0]);
            Texture2D extracted = AssetDatabase.LoadAssetAtPath<Texture2D>(firstExtractedPath);
            Assert.IsTrue(extracted != null, "Should load extracted texture");

            Assert.That(extracted.width, Is.EqualTo(32), "Extracted width should be 32");
            Assert.That(extracted.height, Is.EqualTo(32), "Extracted height should be 32");

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractedSpritesHaveCorrectPixelContent()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractCorrectPixels").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(extractor, Shared2x2Path);
            Assert.IsTrue(entry != null, "Should find shared_2x2 entry");

            extractor.SelectAll(entry);
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extractedGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { uniqueOutputDir }
            );
            Assert.That(extractedGuids.Length, Is.GreaterThan(0));

            string firstExtractedPath = AssetDatabase.GUIDToAssetPath(extractedGuids[0]);
            Texture2D extracted = AssetDatabase.LoadAssetAtPath<Texture2D>(firstExtractedPath);
            Assert.IsTrue(extracted != null, "Should load extracted texture");

            Color[] pixels = extracted.GetPixels();
            bool hasNonZeroPixels = false;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a > 0.01f)
                {
                    hasNonZeroPixels = true;
                    break;
                }
            }

            Assert.IsTrue(hasNonZeroPixels, "Extracted texture should have non-transparent pixels");

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractionCreatesCorrectNumberOfFiles()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractCorrectCount").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(extractor, Shared4x4Path);
            Assert.IsTrue(entry != null, "Should find shared_4x4 entry");

            int selectedCount = 0;
            for (int i = 0; i < entry._sprites.Count; i++)
            {
                if (entry._sprites[i]._isSelected)
                {
                    selectedCount++;
                }
            }

            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extractedGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { uniqueOutputDir }
            );

            Assert.That(
                extractedGuids.Length,
                Is.EqualTo(selectedCount),
                "Should create one file per selected sprite"
            );

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void PartialSelectionOnlyExtractsSelectedSprites()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractPartialSelection").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(extractor, Shared4x4Path);
            Assert.IsTrue(entry != null, "Should find shared_4x4 entry");
            Assert.That(entry._sprites.Count, Is.EqualTo(16));

            extractor.SelectNone(entry);
            int selectedCount = 0;
            for (int i = 0; i < entry._sprites.Count && selectedCount < 4; i++)
            {
                entry._sprites[i]._isSelected = true;
                selectedCount++;
            }

            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extractedGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { uniqueOutputDir }
            );

            Assert.That(
                extractedGuids.Length,
                Is.EqualTo(4),
                "Should extract only 4 selected sprites"
            );

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void OverwriteExistingReplacesFiles()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractOverwrite").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor._overwriteExisting = true;
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(extractor, Shared2x2Path);
            Assert.IsTrue(entry != null, "Should find shared_2x2 entry");

            if (entry._sprites.Count > 0)
            {
                extractor.SelectNone(entry);
                entry._sprites[0]._isSelected = true;
            }

            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] firstExtractionGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { uniqueOutputDir }
            );
            Assert.That(firstExtractionGuids.Length, Is.EqualTo(1));

            DateTime firstWriteTime = File.GetLastWriteTime(
                RelToFull(AssetDatabase.GUIDToAssetPath(firstExtractionGuids[0]))
            );

            System.Threading.Thread.Sleep(100);

            Assert.DoesNotThrow(() => extractor.ExtractSelectedSprites());
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] secondExtractionGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { uniqueOutputDir }
            );
            Assert.That(secondExtractionGuids.Length, Is.EqualTo(1));

            DateTime secondWriteTime = File.GetLastWriteTime(
                RelToFull(AssetDatabase.GUIDToAssetPath(secondExtractionGuids[0]))
            );

            Assert.That(
                secondWriteTime,
                Is.GreaterThanOrEqualTo(firstWriteTime),
                "File should be rewritten"
            );

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractedFileNamingUsesOriginalSpriteName()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractNaming").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(extractor, Shared4x4Path);
            Assert.IsTrue(entry != null, "Should find shared_4x4 entry");

            if (entry._sprites.Count > 0)
            {
                extractor.SelectNone(entry);
                entry._sprites[0]._isSelected = true;
                string expectedNamePart = entry._sprites[0]._originalName;

                extractor.ExtractSelectedSprites();
                AssetDatabaseBatchHelper.RefreshIfNotBatching();

                string[] extractedGuids = AssetDatabase.FindAssets(
                    "t:Texture2D",
                    new[] { uniqueOutputDir }
                );
                Assert.That(extractedGuids.Length, Is.EqualTo(1));

                string extractedPath = AssetDatabase.GUIDToAssetPath(extractedGuids[0]);
                string extractedFileName = Path.GetFileNameWithoutExtension(extractedPath);

                Assert.That(
                    extractedFileName,
                    Does.Contain(expectedNamePart).Or.StartWith("shared_4x4"),
                    "Extracted file should contain original sprite name or sheet name"
                );
            }

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractionWithPivotModeAppliesPivotToOutput()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractPivot").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor._pivotMode = PivotMode.BottomLeft;
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(extractor, Shared2x2Path);
            Assert.IsTrue(entry != null, "Should find shared_2x2 entry");

            extractor.SelectAll(entry);
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extractedGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { uniqueOutputDir }
            );
            Assert.That(extractedGuids.Length, Is.GreaterThan(0));

            string firstExtractedPath = AssetDatabase.GUIDToAssetPath(extractedGuids[0]);
            TextureImporter importer =
                AssetImporter.GetAtPath(firstExtractedPath) as TextureImporter;
            Assert.IsTrue(importer != null, "Should have texture importer");

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            Assert.That(
                settings.spritePivot,
                Is.EqualTo(Vector2.zero).Or.EqualTo(new Vector2(0f, 0f)),
                "Pivot should be bottom-left (0, 0)"
            );

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractionWithCustomPivotAppliesCustomPivotToOutput()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractCustomPivot").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor._pivotMode = PivotMode.Custom;
            extractor._customPivot = new Vector2(0.25f, 0.75f);
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(extractor, Shared2x2Path);
            Assert.IsTrue(entry != null, "Should find shared_2x2 entry");

            extractor.SelectAll(entry);
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extractedGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { uniqueOutputDir }
            );
            Assert.That(extractedGuids.Length, Is.GreaterThan(0));

            string firstExtractedPath = AssetDatabase.GUIDToAssetPath(extractedGuids[0]);
            TextureImporter importer =
                AssetImporter.GetAtPath(firstExtractedPath) as TextureImporter;
            Assert.IsTrue(importer != null, "Should have texture importer");

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            Assert.That(
                settings.spritePivot.x,
                Is.EqualTo(0.25f).Within(0.01f),
                "Custom pivot X should be 0.25"
            );
            Assert.That(
                settings.spritePivot.y,
                Is.EqualTo(0.75f).Within(0.01f),
                "Custom pivot Y should be 0.75"
            );

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractionWithSpritePivotOverrideAppliesSpriteSpecificPivot()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractSpritePivotOverride")
                .SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor._pivotMode = PivotMode.Center;
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(extractor, Shared2x2Path);
            Assert.IsTrue(entry != null, "Should find shared_2x2 entry");
            Assert.That(entry._sprites.Count, Is.GreaterThan(0));

            extractor.SelectNone(entry);
            entry._sprites[0]._isSelected = true;
            entry._sprites[0]._usePivotOverride = true;
            entry._sprites[0]._pivotModeOverride = PivotMode.TopRight;
            entry._sprites[0]._customPivotOverride = new Vector2(1f, 1f);

            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extractedGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { uniqueOutputDir }
            );
            Assert.That(extractedGuids.Length, Is.EqualTo(1));

            string extractedPath = AssetDatabase.GUIDToAssetPath(extractedGuids[0]);
            TextureImporter importer = AssetImporter.GetAtPath(extractedPath) as TextureImporter;
            Assert.IsTrue(importer != null, "Should have texture importer");

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            Assert.That(
                settings.spritePivot.x,
                Is.EqualTo(1f).Within(0.01f),
                "Sprite pivot X should be 1.0"
            );
            Assert.That(
                settings.spritePivot.y,
                Is.EqualTo(1f).Within(0.01f),
                "Sprite pivot Y should be 1.0"
            );

            CleanupExtractedFiles(uniqueOutputDir);
        }

        private static IEnumerable<TestCaseData> ExtractionModesCases()
        {
            yield return new TestCaseData(SpriteSheetExtractor.ExtractionMode.FromMetadata).SetName(
                "Extraction.FromMetadata"
            );
            yield return new TestCaseData(SpriteSheetExtractor.ExtractionMode.GridBased).SetName(
                "Extraction.GridBased"
            );
            yield return new TestCaseData(SpriteSheetExtractor.ExtractionMode.PaddedGrid).SetName(
                "Extraction.PaddedGrid"
            );
        }

        [Test]
        [TestCaseSource(nameof(ExtractionModesCases))]
        public void ExtractionModesAllProduceValidOutput(SpriteSheetExtractor.ExtractionMode mode)
        {
            string uniqueOutputDir = Path.Combine(Root, $"Extract{mode}").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor._extractionMode = mode;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor._paddingLeft = 2;
            extractor._paddingRight = 2;
            extractor._paddingTop = 2;
            extractor._paddingBottom = 2;
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(extractor, Shared2x2Path);
            Assert.IsTrue(entry != null, "Should find shared_2x2 entry");

            extractor.SelectAll(entry);
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extractedGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { uniqueOutputDir }
            );

            Assert.That(
                extractedGuids.Length,
                Is.GreaterThan(0),
                $"Mode {mode} should produce output"
            );

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void LargeSheetExtractionHandlesAllSprites()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractLargeSheet").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(extractor, Shared8x8Path);
            Assert.IsTrue(entry != null, "Should find shared_8x8 entry");

            extractor.SelectAll(entry);
            int selectedCount = 0;
            for (int i = 0; i < entry._sprites.Count; i++)
            {
                if (entry._sprites[i]._isSelected)
                {
                    selectedCount++;
                }
            }

            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extractedGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { uniqueOutputDir }
            );

            Assert.That(extractedGuids.Length, Is.EqualTo(selectedCount));

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void SingleModeSpriteExtractionWorks()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractSingleMode").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(
                extractor,
                SharedSingleModePath
            );
            Assert.IsTrue(entry != null, "Should find shared_single entry");

            extractor.SelectAll(entry);
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extractedGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { uniqueOutputDir }
            );

            Assert.That(
                extractedGuids.Length,
                Is.GreaterThan(0),
                "Should extract single mode sprite"
            );

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void WideSheetExtractionPreservesDimensions()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractWideSheet").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 4;
            extractor._gridRows = 2;
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(
                extractor,
                SharedWidePath
            );
            Assert.IsTrue(entry != null, "Should find shared_wide entry");

            extractor.SelectNone(entry);
            if (entry._sprites.Count > 0)
            {
                entry._sprites[0]._isSelected = true;
            }

            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extractedGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { uniqueOutputDir }
            );
            Assert.That(extractedGuids.Length, Is.EqualTo(1));

            string extractedPath = AssetDatabase.GUIDToAssetPath(extractedGuids[0]);
            Texture2D extracted = AssetDatabase.LoadAssetAtPath<Texture2D>(extractedPath);
            Assert.IsTrue(extracted != null);

            Assert.That(extracted.width, Is.EqualTo(32), "Wide sheet cell width should be 32");
            Assert.That(extracted.height, Is.EqualTo(32), "Wide sheet cell height should be 32");

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void TallSheetExtractionPreservesDimensions()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractTallSheet").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 2;
            extractor._gridRows = 4;
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(
                extractor,
                SharedTallPath
            );
            Assert.IsTrue(entry != null, "Should find shared_tall entry");

            extractor.SelectNone(entry);
            if (entry._sprites.Count > 0)
            {
                entry._sprites[0]._isSelected = true;
            }

            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extractedGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { uniqueOutputDir }
            );
            Assert.That(extractedGuids.Length, Is.EqualTo(1));

            string extractedPath = AssetDatabase.GUIDToAssetPath(extractedGuids[0]);
            Texture2D extracted = AssetDatabase.LoadAssetAtPath<Texture2D>(extractedPath);
            Assert.IsTrue(extracted != null);

            Assert.That(extracted.width, Is.EqualTo(32), "Tall sheet cell width should be 32");
            Assert.That(extracted.height, Is.EqualTo(32), "Tall sheet cell height should be 32");

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void OddDimensionSheetExtractionHandlesRounding()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractOddSheet").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(extractor, SharedOddPath);
            Assert.IsTrue(entry != null, "Should find shared_odd entry");

            extractor.SelectAll(entry);
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extractedGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { uniqueOutputDir }
            );

            Assert.That(
                extractedGuids.Length,
                Is.GreaterThan(0),
                "Should extract sprites from odd dimension sheet"
            );

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void PerSheetExtractionModeOverrideWorks()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractPerSheetMode").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.FromMetadata;
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(extractor, Shared4x4Path);
            Assert.IsTrue(entry != null, "Should find shared_4x4 entry");

            entry._useGlobalSettings = false;
            entry._extractionModeOverride = SpriteSheetExtractor.ExtractionMode.GridBased;
            entry._gridSizeModeOverride = SpriteSheetExtractor.GridSizeMode.Manual;
            entry._gridColumnsOverride = 2;
            entry._gridRowsOverride = 2;

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Shared4x4Path);
            entry._sprites.Clear();
            extractor.PopulateSpritesFromGrid(entry, texture);

            extractor.SelectAll(entry);

            int selectedCount = 0;
            for (int i = 0; i < entry._sprites.Count; i++)
            {
                if (entry._sprites[i]._isSelected)
                {
                    selectedCount++;
                }
            }

            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extractedGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { uniqueOutputDir }
            );

            Assert.That(
                extractedGuids.Length,
                Is.EqualTo(selectedCount),
                "Per-sheet override should extract correct number of sprites"
            );

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void MultipleSheetExtractionInOneOperation()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractMultipleSheets").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();

            int totalSelectedCount = 0;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                extractor.SelectAll(entry);
                for (int j = 0; j < entry._sprites.Count; j++)
                {
                    if (entry._sprites[j]._isSelected)
                    {
                        totalSelectedCount++;
                    }
                }
            }

            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extractedGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { uniqueOutputDir }
            );

            Assert.That(
                extractedGuids.Length,
                Is.EqualTo(totalSelectedCount),
                "Should extract all selected sprites from all sheets"
            );

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractionWithNoOutputDirectoryHandledGracefully()
        {
            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = null;
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(extractor, Shared2x2Path);
            Assert.IsTrue(entry != null, "Should find shared_2x2 entry");

            extractor.SelectAll(entry);

            Assert.DoesNotThrow(() => extractor.ExtractSelectedSprites());
        }

        [Test]
        public void PreviewTexturesCreatedDuringExtraction()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractPreviewTextures").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(extractor, Shared4x4Path);
            Assert.IsTrue(entry != null, "Should find shared_4x4 entry");

            bool hasPreviewBefore = false;
            for (int i = 0; i < entry._sprites.Count; i++)
            {
                if (entry._sprites[i]._previewTexture != null)
                {
                    hasPreviewBefore = true;
                    break;
                }
            }

            Assert.IsTrue(hasPreviewBefore, "Previews should exist after discovery");

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractedSpritesAreReadable()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractReadable").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(extractor, Shared2x2Path);
            Assert.IsTrue(entry != null, "Should find shared_2x2 entry");

            extractor.SelectNone(entry);
            if (entry._sprites.Count > 0)
            {
                entry._sprites[0]._isSelected = true;
            }

            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extractedGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { uniqueOutputDir }
            );
            Assert.That(extractedGuids.Length, Is.EqualTo(1));

            string extractedPath = AssetDatabase.GUIDToAssetPath(extractedGuids[0]);
            Texture2D extracted = AssetDatabase.LoadAssetAtPath<Texture2D>(extractedPath);
            Assert.IsTrue(extracted != null);

            Assert.DoesNotThrow(
                () =>
                {
                    Color[] pixels = extracted.GetPixels();
                    Assert.IsTrue(pixels != null && pixels.Length > 0);
                },
                "Extracted texture should be readable"
            );

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractedSpritesUseSpriteImporterType()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractSpriteType").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(extractor, Shared2x2Path);
            Assert.IsTrue(entry != null, "Should find shared_2x2 entry");

            extractor.SelectNone(entry);
            if (entry._sprites.Count > 0)
            {
                entry._sprites[0]._isSelected = true;
            }

            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extractedGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { uniqueOutputDir }
            );
            Assert.That(extractedGuids.Length, Is.EqualTo(1));

            string extractedPath = AssetDatabase.GUIDToAssetPath(extractedGuids[0]);
            TextureImporter importer = AssetImporter.GetAtPath(extractedPath) as TextureImporter;
            Assert.IsTrue(importer != null);

            Assert.That(
                importer.textureType,
                Is.EqualTo(TextureImporterType.Sprite),
                "Extracted texture should be Sprite type"
            );

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractionClearsNeedsRegenerationFlag()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractClearRegenFlag").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(extractor, Shared2x2Path);
            Assert.IsTrue(entry != null, "Should find shared_2x2 entry");

            entry._needsRegeneration = true;

            extractor.SelectAll(entry);
            extractor.ExtractSelectedSprites();

            Assert.That(
                entry._needsRegeneration,
                Is.False,
                "NeedsRegeneration should be cleared after extraction"
            );

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void PaddedGridExtractionRemovesPadding()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractPaddedGrid").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.PaddedGrid;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor._paddingLeft = 4;
            extractor._paddingRight = 4;
            extractor._paddingTop = 4;
            extractor._paddingBottom = 4;
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(extractor, Shared2x2Path);
            Assert.IsTrue(entry != null, "Should find shared_2x2 entry");

            extractor.SelectNone(entry);
            if (entry._sprites.Count > 0)
            {
                entry._sprites[0]._isSelected = true;
            }

            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extractedGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { uniqueOutputDir }
            );
            Assert.That(extractedGuids.Length, Is.EqualTo(1));

            string extractedPath = AssetDatabase.GUIDToAssetPath(extractedGuids[0]);
            Texture2D extracted = AssetDatabase.LoadAssetAtPath<Texture2D>(extractedPath);
            Assert.IsTrue(extracted != null);

            int expectedWidth = 32 - 8;
            int expectedHeight = 32 - 8;

            Assert.That(
                extracted.width,
                Is.EqualTo(expectedWidth),
                "Padded extraction should remove horizontal padding"
            );
            Assert.That(
                extracted.height,
                Is.EqualTo(expectedHeight),
                "Padded extraction should remove vertical padding"
            );

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void UniqueAssetCreationAndExtractionWorks()
        {
            string uniqueTestName = "UniqueExtractionTest";
            string uniqueOutputDir = Path.Combine(Root, uniqueTestName + "Output").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            string uniqueSheetPath = CreateUniqueSpriteSheet(uniqueTestName, 128, 128, 4, 4);
            string uniqueSheetDir = Path.GetDirectoryName(uniqueSheetPath).SanitizePath();

            SpriteSheetExtractor extractor = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            extractor._inputDirectories = new List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(uniqueSheetDir),
            };
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor._overwriteExisting = true;
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(
                extractor,
                uniqueSheetPath
            );
            Assert.IsTrue(entry != null, "Should find unique sheet entry");

            extractor.SelectAll(entry);
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extractedGuids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { uniqueOutputDir }
            );

            Assert.That(
                extractedGuids.Length,
                Is.EqualTo(16),
                "Should extract all 16 sprites from unique 4x4 sheet"
            );

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void DuplicateSpriteRectsHandledGracefully()
        {
            string uniqueOutputDir = Path.Combine(Root, "ExtractDuplicateRects").SanitizePath();
            EnsureFolder(uniqueOutputDir);
            TrackFolder(uniqueOutputDir);

            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindEntryByPath(extractor, Shared2x2Path);
            Assert.IsTrue(entry != null, "Should find shared_2x2 entry");
            Assert.That(entry._sprites.Count, Is.EqualTo(4));

            if (entry._sprites.Count >= 2)
            {
                entry._sprites[1]._rect = entry._sprites[0]._rect;
            }

            entry._useGlobalSettings = false;
            entry._gridColumnsOverride = 2;
            entry._gridRowsOverride = 2;

            Assert.DoesNotThrow(() => extractor.RepopulateSpritesForEntry(entry));
            Assert.DoesNotThrow(() =>
                extractor.GenerateAllPreviewTexturesInBatch(extractor._discoveredSheets)
            );

            Assert.IsTrue(entry._sprites != null);
            Assert.Greater(entry._sprites.Count, 0);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        #region Alpha Channel Tests

        [Test]
        public void AlphaChannelIsPreservedDuringExtraction()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("alpha_preserve");

            Texture2D tex = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            Color32[] pixels = new Color32[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 0, 0, 128);
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            string path = Path.Combine(Root, "alpha_test.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), tex.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);
            SetupSpriteImporter(path, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.Greater(extracted.Length, 0, "Should extract at least one sprite");

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void HandlesTextureWithAlphaChannel()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("alpha_channel");
            CreateSpriteSheet("with_alpha", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.Greater(extracted.Length, 0);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void HandlesTextureWithoutAlphaChannel()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("no_alpha");

            Texture2D tex = Track(new Texture2D(64, 64, TextureFormat.RGB24, false));
            FillTexture(tex, Color.blue);
            string path = Path.Combine(Root, "no_alpha_test.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), tex.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);
            SetupSpriteImporter(path, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.Greater(extracted.Length, 0);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        #endregion

        #region Naming Tests

        [Test]
        public void DefaultNamingPatternUsesFilenameAndZeroPadding()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("naming_default");
            CreateSpriteSheet("naming_test", 64, 32, 2, 1);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor._namingPrefix = "";
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string output0 = Path.Combine(uniqueOutputDir, "naming_test_000.png").SanitizePath();
            string output1 = Path.Combine(uniqueOutputDir, "naming_test_001.png").SanitizePath();

            Assert.That(
                File.Exists(RelToFull(output0)),
                Is.True,
                "Should use default naming with 000"
            );
            Assert.That(
                File.Exists(RelToFull(output1)),
                Is.True,
                "Should use default naming with 001"
            );

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void CustomPrefixOverridesDefaultNaming()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("naming_prefix");
            CreateSpriteSheet("custom_prefix_test", 64, 32, 2, 1);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor._namingPrefix = "custom_";
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.Greater(extracted.Length, 0);
            Assert.IsTrue(
                Path.GetFileName(extracted[0]).StartsWith("custom_"),
                "Should use custom prefix"
            );

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ZeroPaddingWorksWithLargeSpriteCount()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("zero_padding");
            CreateSpriteSheet("large_count", 256, 256, 8, 8);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(64, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void MultipleSheetsWithSameNamePrefixUseCorrectNaming()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("multi_sheet_naming");
            CreateSpriteSheet("sheet_a", 64, 64, 2, 2);
            CreateSpriteSheet("sheet_b", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(8, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        #endregion

        #region Dry Run Tests

        [Test]
        public void DryRunDoesNotCreateFiles()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("dry_run");
            CreateSpriteSheet("dry_run_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor._dryRun = true;
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(0, extracted.Length, "Dry run should not create files");

            CleanupExtractedFiles(uniqueOutputDir);
        }

        #endregion

        #region No Overwrite Tests

        [Test]
        public void NoOverwriteSkipsExistingFiles()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("no_overwrite");
            CreateSpriteSheet("no_overwrite_test", 64, 64, 2, 2);

            string existingFile = Path.Combine(uniqueOutputDir, "no_overwrite_test_000.png")
                .SanitizePath();
            File.WriteAllBytes(RelToFull(existingFile), new byte[1]);
            TrackAssetPath(existingFile);
            AssetDatabase.ImportAsset(existingFile);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor._overwriteExisting = false;
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            byte[] existingContent = File.ReadAllBytes(RelToFull(existingFile));
            Assert.AreEqual(1, existingContent.Length, "Existing file should not be overwritten");

            CleanupExtractedFiles(uniqueOutputDir);
        }

        #endregion

        #region Dimension Tests

        [Test]
        public void ExtractedTexturesHaveCorrectDimensions()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("dimensions");
            CreateSpriteSheet("dim_test", 128, 128, 4, 4);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(16, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractedSpritesHaveCorrectPixelDimensions()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("pixel_dim");
            CreateSpriteSheet("pixel_dim_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(4, extracted.Length);

            byte[] fileData = File.ReadAllBytes(extracted[0]);
            Texture2D loadedTex = Track(new Texture2D(2, 2));
            loadedTex.LoadImage(fileData);

            Assert.AreEqual(32, loadedTex.width, "Extracted sprite should be 32 pixels wide");
            Assert.AreEqual(32, loadedTex.height, "Extracted sprite should be 32 pixels tall");

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractedSpritesAreImportedAsSingleMode()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("import_mode");
            CreateSpriteSheet("import_mode_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.Greater(extracted.Length, 0);

            string assetPath = extracted[0]
                .Replace(RelToFull(""), "")
                .Replace("\\", "/")
                .TrimStart('/');
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Assert.IsNotNull(importer);
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        #endregion

        #region Grid Extraction Tests

        [Test]
        public void Extracts4x4GridSixteenSprites()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("4x4_grid");
            CreateSpriteSheet("grid_4x4_test", 128, 128, 4, 4);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(16, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractsTwoSpriteSheetMinimumCase()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("min_case");
            CreateSpriteSheet("min_case_test", 64, 32, 2, 1);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(2, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void PaddedGridExtractionAppliesPadding()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("padded_grid");
            CreateSpriteSheet("padded_grid_test", 72, 72, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.PaddedGrid;
            extractor._paddingLeft = 4;
            extractor._paddingRight = 4;
            extractor._paddingTop = 4;
            extractor._paddingBottom = 4;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.Greater(extracted.Length, 0);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        #endregion

        #region Edge Case Extraction Tests

        [Test]
        public void Extraction1x1PixelSpriteWorksCorrectly()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("1x1_sprite");

            Texture2D tex = Track(new Texture2D(2, 2, TextureFormat.RGBA32, false));
            FillTexture(tex, Color.red);
            string path = Path.Combine(Root, "tiny_1x1.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), tex.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);
            SetupSpriteImporter(path, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(4, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractionAtTextureBoundariesWorksCorrectly()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("boundaries");
            CreateSpriteSheet("boundary_extraction", 64, 64, 4, 4);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(16, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractionSpritesTouchingTextureEdgesWorksCorrectly()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("edge_touch");
            CreateSpriteSheet("edge_touch_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(4, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractionHandlesExactBoundarySpritesCorrectly()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("exact_boundary");
            CreateSpriteSheet("exact_boundary_test", 128, 64, 4, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(8, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractionHandlesNegativeSpriteCoordinatesGracefully()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("negative_coords");
            CreateSpriteSheet("negative_coord_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = null;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (extractor._discoveredSheets[i]._assetPath.Contains("negative_coord_test"))
                {
                    entry = extractor._discoveredSheets[i];
                    break;
                }
            }

            Assert.IsNotNull(entry);
            Assert.DoesNotThrow(() => extractor.ExtractSelectedSprites());

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractionHandlesPartiallyOutOfBoundsSpriteRectsGracefully()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("partial_oob");
            CreateSpriteSheet("partial_oob_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();

            Assert.DoesNotThrow(() => extractor.ExtractSelectedSprites());

            CleanupExtractedFiles(uniqueOutputDir);
        }

        #endregion

        #region Large Texture Extraction Tests

        [Test]
        public void Extraction2048x2048TextureWithMultipleSpritesWorksCorrectly()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("large_2048");
            CreateSpriteSheet("large_2048_test", 2048, 2048, 8, 8);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(64, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractionManySmallSpritesFromLargeGridWorksCorrectly()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("many_small");
            CreateSpriteSheet("many_small_test", 512, 512, 16, 16);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(256, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractionVeryLargeSpritesWorksCorrectly()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("very_large");
            CreateSpriteSheet("very_large_test", 1024, 512, 2, 1);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(2, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void HandlesLargeTextureDimensions()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("large_dim");
            CreateSpriteSheet("large_dim_test", 1024, 1024, 4, 4);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(16, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void LargeGridExtractionDoesNotTriggerLoopDetection()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("large_grid_loop");
            CreateSpriteSheet("large_grid_test", 256, 256, 8, 8);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();

            Assert.DoesNotThrow(() => extractor.ExtractSelectedSprites());

            AssetDatabaseBatchHelper.RefreshIfNotBatching();
            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(64, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        #endregion

        #region NPOT Dimension Tests

        [Test]
        public void ExtractionWithNonPowerOfTwoDimensionsWorksCorrectly()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("npot");
            CreateSpriteSheet("npot_extract_test", 100, 100, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(4, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractionWithAsymmetricNPOTDimensionsWorksCorrectly()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("asymmetric_npot");
            CreateSpriteSheet("asymmetric_npot_test", 150, 100, 3, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(6, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractionWithUnevenGridDimensionsWorksCorrectly()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("uneven_grid");
            CreateSpriteSheet("uneven_grid_test", 100, 60, 5, 3);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(15, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractionWithMinimumSpriteSizesWorksCorrectly()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("min_sizes");
            CreateSpriteSheet("min_sizes_test", 16, 16, 4, 4);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(16, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        #endregion

        #region Multiple Extraction Tests

        [Test]
        public void MultipleExtractionRunsWorkCorrectly()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("multi_run");
            CreateSpriteSheet("multi_run_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor._overwriteExisting = true;
            extractor.DiscoverSpriteSheets();

            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] firstRun = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(4, firstRun.Length);

            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] secondRun = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(4, secondRun.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void MultipleExtractionsWithDifferentSizesWorkCorrectly()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("multi_size");
            CreateSpriteSheet("size_a", 64, 64, 2, 2);
            CreateSpriteSheet("size_b", 128, 128, 4, 4);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(20, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        #endregion

        #region Special Character Tests

        [Test]
        public void HandlesSpecialCharactersInFilename()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("special_chars");

            Texture2D tex = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            FillTexture(tex, Color.green);
            string path = Path.Combine(Root, "sprite_with-dash_and.dot.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), tex.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);
            SetupSpriteImporter(path, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.Greater(extracted.Length, 0);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        #endregion

        #region Output Directory Tests

        [Test]
        public void NullOutputDirectoryPreventsExtraction()
        {
            CreateSpriteSheet("null_output_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = null;
            extractor.DiscoverSpriteSheets();

            Assert.DoesNotThrow(() => extractor.ExtractSelectedSprites());
        }

        [Test]
        public void NonFolderOutputDirectoryPreventsExtraction()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("non_folder_test");
            CreateSpriteSheet("non_folder_output", 64, 64, 2, 2);

            Texture2D tex = Track(new Texture2D(32, 32, TextureFormat.RGBA32, false));
            string filePath = Path.Combine(uniqueOutputDir, "not_a_folder.png").SanitizePath();
            File.WriteAllBytes(RelToFull(filePath), tex.EncodeToPNG());
            TrackAssetPath(filePath);
            AssetDatabase.ImportAsset(filePath);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(filePath);
            extractor.DiscoverSpriteSheets();

            Assert.DoesNotThrow(() => extractor.ExtractSelectedSprites());

            CleanupExtractedFiles(uniqueOutputDir);
        }

        #endregion

        #region Empty/Zero Tests

        [Test]
        public void HandlesEmptySpritesheet()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("empty_sheet");

            Texture2D tex = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            FillTexture(tex, Color.white);
            string path = Path.Combine(Root, "empty_sheet_test.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), tex.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            SetSpriteSheet(importer, Array.Empty<SpriteMetaData>());

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();

            Assert.DoesNotThrow(() => extractor.ExtractSelectedSprites());

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void HandlesZeroWidthSpriteRect()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("zero_width");
            CreateSpriteSheet("zero_width_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = null;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (extractor._discoveredSheets[i]._assetPath.Contains("zero_width_test"))
                {
                    entry = extractor._discoveredSheets[i];
                    break;
                }
            }

            Assert.IsNotNull(entry);
            Assert.DoesNotThrow(() => extractor.ExtractSelectedSprites());

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void HandlesNonReadableTextureGracefully()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("non_readable");

            Texture2D tex = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            FillTexture(tex, Color.cyan);
            string path = Path.Combine(Root, "non_readable_test.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), tex.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = false;
            SetupSpritesheet(importer, 2, 2);
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();

            Assert.DoesNotThrow(() => extractor.ExtractSelectedSprites());

            CleanupExtractedFiles(uniqueOutputDir);
        }

        #endregion

        #region Pixel Transfer Tests

        [Test]
        public void PixelColorsAreCorrectlyTransferred()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("pixel_transfer");

            Texture2D tex = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            Color32[] pixels = new Color32[64 * 64];
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    if (x < 32 && y < 32)
                    {
                        pixels[y * 64 + x] = new Color32(255, 0, 0, 255);
                    }
                    else if (x >= 32 && y < 32)
                    {
                        pixels[y * 64 + x] = new Color32(0, 255, 0, 255);
                    }
                    else if (x < 32 && y >= 32)
                    {
                        pixels[y * 64 + x] = new Color32(0, 0, 255, 255);
                    }
                    else
                    {
                        pixels[y * 64 + x] = new Color32(255, 255, 0, 255);
                    }
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            string path = Path.Combine(Root, "pixel_transfer_test.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), tex.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);
            SetupSpriteImporter(path, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(4, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void VerifiesPixelPerfectExtraction()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("pixel_perfect");

            Texture2D tex = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            Color32[] pixels = new Color32[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(
                    (byte)(i % 256),
                    (byte)((i / 256) % 256),
                    (byte)((i / 65536) % 256),
                    255
                );
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            string path = Path.Combine(Root, "pixel_perfect_test.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), tex.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);
            SetupSpriteImporter(path, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(4, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        #endregion

        #region Texture Format Tests

        private static IEnumerable<TestCaseData> TextureFormatCases()
        {
            yield return new TestCaseData(TextureFormat.RGBA32).SetName("Format.RGBA32");
            yield return new TestCaseData(TextureFormat.RGB24).SetName("Format.RGB24");
            yield return new TestCaseData(TextureFormat.ARGB32).SetName("Format.ARGB32");
        }

        [Test]
        [TestCaseSource(nameof(TextureFormatCases))]
        public void ExtractionWorksWithDifferentTextureFormats(TextureFormat format)
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory($"format_{format}");

            Texture2D tex = Track(new Texture2D(64, 64, format, false));
            FillTexture(tex, Color.magenta);
            string path = Path.Combine(Root, $"format_{format}_test.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), tex.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);
            SetupSpriteImporter(path, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.Greater(extracted.Length, 0);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        private static IEnumerable<TestCaseData> VariousSpriteSizeCases()
        {
            yield return new TestCaseData(16, 16, 2, 2).SetName("SpriteSize.16x16.2x2");
            yield return new TestCaseData(32, 32, 2, 2).SetName("SpriteSize.32x32.2x2");
            yield return new TestCaseData(64, 64, 4, 4).SetName("SpriteSize.64x64.4x4");
            yield return new TestCaseData(128, 64, 4, 2).SetName("SpriteSize.128x64.4x2");
        }

        [Test]
        [TestCaseSource(nameof(VariousSpriteSizeCases))]
        public void ExtractionWorksWithVariousSpriteSizes(int width, int height, int cols, int rows)
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory($"size_{width}x{height}");
            CreateSpriteSheet($"size_{width}x{height}_test", width, height, cols, rows);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(cols * rows, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractsVariousSpriteSizes()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("various_sizes");
            CreateSpriteSheet("various_a", 32, 32, 2, 2);
            CreateSpriteSheet("various_b", 64, 64, 2, 2);
            CreateSpriteSheet("various_c", 128, 128, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(12, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        #endregion

        #region Pivot and Border Tests

        [Test]
        public void ExtractsSpritesWithVariousPivots()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("pivots");

            Texture2D tex = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            FillTexture(tex, Color.white);
            string path = Path.Combine(Root, "pivot_test.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), tex.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            SetSpriteSheet(
                importer,
                new SpriteMetaData[]
                {
                    new SpriteMetaData
                    {
                        name = "pivot_center",
                        rect = new Rect(0, 32, 32, 32),
                        pivot = new Vector2(0.5f, 0.5f),
                        alignment = (int)SpriteAlignment.Center,
                    },
                    new SpriteMetaData
                    {
                        name = "pivot_bottom_left",
                        rect = new Rect(32, 32, 32, 32),
                        pivot = new Vector2(0, 0),
                        alignment = (int)SpriteAlignment.BottomLeft,
                    },
                    new SpriteMetaData
                    {
                        name = "pivot_top_right",
                        rect = new Rect(0, 0, 32, 32),
                        pivot = new Vector2(1, 1),
                        alignment = (int)SpriteAlignment.TopRight,
                    },
                    new SpriteMetaData
                    {
                        name = "pivot_custom",
                        rect = new Rect(32, 0, 32, 32),
                        pivot = new Vector2(0.25f, 0.75f),
                        alignment = (int)SpriteAlignment.Custom,
                    },
                }
            );

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(4, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        [Test]
        public void ExtractsSpritesWithBorders()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("borders");

            Texture2D tex = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            FillTexture(tex, Color.gray);
            string path = Path.Combine(Root, "border_test.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), tex.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            SetSpriteSheet(
                importer,
                new SpriteMetaData[]
                {
                    new SpriteMetaData
                    {
                        name = "border_sprite_1",
                        rect = new Rect(0, 32, 32, 32),
                        border = new Vector4(4, 4, 4, 4),
                    },
                    new SpriteMetaData
                    {
                        name = "border_sprite_2",
                        rect = new Rect(32, 32, 32, 32),
                        border = new Vector4(8, 8, 8, 8),
                    },
                    new SpriteMetaData
                    {
                        name = "border_sprite_3",
                        rect = new Rect(0, 0, 32, 32),
                        border = new Vector4(2, 2, 2, 2),
                    },
                    new SpriteMetaData
                    {
                        name = "border_sprite_4",
                        rect = new Rect(32, 0, 32, 32),
                        border = new Vector4(0, 0, 0, 0),
                    },
                }
            );

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(4, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        #endregion

        #region Import Settings Tests

        [Test]
        public void PreserveImportSettingsCopiesSettings()
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory("preserve_settings");
            CreateSpriteSheet("preserve_settings_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor._preserveImportSettings = true;
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.Greater(extracted.Length, 0);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        #endregion

        #region ArrayPool Edge Case Tests

        private static IEnumerable<TestCaseData> ArrayPoolEdgeCaseDimensionCases()
        {
            yield return new TestCaseData(1023, 1023).SetName("ArrayPool.1023x1023");
            yield return new TestCaseData(1024, 1024).SetName("ArrayPool.1024x1024");
            yield return new TestCaseData(1025, 1025).SetName("ArrayPool.1025x1025");
        }

        [Test]
        [TestCaseSource(nameof(ArrayPoolEdgeCaseDimensionCases))]
        public void ExtractionWorksWithArrayPoolEdgeCaseDimensions(int width, int height)
        {
            string uniqueOutputDir = CreateUniqueOutputDirectory($"arraypool_{width}x{height}");
            CreateSpriteSheet($"arraypool_{width}x{height}_test", width, height, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<Object>(uniqueOutputDir);
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string[] extracted = Directory.GetFiles(RelToFull(uniqueOutputDir), "*.png");
            Assert.AreEqual(4, extracted.Length);

            CleanupExtractedFiles(uniqueOutputDir);
        }

        #endregion
    }
#endif
}
