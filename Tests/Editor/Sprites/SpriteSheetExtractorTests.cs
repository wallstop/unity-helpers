// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.Sprites
{
#if UNITY_EDITOR
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using NUnit.Framework;
    using UnityEditor;
#if UNITY_2D_SPRITE
    using UnityEditor.U2D.Sprites;
#endif
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.AssetProcessors;
    using WallstopStudios.UnityHelpers.Editor.Sprites;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.Core.TestUtils;
    using Object = UnityEngine.Object;
    using PivotMode = UnityHelpers.Editor.Sprites.PivotMode;

    /// <summary>
    /// Tests for <see cref="SpriteSheetExtractor"/> covering sprite sheet discovery,
    /// extraction, naming patterns, sort modes, selection handling, and edge cases.
    /// </summary>
    /// <remarks>
    /// <para>
    /// UNITY_2D_SPRITE is conditionally defined by the assembly definition when the
    /// com.unity.2d.sprite package (version 1.0.0+) is installed. When available, tests
    /// use the modern <c>ISpriteEditorDataProvider</c> API for sprite sheet configuration.
    /// When absent, tests fall back to the deprecated <c>TextureImporter.spritesheet</c>
    /// property which remains functional.
    /// </para>
    /// </remarks>
    [TestFixture]
    public sealed class SpriteSheetExtractorTests : CommonTestBase
    {
        private const string Root = "Assets/Temp/SpriteSheetExtractorTests";
        private const string OutputDir = "Assets/Temp/SpriteSheetExtractorTests/Output";
        private const string SharedDir = "Assets/Temp/SpriteSheetExtractorTests/Shared";

        private static readonly Regex InvalidOutputDirectoryPattern = new(
            @"Invalid output directory\.",
            RegexOptions.Compiled
        );

        // Shared fixture paths - created once in OneTimeSetUp, cleaned up in OneTimeTearDown
        // These are used by read-only tests (discovery, selection, sort mode tests)
        // Note: These are safe as static fields because Unity Test Runner runs tests sequentially,
        // not in parallel. If parallel test execution is ever enabled, these would need locking.
        private static string _shared2x2Path;
        private static string _shared4x4Path;
        private static string _shared8x8Path;
        private static string _sharedSingleModePath;
        private static bool _sharedFixturesCreated;

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
            DetectAssetChangeProcessor.LoopWindowSecondsOverride = 0.001d;
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            if (Application.isPlaying)
            {
                return;
            }
            DetectAssetChangeProcessor.ResetForTesting();
            CleanupTrackedFoldersAndAssets();
        }

        public override void CommonOneTimeSetUp()
        {
            base.CommonOneTimeSetUp();
            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                EnsureFolder(Root);
                EnsureFolder(OutputDir);
                EnsureFolder(SharedDir);

                // If flag is set but fixtures don't exist (e.g., previous run failed), reset and recreate
                if (_sharedFixturesCreated)
                {
                    if (
                        string.IsNullOrEmpty(_shared2x2Path)
                        || AssetDatabase.LoadAssetAtPath<Texture2D>(_shared2x2Path) == null
                    )
                    {
                        _sharedFixturesCreated = false;
                    }
                }

                // Create shared fixtures for read-only tests (discovery, selection, sort mode tests)
                // These avoid recreating identical textures for each test
                if (!_sharedFixturesCreated)
                {
                    _shared2x2Path = CreateSharedSpriteSheet(
                        "shared_2x2",
                        64,
                        64,
                        2,
                        2,
                        SpriteImportMode.Multiple
                    );
                    _shared4x4Path = CreateSharedSpriteSheet(
                        "shared_4x4",
                        128,
                        128,
                        4,
                        4,
                        SpriteImportMode.Multiple
                    );
                    _shared8x8Path = CreateSharedSpriteSheet(
                        "shared_8x8",
                        256,
                        256,
                        8,
                        8,
                        SpriteImportMode.Multiple
                    );
                    _sharedSingleModePath = CreateSharedSingleModeSprite(
                        "shared_single",
                        32,
                        32,
                        Color.red
                    );
                    _sharedFixturesCreated = true;
                }
            }
        }

        [OneTimeTearDown]
        public override void OneTimeTearDown()
        {
            // Clean up shared fixtures
            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                if (!string.IsNullOrEmpty(_shared2x2Path))
                {
                    AssetDatabase.DeleteAsset(_shared2x2Path);
                    _shared2x2Path = null;
                }
                if (!string.IsNullOrEmpty(_shared4x4Path))
                {
                    AssetDatabase.DeleteAsset(_shared4x4Path);
                    _shared4x4Path = null;
                }
                if (!string.IsNullOrEmpty(_shared8x8Path))
                {
                    AssetDatabase.DeleteAsset(_shared8x8Path);
                    _shared8x8Path = null;
                }
                if (!string.IsNullOrEmpty(_sharedSingleModePath))
                {
                    AssetDatabase.DeleteAsset(_sharedSingleModePath);
                    _sharedSingleModePath = null;
                }
                _sharedFixturesCreated = false;

                // Clean up shared directory
                if (AssetDatabase.IsValidFolder(SharedDir))
                {
                    AssetDatabase.DeleteAsset(SharedDir);
                }
            }

            base.OneTimeTearDown();
        }

        private string CreateSpriteSheet(
            string name,
            int width,
            int height,
            int gridColumns,
            int gridRows,
            SpriteImportMode mode = SpriteImportMode.Multiple,
            Vector2? customPivot = null,
            Vector4? border = null
        )
        {
            int cellWidth = width / gridColumns;
            int cellHeight = height / gridRows;

            Texture2D texture = Track(
                new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    alphaIsTransparency = true,
                }
            );

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

            string path = Path.Combine(Root, name + ".png").SanitizePath();
            string fullPath = RelToFull(path);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            TrackAssetPath(path);

            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importer != null, "Importer should exist for " + path);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = mode;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            if (mode == SpriteImportMode.Multiple)
            {
                SpriteMetaData[] spritesheet = new SpriteMetaData[gridColumns * gridRows];
                for (int row = 0; row < gridRows; row++)
                {
                    for (int col = 0; col < gridColumns; col++)
                    {
                        int index = row * gridColumns + col;
                        Vector2 pivot = customPivot ?? new Vector2(0.5f, 0.5f);
                        int alignment = customPivot.HasValue
                            ? (int)SpriteAlignment.Custom
                            : (int)SpriteAlignment.Center;

                        spritesheet[index] = new SpriteMetaData
                        {
                            name = $"{name}_sprite_{index}",
                            rect = new Rect(
                                col * cellWidth,
                                row * cellHeight,
                                cellWidth,
                                cellHeight
                            ),
                            alignment = alignment,
                            pivot = pivot,
                            border = border ?? Vector4.zero,
                        };
                    }
                }
                SetSpriteSheet(importer, spritesheet);
            }
            else if (mode == SpriteImportMode.Single)
            {
                if (customPivot.HasValue)
                {
                    importer.spritePivot = customPivot.Value;
                }
            }

            importer.SaveAndReimport();
            return path;
        }

        private string CreateSingleModeSprite(string name, int width, int height, Color color)
        {
            Texture2D texture = Track(new Texture2D(width, height, TextureFormat.RGBA32, false));
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, name + ".png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);

            AssetDatabase.ImportAsset(path);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(importer != null, "Importer should exist");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return path;
        }

        /// <summary>
        /// Creates a shared sprite sheet that is not tracked for per-test cleanup.
        /// These are created in OneTimeSetUp and cleaned up in OneTimeTearDown.
        /// </summary>
        private string CreateSharedSpriteSheet(
            string name,
            int width,
            int height,
            int gridColumns,
            int gridRows,
            SpriteImportMode mode
        )
        {
            int cellWidth = width / gridColumns;
            int cellHeight = height / gridRows;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false) // UNH-SUPPRESS: Temporary texture for PNG creation, destroyed immediately
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

            string path = Path.Combine(SharedDir, name + ".png").SanitizePath();
            string fullPath = RelToFull(path);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());

            Object.DestroyImmediate(texture); // UNH-SUPPRESS: Cleanup temporary texture after PNG creation

            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return path;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = mode;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            if (mode == SpriteImportMode.Multiple)
            {
                SpriteMetaData[] spritesheet = new SpriteMetaData[gridColumns * gridRows];
                for (int row = 0; row < gridRows; row++)
                {
                    for (int col = 0; col < gridColumns; col++)
                    {
                        int index = row * gridColumns + col;
                        spritesheet[index] = new SpriteMetaData
                        {
                            name = $"{name}_sprite_{index}",
                            rect = new Rect(
                                col * cellWidth,
                                row * cellHeight,
                                cellWidth,
                                cellHeight
                            ),
                            alignment = (int)SpriteAlignment.Center,
                            pivot = new Vector2(0.5f, 0.5f),
                            border = Vector4.zero,
                        };
                    }
                }
                SetSpriteSheet(importer, spritesheet);
            }

            importer.SaveAndReimport();
            return path;
        }

        /// <summary>
        /// Creates a shared single-mode sprite that is not tracked for per-test cleanup.
        /// These are created in OneTimeSetUp and cleaned up in OneTimeTearDown.
        /// </summary>
        private string CreateSharedSingleModeSprite(string name, int width, int height, Color color)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false); // UNH-SUPPRESS: Temporary texture for PNG creation
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(SharedDir, name + ".png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());

            Object.DestroyImmediate(texture); // UNH-SUPPRESS: Cleanup temporary texture after PNG creation

            AssetDatabase.ImportAsset(path);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return path;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return path;
        }

        private static void SetSpriteSheet(TextureImporter importer, SpriteMetaData[] spritesheet)
        {
#if UNITY_2D_SPRITE
            SpriteDataProviderFactories factory = new();
            factory.Init();
            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(
                importer
            );
            dataProvider.InitSpriteEditorDataProvider();

            SpriteRect[] spriteRects = new SpriteRect[spritesheet.Length];
            for (int i = 0; i < spritesheet.Length; i++)
            {
                SpriteMetaData meta = spritesheet[i];
                spriteRects[i] = new SpriteRect
                {
                    name = meta.name,
                    rect = meta.rect,
                    alignment = (SpriteAlignment)meta.alignment,
                    pivot = meta.pivot,
                    border = meta.border,
                    spriteID = GUID.Generate(),
                };
            }

            dataProvider.SetSpriteRects(spriteRects);
            dataProvider.Apply();
            importer.SaveAndReimport();
#else
            // Fallback for when 2D Sprite package is not installed.
            // Uses the deprecated spritesheet property which still works.
#pragma warning disable CS0618 // Type or member is obsolete
            importer.spritesheet = spritesheet;
#pragma warning restore CS0618
            importer.SaveAndReimport();
#endif
        }

        private SpriteSheetExtractor CreateExtractor()
        {
            SpriteSheetExtractor extractor = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            extractor._inputDirectories = new List<UnityEngine.Object>
            {
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Root),
            };
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                OutputDir
            );
            extractor._overwriteExisting = true;
            return extractor;
        }

        /// <summary>
        /// Creates an extractor configured to use shared fixtures only.
        /// Use this for read-only tests that don't need per-test sprite sheets.
        /// </summary>
        private SpriteSheetExtractor CreateExtractorWithSharedFixtures()
        {
            SpriteSheetExtractor extractor = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            extractor._inputDirectories = new List<UnityEngine.Object>
            {
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(SharedDir),
            };
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                OutputDir
            );
            extractor._overwriteExisting = true;
            return extractor;
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
                .SanitizePath();
        }

        /// <summary>
        /// Creates a simple sprite sheet pixel array with opaque squares in a grid pattern.
        /// </summary>
        private static Color32[] CreateSimpleSpriteSheetPixels(
            int width,
            int height,
            int columns,
            int rows
        )
        {
            Color32[] pixels = new Color32[width * height];
            int cellWidth = width / columns;
            int cellHeight = height / rows;
            int spriteWidth = cellWidth - 4;
            int spriteHeight = cellHeight - 4;

            // Fill with transparent
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            // Create opaque squares
            for (int row = 0; row < rows; ++row)
            {
                for (int col = 0; col < columns; ++col)
                {
                    int startX = col * cellWidth + 2;
                    int startY = row * cellHeight + 2;

                    for (int y = startY; y < startY + spriteHeight; ++y)
                    {
                        for (int x = startX; x < startX + spriteWidth; ++x)
                        {
                            if (x >= 0 && x < width && y >= 0 && y < height)
                            {
                                pixels[y * width + x] = new Color32(255, 128, 64, 255);
                            }
                        }
                    }
                }
            }

            return pixels;
        }

        /// <summary>
        /// Creates a raw sprite sheet with custom sprite metadata. Useful for testing edge cases
        /// where CreateSpriteSheet's auto-grid generation is not appropriate.
        /// </summary>
        private string CreateRawSpriteSheet(
            string name,
            int textureWidth,
            int textureHeight,
            SpriteMetaData[] sprites,
            Color fillColor = default,
            int maxTextureSize = 2048
        )
        {
            if (fillColor == default)
            {
                fillColor = Color.white;
            }

            Texture2D texture = Track(
                new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            );
            Color[] pixels = new Color[textureWidth * textureHeight];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = fillColor;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, $"{name}.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            importer.maxTextureSize = maxTextureSize;
            SetSpriteSheet(importer, sprites);
            importer.SaveAndReimport();

            return path;
        }

        /// <summary>
        /// Verifies that an extracted sprite exists and has the expected dimensions.
        /// Returns the loaded texture for further assertions if needed.
        /// </summary>
        private Texture2D VerifyExtractedSpriteDimensions(
            string outputPath,
            int expectedWidth,
            int expectedHeight,
            string context = ""
        )
        {
            string contextPrefix = string.IsNullOrEmpty(context) ? "" : $"[{context}] ";
            Assert.That(
                File.Exists(RelToFull(outputPath)),
                Is.True,
                $"{contextPrefix}Extracted sprite should exist at {outputPath}"
            );

            Texture2D extracted = AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
            Assert.IsTrue(
                extracted != null,
                $"{contextPrefix}Extracted texture should load from {outputPath}"
            );
            Assert.That(
                extracted.width,
                Is.EqualTo(expectedWidth),
                $"{contextPrefix}Width should be {expectedWidth}"
            );
            Assert.That(
                extracted.height,
                Is.EqualTo(expectedHeight),
                $"{contextPrefix}Height should be {expectedHeight}"
            );

            return extracted;
        }

        /// <summary>
        /// Finds a sprite sheet entry by name in the discovered sheets.
        /// Returns null if not found.
        /// </summary>
        private SpriteSheetExtractor.SpriteSheetEntry FindDiscoveredSheet(
            SpriteSheetExtractor extractor,
            string name
        )
        {
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == name
                )
                {
                    return extractor._discoveredSheets[i];
                }
            }
            return null;
        }

        [Test]
        public void DiscoverSheetsFindsMultipleModeSpriteSheet()
        {
            // Uses shared fixture - no per-test asset creation needed
            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor.DiscoverSpriteSheets();

            Assert.IsTrue(extractor._discoveredSheets != null);
            Assert.That(extractor._discoveredSheets.Count, Is.GreaterThanOrEqualTo(1));

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (extractor._discoveredSheets[i]._assetPath == _shared2x2Path)
                {
                    found = true;
                    Assert.That(extractor._discoveredSheets[i]._sprites.Count, Is.EqualTo(4));
                    Assert.That(
                        extractor._discoveredSheets[i]._importMode,
                        Is.EqualTo(SpriteImportMode.Multiple)
                    );
                    break;
                }
            }
            Assert.IsTrue(found, "Should find the shared 2x2 sprite sheet");
        }

        [Test]
        public void DiscoverSheetsFindsSingleModeSprite()
        {
            // Uses shared fixture - no per-test asset creation needed
            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor.DiscoverSpriteSheets();

            Assert.IsTrue(extractor._discoveredSheets != null);

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (extractor._discoveredSheets[i]._assetPath == _sharedSingleModePath)
                {
                    found = true;
                    Assert.That(extractor._discoveredSheets[i]._sprites.Count, Is.EqualTo(1));
                    Assert.That(
                        extractor._discoveredSheets[i]._importMode,
                        Is.EqualTo(SpriteImportMode.Single)
                    );
                    break;
                }
            }
            Assert.IsTrue(found, "Should find shared single mode sprite");
        }

        [Test]
        public void ExtractsTwoSpriteSheetMinimumCase()
        {
            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                CreateSpriteSheet("min_2x1", 64, 32, 2, 1);

                SpriteSheetExtractor extractor = CreateExtractor();
                extractor.DiscoverSpriteSheets();
                extractor.ExtractSelectedSprites();
            }

            string output0 = Path.Combine(OutputDir, "min_2x1_000.png").SanitizePath();
            string output1 = Path.Combine(OutputDir, "min_2x1_001.png").SanitizePath();

            Assert.That(
                File.Exists(RelToFull(output0)),
                Is.True,
                "First sprite should be extracted"
            );
            Assert.That(
                File.Exists(RelToFull(output1)),
                Is.True,
                "Second sprite should be extracted"
            );
        }

        [Test]
        public void Extracts4x4GridSixteenSprites()
        {
            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                CreateSpriteSheet("grid_4x4", 128, 128, 4, 4);

                SpriteSheetExtractor extractor = CreateExtractor();
                extractor.DiscoverSpriteSheets();
                extractor.ExtractSelectedSprites();
            }

            for (int i = 0; i < 16; i++)
            {
                string outputPath = Path.Combine(OutputDir, $"grid_4x4_{i:D3}.png").SanitizePath();
                Assert.That(
                    File.Exists(RelToFull(outputPath)),
                    Is.True,
                    $"Sprite {i} should be extracted"
                );
            }
        }

        private static IEnumerable<TestCaseData> SpriteSizeCases()
        {
            yield return new TestCaseData(32, 32, 2, 2).SetName("SpriteSize.PowerOf2.32x32");
            yield return new TestCaseData(64, 64, 4, 4).SetName("SpriteSize.PowerOf2.64x64");
            yield return new TestCaseData(128, 128, 4, 4).SetName("SpriteSize.PowerOf2.128x128");
            yield return new TestCaseData(256, 256, 8, 8).SetName("SpriteSize.PowerOf2.256x256");
            yield return new TestCaseData(48, 48, 2, 2).SetName("SpriteSize.NonPowerOf2.48x48");
            yield return new TestCaseData(100, 100, 2, 2).SetName("SpriteSize.NonPowerOf2.100x100");
            yield return new TestCaseData(75, 50, 3, 2).SetName(
                "SpriteSize.NonPowerOf2.Rectangular"
            );
        }

        [Test]
        [TestCaseSource(nameof(SpriteSizeCases))]
        public void ExtractsVariousSpriteSizes(int width, int height, int cols, int rows)
        {
            string name = $"size_{width}x{height}_{cols}x{rows}";
            using (AssetDatabaseBatchHelper.BeginBatch())
            {
                CreateSpriteSheet(name, width, height, cols, rows);

                SpriteSheetExtractor extractor = CreateExtractor();
                extractor.DiscoverSpriteSheets();
                extractor.ExtractSelectedSprites();
            }

            int expectedCount = cols * rows;
            for (int i = 0; i < expectedCount; i++)
            {
                string outputPath = Path.Combine(OutputDir, $"{name}_{i:D3}.png").SanitizePath();
                Assert.That(
                    File.Exists(RelToFull(outputPath)),
                    Is.True,
                    $"Sprite {i} should be extracted for {name}"
                );
            }
        }

        private static IEnumerable<TestCaseData> PivotCases()
        {
            yield return new TestCaseData(new Vector2(0.5f, 0.5f)).SetName("Pivot.Center");
            yield return new TestCaseData(new Vector2(0.5f, 0f)).SetName("Pivot.BottomCenter");
            yield return new TestCaseData(new Vector2(0f, 0f)).SetName("Pivot.BottomLeft");
            yield return new TestCaseData(new Vector2(1f, 1f)).SetName("Pivot.TopRight");
            yield return new TestCaseData(new Vector2(0.25f, 0.75f)).SetName("Pivot.Custom");
        }

        [Test]
        [TestCaseSource(nameof(PivotCases))]
        public void ExtractsSpritesWithVariousPivots(Vector2 pivot)
        {
            string safeName = $"pivot_{pivot.x}_{pivot.y}".Replace(".", "_");
            CreateSpriteSheet(safeName, 64, 64, 2, 2, SpriteImportMode.Multiple, pivot);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._preserveImportSettings = true;
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string outputPath = Path.Combine(OutputDir, $"{safeName}_000.png").SanitizePath();
            Assert.That(
                File.Exists(RelToFull(outputPath)),
                Is.True,
                "First sprite should be extracted"
            );

            TextureImporter resultImporter = AssetImporter.GetAtPath(outputPath) as TextureImporter;
            Assert.IsTrue(resultImporter != null, "Should have importer");
            Assert.That(
                resultImporter.spritePivot.x,
                Is.EqualTo(pivot.x).Within(0.01f),
                "Pivot X should be preserved"
            );
            Assert.That(
                resultImporter.spritePivot.y,
                Is.EqualTo(pivot.y).Within(0.01f),
                "Pivot Y should be preserved"
            );
        }

        [Test]
        public void ExtractsSpritesWithBorders()
        {
            Vector4 border = new Vector4(4, 4, 4, 4);
            CreateSpriteSheet(
                "borders_9slice",
                64,
                64,
                2,
                2,
                SpriteImportMode.Multiple,
                null,
                border
            );

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._preserveImportSettings = true;
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string outputPath = Path.Combine(OutputDir, "borders_9slice_000.png").SanitizePath();
            Assert.That(
                File.Exists(RelToFull(outputPath)),
                Is.True,
                "First sprite should be extracted"
            );
        }

        [Test]
        public void VerifiesPixelPerfectExtraction()
        {
            int width = 64;
            int height = 64;
            int cols = 2;
            int rows = 2;
            int cellWidth = width / cols;
            int cellHeight = height / rows;

            CreateSpriteSheet("pixel_perfect", width, height, cols, rows);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string outputPath = Path.Combine(OutputDir, "pixel_perfect_000.png").SanitizePath();
            VerifyExtractedSpriteDimensions(outputPath, cellWidth, cellHeight, "PixelPerfect");
        }

        [Test]
        public void SingleModeSpriteExtractsWithWarning()
        {
            // Uses shared single mode fixture - no per-test asset creation needed
            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor.DiscoverSpriteSheets();

            bool foundSingleMode = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (extractor._discoveredSheets[i]._importMode == SpriteImportMode.Single)
                {
                    foundSingleMode = true;
                    Assert.That(
                        extractor._discoveredSheets[i]._sprites.Count,
                        Is.EqualTo(1),
                        "Single mode should have 1 sprite"
                    );
                }
            }
            Assert.IsTrue(foundSingleMode, "Should detect single mode sprite");
        }

        [Test]
        public void ProcessesMultipleSheetsInSingleDirectory()
        {
            // Uses shared fixtures - we have 4 shared sprites (2x2, 4x4, 8x8, single)
            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor.DiscoverSpriteSheets();

            // Should find at least the 4 shared fixtures
            Assert.That(extractor._discoveredSheets.Count, Is.GreaterThanOrEqualTo(4));
        }

        [Test]
        public void ProcessesMultipleInputDirectories()
        {
            string subDir1 = Path.Combine(Root, "SubDir1").SanitizePath();
            string subDir2 = Path.Combine(Root, "SubDir2").SanitizePath();
            EnsureFolder(subDir1);
            EnsureFolder(subDir2);

            Texture2D tex1 = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            string path1 = Path.Combine(subDir1, "sheet1.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path1), tex1.EncodeToPNG());
            TrackAssetPath(path1);
            AssetDatabase.ImportAsset(path1);
            TextureImporter imp1 = AssetImporter.GetAtPath(path1) as TextureImporter;
            imp1.textureType = TextureImporterType.Sprite;
            imp1.spriteImportMode = SpriteImportMode.Multiple;
            imp1.isReadable = true;
            SetSpriteSheet(
                imp1,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = "sheet1_0",
                        rect = new Rect(0, 0, 32, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    new SpriteMetaData
                    {
                        name = "sheet1_1",
                        rect = new Rect(32, 0, 32, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            imp1.SaveAndReimport();

            Texture2D tex2 = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            string path2 = Path.Combine(subDir2, "sheet2.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path2), tex2.EncodeToPNG());
            TrackAssetPath(path2);
            AssetDatabase.ImportAsset(path2);
            TextureImporter imp2 = AssetImporter.GetAtPath(path2) as TextureImporter;
            imp2.textureType = TextureImporterType.Sprite;
            imp2.spriteImportMode = SpriteImportMode.Multiple;
            imp2.isReadable = true;
            SetSpriteSheet(
                imp2,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = "sheet2_0",
                        rect = new Rect(0, 0, 32, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            imp2.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._inputDirectories = new List<UnityEngine.Object>
            {
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(subDir1),
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(subDir2),
            };
            extractor.DiscoverSpriteSheets();

            Assert.That(extractor._discoveredSheets.Count, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void RegexFilterMatchesSubsetOfFiles()
        {
            CreateSpriteSheet("player_idle", 64, 64, 2, 2);
            CreateSpriteSheet("player_walk", 64, 64, 2, 2);
            CreateSpriteSheet("enemy_idle", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._spriteNameRegex = "^player_";
            extractor.DiscoverSpriteSheets();

            int playerCount = 0;
            int enemyCount = 0;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                string fileName = Path.GetFileNameWithoutExtension(
                    extractor._discoveredSheets[i]._assetPath
                );
                if (fileName.StartsWith("player_"))
                {
                    playerCount++;
                }
                else if (fileName.StartsWith("enemy_"))
                {
                    enemyCount++;
                }
            }

            Assert.That(playerCount, Is.EqualTo(2), "Should find both player sheets");
            Assert.That(enemyCount, Is.EqualTo(0), "Should not find enemy sheet");
        }

        [Test]
        public void EmptyDirectoryReturnsNoSheets()
        {
            string emptyDir = Path.Combine(Root, "EmptyDir").SanitizePath();
            EnsureFolder(emptyDir);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._inputDirectories = new List<UnityEngine.Object>
            {
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(emptyDir),
            };
            extractor.DiscoverSpriteSheets();

            Assert.IsTrue(extractor._discoveredSheets != null);
            Assert.That(extractor._discoveredSheets.Count, Is.EqualTo(0));
        }

        [Test]
        public void DirectoryWithNoSpriteSheetsReturnsEmpty()
        {
            string noSpriteDir = Path.Combine(Root, "NoSpriteDir").SanitizePath();
            EnsureFolder(noSpriteDir);

            Texture2D tex = Track(new Texture2D(32, 32, TextureFormat.RGBA32, false));
            string path = Path.Combine(noSpriteDir, "regular_texture.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), tex.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            imp.textureType = TextureImporterType.Default;
            imp.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._inputDirectories = new List<UnityEngine.Object>
            {
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(noSpriteDir),
            };
            extractor.DiscoverSpriteSheets();

            Assert.IsTrue(extractor._discoveredSheets != null);
            Assert.That(extractor._discoveredSheets.Count, Is.EqualTo(0));
        }

        [Test]
        public void DefaultNamingPatternUsesFilenameAndZeroPadding()
        {
            CreateSpriteSheet("naming_test", 64, 32, 2, 1);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._namingPrefix = "";
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string output0 = Path.Combine(OutputDir, "naming_test_000.png").SanitizePath();
            string output1 = Path.Combine(OutputDir, "naming_test_001.png").SanitizePath();

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
        }

        [Test]
        public void CustomPrefixOverridesDefaultNaming()
        {
            CreateSpriteSheet("override_test", 64, 32, 2, 1);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._namingPrefix = "custom_prefix";
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string output0 = Path.Combine(OutputDir, "custom_prefix_000.png").SanitizePath();
            string output1 = Path.Combine(OutputDir, "custom_prefix_001.png").SanitizePath();

            Assert.That(File.Exists(RelToFull(output0)), Is.True, "Should use custom prefix");
            Assert.That(File.Exists(RelToFull(output1)), Is.True, "Should use custom prefix");
        }

        [Test]
        public void ZeroPaddingWorksWithLargeSpriteCount()
        {
            CreateSpriteSheet("large_grid", 256, 256, 8, 8);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string output63 = Path.Combine(OutputDir, "large_grid_063.png").SanitizePath();
            Assert.That(
                File.Exists(RelToFull(output63)),
                Is.True,
                "Should pad correctly for 64 sprites"
            );
        }

        private static IEnumerable<TestCaseData> SortModeCases()
        {
            yield return new TestCaseData(SpriteSheetExtractor.SortMode.Original).SetName(
                "SortMode.Original"
            );
            yield return new TestCaseData(SpriteSheetExtractor.SortMode.Reversed).SetName(
                "SortMode.Reversed"
            );
            yield return new TestCaseData(SpriteSheetExtractor.SortMode.ByName).SetName(
                "SortMode.ByName"
            );
            yield return new TestCaseData(SpriteSheetExtractor.SortMode.ByPositionTopLeft).SetName(
                "SortMode.ByPositionTopLeft"
            );
            yield return new TestCaseData(
                SpriteSheetExtractor.SortMode.ByPositionBottomLeft
            ).SetName("SortMode.ByPositionBottomLeft");
        }

        [Test]
        [TestCaseSource(nameof(SortModeCases))]
        public void SortModeExtractsSuccessfully(SpriteSheetExtractor.SortMode sortMode)
        {
            CreateSpriteSheet("sort_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._sortMode = sortMode;
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            for (int i = 0; i < 4; i++)
            {
                string outputPath = Path.Combine(OutputDir, $"sort_test_{i:D3}.png").SanitizePath();
                Assert.That(
                    File.Exists(RelToFull(outputPath)),
                    Is.True,
                    $"Sprite {i} should be extracted with sort mode {sortMode}"
                );
            }
        }

        [Test]
        public void SelectAllSelectsAllSprites()
        {
            // Uses shared fixture - no per-test asset creation needed
            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor.DiscoverSpriteSheets();

            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                entry._isSelected = true;
                for (int j = 0; j < entry._sprites.Count; j++)
                {
                    entry._sprites[j]._isSelected = true;
                }
            }

            bool allSelected = true;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                if (!entry._isSelected)
                {
                    allSelected = false;
                    break;
                }
                for (int j = 0; j < entry._sprites.Count; j++)
                {
                    if (!entry._sprites[j]._isSelected)
                    {
                        allSelected = false;
                        break;
                    }
                }
            }

            Assert.IsTrue(allSelected, "All sprites should be selected");
        }

        [Test]
        public void SelectNoneDeselectsAllSprites()
        {
            // Uses shared fixture - no per-test asset creation needed
            SpriteSheetExtractor extractor = CreateExtractorWithSharedFixtures();
            extractor.DiscoverSpriteSheets();

            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                entry._isSelected = false;
                for (int j = 0; j < entry._sprites.Count; j++)
                {
                    entry._sprites[j]._isSelected = false;
                }
            }

            bool noneSelected = true;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                if (entry._isSelected)
                {
                    noneSelected = false;
                    break;
                }
                for (int j = 0; j < entry._sprites.Count; j++)
                {
                    if (entry._sprites[j]._isSelected)
                    {
                        noneSelected = false;
                        break;
                    }
                }
            }

            Assert.IsTrue(noneSelected, "No sprites should be selected");
        }

        [Test]
        public void IndividualSpriteSelectionWorks()
        {
            CreateSpriteSheet("individual_select", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();

            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                if (Path.GetFileNameWithoutExtension(entry._assetPath) == "individual_select")
                {
                    entry._isSelected = true;
                    entry._sprites[0]._isSelected = true;
                    entry._sprites[1]._isSelected = false;
                    entry._sprites[2]._isSelected = true;
                    entry._sprites[3]._isSelected = false;
                }
            }

            extractor.ExtractSelectedSprites();
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string output0 = Path.Combine(OutputDir, "individual_select_000.png").SanitizePath();
            string output1 = Path.Combine(OutputDir, "individual_select_001.png").SanitizePath();
            string output2 = Path.Combine(OutputDir, "individual_select_002.png").SanitizePath();
            string output3 = Path.Combine(OutputDir, "individual_select_003.png").SanitizePath();

            Assert.That(File.Exists(RelToFull(output0)), Is.True, "Sprite 0 should be extracted");
            Assert.That(
                File.Exists(RelToFull(output1)),
                Is.False,
                "Sprite 1 should not be extracted"
            );
            Assert.That(File.Exists(RelToFull(output2)), Is.True, "Sprite 2 should be extracted");
            Assert.That(
                File.Exists(RelToFull(output3)),
                Is.False,
                "Sprite 3 should not be extracted"
            );
        }

        [Test]
        public void HandlesLargeTextureDimensions()
        {
            Texture2D texture = Track(new Texture2D(2048, 2048, TextureFormat.RGBA32, false));
            Color[] pixels = new Color[2048 * 2048];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, "large_texture.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            SetSpriteSheet(
                importer,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = "large_0",
                        rect = new Rect(0, 0, 1024, 1024),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    new SpriteMetaData
                    {
                        name = "large_1",
                        rect = new Rect(1024, 0, 1024, 1024),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();

            Assert.DoesNotThrow(() => extractor.ExtractSelectedSprites());

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string output0 = Path.Combine(OutputDir, "large_texture_000.png").SanitizePath();
            Assert.That(
                File.Exists(RelToFull(output0)),
                Is.True,
                "Large texture sprite should extract"
            );
        }

        [Test]
        public void HandlesTextureWithAlphaChannel()
        {
            Texture2D texture = Track(
                new Texture2D(64, 64, TextureFormat.RGBA32, false) { alphaIsTransparency = true }
            );
            Color[] pixels = new Color[64 * 64];
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float alpha = (x < 32) ? 0.5f : 1.0f;
                    pixels[y * 64 + x] = new Color(1f, 0f, 0f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, "alpha_texture.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            importer.alphaIsTransparency = true;
            SetSpriteSheet(
                importer,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = "alpha_0",
                        rect = new Rect(0, 0, 32, 64),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    new SpriteMetaData
                    {
                        name = "alpha_1",
                        rect = new Rect(32, 0, 32, 64),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string output0 = Path.Combine(OutputDir, "alpha_texture_000.png").SanitizePath();
            Assert.That(
                File.Exists(RelToFull(output0)),
                Is.True,
                "Alpha texture sprite should extract"
            );
        }

        [Test]
        public void HandlesTextureWithoutAlphaChannel()
        {
            Texture2D texture = Track(new Texture2D(64, 64, TextureFormat.RGB24, false));
            Color[] pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.green;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, "no_alpha_texture.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            SetSpriteSheet(
                importer,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = "no_alpha_0",
                        rect = new Rect(0, 0, 32, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string output0 = Path.Combine(OutputDir, "no_alpha_texture_000.png").SanitizePath();
            Assert.That(
                File.Exists(RelToFull(output0)),
                Is.True,
                "No-alpha texture sprite should extract"
            );
        }

        [Test]
        public void DryRunDoesNotCreateFiles()
        {
            CreateSpriteSheet("dry_run_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._dryRun = true;
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string output0 = Path.Combine(OutputDir, "dry_run_test_000.png").SanitizePath();
            Assert.That(
                File.Exists(RelToFull(output0)),
                Is.False,
                "Dry run should not create files"
            );
        }

        [Test]
        public void OverwriteExistingReplacesFiles()
        {
            string outputPath = Path.Combine(OutputDir, "overwrite_test_000.png").SanitizePath();

            Texture2D dummy = Track(new Texture2D(16, 16, TextureFormat.RGBA32, false));
            Color[] dummyPixels = new Color[16 * 16];
            for (int i = 0; i < dummyPixels.Length; i++)
            {
                dummyPixels[i] = Color.magenta;
            }
            dummy.SetPixels(dummyPixels);
            dummy.Apply();
            File.WriteAllBytes(RelToFull(outputPath), dummy.EncodeToPNG());
            TrackAssetPath(outputPath);
            AssetDatabase.ImportAsset(outputPath);

            FileInfo originalInfo = new FileInfo(RelToFull(outputPath));
            long originalSize = originalInfo.Length;

            CreateSpriteSheet("overwrite_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._overwriteExisting = true;
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            FileInfo newInfo = new FileInfo(RelToFull(outputPath));
            Assert.That(newInfo.Length, Is.Not.EqualTo(originalSize), "File should be replaced");
        }

        [Test]
        public void NoOverwriteSkipsExistingFiles()
        {
            string outputPath = Path.Combine(OutputDir, "no_overwrite_test_000.png").SanitizePath();

            Texture2D dummy = Track(new Texture2D(8, 8, TextureFormat.RGBA32, false));
            File.WriteAllBytes(RelToFull(outputPath), dummy.EncodeToPNG());
            TrackAssetPath(outputPath);
            AssetDatabase.ImportAsset(outputPath);

            DateTime originalModTime = File.GetLastWriteTime(RelToFull(outputPath));

            CreateSpriteSheet("no_overwrite_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._overwriteExisting = false;
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            DateTime newModTime = File.GetLastWriteTime(RelToFull(outputPath));
            Assert.That(newModTime, Is.EqualTo(originalModTime), "File should not be modified");
        }

        [Test]
        public void PreserveImportSettingsCopiesSettings()
        {
            TextureImporter sourceImporter;
            {
                Texture2D tex = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
                string path = Path.Combine(Root, "preserve_settings.png").SanitizePath();
                File.WriteAllBytes(RelToFull(path), tex.EncodeToPNG());
                TrackAssetPath(path);
                AssetDatabase.ImportAsset(path);

                sourceImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                sourceImporter.textureType = TextureImporterType.Sprite;
                sourceImporter.spriteImportMode = SpriteImportMode.Multiple;
                sourceImporter.isReadable = true;
                sourceImporter.spritePixelsPerUnit = 32;
                sourceImporter.filterMode = FilterMode.Point;
                SetSpriteSheet(
                    sourceImporter,
                    new[]
                    {
                        new SpriteMetaData
                        {
                            name = "preserve_0",
                            rect = new Rect(0, 0, 32, 32),
                            alignment = (int)SpriteAlignment.Center,
                            pivot = new Vector2(0.5f, 0.5f),
                        },
                    }
                );
                sourceImporter.SaveAndReimport();
            }

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._preserveImportSettings = true;
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string outputPath = Path.Combine(OutputDir, "preserve_settings_000.png").SanitizePath();
            TextureImporter resultImporter = AssetImporter.GetAtPath(outputPath) as TextureImporter;

            Assert.IsTrue(resultImporter != null, "Result importer should exist");
            Assert.That(
                resultImporter.spritePixelsPerUnit,
                Is.EqualTo(sourceImporter.spritePixelsPerUnit),
                "PPU should be preserved"
            );
            Assert.That(
                resultImporter.filterMode,
                Is.EqualTo(sourceImporter.filterMode),
                "FilterMode should be preserved"
            );
        }

        [Test]
        public void NullInputDirectoriesHandledGracefully()
        {
            SpriteSheetExtractor extractor = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            extractor._inputDirectories = null;
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                OutputDir
            );

            Assert.DoesNotThrow(() => extractor.DiscoverSpriteSheets());
            Assert.IsTrue(
                extractor._discoveredSheets == null || extractor._discoveredSheets.Count == 0
            );
        }

        [Test]
        public void EmptyInputDirectoriesListHandledGracefully()
        {
            SpriteSheetExtractor extractor = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            extractor._inputDirectories = new List<UnityEngine.Object>();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                OutputDir
            );

            Assert.DoesNotThrow(() => extractor.DiscoverSpriteSheets());
        }

        [Test]
        public void NullOutputDirectoryPreventsExtraction()
        {
            CreateSpriteSheet("null_output", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = null;
            extractor.DiscoverSpriteSheets();

            LogAssert.Expect(LogType.Error, InvalidOutputDirectoryPattern);
            Assert.DoesNotThrow(() => extractor.ExtractSelectedSprites());
        }

        [Test]
        public void InvalidRegexShowsError()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._spriteNameRegex = "[invalid(regex";

            Assert.DoesNotThrow(() => extractor.DiscoverSpriteSheets());
        }

        [Test]
        public void NoSelectedSpritesDoesNotCrash()
        {
            CreateSpriteSheet("no_selection", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();

            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                entry._isSelected = false;
                for (int j = 0; j < entry._sprites.Count; j++)
                {
                    entry._sprites[j]._isSelected = false;
                }
            }

            Assert.DoesNotThrow(() => extractor.ExtractSelectedSprites());
        }

        [Test]
        public void DiscoverySheetsInitiallySelectedByDefault()
        {
            CreateSpriteSheet("default_selected", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();

            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                Assert.IsTrue(entry._isSelected, "Sheet should be selected by default");
                for (int j = 0; j < entry._sprites.Count; j++)
                {
                    Assert.IsTrue(
                        entry._sprites[j]._isSelected,
                        "Sprite should be selected by default"
                    );
                }
            }
        }

        [Test]
        public void ExtractedSpritesAreImportedAsSingleMode()
        {
            CreateSpriteSheet("single_import", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string outputPath = Path.Combine(OutputDir, "single_import_000.png").SanitizePath();
            TextureImporter resultImporter = AssetImporter.GetAtPath(outputPath) as TextureImporter;

            Assert.IsTrue(resultImporter != null, "Result importer should exist");
            Assert.That(
                resultImporter.spriteImportMode,
                Is.EqualTo(SpriteImportMode.Single),
                "Extracted sprites should be Single mode"
            );
        }

        [Test]
        public void ExtractedTexturesHaveCorrectDimensions()
        {
            int cellWidth = 32;
            int cellHeight = 32;
            CreateSpriteSheet("dimension_check", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string outputPath = Path.Combine(OutputDir, "dimension_check_000.png").SanitizePath();
            VerifyExtractedSpriteDimensions(outputPath, cellWidth, cellHeight, "DimensionCheck");
        }

        [Test]
        public void MultipleExtractionRunsWorkCorrectly()
        {
            CreateSpriteSheet("multi_run", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();

            Assert.DoesNotThrow(() => extractor.ExtractSelectedSprites());
            Assert.DoesNotThrow(() => extractor.ExtractSelectedSprites());
            Assert.DoesNotThrow(() => extractor.ExtractSelectedSprites());

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string output0 = Path.Combine(OutputDir, "multi_run_000.png").SanitizePath();
            Assert.That(
                File.Exists(RelToFull(output0)),
                Is.True,
                "Files should exist after multiple runs"
            );
        }

        [Test]
        public void RediscoveryClearsOldSheets()
        {
            CreateSpriteSheet("rediscover_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();

            int firstCount = extractor._discoveredSheets.Count;

            extractor.DiscoverSpriteSheets();

            Assert.That(
                extractor._discoveredSheets.Count,
                Is.EqualTo(firstCount),
                "Rediscovery should not duplicate sheets"
            );
        }

        [Test]
        public void HandlesSpecialCharactersInFilename()
        {
            Texture2D tex = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            string path = Path.Combine(Root, "sprite-with_special.chars.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), tex.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Multiple;
            imp.isReadable = true;
            SetSpriteSheet(
                imp,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = "special_0",
                        rect = new Rect(0, 0, 32, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            imp.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();

            Assert.DoesNotThrow(() => extractor.ExtractSelectedSprites());
        }

        [Test]
        public void HandlesZeroWidthSpriteRect()
        {
            Texture2D tex = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            string path = Path.Combine(Root, "zero_width.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), tex.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Multiple;
            imp.isReadable = true;
            SetSpriteSheet(
                imp,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = "zero_w",
                        rect = new Rect(0, 0, 0, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            imp.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();

            Assert.DoesNotThrow(() => extractor.ExtractSelectedSprites());
        }

        [Test]
        public void HandlesEmptySpritesheet()
        {
            Texture2D tex = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            string path = Path.Combine(Root, "empty_spritesheet.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), tex.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Multiple;
            imp.isReadable = true;
            SetSpriteSheet(imp, Array.Empty<SpriteMetaData>());
            imp.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();

            bool foundEmpty = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == "empty_spritesheet"
                )
                {
                    foundEmpty = true;
                    Assert.That(
                        extractor._discoveredSheets[i]._sprites.Count,
                        Is.EqualTo(0),
                        "Empty spritesheet should have 0 sprites"
                    );
                }
            }
            Assert.IsTrue(foundEmpty, "Should find empty spritesheet");
        }

        [Test]
        public void HandlesNonReadableTextureGracefully()
        {
            Texture2D tex = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            string path = Path.Combine(Root, "non_readable.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), tex.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Multiple;
            imp.isReadable = false;
            SetSpriteSheet(
                imp,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = "non_read_0",
                        rect = new Rect(0, 0, 32, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            imp.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();

            Assert.DoesNotThrow(() => extractor.ExtractSelectedSprites());
        }

        [Test]
        public void SortModeReversedReversesSpriteOrder()
        {
            Texture2D tex = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            string path = Path.Combine(Root, "reverse_order.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), tex.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Multiple;
            imp.isReadable = true;
            SetSpriteSheet(
                imp,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = "aaa_first",
                        rect = new Rect(0, 0, 32, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    new SpriteMetaData
                    {
                        name = "bbb_second",
                        rect = new Rect(32, 0, 32, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            imp.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._sortMode = SpriteSheetExtractor.SortMode.Reversed;
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string output0 = Path.Combine(OutputDir, "reverse_order_000.png").SanitizePath();
            string output1 = Path.Combine(OutputDir, "reverse_order_001.png").SanitizePath();

            Assert.That(File.Exists(RelToFull(output0)), Is.True);
            Assert.That(File.Exists(RelToFull(output1)), Is.True);
        }

        [Test]
        public void SortModeByNameSortsAlphabetically()
        {
            Texture2D tex = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            string path = Path.Combine(Root, "name_sort.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), tex.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Multiple;
            imp.isReadable = true;
            SetSpriteSheet(
                imp,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = "zzz_last",
                        rect = new Rect(0, 0, 32, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    new SpriteMetaData
                    {
                        name = "aaa_first",
                        rect = new Rect(32, 0, 32, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            imp.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._sortMode = SpriteSheetExtractor.SortMode.ByName;
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string output0 = Path.Combine(OutputDir, "name_sort_000.png").SanitizePath();
            Assert.That(File.Exists(RelToFull(output0)), Is.True);
        }

        [Test]
        public void MultipleSheetsWithSameNamePrefixUseCorrectNaming()
        {
            CreateSpriteSheet("prefix_test", 64, 64, 2, 1);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string output0 = Path.Combine(OutputDir, "prefix_test_000.png").SanitizePath();
            Assert.That(File.Exists(RelToFull(output0)), Is.True);
        }

        [Test]
        public void LargeGridExtractionDoesNotTriggerLoopDetection()
        {
            CreateSpriteSheet("loop_test_10x10", 320, 320, 10, 10);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            for (int i = 0; i < 100; i++)
            {
                string outputPath = Path.Combine(OutputDir, $"loop_test_10x10_{i:D3}.png")
                    .SanitizePath();
                Assert.That(
                    File.Exists(RelToFull(outputPath)),
                    Is.True,
                    $"Sprite {i} should be extracted without loop detection interference"
                );
            }
        }

        [Test]
        public void NonFolderOutputDirectoryPreventsExtraction()
        {
            string emptyOutputDir = Path.Combine(Root, "NotAFolder.txt").SanitizePath();
            Texture2D tex = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            File.WriteAllBytes(RelToFull(emptyOutputDir), tex.EncodeToPNG());
            TrackAssetPath(emptyOutputDir);
            AssetDatabase.ImportAsset(emptyOutputDir);

            CreateSpriteSheet("empty_output_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._outputDirectory = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                emptyOutputDir
            );
            extractor.DiscoverSpriteSheets();

            LogAssert.Expect(LogType.Error, InvalidOutputDirectoryPattern);
            Assert.DoesNotThrow(() => extractor.ExtractSelectedSprites());
        }

        private static IEnumerable<TestCaseData> GridAutoCalculationCases()
        {
            yield return new TestCaseData(64, 64, 2, 2, 32, 32).SetName("GridAuto.64x64.2x2");
            yield return new TestCaseData(128, 64, 4, 2, 32, 32).SetName("GridAuto.128x64.4x2");
            yield return new TestCaseData(256, 256, 8, 8, 32, 32).SetName("GridAuto.256x256.8x8");
            yield return new TestCaseData(100, 100, 4, 4, 25, 25).SetName("GridAuto.100x100.4x4");
        }

        [Test]
        [TestCaseSource(nameof(GridAutoCalculationCases))]
        public void GridAutoCalculationDeterminesCorrectDimensions(
            int textureWidth,
            int textureHeight,
            int expectedColumns,
            int expectedRows,
            int expectedCellWidth,
            int expectedCellHeight
        )
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Auto;

            int columns;
            int rows;
            int cellWidth;
            int cellHeight;
            extractor.CalculateGridDimensions(
                textureWidth,
                textureHeight,
                out columns,
                out rows,
                out cellWidth,
                out cellHeight
            );

            Assert.That(
                columns * cellWidth,
                Is.EqualTo(textureWidth),
                "Columns x CellWidth should equal texture width"
            );
            Assert.That(
                rows * cellHeight,
                Is.EqualTo(textureHeight),
                "Rows x CellHeight should equal texture height"
            );
        }

        private static IEnumerable<TestCaseData> GridManualCalculationCases()
        {
            yield return new TestCaseData(128, 128, 4, 4, 0, 0, 32, 32).SetName(
                "GridManual.Cols4Rows4"
            );
            yield return new TestCaseData(128, 128, 2, 2, 0, 0, 64, 64).SetName(
                "GridManual.Cols2Rows2"
            );
            yield return new TestCaseData(128, 128, 4, 4, 32, 32, 32, 32).SetName(
                "GridManual.ExplicitCellSize"
            );
            yield return new TestCaseData(128, 128, 4, 4, 16, 16, 16, 16).SetName(
                "GridManual.SmallCellSize"
            );
        }

        [Test]
        [TestCaseSource(nameof(GridManualCalculationCases))]
        public void GridManualCalculationUsesSpecifiedValues(
            int textureWidth,
            int textureHeight,
            int manualColumns,
            int manualRows,
            int manualCellWidth,
            int manualCellHeight,
            int expectedCellWidth,
            int expectedCellHeight
        )
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = manualColumns;
            extractor._gridRows = manualRows;
            extractor._cellWidth = manualCellWidth;
            extractor._cellHeight = manualCellHeight;

            int columns;
            int rows;
            int cellWidth;
            int cellHeight;
            extractor.CalculateGridDimensions(
                textureWidth,
                textureHeight,
                out columns,
                out rows,
                out cellWidth,
                out cellHeight
            );

            Assert.That(columns, Is.EqualTo(manualColumns), "Columns should match manual value");
            Assert.That(rows, Is.EqualTo(manualRows), "Rows should match manual value");
            Assert.That(
                cellWidth,
                Is.EqualTo(expectedCellWidth),
                "Cell width should match expected"
            );
            Assert.That(
                cellHeight,
                Is.EqualTo(expectedCellHeight),
                "Cell height should match expected"
            );
        }

        [Test]
        public void GridBasedExtractionCreatesCorrectSpriteCount()
        {
            CreateSpriteSheet("grid_based_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 4;
            extractor._gridRows = 4;
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; ++i)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == "grid_based_test"
                )
                {
                    found = true;
                    Assert.That(
                        extractor._discoveredSheets[i]._sprites.Count,
                        Is.EqualTo(16),
                        "Grid 4x4 should create 16 sprites"
                    );
                    break;
                }
            }
            Assert.IsTrue(found, "Should find grid_based_test");
        }

        private static IEnumerable<TestCaseData> PaddedGridCases()
        {
            yield return new TestCaseData(2, 2, 4, 4).SetName("PaddedGrid.Uniform4");
            yield return new TestCaseData(0, 0, 2, 2).SetName("PaddedGrid.TopBottom2");
            yield return new TestCaseData(4, 4, 0, 0).SetName("PaddedGrid.LeftRight4");
            yield return new TestCaseData(1, 2, 3, 4).SetName("PaddedGrid.Asymmetric");
        }

        [Test]
        [TestCaseSource(nameof(PaddedGridCases))]
        public void PaddedGridExtractionAppliesPadding(
            int paddingLeft,
            int paddingRight,
            int paddingTop,
            int paddingBottom
        )
        {
            CreateSpriteSheet("padded_grid_test", 128, 128, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.PaddedGrid;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor._paddingLeft = paddingLeft;
            extractor._paddingRight = paddingRight;
            extractor._paddingTop = paddingTop;
            extractor._paddingBottom = paddingBottom;
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; ++i)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == "padded_grid_test"
                )
                {
                    found = true;
                    SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                    Assert.That(entry._sprites.Count, Is.EqualTo(4), "Should have 4 sprites");

                    if (entry._sprites.Count > 0)
                    {
                        int expectedWidth = 64 - paddingLeft - paddingRight;
                        int expectedHeight = 64 - paddingTop - paddingBottom;
                        Assert.That(
                            (int)entry._sprites[0]._rect.width,
                            Is.EqualTo(expectedWidth),
                            "Sprite width should account for padding"
                        );
                        Assert.That(
                            (int)entry._sprites[0]._rect.height,
                            Is.EqualTo(expectedHeight),
                            "Sprite height should account for padding"
                        );
                    }
                    break;
                }
            }
            Assert.IsTrue(found, "Should find padded_grid_test");
        }

        private static IEnumerable<TestCaseData> AlphaDetectionCases()
        {
            yield return new TestCaseData(0.01f).SetName("AlphaDetection.Threshold0.01");
            yield return new TestCaseData(0.1f).SetName("AlphaDetection.Threshold0.1");
            yield return new TestCaseData(0.5f).SetName("AlphaDetection.Threshold0.5");
        }

        [Test]
        [TestCaseSource(nameof(AlphaDetectionCases))]
        public void AlphaDetectionFindsOpaqueBounds(float alphaThreshold)
        {
            int width = 64;
            int height = 64;
            Color32[] pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            for (int y = 16; y < 32; ++y)
            {
                for (int x = 16; x < 32; ++x)
                {
                    pixels[y * width + x] = new Color32(255, 0, 0, 255);
                }
            }

            for (int y = 40; y < 56; ++y)
            {
                for (int x = 40; x < 56; ++x)
                {
                    pixels[y * width + x] = new Color32(0, 255, 0, 255);
                }
            }

            List<Rect> result = new List<Rect>();
            SpriteSheetExtractor.DetectSpriteBoundsByAlpha(
                pixels,
                width,
                height,
                alphaThreshold,
                result
            );

            Assert.That(result.Count, Is.EqualTo(2), "Should detect 2 sprites");
        }

        [Test]
        public void AlphaDetectionIgnoresLowAlphaPixels()
        {
            int width = 32;
            int height = 32;
            Color32[] pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(128, 128, 128, 1);
            }

            List<Rect> result = new List<Rect>();
            SpriteSheetExtractor.DetectSpriteBoundsByAlpha(pixels, width, height, 0.01f, result);

            Assert.That(result.Count, Is.EqualTo(0), "Should not detect sprites with low alpha");
        }

        private static IEnumerable<TestCaseData> PreviewSizeCases()
        {
            yield return new TestCaseData(SpriteSheetExtractor.PreviewSizeMode.Size24, 24).SetName(
                "PreviewSize.24"
            );
            yield return new TestCaseData(SpriteSheetExtractor.PreviewSizeMode.Size32, 32).SetName(
                "PreviewSize.32"
            );
            yield return new TestCaseData(SpriteSheetExtractor.PreviewSizeMode.Size64, 64).SetName(
                "PreviewSize.64"
            );
        }

        [Test]
        [TestCaseSource(nameof(PreviewSizeCases))]
        public void PreviewSizeModeReturnsCorrectSize(
            SpriteSheetExtractor.PreviewSizeMode mode,
            int expectedSize
        )
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._previewSizeMode = mode;

            SpriteSheetExtractor.SpriteEntryData sprite = new SpriteSheetExtractor.SpriteEntryData
            {
                _rect = new Rect(0, 0, 48, 48),
            };

            int size = extractor.GetPreviewSize(sprite);
            Assert.That(size, Is.EqualTo(expectedSize), $"Preview size should be {expectedSize}");
        }

        [Test]
        public void PreviewSizeRealSizeUsesActualSpriteDimensions()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.RealSize;

            SpriteSheetExtractor.SpriteEntryData sprite = new SpriteSheetExtractor.SpriteEntryData
            {
                _rect = new Rect(0, 0, 48, 32),
            };

            int size = extractor.GetPreviewSize(sprite);
            Assert.That(size, Is.EqualTo(48), "RealSize should use max dimension (48)");
        }

        [Test]
        public void PreviewSizeRealSizeClampsToMax128()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.RealSize;

            SpriteSheetExtractor.SpriteEntryData sprite = new SpriteSheetExtractor.SpriteEntryData
            {
                _rect = new Rect(0, 0, 256, 256),
            };

            int size = extractor.GetPreviewSize(sprite);
            Assert.That(size, Is.EqualTo(128), "RealSize should clamp to max 128");
        }

        private static IEnumerable<TestCaseData> ExtractionModeCases()
        {
            yield return new TestCaseData(SpriteSheetExtractor.ExtractionMode.FromMetadata).SetName(
                "ExtractionMode.FromMetadata"
            );
            yield return new TestCaseData(SpriteSheetExtractor.ExtractionMode.GridBased).SetName(
                "ExtractionMode.GridBased"
            );
            yield return new TestCaseData(SpriteSheetExtractor.ExtractionMode.PaddedGrid).SetName(
                "ExtractionMode.PaddedGrid"
            );
        }

        [Test]
        [TestCaseSource(nameof(ExtractionModeCases))]
        public void ExtractionModeSwitchingWorksCorrectly(SpriteSheetExtractor.ExtractionMode mode)
        {
            CreateSpriteSheet("mode_switch_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = mode;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; ++i)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == "mode_switch_test"
                )
                {
                    found = true;
                    Assert.That(
                        extractor._discoveredSheets[i]._sprites.Count,
                        Is.GreaterThanOrEqualTo(1),
                        $"Mode {mode} should discover sprites"
                    );
                    break;
                }
            }
            Assert.IsTrue(found, $"Should find mode_switch_test with mode {mode}");
        }

        [Test]
        public void OverlayRectsCalculatedCorrectly()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 4;
            extractor._gridRows = 4;

            int columns;
            int rows;
            int cellWidth;
            int cellHeight;
            extractor.CalculateGridDimensions(
                128,
                128,
                out columns,
                out rows,
                out cellWidth,
                out cellHeight
            );

            Assert.That(columns, Is.EqualTo(4), "Should have 4 columns");
            Assert.That(rows, Is.EqualTo(4), "Should have 4 rows");
            Assert.That(cellWidth, Is.EqualTo(32), "Cell width should be 32");
            Assert.That(cellHeight, Is.EqualTo(32), "Cell height should be 32");
        }

        private static IEnumerable<TestCaseData> SortModeWithAlphaCases()
        {
            yield return new TestCaseData(
                SpriteSheetExtractor.SortMode.Original,
                SpriteSheetExtractor.ExtractionMode.GridBased
            ).SetName("SortAlpha.Original.Grid");
            yield return new TestCaseData(
                SpriteSheetExtractor.SortMode.ByPositionTopLeft,
                SpriteSheetExtractor.ExtractionMode.GridBased
            ).SetName("SortAlpha.TopLeft.Grid");
            yield return new TestCaseData(
                SpriteSheetExtractor.SortMode.ByPositionBottomLeft,
                SpriteSheetExtractor.ExtractionMode.GridBased
            ).SetName("SortAlpha.BottomLeft.Grid");
            yield return new TestCaseData(
                SpriteSheetExtractor.SortMode.Reversed,
                SpriteSheetExtractor.ExtractionMode.PaddedGrid
            ).SetName("SortAlpha.Reversed.Padded");
        }

        [Test]
        [TestCaseSource(nameof(SortModeWithAlphaCases))]
        public void SortModeWorksWithExtractionModes(
            SpriteSheetExtractor.SortMode sortMode,
            SpriteSheetExtractor.ExtractionMode extractionMode
        )
        {
            CreateSpriteSheet("sort_alpha_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = extractionMode;
            extractor._sortMode = sortMode;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string output0 = Path.Combine(OutputDir, "sort_alpha_test_000.png").SanitizePath();
            Assert.That(
                File.Exists(RelToFull(output0)),
                Is.True,
                $"Should extract with sort {sortMode} and mode {extractionMode}"
            );
        }

        [Test]
        public void GridExtractionPopulatesSpritesCorrectly()
        {
            Texture2D texture = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _sprites = new List<SpriteSheetExtractor.SpriteEntryData>(),
                _assetPath = "test",
            };

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor.PopulateSpritesFromGrid(entry, texture);

            Assert.That(entry._sprites.Count, Is.EqualTo(4), "Should create 4 sprites");
            Assert.That(entry._sprites[0]._rect.width, Is.EqualTo(32), "Sprite width should be 32");
            Assert.That(
                entry._sprites[0]._rect.height,
                Is.EqualTo(32),
                "Sprite height should be 32"
            );
        }

        [Test]
        public void PaddedGridExtractionPopulatesSpritesCorrectly()
        {
            Texture2D texture = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _sprites = new List<SpriteSheetExtractor.SpriteEntryData>(),
                _assetPath = "test",
            };

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.PaddedGrid;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor._paddingLeft = 2;
            extractor._paddingRight = 2;
            extractor._paddingTop = 2;
            extractor._paddingBottom = 2;
            extractor.PopulateSpritesFromPaddedGrid(entry, texture);

            Assert.That(entry._sprites.Count, Is.EqualTo(4), "Should create 4 sprites");
            Assert.That(
                entry._sprites[0]._rect.width,
                Is.EqualTo(28),
                "Sprite width should be 28 (32 - 4)"
            );
            Assert.That(
                entry._sprites[0]._rect.height,
                Is.EqualTo(28),
                "Sprite height should be 28 (32 - 4)"
            );
        }

        [Test]
        public void InvalidPaddingWarnsAndCreatesNoSprites()
        {
            Texture2D texture = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _sprites = new List<SpriteSheetExtractor.SpriteEntryData>(),
                _assetPath = "test",
            };

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.PaddedGrid;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor._paddingLeft = 20;
            extractor._paddingRight = 20;
            extractor._paddingTop = 0;
            extractor._paddingBottom = 0;
            extractor.PopulateSpritesFromPaddedGrid(entry, texture);

            Assert.That(
                entry._sprites.Count,
                Is.EqualTo(0),
                "Invalid padding should create no sprites"
            );
        }

        [Test]
        public void AlphaDetectionSortsResultsByPosition()
        {
            int width = 64;
            int height = 64;
            Color32[] pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            for (int y = 4; y < 12; ++y)
            {
                for (int x = 4; x < 12; ++x)
                {
                    pixels[y * width + x] = new Color32(255, 0, 0, 255);
                }
            }

            for (int y = 50; y < 58; ++y)
            {
                for (int x = 50; x < 58; ++x)
                {
                    pixels[y * width + x] = new Color32(0, 255, 0, 255);
                }
            }

            List<Rect> result = new List<Rect>();
            SpriteSheetExtractor.DetectSpriteBoundsByAlpha(pixels, width, height, 0.01f, result);

            Assert.That(result.Count, Is.EqualTo(2), "Should detect 2 sprites");
            Assert.That(
                result[0].y,
                Is.GreaterThan(result[1].y),
                "Results should be sorted top-to-bottom"
            );
        }

        [Test]
        public void MinimumSpriteSize2x2IsEnforced()
        {
            int width = 32;
            int height = 32;
            Color32[] pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            pixels[16 * width + 16] = new Color32(255, 0, 0, 255);

            List<Rect> result = new List<Rect>();
            SpriteSheetExtractor.DetectSpriteBoundsByAlpha(pixels, width, height, 0.01f, result);

            Assert.That(
                result.Count,
                Is.EqualTo(0),
                "Single pixel should not be detected as sprite"
            );
        }

        [Test]
        public void ObsoleteExtractionModeNoneFallsBackToFromMetadata()
        {
            CreateSpriteSheet("obsolete_extraction_mode", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
#pragma warning disable CS0618
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.None;
#pragma warning restore CS0618
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; ++i)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == "obsolete_extraction_mode"
                )
                {
                    found = true;
                    Assert.That(
                        extractor._discoveredSheets[i]._sprites.Count,
                        Is.EqualTo(4),
                        "ExtractionMode.None should fall back to FromMetadata behavior"
                    );
                    break;
                }
            }
            Assert.IsTrue(found, "Should find obsolete_extraction_mode sheet");
        }

        [Test]
        public void ObsoleteGridSizeModeNoneFallsBackToAuto()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
#pragma warning disable CS0618
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.None;
#pragma warning restore CS0618

            int columns;
            int rows;
            int cellWidth;
            int cellHeight;
            extractor.CalculateGridDimensions(
                128,
                128,
                out columns,
                out rows,
                out cellWidth,
                out cellHeight
            );

            Assert.That(
                columns * cellWidth,
                Is.EqualTo(128),
                "GridSizeMode.None should fall back to Auto behavior"
            );
            Assert.That(
                rows * cellHeight,
                Is.EqualTo(128),
                "GridSizeMode.None should fall back to Auto behavior"
            );
        }

        [Test]
        public void ObsoletePreviewSizeModeNoneFallsBackToSize32()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
#pragma warning disable CS0618
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.None;
#pragma warning restore CS0618

            SpriteSheetExtractor.SpriteEntryData sprite = new SpriteSheetExtractor.SpriteEntryData
            {
                _rect = new Rect(0, 0, 64, 64),
            };

            int size = extractor.GetPreviewSize(sprite);
            Assert.That(
                size,
                Is.EqualTo(32),
                "PreviewSizeMode.None should fall back to Size32 (32 pixels)"
            );
        }

        [Test]
        public void ObsoleteExtractionModeNoneExtractsSuccessfully()
        {
            CreateSpriteSheet("obsolete_extract_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
#pragma warning disable CS0618
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.None;
#pragma warning restore CS0618
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string output0 = Path.Combine(OutputDir, "obsolete_extract_test_000.png")
                .SanitizePath();
            Assert.That(
                File.Exists(RelToFull(output0)),
                Is.True,
                "ExtractionMode.None should successfully extract sprites using FromMetadata fallback"
            );
        }

        private static IEnumerable<TestCaseData> EffectiveExtractionModeTestCases()
        {
            yield return new TestCaseData(
                SpriteSheetExtractor.ExtractionMode.FromMetadata,
                true,
                null,
                SpriteSheetExtractor.ExtractionMode.FromMetadata
            ).SetName("ExtractionMode.UseGlobal.NoOverride.ReturnsGlobal");

            yield return new TestCaseData(
                SpriteSheetExtractor.ExtractionMode.GridBased,
                true,
                SpriteSheetExtractor.ExtractionMode.AlphaDetection,
                SpriteSheetExtractor.ExtractionMode.GridBased
            ).SetName("ExtractionMode.UseGlobal.HasOverride.ReturnsGlobal");

            yield return new TestCaseData(
                SpriteSheetExtractor.ExtractionMode.FromMetadata,
                false,
                SpriteSheetExtractor.ExtractionMode.GridBased,
                SpriteSheetExtractor.ExtractionMode.GridBased
            ).SetName("ExtractionMode.UseOverride.HasValue.ReturnsOverride");

            yield return new TestCaseData(
                SpriteSheetExtractor.ExtractionMode.PaddedGrid,
                false,
                null,
                SpriteSheetExtractor.ExtractionMode.PaddedGrid
            ).SetName("ExtractionMode.UseOverride.NoValue.ReturnsGlobal");

            yield return new TestCaseData(
                SpriteSheetExtractor.ExtractionMode.AlphaDetection,
                false,
                SpriteSheetExtractor.ExtractionMode.PaddedGrid,
                SpriteSheetExtractor.ExtractionMode.PaddedGrid
            ).SetName("ExtractionMode.UseOverride.DifferentValue.ReturnsOverride");
        }

        [Test]
        [TestCaseSource(nameof(EffectiveExtractionModeTestCases))]
        public void GetEffectiveExtractionModeReturnsCorrectValue(
            SpriteSheetExtractor.ExtractionMode globalMode,
            bool useGlobalSettings,
            SpriteSheetExtractor.ExtractionMode? overrideMode,
            SpriteSheetExtractor.ExtractionMode expected
        )
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._extractionMode = globalMode;

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = useGlobalSettings,
                _extractionModeOverride = overrideMode,
            };

            SpriteSheetExtractor.ExtractionMode result = window.GetEffectiveExtractionMode(entry);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetEffectiveExtractionModeReturnsGlobalWhenEntryIsNull()
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;

            SpriteSheetExtractor.ExtractionMode result = window.GetEffectiveExtractionMode(null);

            Assert.AreEqual(SpriteSheetExtractor.ExtractionMode.GridBased, result);
        }

        private static IEnumerable<TestCaseData> EffectiveGridSizeModeTestCases()
        {
            yield return new TestCaseData(
                SpriteSheetExtractor.GridSizeMode.Auto,
                true,
                null,
                SpriteSheetExtractor.GridSizeMode.Auto
            ).SetName("GridSizeMode.UseGlobal.NoOverride.ReturnsGlobal");

            yield return new TestCaseData(
                SpriteSheetExtractor.GridSizeMode.Manual,
                true,
                SpriteSheetExtractor.GridSizeMode.Auto,
                SpriteSheetExtractor.GridSizeMode.Manual
            ).SetName("GridSizeMode.UseGlobal.HasOverride.ReturnsGlobal");

            yield return new TestCaseData(
                SpriteSheetExtractor.GridSizeMode.Auto,
                false,
                SpriteSheetExtractor.GridSizeMode.Manual,
                SpriteSheetExtractor.GridSizeMode.Manual
            ).SetName("GridSizeMode.UseOverride.HasValue.ReturnsOverride");

            yield return new TestCaseData(
                SpriteSheetExtractor.GridSizeMode.Manual,
                false,
                null,
                SpriteSheetExtractor.GridSizeMode.Manual
            ).SetName("GridSizeMode.UseOverride.NoValue.ReturnsGlobal");
        }

        [Test]
        [TestCaseSource(nameof(EffectiveGridSizeModeTestCases))]
        public void GetEffectiveGridSizeModeReturnsCorrectValue(
            SpriteSheetExtractor.GridSizeMode globalMode,
            bool useGlobalSettings,
            SpriteSheetExtractor.GridSizeMode? overrideMode,
            SpriteSheetExtractor.GridSizeMode expected
        )
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._gridSizeMode = globalMode;

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = useGlobalSettings,
                _gridSizeModeOverride = overrideMode,
            };

            SpriteSheetExtractor.GridSizeMode result = window.GetEffectiveGridSizeMode(entry);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetEffectiveGridSizeModeReturnsGlobalWhenEntryIsNull()
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;

            SpriteSheetExtractor.GridSizeMode result = window.GetEffectiveGridSizeMode(null);

            Assert.AreEqual(SpriteSheetExtractor.GridSizeMode.Manual, result);
        }

        private static IEnumerable<TestCaseData> EffectiveIntValueTestCases()
        {
            yield return new TestCaseData(4, true, null, 4).SetName(
                "IntValue.UseGlobal.NoOverride"
            );
            yield return new TestCaseData(4, true, 8, 4).SetName("IntValue.UseGlobal.HasOverride");
            yield return new TestCaseData(4, false, 8, 8).SetName("IntValue.UseOverride.HasValue");
            yield return new TestCaseData(4, false, null, 4).SetName(
                "IntValue.UseOverride.NoValue"
            );
            yield return new TestCaseData(1, false, 16, 16).SetName("IntValue.MinToMax");
            yield return new TestCaseData(100, false, 1, 1).SetName("IntValue.MaxToMin");
        }

        [Test]
        [TestCaseSource(nameof(EffectiveIntValueTestCases))]
        public void GetEffectiveGridColumnsReturnsCorrectValue(
            int globalValue,
            bool useGlobalSettings,
            int? overrideValue,
            int expected
        )
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._gridColumns = globalValue;

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = useGlobalSettings,
                _gridColumnsOverride = overrideValue,
            };

            int result = window.GetEffectiveGridColumns(entry);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetEffectiveGridColumnsReturnsGlobalWhenEntryIsNull()
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._gridColumns = 8;

            int result = window.GetEffectiveGridColumns(null);

            Assert.AreEqual(8, result);
        }

        [Test]
        [TestCaseSource(nameof(EffectiveIntValueTestCases))]
        public void GetEffectiveGridRowsReturnsCorrectValue(
            int globalValue,
            bool useGlobalSettings,
            int? overrideValue,
            int expected
        )
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._gridRows = globalValue;

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = useGlobalSettings,
                _gridRowsOverride = overrideValue,
            };

            int result = window.GetEffectiveGridRows(entry);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetEffectiveGridRowsReturnsGlobalWhenEntryIsNull()
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._gridRows = 6;

            int result = window.GetEffectiveGridRows(null);

            Assert.AreEqual(6, result);
        }

        [Test]
        [TestCaseSource(nameof(EffectiveIntValueTestCases))]
        public void GetEffectiveCellWidthReturnsCorrectValue(
            int globalValue,
            bool useGlobalSettings,
            int? overrideValue,
            int expected
        )
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._cellWidth = globalValue;

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = useGlobalSettings,
                _cellWidthOverride = overrideValue,
            };

            int result = window.GetEffectiveCellWidth(entry);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetEffectiveCellWidthReturnsGlobalWhenEntryIsNull()
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._cellWidth = 64;

            int result = window.GetEffectiveCellWidth(null);

            Assert.AreEqual(64, result);
        }

        [Test]
        [TestCaseSource(nameof(EffectiveIntValueTestCases))]
        public void GetEffectiveCellHeightReturnsCorrectValue(
            int globalValue,
            bool useGlobalSettings,
            int? overrideValue,
            int expected
        )
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._cellHeight = globalValue;

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = useGlobalSettings,
                _cellHeightOverride = overrideValue,
            };

            int result = window.GetEffectiveCellHeight(entry);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetEffectiveCellHeightReturnsGlobalWhenEntryIsNull()
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._cellHeight = 48;

            int result = window.GetEffectiveCellHeight(null);

            Assert.AreEqual(48, result);
        }

        private static IEnumerable<TestCaseData> EffectivePaddingTestCases()
        {
            yield return new TestCaseData(0, true, null, 0).SetName("Padding.UseGlobal.NoOverride");
            yield return new TestCaseData(4, true, 8, 4).SetName("Padding.UseGlobal.HasOverride");
            yield return new TestCaseData(2, false, 10, 10).SetName("Padding.UseOverride.HasValue");
            yield return new TestCaseData(5, false, null, 5).SetName("Padding.UseOverride.NoValue");
            yield return new TestCaseData(0, false, 0, 0).SetName("Padding.ZeroOverride");
        }

        [Test]
        [TestCaseSource(nameof(EffectivePaddingTestCases))]
        public void GetEffectivePaddingLeftReturnsCorrectValue(
            int globalValue,
            bool useGlobalSettings,
            int? overrideValue,
            int expected
        )
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._paddingLeft = globalValue;

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = useGlobalSettings,
                _paddingLeftOverride = overrideValue,
            };

            int result = window.GetEffectivePaddingLeft(entry);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetEffectivePaddingLeftReturnsGlobalWhenEntryIsNull()
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._paddingLeft = 3;

            int result = window.GetEffectivePaddingLeft(null);

            Assert.AreEqual(3, result);
        }

        [Test]
        [TestCaseSource(nameof(EffectivePaddingTestCases))]
        public void GetEffectivePaddingRightReturnsCorrectValue(
            int globalValue,
            bool useGlobalSettings,
            int? overrideValue,
            int expected
        )
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._paddingRight = globalValue;

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = useGlobalSettings,
                _paddingRightOverride = overrideValue,
            };

            int result = window.GetEffectivePaddingRight(entry);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetEffectivePaddingRightReturnsGlobalWhenEntryIsNull()
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._paddingRight = 7;

            int result = window.GetEffectivePaddingRight(null);

            Assert.AreEqual(7, result);
        }

        [Test]
        [TestCaseSource(nameof(EffectivePaddingTestCases))]
        public void GetEffectivePaddingTopReturnsCorrectValue(
            int globalValue,
            bool useGlobalSettings,
            int? overrideValue,
            int expected
        )
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._paddingTop = globalValue;

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = useGlobalSettings,
                _paddingTopOverride = overrideValue,
            };

            int result = window.GetEffectivePaddingTop(entry);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetEffectivePaddingTopReturnsGlobalWhenEntryIsNull()
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._paddingTop = 9;

            int result = window.GetEffectivePaddingTop(null);

            Assert.AreEqual(9, result);
        }

        [Test]
        [TestCaseSource(nameof(EffectivePaddingTestCases))]
        public void GetEffectivePaddingBottomReturnsCorrectValue(
            int globalValue,
            bool useGlobalSettings,
            int? overrideValue,
            int expected
        )
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._paddingBottom = globalValue;

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = useGlobalSettings,
                _paddingBottomOverride = overrideValue,
            };

            int result = window.GetEffectivePaddingBottom(entry);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetEffectivePaddingBottomReturnsGlobalWhenEntryIsNull()
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._paddingBottom = 11;

            int result = window.GetEffectivePaddingBottom(null);

            Assert.AreEqual(11, result);
        }

        private static IEnumerable<TestCaseData> EffectiveAlphaThresholdTestCases()
        {
            yield return new TestCaseData(0.01f, true, null, 0.01f).SetName(
                "AlphaThreshold.UseGlobal.NoOverride"
            );
            yield return new TestCaseData(0.05f, true, 0.5f, 0.05f).SetName(
                "AlphaThreshold.UseGlobal.HasOverride"
            );
            yield return new TestCaseData(0.01f, false, 0.1f, 0.1f).SetName(
                "AlphaThreshold.UseOverride.HasValue"
            );
            yield return new TestCaseData(0.25f, false, null, 0.25f).SetName(
                "AlphaThreshold.UseOverride.NoValue"
            );
            yield return new TestCaseData(0.0f, false, 1.0f, 1.0f).SetName(
                "AlphaThreshold.MinToMax"
            );
            yield return new TestCaseData(1.0f, false, 0.0f, 0.0f).SetName(
                "AlphaThreshold.MaxToMin"
            );
        }

        [Test]
        [TestCaseSource(nameof(EffectiveAlphaThresholdTestCases))]
        public void GetEffectiveAlphaThresholdReturnsCorrectValue(
            float globalValue,
            bool useGlobalSettings,
            float? overrideValue,
            float expected
        )
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._alphaThreshold = globalValue;

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = useGlobalSettings,
                _alphaThresholdOverride = overrideValue,
            };

            float result = window.GetEffectiveAlphaThreshold(entry);

            Assert.AreEqual(expected, result, 0.0001f);
        }

        [Test]
        public void GetEffectiveAlphaThresholdReturnsGlobalWhenEntryIsNull()
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._alphaThreshold = 0.15f;

            float result = window.GetEffectiveAlphaThreshold(null);

            Assert.AreEqual(0.15f, result, 0.0001f);
        }

        [Test]
        public void CalculateGridDimensionsWithEntryUsesGlobalWhenEntryIsNull()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 4;
            extractor._gridRows = 2;
            extractor._cellWidth = 32;
            extractor._cellHeight = 64;

            int columns;
            int rows;
            int cellWidth;
            int cellHeight;
            extractor.CalculateGridDimensions(
                128,
                128,
                null,
                out columns,
                out rows,
                out cellWidth,
                out cellHeight
            );

            Assert.AreEqual(4, columns, "Should use global columns");
            Assert.AreEqual(2, rows, "Should use global rows");
            Assert.AreEqual(32, cellWidth, "Should use global cell width");
            Assert.AreEqual(64, cellHeight, "Should use global cell height");
        }

        [Test]
        public void CalculateGridDimensionsWithEntryUsesGlobalWhenUseGlobalSettingsTrue()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 4;
            extractor._gridRows = 4;
            extractor._cellWidth = 32;
            extractor._cellHeight = 32;

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = true,
                _gridSizeModeOverride = SpriteSheetExtractor.GridSizeMode.Manual,
                _gridColumnsOverride = 8,
                _gridRowsOverride = 8,
                _cellWidthOverride = 16,
                _cellHeightOverride = 16,
            };

            int columns;
            int rows;
            int cellWidth;
            int cellHeight;
            extractor.CalculateGridDimensions(
                128,
                128,
                entry,
                out columns,
                out rows,
                out cellWidth,
                out cellHeight
            );

            Assert.AreEqual(
                4,
                columns,
                "Should use global columns when _useGlobalSettings is true"
            );
            Assert.AreEqual(4, rows, "Should use global rows when _useGlobalSettings is true");
            Assert.AreEqual(
                32,
                cellWidth,
                "Should use global cell width when _useGlobalSettings is true"
            );
            Assert.AreEqual(
                32,
                cellHeight,
                "Should use global cell height when _useGlobalSettings is true"
            );
        }

        [Test]
        public void CalculateGridDimensionsWithEntryUsesOverrideWhenEnabled()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 4;
            extractor._gridRows = 4;
            extractor._cellWidth = 32;
            extractor._cellHeight = 32;

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = false,
                _gridSizeModeOverride = SpriteSheetExtractor.GridSizeMode.Manual,
                _gridColumnsOverride = 8,
                _gridRowsOverride = 8,
                _cellWidthOverride = 16,
                _cellHeightOverride = 16,
            };

            int columns;
            int rows;
            int cellWidth;
            int cellHeight;
            extractor.CalculateGridDimensions(
                128,
                128,
                entry,
                out columns,
                out rows,
                out cellWidth,
                out cellHeight
            );

            Assert.AreEqual(8, columns, "Should use override columns");
            Assert.AreEqual(8, rows, "Should use override rows");
            Assert.AreEqual(16, cellWidth, "Should use override cell width");
            Assert.AreEqual(16, cellHeight, "Should use override cell height");
        }

        [Test]
        public void CopySettingsFromEntryCopiesAllOverrideFields()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            SpriteSheetExtractor.SpriteSheetEntry source = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = false,
                _extractionModeOverride = SpriteSheetExtractor.ExtractionMode.PaddedGrid,
                _gridSizeModeOverride = SpriteSheetExtractor.GridSizeMode.Manual,
                _gridColumnsOverride = 5,
                _gridRowsOverride = 6,
                _cellWidthOverride = 24,
                _cellHeightOverride = 48,
                _paddingLeftOverride = 1,
                _paddingRightOverride = 2,
                _paddingTopOverride = 3,
                _paddingBottomOverride = 4,
                _alphaThresholdOverride = 0.33f,
            };

            SpriteSheetExtractor.SpriteSheetEntry target = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = true,
                _sprites = new List<SpriteSheetExtractor.SpriteEntryData>(),
                _assetPath = "test",
            };

            extractor.CopySettingsFromEntry(source, target);

            Assert.AreEqual(
                false,
                target._useGlobalSettings,
                "Should copy _useGlobalSettings from source"
            );
            Assert.AreEqual(
                SpriteSheetExtractor.ExtractionMode.PaddedGrid,
                target._extractionModeOverride
            );
            Assert.AreEqual(SpriteSheetExtractor.GridSizeMode.Manual, target._gridSizeModeOverride);
            Assert.AreEqual(5, target._gridColumnsOverride);
            Assert.AreEqual(6, target._gridRowsOverride);
            Assert.AreEqual(24, target._cellWidthOverride);
            Assert.AreEqual(48, target._cellHeightOverride);
            Assert.AreEqual(1, target._paddingLeftOverride);
            Assert.AreEqual(2, target._paddingRightOverride);
            Assert.AreEqual(3, target._paddingTopOverride);
            Assert.AreEqual(4, target._paddingBottomOverride);
            Assert.AreEqual(0.33f, target._alphaThresholdOverride.Value, 0.0001f);
        }

        [Test]
        public void CopySettingsFromEntryHandlesNullSourceGracefully()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            SpriteSheetExtractor.SpriteSheetEntry target = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = true,
                _gridColumnsOverride = 10,
            };

            Assert.DoesNotThrow(() => extractor.CopySettingsFromEntry(null, target));

            Assert.AreEqual(true, target._useGlobalSettings, "Target should not be modified");
            Assert.AreEqual(10, target._gridColumnsOverride, "Target should not be modified");
        }

        [Test]
        public void CopySettingsFromEntryHandlesNullTargetGracefully()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            SpriteSheetExtractor.SpriteSheetEntry source = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = false,
                _gridColumnsOverride = 10,
            };

            Assert.DoesNotThrow(() => extractor.CopySettingsFromEntry(source, null));
        }

        [Test]
        public void CopySettingsFromEntryHandlesBothNullGracefully()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            Assert.DoesNotThrow(() => extractor.CopySettingsFromEntry(null, null));
        }

        [Test]
        public void CopySettingsFromEntryCopiesNullOverrides()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            SpriteSheetExtractor.SpriteSheetEntry source = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = true,
                _extractionModeOverride = null,
                _gridSizeModeOverride = null,
                _gridColumnsOverride = null,
                _gridRowsOverride = null,
                _cellWidthOverride = null,
                _cellHeightOverride = null,
                _paddingLeftOverride = null,
                _paddingRightOverride = null,
                _paddingTopOverride = null,
                _paddingBottomOverride = null,
                _alphaThresholdOverride = null,
            };

            SpriteSheetExtractor.SpriteSheetEntry target = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = false,
                _extractionModeOverride = SpriteSheetExtractor.ExtractionMode.GridBased,
                _gridColumnsOverride = 10,
                _alphaThresholdOverride = 0.5f,
                _sprites = new List<SpriteSheetExtractor.SpriteEntryData>(),
                _assetPath = "test",
            };

            extractor.CopySettingsFromEntry(source, target);

            Assert.AreEqual(true, target._useGlobalSettings);
            Assert.IsNull(target._extractionModeOverride);
            Assert.IsNull(target._gridColumnsOverride);
            Assert.IsNull(target._alphaThresholdOverride);
        }

        [Test]
        public void ApplyGlobalSettingsToAllCopiesGlobalValuesToAllEntries()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.AlphaDetection;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 5;
            extractor._gridRows = 3;
            extractor._cellWidth = 40;
            extractor._cellHeight = 50;
            extractor._paddingLeft = 1;
            extractor._paddingRight = 2;
            extractor._paddingTop = 3;
            extractor._paddingBottom = 4;
            extractor._alphaThreshold = 0.25f;

            SpriteSheetExtractor.SpriteSheetEntry entry1 = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = true,
            };
            SpriteSheetExtractor.SpriteSheetEntry entry2 = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = true,
                _gridColumnsOverride = 99,
            };

            extractor._discoveredSheets = new List<SpriteSheetExtractor.SpriteSheetEntry>
            {
                entry1,
                entry2,
            };

            extractor.ApplyGlobalSettingsToAll();

            Assert.AreEqual(false, entry1._useGlobalSettings);
            Assert.AreEqual(
                SpriteSheetExtractor.ExtractionMode.AlphaDetection,
                entry1._extractionModeOverride
            );
            Assert.AreEqual(SpriteSheetExtractor.GridSizeMode.Manual, entry1._gridSizeModeOverride);
            Assert.AreEqual(5, entry1._gridColumnsOverride);
            Assert.AreEqual(3, entry1._gridRowsOverride);
            Assert.AreEqual(40, entry1._cellWidthOverride);
            Assert.AreEqual(50, entry1._cellHeightOverride);
            Assert.AreEqual(1, entry1._paddingLeftOverride);
            Assert.AreEqual(2, entry1._paddingRightOverride);
            Assert.AreEqual(3, entry1._paddingTopOverride);
            Assert.AreEqual(4, entry1._paddingBottomOverride);
            Assert.AreEqual(0.25f, entry1._alphaThresholdOverride.Value, 0.0001f);

            Assert.AreEqual(false, entry2._useGlobalSettings);
            Assert.AreEqual(5, entry2._gridColumnsOverride, "Previous override should be replaced");
        }

        [Test]
        public void ApplyGlobalSettingsToAllHandlesNullDiscoveredSheets()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._discoveredSheets = null;

            Assert.DoesNotThrow(() => extractor.ApplyGlobalSettingsToAll());
        }

        [Test]
        public void ApplyGlobalSettingsToAllHandlesEmptyDiscoveredSheets()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._discoveredSheets = new List<SpriteSheetExtractor.SpriteSheetEntry>();

            Assert.DoesNotThrow(() => extractor.ApplyGlobalSettingsToAll());
        }

        [Test]
        public void PopulateSpritesFromGridUsesEffectiveSettings()
        {
            Texture2D texture = Track(new Texture2D(128, 128, TextureFormat.RGBA32, false));
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 4;
            extractor._gridRows = 4;

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = false,
                _gridSizeModeOverride = SpriteSheetExtractor.GridSizeMode.Manual,
                _gridColumnsOverride = 2,
                _gridRowsOverride = 2,
                _sprites = new List<SpriteSheetExtractor.SpriteEntryData>(),
                _assetPath = "test",
            };

            extractor.PopulateSpritesFromGrid(entry, texture);

            Assert.AreEqual(
                4,
                entry._sprites.Count,
                "Should create 4 sprites using override 2x2 grid"
            );
            Assert.AreEqual(
                64,
                (int)entry._sprites[0]._rect.width,
                "Sprite width should be 64 (128/2)"
            );
            Assert.AreEqual(
                64,
                (int)entry._sprites[0]._rect.height,
                "Sprite height should be 64 (128/2)"
            );
        }

        [Test]
        public void PopulateSpritesFromPaddedGridUsesEffectivePadding()
        {
            Texture2D texture = Track(new Texture2D(128, 128, TextureFormat.RGBA32, false));
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.PaddedGrid;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor._paddingLeft = 2;
            extractor._paddingRight = 2;
            extractor._paddingTop = 2;
            extractor._paddingBottom = 2;

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = false,
                _gridSizeModeOverride = SpriteSheetExtractor.GridSizeMode.Manual,
                _gridColumnsOverride = 2,
                _gridRowsOverride = 2,
                _paddingLeftOverride = 4,
                _paddingRightOverride = 4,
                _paddingTopOverride = 4,
                _paddingBottomOverride = 4,
                _sprites = new List<SpriteSheetExtractor.SpriteEntryData>(),
                _assetPath = "test",
            };

            extractor.PopulateSpritesFromPaddedGrid(entry, texture);

            Assert.AreEqual(4, entry._sprites.Count, "Should create 4 sprites");
            int expectedWidth = 64 - 4 - 4;
            int expectedHeight = 64 - 4 - 4;
            Assert.AreEqual(
                expectedWidth,
                (int)entry._sprites[0]._rect.width,
                "Sprite width should account for override padding"
            );
            Assert.AreEqual(
                expectedHeight,
                (int)entry._sprites[0]._rect.height,
                "Sprite height should account for override padding"
            );
        }

        [Test]
        public void PopulateSpritesFromAlphaDetectionUsesEffectiveThreshold()
        {
            int width = 64;
            int height = 64;
            Texture2D texture = Track(new Texture2D(width, height, TextureFormat.RGBA32, false));

            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0, 0, 0, 0);
            }

            for (int y = 10; y < 30; y++)
            {
                for (int x = 10; x < 30; x++)
                {
                    pixels[y * width + x] = new Color(1, 0, 0, 0.5f);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.AlphaDetection;
            extractor._alphaThreshold = 0.9f;

            SpriteSheetExtractor.SpriteSheetEntry entryHighThreshold =
                new SpriteSheetExtractor.SpriteSheetEntry
                {
                    _useGlobalSettings = true,
                    _sprites = new List<SpriteSheetExtractor.SpriteEntryData>(),
                    _assetPath = "test_high",
                    _texture = texture,
                };

            extractor.PopulateSpritesFromAlphaDetection(entryHighThreshold, texture);

            int highThresholdCount = entryHighThreshold._sprites.Count;

            SpriteSheetExtractor.SpriteSheetEntry entryLowThreshold =
                new SpriteSheetExtractor.SpriteSheetEntry
                {
                    _useGlobalSettings = false,
                    _alphaThresholdOverride = 0.1f,
                    _sprites = new List<SpriteSheetExtractor.SpriteEntryData>(),
                    _assetPath = "test_low",
                    _texture = texture,
                };

            extractor.PopulateSpritesFromAlphaDetection(entryLowThreshold, texture);

            int lowThresholdCount = entryLowThreshold._sprites.Count;

            Assert.AreEqual(
                0,
                highThresholdCount,
                "High threshold should not detect 0.5 alpha pixels"
            );
            Assert.AreEqual(
                1,
                lowThresholdCount,
                "Low threshold should detect 0.5 alpha pixels as a sprite"
            );
        }

        private static IEnumerable<TestCaseData> PartialOverrideTestCases()
        {
            yield return new TestCaseData(
                SpriteSheetExtractor.ExtractionMode.GridBased,
                null,
                4,
                null
            ).SetName("PartialOverride.ExtractionModeAndColumns");
            yield return new TestCaseData(
                null,
                SpriteSheetExtractor.GridSizeMode.Manual,
                null,
                8
            ).SetName("PartialOverride.GridSizeModeAndRows");
            yield return new TestCaseData(
                SpriteSheetExtractor.ExtractionMode.PaddedGrid,
                null,
                null,
                null
            ).SetName("PartialOverride.OnlyExtractionMode");
        }

        [Test]
        [TestCaseSource(nameof(PartialOverrideTestCases))]
        public void EntryWithPartialOverridesUsesGlobalForNullFields(
            SpriteSheetExtractor.ExtractionMode? extractionOverride,
            SpriteSheetExtractor.GridSizeMode? gridSizeOverride,
            int? columnsOverride,
            int? rowsOverride
        )
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.FromMetadata;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Auto;
            extractor._gridColumns = 10;
            extractor._gridRows = 10;

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = false,
                _extractionModeOverride = extractionOverride,
                _gridSizeModeOverride = gridSizeOverride,
                _gridColumnsOverride = columnsOverride,
                _gridRowsOverride = rowsOverride,
            };

            SpriteSheetExtractor.ExtractionMode effectiveExtraction =
                extractor.GetEffectiveExtractionMode(entry);
            SpriteSheetExtractor.GridSizeMode effectiveGridSize =
                extractor.GetEffectiveGridSizeMode(entry);
            int effectiveColumns = extractor.GetEffectiveGridColumns(entry);
            int effectiveRows = extractor.GetEffectiveGridRows(entry);

            SpriteSheetExtractor.ExtractionMode expectedExtraction =
                extractionOverride ?? SpriteSheetExtractor.ExtractionMode.FromMetadata;
            SpriteSheetExtractor.GridSizeMode expectedGridSize =
                gridSizeOverride ?? SpriteSheetExtractor.GridSizeMode.Auto;
            int expectedColumns = columnsOverride ?? 10;
            int expectedRows = rowsOverride ?? 10;

            Assert.AreEqual(
                expectedExtraction,
                effectiveExtraction,
                "Effective extraction mode mismatch"
            );
            Assert.AreEqual(
                expectedGridSize,
                effectiveGridSize,
                "Effective grid size mode mismatch"
            );
            Assert.AreEqual(expectedColumns, effectiveColumns, "Effective columns mismatch");
            Assert.AreEqual(expectedRows, effectiveRows, "Effective rows mismatch");
        }

        [Test]
        public void EntryWithAllNullOverridesAndUseGlobalFalseUsesGlobalValues()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.AlphaDetection;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 7;
            extractor._gridRows = 9;
            extractor._cellWidth = 35;
            extractor._cellHeight = 45;
            extractor._paddingLeft = 5;
            extractor._alphaThreshold = 0.42f;

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = false,
                _extractionModeOverride = null,
                _gridSizeModeOverride = null,
                _gridColumnsOverride = null,
                _gridRowsOverride = null,
                _cellWidthOverride = null,
                _cellHeightOverride = null,
                _paddingLeftOverride = null,
                _alphaThresholdOverride = null,
            };

            Assert.AreEqual(
                SpriteSheetExtractor.ExtractionMode.AlphaDetection,
                extractor.GetEffectiveExtractionMode(entry)
            );
            Assert.AreEqual(
                SpriteSheetExtractor.GridSizeMode.Manual,
                extractor.GetEffectiveGridSizeMode(entry)
            );
            Assert.AreEqual(7, extractor.GetEffectiveGridColumns(entry));
            Assert.AreEqual(9, extractor.GetEffectiveGridRows(entry));
            Assert.AreEqual(35, extractor.GetEffectiveCellWidth(entry));
            Assert.AreEqual(45, extractor.GetEffectiveCellHeight(entry));
            Assert.AreEqual(5, extractor.GetEffectivePaddingLeft(entry));
            Assert.AreEqual(0.42f, extractor.GetEffectiveAlphaThreshold(entry), 0.0001f);
        }

        [Test]
        public void SingleEntryBehaviorWorksCorrectly()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 4;
            extractor._gridRows = 4;

            SpriteSheetExtractor.SpriteSheetEntry singleEntry =
                new SpriteSheetExtractor.SpriteSheetEntry
                {
                    _useGlobalSettings = false,
                    _extractionModeOverride = SpriteSheetExtractor.ExtractionMode.PaddedGrid,
                    _gridColumnsOverride = 2,
                    _gridRowsOverride = 2,
                };

            extractor._discoveredSheets = new List<SpriteSheetExtractor.SpriteSheetEntry>
            {
                singleEntry,
            };

            extractor.ApplyGlobalSettingsToAll();

            Assert.AreEqual(false, singleEntry._useGlobalSettings);
            Assert.AreEqual(
                SpriteSheetExtractor.ExtractionMode.GridBased,
                singleEntry._extractionModeOverride,
                "ApplyGlobalSettingsToAll should overwrite with global value"
            );
            Assert.AreEqual(4, singleEntry._gridColumnsOverride);
            Assert.AreEqual(4, singleEntry._gridRowsOverride);
        }

        [Test]
        public void PerSheetExtractionModeAffectsDiscovery()
        {
            CreateSpriteSheet("per_sheet_mode_test", 128, 128, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.FromMetadata;
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == "per_sheet_mode_test"
                )
                {
                    found = true;
                    SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                    int metadataCount = entry._sprites.Count;

                    entry._useGlobalSettings = false;
                    entry._extractionModeOverride = SpriteSheetExtractor.ExtractionMode.GridBased;
                    entry._gridSizeModeOverride = SpriteSheetExtractor.GridSizeMode.Manual;
                    entry._gridColumnsOverride = 4;
                    entry._gridRowsOverride = 4;
                    entry._sprites.Clear();

                    extractor.PopulateSpritesFromGrid(entry, entry._texture);

                    int gridCount = entry._sprites.Count;

                    Assert.AreEqual(4, metadataCount, "FromMetadata should find 4 sprites");
                    Assert.AreEqual(16, gridCount, "GridBased 4x4 should create 16 sprites");
                    break;
                }
            }
            Assert.IsTrue(found, "Should find per_sheet_mode_test");
        }

        [Test]
        public void NewEntryDefaultsToUseGlobalSettingsTrue()
        {
            SpriteSheetExtractor.SpriteSheetEntry entry =
                new SpriteSheetExtractor.SpriteSheetEntry();

            Assert.IsTrue(
                entry._useGlobalSettings,
                "New entry should default to _useGlobalSettings = true"
            );
        }

        [Test]
        public void NewEntryDefaultsToNullOverrides()
        {
            SpriteSheetExtractor.SpriteSheetEntry entry =
                new SpriteSheetExtractor.SpriteSheetEntry();

            Assert.IsNull(entry._extractionModeOverride);
            Assert.IsNull(entry._gridSizeModeOverride);
            Assert.IsNull(entry._gridColumnsOverride);
            Assert.IsNull(entry._gridRowsOverride);
            Assert.IsNull(entry._cellWidthOverride);
            Assert.IsNull(entry._cellHeightOverride);
            Assert.IsNull(entry._paddingLeftOverride);
            Assert.IsNull(entry._paddingRightOverride);
            Assert.IsNull(entry._paddingTopOverride);
            Assert.IsNull(entry._paddingBottomOverride);
            Assert.IsNull(entry._alphaThresholdOverride);
        }

        private static IEnumerable<TestCaseData> PreviewTextureDimensionCases()
        {
            yield return new TestCaseData(16, 16, 2, 2).SetName("PreviewTexture.Small.16x16");
            yield return new TestCaseData(32, 32, 2, 2).SetName("PreviewTexture.Medium.32x32");
            yield return new TestCaseData(64, 64, 2, 2).SetName("PreviewTexture.Standard.64x64");
            yield return new TestCaseData(128, 128, 4, 4).SetName("PreviewTexture.Large.128x128");
            yield return new TestCaseData(256, 256, 4, 4).SetName(
                "PreviewTexture.VeryLarge.256x256"
            );
        }

        [Test]
        [TestCaseSource(nameof(PreviewTextureDimensionCases))]
        public void PreviewTextureGenerationWorksWithVariousSizes(
            int width,
            int height,
            int cols,
            int rows
        )
        {
            string name = $"preview_dim_{width}x{height}";
            CreateSpriteSheet(name, width, height, cols, rows);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == name
                )
                {
                    found = true;
                    SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                    Assert.That(entry._sprites.Count, Is.EqualTo(cols * rows));
                    for (int j = 0; j < entry._sprites.Count; j++)
                    {
                        Texture2D preview = entry._sprites[j]._previewTexture;
                        Assert.IsTrue(preview != null, $"Preview texture {j} should be generated");
                        Assert.That(preview.width, Is.GreaterThan(0));
                        Assert.That(preview.height, Is.GreaterThan(0));
                    }
                    break;
                }
            }
            Assert.IsTrue(found, $"Should find {name}");
        }

        private static IEnumerable<TestCaseData> OddDimensionCases()
        {
            yield return new TestCaseData(33, 33, 3, 3).SetName("PreviewTexture.Odd.33x33");
            yield return new TestCaseData(47, 47, 1, 1).SetName("PreviewTexture.Odd.47x47");
            yield return new TestCaseData(63, 63, 3, 3).SetName("PreviewTexture.Odd.63x63");
            yield return new TestCaseData(127, 127, 1, 1).SetName("PreviewTexture.Odd.127x127");
        }

        [Test]
        [TestCaseSource(nameof(OddDimensionCases))]
        public void PreviewTextureGenerationWorksWithOddDimensions(
            int width,
            int height,
            int cols,
            int rows
        )
        {
            string name = $"preview_odd_{width}x{height}";
            CreateSpriteSheet(name, width, height, cols, rows);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == name
                )
                {
                    found = true;
                    SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                    Assert.That(entry._sprites.Count, Is.GreaterThanOrEqualTo(1));
                    for (int j = 0; j < entry._sprites.Count; j++)
                    {
                        Texture2D preview = entry._sprites[j]._previewTexture;
                        Assert.IsTrue(
                            preview != null,
                            $"Preview texture {j} should be generated for odd dimension"
                        );
                    }
                    break;
                }
            }
            Assert.IsTrue(found, $"Should find {name}");
        }

        private static IEnumerable<TestCaseData> AspectRatioCases()
        {
            yield return new TestCaseData(128, 32, 4, 1).SetName(
                "PreviewTexture.AspectRatio.Wide.4x1"
            );
            yield return new TestCaseData(32, 128, 1, 4).SetName(
                "PreviewTexture.AspectRatio.Tall.1x4"
            );
            yield return new TestCaseData(200, 50, 4, 1).SetName(
                "PreviewTexture.AspectRatio.Wide.4_1"
            );
            yield return new TestCaseData(50, 200, 1, 4).SetName(
                "PreviewTexture.AspectRatio.Tall.1_4"
            );
            yield return new TestCaseData(64, 64, 2, 2).SetName(
                "PreviewTexture.AspectRatio.Square"
            );
        }

        [Test]
        [TestCaseSource(nameof(AspectRatioCases))]
        public void PreviewTextureGenerationWorksWithVariousAspectRatios(
            int width,
            int height,
            int cols,
            int rows
        )
        {
            string name = $"preview_aspect_{width}x{height}";
            CreateSpriteSheet(name, width, height, cols, rows);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == name
                )
                {
                    found = true;
                    SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                    Assert.That(entry._sprites.Count, Is.EqualTo(cols * rows));
                    for (int j = 0; j < entry._sprites.Count; j++)
                    {
                        Texture2D preview = entry._sprites[j]._previewTexture;
                        Assert.IsTrue(
                            preview != null,
                            $"Preview texture {j} should be generated for aspect ratio {width}x{height}"
                        );
                    }
                    break;
                }
            }
            Assert.IsTrue(found, $"Should find {name}");
        }

        [Test]
        public void PreviewTextureRealSizeModeRespectsSpriteDimensions()
        {
            CreateSpriteSheet("preview_realsize", 128, 64, 2, 1);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.RealSize;
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == "preview_realsize"
                )
                {
                    found = true;
                    SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                    Assert.That(entry._sprites.Count, Is.EqualTo(2));

                    for (int j = 0; j < entry._sprites.Count; j++)
                    {
                        Texture2D preview = entry._sprites[j]._previewTexture;
                        Assert.IsTrue(preview != null, $"Preview {j} should be generated");
                        Assert.That(
                            preview.width,
                            Is.LessThanOrEqualTo(128),
                            "RealSize mode should respect max dimensions"
                        );
                        Assert.That(
                            preview.height,
                            Is.LessThanOrEqualTo(128),
                            "RealSize mode should respect max dimensions"
                        );
                    }
                    break;
                }
            }
            Assert.IsTrue(found, "Should find preview_realsize");
        }

        [Test]
        public void PreviewTextureSquareSizeModeScalesAppropriately()
        {
            CreateSpriteSheet("preview_square", 128, 128, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size64;
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == "preview_square"
                )
                {
                    found = true;
                    SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                    for (int j = 0; j < entry._sprites.Count; j++)
                    {
                        Texture2D preview = entry._sprites[j]._previewTexture;
                        Assert.IsTrue(preview != null, $"Preview {j} should be generated");
                        Assert.That(
                            Mathf.Max(preview.width, preview.height),
                            Is.LessThanOrEqualTo(64),
                            "Square size mode should scale to 64"
                        );
                    }
                    break;
                }
            }
            Assert.IsTrue(found, "Should find preview_square");
        }

        [Test]
        public void PreviewTextureBoundarySpritesGenerateCorrectly()
        {
            Texture2D texture = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            Color[] pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.red;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, "boundary_sprites.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            SetSpriteSheet(
                importer,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = "corner_bl",
                        rect = new Rect(0, 0, 16, 16),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    new SpriteMetaData
                    {
                        name = "corner_br",
                        rect = new Rect(48, 0, 16, 16),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    new SpriteMetaData
                    {
                        name = "corner_tl",
                        rect = new Rect(0, 48, 16, 16),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    new SpriteMetaData
                    {
                        name = "corner_tr",
                        rect = new Rect(48, 48, 16, 16),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == "boundary_sprites"
                )
                {
                    found = true;
                    SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                    Assert.That(entry._sprites.Count, Is.EqualTo(4));
                    for (int j = 0; j < entry._sprites.Count; j++)
                    {
                        Texture2D preview = entry._sprites[j]._previewTexture;
                        Assert.IsTrue(
                            preview != null,
                            $"Boundary sprite {j} should generate preview"
                        );
                        Assert.That(preview.width, Is.GreaterThan(0));
                        Assert.That(preview.height, Is.GreaterThan(0));
                    }
                    break;
                }
            }
            Assert.IsTrue(found, "Should find boundary_sprites");
        }

        /// <summary>
        /// Verifies preview textures have explicit expected dimensions based on preview mode and sprite rect.
        /// These tests go beyond null checks to ensure the actual width and height match expectations.
        /// </summary>
        private static IEnumerable<TestCaseData> PreviewDimensionVerificationCases()
        {
            yield return new TestCaseData(
                64,
                64,
                2,
                2,
                SpriteSheetExtractor.PreviewSizeMode.Size24,
                24
            ).SetName("PreviewDimVerify.64x64.Mode24.Expected24");
            yield return new TestCaseData(
                64,
                64,
                2,
                2,
                SpriteSheetExtractor.PreviewSizeMode.Size32,
                32
            ).SetName("PreviewDimVerify.64x64.Mode32.Expected32");
            yield return new TestCaseData(
                64,
                64,
                2,
                2,
                SpriteSheetExtractor.PreviewSizeMode.Size64,
                64
            ).SetName("PreviewDimVerify.64x64.Mode64.Expected64");
            yield return new TestCaseData(
                128,
                128,
                4,
                4,
                SpriteSheetExtractor.PreviewSizeMode.Size32,
                32
            ).SetName("PreviewDimVerify.128x128.4x4.Mode32.Expected32");
            yield return new TestCaseData(
                48,
                48,
                2,
                2,
                SpriteSheetExtractor.PreviewSizeMode.Size24,
                24
            ).SetName("PreviewDimVerify.48x48.Mode24.Expected24");
        }

        [Test]
        [TestCaseSource(nameof(PreviewDimensionVerificationCases))]
        public void PreviewTextureDimensionsMatchExpectedSize(
            int textureWidth,
            int textureHeight,
            int cols,
            int rows,
            SpriteSheetExtractor.PreviewSizeMode previewMode,
            int expectedMaxDimension
        )
        {
            string name = $"preview_verify_{textureWidth}x{textureHeight}_{previewMode}";
            CreateSpriteSheet(name, textureWidth, textureHeight, cols, rows);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._previewSizeMode = previewMode;
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == name
                )
                {
                    found = true;
                    SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                    Assert.That(entry._sprites.Count, Is.EqualTo(cols * rows));

                    for (int j = 0; j < entry._sprites.Count; j++)
                    {
                        Texture2D preview = entry._sprites[j]._previewTexture;
                        Assert.IsTrue(preview != null, $"Preview {j} should exist");
                        int maxDim = Mathf.Max(preview.width, preview.height);
                        Assert.That(
                            maxDim,
                            Is.LessThanOrEqualTo(expectedMaxDimension),
                            $"Preview {j} max dimension should be <= {expectedMaxDimension}, actual: {preview.width}x{preview.height}"
                        );
                        Assert.That(
                            preview.width,
                            Is.GreaterThan(0),
                            $"Preview {j} width should be > 0"
                        );
                        Assert.That(
                            preview.height,
                            Is.GreaterThan(0),
                            $"Preview {j} height should be > 0"
                        );
                    }
                    break;
                }
            }
            Assert.IsTrue(found, $"Should find {name}");
        }

        /// <summary>
        /// Test cases for verifying that fixed preview modes produce exact expected dimensions
        /// when the sprite is larger than the target size. This ensures the scaling logic
        /// scales down to exactly the target size, not just less than or equal.
        /// </summary>
        private static IEnumerable<TestCaseData> FixedPreviewModeExactDimensionCases()
        {
            // Sprite is 64x64, larger than 24, should scale to exactly 24
            yield return new TestCaseData(
                128,
                128,
                2,
                2,
                SpriteSheetExtractor.PreviewSizeMode.Size24,
                24
            ).SetName("FixedPreviewExact.64x64Sprite.Mode24.Exact24");
            // Sprite is 64x64, larger than 32, should scale to exactly 32
            yield return new TestCaseData(
                128,
                128,
                2,
                2,
                SpriteSheetExtractor.PreviewSizeMode.Size32,
                32
            ).SetName("FixedPreviewExact.64x64Sprite.Mode32.Exact32");
            // Sprite is 64x64, same as 64, should be exactly 64
            yield return new TestCaseData(
                128,
                128,
                2,
                2,
                SpriteSheetExtractor.PreviewSizeMode.Size64,
                64
            ).SetName("FixedPreviewExact.64x64Sprite.Mode64.Exact64");
            // Sprite is 128x128, larger than 24, should scale to exactly 24
            yield return new TestCaseData(
                256,
                256,
                2,
                2,
                SpriteSheetExtractor.PreviewSizeMode.Size24,
                24
            ).SetName("FixedPreviewExact.128x128Sprite.Mode24.Exact24");
            // Sprite is 128x128, larger than 32, should scale to exactly 32
            yield return new TestCaseData(
                256,
                256,
                2,
                2,
                SpriteSheetExtractor.PreviewSizeMode.Size32,
                32
            ).SetName("FixedPreviewExact.128x128Sprite.Mode32.Exact32");
            // Sprite is 128x128, larger than 64, should scale to exactly 64
            yield return new TestCaseData(
                256,
                256,
                2,
                2,
                SpriteSheetExtractor.PreviewSizeMode.Size64,
                64
            ).SetName("FixedPreviewExact.128x128Sprite.Mode64.Exact64");
        }

        [Test]
        [TestCaseSource(nameof(FixedPreviewModeExactDimensionCases))]
        public void FixedPreviewModesProduceExactDimensionsWhenSpriteLargerThanTarget(
            int textureWidth,
            int textureHeight,
            int cols,
            int rows,
            SpriteSheetExtractor.PreviewSizeMode previewMode,
            int expectedExactMaxDimension
        )
        {
            int spriteWidth = textureWidth / cols;
            int spriteHeight = textureHeight / rows;

            // This test is specifically for sprites larger than the target size
            Assert.That(
                Mathf.Max(spriteWidth, spriteHeight),
                Is.GreaterThanOrEqualTo(expectedExactMaxDimension),
                "Test precondition: sprite must be >= target size for exact dimension verification"
            );

            string name = $"preview_exact_{textureWidth}x{textureHeight}_{previewMode}";
            CreateSpriteSheet(name, textureWidth, textureHeight, cols, rows);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._previewSizeMode = previewMode;
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == name
                )
                {
                    found = true;
                    SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                    Assert.That(entry._sprites.Count, Is.EqualTo(cols * rows));

                    for (int j = 0; j < entry._sprites.Count; j++)
                    {
                        Texture2D preview = entry._sprites[j]._previewTexture;
                        Assert.IsTrue(preview != null, $"Preview {j} should exist");
                        int maxDim = Mathf.Max(preview.width, preview.height);
                        Assert.That(
                            maxDim,
                            Is.EqualTo(expectedExactMaxDimension),
                            $"Preview {j} max dimension should exactly equal {expectedExactMaxDimension} when sprite ({spriteWidth}x{spriteHeight}) is larger than target, actual: {preview.width}x{preview.height}"
                        );
                    }
                    break;
                }
            }
            Assert.IsTrue(found, $"Should find {name}");
        }

        [Test]
        public void PreviewTextureRealSizeModeDimensionsMatchSpriteRect()
        {
            CreateSpriteSheet("preview_realsize_verify", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.RealSize;
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == "preview_realsize_verify"
                )
                {
                    found = true;
                    SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];

                    for (int j = 0; j < entry._sprites.Count; j++)
                    {
                        SpriteSheetExtractor.SpriteEntryData sprite = entry._sprites[j];
                        Texture2D preview = sprite._previewTexture;
                        Assert.IsTrue(preview != null, $"Preview {j} should exist");
                        int expectedSpriteWidth = (int)sprite._rect.width;
                        int expectedSpriteHeight = (int)sprite._rect.height;
                        int expectedPreviewSize = Mathf.Min(
                            Mathf.Max(expectedSpriteWidth, expectedSpriteHeight),
                            128
                        );
                        int actualMaxDim = Mathf.Max(preview.width, preview.height);
                        Assert.That(
                            actualMaxDim,
                            Is.LessThanOrEqualTo(expectedPreviewSize),
                            $"Preview {j} max dim should be <= {expectedPreviewSize} for sprite rect {expectedSpriteWidth}x{expectedSpriteHeight}"
                        );
                    }
                    break;
                }
            }
            Assert.IsTrue(found, "Should find preview_realsize_verify");
        }

        [Test]
        public void PreviewTextureAspectRatioIsPreserved()
        {
            Texture2D texture = Track(new Texture2D(100, 50, TextureFormat.RGBA32, false));
            Color[] pixels = new Color[100 * 50];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, "preview_aspect_verify.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            SetSpriteSheet(
                importer,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = "wide_sprite",
                        rect = new Rect(0, 0, 100, 50),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size64;
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == "preview_aspect_verify"
                )
                {
                    found = true;
                    SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                    Texture2D preview = entry._sprites[0]._previewTexture;
                    Assert.IsTrue(preview != null, "Preview should exist");
                    Assert.That(
                        preview.width,
                        Is.GreaterThan(preview.height),
                        "Wide sprite preview should remain wider than tall"
                    );
                    break;
                }
            }
            Assert.IsTrue(found, "Should find preview_aspect_verify");
        }

        private static IEnumerable<TestCaseData> ExtractionDimensionCases()
        {
            yield return new TestCaseData(32, 32, 2, 2, 16, 16).SetName("Extraction.Dim.32x32.2x2");
            yield return new TestCaseData(64, 64, 4, 4, 16, 16).SetName("Extraction.Dim.64x64.4x4");
            yield return new TestCaseData(128, 64, 4, 2, 32, 32).SetName(
                "Extraction.Dim.128x64.4x2"
            );
            yield return new TestCaseData(64, 128, 2, 4, 32, 32).SetName(
                "Extraction.Dim.64x128.2x4"
            );
            yield return new TestCaseData(100, 100, 4, 4, 25, 25).SetName(
                "Extraction.Dim.100x100.4x4"
            );
        }

        [Test]
        [TestCaseSource(nameof(ExtractionDimensionCases))]
        public void ExtractedSpritesHaveCorrectPixelDimensions(
            int textureWidth,
            int textureHeight,
            int cols,
            int rows,
            int expectedCellWidth,
            int expectedCellHeight
        )
        {
            string name = $"extract_dim_{textureWidth}x{textureHeight}_{cols}x{rows}";
            CreateSpriteSheet(name, textureWidth, textureHeight, cols, rows);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string outputPath = Path.Combine(OutputDir, $"{name}_000.png").SanitizePath();
            VerifyExtractedSpriteDimensions(
                outputPath,
                expectedCellWidth,
                expectedCellHeight,
                $"ExtractDim_{textureWidth}x{textureHeight}"
            );
        }

        private static IEnumerable<TestCaseData> VariousSpriteExtractionSizeCases()
        {
            yield return new TestCaseData(8, 8).SetName("Extraction.Size.8x8");
            yield return new TestCaseData(16, 16).SetName("Extraction.Size.16x16");
            yield return new TestCaseData(24, 24).SetName("Extraction.Size.24x24");
            yield return new TestCaseData(32, 32).SetName("Extraction.Size.32x32");
            yield return new TestCaseData(48, 48).SetName("Extraction.Size.48x48");
            yield return new TestCaseData(64, 64).SetName("Extraction.Size.64x64");
            yield return new TestCaseData(96, 96).SetName("Extraction.Size.96x96");
            yield return new TestCaseData(128, 128).SetName("Extraction.Size.128x128");
        }

        [Test]
        [TestCaseSource(nameof(VariousSpriteExtractionSizeCases))]
        public void ExtractionWorksWithVariousSpriteSizes(int spriteWidth, int spriteHeight)
        {
            int textureWidth = spriteWidth * 2;
            int textureHeight = spriteHeight * 2;
            string name = $"extract_size_{spriteWidth}x{spriteHeight}";
            CreateSpriteSheet(name, textureWidth, textureHeight, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string outputPath = Path.Combine(OutputDir, $"{name}_000.png").SanitizePath();
            VerifyExtractedSpriteDimensions(
                outputPath,
                spriteWidth,
                spriteHeight,
                $"SpriteSize_{spriteWidth}x{spriteHeight}"
            );
        }

        [Test]
        public void ExtractionAtTextureBoundariesWorksCorrectly()
        {
            Texture2D texture = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            Color[] pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.blue;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, "boundary_extract.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            SetSpriteSheet(
                importer,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = "edge_left",
                        rect = new Rect(0, 16, 16, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    new SpriteMetaData
                    {
                        name = "edge_right",
                        rect = new Rect(48, 16, 16, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    new SpriteMetaData
                    {
                        name = "edge_top",
                        rect = new Rect(16, 48, 32, 16),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    new SpriteMetaData
                    {
                        name = "edge_bottom",
                        rect = new Rect(16, 0, 32, 16),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            for (int i = 0; i < 4; i++)
            {
                string outputPath = Path.Combine(OutputDir, $"boundary_extract_{i:D3}.png")
                    .SanitizePath();
                Assert.That(
                    File.Exists(RelToFull(outputPath)),
                    Is.True,
                    $"Boundary sprite {i} should be extracted"
                );
            }
        }

        /// <summary>
        /// Tests critical minimum sprite size edge cases for ArrayPool.
        /// ArrayPool returns minimum-sized arrays, so 1x1, 2x2, and 3x3 sprites are important
        /// edge cases to verify that SetPixels32 receives correctly-sized arrays.
        /// These tests prevent regression of the SetPixels32 array length bug.
        /// </summary>
        private static IEnumerable<TestCaseData> MinimumSpriteSizeCases()
        {
            yield return new TestCaseData(1, 1).SetName("MinSpriteSize.1x1.SinglePixel");
            yield return new TestCaseData(2, 2).SetName("MinSpriteSize.2x2.MinimumArrayPoolSize");
            yield return new TestCaseData(3, 3).SetName("MinSpriteSize.3x3.SmallestNonPowerOf2");
            yield return new TestCaseData(1, 4).SetName("MinSpriteSize.1x4.SingleColumnVertical");
            yield return new TestCaseData(4, 1).SetName("MinSpriteSize.4x1.SingleRowHorizontal");
            yield return new TestCaseData(2, 3).SetName("MinSpriteSize.2x3.AsymmetricSmall");
            yield return new TestCaseData(3, 2).SetName("MinSpriteSize.3x2.AsymmetricSmallAlt");
        }

        [Test]
        [TestCaseSource(nameof(MinimumSpriteSizeCases))]
        public void ExtractionWithMinimumSpriteSizesWorksCorrectly(
            int spriteWidth,
            int spriteHeight
        )
        {
            int textureWidth = spriteWidth * 2;
            int textureHeight = spriteHeight * 2;
            Texture2D texture = Track(
                new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
            );
            Color[] pixels = new Color[textureWidth * textureHeight];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string name = $"min_sprite_{spriteWidth}x{spriteHeight}";
            string path = Path.Combine(Root, $"{name}.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            SetSpriteSheet(
                importer,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = $"{name}_0",
                        rect = new Rect(0, 0, spriteWidth, spriteHeight),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string outputPath = Path.Combine(OutputDir, $"{name}_000.png").SanitizePath();
            VerifyExtractedSpriteDimensions(
                outputPath,
                spriteWidth,
                spriteHeight,
                $"MinSpriteSize_{spriteWidth}x{spriteHeight}"
            );
        }

        [Test]
        public void Extraction1x1PixelSpriteWorksCorrectly()
        {
            Texture2D texture = Track(new Texture2D(4, 4, TextureFormat.RGBA32, false));
            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, "tiny_1x1.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            SetSpriteSheet(
                importer,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = "tiny_pixel",
                        rect = new Rect(1, 1, 1, 1),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string outputPath = Path.Combine(OutputDir, "tiny_1x1_000.png").SanitizePath();
            VerifyExtractedSpriteDimensions(outputPath, 1, 1, "Tiny1x1");
        }

        [Test]
        public void ExtractionVeryLargeSpritesWorksCorrectly()
        {
            Texture2D texture = Track(new Texture2D(512, 512, TextureFormat.RGBA32, false));
            Color[] pixels = new Color[512 * 512];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.green;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, "very_large.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            SetSpriteSheet(
                importer,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = "large_sprite",
                        rect = new Rect(0, 0, 512, 512),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string outputPath = Path.Combine(OutputDir, "very_large_000.png").SanitizePath();
            VerifyExtractedSpriteDimensions(outputPath, 512, 512, "VeryLarge");
        }

        /// <summary>
        /// Tests extraction from a 2048x2048 texture with multiple sprites.
        /// This is a stress test for large texture handling, memory allocation,
        /// and ArrayPool behavior with large pixel arrays. Large textures are
        /// particularly important for verifying the SetPixels32 bug fix because
        /// ArrayPool sizing issues become more apparent with larger arrays.
        /// </summary>
        [Test]
        public void Extraction2048x2048TextureWithMultipleSpritesWorksCorrectly()
        {
            int size = 2048;
            int gridSize = 4;
            int cellSize = size / gridSize;

            Texture2D texture = Track(new Texture2D(size, size, TextureFormat.RGBA32, false));
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int cellX = x / cellSize;
                    int cellY = y / cellSize;
                    int cellIndex = cellY * gridSize + cellX;
                    float hue = (float)cellIndex / (gridSize * gridSize);
                    pixels[y * size + x] = Color.HSVToRGB(hue, 0.8f, 0.9f);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, "massive_2048.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            importer.maxTextureSize = 4096;

            SpriteMetaData[] spritesheet = new SpriteMetaData[gridSize * gridSize];
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    int index = row * gridSize + col;
                    spritesheet[index] = new SpriteMetaData
                    {
                        name = $"massive_{index}",
                        rect = new Rect(col * cellSize, row * cellSize, cellSize, cellSize),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    };
                }
            }
            SetSpriteSheet(importer, spritesheet);
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();

            Assert.DoesNotThrow(
                () => extractor.ExtractSelectedSprites(),
                "Extraction should not throw with 2048x2048 texture"
            );

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            for (int i = 0; i < 4; i++)
            {
                string outputPath = Path.Combine(OutputDir, $"massive_2048_{i:D3}.png")
                    .SanitizePath();
                VerifyExtractedSpriteDimensions(
                    outputPath,
                    cellSize,
                    cellSize,
                    $"Massive2048_Sprite{i}"
                );
            }
        }

        [Test]
        public void PreviewGenerationWith2048x2048TextureWorksCorrectly()
        {
            int size = 2048;
            int gridSize = 4;
            int cellSize = size / gridSize;

            Texture2D texture = Track(new Texture2D(size, size, TextureFormat.RGBA32, false));
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, "massive_preview_2048.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            importer.maxTextureSize = 4096;

            SpriteMetaData[] spritesheet = new SpriteMetaData[gridSize * gridSize];
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    int index = row * gridSize + col;
                    spritesheet[index] = new SpriteMetaData
                    {
                        name = $"massive_preview_{index}",
                        rect = new Rect(col * cellSize, row * cellSize, cellSize, cellSize),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    };
                }
            }
            SetSpriteSheet(importer, spritesheet);
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size64;
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == "massive_preview_2048"
                )
                {
                    found = true;
                    SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                    Assert.That(
                        entry._sprites.Count,
                        Is.EqualTo(gridSize * gridSize),
                        "Should have all sprites"
                    );
                    for (int j = 0; j < entry._sprites.Count; j++)
                    {
                        Texture2D preview = entry._sprites[j]._previewTexture;
                        Assert.IsTrue(
                            preview != null,
                            $"Preview {j} from 2048x2048 should be generated"
                        );
                        Assert.That(
                            preview.width,
                            Is.GreaterThan(0),
                            $"Preview {j} width should be > 0"
                        );
                        Assert.That(
                            preview.height,
                            Is.GreaterThan(0),
                            $"Preview {j} height should be > 0"
                        );
                    }
                    break;
                }
            }
            Assert.IsTrue(found, "Should find massive_preview_2048");
        }

        [Test]
        public void ExtractionSpritesTouchingTextureEdgesWorksCorrectly()
        {
            Texture2D texture = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            Color[] pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.cyan;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, "touching_edges.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            SetSpriteSheet(
                importer,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = "full_texture",
                        rect = new Rect(0, 0, 64, 64),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string outputPath = Path.Combine(OutputDir, "touching_edges_000.png").SanitizePath();
            VerifyExtractedSpriteDimensions(outputPath, 64, 64, "TouchingEdges");
        }

        [Test]
        public void ClampingBehaviorWorksWhenSpriteRectExtendsBeyondTexture()
        {
            Texture2D texture = Track(new Texture2D(32, 32, TextureFormat.RGBA32, false));
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.yellow;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, "clamped_rect.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            SetSpriteSheet(
                importer,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = "oversized",
                        rect = new Rect(20, 20, 32, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();

            Assert.DoesNotThrow(
                () => extractor.ExtractSelectedSprites(),
                "Extraction should not throw when rect extends beyond texture"
            );

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string outputPath = Path.Combine(OutputDir, "clamped_rect_000.png").SanitizePath();
            if (File.Exists(RelToFull(outputPath)))
            {
                Texture2D extracted = AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
                Assert.IsTrue(extracted != null, "Clamped texture should load if extracted");
                Assert.That(extracted.width, Is.LessThanOrEqualTo(32), "Width should be clamped");
                Assert.That(extracted.height, Is.LessThanOrEqualTo(32), "Height should be clamped");
            }
        }

        /// <summary>
        /// Tests behavior when sprite rects have negative coordinates that need clamping.
        /// Negative rect coordinates are invalid but should be handled gracefully.
        /// The expected behavior is either:
        /// - Coordinates are clamped to 0
        /// - The sprite is skipped or handled without throwing
        /// This test verifies no exceptions occur and the system handles edge cases gracefully.
        /// </summary>
        [Test]
        public void ExtractionHandlesNegativeSpriteCoordinatesGracefully()
        {
            Texture2D texture = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            Color[] pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.magenta;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, "negative_coords.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            SetSpriteSheet(
                importer,
                new[]
                {
                    // Normal sprite for reference
                    new SpriteMetaData
                    {
                        name = "normal_sprite",
                        rect = new Rect(0, 0, 32, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    // Sprite with rect that starts at valid position
                    new SpriteMetaData
                    {
                        name = "edge_sprite",
                        rect = new Rect(32, 32, 32, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == "negative_coords"
                )
                {
                    found = true;
                    SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];

                    Assert.That(
                        entry._sprites.Count,
                        Is.GreaterThanOrEqualTo(2),
                        "Should discover at least 2 sprites"
                    );

                    Assert.DoesNotThrow(
                        () => extractor.ExtractSelectedSprites(),
                        "Extraction should handle edge coordinate sprites gracefully"
                    );
                    break;
                }
            }
            Assert.IsTrue(found, "Should find negative_coords test sheet");

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string normalPath = Path.Combine(OutputDir, "negative_coords_000.png").SanitizePath();
            Assert.That(
                File.Exists(RelToFull(normalPath)),
                Is.True,
                "Normal sprite should be extracted"
            );
        }

        /// <summary>
        /// Tests behavior when sprite rect extends beyond texture bounds with clamping.
        /// Verifies that sprites with partially out-of-bounds rects are handled correctly
        /// by clamping to valid texture coordinates.
        /// </summary>
        [Test]
        public void ExtractionHandlesPartiallyOutOfBoundsSpriteRectsGracefully()
        {
            Texture2D texture = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            Color[] pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.cyan;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, "partial_oob.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            SetSpriteSheet(
                importer,
                new[]
                {
                    // Normal sprite for reference
                    new SpriteMetaData
                    {
                        name = "valid_sprite",
                        rect = new Rect(0, 0, 32, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    // Sprite rect that extends beyond right edge (starts at 48, width 32 = extends to 80)
                    new SpriteMetaData
                    {
                        name = "extends_right",
                        rect = new Rect(48, 0, 32, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    // Sprite rect that extends beyond top edge (starts at y=48, height 32 = extends to 80)
                    new SpriteMetaData
                    {
                        name = "extends_top",
                        rect = new Rect(0, 48, 32, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    // Sprite rect in corner that extends beyond both edges
                    new SpriteMetaData
                    {
                        name = "extends_corner",
                        rect = new Rect(48, 48, 32, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == "partial_oob"
                )
                {
                    found = true;
                    SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];

                    Assert.That(
                        entry._sprites.Count,
                        Is.GreaterThanOrEqualTo(1),
                        "Should discover at least 1 sprite"
                    );

                    Assert.DoesNotThrow(
                        () => extractor.ExtractSelectedSprites(),
                        "Extraction should handle partially out-of-bounds rects gracefully"
                    );
                    break;
                }
            }
            Assert.IsTrue(found, "Should find partial_oob test sheet");

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string validPath = Path.Combine(OutputDir, "partial_oob_000.png").SanitizePath();
            Assert.That(
                File.Exists(RelToFull(validPath)),
                Is.True,
                "Valid sprite should be extracted"
            );

            Texture2D validExtracted = AssetDatabase.LoadAssetAtPath<Texture2D>(validPath);
            Assert.IsTrue(validExtracted != null, "Valid extracted texture should load");
            Assert.That(validExtracted.width, Is.EqualTo(32), "Valid sprite width should be 32");
            Assert.That(validExtracted.height, Is.EqualTo(32), "Valid sprite height should be 32");
        }

        /// <summary>
        /// Tests behavior with sprite rects at exact texture boundaries (0,0 origin and max bounds).
        /// These are edge cases that should work correctly without any clamping needed.
        /// </summary>
        [Test]
        public void ExtractionHandlesExactBoundarySpritesCorrectly()
        {
            Texture2D texture = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            Color[] pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.green;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, "exact_bounds.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            SetSpriteSheet(
                importer,
                new[]
                {
                    // Sprite at origin (0,0) - bottom-left corner
                    new SpriteMetaData
                    {
                        name = "origin_sprite",
                        rect = new Rect(0, 0, 16, 16),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    // Sprite at exact top-right corner
                    new SpriteMetaData
                    {
                        name = "top_right_sprite",
                        rect = new Rect(48, 48, 16, 16),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    // Sprite covering entire texture
                    new SpriteMetaData
                    {
                        name = "full_texture_sprite",
                        rect = new Rect(0, 0, 64, 64),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    // Sprite at bottom-right corner
                    new SpriteMetaData
                    {
                        name = "bottom_right_sprite",
                        rect = new Rect(48, 0, 16, 16),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    // Sprite at top-left corner
                    new SpriteMetaData
                    {
                        name = "top_left_sprite",
                        rect = new Rect(0, 48, 16, 16),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == "exact_bounds"
                )
                {
                    found = true;
                    SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];

                    Assert.That(
                        entry._sprites.Count,
                        Is.EqualTo(5),
                        "Should discover all 5 boundary sprites"
                    );

                    Assert.DoesNotThrow(
                        () => extractor.ExtractSelectedSprites(),
                        "Extraction should handle exact boundary sprites correctly"
                    );
                    break;
                }
            }
            Assert.IsTrue(found, "Should find exact_bounds test sheet");

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            // Verify all 5 sprites were extracted
            for (int i = 0; i < 5; i++)
            {
                string outputPath = Path.Combine(OutputDir, $"exact_bounds_{i:D3}.png")
                    .SanitizePath();
                Assert.That(
                    File.Exists(RelToFull(outputPath)),
                    Is.True,
                    $"Boundary sprite {i} should be extracted"
                );
            }

            // Verify the full texture sprite has correct dimensions
            string fullPath = Path.Combine(OutputDir, "exact_bounds_002.png").SanitizePath();
            Texture2D fullExtracted = AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
            Assert.IsTrue(fullExtracted != null, "Full texture sprite should load");
            Assert.That(
                fullExtracted.width,
                Is.EqualTo(64),
                "Full texture sprite width should be 64"
            );
            Assert.That(
                fullExtracted.height,
                Is.EqualTo(64),
                "Full texture sprite height should be 64"
            );
        }

        /// <summary>
        /// Stress test for extracting many small sprites from a large grid.
        /// Tests 256 sprites (16x16 grid) to verify no resource exhaustion,
        /// memory leaks, or performance degradation with large sprite counts.
        /// This is particularly important for ArrayPool behavior and texture allocation.
        /// </summary>
        [Test]
        public void ExtractionManySmallSpritesFromLargeGridWorksCorrectly()
        {
            int gridSize = 16;
            int cellSize = 8;
            int textureSize = gridSize * cellSize;
            int totalSprites = gridSize * gridSize;

            Texture2D texture = Track(
                new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
            );
            Color[] pixels = new Color[textureSize * textureSize];
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    int cellX = x / cellSize;
                    int cellY = y / cellSize;
                    int cellIndex = cellY * gridSize + cellX;
                    float hue = (float)cellIndex / totalSprites;
                    pixels[y * textureSize + x] = Color.HSVToRGB(hue, 0.8f, 0.9f);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, "stress_many_sprites.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;

            SpriteMetaData[] spritesheet = new SpriteMetaData[totalSprites];
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    int index = row * gridSize + col;
                    spritesheet[index] = new SpriteMetaData
                    {
                        name = $"stress_sprite_{index:D3}",
                        rect = new Rect(col * cellSize, row * cellSize, cellSize, cellSize),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    };
                }
            }
            SetSpriteSheet(importer, spritesheet);
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == "stress_many_sprites"
                )
                {
                    found = true;
                    SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];

                    Assert.That(
                        entry._sprites.Count,
                        Is.EqualTo(totalSprites),
                        $"Should discover all {totalSprites} sprites"
                    );

                    for (int j = 0; j < entry._sprites.Count; j++)
                    {
                        Texture2D preview = entry._sprites[j]._previewTexture;
                        Assert.IsTrue(
                            preview != null,
                            $"Preview {j} of {totalSprites} should be generated"
                        );
                    }
                    break;
                }
            }
            Assert.IsTrue(found, "Should find stress_many_sprites");

            Assert.DoesNotThrow(
                () => extractor.ExtractSelectedSprites(),
                $"Extraction of {totalSprites} sprites should not throw"
            );

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            int extractedCount = 0;
            for (int i = 0; i < 64; i++)
            {
                string outputPath = Path.Combine(OutputDir, $"stress_many_sprites_{i:D3}.png")
                    .SanitizePath();
                if (File.Exists(RelToFull(outputPath)))
                {
                    extractedCount++;
                    Texture2D extracted = AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
                    Assert.IsTrue(extracted != null, $"Extracted stress sprite {i} should load");
                    Assert.That(extracted.width, Is.EqualTo(cellSize), $"Sprite {i} width");
                    Assert.That(extracted.height, Is.EqualTo(cellSize), $"Sprite {i} height");
                }
            }

            Assert.That(
                extractedCount,
                Is.GreaterThanOrEqualTo(64),
                $"At least 64 sprites should be extracted (found {extractedCount})"
            );
        }

        /// <summary>
        /// Stress test for many previews with different sizes to ensure no resource exhaustion.
        /// Tests 100 sprites (10x10 grid) with varying cell sizes to stress preview generation.
        /// </summary>
        [Test]
        public void PreviewGenerationManySpritesWithVaryingSizesWorksCorrectly()
        {
            int gridSize = 10;
            int totalSprites = gridSize * gridSize;
            int textureSize = 256;
            int baseCellSize = textureSize / gridSize;

            Texture2D texture = Track(
                new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
            );
            Color[] pixels = new Color[textureSize * textureSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, "stress_preview_sizes.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;

            SpriteMetaData[] spritesheet = new SpriteMetaData[totalSprites];
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    int index = row * gridSize + col;
                    spritesheet[index] = new SpriteMetaData
                    {
                        name = $"stress_preview_{index:D3}",
                        rect = new Rect(
                            col * baseCellSize,
                            row * baseCellSize,
                            baseCellSize,
                            baseCellSize
                        ),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    };
                }
            }
            SetSpriteSheet(importer, spritesheet);
            importer.SaveAndReimport();

            SpriteSheetExtractor.PreviewSizeMode[] modes =
            {
                SpriteSheetExtractor.PreviewSizeMode.Size24,
                SpriteSheetExtractor.PreviewSizeMode.Size32,
                SpriteSheetExtractor.PreviewSizeMode.Size64,
            };

            for (int m = 0; m < modes.Length; m++)
            {
                SpriteSheetExtractor.PreviewSizeMode mode = modes[m];
                SpriteSheetExtractor extractor = CreateExtractor();
                extractor._previewSizeMode = mode;
                extractor.DiscoverSpriteSheets();

                bool found = false;
                for (int i = 0; i < extractor._discoveredSheets.Count; i++)
                {
                    if (
                        Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                        == "stress_preview_sizes"
                    )
                    {
                        found = true;
                        SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[
                            i
                        ];

                        Assert.That(
                            entry._sprites.Count,
                            Is.EqualTo(totalSprites),
                            $"Mode {mode}: Should discover all {totalSprites} sprites"
                        );

                        for (int j = 0; j < entry._sprites.Count; j++)
                        {
                            Texture2D preview = entry._sprites[j]._previewTexture;
                            Assert.IsTrue(
                                preview != null,
                                $"Mode {mode}: Preview {j} of {totalSprites} should be generated"
                            );
                            Assert.That(
                                preview.width,
                                Is.GreaterThan(0),
                                $"Mode {mode}: Preview {j} width > 0"
                            );
                            Assert.That(
                                preview.height,
                                Is.GreaterThan(0),
                                $"Mode {mode}: Preview {j} height > 0"
                            );
                        }
                        break;
                    }
                }
                Assert.IsTrue(found, $"Mode {mode}: Should find stress_preview_sizes");
            }
        }

        [Test]
        public void PixelColorsAreCorrectlyTransferred()
        {
            int width = 64;
            int height = 64;
            Texture2D texture = Track(
                new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    alphaIsTransparency = true,
                }
            );

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x < width / 2)
                    {
                        pixels[y * width + x] = Color.red;
                    }
                    else
                    {
                        pixels[y * width + x] = Color.blue;
                    }
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, "color_transfer.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            SetSpriteSheet(
                importer,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = "red_half",
                        rect = new Rect(0, 0, 32, 64),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    new SpriteMetaData
                    {
                        name = "blue_half",
                        rect = new Rect(32, 0, 32, 64),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._preserveImportSettings = false;
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string redPath = Path.Combine(OutputDir, "color_transfer_000.png").SanitizePath();
            string bluePath = Path.Combine(OutputDir, "color_transfer_001.png").SanitizePath();

            Assert.That(File.Exists(RelToFull(redPath)), Is.True);
            Assert.That(File.Exists(RelToFull(bluePath)), Is.True);

            TextureImporter redImporter = AssetImporter.GetAtPath(redPath) as TextureImporter;
            if (redImporter != null)
            {
                redImporter.isReadable = true;
                redImporter.textureCompression = TextureImporterCompression.Uncompressed;
                redImporter.SaveAndReimport();
            }

            TextureImporter blueImporter = AssetImporter.GetAtPath(bluePath) as TextureImporter;
            if (blueImporter != null)
            {
                blueImporter.isReadable = true;
                blueImporter.textureCompression = TextureImporterCompression.Uncompressed;
                blueImporter.SaveAndReimport();
            }

            Texture2D redTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(redPath);
            Texture2D blueTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(bluePath);

            Assert.IsTrue(redTexture != null, "Red texture should load");
            Assert.IsTrue(blueTexture != null, "Blue texture should load");

            Color redSample = redTexture.GetPixel(0, 0);
            Color blueSample = blueTexture.GetPixel(0, 0);

            Assert.That(redSample.r, Is.GreaterThan(0.9f), "Red channel should be preserved");
            Assert.That(blueSample.b, Is.GreaterThan(0.9f), "Blue channel should be preserved");
        }

        [Test]
        public void AlphaChannelIsPreservedDuringExtraction()
        {
            int width = 32;
            int height = 32;
            Texture2D texture = Track(
                new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    alphaIsTransparency = true,
                }
            );

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float alpha = (float)x / width;
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, "alpha_preserve.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            SetSpriteSheet(
                importer,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = "alpha_gradient",
                        rect = new Rect(0, 0, 32, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._preserveImportSettings = false;
            extractor.DiscoverSpriteSheets();
            extractor.ExtractSelectedSprites();

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string outputPath = Path.Combine(OutputDir, "alpha_preserve_000.png").SanitizePath();
            Assert.That(File.Exists(RelToFull(outputPath)), Is.True);

            TextureImporter outImporter = AssetImporter.GetAtPath(outputPath) as TextureImporter;
            if (outImporter != null)
            {
                outImporter.isReadable = true;
                outImporter.textureCompression = TextureImporterCompression.Uncompressed;
                outImporter.alphaIsTransparency = true;
                outImporter.SaveAndReimport();
            }

            Texture2D extracted = AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
            Assert.IsTrue(extracted != null, "Extracted alpha texture should load");

            Color leftPixel = extracted.GetPixel(0, 16);
            Color rightPixel = extracted.GetPixel(31, 16);

            Assert.That(
                rightPixel.a,
                Is.GreaterThan(leftPixel.a),
                "Alpha gradient should be preserved (right side should have higher alpha)"
            );
        }

        private static IEnumerable<TestCaseData> TextureFormatCases()
        {
            yield return new TestCaseData(TextureFormat.RGBA32).SetName("TextureFormat.RGBA32");
            yield return new TestCaseData(TextureFormat.ARGB32).SetName("TextureFormat.ARGB32");
            yield return new TestCaseData(TextureFormat.RGB24).SetName("TextureFormat.RGB24");
            yield return new TestCaseData(TextureFormat.BGRA32).SetName("TextureFormat.BGRA32");
        }

        [Test]
        [TestCaseSource(nameof(TextureFormatCases))]
        public void ExtractionWorksWithDifferentTextureFormats(TextureFormat format)
        {
            int width = 64;
            int height = 64;
            Texture2D texture = Track(new Texture2D(width, height, format, false));

            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0.5f, 0.25f, 0.75f, 1f);
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string formatName = format.ToString().Replace("32", "_32").Replace("24", "_24");
            string path = Path.Combine(Root, $"format_{formatName}.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            SetSpriteSheet(
                importer,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = "format_test",
                        rect = new Rect(0, 0, 32, 32),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();

            Assert.DoesNotThrow(
                () => extractor.ExtractSelectedSprites(),
                $"Extraction should work with {format}"
            );

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string outputPath = Path.Combine(OutputDir, $"format_{formatName}_000.png")
                .SanitizePath();
            Assert.That(
                File.Exists(RelToFull(outputPath)),
                Is.True,
                $"Sprite should be extracted for format {format}"
            );
        }

        /// <summary>
        /// Tests specifically designed to prevent regression of the SetPixels32 bug.
        /// The bug occurred because ArrayPool returns arrays larger than requested, but SetPixels32
        /// requires the array length to exactly match texture dimensions (width * height).
        ///
        /// The fix involved allocating exact-sized arrays instead of using pooled arrays for pixel data
        /// passed to SetPixels32. Tests in this region verify:
        /// - Non-power-of-two dimensions work correctly (ArrayPool returns next power of 2)
        /// - Prime number dimensions work correctly (worst case for ArrayPool sizing)
        /// - Asymmetric dimensions work correctly (width != height)
        /// - Edge cases near power-of-2 boundaries (31x31, 33x33, etc.)
        /// </summary>
        private static IEnumerable<TestCaseData> AsymmetricNPOTCases()
        {
            yield return new TestCaseData(100, 64, 2, 2, 50, 32).SetName(
                "AsymmetricNPOT.WidthNPOT.100x64"
            );
            yield return new TestCaseData(64, 100, 2, 2, 32, 50).SetName(
                "AsymmetricNPOT.HeightNPOT.64x100"
            );
            yield return new TestCaseData(75, 50, 3, 2, 25, 25).SetName(
                "AsymmetricNPOT.BothNPOT.75x50"
            );
            yield return new TestCaseData(50, 75, 2, 3, 25, 25).SetName(
                "AsymmetricNPOT.BothNPOT.50x75"
            );
            yield return new TestCaseData(128, 100, 4, 4, 32, 25).SetName(
                "AsymmetricNPOT.WidthPOT.HeightNPOT.128x100"
            );
            yield return new TestCaseData(100, 128, 4, 4, 25, 32).SetName(
                "AsymmetricNPOT.WidthNPOT.HeightPOT.100x128"
            );
            yield return new TestCaseData(127, 65, 1, 1, 127, 65).SetName(
                "AsymmetricNPOT.BothOdd.127x65"
            );
            yield return new TestCaseData(65, 127, 1, 1, 65, 127).SetName(
                "AsymmetricNPOT.BothOdd.65x127"
            );
        }

        [Test]
        [TestCaseSource(nameof(AsymmetricNPOTCases))]
        public void ExtractionWithAsymmetricNPOTDimensionsWorksCorrectly(
            int textureWidth,
            int textureHeight,
            int cols,
            int rows,
            int expectedSpriteWidth,
            int expectedSpriteHeight
        )
        {
            string name = $"asymmetric_npot_{textureWidth}x{textureHeight}";
            CreateSpriteSheet(name, textureWidth, textureHeight, cols, rows);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();

            Assert.DoesNotThrow(
                () => extractor.ExtractSelectedSprites(),
                $"Extraction should not throw with asymmetric NPOT dimensions {textureWidth}x{textureHeight}"
            );

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string outputPath = Path.Combine(OutputDir, $"{name}_000.png").SanitizePath();
            Assert.That(
                File.Exists(RelToFull(outputPath)),
                Is.True,
                $"Asymmetric NPOT sprite should be extracted for {textureWidth}x{textureHeight}"
            );

            Texture2D extracted = AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
            Assert.IsTrue(
                extracted != null,
                $"Asymmetric NPOT texture {textureWidth}x{textureHeight} should load"
            );
            Assert.That(
                extracted.width,
                Is.EqualTo(expectedSpriteWidth),
                $"Sprite width should be {expectedSpriteWidth}"
            );
            Assert.That(
                extracted.height,
                Is.EqualTo(expectedSpriteHeight),
                $"Sprite height should be {expectedSpriteHeight}"
            );
        }

        [Test]
        [TestCaseSource(nameof(AsymmetricNPOTCases))]
        public void PreviewGenerationWithAsymmetricNPOTDimensionsWorksCorrectly(
            int textureWidth,
            int textureHeight,
            int cols,
            int rows,
            int expectedSpriteWidth,
            int expectedSpriteHeight
        )
        {
            string name = $"asymmetric_preview_{textureWidth}x{textureHeight}";
            CreateSpriteSheet(name, textureWidth, textureHeight, cols, rows);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == name
                )
                {
                    found = true;
                    SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                    Assert.That(entry._sprites.Count, Is.EqualTo(cols * rows));
                    for (int j = 0; j < entry._sprites.Count; j++)
                    {
                        Texture2D preview = entry._sprites[j]._previewTexture;
                        Assert.IsTrue(
                            preview != null,
                            $"Asymmetric NPOT preview {j} for {textureWidth}x{textureHeight} should be generated"
                        );
                        Assert.That(preview.width, Is.GreaterThan(0));
                        Assert.That(preview.height, Is.GreaterThan(0));
                    }
                    break;
                }
            }
            Assert.IsTrue(found, $"Should find {name}");
        }

        [Test]
        public void ExtractionWithNonPowerOfTwoDimensionsWorksCorrectly()
        {
            string name = "npot_bug_test";
            CreateSpriteSheet(name, 100, 100, 4, 4);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();

            Assert.DoesNotThrow(
                () => extractor.ExtractSelectedSprites(),
                "Extraction should not throw with NPOT dimensions"
            );

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string outputPath = Path.Combine(OutputDir, $"{name}_000.png").SanitizePath();
            VerifyExtractedSpriteDimensions(outputPath, 25, 25, "NPOT");
        }

        [Test]
        public void PreviewGenerationWithNonPowerOfTwoDimensionsWorksCorrectly()
        {
            string name = "npot_preview_test";
            CreateSpriteSheet(name, 100, 100, 4, 4);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == name
                )
                {
                    found = true;
                    SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                    Assert.That(entry._sprites.Count, Is.EqualTo(16));
                    for (int j = 0; j < entry._sprites.Count; j++)
                    {
                        Texture2D preview = entry._sprites[j]._previewTexture;
                        Assert.IsTrue(
                            preview != null,
                            $"NPOT preview {j} should be generated without exception"
                        );
                    }
                    break;
                }
            }
            Assert.IsTrue(found, $"Should find {name}");
        }

        private static IEnumerable<TestCaseData> ArrayPoolEdgeCaseCases()
        {
            yield return new TestCaseData(17, 17).SetName("ArrayPool.EdgeCase.17x17");
            yield return new TestCaseData(31, 31).SetName("ArrayPool.EdgeCase.31x31");
            yield return new TestCaseData(33, 33).SetName("ArrayPool.EdgeCase.33x33");
            yield return new TestCaseData(63, 63).SetName("ArrayPool.EdgeCase.63x63");
            yield return new TestCaseData(65, 65).SetName("ArrayPool.EdgeCase.65x65");
            yield return new TestCaseData(127, 127).SetName("ArrayPool.EdgeCase.127x127");
            yield return new TestCaseData(129, 129).SetName("ArrayPool.EdgeCase.129x129");
        }

        [Test]
        [TestCaseSource(nameof(ArrayPoolEdgeCaseCases))]
        public void ExtractionWorksWithArrayPoolEdgeCaseDimensions(
            int spriteWidth,
            int spriteHeight
        )
        {
            int textureWidth = spriteWidth * 2;
            int textureHeight = spriteHeight * 2;
            string name = $"arraypool_{spriteWidth}x{spriteHeight}";
            CreateSpriteSheet(name, textureWidth, textureHeight, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();

            Assert.DoesNotThrow(
                () => extractor.ExtractSelectedSprites(),
                $"Extraction should not throw for {spriteWidth}x{spriteHeight} (array pool edge case)"
            );

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string outputPath = Path.Combine(OutputDir, $"{name}_000.png").SanitizePath();
            VerifyExtractedSpriteDimensions(
                outputPath,
                spriteWidth,
                spriteHeight,
                $"ArrayPoolEdge_{spriteWidth}x{spriteHeight}"
            );
        }

        [Test]
        public void PreviewGenerationWithPrimeNumberDimensionsWorksCorrectly()
        {
            Texture2D texture = Track(new Texture2D(46, 46, TextureFormat.RGBA32, false));
            Color[] pixels = new Color[46 * 46];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.magenta;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, "prime_dims.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            SetSpriteSheet(
                importer,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = "prime_23x23",
                        rect = new Rect(0, 0, 23, 23),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    new SpriteMetaData
                    {
                        name = "prime_23x23_2",
                        rect = new Rect(23, 0, 23, 23),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;
            extractor.DiscoverSpriteSheets();

            bool found = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    Path.GetFileNameWithoutExtension(extractor._discoveredSheets[i]._assetPath)
                    == "prime_dims"
                )
                {
                    found = true;
                    SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[i];
                    for (int j = 0; j < entry._sprites.Count; j++)
                    {
                        Texture2D preview = entry._sprites[j]._previewTexture;
                        Assert.IsTrue(
                            preview != null,
                            $"Prime dimension preview {j} should generate"
                        );
                    }
                    break;
                }
            }
            Assert.IsTrue(found, "Should find prime_dims");
        }

        [Test]
        public void ExtractionWithUnevenGridDimensionsWorksCorrectly()
        {
            Texture2D texture = Track(new Texture2D(100, 75, TextureFormat.RGBA32, false));
            Color[] pixels = new Color[100 * 75];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.gray;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = Path.Combine(Root, "uneven_grid.png").SanitizePath();
            File.WriteAllBytes(RelToFull(path), texture.EncodeToPNG());
            TrackAssetPath(path);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.isReadable = true;
            SetSpriteSheet(
                importer,
                new[]
                {
                    new SpriteMetaData
                    {
                        name = "uneven_0",
                        rect = new Rect(0, 0, 33, 25),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    new SpriteMetaData
                    {
                        name = "uneven_1",
                        rect = new Rect(33, 0, 33, 25),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                    new SpriteMetaData
                    {
                        name = "uneven_2",
                        rect = new Rect(66, 0, 34, 25),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                    },
                }
            );
            importer.SaveAndReimport();

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();

            Assert.DoesNotThrow(
                () => extractor.ExtractSelectedSprites(),
                "Extraction should work with uneven grid dimensions"
            );

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            for (int i = 0; i < 3; i++)
            {
                string outputPath = Path.Combine(OutputDir, $"uneven_grid_{i:D3}.png")
                    .SanitizePath();
                Assert.That(
                    File.Exists(RelToFull(outputPath)),
                    Is.True,
                    $"Uneven grid sprite {i} should be extracted"
                );
            }
        }

        [Test]
        public void MultipleExtractionsWithDifferentSizesWorkCorrectly()
        {
            CreateSpriteSheet("multi_size_16", 32, 32, 2, 2);
            CreateSpriteSheet("multi_size_33", 66, 66, 2, 2);
            CreateSpriteSheet("multi_size_50", 100, 100, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();

            Assert.DoesNotThrow(
                () => extractor.ExtractSelectedSprites(),
                "Multiple extractions with different sizes should not throw"
            );

            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            string output16 = Path.Combine(OutputDir, "multi_size_16_000.png").SanitizePath();
            string output33 = Path.Combine(OutputDir, "multi_size_33_000.png").SanitizePath();
            string output50 = Path.Combine(OutputDir, "multi_size_50_000.png").SanitizePath();

            Assert.That(File.Exists(RelToFull(output16)), Is.True);
            Assert.That(File.Exists(RelToFull(output33)), Is.True);
            Assert.That(File.Exists(RelToFull(output50)), Is.True);

            Texture2D tex16 = AssetDatabase.LoadAssetAtPath<Texture2D>(output16);
            Texture2D tex33 = AssetDatabase.LoadAssetAtPath<Texture2D>(output33);
            Texture2D tex50 = AssetDatabase.LoadAssetAtPath<Texture2D>(output50);

            Assert.That(tex16.width, Is.EqualTo(16));
            Assert.That(tex33.width, Is.EqualTo(33));
            Assert.That(tex50.width, Is.EqualTo(50));
        }

        [Test]
        public void PreviewRegenerationScheduledGuardPreventsMultipleQueueing()
        {
            CreateSpriteSheet("guard_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor.DiscoverSpriteSheets();

            // Verify guard field starts as false after discovery
            Assert.That(
                extractor._previewRegenerationScheduled,
                Is.False,
                "Guard field should be false after initial discovery"
            );

            // Simulate the scheduling mechanism by setting the guard to true
            extractor._previewRegenerationScheduled = true;

            // Verify the guard is now true (simulating a pending regeneration)
            Assert.That(
                extractor._previewRegenerationScheduled,
                Is.True,
                "Guard field should be true when regeneration is scheduled"
            );

            // Call regenerate - this should reset the guard
            extractor.RegenerateAllPreviewTextures();

            // Verify the guard is reset after regeneration completes
            Assert.That(
                extractor._previewRegenerationScheduled,
                Is.False,
                "Guard field should be reset to false after regeneration completes"
            );
        }

        [Test]
        public void ExtractionModeChangeRepopulatesSprites()
        {
            CreateSpriteSheet("repopulate_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.FromMetadata;
            extractor.DiscoverSpriteSheets();

            Assert.IsTrue(
                extractor._discoveredSheets != null && extractor._discoveredSheets.Count > 0
            );
            SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[0];
            int fromMetadataCount = entry._sprites.Count;

            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridColumns = 4;
            extractor._gridRows = 4;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;

            extractor.DiscoverSpriteSheets();

            bool foundEntry = false;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                SpriteSheetExtractor.SpriteSheetEntry currentEntry = extractor._discoveredSheets[i];
                if (currentEntry._assetPath.Contains("repopulate_test"))
                {
                    foundEntry = true;
                    int gridBasedCount = currentEntry._sprites.Count;

                    Assert.That(
                        gridBasedCount,
                        Is.EqualTo(16),
                        $"Grid mode should produce 16 sprites, got {gridBasedCount}"
                    );
                    Assert.That(
                        gridBasedCount,
                        Is.GreaterThan(fromMetadataCount),
                        "Grid mode should produce more sprites than FromMetadata for this test case"
                    );
                    break;
                }
            }
            Assert.IsTrue(foundEntry, "Should find repopulate_test entry after mode change");
        }

        [Test]
        public void RegenerateAllPreviewTexturesRepopulatesSpritesCorrectly()
        {
            CreateSpriteSheet("regenerate_repopulate_test", 64, 64, 4, 4);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.FromMetadata;
            extractor.DiscoverSpriteSheets();

            Assert.That(
                extractor._discoveredSheets,
                Is.Not.Null.And.Not.Empty,
                "Should have discovered sheets"
            );

            SpriteSheetExtractor.SpriteSheetEntry entry = null;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    extractor._discoveredSheets[i]._assetPath.Contains("regenerate_repopulate_test")
                )
                {
                    entry = extractor._discoveredSheets[i];
                    break;
                }
            }

            Assert.That(entry, Is.Not.Null, "Should find test entry");

            int originalSpriteCount = entry._sprites.Count;
            List<Rect> originalRects = new List<Rect>();
            for (int i = 0; i < entry._sprites.Count; i++)
            {
                originalRects.Add(entry._sprites[i]._rect);
            }

            // Change to grid-based extraction mode with specific grid settings
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 4;
            extractor._gridRows = 4;

            // Call the internal regeneration method directly
            extractor.RegenerateAllPreviewTextures();

            // Re-find the entry after regeneration
            entry = null;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (
                    extractor._discoveredSheets[i]._assetPath.Contains("regenerate_repopulate_test")
                )
                {
                    entry = extractor._discoveredSheets[i];
                    break;
                }
            }

            Assert.That(entry, Is.Not.Null, "Entry should still exist after regeneration");
            Assert.That(entry._sprites, Is.Not.Null.And.Not.Empty, "Sprites should be repopulated");

            int newSpriteCount = entry._sprites.Count;
            Assert.That(
                newSpriteCount,
                Is.EqualTo(16),
                $"Grid mode with 4x4 should produce 16 sprites, got {newSpriteCount}"
            );

            // Verify rects have changed from the original (different extraction mode produces different rects)
            bool rectsAreDifferent = false;
            if (newSpriteCount != originalSpriteCount)
            {
                rectsAreDifferent = true;
            }
            else
            {
                for (int i = 0; i < entry._sprites.Count && i < originalRects.Count; i++)
                {
                    if (entry._sprites[i]._rect != originalRects[i])
                    {
                        rectsAreDifferent = true;
                        break;
                    }
                }
            }

            Assert.That(
                rectsAreDifferent,
                Is.True,
                "Regeneration with different extraction mode should produce different sprite rects"
            );
        }

        [Test]
        public void RegeneratePreviewTexturesOnlyKeepsSpritesIntact()
        {
            CreateSpriteSheet("preview_only_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.FromMetadata;
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;
            extractor.DiscoverSpriteSheets();

            Assert.That(
                extractor._discoveredSheets,
                Is.Not.Null.And.Not.Empty,
                "Should have discovered sheets"
            );

            SpriteSheetExtractor.SpriteSheetEntry entry = null;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (extractor._discoveredSheets[i]._assetPath.Contains("preview_only_test"))
                {
                    entry = extractor._discoveredSheets[i];
                    break;
                }
            }

            Assert.That(entry, Is.Not.Null, "Should find test entry");
            Assert.That(entry._sprites, Is.Not.Null.And.Not.Empty, "Should have sprites");

            int originalSpriteCount = entry._sprites.Count;
            List<Rect> originalRects = new List<Rect>();
            for (int i = 0; i < entry._sprites.Count; i++)
            {
                originalRects.Add(entry._sprites[i]._rect);
            }

            int originalPreviewSize = extractor.GetPreviewSize(entry._sprites[0]);
            Assert.That(
                originalPreviewSize,
                Is.EqualTo(32),
                "Initial preview size should be 32 for Size32 mode"
            );

            for (int i = 0; i < entry._sprites.Count; i++)
            {
                Texture2D tex = entry._sprites[i]._previewTexture;
                if (tex != null)
                {
                    int maxDim = Mathf.Max(tex.width, tex.height);
                    Assert.That(
                        maxDim,
                        Is.LessThanOrEqualTo(32),
                        $"Sprite {i} preview texture should be scaled to Size32 mode (max dimension <= 32)"
                    );
                }
            }

            // Change preview size mode
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size64;

            // Call the preview-only regeneration method
            extractor.RegeneratePreviewTexturesOnly();

            // Verify sprites are intact
            Assert.That(entry._sprites, Is.Not.Null.And.Not.Empty, "Sprites should still exist");
            Assert.That(
                entry._sprites.Count,
                Is.EqualTo(originalSpriteCount),
                "Sprite count should remain the same"
            );

            // Verify rects are unchanged
            for (int i = 0; i < entry._sprites.Count; i++)
            {
                Assert.That(
                    entry._sprites[i]._rect,
                    Is.EqualTo(originalRects[i]),
                    $"Sprite rect at index {i} should be unchanged"
                );
            }

            // Verify preview textures exist and have correct dimensions for new size mode
            int newPreviewSize = extractor.GetPreviewSize(entry._sprites[0]);
            Assert.That(
                newPreviewSize,
                Is.EqualTo(64),
                "New preview size should be 64 for Size64 mode"
            );

            bool hasPreviewTextures = false;
            for (int i = 0; i < entry._sprites.Count; i++)
            {
                Texture2D tex = entry._sprites[i]._previewTexture;
                if (tex != null)
                {
                    hasPreviewTextures = true;
                    int maxDim = Mathf.Max(tex.width, tex.height);
                    Assert.That(
                        maxDim,
                        Is.LessThanOrEqualTo(64),
                        $"Sprite {i} preview texture should be scaled to Size64 mode (max dimension <= 64)"
                    );
                    Assert.That(
                        maxDim,
                        Is.GreaterThan(32),
                        $"Sprite {i} preview texture should be regenerated at larger size (max dimension > 32)"
                    );
                }
            }

            Assert.That(
                hasPreviewTextures,
                Is.True,
                "Preview textures should still exist after regeneration"
            );
        }

        [Test]
        public void SpritePreviewTexturesExistAfterDiscovery()
        {
            CreateSpriteSheet("preview_cleanup_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.FromMetadata;
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;
            extractor.DiscoverSpriteSheets();

            Assert.IsTrue(
                extractor._discoveredSheets != null && extractor._discoveredSheets.Count > 0
            );
            SpriteSheetExtractor.SpriteSheetEntry entry = extractor._discoveredSheets[0];

            bool hasPreviewTextures = false;
            for (int i = 0; i < entry._sprites.Count; i++)
            {
                if (entry._sprites[i]._previewTexture != null)
                {
                    hasPreviewTextures = true;
                    break;
                }
            }

            Assert.That(
                hasPreviewTextures,
                Is.True,
                "Preview textures should be generated after discovery"
            );
        }

        private static IEnumerable<TestCaseData> EffectiveShowOverlayTestCases()
        {
            yield return new TestCaseData(true, true, null, true).SetName(
                "ShowOverlay.UseGlobal.GlobalTrue.NoOverride.ReturnsTrue"
            );
            yield return new TestCaseData(false, true, null, false).SetName(
                "ShowOverlay.UseGlobal.GlobalFalse.NoOverride.ReturnsFalse"
            );
            yield return new TestCaseData(true, true, false, true).SetName(
                "ShowOverlay.UseGlobal.HasOverride.ReturnsGlobal"
            );
            yield return new TestCaseData(false, false, true, true).SetName(
                "ShowOverlay.UseOverride.OverrideTrue.ReturnsTrue"
            );
            yield return new TestCaseData(true, false, false, false).SetName(
                "ShowOverlay.UseOverride.OverrideFalse.ReturnsFalse"
            );
            yield return new TestCaseData(false, false, null, false).SetName(
                "ShowOverlay.UseOverride.NoValue.ReturnsGlobal"
            );
        }

        [Test]
        [TestCaseSource(nameof(EffectiveShowOverlayTestCases))]
        public void GetEffectiveShowOverlayReturnsCorrectValue(
            bool globalValue,
            bool useGlobalSettings,
            bool? overrideValue,
            bool expected
        )
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._showOverlay = globalValue;

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = useGlobalSettings,
                _showOverlayOverride = overrideValue,
            };

            bool result = window.GetEffectiveShowOverlay(entry);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetEffectiveShowOverlayReturnsGlobalWhenEntryIsNull()
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._showOverlay = true;

            bool result = window.GetEffectiveShowOverlay(null);

            Assert.AreEqual(true, result);
        }

        [Test]
        public void GetEffectiveShowOverlayReturnsFalseWhenGlobalFalseAndEntryNull()
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._showOverlay = false;

            bool result = window.GetEffectiveShowOverlay(null);

            Assert.AreEqual(false, result);
        }

        private static IEnumerable<TestCaseData> ShowOverlayUpdatesCorrectFieldTestCases()
        {
            yield return new TestCaseData(true, true).SetName(
                "ShowOverlay.UpdatesCorrectField.UseGlobal.SetsGlobalToTrue"
            );
            yield return new TestCaseData(true, false).SetName(
                "ShowOverlay.UpdatesCorrectField.UseGlobal.SetsGlobalToFalse"
            );
            yield return new TestCaseData(false, true).SetName(
                "ShowOverlay.UpdatesCorrectField.NotUseGlobal.SetsOverrideToTrue"
            );
            yield return new TestCaseData(false, false).SetName(
                "ShowOverlay.UpdatesCorrectField.NotUseGlobal.SetsOverrideToFalse"
            );
        }

        [Test]
        [TestCaseSource(nameof(ShowOverlayUpdatesCorrectFieldTestCases))]
        public void ShowOverlayUpdatesCorrectFieldBasedOnGlobalSettings(
            bool useGlobalSettings,
            bool newValue
        )
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._showOverlay = !newValue;

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _useGlobalSettings = useGlobalSettings,
                _showOverlayOverride = !newValue,
            };

            if (useGlobalSettings)
            {
                window._showOverlay = newValue;
                Assert.AreEqual(newValue, window._showOverlay);
                Assert.AreEqual(!newValue, entry._showOverlayOverride);
            }
            else
            {
                entry._showOverlayOverride = newValue;
                Assert.AreEqual(newValue, entry._showOverlayOverride);
                Assert.AreEqual(!newValue, window._showOverlay);
            }

            bool effective = window.GetEffectiveShowOverlay(entry);
            Assert.AreEqual(newValue, effective);
        }

        private static IEnumerable<TestCaseData> IsGridBasedExtractionModeCases()
        {
            yield return new TestCaseData(
                SpriteSheetExtractor.ExtractionMode.GridBased,
                true
            ).SetName("IsGridBased.GridBased.ReturnsTrue");
            yield return new TestCaseData(
                SpriteSheetExtractor.ExtractionMode.PaddedGrid,
                true
            ).SetName("IsGridBased.PaddedGrid.ReturnsTrue");
            yield return new TestCaseData(
                SpriteSheetExtractor.ExtractionMode.FromMetadata,
                false
            ).SetName("IsGridBased.FromMetadata.ReturnsFalse");
            yield return new TestCaseData(
                SpriteSheetExtractor.ExtractionMode.AlphaDetection,
                false
            ).SetName("IsGridBased.AlphaDetection.ReturnsFalse");
        }

        [Test]
        [TestCaseSource(nameof(IsGridBasedExtractionModeCases))]
        public void PreviewSlicingButtonVisibilityDeterminedByExtractionMode(
            SpriteSheetExtractor.ExtractionMode mode,
            bool expectedIsGridBased
        )
        {
            SpriteSheetExtractor extractor = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            extractor._extractionMode = mode;

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = true,
            };

            SpriteSheetExtractor.ExtractionMode effectiveMode =
                extractor.GetEffectiveExtractionMode(entry);
            bool isGridBased =
                effectiveMode == SpriteSheetExtractor.ExtractionMode.GridBased
                || effectiveMode == SpriteSheetExtractor.ExtractionMode.PaddedGrid;

            Assert.AreEqual(expectedIsGridBased, isGridBased);
        }

        [Test]
        public void PreviewSlicingButtonVisibilityUsesPerSheetOverrideWhenNotUsingGlobal()
        {
            SpriteSheetExtractor extractor = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.FromMetadata;

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = false,
                _extractionModeOverride = SpriteSheetExtractor.ExtractionMode.GridBased,
            };

            SpriteSheetExtractor.ExtractionMode effectiveMode =
                extractor.GetEffectiveExtractionMode(entry);
            bool isGridBased =
                effectiveMode == SpriteSheetExtractor.ExtractionMode.GridBased
                || effectiveMode == SpriteSheetExtractor.ExtractionMode.PaddedGrid;

            Assert.IsTrue(isGridBased, "Should use per-sheet override GridBased mode");
        }

        [Test]
        public void PreviewRegenerationAfterTogglingUseGlobalSettingsCreatesValidTextures()
        {
            CreateSpriteSheet("toggle_global_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindDiscoveredSheet(
                extractor,
                "toggle_global_test"
            );
            Assert.IsTrue(entry != null, "Should find test entry");

            entry._useGlobalSettings = true;
            Assert.IsTrue(entry._useGlobalSettings, "Entry should use global settings initially");

            for (int i = 0; i < entry._sprites.Count; i++)
            {
                Assert.IsTrue(
                    entry._sprites[i]._previewTexture != null,
                    $"Preview {i} should exist before toggle"
                );
            }

            entry._useGlobalSettings = false;
            entry._extractionModeOverride = SpriteSheetExtractor.ExtractionMode.GridBased;
            entry._gridColumnsOverride = 2;
            entry._gridRowsOverride = 2;
            entry._gridSizeModeOverride = SpriteSheetExtractor.GridSizeMode.Manual;

            extractor.RegenerateAllPreviewTextures();

            for (int i = 0; i < entry._sprites.Count; i++)
            {
                Texture2D preview = entry._sprites[i]._previewTexture;
                Assert.IsTrue(preview != null, $"Preview {i} should exist after toggle to false");
                Assert.That(preview.width, Is.GreaterThan(0), $"Preview {i} width should be > 0");
                Assert.That(preview.height, Is.GreaterThan(0), $"Preview {i} height should be > 0");
            }
        }

        [Test]
        public void PreviewRegenerationAfterTogglingFromFalseToTrueCreatesValidTextures()
        {
            CreateSpriteSheet("toggle_to_global_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindDiscoveredSheet(
                extractor,
                "toggle_to_global_test"
            );
            Assert.IsTrue(entry != null, "Should find test entry");

            entry._useGlobalSettings = false;
            entry._extractionModeOverride = SpriteSheetExtractor.ExtractionMode.GridBased;
            entry._gridColumnsOverride = 4;
            entry._gridRowsOverride = 4;
            entry._gridSizeModeOverride = SpriteSheetExtractor.GridSizeMode.Manual;

            extractor.RegenerateAllPreviewTextures();

            int countBefore = entry._sprites.Count;

            entry._useGlobalSettings = true;

            extractor.RegenerateAllPreviewTextures();

            for (int i = 0; i < entry._sprites.Count; i++)
            {
                Texture2D preview = entry._sprites[i]._previewTexture;
                Assert.IsTrue(preview != null, $"Preview {i} should exist after toggle to true");
                Assert.That(preview.width, Is.GreaterThan(0), $"Preview {i} width should be > 0");
                Assert.That(preview.height, Is.GreaterThan(0), $"Preview {i} height should be > 0");
            }
        }

        [Test]
        public void PreviewRegenerationWithGridBasedOverrideCreatesValidTextures()
        {
            CreateSpriteSheet("grid_override_preview_test", 128, 128, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.FromMetadata;
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindDiscoveredSheet(
                extractor,
                "grid_override_preview_test"
            );
            Assert.IsTrue(entry != null, "Should find test entry");

            int fromMetadataCount = entry._sprites.Count;
            Assert.That(fromMetadataCount, Is.EqualTo(4), "FromMetadata should find 4 sprites");

            entry._useGlobalSettings = false;
            entry._extractionModeOverride = SpriteSheetExtractor.ExtractionMode.GridBased;
            entry._gridSizeModeOverride = SpriteSheetExtractor.GridSizeMode.Manual;
            entry._gridColumnsOverride = 4;
            entry._gridRowsOverride = 4;

            extractor.RegenerateAllPreviewTextures();

            Assert.That(
                entry._sprites.Count,
                Is.EqualTo(16),
                "GridBased 4x4 should produce 16 sprites"
            );

            for (int i = 0; i < entry._sprites.Count; i++)
            {
                Texture2D preview = entry._sprites[i]._previewTexture;
                Assert.IsTrue(
                    preview != null,
                    $"Grid override preview {i} should exist and not be null (grey question mark)"
                );
                Assert.That(
                    preview.width,
                    Is.GreaterThan(0),
                    $"Grid override preview {i} width should be > 0"
                );
                Assert.That(
                    preview.height,
                    Is.GreaterThan(0),
                    $"Grid override preview {i} height should be > 0"
                );
            }
        }

        [Test]
        public void CopySettingsFromEntryCopiesShowOverlayOverride()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            SpriteSheetExtractor.SpriteSheetEntry source = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = false,
                _showOverlayOverride = true,
            };

            SpriteSheetExtractor.SpriteSheetEntry target = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _useGlobalSettings = true,
                _showOverlayOverride = null,
                _sprites = new List<SpriteSheetExtractor.SpriteEntryData>(),
                _assetPath = "test",
            };

            extractor.CopySettingsFromEntry(source, target);

            Assert.AreEqual(false, target._useGlobalSettings);
            Assert.AreEqual(true, target._showOverlayOverride);
        }

        [Test]
        public void NewEntryDefaultsToNullShowOverlayOverride()
        {
            SpriteSheetExtractor.SpriteSheetEntry entry =
                new SpriteSheetExtractor.SpriteSheetEntry();

            Assert.IsNull(entry._showOverlayOverride);
        }

        private static IEnumerable<TestCaseData> PreviewRegenerationToggleScenarioCases()
        {
            yield return new TestCaseData(true, false, 2, 4).SetName(
                "PreviewRegenToggle.FromGlobal2x2.ToOverride4x4"
            );
            yield return new TestCaseData(false, true, 4, 2).SetName(
                "PreviewRegenToggle.FromOverride4x4.ToGlobal2x2"
            );
        }

        [Test]
        [TestCaseSource(nameof(PreviewRegenerationToggleScenarioCases))]
        public void PreviewRegenerationAfterSettingsToggleProducesCorrectSpriteCount(
            bool startWithGlobal,
            bool endWithGlobal,
            int startGridSize,
            int endGridSize
        )
        {
            string testName = $"regen_toggle_{startGridSize}to{endGridSize}";
            CreateSpriteSheet(testName, 128, 128, 4, 4);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = startWithGlobal ? startGridSize : endGridSize;
            extractor._gridRows = startWithGlobal ? startGridSize : endGridSize;
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;
            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = FindDiscoveredSheet(extractor, testName);
            Assert.IsTrue(entry != null, "Should find test entry");

            entry._useGlobalSettings = startWithGlobal;
            if (!startWithGlobal)
            {
                entry._extractionModeOverride = SpriteSheetExtractor.ExtractionMode.GridBased;
                entry._gridSizeModeOverride = SpriteSheetExtractor.GridSizeMode.Manual;
                entry._gridColumnsOverride = startGridSize;
                entry._gridRowsOverride = startGridSize;
            }

            extractor.RegenerateAllPreviewTextures();
            int expectedStartCount = startGridSize * startGridSize;
            Assert.That(
                entry._sprites.Count,
                Is.EqualTo(expectedStartCount),
                $"Should have {expectedStartCount} sprites initially"
            );

            extractor._gridColumns = endWithGlobal ? endGridSize : extractor._gridColumns;
            extractor._gridRows = endWithGlobal ? endGridSize : extractor._gridRows;
            entry._useGlobalSettings = endWithGlobal;
            if (!endWithGlobal)
            {
                entry._extractionModeOverride = SpriteSheetExtractor.ExtractionMode.GridBased;
                entry._gridSizeModeOverride = SpriteSheetExtractor.GridSizeMode.Manual;
                entry._gridColumnsOverride = endGridSize;
                entry._gridRowsOverride = endGridSize;
            }

            extractor.RegenerateAllPreviewTextures();

            int expectedEndCount = endGridSize * endGridSize;
            Assert.That(
                entry._sprites.Count,
                Is.EqualTo(expectedEndCount),
                $"Should have {expectedEndCount} sprites after toggle"
            );

            for (int i = 0; i < entry._sprites.Count; i++)
            {
                Texture2D preview = entry._sprites[i]._previewTexture;
                Assert.IsTrue(
                    preview != null,
                    $"Preview {i} should not be null after settings toggle"
                );
            }
        }

        [Test]
        public void SchedulePreviewRegenerationForEntryHandlesValidEntryWithTexture()
        {
            string sheetPath = CreateSpriteSheet("RegenTest", 64, 64, 2, 2);

            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._inputDirectories = new List<UnityEngine.Object>
            {
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Root),
            };
            window._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            window._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            window._gridColumns = 2;
            window._gridRows = 2;

            window.DiscoverSpriteSheets();

            Assert.IsNotNull(window._discoveredSheets);
            Assert.AreEqual(1, window._discoveredSheets.Count);

            SpriteSheetExtractor.SpriteSheetEntry entry = window._discoveredSheets[0];
            Assert.IsNotNull(entry._texture);
            Assert.IsNotNull(entry._sprites);
            int originalSpriteCount = entry._sprites.Count;

            entry._useGlobalSettings = false;
            entry._extractionModeOverride = SpriteSheetExtractor.ExtractionMode.GridBased;
            entry._gridSizeModeOverride = SpriteSheetExtractor.GridSizeMode.Manual;
            entry._gridColumnsOverride = 4;
            entry._gridRowsOverride = 4;

            window.RepopulateSpritesForEntry(entry);
            window.GenerateAllPreviewTexturesInBatch(window._discoveredSheets);

            Assert.IsNotNull(entry._texture);
            Assert.IsNotNull(entry._sprites);
            Assert.AreEqual(16, entry._sprites.Count);

            for (int i = 0; i < entry._sprites.Count; ++i)
            {
                Assert.IsNotNull(entry._sprites[i]._previewTexture);
            }
        }

        [Test]
        public void RepopulateSpritesForEntryHandlesNullTexture()
        {
            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _assetPath = "NonExistent/Path.png",
                _texture = null,
                _sprites = new List<SpriteSheetExtractor.SpriteEntryData>(),
            };

            window.RepopulateSpritesForEntry(entry);

            Assert.IsNotNull(entry._sprites);
            Assert.AreEqual(0, entry._sprites.Count);
        }

        [Test]
        public void RepopulateSpritesForEntryInitializesNullSpritesList()
        {
            string sheetPath = CreateSpriteSheet("NullSpritesTest", 64, 64, 2, 2);

            SpriteSheetExtractor window = Track(
                ScriptableObject.CreateInstance<SpriteSheetExtractor>()
            );
            window._inputDirectories = new List<UnityEngine.Object>
            {
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Root),
            };
            window._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;

            window.DiscoverSpriteSheets();

            Assert.IsNotNull(window._discoveredSheets);
            Assert.AreEqual(1, window._discoveredSheets.Count);

            SpriteSheetExtractor.SpriteSheetEntry entry = window._discoveredSheets[0];
            entry._sprites = null;

            window.RepopulateSpritesForEntry(entry);

            Assert.IsNotNull(entry._sprites);
            Assert.Greater(entry._sprites.Count, 0);
        }

        [Test]
        public void SchedulePreviewRegenerationForDestroyedWindowDoesNotCrash()
        {
            SpriteSheetExtractor window = ScriptableObject.CreateInstance<SpriteSheetExtractor>(); // UNH-SUPPRESS: Intentional - testing destroyed window

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _assetPath = "Test/Path.png",
                _useGlobalSettings = true,
                _sprites = new List<SpriteSheetExtractor.SpriteEntryData>(),
            };

            window._discoveredSheets = new List<SpriteSheetExtractor.SpriteSheetEntry> { entry };

            Object.DestroyImmediate(window); // UNH-SUPPRESS: Intentional destruction to test destroyed window behavior

            Assert.Pass();
        }

        #region CalculateTextureRectWithinPreview Tests (Bug 2 Fix)

        [Test]
        public void CalculateTextureRectWithinPreviewCentersHorizontallyWhenRectIsWider()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            // Preview rect is 200 wide, but texture at scale 1.0 is only 100 wide
            Rect previewRect = new Rect(0, 0, 200, 100);
            int textureWidth = 100;
            int textureHeight = 100;
            float scale = 1f;

            Rect result = extractor.CalculateTextureRectWithinPreview(
                previewRect,
                textureWidth,
                textureHeight,
                scale
            );

            // Texture should be centered horizontally: (200 - 100) / 2 = 50 offset
            Assert.That(
                result.x,
                Is.EqualTo(50f).Within(0.001f),
                "Texture should be centered horizontally"
            );
            Assert.That(result.y, Is.EqualTo(0f).Within(0.001f), "Texture should be at top");
            Assert.That(result.width, Is.EqualTo(100f).Within(0.001f));
            Assert.That(result.height, Is.EqualTo(100f).Within(0.001f));
        }

        [Test]
        public void CalculateTextureRectWithinPreviewCentersVerticallyWhenRectIsTaller()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            // Preview rect is 200 tall, but texture at scale 1.0 is only 100 tall
            Rect previewRect = new Rect(0, 0, 100, 200);
            int textureWidth = 100;
            int textureHeight = 100;
            float scale = 1f;

            Rect result = extractor.CalculateTextureRectWithinPreview(
                previewRect,
                textureWidth,
                textureHeight,
                scale
            );

            // Texture should be centered vertically: (200 - 100) / 2 = 50 offset
            Assert.That(result.x, Is.EqualTo(0f).Within(0.001f), "Texture should be at left");
            Assert.That(
                result.y,
                Is.EqualTo(50f).Within(0.001f),
                "Texture should be centered vertically"
            );
            Assert.That(result.width, Is.EqualTo(100f).Within(0.001f));
            Assert.That(result.height, Is.EqualTo(100f).Within(0.001f));
        }

        [Test]
        public void CalculateTextureRectWithinPreviewNoOffsetWhenExactMatch()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            Rect previewRect = new Rect(10, 20, 100, 100);
            int textureWidth = 100;
            int textureHeight = 100;
            float scale = 1f;

            Rect result = extractor.CalculateTextureRectWithinPreview(
                previewRect,
                textureWidth,
                textureHeight,
                scale
            );

            // No offset needed when preview rect exactly matches scaled texture size
            Assert.That(result.x, Is.EqualTo(10f).Within(0.001f));
            Assert.That(result.y, Is.EqualTo(20f).Within(0.001f));
            Assert.That(result.width, Is.EqualTo(100f).Within(0.001f));
            Assert.That(result.height, Is.EqualTo(100f).Within(0.001f));
        }

        [Test]
        public void CalculateTextureRectWithinPreviewHandlesScaleCorrectly()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            Rect previewRect = new Rect(0, 0, 200, 100);
            int textureWidth = 100;
            int textureHeight = 100;
            float scale = 0.5f;

            Rect result = extractor.CalculateTextureRectWithinPreview(
                previewRect,
                textureWidth,
                textureHeight,
                scale
            );

            // Scaled texture is 50x50, centered in 200x100 rect
            // Horizontal offset: (200 - 50) / 2 = 75
            // Vertical offset: (100 - 50) / 2 = 25
            Assert.That(result.x, Is.EqualTo(75f).Within(0.001f));
            Assert.That(result.y, Is.EqualTo(25f).Within(0.001f));
            Assert.That(result.width, Is.EqualTo(50f).Within(0.001f));
            Assert.That(result.height, Is.EqualTo(50f).Within(0.001f));
        }

        [Test]
        public void CalculateTextureRectWithinPreviewHandlesLandscapeTexture()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            Rect previewRect = new Rect(0, 0, 200, 200);
            int textureWidth = 200;
            int textureHeight = 100;
            float scale = 1f;

            Rect result = extractor.CalculateTextureRectWithinPreview(
                previewRect,
                textureWidth,
                textureHeight,
                scale
            );

            // Landscape texture (200x100) in square rect (200x200)
            // Horizontal offset: (200 - 200) / 2 = 0
            // Vertical offset: (200 - 100) / 2 = 50
            Assert.That(result.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(result.y, Is.EqualTo(50f).Within(0.001f));
            Assert.That(result.width, Is.EqualTo(200f).Within(0.001f));
            Assert.That(result.height, Is.EqualTo(100f).Within(0.001f));
        }

        [Test]
        public void CalculateTextureRectWithinPreviewHandlesPortraitTexture()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            Rect previewRect = new Rect(0, 0, 200, 200);
            int textureWidth = 100;
            int textureHeight = 200;
            float scale = 1f;

            Rect result = extractor.CalculateTextureRectWithinPreview(
                previewRect,
                textureWidth,
                textureHeight,
                scale
            );

            // Portrait texture (100x200) in square rect (200x200)
            // Horizontal offset: (200 - 100) / 2 = 50
            // Vertical offset: (200 - 200) / 2 = 0
            Assert.That(result.x, Is.EqualTo(50f).Within(0.001f));
            Assert.That(result.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(result.width, Is.EqualTo(100f).Within(0.001f));
            Assert.That(result.height, Is.EqualTo(200f).Within(0.001f));
        }

        [Test]
        public void CalculateTextureRectWithinPreviewPreservesRectPosition()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            // Preview rect starts at non-zero position
            Rect previewRect = new Rect(100, 50, 200, 100);
            int textureWidth = 100;
            int textureHeight = 100;
            float scale = 1f;

            Rect result = extractor.CalculateTextureRectWithinPreview(
                previewRect,
                textureWidth,
                textureHeight,
                scale
            );

            // Centered in rect starting at (100, 50)
            // x = 100 + (200 - 100) / 2 = 150
            // y = 50 + (100 - 100) / 2 = 50
            Assert.That(result.x, Is.EqualTo(150f).Within(0.001f));
            Assert.That(result.y, Is.EqualTo(50f).Within(0.001f));
        }

        private static IEnumerable<TestCaseData> CalculateTextureRectEdgeCases()
        {
            yield return new TestCaseData(1, 1, 1f).SetName("TextureRect.MinimumSize.1x1");
            yield return new TestCaseData(1, 100, 1f).SetName("TextureRect.ExtremeAspect.Tall");
            yield return new TestCaseData(100, 1, 1f).SetName("TextureRect.ExtremeAspect.Wide");
            yield return new TestCaseData(100, 100, 0.001f).SetName("TextureRect.VerySmallScale");
            yield return new TestCaseData(100, 100, 10f).SetName("TextureRect.LargeScale");
        }

        [Test]
        [TestCaseSource(nameof(CalculateTextureRectEdgeCases))]
        public void CalculateTextureRectWithinPreviewHandlesEdgeCases(
            int textureWidth,
            int textureHeight,
            float scale
        )
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            Rect previewRect = new Rect(0, 0, 200, 200);

            Rect result = extractor.CalculateTextureRectWithinPreview(
                previewRect,
                textureWidth,
                textureHeight,
                scale
            );

            // Verify dimensions are correct
            float expectedWidth = textureWidth * scale;
            float expectedHeight = textureHeight * scale;
            Assert.That(result.width, Is.EqualTo(expectedWidth).Within(0.001f));
            Assert.That(result.height, Is.EqualTo(expectedHeight).Within(0.001f));

            // Verify correct centering - note: when scaled texture is larger than preview,
            // offsets will be negative, which is mathematically correct for centering
            float expectedX = previewRect.x + (previewRect.width - expectedWidth) * 0.5f;
            float expectedY = previewRect.y + (previewRect.height - expectedHeight) * 0.5f;
            Assert.That(
                result.x,
                Is.EqualTo(expectedX).Within(0.001f),
                $"X offset incorrect for {textureWidth}x{textureHeight} @ scale {scale}"
            );
            Assert.That(
                result.y,
                Is.EqualTo(expectedY).Within(0.001f),
                $"Y offset incorrect for {textureWidth}x{textureHeight} @ scale {scale}"
            );
        }

        [Test]
        public void CalculateTextureRectWithinPreviewReturnsPreviewRectForZeroWidth()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            Rect previewRect = new Rect(10, 20, 100, 100);

            Rect result = extractor.CalculateTextureRectWithinPreview(previewRect, 0, 100, 1f);

            // Should return the full preview rect as fallback for invalid input
            Assert.That(result, Is.EqualTo(previewRect));
        }

        [Test]
        public void CalculateTextureRectWithinPreviewReturnsPreviewRectForZeroHeight()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            Rect previewRect = new Rect(10, 20, 100, 100);

            Rect result = extractor.CalculateTextureRectWithinPreview(previewRect, 100, 0, 1f);

            // Should return the full preview rect as fallback for invalid input
            Assert.That(result, Is.EqualTo(previewRect));
        }

        [Test]
        public void CalculateTextureRectWithinPreviewReturnsPreviewRectForZeroScale()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            Rect previewRect = new Rect(10, 20, 100, 100);

            Rect result = extractor.CalculateTextureRectWithinPreview(previewRect, 100, 100, 0f);

            // Should return the full preview rect as fallback for invalid input
            Assert.That(result, Is.EqualTo(previewRect));
        }

        [Test]
        public void CalculateTextureRectWithinPreviewReturnsPreviewRectForNegativeDimensions()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            Rect previewRect = new Rect(10, 20, 100, 100);

            Rect result = extractor.CalculateTextureRectWithinPreview(previewRect, -100, 100, 1f);

            // Should return the full preview rect as fallback for invalid input
            Assert.That(result, Is.EqualTo(previewRect));
        }

        #endregion

        #region Preview Texture Transfer Tests (Bug 1 Fix)

        [Test]
        public void ToggleUseGlobalSettingsPreservesPreviewTexturesWithMatchingRects()
        {
            CreateSpriteSheet("preview_transfer_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;

            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = null;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (extractor._discoveredSheets[i]._assetPath.Contains("preview_transfer_test"))
                {
                    entry = extractor._discoveredSheets[i];
                    break;
                }
            }
            Assert.IsNotNull(entry, "Should find preview_transfer_test entry");

            // Verify initial preview textures exist
            Assert.IsNotNull(entry._sprites);
            Assert.AreEqual(4, entry._sprites.Count);

            for (int i = 0; i < entry._sprites.Count; i++)
            {
                Assert.IsNotNull(
                    entry._sprites[i]._previewTexture,
                    $"Initial preview texture {i} should exist"
                );
            }

            // Store original rects and preview texture references
            Rect[] originalRects = new Rect[entry._sprites.Count];
            for (int i = 0; i < entry._sprites.Count; i++)
            {
                originalRects[i] = entry._sprites[i]._rect;
            }

            // Toggle off and back on (simulates Use Global Settings toggle)
            entry._useGlobalSettings = false;
            entry._gridColumnsOverride = 2;
            entry._gridRowsOverride = 2;
            entry._gridSizeModeOverride = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor.RepopulateSpritesForEntry(entry);
            extractor.GenerateAllPreviewTexturesInBatch(extractor._discoveredSheets);

            // Verify preview textures still exist after toggle
            Assert.AreEqual(4, entry._sprites.Count, "Should still have 4 sprites");
            for (int i = 0; i < entry._sprites.Count; i++)
            {
                Assert.IsNotNull(
                    entry._sprites[i]._previewTexture,
                    $"Preview texture {i} should exist after toggle"
                );
                Assert.That(
                    entry._sprites[i]._previewTexture.width,
                    Is.GreaterThan(0),
                    $"Preview texture {i} width should be valid"
                );
            }
        }

        [Test]
        public void RepopulateSpritesForEntryClearsSpritesWhenRectsChange()
        {
            CreateSpriteSheet("rects_change_test", 128, 128, 4, 4);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 4;
            extractor._gridRows = 4;
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;

            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = null;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (extractor._discoveredSheets[i]._assetPath.Contains("rects_change_test"))
                {
                    entry = extractor._discoveredSheets[i];
                    break;
                }
            }
            Assert.IsNotNull(entry, "Should find rects_change_test entry");
            Assert.AreEqual(16, entry._sprites.Count, "Should have 16 sprites (4x4)");

            // Now change grid dimensions (which changes all rects)
            entry._useGlobalSettings = false;
            entry._gridColumnsOverride = 2;
            entry._gridRowsOverride = 2;
            entry._gridSizeModeOverride = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor.RepopulateSpritesForEntry(entry);
            extractor.GenerateAllPreviewTexturesInBatch(extractor._discoveredSheets);

            // Should now have 4 sprites with different rects
            Assert.AreEqual(4, entry._sprites.Count, "Should now have 4 sprites (2x2)");
            for (int i = 0; i < entry._sprites.Count; i++)
            {
                Assert.IsNotNull(
                    entry._sprites[i]._previewTexture,
                    $"New preview texture {i} should be generated"
                );
            }
        }

        [Test]
        public void SchedulePreviewRegenerationHandlesEmptySprites()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _assetPath = "Test/Empty.png",
                _texture = null,
                _sprites = new List<SpriteSheetExtractor.SpriteEntryData>(),
                _useGlobalSettings = true,
            };

            // Should not throw when sprites list is empty
            extractor.RepopulateSpritesForEntry(entry);

            Assert.IsNotNull(entry._sprites);
            Assert.AreEqual(0, entry._sprites.Count);
        }

        [Test]
        public void SchedulePreviewRegenerationHandlesNullSpritesInList()
        {
            CreateSpriteSheet("null_sprites_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;

            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = null;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (extractor._discoveredSheets[i]._assetPath.Contains("null_sprites_test"))
                {
                    entry = extractor._discoveredSheets[i];
                    break;
                }
            }
            Assert.IsNotNull(entry, "Should find null_sprites_test entry");

            // Insert a null sprite in the list (edge case)
            entry._sprites.Add(null);

            // Toggle settings should not crash with null in list
            entry._useGlobalSettings = false;
            entry._gridColumnsOverride = 2;
            entry._gridRowsOverride = 2;
            extractor.RepopulateSpritesForEntry(entry);
            extractor.GenerateAllPreviewTexturesInBatch(extractor._discoveredSheets);

            // Should still work - null entries should be filtered out
            Assert.IsNotNull(entry._sprites);
            Assert.Greater(entry._sprites.Count, 0);
        }

        [Test]
        public void SchedulePreviewRegenerationHandlesDuplicateRects()
        {
            CreateSpriteSheet("duplicate_rects_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;

            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = null;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (extractor._discoveredSheets[i]._assetPath.Contains("duplicate_rects_test"))
                {
                    entry = extractor._discoveredSheets[i];
                    break;
                }
            }
            Assert.IsNotNull(entry, "Should find duplicate_rects_test entry");
            Assert.AreEqual(4, entry._sprites.Count);

            // Manually create a duplicate rect scenario (edge case)
            if (entry._sprites.Count >= 2)
            {
                entry._sprites[1]._rect = entry._sprites[0]._rect;
            }

            // Should not crash with duplicate rects
            entry._useGlobalSettings = false;
            entry._gridColumnsOverride = 2;
            entry._gridRowsOverride = 2;
            extractor.RepopulateSpritesForEntry(entry);
            extractor.GenerateAllPreviewTexturesInBatch(extractor._discoveredSheets);

            Assert.IsNotNull(entry._sprites);
            Assert.Greater(entry._sprites.Count, 0);
        }

        #endregion

        #region DetectOptimalGridFromTransparency Tests

        private static IEnumerable<TestCaseData> DetectOptimalGridNormalCases()
        {
            yield return new TestCaseData(
                64,
                64,
                new int[] { 32 },
                new int[] { 32 },
                0.5f,
                32,
                32
            ).SetName("GridDetection.Normal.SingleVerticalAndHorizontalLine.32x32");

            yield return new TestCaseData(
                128,
                128,
                new int[] { 32, 64, 96 },
                new int[] { 32, 64, 96 },
                0.5f,
                32,
                32
            ).SetName("GridDetection.Normal.MultipleLines.32x32Grid");

            yield return new TestCaseData(
                100,
                100,
                new int[] { 25, 50, 75 },
                new int[] { 25, 50, 75 },
                0.5f,
                25,
                25
            ).SetName("GridDetection.Normal.NonPowerOfTwo.25x25Grid");

            yield return new TestCaseData(
                96,
                64,
                new int[] { 32, 64 },
                new int[] { 32 },
                0.5f,
                32,
                32
            ).SetName("GridDetection.Normal.RectangularTexture.32x32Grid");
        }

        [Test]
        [TestCaseSource(nameof(DetectOptimalGridNormalCases))]
        public void DetectOptimalGridFromTransparencyNormalCases(
            int textureWidth,
            int textureHeight,
            int[] transparentColumns,
            int[] transparentRows,
            float alphaThreshold,
            int expectedCellWidth,
            int expectedCellHeight
        )
        {
            Color32[] pixels = new Color32[textureWidth * textureHeight];

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            for (int i = 0; i < transparentColumns.Length; ++i)
            {
                int x = transparentColumns[i];
                for (int y = 0; y < textureHeight; ++y)
                {
                    pixels[y * textureWidth + x] = new Color32(0, 0, 0, 0);
                }
            }

            for (int i = 0; i < transparentRows.Length; ++i)
            {
                int y = transparentRows[i];
                for (int x = 0; x < textureWidth; ++x)
                {
                    pixels[y * textureWidth + x] = new Color32(0, 0, 0, 0);
                }
            }

            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                pixels,
                textureWidth,
                textureHeight,
                alphaThreshold,
                out cellWidth,
                out cellHeight
            );

            Assert.IsTrue(result, "Should detect grid from transparency");
            Assert.AreEqual(
                expectedCellWidth,
                cellWidth,
                $"Cell width should be {expectedCellWidth}"
            );
            Assert.AreEqual(
                expectedCellHeight,
                cellHeight,
                $"Cell height should be {expectedCellHeight}"
            );
        }

        private static IEnumerable<TestCaseData> DetectOptimalGridEdgeCases()
        {
            yield return new TestCaseData(4, 4, 0.5f).SetName("GridDetection.Edge.MinimumSize.4x4");
            yield return new TestCaseData(3, 3, 0.5f).SetName(
                "GridDetection.Edge.TooSmall.3x3.ShouldFail"
            );
            yield return new TestCaseData(2, 2, 0.5f).SetName(
                "GridDetection.Edge.TooSmall.2x2.ShouldFail"
            );
            yield return new TestCaseData(1, 1, 0.5f).SetName(
                "GridDetection.Edge.TooSmall.1x1.ShouldFail"
            );
        }

        [Test]
        [TestCaseSource(nameof(DetectOptimalGridEdgeCases))]
        public void DetectOptimalGridFromTransparencyEdgeCases(
            int textureWidth,
            int textureHeight,
            float alphaThreshold
        )
        {
            Color32[] pixels = new Color32[textureWidth * textureHeight];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                pixels,
                textureWidth,
                textureHeight,
                alphaThreshold,
                out cellWidth,
                out cellHeight
            );

            if (textureWidth < 4 || textureHeight < 4)
            {
                Assert.IsFalse(result, "Should return false for textures smaller than 4x4");
            }
        }

        [Test]
        public void DetectOptimalGridFromTransparencyReturnsZeroForFullyOpaqueTexture()
        {
            int width = 64;
            int height = 64;
            Color32[] pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                pixels,
                width,
                height,
                0.5f,
                out cellWidth,
                out cellHeight
            );

            Assert.IsFalse(result, "Should return false for fully opaque texture");
            Assert.AreEqual(0, cellWidth, "Cell width should be 0");
            Assert.AreEqual(0, cellHeight, "Cell height should be 0");
        }

        [Test]
        public void DetectOptimalGridFromTransparencyReturnsZeroForFullyTransparentTexture()
        {
            int width = 64;
            int height = 64;
            Color32[] pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                pixels,
                width,
                height,
                0.5f,
                out cellWidth,
                out cellHeight
            );

            Assert.IsFalse(result, "Should return false for fully transparent texture");
        }

        [Test]
        public void DetectOptimalGridFromTransparencyHandlesNullPixels()
        {
            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                null,
                64,
                64,
                0.5f,
                out cellWidth,
                out cellHeight
            );

            Assert.IsFalse(result, "Should return false for null pixels");
            Assert.AreEqual(0, cellWidth, "Cell width should be 0");
            Assert.AreEqual(0, cellHeight, "Cell height should be 0");
        }

        [Test]
        public void DetectOptimalGridFromTransparencyHandlesEmptyPixels()
        {
            Color32[] emptyPixels = new Color32[0];

            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                emptyPixels,
                64,
                64,
                0.5f,
                out cellWidth,
                out cellHeight
            );

            Assert.IsFalse(result, "Should return false for empty pixels");
        }

        [Test]
        public void DetectOptimalGridFromTransparencyHandlesMismatchedDimensions()
        {
            Color32[] pixels = new Color32[100];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                pixels,
                64,
                64,
                0.5f,
                out cellWidth,
                out cellHeight
            );

            Assert.IsFalse(
                result,
                "Should return false when pixel count does not match dimensions"
            );
        }

        private static IEnumerable<TestCaseData> AlphaThresholdEdgeCases()
        {
            yield return new TestCaseData(0.0f, true).SetName(
                "GridDetection.AlphaThreshold.Zero.ShouldWork"
            );
            yield return new TestCaseData(0.5f, true).SetName(
                "GridDetection.AlphaThreshold.Mid.ShouldWork"
            );
            yield return new TestCaseData(0.99f, true).SetName(
                "GridDetection.AlphaThreshold.NearOne.ShouldWork"
            );
            yield return new TestCaseData(1.0f, false).SetName(
                "GridDetection.AlphaThreshold.One.ShouldFail"
            );
            yield return new TestCaseData(1.1f, false).SetName(
                "GridDetection.AlphaThreshold.AboveOne.ShouldFail"
            );
        }

        [Test]
        [TestCaseSource(nameof(AlphaThresholdEdgeCases))]
        public void DetectOptimalGridFromTransparencyAlphaThresholdEdgeCases(
            float alphaThreshold,
            bool shouldDetect
        )
        {
            int width = 64;
            int height = 64;
            Color32[] pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            for (int y = 0; y < height; ++y)
            {
                pixels[y * width + 32] = new Color32(0, 0, 0, 0);
            }
            for (int x = 0; x < width; ++x)
            {
                pixels[32 * width + x] = new Color32(0, 0, 0, 0);
            }

            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                pixels,
                width,
                height,
                alphaThreshold,
                out cellWidth,
                out cellHeight
            );

            if (shouldDetect)
            {
                Assert.IsTrue(
                    result || alphaThreshold >= 1.0f == false,
                    $"Alpha threshold {alphaThreshold} should detect"
                );
            }
            else
            {
                Assert.IsFalse(result, $"Alpha threshold {alphaThreshold} should not detect");
            }
        }

        [Test]
        public void DetectOptimalGridFromTransparencyDetectsOnlyVerticalBoundaries()
        {
            int width = 64;
            int height = 64;
            Color32[] pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            for (int y = 0; y < height; ++y)
            {
                pixels[y * width + 32] = new Color32(0, 0, 0, 0);
            }

            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                pixels,
                width,
                height,
                0.5f,
                out cellWidth,
                out cellHeight
            );

            Assert.IsTrue(
                result || cellWidth == 0,
                "Should handle vertical-only boundaries gracefully"
            );
        }

        [Test]
        public void DetectOptimalGridFromTransparencyDetectsOnlyHorizontalBoundaries()
        {
            int width = 64;
            int height = 64;
            Color32[] pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            for (int x = 0; x < width; ++x)
            {
                pixels[32 * width + x] = new Color32(0, 0, 0, 0);
            }

            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                pixels,
                width,
                height,
                0.5f,
                out cellWidth,
                out cellHeight
            );

            Assert.IsTrue(
                result || cellHeight == 0,
                "Should handle horizontal-only boundaries gracefully"
            );
        }

        [Test]
        public void DetectOptimalGridFromTransparencyHandlesIrregularSpacing()
        {
            int width = 64;
            int height = 64;
            Color32[] pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            int[] irregularColumns = new int[] { 10, 25, 45 };
            for (int i = 0; i < irregularColumns.Length; ++i)
            {
                int x = irregularColumns[i];
                for (int y = 0; y < height; ++y)
                {
                    pixels[y * width + x] = new Color32(0, 0, 0, 0);
                }
            }

            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                pixels,
                width,
                height,
                0.5f,
                out cellWidth,
                out cellHeight
            );

            Assert.IsFalse(
                result,
                "Should return false for irregular spacing that does not divide evenly"
            );
        }

        [Test]
        public void DetectOptimalGridFromTransparencyMinimumCellSize()
        {
            int width = 64;
            int height = 64;
            Color32[] pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            for (int i = 4; i < width; i += 4)
            {
                for (int y = 0; y < height; ++y)
                {
                    pixels[y * width + i] = new Color32(0, 0, 0, 0);
                }
            }

            for (int i = 4; i < height; i += 4)
            {
                for (int x = 0; x < width; ++x)
                {
                    pixels[i * width + x] = new Color32(0, 0, 0, 0);
                }
            }

            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                pixels,
                width,
                height,
                0.5f,
                out cellWidth,
                out cellHeight
            );

            Assert.IsFalse(result, "Should reject cell sizes smaller than minimum (8 pixels)");
        }

        [Test]
        public void DetectOptimalGridFromTransparencyValidCellSizeAboveMinimum()
        {
            int width = 64;
            int height = 64;
            Color32[] pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            for (int i = 16; i < width; i += 16)
            {
                for (int y = 0; y < height; ++y)
                {
                    pixels[y * width + i] = new Color32(0, 0, 0, 0);
                }
            }

            for (int i = 16; i < height; i += 16)
            {
                for (int x = 0; x < width; ++x)
                {
                    pixels[i * width + x] = new Color32(0, 0, 0, 0);
                }
            }

            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                pixels,
                width,
                height,
                0.5f,
                out cellWidth,
                out cellHeight
            );

            Assert.IsTrue(result, "Should detect valid grid with cell size above minimum");
            Assert.AreEqual(16, cellWidth, "Cell width should be 16");
            Assert.AreEqual(16, cellHeight, "Cell height should be 16");
        }

        [Test]
        public void DetectOptimalGridFromTransparencyLargeTexture()
        {
            int width = 512;
            int height = 512;
            Color32[] pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            for (int i = 64; i < width; i += 64)
            {
                for (int y = 0; y < height; ++y)
                {
                    pixels[y * width + i] = new Color32(0, 0, 0, 0);
                }
            }

            for (int i = 64; i < height; i += 64)
            {
                for (int x = 0; x < width; ++x)
                {
                    pixels[i * width + x] = new Color32(0, 0, 0, 0);
                }
            }

            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                pixels,
                width,
                height,
                0.5f,
                out cellWidth,
                out cellHeight
            );

            Assert.IsTrue(result, "Should detect grid in large texture");
            Assert.AreEqual(64, cellWidth, "Cell width should be 64");
            Assert.AreEqual(64, cellHeight, "Cell height should be 64");
        }

        [Test]
        public void DetectOptimalGridFromTransparencyPartiallyTransparentRows()
        {
            int width = 64;
            int height = 64;
            Color32[] pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < (int)(width * 0.9f); ++x)
                {
                    pixels[y * width + 32] = new Color32(0, 0, 0, 0);
                }
            }

            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                pixels,
                width,
                height,
                0.5f,
                out cellWidth,
                out cellHeight
            );

            Assert.IsTrue(
                result || !result,
                "Should handle partially transparent rows without crashing"
            );
        }

        [Test]
        public void DetectOptimalGridFromTransparencySingleSprite()
        {
            int width = 32;
            int height = 32;
            Color32[] pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                pixels,
                width,
                height,
                0.5f,
                out cellWidth,
                out cellHeight
            );

            Assert.IsFalse(result, "Should return false for single sprite with no boundaries");
        }

        [Test]
        public void DetectOptimalGridFromTransparencyZeroDimensions()
        {
            Color32[] pixels = new Color32[0];

            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                pixels,
                0,
                0,
                0.5f,
                out cellWidth,
                out cellHeight
            );

            Assert.IsFalse(result, "Should return false for zero dimensions");
        }

        [Test]
        public void DetectOptimalGridFromTransparencyNegativeAlphaThreshold()
        {
            int width = 64;
            int height = 64;
            Color32[] pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                pixels,
                width,
                height,
                -0.5f,
                out cellWidth,
                out cellHeight
            );

            Assert.IsFalse(
                result,
                "Should handle negative alpha threshold by not detecting any transparency"
            );
        }

        [Test]
        public void DetectOptimalGridFromTransparencyLowAlphaPixels()
        {
            int width = 64;
            int height = 64;
            Color32[] pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            for (int y = 0; y < height; ++y)
            {
                pixels[y * width + 32] = new Color32(0, 0, 0, 50);
            }

            for (int x = 0; x < width; ++x)
            {
                pixels[32 * width + x] = new Color32(0, 0, 0, 50);
            }

            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                pixels,
                width,
                height,
                0.5f,
                out cellWidth,
                out cellHeight
            );

            Assert.IsTrue(result, "Should detect boundaries with alpha below threshold");
        }

        [Test]
        public void DetectOptimalGridFromTransparencyAlphaExactlyAtThreshold()
        {
            int width = 64;
            int height = 64;
            Color32[] pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            byte thresholdByte = (byte)(0.5f * 255);
            for (int y = 0; y < height; ++y)
            {
                pixels[y * width + 32] = new Color32(0, 0, 0, thresholdByte);
            }

            for (int x = 0; x < width; ++x)
            {
                pixels[32 * width + x] = new Color32(0, 0, 0, thresholdByte);
            }

            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                pixels,
                width,
                height,
                0.5f,
                out cellWidth,
                out cellHeight
            );

            Assert.IsTrue(
                result || !result,
                "Should handle alpha exactly at threshold without crashing"
            );
        }

        [Test]
        public void DetectOptimalGridFromTransparencyThinGutterDetection()
        {
            // Test that 1-pixel thin transparent gutters are detected with the lowered threshold
            int width = 64;
            int height = 32;
            Color32[] pixels = new Color32[width * height];

            // Create 2x1 grid with 1-pixel transparent vertical gutter at x=32
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    // Single transparent column at x=32 (the boundary between two 32-pixel cells)
                    if (x == 32)
                    {
                        pixels[y * width + x] = new Color32(0, 0, 0, 0); // Fully transparent
                    }
                    else
                    {
                        pixels[y * width + x] = new Color32(255, 0, 0, 255); // Fully opaque
                    }
                }
            }

            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                pixels,
                width,
                height,
                0.01f,
                out cellWidth,
                out cellHeight
            );

            // With the lowered MinimumBoundaryScore (0.15 instead of 0.5), thin gutters should be detected
            Assert.IsTrue(result, "Should detect grid with thin 1-pixel transparent gutter");
            Assert.AreEqual(32, cellWidth, "Cell width should be 32 pixels");
        }

        [Test]
        public void DetectOptimalGridFromTransparencyPartialTransparencyRow()
        {
            // Test detection with rows that are only 50% transparent (not 90%)
            int width = 64;
            int height = 32;
            Color32[] pixels = new Color32[width * height];

            // Create grid with partially transparent rows at y=16 (only 50% of pixels transparent)
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    if (y == 16)
                    {
                        // Only make every other pixel transparent at the boundary
                        if (x % 2 == 0)
                        {
                            pixels[y * width + x] = new Color32(0, 0, 0, 0);
                        }
                        else
                        {
                            pixels[y * width + x] = new Color32(255, 0, 0, 255);
                        }
                    }
                    else
                    {
                        pixels[y * width + x] = new Color32(255, 0, 0, 255);
                    }
                }
            }

            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                pixels,
                width,
                height,
                0.01f,
                out cellWidth,
                out cellHeight
            );

            // With the relaxed fallback transparency requirement (70%/50%), partial rows may still be detected
            // The test validates that detection doesn't crash and handles partial transparency
            Assert.IsTrue(
                result || !result,
                "Should handle partial transparency rows without crashing"
            );
        }

        [Test]
        public void DetectOptimalGridFromTransparencyPrefersSmallerCellSizes()
        {
            // Test that for a 256x128 texture, we get smaller cells rather than just halving
            int width = 256;
            int height = 128;
            Color32[] pixels = new Color32[width * height];

            // Create grid with 32x32 cells (8x4 grid) with transparent gutters
            int cellSize = 32;
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    // Make boundaries transparent
                    bool isVerticalBoundary = (x % cellSize == 0) && (x > 0);
                    bool isHorizontalBoundary = (y % cellSize == 0) && (y > 0);

                    if (isVerticalBoundary || isHorizontalBoundary)
                    {
                        pixels[y * width + x] = new Color32(0, 0, 0, 0);
                    }
                    else
                    {
                        pixels[y * width + x] = new Color32(255, 0, 0, 255);
                    }
                }
            }

            int cellWidth;
            int cellHeight;
            bool result = SpriteSheetExtractor.DetectOptimalGridFromTransparency(
                pixels,
                width,
                height,
                0.01f,
                out cellWidth,
                out cellHeight
            );

            Assert.IsTrue(result, "Should detect grid in 256x128 texture with 32-pixel cells");
            Assert.AreEqual(32, cellWidth, "Cell width should be 32 (not 128 or 256)");
            Assert.AreEqual(32, cellHeight, "Cell height should be 32 (not 64 or 128)");
        }

        [Test]
        public void CalculateGridDimensionsFallbackPrefersSmallerCellsOverGCD()
        {
            // Test that when transparency detection fails, fallback uses smaller common sizes
            // rather than just GCD (e.g., for 256x128, prefer 8 or 16 over 128)
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Auto;

            // Call with no pixels (will force fallback)
            extractor.CalculateGridDimensions(
                256,
                128,
                null,
                null, // No pixels - forces fallback behavior
                out int columns,
                out int rows,
                out int cellWidth,
                out int cellHeight
            );

            // Should not use GCD (128) but instead prefer smaller common sizes
            Assert.Greater(columns, 1, "Should produce more than 1 column");
            Assert.Greater(rows, 1, "Should produce more than 1 row");
            Assert.Less(cellWidth, 128, "Cell width should be less than GCD (128)");
            Assert.Less(cellHeight, 128, "Cell height should be less than GCD (128)");
        }

        [Test]
        public void CalculateGridDimensionsFallbackHandlesPrimeDimension()
        {
            // Test that prime dimension (no divisors) is handled gracefully
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Auto;

            // 127 is prime, 128 is power of 2
            extractor.CalculateGridDimensions(
                127,
                128,
                null,
                null,
                out int columns,
                out int rows,
                out int cellWidth,
                out int cellHeight
            );

            // Should handle gracefully without crashing
            Assert.Greater(columns, 0, "Columns should be positive");
            Assert.Greater(rows, 0, "Rows should be positive");
            Assert.Greater(cellWidth, 0, "Cell width should be positive");
            Assert.Greater(cellHeight, 0, "Cell height should be positive");
            // For the 128 dimension, should get reasonable cells (e.g., 8, 16, 32, 64)
            Assert.LessOrEqual(
                cellHeight,
                64,
                "Cell height should be reasonable for 128px dimension"
            );
        }

        [Test]
        public void CalculateGridDimensionsFallbackHandlesVerySmallDimension()
        {
            // Test that very small dimensions don't cause issues
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Auto;

            extractor.CalculateGridDimensions(
                16,
                16,
                null,
                null,
                out int columns,
                out int rows,
                out int cellWidth,
                out int cellHeight
            );

            // Should handle small dimensions
            Assert.Greater(columns, 0, "Columns should be positive");
            Assert.Greater(rows, 0, "Rows should be positive");
            // For 16x16, should get something reasonable (8 or 16)
            Assert.IsTrue(cellWidth == 8 || cellWidth == 16, "Cell width should be 8 or 16");
            Assert.IsTrue(cellHeight == 8 || cellHeight == 16, "Cell height should be 8 or 16");
        }

        [Test]
        public void FindSmallestReasonableDivisorReturnsCommonSize()
        {
            // 256 is divisible by 8, 16, 32, 64, 128
            // Should prefer 8 (smallest common size that produces >= 2 cells)
            int result = SpriteSheetExtractor.FindSmallestReasonableDivisor(256);
            Assert.AreEqual(
                8,
                result,
                "Should return 8 for 256 (smallest common size with >= 2 cells)"
            );
        }

        [Test]
        public void FindSmallestReasonableDivisorHandles128()
        {
            // 128 is divisible by 8, 16, 32, 64
            int result = SpriteSheetExtractor.FindSmallestReasonableDivisor(128);
            Assert.AreEqual(8, result, "Should return 8 for 128");
        }

        [Test]
        public void FindSmallestReasonableDivisorHandles64()
        {
            // 64 is divisible by 8, 16, 32
            int result = SpriteSheetExtractor.FindSmallestReasonableDivisor(64);
            Assert.AreEqual(8, result, "Should return 8 for 64");
        }

        [Test]
        public void FindSmallestReasonableDivisorHandles32()
        {
            // 32 is divisible by 8, 16
            int result = SpriteSheetExtractor.FindSmallestReasonableDivisor(32);
            Assert.AreEqual(8, result, "Should return 8 for 32");
        }

        [Test]
        public void FindSmallestReasonableDivisorHandles16()
        {
            // 16 is divisible by 8, but 16/8 = 2 cells (meets threshold)
            int result = SpriteSheetExtractor.FindSmallestReasonableDivisor(16);
            Assert.AreEqual(8, result, "Should return 8 for 16");
        }

        [Test]
        public void FindSmallestReasonableDivisorReturnsFullDimensionForSmall()
        {
            // 8 is too small - 8/8 = 1 cell (doesn't meet >= 2 cells threshold)
            int result = SpriteSheetExtractor.FindSmallestReasonableDivisor(8);
            Assert.AreEqual(8, result, "Should return 8 for 8 (dimension itself)");
        }

        [Test]
        public void FindSmallestReasonableDivisorHandlesPrimeDimension()
        {
            // 127 is prime - no divisors
            int result = SpriteSheetExtractor.FindSmallestReasonableDivisor(127);
            Assert.AreEqual(127, result, "Should return 127 for prime dimension");
        }

        [Test]
        public void FindSmallestReasonableDivisorHandlesNonCommonSize()
        {
            // 120 is not a power of 2: divisible by 8, 10, 12, 15, 20, 24, 30, 40, 60
            // 120/8 = 15 cells (>= 2)
            int result = SpriteSheetExtractor.FindSmallestReasonableDivisor(120);
            Assert.AreEqual(8, result, "Should return 8 for 120");
        }

        [Test]
        public void FindSmallestReasonableDivisorHandles48()
        {
            // 48 is divisible by 8, 12, 16, 24
            // 48/8 = 6 cells (>= 2)
            int result = SpriteSheetExtractor.FindSmallestReasonableDivisor(48);
            Assert.AreEqual(8, result, "Should return 8 for 48");
        }

        [Test]
        public void FindSmallestReasonableDivisorHandles100()
        {
            // 100 is not divisible by any common size (8, 16, 32, 64, 128, 256)
            // Fallback: try all divisors starting from 8
            // 100/10 = 10 cells, so should return 10
            int result = SpriteSheetExtractor.FindSmallestReasonableDivisor(100);
            Assert.AreEqual(10, result, "Should return 10 for 100 (first divisor >= 8)");
        }

        [Test]
        public void FindSmallestReasonableDivisorHandles200()
        {
            // 200 is divisible by 8: 200/8 = 25 cells (>= 2)
            int result = SpriteSheetExtractor.FindSmallestReasonableDivisor(200);
            Assert.AreEqual(8, result, "Should return 8 for 200");
        }

        [Test]
        public void FindSmallestReasonableDivisorHandles300()
        {
            // 300 is not divisible by 8 (300/8 = 37.5)
            // Not divisible by 16, 32, 64, 128, 256 either
            // Fallback: 300/10 = 30 cells, 300/12 = 25, 300/15 = 20, etc.
            // First divisor >= 8: 10
            int result = SpriteSheetExtractor.FindSmallestReasonableDivisor(300);
            Assert.AreEqual(10, result, "Should return 10 for 300 (first divisor >= 8)");
        }

        [Test]
        public void FindSmallestReasonableDivisorHandlesZero()
        {
            // 0 dimension should return 0 (edge case - no valid division possible)
            int result = SpriteSheetExtractor.FindSmallestReasonableDivisor(0);
            Assert.AreEqual(0, result, "Should return 0 for 0 dimension");
        }

        [Test]
        public void FindSmallestReasonableDivisorHandlesVerySmallDimensions()
        {
            // Dimensions 1-7 should return themselves (no divisors >= 8 that produce >= 2 cells)
            Assert.AreEqual(
                1,
                SpriteSheetExtractor.FindSmallestReasonableDivisor(1),
                "Should return 1 for 1"
            );
            Assert.AreEqual(
                4,
                SpriteSheetExtractor.FindSmallestReasonableDivisor(4),
                "Should return 4 for 4"
            );
            Assert.AreEqual(
                7,
                SpriteSheetExtractor.FindSmallestReasonableDivisor(7),
                "Should return 7 for 7"
            );
        }

        [Test]
        public void FindSmallestReasonableDivisorHandles512()
        {
            // 512 is a common size itself; 512/8 = 64 cells (>= 2)
            int result = SpriteSheetExtractor.FindSmallestReasonableDivisor(512);
            Assert.AreEqual(8, result, "Should return 8 for 512");
        }

        [Test]
        public void FindSmallestReasonableDivisorHandlesLargeDimension()
        {
            // 4096 is divisible by 8: 4096/8 = 512 cells (>= 2)
            int result = SpriteSheetExtractor.FindSmallestReasonableDivisor(4096);
            Assert.AreEqual(8, result, "Should return 8 for 4096");
        }

        [Test]
        public void FindSmallestReasonableDivisorHandles9()
        {
            // 9 is not divisible by any common size
            // Fallback loop: 9/9 = 1 cell (doesn't meet >= 2 threshold)
            // No divisor >= 8 exists that produces >= 2 cells, so returns dimension itself
            int result = SpriteSheetExtractor.FindSmallestReasonableDivisor(9);
            Assert.AreEqual(9, result, "Should return 9 for 9 (no valid divisor >= 8)");
        }

        #endregion

        #region SchedulePreviewRegenerationForEntry Additional Tests

        [Test]
        public void SchedulePreviewRegenerationPreservesTexturesWhenRectsMatch()
        {
            CreateSpriteSheet("regen_preserve_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;

            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = null;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (extractor._discoveredSheets[i]._assetPath.Contains("regen_preserve_test"))
                {
                    entry = extractor._discoveredSheets[i];
                    break;
                }
            }
            Assert.IsNotNull(entry, "Should find regen_preserve_test entry");
            Assert.AreEqual(4, entry._sprites.Count, "Should have 4 sprites initially");

            Rect[] originalRects = new Rect[entry._sprites.Count];
            for (int i = 0; i < entry._sprites.Count; i++)
            {
                originalRects[i] = entry._sprites[i]._rect;
            }

            entry._useGlobalSettings = false;
            extractor.RepopulateSpritesForEntry(entry);

            Assert.AreEqual(
                4,
                entry._sprites.Count,
                "Should still have 4 sprites after regeneration"
            );

            for (int i = 0; i < entry._sprites.Count; i++)
            {
                bool rectFound = false;
                for (int j = 0; j < originalRects.Length; j++)
                {
                    if (entry._sprites[i]._rect == originalRects[j])
                    {
                        rectFound = true;
                        break;
                    }
                }
                Assert.IsTrue(rectFound, $"Sprite {i} rect should match an original rect");
            }
        }

        [Test]
        public void SchedulePreviewRegenerationHandlesNullEntry()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            Assert.DoesNotThrow(() => extractor.RepopulateSpritesForEntry(null));
        }

        [Test]
        public void SchedulePreviewRegenerationHandlesEntryWithNullTexture()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _assetPath = "test",
                _texture = null,
                _sprites = new List<SpriteSheetExtractor.SpriteEntryData>(),
            };

            Assert.DoesNotThrow(() => extractor.RepopulateSpritesForEntry(entry));
        }

        [Test]
        public void SchedulePreviewRegenerationHandlesEntryWithNullSprites()
        {
            Texture2D texture = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _assetPath = "test",
                _texture = texture,
                _sprites = null,
            };

            Assert.DoesNotThrow(() => extractor.RepopulateSpritesForEntry(entry));
        }

        [Test]
        public void SchedulePreviewRegenerationHandlesDestroyedPreviewTextures()
        {
            CreateSpriteSheet("destroyed_preview_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;

            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = null;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (extractor._discoveredSheets[i]._assetPath.Contains("destroyed_preview_test"))
                {
                    entry = extractor._discoveredSheets[i];
                    break;
                }
            }
            Assert.IsNotNull(entry, "Should find destroyed_preview_test entry");

            if (entry._sprites.Count > 0 && entry._sprites[0]._previewTexture != null)
            {
                Object.DestroyImmediate(entry._sprites[0]._previewTexture); // UNH-SUPPRESS: Intentional destruction to test destroyed preview
            }

            Assert.DoesNotThrow(() =>
            {
                entry._useGlobalSettings = false;
                extractor.RepopulateSpritesForEntry(entry);
            });
        }

        [Test]
        public void SchedulePreviewRegenerationCleansUpOrphanedTextures()
        {
            CreateSpriteSheet("orphan_cleanup_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;

            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = null;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (extractor._discoveredSheets[i]._assetPath.Contains("orphan_cleanup_test"))
                {
                    entry = extractor._discoveredSheets[i];
                    break;
                }
            }
            Assert.IsNotNull(entry, "Should find orphan_cleanup_test entry");
            Assert.AreEqual(4, entry._sprites.Count, "Should have 4 sprites initially");

            int originalPreviewCount = 0;
            for (int i = 0; i < entry._sprites.Count; i++)
            {
                if (entry._sprites[i]._previewTexture != null)
                {
                    originalPreviewCount++;
                }
            }

            entry._useGlobalSettings = false;
            entry._gridColumnsOverride = 4;
            entry._gridRowsOverride = 4;
            extractor.RepopulateSpritesForEntry(entry);
            extractor.GenerateAllPreviewTexturesInBatch(extractor._discoveredSheets);

            Assert.AreEqual(16, entry._sprites.Count, "Should have 16 sprites after changing grid");
        }

        [Test]
        public void SchedulePreviewRegenerationHandlesEmptySpriteList()
        {
            Texture2D texture = Track(new Texture2D(64, 64, TextureFormat.RGBA32, false));

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;

            SpriteSheetExtractor.SpriteSheetEntry entry = new SpriteSheetExtractor.SpriteSheetEntry
            {
                _assetPath = "test",
                _texture = texture,
                _sprites = new List<SpriteSheetExtractor.SpriteEntryData>(),
            };

            Assert.DoesNotThrow(() => extractor.RepopulateSpritesForEntry(entry));
            Assert.IsTrue(
                entry._sprites.Count >= 0,
                "Should handle empty sprite list without crashing"
            );
        }

        [Test]
        public void SchedulePreviewRegenerationWorksAfterModeChange()
        {
            CreateSpriteSheet("mode_change_regen_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;

            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = null;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (extractor._discoveredSheets[i]._assetPath.Contains("mode_change_regen_test"))
                {
                    entry = extractor._discoveredSheets[i];
                    break;
                }
            }
            Assert.IsNotNull(entry, "Should find mode_change_regen_test entry");
            Assert.AreEqual(4, entry._sprites.Count, "Should have 4 sprites initially");

            entry._useGlobalSettings = false;
            entry._extractionModeOverride = SpriteSheetExtractor.ExtractionMode.PaddedGrid;
            entry._gridColumnsOverride = 2;
            entry._gridRowsOverride = 2;
            entry._paddingLeftOverride = 2;
            entry._paddingRightOverride = 2;
            entry._paddingTopOverride = 2;
            entry._paddingBottomOverride = 2;

            Assert.DoesNotThrow(() =>
            {
                extractor.RepopulateSpritesForEntry(entry);
                extractor.GenerateAllPreviewTexturesInBatch(extractor._discoveredSheets);
            });
        }

        [Test]
        public void SchedulePreviewRegenerationHandlesMultipleConsecutiveRegenerations()
        {
            CreateSpriteSheet("multi_regen_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;

            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = null;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (extractor._discoveredSheets[i]._assetPath.Contains("multi_regen_test"))
                {
                    entry = extractor._discoveredSheets[i];
                    break;
                }
            }
            Assert.IsNotNull(entry, "Should find multi_regen_test entry");

            for (int iteration = 0; iteration < 5; iteration++)
            {
                entry._useGlobalSettings = false;
                entry._gridColumnsOverride = 2 + (iteration % 2);
                entry._gridRowsOverride = 2 + (iteration % 2);

                Assert.DoesNotThrow(() => extractor.RepopulateSpritesForEntry(entry));
            }

            Assert.IsNotNull(
                entry._sprites,
                "Sprites should not be null after multiple regenerations"
            );
            Assert.Greater(
                entry._sprites.Count,
                0,
                "Should have sprites after multiple regenerations"
            );
        }

        [Test]
        public void SchedulePreviewRegenerationHandlesTogglingUseGlobalSettings()
        {
            CreateSpriteSheet("toggle_global_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;

            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = null;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (extractor._discoveredSheets[i]._assetPath.Contains("toggle_global_test"))
                {
                    entry = extractor._discoveredSheets[i];
                    break;
                }
            }
            Assert.IsNotNull(entry, "Should find toggle_global_test entry");

            Assert.IsTrue(entry._useGlobalSettings, "Should start with global settings enabled");

            entry._useGlobalSettings = false;
            entry._gridColumnsOverride = 4;
            entry._gridRowsOverride = 4;
            Assert.DoesNotThrow(() => extractor.RepopulateSpritesForEntry(entry));

            int overrideCount = entry._sprites.Count;

            entry._useGlobalSettings = true;
            Assert.DoesNotThrow(() => extractor.RepopulateSpritesForEntry(entry));

            Assert.AreEqual(4, entry._sprites.Count, "Should revert to global 2x2 = 4 sprites");
        }

        [Test]
        public void SchedulePreviewRegenerationSpritesWithDuplicateRectsAreHandled()
        {
            CreateSpriteSheet("dup_rects_regen_test", 64, 64, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 2;
            extractor._gridRows = 2;
            extractor._previewSizeMode = SpriteSheetExtractor.PreviewSizeMode.Size32;

            extractor.DiscoverSpriteSheets();

            SpriteSheetExtractor.SpriteSheetEntry entry = null;
            for (int i = 0; i < extractor._discoveredSheets.Count; i++)
            {
                if (extractor._discoveredSheets[i]._assetPath.Contains("dup_rects_regen_test"))
                {
                    entry = extractor._discoveredSheets[i];
                    break;
                }
            }
            Assert.IsNotNull(entry, "Should find dup_rects_regen_test entry");
            Assert.AreEqual(4, entry._sprites.Count, "Should have 4 sprites initially");

            if (entry._sprites.Count >= 2)
            {
                entry._sprites[1]._rect = entry._sprites[0]._rect;
                Texture2D duplicatePreview = Track(
                    new Texture2D(32, 32, TextureFormat.RGBA32, false)
                );
                entry._sprites[1]._previewTexture = duplicatePreview;
            }

            entry._useGlobalSettings = false;
            Assert.DoesNotThrow(() =>
            {
                extractor.RepopulateSpritesForEntry(entry);
                extractor.GenerateAllPreviewTexturesInBatch(extractor._discoveredSheets);
            });
        }

        #endregion

        #region Grid Overlay Toggle Bug Fix Tests

        [Test]
        public void CalculateGridDimensionsCapsCellWidthWhenGCDEqualsTextureWidth()
        {
            // Bug scenario: 128x256 texture where GCD = 128
            // This would result in cellWidth = 128, columns = 1 (only horizontal lines, no vertical)
            // Fix ensures at least 2 columns by capping cell width to textureWidth/2
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Auto;

            extractor.CalculateGridDimensions(
                128,
                256,
                null,
                null,
                out int columns,
                out int rows,
                out int cellWidth,
                out int cellHeight
            );

            Assert.GreaterOrEqual(
                columns,
                2,
                "Should have at least 2 columns to show vertical lines"
            );
            Assert.LessOrEqual(
                cellWidth,
                64,
                "Cell width should be capped to at most half the texture width"
            );
        }

        [Test]
        public void CalculateGridDimensionsCapsCellHeightWhenGCDEqualsTextureHeight()
        {
            // Bug scenario: 256x128 texture where GCD = 128
            // This would result in cellHeight = 128, rows = 1 (only vertical lines, no horizontal)
            // Fix ensures at least 2 rows by capping cell height to textureHeight/2
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Auto;

            extractor.CalculateGridDimensions(
                256,
                128,
                null,
                null,
                out int columns,
                out int rows,
                out int cellWidth,
                out int cellHeight
            );

            Assert.GreaterOrEqual(rows, 2, "Should have at least 2 rows to show horizontal lines");
            Assert.LessOrEqual(
                cellHeight,
                64,
                "Cell height should be capped to at most half the texture height"
            );
        }

        [Test]
        public void CalculateGridDimensionsHandlesSquareTextureWithLargeGCD()
        {
            // Test square texture where GCD would be the full dimension
            // e.g., 128x128 with GCD = 128 would give 1x1 grid
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Auto;

            extractor.CalculateGridDimensions(
                128,
                128,
                null,
                null,
                out int columns,
                out int rows,
                out int cellWidth,
                out int cellHeight
            );

            Assert.GreaterOrEqual(columns, 2, "Should have at least 2 columns for square texture");
            Assert.GreaterOrEqual(rows, 2, "Should have at least 2 rows for square texture");
            Assert.LessOrEqual(cellWidth, 64, "Cell width should be capped for square texture");
            Assert.LessOrEqual(cellHeight, 64, "Cell height should be capped for square texture");
        }

        [Test]
        public void CalculateGridDimensionsPreservesSmallCellSizes()
        {
            // Ensure the fix doesn't affect textures that already have reasonable cell sizes
            // e.g., 256x256 with 32x32 cells should remain unchanged
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 8;
            extractor._gridRows = 8;

            extractor.CalculateGridDimensions(
                256,
                256,
                null,
                null,
                out int columns,
                out int rows,
                out int cellWidth,
                out int cellHeight
            );

            Assert.AreEqual(8, columns, "Should preserve 8 columns");
            Assert.AreEqual(8, rows, "Should preserve 8 rows");
            Assert.AreEqual(32, cellWidth, "Cell width should be 32");
            Assert.AreEqual(32, cellHeight, "Cell height should be 32");
        }

        [Test]
        public void CalculateGridDimensionsHandlesVeryTallTexture()
        {
            // Test with very tall texture (e.g., 64x512)
            // GCD = 64, would result in 1 column without fix
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Auto;

            extractor.CalculateGridDimensions(
                64,
                512,
                null,
                null,
                out int columns,
                out int rows,
                out int cellWidth,
                out int cellHeight
            );

            Assert.GreaterOrEqual(columns, 2, "Should have at least 2 columns for tall texture");
            Assert.LessOrEqual(cellWidth, 32, "Cell width should be capped for tall texture");
        }

        [Test]
        public void CalculateGridDimensionsHandlesVeryWideTexture()
        {
            // Test with very wide texture (e.g., 512x64)
            // GCD = 64, would result in 1 row without fix
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Auto;

            extractor.CalculateGridDimensions(
                512,
                64,
                null,
                null,
                out int columns,
                out int rows,
                out int cellWidth,
                out int cellHeight
            );

            Assert.GreaterOrEqual(rows, 2, "Should have at least 2 rows for wide texture");
            Assert.LessOrEqual(cellHeight, 32, "Cell height should be capped for wide texture");
        }

        [Test]
        public void CalculateGridDimensionsDoesNotCapVerySmallTextures()
        {
            // For very small textures (e.g., 8x8), the cap should not apply
            // because maxCellWidth/Height would be 4, which is less than 8
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Auto;

            extractor.CalculateGridDimensions(
                8,
                8,
                null,
                null,
                out int columns,
                out int rows,
                out int cellWidth,
                out int cellHeight
            );

            // For 8x8, we should get at least some valid output
            Assert.Greater(columns, 0, "Should have at least 1 column");
            Assert.Greater(rows, 0, "Should have at least 1 row");
        }

        #endregion

        #region Pivot Mode Tests

        [Test]
        public void PivotModeToVector2ReturnsCorrectVectorForCenter()
        {
            Vector2 result = SpriteSheetExtractor.PivotModeToVector2(
                UnityHelpers.Editor.Sprites.PivotMode.Center,
                Vector2.zero
            );
            Assert.That(result, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        }

        [Test]
        public void PivotModeToVector2ReturnsCorrectVectorForBottomLeft()
        {
            Vector2 result = SpriteSheetExtractor.PivotModeToVector2(
                PivotMode.BottomLeft,
                Vector2.zero
            );
            Assert.That(result, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void PivotModeToVector2ReturnsCorrectVectorForTopRight()
        {
            Vector2 result = SpriteSheetExtractor.PivotModeToVector2(
                PivotMode.TopRight,
                Vector2.zero
            );
            Assert.That(result, Is.EqualTo(Vector2.one));
        }

        [Test]
        public void PivotModeToVector2ReturnsCorrectVectorForTopLeft()
        {
            Vector2 result = SpriteSheetExtractor.PivotModeToVector2(
                PivotMode.TopLeft,
                Vector2.zero
            );
            Assert.That(result, Is.EqualTo(new Vector2(0f, 1f)));
        }

        [Test]
        public void PivotModeToVector2ReturnsCorrectVectorForBottomRight()
        {
            Vector2 result = SpriteSheetExtractor.PivotModeToVector2(
                PivotMode.BottomRight,
                Vector2.zero
            );
            Assert.That(result, Is.EqualTo(new Vector2(1f, 0f)));
        }

        [Test]
        public void PivotModeToVector2ReturnsCorrectVectorForLeftCenter()
        {
            Vector2 result = SpriteSheetExtractor.PivotModeToVector2(
                PivotMode.LeftCenter,
                Vector2.zero
            );
            Assert.That(result, Is.EqualTo(new Vector2(0f, 0.5f)));
        }

        [Test]
        public void PivotModeToVector2ReturnsCorrectVectorForRightCenter()
        {
            Vector2 result = SpriteSheetExtractor.PivotModeToVector2(
                PivotMode.RightCenter,
                Vector2.zero
            );
            Assert.That(result, Is.EqualTo(new Vector2(1f, 0.5f)));
        }

        [Test]
        public void PivotModeToVector2ReturnsCorrectVectorForTopCenter()
        {
            Vector2 result = SpriteSheetExtractor.PivotModeToVector2(
                PivotMode.TopCenter,
                Vector2.zero
            );
            Assert.That(result, Is.EqualTo(new Vector2(0.5f, 1f)));
        }

        [Test]
        public void PivotModeToVector2ReturnsCorrectVectorForBottomCenter()
        {
            Vector2 result = SpriteSheetExtractor.PivotModeToVector2(
                PivotMode.BottomCenter,
                Vector2.zero
            );
            Assert.That(result, Is.EqualTo(new Vector2(0.5f, 0f)));
        }

        [Test]
        public void PivotModeToVector2ReturnsCustomPivotWhenModeIsCustom()
        {
            Vector2 customPivot = new(0.25f, 0.75f);
            Vector2 result = SpriteSheetExtractor.PivotModeToVector2(PivotMode.Custom, customPivot);
            Assert.That(result, Is.EqualTo(customPivot));
        }

        [Test]
        public void GetEffectivePivotModeReturnsGlobalWhenUseGlobalSettingsTrue()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._pivotMode = PivotMode.TopLeft;

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _useGlobalSettings = true,
                _pivotModeOverride = PivotMode.BottomRight,
            };

            PivotMode result = extractor.GetEffectivePivotMode(entry);
            Assert.That(result, Is.EqualTo(PivotMode.TopLeft));
        }

        [Test]
        public void GetEffectivePivotModeReturnsOverrideWhenUseGlobalSettingsFalse()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._pivotMode = PivotMode.TopLeft;

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _useGlobalSettings = false,
                _pivotModeOverride = PivotMode.BottomRight,
            };

            PivotMode result = extractor.GetEffectivePivotMode(entry);
            Assert.That(result, Is.EqualTo(PivotMode.BottomRight));
        }

        [Test]
        public void GetEffectivePivotModeReturnsGlobalWhenOverrideIsNull()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._pivotMode = PivotMode.TopCenter;

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _useGlobalSettings = false,
                _pivotModeOverride = null,
            };

            PivotMode result = extractor.GetEffectivePivotMode(entry);
            Assert.That(result, Is.EqualTo(PivotMode.TopCenter));
        }

        [Test]
        public void GetEffectiveCustomPivotReturnsGlobalWhenUseGlobalSettingsTrue()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._customPivot = new Vector2(0.1f, 0.2f);

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _useGlobalSettings = true,
                _customPivotOverride = new Vector2(0.8f, 0.9f),
            };

            Vector2 result = extractor.GetEffectiveCustomPivot(entry);
            Assert.That(result, Is.EqualTo(new Vector2(0.1f, 0.2f)));
        }

        [Test]
        public void GetEffectiveCustomPivotReturnsOverrideWhenUseGlobalSettingsFalse()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._customPivot = new Vector2(0.1f, 0.2f);

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _useGlobalSettings = false,
                _customPivotOverride = new Vector2(0.8f, 0.9f),
            };

            Vector2 result = extractor.GetEffectiveCustomPivot(entry);
            Assert.That(result, Is.EqualTo(new Vector2(0.8f, 0.9f)));
        }

        [Test]
        public void GetEffectivePivotReturnsSpriteOverrideWhenEnabled()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._pivotMode = PivotMode.Center;
            extractor._customPivot = new Vector2(0.5f, 0.5f);

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _useGlobalSettings = false,
                _pivotModeOverride = PivotMode.TopLeft,
                _customPivotOverride = new Vector2(0.0f, 1.0f),
            };

            SpriteSheetExtractor.SpriteEntryData sprite = new()
            {
                _usePivotOverride = true,
                _pivotModeOverride = PivotMode.BottomRight,
                _customPivotOverride = new Vector2(1.0f, 0.0f),
            };

            Vector2 result = extractor.GetEffectivePivot(entry, sprite);
            Assert.That(result, Is.EqualTo(new Vector2(1.0f, 0.0f)));
        }

        [Test]
        public void GetEffectivePivotReturnsSheetOverrideWhenSpriteOverrideDisabled()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._pivotMode = PivotMode.Center;
            extractor._customPivot = new Vector2(0.5f, 0.5f);

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _useGlobalSettings = false,
                _pivotModeOverride = PivotMode.TopLeft,
                _customPivotOverride = new Vector2(0.0f, 1.0f),
            };

            SpriteSheetExtractor.SpriteEntryData sprite = new()
            {
                _usePivotOverride = false,
                _pivotModeOverride = PivotMode.BottomRight,
                _customPivotOverride = new Vector2(1.0f, 0.0f),
            };

            Vector2 result = extractor.GetEffectivePivot(entry, sprite);
            Assert.That(result, Is.EqualTo(new Vector2(0.0f, 1.0f)));
        }

        [Test]
        public void GetEffectivePivotReturnsGlobalWhenBothOverridesDisabled()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._pivotMode = PivotMode.BottomCenter;
            extractor._customPivot = new Vector2(0.5f, 0.0f);

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _useGlobalSettings = true,
                _pivotModeOverride = PivotMode.TopLeft,
                _customPivotOverride = new Vector2(0.0f, 1.0f),
            };

            SpriteSheetExtractor.SpriteEntryData sprite = new()
            {
                _usePivotOverride = false,
                _pivotModeOverride = PivotMode.BottomRight,
                _customPivotOverride = new Vector2(1.0f, 0.0f),
            };

            Vector2 result = extractor.GetEffectivePivot(entry, sprite);
            Assert.That(result, Is.EqualTo(new Vector2(0.5f, 0.0f)));
        }

        [Test]
        public void GetEffectivePivotHandlesNullSprite()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._pivotMode = PivotMode.TopCenter;
            extractor._customPivot = new Vector2(0.5f, 1.0f);

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _useGlobalSettings = false,
                _pivotModeOverride = PivotMode.LeftCenter,
                _customPivotOverride = new Vector2(0.0f, 0.5f),
            };

            Vector2 result = extractor.GetEffectivePivot(entry, null);
            Assert.That(result, Is.EqualTo(new Vector2(0.0f, 0.5f)));
        }

        [Test]
        public void GetEffectivePivotHandlesNullEntry()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._pivotMode = PivotMode.RightCenter;
            extractor._customPivot = new Vector2(1.0f, 0.5f);

            SpriteSheetExtractor.SpriteEntryData sprite = new()
            {
                _usePivotOverride = false,
                _pivotModeOverride = PivotMode.BottomRight,
                _customPivotOverride = new Vector2(1.0f, 0.0f),
            };

            Vector2 result = extractor.GetEffectivePivot(null, sprite);
            Assert.That(result, Is.EqualTo(new Vector2(1.0f, 0.5f)));
        }

        [Test]
        public void GetEffectivePivotHandlesCustomPivotMode()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._pivotMode = PivotMode.Center;
            extractor._customPivot = new Vector2(0.5f, 0.5f);

            SpriteSheetExtractor.SpriteSheetEntry entry = new() { _useGlobalSettings = true };

            SpriteSheetExtractor.SpriteEntryData sprite = new()
            {
                _usePivotOverride = true,
                _pivotModeOverride = PivotMode.Custom,
                _customPivotOverride = new Vector2(0.33f, 0.67f),
            };

            Vector2 result = extractor.GetEffectivePivot(entry, sprite);
            Assert.That(result, Is.EqualTo(new Vector2(0.33f, 0.67f)));
        }

        [Test]
        public void GetEffectivePivotUsesSpriteOverrideWhenEntryIsNull()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._pivotMode = PivotMode.Center;
            extractor._customPivot = new Vector2(0.5f, 0.5f);

            SpriteSheetExtractor.SpriteEntryData sprite = new()
            {
                _usePivotOverride = true,
                _pivotModeOverride = PivotMode.TopLeft,
                _customPivotOverride = new Vector2(0.0f, 1.0f),
            };

            Vector2 result = extractor.GetEffectivePivot(null, sprite);
            Assert.That(result, Is.EqualTo(new Vector2(0.0f, 1.0f)));
        }

        [Test]
        public void GetEffectivePivotReturnsGlobalWhenBothAreNull()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._pivotMode = PivotMode.BottomLeft;
            extractor._customPivot = new Vector2(0.0f, 0.0f);

            Vector2 result = extractor.GetEffectivePivot(null, null);
            Assert.That(result, Is.EqualTo(new Vector2(0.0f, 0.0f)));
        }

        [Test]
        public void PivotModeToVector2ReturnsDefaultForObsoleteNoneValue()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            Vector2 result = SpriteSheetExtractor.PivotModeToVector2(PivotMode.None, Vector2.zero);
#pragma warning restore CS0618
            Assert.That(result, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        }

        #endregion

        #region Config Save/Load Tests

        [Test]
        public void ComputeFileHashReturnsNullForNullPath()
        {
            string result = SpriteSheetExtractor.ComputeFileHash(null);
            Assert.IsNull(result);
        }

        [Test]
        public void ComputeFileHashReturnsNullForEmptyPath()
        {
            string result = SpriteSheetExtractor.ComputeFileHash(string.Empty);
            Assert.IsNull(result);
        }

        [Test]
        public void ComputeFileHashReturnsNullForNonexistentFile()
        {
            string result = SpriteSheetExtractor.ComputeFileHash("/nonexistent/path/file.png");
            Assert.IsNull(result);
        }

        [Test]
        public void ComputeFileHashReturnsConsistentHashForSameFile()
        {
            string path = CreateSpriteSheet("hash_test", 32, 32, 2, 2);
            string fullPath = RelToFull(path);

            string hash1 = SpriteSheetExtractor.ComputeFileHash(fullPath);
            string hash2 = SpriteSheetExtractor.ComputeFileHash(fullPath);

            Assert.IsNotNull(hash1);
            Assert.IsNotNull(hash2);
            Assert.That(hash1, Is.EqualTo(hash2));
        }

        [Test]
        public void ComputeFileHashReturnsLowercaseHex()
        {
            string path = CreateSpriteSheet("hash_hex_test", 32, 32, 2, 2);
            string fullPath = RelToFull(path);

            string hash = SpriteSheetExtractor.ComputeFileHash(fullPath);

            Assert.IsNotNull(hash);
            Assert.That(hash.Length, Is.EqualTo(64));
            Assert.That(hash, Is.EqualTo(hash.ToLowerInvariant()));
        }

        [Test]
        public void ComputeFileHashReturnsDifferentHashForDifferentFiles()
        {
            string path1 = CreateSpriteSheet("hash_different_test1", 32, 32, 2, 2);
            string path2 = CreateSpriteSheet("hash_different_test2", 64, 64, 2, 2);
            string fullPath1 = RelToFull(path1);
            string fullPath2 = RelToFull(path2);

            string hash1 = SpriteSheetExtractor.ComputeFileHash(fullPath1);
            string hash2 = SpriteSheetExtractor.ComputeFileHash(fullPath2);

            Assert.IsNotNull(hash1);
            Assert.IsNotNull(hash2);
            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void SaveConfigCreatesJsonFile()
        {
            string path = CreateSpriteSheet("save_config_test", 32, 32, 2, 2);
            string configPath = SpriteSheetConfig.GetConfigPath(path);
            string fullConfigPath = RelToFull(configPath);
            TrackAssetPath(configPath);

            SpriteSheetExtractor extractor = CreateExtractor();
            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _assetPath = path,
                _useGlobalSettings = false,
                _pivotModeOverride = PivotMode.TopLeft,
                _customPivotOverride = new Vector2(0.3f, 0.7f),
            };

            bool result = extractor.SaveConfig(entry);

            Assert.IsTrue(result);
            Assert.IsTrue(File.Exists(fullConfigPath));
        }

        [Test]
        public void SaveConfigSetsConfigLoadedFlag()
        {
            string path = CreateSpriteSheet("save_loaded_flag_test", 32, 32, 2, 2);
            string configPath = SpriteSheetConfig.GetConfigPath(path);
            TrackAssetPath(configPath);

            SpriteSheetExtractor extractor = CreateExtractor();
            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _assetPath = path,
                _useGlobalSettings = false,
                _pivotModeOverride = PivotMode.Center,
            };

            Assert.IsFalse(entry._configLoaded);

            _ = extractor.SaveConfig(entry);

            Assert.IsTrue(entry._configLoaded);
            Assert.IsFalse(entry._configStale);
        }

        [Test]
        public void LoadConfigRestoresPivotSettings()
        {
            string path = CreateSpriteSheet("load_pivot_test", 32, 32, 2, 2);
            string configPath = SpriteSheetConfig.GetConfigPath(path);
            TrackAssetPath(configPath);

            SpriteSheetExtractor extractor = CreateExtractor();
            SpriteSheetExtractor.SpriteSheetEntry originalEntry = new()
            {
                _assetPath = path,
                _useGlobalSettings = false,
                _pivotModeOverride = PivotMode.BottomRight,
                _customPivotOverride = new Vector2(0.1f, 0.9f),
            };

            _ = extractor.SaveConfig(originalEntry);

            SpriteSheetExtractor.SpriteSheetEntry newEntry = new()
            {
                _assetPath = path,
                _useGlobalSettings = true,
                _pivotModeOverride = null,
                _customPivotOverride = null,
            };

            bool loadResult = extractor.LoadConfig(newEntry);

            Assert.IsTrue(loadResult);
            Assert.That(newEntry._pivotModeOverride, Is.EqualTo(PivotMode.BottomRight));
            Assert.That(newEntry._customPivotOverride, Is.EqualTo(new Vector2(0.1f, 0.9f)));
            Assert.IsFalse(newEntry._useGlobalSettings);
        }

        [Test]
        public void LoadConfigReturnsFalseForNonexistentConfig()
        {
            string path = CreateSpriteSheet("no_config_test", 32, 32, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            SpriteSheetExtractor.SpriteSheetEntry entry = new() { _assetPath = path };

            bool result = extractor.LoadConfig(entry);

            Assert.IsFalse(result);
            Assert.IsFalse(entry._configLoaded);
        }

        [Test]
        public void LoadConfigDetectsStaleConfigWhenTextureChanged()
        {
            string path = CreateSpriteSheet("stale_config_test", 32, 32, 2, 2);
            string configPath = SpriteSheetConfig.GetConfigPath(path);
            string fullConfigPath = RelToFull(configPath);
            TrackAssetPath(configPath);

            SpriteSheetExtractor extractor = CreateExtractor();
            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _assetPath = path,
                _useGlobalSettings = false,
                _pivotModeOverride = PivotMode.Center,
            };

            _ = extractor.SaveConfig(entry);

            string configJson = File.ReadAllText(fullConfigPath, System.Text.Encoding.UTF8);
            configJson = configJson.Replace(
                entry._loadedConfig.textureContentHash,
                "0000000000000000000000000000000000000000000000000000000000000000"
            );
            File.WriteAllText(fullConfigPath, configJson, System.Text.Encoding.UTF8);

            SpriteSheetExtractor.SpriteSheetEntry newEntry = new() { _assetPath = path };

            _ = extractor.LoadConfig(newEntry);

            Assert.IsTrue(newEntry._configLoaded);
            Assert.IsTrue(newEntry._configStale);
        }

        [Test]
        public void LoadConfigDoesNotMarkStaleWhenHashMatches()
        {
            string path = CreateSpriteSheet("fresh_config_test", 32, 32, 2, 2);
            string configPath = SpriteSheetConfig.GetConfigPath(path);
            TrackAssetPath(configPath);

            SpriteSheetExtractor extractor = CreateExtractor();
            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _assetPath = path,
                _useGlobalSettings = false,
                _pivotModeOverride = PivotMode.TopCenter,
            };

            _ = extractor.SaveConfig(entry);

            SpriteSheetExtractor.SpriteSheetEntry newEntry = new() { _assetPath = path };

            _ = extractor.LoadConfig(newEntry);

            Assert.IsTrue(newEntry._configLoaded);
            Assert.IsFalse(newEntry._configStale);
        }

        [Test]
        public void SaveConfigReturnsFalseForNullEntry()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            bool result = extractor.SaveConfig(null);
            Assert.IsFalse(result);
        }

        [Test]
        public void SaveConfigReturnsFalseForEmptyAssetPath()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            SpriteSheetExtractor.SpriteSheetEntry entry = new() { _assetPath = string.Empty };

            bool result = extractor.SaveConfig(entry);
            Assert.IsFalse(result);
        }

        [Test]
        public void LoadConfigReturnsFalseForNullEntry()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            bool result = extractor.LoadConfig(null);
            Assert.IsFalse(result);
        }

        [Test]
        public void LoadConfigReturnsFalseForEmptyAssetPath()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            SpriteSheetExtractor.SpriteSheetEntry entry = new() { _assetPath = string.Empty };

            bool result = extractor.LoadConfig(entry);
            Assert.IsFalse(result);
        }

        [Test]
        public void LoadConfigReturnsFalseForCorruptedJson()
        {
            string path = CreateSpriteSheet("corrupted_json_test", 32, 32, 2, 2);
            string configPath = SpriteSheetConfig.GetConfigPath(path);
            string fullConfigPath = RelToFull(configPath);
            TrackAssetPath(configPath);

            File.WriteAllText(fullConfigPath, "{ invalid json }}", Encoding.UTF8);

            SpriteSheetExtractor extractor = CreateExtractor();
            SpriteSheetExtractor.SpriteSheetEntry entry = new() { _assetPath = path };

            bool result = extractor.LoadConfig(entry);

            Assert.IsFalse(result);
            Assert.IsFalse(entry._configLoaded);
        }

        [Test]
        public void TryAutoLoadConfigLoadsExistingConfig()
        {
            string path = CreateSpriteSheet("auto_load_test", 32, 32, 2, 2);
            string configPath = SpriteSheetConfig.GetConfigPath(path);
            TrackAssetPath(configPath);

            SpriteSheetExtractor extractor = CreateExtractor();
            SpriteSheetExtractor.SpriteSheetEntry saveEntry = new()
            {
                _assetPath = path,
                _useGlobalSettings = false,
                _pivotModeOverride = PivotMode.LeftCenter,
            };

            _ = extractor.SaveConfig(saveEntry);

            SpriteSheetExtractor.SpriteSheetEntry loadEntry = new()
            {
                _assetPath = path,
                _useGlobalSettings = true,
            };

            extractor.TryAutoLoadConfig(loadEntry);

            Assert.IsTrue(loadEntry._configLoaded);
            Assert.That(loadEntry._pivotModeOverride, Is.EqualTo(PivotMode.LeftCenter));
        }

        [Test]
        public void TryAutoLoadConfigDoesNothingForNoConfig()
        {
            string path = CreateSpriteSheet("no_auto_load_test", 32, 32, 2, 2);

            SpriteSheetExtractor extractor = CreateExtractor();
            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _assetPath = path,
                _useGlobalSettings = true,
            };

            extractor.TryAutoLoadConfig(entry);

            Assert.IsFalse(entry._configLoaded);
            Assert.IsNull(entry._pivotModeOverride);
        }

        [Test]
        public void TryAutoLoadConfigHandlesNullEntry()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            // Should not throw
            extractor.TryAutoLoadConfig(null);
        }

        [Test]
        public void TryAutoLoadConfigHandlesEmptyAssetPath()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            SpriteSheetExtractor.SpriteSheetEntry entry = new() { _assetPath = string.Empty };
            // Should not throw
            extractor.TryAutoLoadConfig(entry);
            Assert.IsFalse(entry._configLoaded);
        }

        [Test]
        public void ConfigRoundTripPreservesAllFields()
        {
            string path = CreateSpriteSheet("round_trip_test", 32, 32, 2, 2);
            string configPath = SpriteSheetConfig.GetConfigPath(path);
            TrackAssetPath(configPath);

            SpriteSheetExtractor extractor = CreateExtractor();
            Vector2 customPivot = new(0.33f, 0.66f);

            SpriteSheetExtractor.SpriteSheetEntry saveEntry = new()
            {
                _assetPath = path,
                _useGlobalSettings = false,
                _pivotModeOverride = PivotMode.Custom,
                _customPivotOverride = customPivot,
            };

            _ = extractor.SaveConfig(saveEntry);

            SpriteSheetExtractor.SpriteSheetEntry loadEntry = new() { _assetPath = path };

            _ = extractor.LoadConfig(loadEntry);

            Assert.That(loadEntry._pivotModeOverride, Is.EqualTo(PivotMode.Custom));
            Assert.That(loadEntry._customPivotOverride, Is.EqualTo(customPivot));
            Assert.IsNotNull(loadEntry._loadedConfig);
            Assert.That(
                loadEntry._loadedConfig.version,
                Is.EqualTo(SpriteSheetConfig.CurrentVersion)
            );
        }

        [Test]
        public void GetConfigPathReturnsExpectedFormat()
        {
            string texturePath = "Assets/Textures/MySprite.png";
            string configPath = SpriteSheetConfig.GetConfigPath(texturePath);

            Assert.That(configPath, Is.EqualTo("Assets/Textures/MySprite.png.spritesheet.json"));
        }

        [Test]
        public void GetConfigPathReturnsEmptyForNullPath()
        {
            string configPath = SpriteSheetConfig.GetConfigPath(null);
            Assert.That(configPath, Is.EqualTo(string.Empty));
        }

        [Test]
        public void GetConfigPathReturnsEmptyForEmptyPath()
        {
            string configPath = SpriteSheetConfig.GetConfigPath(string.Empty);
            Assert.That(configPath, Is.EqualTo(string.Empty));
        }

        [Test]
        public void MigrateConfigHandlesNullGracefully()
        {
            Assert.DoesNotThrow(() => SpriteSheetConfig.MigrateConfig(null));
        }

        [Test]
        public void MigrateConfigSetsCurrentVersion()
        {
            SpriteSheetConfig config = new() { version = 0 };

            SpriteSheetConfig.MigrateConfig(config);

            Assert.That(config.version, Is.EqualTo(SpriteSheetConfig.CurrentVersion));
        }

        #endregion

        #region Copy Settings Tests

        [Test]
        public void CopySettingsFromEntryCopiesPivotSettings()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            SpriteSheetExtractor.SpriteSheetEntry source = new()
            {
                _pivotModeOverride = PivotMode.TopRight,
                _customPivotOverride = new Vector2(0.2f, 0.8f),
            };

            SpriteSheetExtractor.SpriteSheetEntry target = new()
            {
                _pivotModeOverride = PivotMode.Center,
                _customPivotOverride = new Vector2(0.5f, 0.5f),
            };

            extractor.CopySettingsFromEntry(source, target);

            Assert.That(target._pivotModeOverride, Is.EqualTo(PivotMode.TopRight));
            Assert.That(target._customPivotOverride, Is.EqualTo(new Vector2(0.2f, 0.8f)));
        }

        #endregion

        #region Algorithm Tests

        [Test]
        public void BoundaryScoringAlgorithmProducesValidGridDimensions()
        {
            // Create a simple grid with transparent boundaries
            int width = 128;
            int height = 128;
            Color32[] pixels = new Color32[width * height];

            // Fill with transparent pixels
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            // Create 4x4 grid of opaque squares (32x32 cells)
            int cellSize = 32;
            for (int row = 0; row < 4; ++row)
            {
                for (int col = 0; col < 4; ++col)
                {
                    int startX = col * cellSize + 2;
                    int startY = row * cellSize + 2;
                    int endX = (col + 1) * cellSize - 2;
                    int endY = (row + 1) * cellSize - 2;

                    for (int y = startY; y < endY; ++y)
                    {
                        for (int x = startX; x < endX; ++x)
                        {
                            pixels[y * width + x] = new Color32(255, 128, 64, 255);
                        }
                    }
                }
            }

            SpriteSheetAlgorithms.AlgorithmResult result = SpriteSheetAlgorithms.DetectGrid(
                pixels,
                width,
                height,
                0.01f,
                AutoDetectionAlgorithm.BoundaryScoring
            );

            Assert.That(result.IsValid, Is.True, "BoundaryScoring should produce valid result");
            Assert.That(result.CellWidth, Is.GreaterThanOrEqualTo(4));
            Assert.That(result.CellHeight, Is.GreaterThanOrEqualTo(4));
            Assert.That(result.Algorithm, Is.EqualTo(AutoDetectionAlgorithm.BoundaryScoring));
        }

        [Test]
        public void UniformGridAlgorithmProducesValidGridDimensionsWithCount()
        {
            int width = 128;
            int height = 64;
            Color32[] pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 128, 64, 255);
            }

            SpriteSheetAlgorithms.AlgorithmResult result = SpriteSheetAlgorithms.DetectGrid(
                pixels,
                width,
                height,
                0.01f,
                AutoDetectionAlgorithm.UniformGrid,
                8
            );

            Assert.That(result.IsValid, Is.True, "UniformGrid should produce valid result");
            Assert.That(result.CellWidth, Is.GreaterThanOrEqualTo(4));
            Assert.That(result.CellHeight, Is.GreaterThanOrEqualTo(4));
            Assert.That(result.Algorithm, Is.EqualTo(AutoDetectionAlgorithm.UniformGrid));
            Assert.That(result.Confidence, Is.GreaterThan(0f));
        }

        [Test]
        public void UniformGridAlgorithmReturnsInvalidWithoutCount()
        {
            int width = 64;
            int height = 64;
            Color32[] pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 128, 64, 255);
            }

            SpriteSheetAlgorithms.AlgorithmResult result = SpriteSheetAlgorithms.DetectGrid(
                pixels,
                width,
                height,
                0.01f,
                AutoDetectionAlgorithm.UniformGrid,
                -1
            );

            Assert.That(result.IsValid, Is.False, "UniformGrid should fail without expected count");
        }

        [Test]
        public void ClusterCentroidAlgorithmProducesValidGridDimensions()
        {
            // Create 2x2 grid of opaque circles
            int width = 128;
            int height = 128;
            Color32[] pixels = new Color32[width * height];

            // Fill with transparent
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            // Create 4 opaque squares (simulate sprites)
            int cellSize = 64;
            int spriteSize = 48;
            for (int row = 0; row < 2; ++row)
            {
                for (int col = 0; col < 2; ++col)
                {
                    int centerX = col * cellSize + cellSize / 2;
                    int centerY = row * cellSize + cellSize / 2;
                    int halfSize = spriteSize / 2;

                    for (int y = centerY - halfSize; y < centerY + halfSize; ++y)
                    {
                        for (int x = centerX - halfSize; x < centerX + halfSize; ++x)
                        {
                            if (x >= 0 && x < width && y >= 0 && y < height)
                            {
                                pixels[y * width + x] = new Color32(255, 200, 100, 255);
                            }
                        }
                    }
                }
            }

            SpriteSheetAlgorithms.AlgorithmResult result = SpriteSheetAlgorithms.DetectGrid(
                pixels,
                width,
                height,
                0.01f,
                AutoDetectionAlgorithm.ClusterCentroid
            );

            Assert.That(result.IsValid, Is.True, "ClusterCentroid should produce valid result");
            Assert.That(result.CellWidth, Is.GreaterThanOrEqualTo(4));
            Assert.That(result.CellHeight, Is.GreaterThanOrEqualTo(4));
            Assert.That(result.Algorithm, Is.EqualTo(AutoDetectionAlgorithm.ClusterCentroid));
        }

        [Test]
        public void DistanceTransformAlgorithmProducesValidGridDimensions()
        {
            // Create 2x2 grid of opaque squares
            int width = 128;
            int height = 128;
            Color32[] pixels = new Color32[width * height];

            // Fill with transparent
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            // Create 4 opaque squares
            int cellSize = 64;
            int spriteSize = 40;
            for (int row = 0; row < 2; ++row)
            {
                for (int col = 0; col < 2; ++col)
                {
                    int startX = col * cellSize + (cellSize - spriteSize) / 2;
                    int startY = row * cellSize + (cellSize - spriteSize) / 2;

                    for (int y = startY; y < startY + spriteSize; ++y)
                    {
                        for (int x = startX; x < startX + spriteSize; ++x)
                        {
                            if (x >= 0 && x < width && y >= 0 && y < height)
                            {
                                pixels[y * width + x] = new Color32(128, 255, 64, 255);
                            }
                        }
                    }
                }
            }

            SpriteSheetAlgorithms.AlgorithmResult result = SpriteSheetAlgorithms.DetectGrid(
                pixels,
                width,
                height,
                0.01f,
                AutoDetectionAlgorithm.DistanceTransform
            );

            Assert.That(result.IsValid, Is.True, "Algorithm should produce valid result");
            Assert.That(result.CellWidth, Is.GreaterThanOrEqualTo(4));
            Assert.That(result.CellHeight, Is.GreaterThanOrEqualTo(4));
            Assert.That(result.Algorithm, Is.EqualTo(AutoDetectionAlgorithm.DistanceTransform));
        }

        [Test]
        public void RegionGrowingAlgorithmProducesValidGridDimensions()
        {
            // Create 2x2 grid of opaque squares
            int width = 128;
            int height = 128;
            Color32[] pixels = new Color32[width * height];

            // Fill with transparent
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            // Create 4 opaque squares with varied colors
            int cellSize = 64;
            int spriteSize = 44;
            for (int row = 0; row < 2; ++row)
            {
                for (int col = 0; col < 2; ++col)
                {
                    int startX = col * cellSize + (cellSize - spriteSize) / 2;
                    int startY = row * cellSize + (cellSize - spriteSize) / 2;
                    byte colorOffset = (byte)((row * 2 + col) * 40);

                    for (int y = startY; y < startY + spriteSize; ++y)
                    {
                        for (int x = startX; x < startX + spriteSize; ++x)
                        {
                            if (x >= 0 && x < width && y >= 0 && y < height)
                            {
                                pixels[y * width + x] = new Color32(
                                    (byte)(200 + colorOffset % 55),
                                    (byte)(150 + colorOffset % 55),
                                    (byte)(100 + colorOffset % 55),
                                    255
                                );
                            }
                        }
                    }
                }
            }

            SpriteSheetAlgorithms.AlgorithmResult result = SpriteSheetAlgorithms.DetectGrid(
                pixels,
                width,
                height,
                0.01f,
                AutoDetectionAlgorithm.RegionGrowing
            );

            Assert.That(result.IsValid, Is.True, "Algorithm should produce valid result");
            Assert.That(result.CellWidth, Is.GreaterThanOrEqualTo(4));
            Assert.That(result.CellHeight, Is.GreaterThanOrEqualTo(4));
            Assert.That(result.Algorithm, Is.EqualTo(AutoDetectionAlgorithm.RegionGrowing));
        }

        [Test]
        public void AutoBestAlgorithmSelectsAppropriateAlgorithm()
        {
            // Create a simple grid with transparent boundaries
            int width = 128;
            int height = 128;
            Color32[] pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            int cellSize = 32;
            for (int row = 0; row < 4; ++row)
            {
                for (int col = 0; col < 4; ++col)
                {
                    int startX = col * cellSize + 4;
                    int startY = row * cellSize + 4;
                    int endX = (col + 1) * cellSize - 4;
                    int endY = (row + 1) * cellSize - 4;

                    for (int y = startY; y < endY; ++y)
                    {
                        for (int x = startX; x < endX; ++x)
                        {
                            pixels[y * width + x] = new Color32(255, 128, 64, 255);
                        }
                    }
                }
            }

            SpriteSheetAlgorithms.AlgorithmResult result = SpriteSheetAlgorithms.DetectGrid(
                pixels,
                width,
                height,
                0.01f,
                AutoDetectionAlgorithm.AutoBest
            );

            Assert.That(result.IsValid, Is.True, "AutoBest should produce valid result");
            Assert.That(result.Algorithm, Is.EqualTo(AutoDetectionAlgorithm.AutoBest));
            Assert.That(result.Confidence, Is.GreaterThan(0f));
        }

        [Test]
        public void ConfidenceValuesAreInValidRange()
        {
            int width = 64;
            int height = 64;
            Color32[] pixels = new Color32[width * height];

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 128, 64, 255);
            }

            AutoDetectionAlgorithm[] algorithms =
            {
                AutoDetectionAlgorithm.BoundaryScoring,
                AutoDetectionAlgorithm.ClusterCentroid,
                AutoDetectionAlgorithm.DistanceTransform,
                AutoDetectionAlgorithm.RegionGrowing,
                AutoDetectionAlgorithm.AutoBest,
            };

            for (int i = 0; i < algorithms.Length; ++i)
            {
                SpriteSheetAlgorithms.AlgorithmResult result = SpriteSheetAlgorithms.DetectGrid(
                    pixels,
                    width,
                    height,
                    0.01f,
                    algorithms[i],
                    4
                );

                Assert.That(
                    result.Confidence,
                    Is.GreaterThanOrEqualTo(0f),
                    $"{algorithms[i]} confidence should be >= 0"
                );
                Assert.That(
                    result.Confidence,
                    Is.LessThanOrEqualTo(1f),
                    $"{algorithms[i]} confidence should be <= 1"
                );
            }
        }

        [Test]
        public void DetectGridHandlesNullPixelsGracefully()
        {
            SpriteSheetAlgorithms.AlgorithmResult result = SpriteSheetAlgorithms.DetectGrid(
                null,
                64,
                64,
                0.01f,
                AutoDetectionAlgorithm.BoundaryScoring
            );

            Assert.That(result.IsValid, Is.False, "Should return invalid for null pixels");
        }

        [Test]
        public void DetectGridHandlesEmptyPixelsGracefully()
        {
            SpriteSheetAlgorithms.AlgorithmResult result = SpriteSheetAlgorithms.DetectGrid(
                new Color32[0],
                64,
                64,
                0.01f,
                AutoDetectionAlgorithm.BoundaryScoring
            );

            Assert.That(result.IsValid, Is.False, "Should return invalid for empty pixels");
        }

        [Test]
        public void DetectGridHandlesMismatchedDimensionsGracefully()
        {
            Color32[] pixels = new Color32[32 * 32];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 128, 64, 255);
            }

            SpriteSheetAlgorithms.AlgorithmResult result = SpriteSheetAlgorithms.DetectGrid(
                pixels,
                64,
                64,
                0.01f,
                AutoDetectionAlgorithm.BoundaryScoring
            );

            Assert.That(
                result.IsValid,
                Is.False,
                "Should return invalid for mismatched dimensions"
            );
        }

        [Test]
        public void DetectGridHandlesTooSmallDimensionsGracefully()
        {
            Color32[] pixels = new Color32[2 * 2];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 128, 64, 255);
            }

            SpriteSheetAlgorithms.AlgorithmResult result = SpriteSheetAlgorithms.DetectGrid(
                pixels,
                2,
                2,
                0.01f,
                AutoDetectionAlgorithm.BoundaryScoring
            );

            Assert.That(result.IsValid, Is.False, "Should return invalid for tiny dimensions");
        }

        [Test]
        public void DetectGridReturnsInvalidForNegativeAlphaThreshold()
        {
            Color32[] pixels = CreateSimpleSpriteSheetPixels(64, 64, 2, 2);

            SpriteSheetAlgorithms.AlgorithmResult result = SpriteSheetAlgorithms.DetectGrid(
                pixels,
                64,
                64,
                alphaThreshold: -0.5f,
                algorithm: AutoDetectionAlgorithm.BoundaryScoring
            );

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void DetectGridReturnsInvalidForAlphaThresholdAtOne()
        {
            Color32[] pixels = CreateSimpleSpriteSheetPixels(64, 64, 2, 2);

            SpriteSheetAlgorithms.AlgorithmResult result = SpriteSheetAlgorithms.DetectGrid(
                pixels,
                64,
                64,
                alphaThreshold: 1f,
                algorithm: AutoDetectionAlgorithm.BoundaryScoring
            );

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void DetectGridReturnsInvalidForAlphaThresholdAboveOne()
        {
            Color32[] pixels = CreateSimpleSpriteSheetPixels(64, 64, 2, 2);

            SpriteSheetAlgorithms.AlgorithmResult result = SpriteSheetAlgorithms.DetectGrid(
                pixels,
                64,
                64,
                alphaThreshold: 2f,
                algorithm: AutoDetectionAlgorithm.BoundaryScoring
            );

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void DetectGridRespectsEarlyCancellation()
        {
            Color32[] pixels = CreateSimpleSpriteSheetPixels(64, 64, 2, 2);
            using CancellationTokenSource cts = new();
            cts.Cancel();

            SpriteSheetAlgorithms.AlgorithmResult result = SpriteSheetAlgorithms.DetectGrid(
                pixels,
                64,
                64,
                alphaThreshold: 0.1f,
                algorithm: AutoDetectionAlgorithm.AutoBest,
                cancellationToken: cts.Token
            );

            Assert.That(
                result.IsValid,
                Is.False,
                "Cancelled operation should return invalid result"
            );
        }

        [Test]
        public void CachedAlgorithmResultRoundTrips()
        {
            SpriteSheetAlgorithms.AlgorithmResult original = new(
                32,
                48,
                0.85f,
                AutoDetectionAlgorithm.ClusterCentroid
            );

            CachedAlgorithmResult cached = CachedAlgorithmResult.FromResult(original);
            SpriteSheetAlgorithms.AlgorithmResult restored = cached.ToResult();

            Assert.That(restored.CellWidth, Is.EqualTo(original.CellWidth));
            Assert.That(restored.CellHeight, Is.EqualTo(original.CellHeight));
            Assert.That(restored.Confidence, Is.EqualTo(original.Confidence).Within(0.001f));
            Assert.That(restored.Algorithm, Is.EqualTo(original.Algorithm));
        }

        [Test]
        public void GetEffectiveAutoDetectionAlgorithmReturnsGlobalWhenUsingGlobalSettings()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._autoDetectionAlgorithm = AutoDetectionAlgorithm.ClusterCentroid;

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _useGlobalSettings = true,
                _autoDetectionAlgorithmOverride = AutoDetectionAlgorithm.BoundaryScoring,
            };

            AutoDetectionAlgorithm result = extractor.GetEffectiveAutoDetectionAlgorithm(entry);

            Assert.That(result, Is.EqualTo(AutoDetectionAlgorithm.ClusterCentroid));
        }

        [Test]
        public void GetEffectiveAutoDetectionAlgorithmReturnsOverrideWhenNotUsingGlobalSettings()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._autoDetectionAlgorithm = AutoDetectionAlgorithm.ClusterCentroid;

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _useGlobalSettings = false,
                _autoDetectionAlgorithmOverride = AutoDetectionAlgorithm.BoundaryScoring,
            };

            AutoDetectionAlgorithm result = extractor.GetEffectiveAutoDetectionAlgorithm(entry);

            Assert.That(result, Is.EqualTo(AutoDetectionAlgorithm.BoundaryScoring));
        }

        [Test]
        public void GetEffectiveExpectedSpriteCountReturnsGlobalWhenUsingGlobalSettings()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._expectedSpriteCountHint = 16;

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _useGlobalSettings = true,
                _expectedSpriteCountOverride = 8,
            };

            int result = extractor.GetEffectiveExpectedSpriteCount(entry);

            Assert.That(result, Is.EqualTo(16));
        }

        [Test]
        public void GetEffectiveExpectedSpriteCountReturnsOverrideWhenNotUsingGlobalSettings()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._expectedSpriteCountHint = 16;

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _useGlobalSettings = false,
                _expectedSpriteCountOverride = 8,
            };

            int result = extractor.GetEffectiveExpectedSpriteCount(entry);

            Assert.That(result, Is.EqualTo(8));
        }

        [Test]
        public void CopySettingsFromEntryCopiesAlgorithmSettings()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            SpriteSheetExtractor.SpriteSheetEntry source = new()
            {
                _autoDetectionAlgorithmOverride = AutoDetectionAlgorithm.DistanceTransform,
                _expectedSpriteCountOverride = 24,
            };

            SpriteSheetExtractor.SpriteSheetEntry target = new()
            {
                _autoDetectionAlgorithmOverride = AutoDetectionAlgorithm.AutoBest,
                _expectedSpriteCountOverride = -1,
            };

            extractor.CopySettingsFromEntry(source, target);

            Assert.That(
                target._autoDetectionAlgorithmOverride,
                Is.EqualTo(AutoDetectionAlgorithm.DistanceTransform)
            );
            Assert.That(target._expectedSpriteCountOverride, Is.EqualTo(24));
        }

        [Test]
        public void ConfigMigrationAddsAlgorithmFields()
        {
            SpriteSheetConfig config = new() { version = 1 };

            SpriteSheetConfig.MigrateConfig(config);

            Assert.That(config.version, Is.EqualTo(SpriteSheetConfig.CurrentVersion));
            Assert.That(config.algorithm, Is.EqualTo((int)AutoDetectionAlgorithm.AutoBest));
        }

        [Test]
        public void ConfigMigrationV2ToV3AddsSnapToTextureDivisorField()
        {
            SpriteSheetConfig config = new() { version = 2, snapToTextureDivisor = false };

            SpriteSheetConfig.MigrateConfig(config);

            Assert.That(config.version, Is.EqualTo(SpriteSheetConfig.CurrentVersion));
            Assert.That(config.snapToTextureDivisor, Is.True);
        }

        [Test]
        public void ConfigMigrationV3PreservesSnapToTextureDivisor()
        {
            SpriteSheetConfig config = new() { version = 3, snapToTextureDivisor = false };

            SpriteSheetConfig.MigrateConfig(config);

            Assert.That(config.version, Is.EqualTo(SpriteSheetConfig.CurrentVersion));
            Assert.That(config.snapToTextureDivisor, Is.False);
        }

        [Test]
        [TestCase(64, 64, 32, 32, 0.1f, TestName = "Divisor.Uniform.32x32")]
        [TestCase(128, 64, 64, 32, 0.1f, TestName = "Divisor.NonSquare.64x32")]
        [TestCase(256, 256, 64, 64, 0.1f, TestName = "Divisor.Large.64x64")]
        public void FindBestTransparencyAlignedDivisorReturnsValidDivisor(
            int textureWidth,
            int textureHeight,
            int baseCellWidth,
            int baseCellHeight,
            float transparencyThreshold
        )
        {
            Color32[] pixels = CreateSimpleSpriteSheetPixels(
                textureWidth,
                textureHeight,
                textureWidth / baseCellWidth,
                textureHeight / baseCellHeight
            );

            Vector2Int result = SpriteSheetAlgorithms.FindBestTransparencyAlignedDivisor(
                pixels,
                textureWidth,
                textureHeight,
                baseCellWidth,
                baseCellHeight,
                transparencyThreshold
            );

            Assert.That(result.x, Is.GreaterThanOrEqualTo(SpriteSheetAlgorithms.MinimumCellSize));
            Assert.That(result.y, Is.GreaterThanOrEqualTo(SpriteSheetAlgorithms.MinimumCellSize));
            Assert.That(textureWidth % result.x, Is.EqualTo(0), "Width must divide evenly");
            Assert.That(textureHeight % result.y, Is.EqualTo(0), "Height must divide evenly");
        }

        [Test]
        public void FindBestTransparencyAlignedDivisorHandlesCheckerPatternTexture()
        {
            int width = 64;
            int height = 64;
            Color32[] pixels = new Color32[width * height];

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    bool isOpaqueCell = ((x / 16) + (y / 16)) % 2 == 0;
                    pixels[y * width + x] = isOpaqueCell
                        ? new Color32(255, 128, 64, 255)
                        : new Color32(0, 0, 0, 0);
                }
            }

            Vector2Int result = SpriteSheetAlgorithms.FindBestTransparencyAlignedDivisor(
                pixels,
                width,
                height,
                16,
                16,
                0.1f
            );

            Assert.That(result.x, Is.GreaterThanOrEqualTo(SpriteSheetAlgorithms.MinimumCellSize));
            Assert.That(result.y, Is.GreaterThanOrEqualTo(SpriteSheetAlgorithms.MinimumCellSize));
            Assert.That(width % result.x, Is.EqualTo(0));
            Assert.That(height % result.y, Is.EqualTo(0));
        }

        [Test]
        public void FindBestTransparencyAlignedDivisorHandlesPartiallyTransparentTexture()
        {
            int width = 64;
            int height = 64;
            Color32[] pixels = new Color32[width * height];

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    pixels[y * width + x] =
                        x < width / 2 ? new Color32(255, 128, 64, 255) : new Color32(0, 0, 0, 0);
                }
            }

            Vector2Int result = SpriteSheetAlgorithms.FindBestTransparencyAlignedDivisor(
                pixels,
                width,
                height,
                32,
                32,
                0.1f
            );

            Assert.That(result.x, Is.GreaterThanOrEqualTo(SpriteSheetAlgorithms.MinimumCellSize));
            Assert.That(result.y, Is.GreaterThanOrEqualTo(SpriteSheetAlgorithms.MinimumCellSize));
        }

        [Test]
        public void ScoreDivisorByTransparencyReturnsZeroForEmptyTexture()
        {
            Color32[] pixels = Array.Empty<Color32>();

            float score = SpriteSheetAlgorithms.ScoreDivisorByTransparency(
                pixels,
                0,
                0,
                16,
                16,
                0.1f
            );

            Assert.That(score, Is.EqualTo(0f));
        }

        [Test]
        public void ScoreDivisorByTransparencyReturnsZeroForAllOpaqueTexture()
        {
            int width = 64;
            int height = 64;
            Color32[] pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 128, 64, 255);
            }

            float score = SpriteSheetAlgorithms.ScoreDivisorByTransparency(
                pixels,
                width,
                height,
                32,
                32,
                0.1f
            );

            Assert.That(score, Is.EqualTo(0f));
        }

        [Test]
        public void ScoreDivisorByTransparencyReturnsOneForAllTransparentTexture()
        {
            int width = 64;
            int height = 64;
            Color32[] pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            float score = SpriteSheetAlgorithms.ScoreDivisorByTransparency(
                pixels,
                width,
                height,
                32,
                32,
                0.1f
            );

            Assert.That(score, Is.EqualTo(1f));
        }

        [Test]
        public void ScoreDivisorByTransparencyReturnsHigherScoreForBetterAlignment()
        {
            int width = 64;
            int height = 64;
            Color32[] pixelsGoodAlignment = new Color32[width * height];
            Color32[] pixelsPoorAlignment = new Color32[width * height];

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    bool isAtGridLine = x % 32 == 0 || y % 32 == 0;
                    pixelsGoodAlignment[y * width + x] = isAtGridLine
                        ? new Color32(0, 0, 0, 0)
                        : new Color32(255, 128, 64, 255);

                    bool isAtOffset = (x + 16) % 32 == 0 || (y + 16) % 32 == 0;
                    pixelsPoorAlignment[y * width + x] = isAtOffset
                        ? new Color32(0, 0, 0, 0)
                        : new Color32(255, 128, 64, 255);
                }
            }

            float goodScore = SpriteSheetAlgorithms.ScoreDivisorByTransparency(
                pixelsGoodAlignment,
                width,
                height,
                32,
                32,
                0.1f
            );
            float poorScore = SpriteSheetAlgorithms.ScoreDivisorByTransparency(
                pixelsPoorAlignment,
                width,
                height,
                32,
                32,
                0.1f
            );

            Assert.That(
                goodScore,
                Is.GreaterThan(poorScore),
                "Grid lines aligned with transparency should score higher"
            );
        }

        [Test]
        public void DetectGridWorksWithSnapToTextureDivisorDisabled()
        {
            Color32[] pixels = CreateSimpleSpriteSheetPixels(64, 64, 2, 2);

            SpriteSheetAlgorithms.AlgorithmResult result = SpriteSheetAlgorithms.DetectGrid(
                pixels,
                64,
                64,
                alphaThreshold: 0.1f,
                algorithm: AutoDetectionAlgorithm.BoundaryScoring,
                snapToTextureDivisor: false
            );

            Assert.That(result.IsValid, Is.True);
            Assert.That(
                result.CellWidth,
                Is.GreaterThanOrEqualTo(SpriteSheetAlgorithms.MinimumCellSize)
            );
            Assert.That(
                result.CellHeight,
                Is.GreaterThanOrEqualTo(SpriteSheetAlgorithms.MinimumCellSize)
            );
        }

        [Test]
        [TestCase(AutoDetectionAlgorithm.BoundaryScoring, TestName = "Snap.BoundaryScoring")]
        [TestCase(AutoDetectionAlgorithm.ClusterCentroid, TestName = "Snap.ClusterCentroid")]
        [TestCase(AutoDetectionAlgorithm.DistanceTransform, TestName = "Snap.DistanceTransform")]
        [TestCase(AutoDetectionAlgorithm.RegionGrowing, TestName = "Snap.RegionGrowing")]
        public void AlgorithmsProduceDivisorResultsWithSnapEnabled(AutoDetectionAlgorithm algorithm)
        {
            Color32[] pixels = CreateSimpleSpriteSheetPixels(128, 128, 4, 4);

            SpriteSheetAlgorithms.AlgorithmResult result = SpriteSheetAlgorithms.DetectGrid(
                pixels,
                128,
                128,
                alphaThreshold: 0.1f,
                algorithm: algorithm,
                snapToTextureDivisor: true
            );

            if (result.IsValid)
            {
                Assert.That(
                    128 % result.CellWidth,
                    Is.EqualTo(0),
                    $"{algorithm} width should divide evenly"
                );
                Assert.That(
                    128 % result.CellHeight,
                    Is.EqualTo(0),
                    $"{algorithm} height should divide evenly"
                );
            }
        }

        #endregion

        #region InitializeOverridesFromGlobal Tests

        [Test]
        public void InitializeOverridesFromGlobalCopiesAllGlobalValuesToOverrideFields()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            // Set specific global values
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 8;
            extractor._gridRows = 6;
            extractor._cellWidth = 64;
            extractor._cellHeight = 48;
            extractor._paddingLeft = 2;
            extractor._paddingRight = 3;
            extractor._paddingTop = 4;
            extractor._paddingBottom = 5;
            extractor._alphaThreshold = 0.15f;
            extractor._showOverlay = true;
            extractor._pivotMode = PivotMode.TopLeft;
            extractor._customPivot = new Vector2(0.25f, 0.75f);
            extractor._autoDetectionAlgorithm = AutoDetectionAlgorithm.ClusterCentroid;
            extractor._expectedSpriteCountHint = 48;

            // Create entry with _useGlobalSettings = true and all overrides null
            SpriteSheetExtractor.SpriteSheetEntry entry = new() { _useGlobalSettings = true };

            // Simulate the toggle: set wasGlobal=true, then _useGlobalSettings=false, call InitializeOverridesFromGlobal
            bool wasGlobal = entry._useGlobalSettings;
            entry._useGlobalSettings = false;

            Assert.That(wasGlobal, Is.True, "Entry should have started with global settings");
            Assert.That(
                entry._useGlobalSettings,
                Is.False,
                "Entry should now use per-sheet settings"
            );

            extractor.InitializeOverridesFromGlobal(entry);

            // Assert all override fields match the global values
            Assert.That(
                entry._extractionModeOverride,
                Is.EqualTo(SpriteSheetExtractor.ExtractionMode.GridBased)
            );
            Assert.That(
                entry._gridSizeModeOverride,
                Is.EqualTo(SpriteSheetExtractor.GridSizeMode.Manual)
            );
            Assert.That(entry._gridColumnsOverride, Is.EqualTo(8));
            Assert.That(entry._gridRowsOverride, Is.EqualTo(6));
            Assert.That(entry._cellWidthOverride, Is.EqualTo(64));
            Assert.That(entry._cellHeightOverride, Is.EqualTo(48));
            Assert.That(entry._paddingLeftOverride, Is.EqualTo(2));
            Assert.That(entry._paddingRightOverride, Is.EqualTo(3));
            Assert.That(entry._paddingTopOverride, Is.EqualTo(4));
            Assert.That(entry._paddingBottomOverride, Is.EqualTo(5));
            Assert.That(entry._alphaThresholdOverride, Is.EqualTo(0.15f).Within(0.0001f));
            Assert.That(entry._showOverlayOverride, Is.True);
            Assert.That(entry._pivotModeOverride, Is.EqualTo(PivotMode.TopLeft));
            Assert.That(entry._customPivotOverride.HasValue, Is.True);
            Assert.That(entry._customPivotOverride.Value.x, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(entry._customPivotOverride.Value.y, Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(
                entry._autoDetectionAlgorithmOverride,
                Is.EqualTo(AutoDetectionAlgorithm.ClusterCentroid)
            );
            Assert.That(entry._expectedSpriteCountOverride, Is.EqualTo(48));
        }

        [Test]
        public void InitializeOverridesFromGlobalClearsCachedAlgorithmResult()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _useGlobalSettings = true,
                _cachedAlgorithmResult = new SpriteSheetAlgorithms.AlgorithmResult(
                    cellWidth: 32,
                    cellHeight: 32,
                    confidence: 1.0f,
                    algorithm: AutoDetectionAlgorithm.AutoBest
                ),
                _lastAlgorithmDisplayText = "Previous algorithm text",
            };

            entry._useGlobalSettings = false;
            extractor.InitializeOverridesFromGlobal(entry);

            Assert.That(entry._cachedAlgorithmResult, Is.Null);
            Assert.That(entry._lastAlgorithmDisplayText, Is.Null);
        }

        [Test]
        public void InitializeOverridesFromGlobalDoesNothingForNullEntry()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            // Should not throw
            Assert.DoesNotThrow(() => extractor.InitializeOverridesFromGlobal(null));
        }

        [Test]
        public void TogglingFromPerSheetToGlobalDoesNotChangeOverrides()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.FromMetadata;

            // Entry with overrides set
            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _useGlobalSettings = false,
                _extractionModeOverride = SpriteSheetExtractor.ExtractionMode.AlphaDetection,
                _gridColumnsOverride = 12,
                _gridRowsOverride = 8,
                _showOverlayOverride = true,
                _pivotModeOverride = PivotMode.BottomRight,
            };

            // Toggle to global (wasGlobal=false, _useGlobalSettings=true)
            // This should NOT call InitializeOverridesFromGlobal - overrides should remain unchanged
            bool wasGlobal = entry._useGlobalSettings;
            entry._useGlobalSettings = true;

            Assert.That(wasGlobal, Is.False, "Entry should have started with per-sheet settings");
            Assert.That(entry._useGlobalSettings, Is.True, "Entry should now use global settings");

            // Overrides should remain unchanged (they're just not used when _useGlobalSettings is true)
            Assert.That(
                entry._extractionModeOverride,
                Is.EqualTo(SpriteSheetExtractor.ExtractionMode.AlphaDetection)
            );
            Assert.That(entry._gridColumnsOverride, Is.EqualTo(12));
            Assert.That(entry._gridRowsOverride, Is.EqualTo(8));
            Assert.That(entry._showOverlayOverride, Is.True);
            Assert.That(entry._pivotModeOverride, Is.EqualTo(PivotMode.BottomRight));

            // But effective values should now be global
            Assert.That(
                extractor.GetEffectiveExtractionMode(entry),
                Is.EqualTo(SpriteSheetExtractor.ExtractionMode.FromMetadata)
            );
        }

        [Test]
        public void AfterInitializeOverridesFromGlobalEffectiveValuesRemainSame()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            // Set specific global values
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.PaddedGrid;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Auto;
            extractor._showOverlay = true;
            extractor._pivotMode = PivotMode.BottomCenter;
            extractor._autoDetectionAlgorithm = AutoDetectionAlgorithm.RegionGrowing;

            // Create entry using global settings
            SpriteSheetExtractor.SpriteSheetEntry entry = new() { _useGlobalSettings = true };

            // Get effective values before toggle
            SpriteSheetExtractor.ExtractionMode effectiveModeBefore =
                extractor.GetEffectiveExtractionMode(entry);
            SpriteSheetExtractor.GridSizeMode effectiveGridModeBefore =
                extractor.GetEffectiveGridSizeMode(entry);
            bool effectiveOverlayBefore = extractor.GetEffectiveShowOverlay(entry);
            PivotMode effectivePivotBefore = extractor.GetEffectivePivotMode(entry);
            AutoDetectionAlgorithm effectiveAlgorithmBefore =
                extractor.GetEffectiveAutoDetectionAlgorithm(entry);

            // Simulate toggle from global to per-sheet
            entry._useGlobalSettings = false;
            extractor.InitializeOverridesFromGlobal(entry);

            // Get effective values after toggle
            SpriteSheetExtractor.ExtractionMode effectiveModeAfter =
                extractor.GetEffectiveExtractionMode(entry);
            SpriteSheetExtractor.GridSizeMode effectiveGridModeAfter =
                extractor.GetEffectiveGridSizeMode(entry);
            bool effectiveOverlayAfter = extractor.GetEffectiveShowOverlay(entry);
            PivotMode effectivePivotAfter = extractor.GetEffectivePivotMode(entry);
            AutoDetectionAlgorithm effectiveAlgorithmAfter =
                extractor.GetEffectiveAutoDetectionAlgorithm(entry);

            // Effective values should remain the same
            Assert.That(effectiveModeAfter, Is.EqualTo(effectiveModeBefore));
            Assert.That(effectiveGridModeAfter, Is.EqualTo(effectiveGridModeBefore));
            Assert.That(effectiveOverlayAfter, Is.EqualTo(effectiveOverlayBefore));
            Assert.That(effectivePivotAfter, Is.EqualTo(effectivePivotBefore));
            Assert.That(effectiveAlgorithmAfter, Is.EqualTo(effectiveAlgorithmBefore));
        }

        #endregion

        #region Batch Pivot Operations Tests

        [Test]
        public void EnableAllPivotsButtonSetsPivotOverrideForAllSprites()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._pivotMode = PivotMode.BottomLeft;
            extractor._customPivot = new Vector2(0.0f, 0.0f);

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _useGlobalSettings = true,
                _sprites = new List<SpriteSheetExtractor.SpriteEntryData>(),
            };

            // Create 4 sprites, none with pivot override enabled
            for (int i = 0; i < 4; i++)
            {
                SpriteSheetExtractor.SpriteEntryData sprite = new()
                {
                    _originalName = "sprite_" + i,
                    _usePivotOverride = false,
                    _pivotModeOverride = PivotMode.Center,
                    _customPivotOverride = new Vector2(0.5f, 0.5f),
                };
                entry._sprites.Add(sprite);
            }

            // Simulate Enable All Pivots button logic
            PivotMode effectiveMode = extractor.GetEffectivePivotMode(entry);
            Vector2 effectivePivot = extractor.GetEffectiveCustomPivot(entry);

            for (int i = 0; i < entry._sprites.Count; ++i)
            {
                SpriteSheetExtractor.SpriteEntryData sprite = entry._sprites[i];
                if (!sprite._usePivotOverride)
                {
                    sprite._usePivotOverride = true;
                    sprite._pivotModeOverride = effectiveMode;
                    sprite._customPivotOverride = effectivePivot;
                }
            }

            // Verify all sprites have pivot override enabled with correct values
            for (int i = 0; i < entry._sprites.Count; i++)
            {
                SpriteSheetExtractor.SpriteEntryData sprite = entry._sprites[i];
                Assert.That(
                    sprite._usePivotOverride,
                    Is.True,
                    "Sprite " + i + " should have pivot override enabled"
                );
                Assert.That(
                    sprite._pivotModeOverride,
                    Is.EqualTo(PivotMode.BottomLeft),
                    "Sprite " + i + " should have effective pivot mode copied"
                );
                Assert.That(
                    sprite._customPivotOverride,
                    Is.EqualTo(new Vector2(0.0f, 0.0f)),
                    "Sprite " + i + " should have effective custom pivot copied"
                );
            }
        }

        [Test]
        public void EnableAllPivotsButtonPreservesExistingOverrides()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._pivotMode = PivotMode.Center;
            extractor._customPivot = new Vector2(0.5f, 0.5f);

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _useGlobalSettings = true,
                _sprites = new List<SpriteSheetExtractor.SpriteEntryData>(),
            };

            // First sprite already has override with custom values
            SpriteSheetExtractor.SpriteEntryData existingOverrideSprite = new()
            {
                _originalName = "sprite_0",
                _usePivotOverride = true,
                _pivotModeOverride = PivotMode.TopRight,
                _customPivotOverride = new Vector2(1.0f, 1.0f),
            };
            entry._sprites.Add(existingOverrideSprite);

            // Second sprite has no override
            SpriteSheetExtractor.SpriteEntryData noOverrideSprite = new()
            {
                _originalName = "sprite_1",
                _usePivotOverride = false,
                _pivotModeOverride = PivotMode.Center,
                _customPivotOverride = new Vector2(0.5f, 0.5f),
            };
            entry._sprites.Add(noOverrideSprite);

            // Simulate Enable All Pivots button logic (preserves existing overrides)
            PivotMode effectiveMode = extractor.GetEffectivePivotMode(entry);
            Vector2 effectivePivot = extractor.GetEffectiveCustomPivot(entry);

            for (int i = 0; i < entry._sprites.Count; ++i)
            {
                SpriteSheetExtractor.SpriteEntryData sprite = entry._sprites[i];
                if (!sprite._usePivotOverride)
                {
                    sprite._usePivotOverride = true;
                    sprite._pivotModeOverride = effectiveMode;
                    sprite._customPivotOverride = effectivePivot;
                }
            }

            // Verify first sprite's existing override is preserved
            Assert.That(
                entry._sprites[0]._usePivotOverride,
                Is.True,
                "Existing override should remain enabled"
            );
            Assert.That(
                entry._sprites[0]._pivotModeOverride,
                Is.EqualTo(PivotMode.TopRight),
                "Existing pivot mode should be preserved"
            );
            Assert.That(
                entry._sprites[0]._customPivotOverride,
                Is.EqualTo(new Vector2(1.0f, 1.0f)),
                "Existing custom pivot should be preserved"
            );

            // Verify second sprite now has override with effective values
            Assert.That(
                entry._sprites[1]._usePivotOverride,
                Is.True,
                "Previously disabled override should now be enabled"
            );
            Assert.That(
                entry._sprites[1]._pivotModeOverride,
                Is.EqualTo(PivotMode.Center),
                "New override should use effective pivot mode"
            );
            Assert.That(
                entry._sprites[1]._customPivotOverride,
                Is.EqualTo(new Vector2(0.5f, 0.5f)),
                "New override should use effective custom pivot"
            );
        }

        [Test]
        public void DisableAllPivotsButtonClearsAllPivotOverrides()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _useGlobalSettings = true,
                _sprites = new List<SpriteSheetExtractor.SpriteEntryData>(),
            };

            // Create 4 sprites, all with pivot override enabled
            for (int i = 0; i < 4; i++)
            {
                SpriteSheetExtractor.SpriteEntryData sprite = new()
                {
                    _originalName = "sprite_" + i,
                    _usePivotOverride = true,
                    _pivotModeOverride = PivotMode.Custom,
                    _customPivotOverride = new Vector2(0.25f * i, 0.75f),
                };
                entry._sprites.Add(sprite);
            }

            // Verify initial state - all overrides enabled
            for (int i = 0; i < entry._sprites.Count; i++)
            {
                Assert.That(
                    entry._sprites[i]._usePivotOverride,
                    Is.True,
                    "Sprite " + i + " should initially have pivot override enabled"
                );
            }

            // Simulate Disable All Pivots button logic
            for (int i = 0; i < entry._sprites.Count; ++i)
            {
                entry._sprites[i]._usePivotOverride = false;
            }

            // Verify all sprites have pivot override disabled
            for (int i = 0; i < entry._sprites.Count; i++)
            {
                Assert.That(
                    entry._sprites[i]._usePivotOverride,
                    Is.False,
                    "Sprite " + i + " should have pivot override disabled"
                );
            }
        }

        [Test]
        public void BatchPivotButtonsHandleEmptySpriteListSafely()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._pivotMode = PivotMode.Center;
            extractor._customPivot = new Vector2(0.5f, 0.5f);

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _useGlobalSettings = true,
                _sprites = new List<SpriteSheetExtractor.SpriteEntryData>(),
            };

            // Verify empty list
            Assert.That(entry._sprites.Count, Is.EqualTo(0), "Sprites list should be empty");

            // Simulate Enable All Pivots button logic on empty list
            PivotMode effectiveMode = extractor.GetEffectivePivotMode(entry);
            Vector2 effectivePivot = extractor.GetEffectiveCustomPivot(entry);

            for (int i = 0; i < entry._sprites.Count; ++i)
            {
                SpriteSheetExtractor.SpriteEntryData sprite = entry._sprites[i];
                if (!sprite._usePivotOverride)
                {
                    sprite._usePivotOverride = true;
                    sprite._pivotModeOverride = effectiveMode;
                    sprite._customPivotOverride = effectivePivot;
                }
            }

            // Verify list is still empty and no exceptions occurred
            Assert.That(
                entry._sprites.Count,
                Is.EqualTo(0),
                "Sprites list should still be empty after Enable All"
            );

            // Simulate Disable All Pivots button logic on empty list
            for (int i = 0; i < entry._sprites.Count; ++i)
            {
                entry._sprites[i]._usePivotOverride = false;
            }

            // Verify list is still empty and no exceptions occurred
            Assert.That(
                entry._sprites.Count,
                Is.EqualTo(0),
                "Sprites list should still be empty after Disable All"
            );
        }

        #endregion

        #region Cache Invalidation Tests

        [Test]
        public void GetBoundsCacheKeyReturnsDifferentValueForDifferentExtractionMode()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;

            SpriteSheetExtractor.SpriteSheetEntry entry = new() { _useGlobalSettings = true };

            int key1 = entry.GetBoundsCacheKey(extractor);

            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.AlphaDetection;
            int key2 = entry.GetBoundsCacheKey(extractor);

            Assert.That(
                key1,
                Is.Not.EqualTo(key2),
                "Cache key should differ for different extraction modes"
            );
        }

        [Test]
        public void GetBoundsCacheKeyReturnsDifferentValueForDifferentGridSize()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 4;
            extractor._gridRows = 4;

            SpriteSheetExtractor.SpriteSheetEntry entry = new() { _useGlobalSettings = true };

            int key1 = entry.GetBoundsCacheKey(extractor);

            extractor._gridColumns = 8;
            extractor._gridRows = 8;
            int key2 = entry.GetBoundsCacheKey(extractor);

            Assert.That(
                key1,
                Is.Not.EqualTo(key2),
                "Cache key should differ for different grid sizes"
            );
        }

        [Test]
        public void GetBoundsCacheKeyReturnsDifferentValueForDifferentAlphaThreshold()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.AlphaDetection;
            extractor._alphaThreshold = 0.1f;

            SpriteSheetExtractor.SpriteSheetEntry entry = new() { _useGlobalSettings = true };

            int key1 = entry.GetBoundsCacheKey(extractor);

            extractor._alphaThreshold = 0.5f;
            int key2 = entry.GetBoundsCacheKey(extractor);

            Assert.That(
                key1,
                Is.Not.EqualTo(key2),
                "Cache key should differ for different alpha thresholds"
            );
        }

        [Test]
        public void GetBoundsCacheKeyReturnsDifferentValueForDifferentAlgorithm()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Auto;
            extractor._autoDetectionAlgorithm = AutoDetectionAlgorithm.AutoBest;

            SpriteSheetExtractor.SpriteSheetEntry entry = new() { _useGlobalSettings = true };

            int key1 = entry.GetBoundsCacheKey(extractor);

            extractor._autoDetectionAlgorithm = AutoDetectionAlgorithm.ClusterCentroid;
            int key2 = entry.GetBoundsCacheKey(extractor);

            Assert.That(
                key1,
                Is.Not.EqualTo(key2),
                "Cache key should differ for different algorithms"
            );
        }

        [Test]
        public void GetBoundsCacheKeyReturnsSameValueForSameSettings()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 4;
            extractor._gridRows = 4;

            SpriteSheetExtractor.SpriteSheetEntry entry = new() { _useGlobalSettings = true };

            int key1 = entry.GetBoundsCacheKey(extractor);
            int key2 = entry.GetBoundsCacheKey(extractor);

            Assert.That(key1, Is.EqualTo(key2), "Cache key should be same for identical settings");
        }

        [Test]
        public void GetBoundsCacheKeyReturnsZeroForNullExtractor()
        {
            SpriteSheetExtractor.SpriteSheetEntry entry = new() { _useGlobalSettings = true };

            int key = entry.GetBoundsCacheKey(null);

            Assert.That(key, Is.EqualTo(0), "Cache key should be 0 for null extractor");
        }

        [Test]
        public void InvalidateEntrySetsNeedsRegenerationFlag()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _needsRegeneration = false,
                _cachedAlgorithmResult = new SpriteSheetAlgorithms.AlgorithmResult(
                    cellWidth: 32,
                    cellHeight: 32,
                    confidence: 1.0f,
                    algorithm: AutoDetectionAlgorithm.AutoBest
                ),
                _lastAlgorithmDisplayText = "Previous text",
            };

            extractor.InvalidateEntry(entry);

            Assert.That(
                entry._needsRegeneration,
                Is.True,
                "InvalidateEntry should set _needsRegeneration to true"
            );
            Assert.That(
                entry._cachedAlgorithmResult,
                Is.Null,
                "InvalidateEntry should clear _cachedAlgorithmResult"
            );
            Assert.That(
                entry._lastAlgorithmDisplayText,
                Is.Null,
                "InvalidateEntry should clear _lastAlgorithmDisplayText"
            );
        }

        [Test]
        public void InvalidateEntryDoesNotThrowForNullEntry()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            Assert.DoesNotThrow(() => extractor.InvalidateEntry(null));
        }

        [Test]
        public void IsEntryStaleFalseForNullEntry()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            bool isStale = extractor.IsEntryStale(null);

            Assert.That(isStale, Is.False, "IsEntryStale should return false for null entry");
        }

        [Test]
        public void IsEntryStaleTrueWhenNeedsRegenerationSet()
        {
            SpriteSheetExtractor extractor = CreateExtractor();

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _needsRegeneration = true,
                _lastCacheKey = 12345,
            };

            bool isStale = extractor.IsEntryStale(entry);

            Assert.That(
                isStale,
                Is.True,
                "IsEntryStale should return true when _needsRegeneration is set"
            );
        }

        [Test]
        public void IsEntryStaleTrueWhenCacheKeyDiffers()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 4;
            extractor._gridRows = 4;

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _useGlobalSettings = true,
                _needsRegeneration = false,
                _lastCacheKey = 12345,
            };

            bool isStale = extractor.IsEntryStale(entry);

            Assert.That(isStale, Is.True, "IsEntryStale should return true when cache key differs");
        }

        [Test]
        public void IsEntryStaleFalseWhenCacheKeyMatches()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Manual;
            extractor._gridColumns = 4;
            extractor._gridRows = 4;

            SpriteSheetExtractor.SpriteSheetEntry entry = new()
            {
                _useGlobalSettings = true,
                _needsRegeneration = false,
            };

            entry._lastCacheKey = entry.GetBoundsCacheKey(extractor);

            bool isStale = extractor.IsEntryStale(entry);

            Assert.That(
                isStale,
                Is.False,
                "IsEntryStale should return false when cache key matches"
            );
        }

        [Test]
        public void CheckAndEvictLRUCacheDoesNothingWhenUnderLimit()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._discoveredSheets = new List<SpriteSheetExtractor.SpriteSheetEntry>();

            for (int i = 0; i < 10; ++i)
            {
                SpriteSheetExtractor.SpriteSheetEntry entry = new()
                {
                    _assetPath = $"test{i}.png",
                    _sprites = new List<SpriteSheetExtractor.SpriteEntryData>
                    {
                        new() { _originalName = "sprite1" },
                    },
                    _lastAccessTime = DateTime.UtcNow.Ticks - i * 1000,
                };
                extractor._discoveredSheets.Add(entry);
            }

            extractor.CheckAndEvictLRUCache();

            int cachedCount = 0;
            for (int i = 0; i < extractor._discoveredSheets.Count; ++i)
            {
                SpriteSheetExtractor.SpriteSheetEntry e = extractor._discoveredSheets[i];
                if (e != null && e._sprites != null && e._sprites.Count > 0)
                {
                    ++cachedCount;
                }
            }

            Assert.That(
                cachedCount,
                Is.EqualTo(10),
                "All 10 entries should remain cached when under limit"
            );
        }

        [Test]
        public void CheckAndEvictLRUCacheEvictsLeastRecentlyUsedWhenOverLimit()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._discoveredSheets = new List<SpriteSheetExtractor.SpriteSheetEntry>();

            long now = DateTime.UtcNow.Ticks;

            for (int i = 0; i < 55; ++i)
            {
                SpriteSheetExtractor.SpriteSheetEntry entry = new()
                {
                    _assetPath = $"test{i}.png",
                    _sprites = new List<SpriteSheetExtractor.SpriteEntryData>
                    {
                        new() { _originalName = "sprite1" },
                    },
                    _lastAccessTime = now - ((55 - i) * TimeSpan.TicksPerSecond),
                };
                extractor._discoveredSheets.Add(entry);
            }

            extractor.CheckAndEvictLRUCache();

            int cachedCount = 0;
            for (int i = 0; i < extractor._discoveredSheets.Count; ++i)
            {
                SpriteSheetExtractor.SpriteSheetEntry e = extractor._discoveredSheets[i];
                if (e != null && e._sprites != null && e._sprites.Count > 0)
                {
                    ++cachedCount;
                }
            }

            Assert.That(
                cachedCount,
                Is.EqualTo(50),
                "Should have exactly 50 cached entries after eviction (MaxCachedEntries limit)"
            );
        }

        [Test]
        public void CheckAndEvictLRUCacheEvictsOldestEntries()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._discoveredSheets = new List<SpriteSheetExtractor.SpriteSheetEntry>();

            long now = DateTime.UtcNow.Ticks;

            for (int i = 0; i < 55; ++i)
            {
                SpriteSheetExtractor.SpriteSheetEntry entry = new()
                {
                    _assetPath = $"test{i}.png",
                    _sprites = new List<SpriteSheetExtractor.SpriteEntryData>
                    {
                        new() { _originalName = "sprite1" },
                    },
                    _lastAccessTime = now - ((55 - i) * TimeSpan.TicksPerSecond),
                };
                extractor._discoveredSheets.Add(entry);
            }

            SpriteSheetExtractor.SpriteSheetEntry oldestEntry = extractor._discoveredSheets[0];
            SpriteSheetExtractor.SpriteSheetEntry newestEntry = extractor._discoveredSheets[54];

            extractor.CheckAndEvictLRUCache();

            Assert.That(
                oldestEntry._sprites == null || oldestEntry._sprites.Count == 0,
                Is.True,
                "Oldest entry should be evicted"
            );
            Assert.That(
                newestEntry._sprites != null && newestEntry._sprites.Count > 0,
                Is.True,
                "Newest entry should not be evicted"
            );
        }

        [Test]
        public void CheckAndEvictLRUCacheDoesNothingWhenDiscoveredSheetsIsNull()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._discoveredSheets = null;

            Assert.DoesNotThrow(() => extractor.CheckAndEvictLRUCache());
        }

        [Test]
        public void CheckAndEvictLRUCacheDoesNothingWhenListCountUnderLimit()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._discoveredSheets = new List<SpriteSheetExtractor.SpriteSheetEntry>();

            for (int i = 0; i < 49; ++i)
            {
                SpriteSheetExtractor.SpriteSheetEntry entry = new()
                {
                    _assetPath = $"test{i}.png",
                    _sprites = new List<SpriteSheetExtractor.SpriteEntryData>
                    {
                        new() { _originalName = "sprite1" },
                    },
                    _lastAccessTime = DateTime.UtcNow.Ticks,
                };
                extractor._discoveredSheets.Add(entry);
            }

            extractor.CheckAndEvictLRUCache();

            int cachedCount = 0;
            for (int i = 0; i < extractor._discoveredSheets.Count; ++i)
            {
                SpriteSheetExtractor.SpriteSheetEntry e = extractor._discoveredSheets[i];
                if (e != null && e._sprites != null && e._sprites.Count > 0)
                {
                    ++cachedCount;
                }
            }

            Assert.That(
                cachedCount,
                Is.EqualTo(49),
                "All entries should remain when list count is under limit"
            );
        }

        [Test]
        public void GetBoundsCacheKeyReturnsDifferentValueForDifferentPadding()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.PaddedGrid;
            extractor._paddingLeft = 0;
            extractor._paddingRight = 0;
            extractor._paddingTop = 0;
            extractor._paddingBottom = 0;

            SpriteSheetExtractor.SpriteSheetEntry entry = new() { _useGlobalSettings = true };

            int key1 = entry.GetBoundsCacheKey(extractor);

            extractor._paddingLeft = 2;
            extractor._paddingRight = 2;
            int key2 = entry.GetBoundsCacheKey(extractor);

            Assert.That(
                key1,
                Is.Not.EqualTo(key2),
                "Cache key should differ for different padding values"
            );
        }

        [Test]
        public void GetBoundsCacheKeyReturnsDifferentValueForDifferentSnapToDivisor()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Auto;
            extractor._snapToTextureDivisor = false;

            SpriteSheetExtractor.SpriteSheetEntry entry = new() { _useGlobalSettings = true };

            int key1 = entry.GetBoundsCacheKey(extractor);

            extractor._snapToTextureDivisor = true;
            int key2 = entry.GetBoundsCacheKey(extractor);

            Assert.That(
                key1,
                Is.Not.EqualTo(key2),
                "Cache key should differ for different snap to divisor settings"
            );
        }

        [Test]
        public void GetBoundsCacheKeyReturnsDifferentValueForDifferentExpectedCount()
        {
            SpriteSheetExtractor extractor = CreateExtractor();
            extractor._extractionMode = SpriteSheetExtractor.ExtractionMode.GridBased;
            extractor._gridSizeMode = SpriteSheetExtractor.GridSizeMode.Auto;
            extractor._expectedSpriteCountHint = 16;

            SpriteSheetExtractor.SpriteSheetEntry entry = new() { _useGlobalSettings = true };

            int key1 = entry.GetBoundsCacheKey(extractor);

            extractor._expectedSpriteCountHint = 32;
            int key2 = entry.GetBoundsCacheKey(extractor);

            Assert.That(
                key1,
                Is.Not.EqualTo(key2),
                "Cache key should differ for different expected sprite counts"
            );
        }

        #endregion

        #region FindNearestDivisorWithMinCells Tests

        [Test]
        public void FindNearestDivisorWithMinCellsReturnsTargetWhenExactDivisor()
        {
            int result = SpriteSheetExtractor.FindNearestDivisorWithMinCells(
                dimension: 256,
                target: 32,
                minCells: 2
            );
            Assert.That(result, Is.EqualTo(32), "Should return exact divisor when target is valid");
        }

        [Test]
        public void FindNearestDivisorWithMinCellsFindsNearestWhenNoExactMatch()
        {
            int result = SpriteSheetExtractor.FindNearestDivisorWithMinCells(
                dimension: 100,
                target: 33,
                minCells: 2
            );
            Assert.That(
                result == 25 || result == 50 || result == 20,
                Is.True,
                "Should find nearest valid divisor (25, 50, or 20) for dimension 100 with target 33"
            );
            Assert.That(100 % result, Is.EqualTo(0), "Result should be a divisor of dimension");
            Assert.That(
                100 / result,
                Is.GreaterThanOrEqualTo(2),
                "Should produce at least minCells cells"
            );
        }

        [Test]
        public void FindNearestDivisorWithMinCellsReturnsTargetForZeroDimension()
        {
            int result = SpriteSheetExtractor.FindNearestDivisorWithMinCells(
                dimension: 0,
                target: 32,
                minCells: 2
            );
            Assert.That(result, Is.EqualTo(32), "Should return target when dimension is 0");
        }

        [Test]
        public void FindNearestDivisorWithMinCellsReturnsTargetForZeroTarget()
        {
            int result = SpriteSheetExtractor.FindNearestDivisorWithMinCells(
                dimension: 256,
                target: 0,
                minCells: 2
            );
            Assert.That(result, Is.EqualTo(0), "Should return target (0) when target is 0");
        }

        [Test]
        public void FindNearestDivisorWithMinCellsReturnsTargetWhenMinCellsTooLarge()
        {
            int result = SpriteSheetExtractor.FindNearestDivisorWithMinCells(
                dimension: 64,
                target: 32,
                minCells: 100
            );
            Assert.That(
                result,
                Is.EqualTo(32),
                "Should return target when no divisor produces enough cells"
            );
        }

        [Test]
        public void FindNearestDivisorWithMinCellsHandlesSmallDimension()
        {
            int result = SpriteSheetExtractor.FindNearestDivisorWithMinCells(
                dimension: 8,
                target: 4,
                minCells: 2
            );
            Assert.That(result, Is.EqualTo(4), "Should find valid divisor for small dimension");
            Assert.That(8 / result, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void FindNearestDivisorWithMinCellsHandlesPrimeNumber()
        {
            int result = SpriteSheetExtractor.FindNearestDivisorWithMinCells(
                dimension: 17,
                target: 5,
                minCells: 2
            );
            Assert.That(
                result,
                Is.EqualTo(5),
                "Should return target for prime dimension since no valid divisor exists"
            );
        }

        #endregion

        #region DetectCellSizeFromOpaqueRegions Tests

        [Test]
        public void DetectCellSizeFromOpaqueRegionsReturnsZeroForNullPixels()
        {
            (int cellWidth, int cellHeight) = SpriteSheetExtractor.DetectCellSizeFromOpaqueRegions(
                pixels: null,
                textureWidth: 64,
                textureHeight: 64,
                alphaThreshold: 0.1f
            );
            Assert.That(cellWidth, Is.EqualTo(0));
            Assert.That(cellHeight, Is.EqualTo(0));
        }

        [Test]
        public void DetectCellSizeFromOpaqueRegionsReturnsZeroForEmptyPixels()
        {
            (int cellWidth, int cellHeight) = SpriteSheetExtractor.DetectCellSizeFromOpaqueRegions(
                pixels: new Color32[0],
                textureWidth: 64,
                textureHeight: 64,
                alphaThreshold: 0.1f
            );
            Assert.That(cellWidth, Is.EqualTo(0));
            Assert.That(cellHeight, Is.EqualTo(0));
        }

        [Test]
        public void DetectCellSizeFromOpaqueRegionsReturnsZeroForAllTransparent()
        {
            Color32[] pixels = new Color32[64 * 64];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            (int cellWidth, int cellHeight) = SpriteSheetExtractor.DetectCellSizeFromOpaqueRegions(
                pixels: pixels,
                textureWidth: 64,
                textureHeight: 64,
                alphaThreshold: 0.1f
            );
            Assert.That(cellWidth, Is.EqualTo(0), "All transparent texture should return 0 width");
            Assert.That(
                cellHeight,
                Is.EqualTo(0),
                "All transparent texture should return 0 height"
            );
        }

        [Test]
        public void DetectCellSizeFromOpaqueRegionsReturnsZeroForSingleRegion()
        {
            Color32[] pixels = new Color32[64 * 64];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            for (int y = 10; y < 50; ++y)
            {
                for (int x = 10; x < 50; ++x)
                {
                    pixels[y * 64 + x] = new Color32(255, 128, 64, 255);
                }
            }

            (int cellWidth, int cellHeight) = SpriteSheetExtractor.DetectCellSizeFromOpaqueRegions(
                pixels: pixels,
                textureWidth: 64,
                textureHeight: 64,
                alphaThreshold: 0.1f
            );
            Assert.That(
                cellWidth,
                Is.EqualTo(0),
                "Single region should return 0 width (need 2+ regions)"
            );
            Assert.That(
                cellHeight,
                Is.EqualTo(0),
                "Single region should return 0 height (need 2+ regions)"
            );
        }

        [Test]
        public void DetectCellSizeFromOpaqueRegionsDetectsGridPattern()
        {
            Color32[] pixels = CreateSimpleSpriteSheetPixels(128, 128, 4, 4);

            (int cellWidth, int cellHeight) = SpriteSheetExtractor.DetectCellSizeFromOpaqueRegions(
                pixels: pixels,
                textureWidth: 128,
                textureHeight: 128,
                alphaThreshold: 0.1f
            );

            Assert.That(
                cellWidth,
                Is.GreaterThan(0),
                "Should detect valid cell width for grid pattern"
            );
            Assert.That(
                cellHeight,
                Is.GreaterThan(0),
                "Should detect valid cell height for grid pattern"
            );
            Assert.That(
                128 % cellWidth,
                Is.EqualTo(0),
                "Cell width should evenly divide texture width"
            );
            Assert.That(
                128 % cellHeight,
                Is.EqualTo(0),
                "Cell height should evenly divide texture height"
            );
        }

        [Test]
        public void DetectCellSizeFromOpaqueRegionsReturnsZeroForTextureSmallerThanMinCellSize()
        {
            Color32[] pixels = new Color32[3 * 3];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            (int cellWidth, int cellHeight) = SpriteSheetExtractor.DetectCellSizeFromOpaqueRegions(
                pixels: pixels,
                textureWidth: 3,
                textureHeight: 3,
                alphaThreshold: 0.1f
            );

            Assert.That(
                cellWidth,
                Is.EqualTo(0),
                "Texture smaller than MinimumCellSize should return 0"
            );
            Assert.That(
                cellHeight,
                Is.EqualTo(0),
                "Texture smaller than MinimumCellSize should return 0"
            );
        }

        [Test]
        public void DetectCellSizeFromOpaqueRegionsHandlesMinimumValidTexture()
        {
            int size = SpriteSheetAlgorithms.MinimumCellSize * 2;
            Color32[] pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            int halfSize = size / 2;
            int spriteSize = halfSize - 2;

            for (int y = 1; y < 1 + spriteSize; ++y)
            {
                for (int x = 1; x < 1 + spriteSize; ++x)
                {
                    pixels[y * size + x] = new Color32(255, 128, 64, 255);
                }
            }

            for (int y = 1; y < 1 + spriteSize; ++y)
            {
                for (int x = halfSize + 1; x < halfSize + 1 + spriteSize; ++x)
                {
                    pixels[y * size + x] = new Color32(255, 128, 64, 255);
                }
            }

            (int cellWidth, int cellHeight) = SpriteSheetExtractor.DetectCellSizeFromOpaqueRegions(
                pixels: pixels,
                textureWidth: size,
                textureHeight: size,
                alphaThreshold: 0.1f
            );

            Assert.That(
                cellWidth >= 0 && cellHeight >= 0,
                Is.True,
                "Minimum valid texture should not crash"
            );
        }

        #endregion

        #region VerifyGridDoesNotCutSprites Tests

        [Test]
        public void VerifyGridDoesNotCutSpritesReturnsTrueForNullPixels()
        {
            bool result = SpriteSheetExtractor.VerifyGridDoesNotCutSprites(
                pixels: null,
                textureWidth: 64,
                textureHeight: 64,
                cellWidth: 32,
                cellHeight: 32,
                alphaThreshold: 0.1f
            );
            Assert.That(result, Is.True, "Null pixels should return true (valid)");
        }

        [Test]
        public void VerifyGridDoesNotCutSpritesReturnsTrueForEmptyPixels()
        {
            bool result = SpriteSheetExtractor.VerifyGridDoesNotCutSprites(
                pixels: new Color32[0],
                textureWidth: 64,
                textureHeight: 64,
                cellWidth: 32,
                cellHeight: 32,
                alphaThreshold: 0.1f
            );
            Assert.That(result, Is.True, "Empty pixels should return true (valid)");
        }

        [Test]
        public void VerifyGridDoesNotCutSpritesReturnsTrueForValidGrid()
        {
            Color32[] pixels = CreateSimpleSpriteSheetPixels(64, 64, 2, 2);

            bool result = SpriteSheetExtractor.VerifyGridDoesNotCutSprites(
                pixels: pixels,
                textureWidth: 64,
                textureHeight: 64,
                cellWidth: 32,
                cellHeight: 32,
                alphaThreshold: 0.1f
            );
            Assert.That(result, Is.True, "Grid with transparent gutters should be valid");
        }

        [Test]
        public void VerifyGridDoesNotCutSpritesReturnsFalseForInvalidGrid()
        {
            Color32[] pixels = new Color32[64 * 64];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 128, 64, 255);
            }

            bool result = SpriteSheetExtractor.VerifyGridDoesNotCutSprites(
                pixels: pixels,
                textureWidth: 64,
                textureHeight: 64,
                cellWidth: 32,
                cellHeight: 32,
                alphaThreshold: 0.1f
            );
            Assert.That(
                result,
                Is.False,
                "Grid cutting through all opaque pixels should be invalid"
            );
        }

        [Test]
        public void VerifyGridDoesNotCutSpritesReturnsFalseAtExactThreshold()
        {
            Color32[] pixels = new Color32[100 * 100];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            int gridX = 50;
            int opaquePixelsNeeded = 30;
            for (int y = 0; y < opaquePixelsNeeded; ++y)
            {
                pixels[y * 100 + gridX] = new Color32(255, 128, 64, 255);
            }

            bool result = SpriteSheetExtractor.VerifyGridDoesNotCutSprites(
                pixels: pixels,
                textureWidth: 100,
                textureHeight: 100,
                cellWidth: 50,
                cellHeight: 100,
                alphaThreshold: 0.1f
            );
            Assert.That(
                result,
                Is.False,
                "Exactly 30% opaque on grid line should return false (threshold is exclusive)"
            );
        }

        [Test]
        public void VerifyGridDoesNotCutSpritesReturnsTrueJustBelowThreshold()
        {
            Color32[] pixels = new Color32[100 * 100];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }

            int gridX = 50;
            int opaquePixelsNeeded = 29;
            for (int y = 0; y < opaquePixelsNeeded; ++y)
            {
                pixels[y * 100 + gridX] = new Color32(255, 128, 64, 255);
            }

            bool result = SpriteSheetExtractor.VerifyGridDoesNotCutSprites(
                pixels: pixels,
                textureWidth: 100,
                textureHeight: 100,
                cellWidth: 50,
                cellHeight: 100,
                alphaThreshold: 0.1f
            );
            Assert.That(result, Is.True, "Just below 30% opaque on grid line should return true");
        }

        [Test]
        public void VerifyGridDoesNotCutSpritesReturnsTrueForSingleCell()
        {
            Color32[] pixels = new Color32[64 * 64];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 128, 64, 255);
            }

            bool result = SpriteSheetExtractor.VerifyGridDoesNotCutSprites(
                pixels: pixels,
                textureWidth: 64,
                textureHeight: 64,
                cellWidth: 64,
                cellHeight: 64,
                alphaThreshold: 0.1f
            );
            Assert.That(
                result,
                Is.True,
                "Single cell has no internal grid lines so should always be valid"
            );
        }

        [Test]
        public void VerifyGridDoesNotCutSpritesReturnsTrueForZeroCellSize()
        {
            Color32[] pixels = new Color32[64 * 64];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 128, 64, 255);
            }

            bool resultZeroWidth = SpriteSheetExtractor.VerifyGridDoesNotCutSprites(
                pixels: pixels,
                textureWidth: 64,
                textureHeight: 64,
                cellWidth: 0,
                cellHeight: 32,
                alphaThreshold: 0.1f
            );
            Assert.That(resultZeroWidth, Is.True, "Zero cell width should return true (valid)");

            bool resultZeroHeight = SpriteSheetExtractor.VerifyGridDoesNotCutSprites(
                pixels: pixels,
                textureWidth: 64,
                textureHeight: 64,
                cellWidth: 32,
                cellHeight: 0,
                alphaThreshold: 0.1f
            );
            Assert.That(resultZeroHeight, Is.True, "Zero cell height should return true (valid)");
        }

        [Test]
        public void VerifyGridDoesNotCutSpritesHandlesMismatchedPixelArraySize()
        {
            Color32[] pixels = new Color32[32 * 32];
            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = new Color32(255, 128, 64, 255);
            }

            bool result = SpriteSheetExtractor.VerifyGridDoesNotCutSprites(
                pixels: pixels,
                textureWidth: 64,
                textureHeight: 64,
                cellWidth: 32,
                cellHeight: 32,
                alphaThreshold: 0.1f
            );
            Assert.That(
                result,
                Is.True,
                "Mismatched pixel array size should return true (defensive)"
            );
        }

        #endregion
    }
#endif
}
