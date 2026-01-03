// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Windows
{
#if UNITY_EDITOR
    using System.Collections.Generic;
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor;
    using WallstopStudios.UnityHelpers.Editor.AssetProcessors;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using WallstopStudios.UnityHelpers.Tests.Core.TestUtils;

    public sealed class FitTextureSizeWindowTests : CommonTestBase
    {
        private const string Root = "Assets/Temp/FitTextureSizeTests";

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
            // Reset the DetectAssetChangeProcessor to avoid triggering loop protection
            // when running many texture-related tests in succession
            DetectAssetChangeProcessor.ResetForTesting();
            EnsureFolder(Root);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            // Clean up only tracked folders/assets that this test created
            CleanupTrackedFoldersAndAssets();
        }

        [Test]
        public void GrowOnlyRaisesToNextPowerOfTwo()
        {
            string path = Path.Combine(Root, "grow.png").SanitizePath();
            CreatePng(path, 300, 100, Color.magenta);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null, "Importer should exist");
            imp.maxTextureSize = 128;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.GrowOnly;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            int count = window.CalculateTextureChanges(true);
            Assert.That(count, Is.GreaterThanOrEqualTo(1), "Expected at least one change");

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            Assert.That(
                imp.maxTextureSize,
                Is.EqualTo(512),
                "Max size should increase to next POT >= largest dimension"
            );
        }

        [Test]
        public void ShrinkOnlyReducesToTightPowerOfTwo()
        {
            string path = Path.Combine(Root, "shrink.png").SanitizePath();
            CreatePng(path, 300, 100, Color.cyan);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null, "Importer should exist");
            imp.maxTextureSize = 2048;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.ShrinkOnly;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            int count = window.CalculateTextureChanges(true);
            Assert.That(count, Is.GreaterThanOrEqualTo(1), "Expected at least one change");

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            // 300 pixels requires 512 POT to fit; shrink from 2048 to 512
            Assert.That(
                imp.maxTextureSize,
                Is.EqualTo(512),
                "Max size should shrink to tight POT that fits the source (512 >= 300)"
            );
        }

        [Test]
        public void ShrinkOnlyKeepsExactPowerOfTwo()
        {
            string path = Path.Combine(Root, "shrinkExact.png").SanitizePath();
            CreatePng(path, 256, 128, Color.yellow);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null, "Importer should exist");
            imp.maxTextureSize = 1024;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.ShrinkOnly;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };
            int count = window.CalculateTextureChanges(true);
            Assert.That(count, Is.GreaterThanOrEqualTo(1), "Expected at least one change");

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            Assert.That(imp.maxTextureSize, Is.EqualTo(256), "Should keep exact POT");
        }

        [Test]
        public void ShrinkOnlyShrinksFromSlightlyOverPot()
        {
            string path = Path.Combine(Root, "shrinkOver.png").SanitizePath();
            CreatePng(path, 257, 64, Color.gray);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null, "Importer should exist");
            imp.maxTextureSize = 2048;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.ShrinkOnly;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };
            int count = window.CalculateTextureChanges(true);
            Assert.That(count, Is.GreaterThanOrEqualTo(1), "Expected at least one change");

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            // 257 pixels requires 512 POT to fit; shrink from 2048 to 512
            Assert.That(
                imp.maxTextureSize,
                Is.EqualTo(512),
                "Should shrink to 512 (smallest POT that fits 257)"
            );
        }

        [Test]
        public void GrowOnlyDoesNotShrinkWhenAlreadyLarge()
        {
            string path = Path.Combine(Root, "growNoChange.png").SanitizePath();
            CreatePng(path, 300, 100, Color.white);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            imp.maxTextureSize = 2048;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.GrowOnly;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };
            int count = window.CalculateTextureChanges(true);

            // Expect no change because it's already large enough (GrowOnly)
            Assert.That(count, Is.EqualTo(0));
            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            Assert.That(imp.maxTextureSize, Is.EqualTo(2048));
        }

        [Test]
        public void ClampMinRaisesToMinimum()
        {
            string path = Path.Combine(Root, "clampMin.png").SanitizePath();
            CreatePng(path, 64, 64, Color.red);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            imp.maxTextureSize = 32;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.RoundToNearest;
            window._minAllowedTextureSize = 256;
            window._maxAllowedTextureSize = 8192;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            int count = window.CalculateTextureChanges(true);
            Assert.That(count, Is.GreaterThanOrEqualTo(1));

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            Assert.That(imp.maxTextureSize, Is.EqualTo(256));
        }

        [Test]
        public void ClampMaxCapsOversize()
        {
            string path = Path.Combine(Root, "clampMax.png").SanitizePath();
            // Force next POT far above Unity cap to ensure clamp path is tested.
            CreatePng(path, 9001, 10, Color.black);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            imp.maxTextureSize = 128;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.GrowOnly;
            window._minAllowedTextureSize = 32;
            window._maxAllowedTextureSize = 8192;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            int count = window.CalculateTextureChanges(true);
            Assert.That(count, Is.GreaterThanOrEqualTo(1));

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            Assert.That(imp.maxTextureSize, Is.EqualTo(8192));
        }

        [Test]
        public void PlatformOverrideAndroidApplied()
        {
            string path = Path.Combine(Root, "android.png").SanitizePath();
            CreatePng(path, 300, 100, Color.magenta);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            imp.maxTextureSize = 128;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.RoundToNearest;
            window._applyToAndroid = true;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            _ = window.CalculateTextureChanges(true);

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            TextureImporterPlatformSettings android = imp.GetPlatformTextureSettings("Android");
            Assert.IsTrue(android.overridden);
            Assert.That(android.maxTextureSize, Is.EqualTo(256));
        }

        [Test]
        public void OnlySpritesFiltersNonSprites()
        {
            string spritePath = Path.Combine(Root, "sprite.png").SanitizePath();
            string texPath = Path.Combine(Root, "tex.png").SanitizePath();
            CreatePng(spritePath, 300, 100, Color.yellow);
            CreatePng(texPath, 300, 100, Color.cyan);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter spriteImp = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            TextureImporter texImp = AssetImporter.GetAtPath(texPath) as TextureImporter;
            Assert.IsTrue(spriteImp != null);
            Assert.IsTrue(texImp != null);
            spriteImp.textureType = TextureImporterType.Sprite;
            spriteImp.maxTextureSize = 1024;
            texImp.textureType = TextureImporterType.Default;
            texImp.maxTextureSize = 1024;
            spriteImp.SaveAndReimport();
            texImp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.ShrinkOnly;
            window._onlySprites = true;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            _ = window.CalculateTextureChanges(true);

            spriteImp = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            texImp = AssetImporter.GetAtPath(texPath) as TextureImporter;
            // 300 pixels requires 512 POT to fit; shrink from 1024 to 512
            Assert.That(spriteImp.maxTextureSize, Is.EqualTo(512));
            Assert.That(texImp.maxTextureSize, Is.EqualTo(1024));
        }

        [Test]
        public void NameFilterContainsOnlyMatches()
        {
            string heroPath = Path.Combine(Root, "hero_idle.png").SanitizePath();
            string villPath = Path.Combine(Root, "villain_idle.png").SanitizePath();
            CreatePng(heroPath, 300, 100, Color.white);
            CreatePng(villPath, 300, 100, Color.white);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter heroImp = AssetImporter.GetAtPath(heroPath) as TextureImporter;
            TextureImporter villImp = AssetImporter.GetAtPath(villPath) as TextureImporter;
            heroImp.maxTextureSize = 128;
            villImp.maxTextureSize = 128;
            heroImp.SaveAndReimport();
            villImp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.GrowOnly;
            window._nameFilter = "hero";
            window._useRegexForName = false;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            _ = window.CalculateTextureChanges(true);

            heroImp = AssetImporter.GetAtPath(heroPath) as TextureImporter;
            villImp = AssetImporter.GetAtPath(villPath) as TextureImporter;
            Assert.That(heroImp.maxTextureSize, Is.EqualTo(512));
            Assert.That(villImp.maxTextureSize, Is.EqualTo(128));
        }

        [Test]
        public void NameFilterRegexMatches()
        {
            string aPath = Path.Combine(Root, "item01.png").SanitizePath();
            string bPath = Path.Combine(Root, "itemABC.png").SanitizePath();
            CreatePng(aPath, 300, 100, Color.white);
            CreatePng(bPath, 300, 100, Color.white);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter aImp = AssetImporter.GetAtPath(aPath) as TextureImporter;
            TextureImporter bImp = AssetImporter.GetAtPath(bPath) as TextureImporter;
            aImp.maxTextureSize = 128;
            bImp.maxTextureSize = 128;
            aImp.SaveAndReimport();
            bImp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.GrowOnly;
            window._nameFilter = "^item\\d{2}$";
            window._useRegexForName = true;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            _ = window.CalculateTextureChanges(true);

            aImp = AssetImporter.GetAtPath(aPath) as TextureImporter;
            bImp = AssetImporter.GetAtPath(bPath) as TextureImporter;
            Assert.That(aImp.maxTextureSize, Is.EqualTo(512));
            Assert.That(bImp.maxTextureSize, Is.EqualTo(128));
        }

        [Test]
        public void LabelFilterMatchesOnlyLabeled()
        {
            string labeledPath = Path.Combine(Root, "labeled.png").SanitizePath();
            string unlabeledPath = Path.Combine(Root, "unlabeled.png").SanitizePath();
            CreatePng(labeledPath, 300, 100, Color.gray);
            CreatePng(unlabeledPath, 300, 100, Color.gray);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            Object labeledObj = AssetDatabase.LoadAssetAtPath<Object>(labeledPath);
            AssetDatabase.SetLabels(labeledObj, new[] { "FitMe", "TagA" });
            AssetDatabase.SaveAssets();

            TextureImporter labImp = AssetImporter.GetAtPath(labeledPath) as TextureImporter;
            TextureImporter unlabImp = AssetImporter.GetAtPath(unlabeledPath) as TextureImporter;
            labImp.maxTextureSize = 128;
            unlabImp.maxTextureSize = 128;
            labImp.SaveAndReimport();
            unlabImp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.GrowOnly;
            window._labelFilterCsv = "FitMe";
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            _ = window.CalculateTextureChanges(true);

            labImp = AssetImporter.GetAtPath(labeledPath) as TextureImporter;
            unlabImp = AssetImporter.GetAtPath(unlabeledPath) as TextureImporter;
            Assert.That(labImp.maxTextureSize, Is.EqualTo(512));
            Assert.That(unlabImp.maxTextureSize, Is.EqualTo(128));
        }

        [Test]
        public void SelectionOnlyProcessesOnlySelectedAsset()
        {
            string aPath = Path.Combine(Root, "sel_a.png").SanitizePath();
            string bPath = Path.Combine(Root, "sel_b.png").SanitizePath();
            CreatePng(aPath, 300, 100, Color.white);
            CreatePng(bPath, 300, 100, Color.white);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter aImp = AssetImporter.GetAtPath(aPath) as TextureImporter;
            TextureImporter bImp = AssetImporter.GetAtPath(bPath) as TextureImporter;
            aImp.maxTextureSize = 128;
            bImp.maxTextureSize = 128;
            aImp.SaveAndReimport();
            bImp.SaveAndReimport();

            Object aObj = AssetDatabase.LoadAssetAtPath<Object>(aPath);
            Selection.objects = new[] { aObj };

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.GrowOnly;
            window._useSelectionOnly = true;

            int count = window.CalculateTextureChanges(true);
            Assert.That(count, Is.EqualTo(1));

            aImp = AssetImporter.GetAtPath(aPath) as TextureImporter;
            bImp = AssetImporter.GetAtPath(bPath) as TextureImporter;
            Assert.That(aImp.maxTextureSize, Is.EqualTo(512));
            Assert.That(bImp.maxTextureSize, Is.EqualTo(128));
        }

        [Test]
        public void NameFilterCaseSensitivityHonored()
        {
            string path = Path.Combine(Root, "Hero.png").SanitizePath();
            CreatePng(path, 300, 100, Color.white);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            imp.maxTextureSize = 128;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            // Case-sensitive search for lower-case 'hero' should not match 'Hero'
            window._fitMode = FitMode.GrowOnly;
            window._nameFilter = "hero";
            window._caseSensitiveNameFilter = true;
            _ = window.CalculateTextureChanges(true);
            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(imp.maxTextureSize, Is.EqualTo(128));

            // Case-insensitive should match
            window._caseSensitiveNameFilter = false;
            _ = window.CalculateTextureChanges(true);
            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(imp.maxTextureSize, Is.EqualTo(512));
        }

        [Test]
        public void LabelFilterCaseSensitivityHonored()
        {
            string path = Path.Combine(Root, "labelCase.png").SanitizePath();
            CreatePng(path, 300, 100, Color.gray);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            AssetDatabase.SetLabels(obj, new[] { "FitMe" });
            AssetDatabase.SaveAssets();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            imp.maxTextureSize = 128;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.GrowOnly;
            window._labelFilterCsv = "fitme";
            window._caseSensitiveNameFilter = true;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            // Case-sensitive 'fitme' should not match 'FitMe'
            _ = window.CalculateTextureChanges(true);
            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(imp.maxTextureSize, Is.EqualTo(128));

            // Case-insensitive should match
            window._caseSensitiveNameFilter = false;
            _ = window.CalculateTextureChanges(true);
            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(imp.maxTextureSize, Is.EqualTo(512));
        }

        [Test]
        public void PlatformOverrideStandaloneApplied()
        {
            string path = Path.Combine(Root, "standalone.png").SanitizePath();
            CreatePng(path, 300, 100, Color.magenta);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            imp.maxTextureSize = 128;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.RoundToNearest;
            window._applyToStandalone = true;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            _ = window.CalculateTextureChanges(true);

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            TextureImporterPlatformSettings st = imp.GetPlatformTextureSettings("Standalone");
            Assert.IsTrue(st.overridden);
            Assert.That(st.maxTextureSize, Is.EqualTo(256));
        }

        [Test]
        public void PlatformOverrideIOSApplied()
        {
            string path = Path.Combine(Root, "ios.png").SanitizePath();
            CreatePng(path, 300, 100, Color.magenta);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            imp.maxTextureSize = 128;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.RoundToNearest;
            window._applyToiOS = true;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            _ = window.CalculateTextureChanges(true);

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            TextureImporterPlatformSettings ios = imp.GetPlatformTextureSettings("iPhone");
            Assert.IsTrue(ios.overridden);
            Assert.That(ios.maxTextureSize, Is.EqualTo(256));
        }

        [Test]
        public void MixedSelectionFoldersAndFilesWithLabelCsvOnlyLabelsFromFoldersAreProcessed()
        {
            // Prepare: one labeled texture under a folder, one unlabeled file selected directly
            string folder = Path.Combine(Root, "Sub").SanitizePath();
            EnsureFolder(folder);
            string labeledUnderFolder = Path.Combine(folder, "inFolder.png").SanitizePath();
            string directFile = Path.Combine(Root, "direct.png").SanitizePath();

            CreatePng(labeledUnderFolder, 300, 100, Color.gray);
            CreatePng(directFile, 300, 100, Color.gray);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            Object labeledObj = AssetDatabase.LoadAssetAtPath<Object>(labeledUnderFolder);
            AssetDatabase.SetLabels(labeledObj, new[] { "OnlyMe" });
            AssetDatabase.SaveAssets();

            TextureImporter folderImp =
                AssetImporter.GetAtPath(labeledUnderFolder) as TextureImporter;
            TextureImporter directImp = AssetImporter.GetAtPath(directFile) as TextureImporter;
            folderImp.maxTextureSize = 128;
            directImp.maxTextureSize = 128;
            folderImp.SaveAndReimport();
            directImp.SaveAndReimport();

            // Select folder and the direct file simultaneously
            Object folderObj = AssetDatabase.LoadAssetAtPath<Object>(folder);
            Object directObj = AssetDatabase.LoadAssetAtPath<Object>(directFile);
            Selection.objects = new[] { folderObj, directObj };

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.GrowOnly;
            window._useSelectionOnly = true;
            window._labelFilterCsv = "OnlyMe"; // case-insensitive path used by l: query
            window._caseSensitiveNameFilter = false;

            _ = window.CalculateTextureChanges(true);

            folderImp = AssetImporter.GetAtPath(labeledUnderFolder) as TextureImporter;
            directImp = AssetImporter.GetAtPath(directFile) as TextureImporter;
            // Only labeled under folder changes; direct file with no label should not change
            Assert.That(folderImp.maxTextureSize, Is.EqualTo(512));
            Assert.That(directImp.maxTextureSize, Is.EqualTo(128));
        }

        [Test]
        public void LastRunSummaryReflectsCounts()
        {
            string aPath = Path.Combine(Root, "sumA.png").SanitizePath();
            string bPath = Path.Combine(Root, "sumB.png").SanitizePath();
            // a: 300x100 -> will grow to 512 (change)
            // b: 128x128 with max=128 -> unchanged
            CreatePng(aPath, 300, 100, Color.white);
            CreatePng(bPath, 128, 128, Color.white);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter aImp = AssetImporter.GetAtPath(aPath) as TextureImporter;
            TextureImporter bImp = AssetImporter.GetAtPath(bPath) as TextureImporter;
            aImp.maxTextureSize = 128;
            bImp.maxTextureSize = 128;
            aImp.SaveAndReimport();
            bImp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.GrowOnly;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            int changed = window.CalculateTextureChanges(true);
            Assert.That(changed, Is.EqualTo(1));
            Assert.IsTrue(window._hasLastRunSummary);
            Assert.That(window._lastRunTotal, Is.EqualTo(2));
            Assert.That(window._lastRunChanged, Is.EqualTo(1));
            Assert.That(window._lastRunGrows, Is.EqualTo(1));
            Assert.That(window._lastRunShrinks, Is.EqualTo(0));
            Assert.That(window._lastRunUnchanged, Is.EqualTo(1));
        }

        [Test]
        public void RoundToNearestChoosesLowerWhenCloser()
        {
            string path = Path.Combine(Root, "roundLower.png").SanitizePath();
            // Largest dimension 300 -> nearest POT is 256 (diff 44 vs 212)
            CreatePng(path, 300, 100, Color.green);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            imp.maxTextureSize = 2048;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.RoundToNearest;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            int count = window.CalculateTextureChanges(true);
            Assert.That(count, Is.GreaterThanOrEqualTo(1));

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            Assert.That(imp.maxTextureSize, Is.EqualTo(256));
        }

        [Test]
        public void RoundToNearestRoundsUpOnTie()
        {
            string path = Path.Combine(Root, "roundUpTie.png").SanitizePath();
            // Largest dimension 384 is exactly halfway between 256 and 512; ties round up to 512
            CreatePng(path, 384, 10, Color.blue);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            imp.maxTextureSize = 128;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.RoundToNearest;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            int count = window.CalculateTextureChanges(true);
            Assert.That(count, Is.GreaterThanOrEqualTo(1));

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            Assert.That(imp.maxTextureSize, Is.EqualTo(512));
        }

        [Test]
        [TestCaseSource(nameof(GrowAndShrinkModeTestCases))]
        public void GrowAndShrinkModeCalculatesCorrectSize(
            int width,
            int height,
            int currentMaxSize,
            int expectedSize
        )
        {
            string path = Path.Combine(Root, $"growShrink_{width}x{height}.png").SanitizePath();
            CreatePng(path, width, height, Color.white);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null, "Importer should exist");
            imp.maxTextureSize = currentMaxSize;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.GrowAndShrink;
            window._minAllowedTextureSize = 1; // Allow edge case testing of very small textures
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            _ = window.CalculateTextureChanges(true);

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            Assert.That(
                imp.maxTextureSize,
                Is.EqualTo(expectedSize),
                $"Size {width}x{height} should compute to {expectedSize}"
            );
        }

        private static IEnumerable<TestCaseData> GrowAndShrinkModeTestCases()
        {
            yield return new TestCaseData(400, 240, 256, 512).SetName(
                "GrowAndShrink.400x240.OriginalBugCase.Grows512"
            );
            yield return new TestCaseData(450, 254, 256, 512).SetName(
                "GrowAndShrink.450x254.ReportedBugCase.Grows512"
            );
            yield return new TestCaseData(257, 64, 128, 512).SetName(
                "GrowAndShrink.257x64.JustOver256.Grows512"
            );
            yield return new TestCaseData(256, 256, 256, 256).SetName(
                "GrowAndShrink.256x256.ExactPOT.Stays256"
            );
            yield return new TestCaseData(255, 255, 512, 256).SetName(
                "GrowAndShrink.255x255.JustUnder256.Shrinks256"
            );
            yield return new TestCaseData(513, 400, 512, 1024).SetName(
                "GrowAndShrink.513x400.JustOver512.Grows1024"
            );
            yield return new TestCaseData(1, 1, 512, 1).SetName(
                "GrowAndShrink.1x1.Minimum.Shrinks1"
            );
            yield return new TestCaseData(1024, 1024, 1024, 1024).SetName(
                "GrowAndShrink.1024x1024.ExactPOT.Stays1024"
            );
            yield return new TestCaseData(128, 128, 2048, 128).SetName(
                "GrowAndShrink.128x128.LargerCurrent.Shrinks128"
            );
            yield return new TestCaseData(129, 1, 64, 256).SetName(
                "GrowAndShrink.129x1.Asymmetric.Grows256"
            );
            yield return new TestCaseData(64, 65, 32, 128).SetName(
                "GrowAndShrink.64x65.HeightLarger.Grows128"
            );
            yield return new TestCaseData(2, 2, 1024, 2).SetName(
                "GrowAndShrink.2x2.VerySmall.Shrinks2"
            );
            yield return new TestCaseData(2048, 2048, 1024, 2048).SetName(
                "GrowAndShrink.2048x2048.LargePOT.Grows2048"
            );
            yield return new TestCaseData(4096, 4096, 2048, 4096).SetName(
                "GrowAndShrink.4096x4096.VeryLarge.Grows4096"
            );
            yield return new TestCaseData(100, 200, 512, 256).SetName(
                "GrowAndShrink.100x200.Portrait.Shrinks256"
            );
            yield return new TestCaseData(300, 100, 1024, 512).SetName(
                "GrowAndShrink.300x100.Landscape.Shrinks512"
            );
            yield return new TestCaseData(511, 511, 256, 512).SetName(
                "GrowAndShrink.511x511.JustUnder512.Grows512"
            );
            yield return new TestCaseData(512, 1, 2048, 512).SetName(
                "GrowAndShrink.512x1.WideStrip.Shrinks512"
            );
            yield return new TestCaseData(1, 512, 64, 512).SetName(
                "GrowAndShrink.1x512.TallStrip.Grows512"
            );
        }

        [Test]
        [TestCaseSource(nameof(GrowOnlyModeTestCases))]
        public void GrowOnlyModeCalculatesCorrectSize(
            int width,
            int height,
            int currentMaxSize,
            int expectedSize
        )
        {
            string path = Path.Combine(Root, $"growOnly_{width}x{height}_{currentMaxSize}.png")
                .SanitizePath();
            CreatePng(path, width, height, Color.green);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null, "Importer should exist");
            imp.maxTextureSize = currentMaxSize;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.GrowOnly;
            window._minAllowedTextureSize = 1; // Allow edge case testing of very small textures
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            _ = window.CalculateTextureChanges(true);

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            Assert.That(
                imp.maxTextureSize,
                Is.EqualTo(expectedSize),
                $"GrowOnly with current={currentMaxSize} and size {width}x{height} should be {expectedSize}"
            );
        }

        private static IEnumerable<TestCaseData> GrowOnlyModeTestCases()
        {
            yield return new TestCaseData(400, 240, 256, 512).SetName(
                "GrowOnly.400x240.Current256.Grows512"
            );
            yield return new TestCaseData(400, 240, 512, 512).SetName(
                "GrowOnly.400x240.Current512.StaysSame"
            );
            yield return new TestCaseData(400, 240, 1024, 1024).SetName(
                "GrowOnly.400x240.Current1024.AlreadyBigEnough"
            );
            yield return new TestCaseData(100, 100, 128, 128).SetName(
                "GrowOnly.100x100.Current128.AlreadyBigEnough"
            );
            yield return new TestCaseData(257, 64, 128, 512).SetName(
                "GrowOnly.257x64.Current128.Grows512"
            );
            yield return new TestCaseData(1, 1, 64, 64).SetName(
                "GrowOnly.1x1.Current64.AlreadyBigEnough"
            );
            yield return new TestCaseData(1025, 1, 512, 2048).SetName(
                "GrowOnly.1025x1.Current512.Grows2048"
            );
            yield return new TestCaseData(64, 64, 32, 64).SetName(
                "GrowOnly.64x64.Current32.Grows64"
            );
            yield return new TestCaseData(2048, 100, 1024, 2048).SetName(
                "GrowOnly.2048x100.Current1024.Grows2048"
            );
            yield return new TestCaseData(100, 2048, 1024, 2048).SetName(
                "GrowOnly.100x2048.Portrait.Grows2048"
            );
            // Additional edge cases
            yield return new TestCaseData(256, 256, 256, 256).SetName(
                "GrowOnly.256x256.Current256.ExactPOTNoChange"
            );
            yield return new TestCaseData(256, 256, 128, 256).SetName(
                "GrowOnly.256x256.Current128.GrowsToExactPOT"
            );
            yield return new TestCaseData(1, 1, 1, 1).SetName(
                "GrowOnly.1x1.Current1.MinimumNoChange"
            );
            yield return new TestCaseData(2, 2, 1, 2).SetName("GrowOnly.2x2.Current1.GrowsTo2");
            yield return new TestCaseData(1, 512, 256, 512).SetName(
                "GrowOnly.1x512.Current256.TallStripGrows512"
            );
            yield return new TestCaseData(512, 1, 256, 512).SetName(
                "GrowOnly.512x1.Current256.WideStripGrows512"
            );
            yield return new TestCaseData(4096, 4096, 2048, 4096).SetName(
                "GrowOnly.4096x4096.Current2048.LargeGrows4096"
            );
        }

        [Test]
        [TestCaseSource(nameof(ShrinkOnlyModeTestCases))]
        public void ShrinkOnlyModeCalculatesCorrectSize(
            int width,
            int height,
            int currentMaxSize,
            int expectedSize
        )
        {
            string path = Path.Combine(Root, $"shrinkOnly_{width}x{height}_{currentMaxSize}.png")
                .SanitizePath();
            CreatePng(path, width, height, Color.blue);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null, "Importer should exist");
            imp.maxTextureSize = currentMaxSize;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.ShrinkOnly;
            window._minAllowedTextureSize = 1; // Allow edge case testing of very small textures
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            _ = window.CalculateTextureChanges(true);

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            Assert.That(
                imp.maxTextureSize,
                Is.EqualTo(expectedSize),
                $"ShrinkOnly with current={currentMaxSize} and size {width}x{height} should be {expectedSize}"
            );
        }

        private static IEnumerable<TestCaseData> ShrinkOnlyModeTestCases()
        {
            yield return new TestCaseData(400, 240, 512, 512).SetName(
                "ShrinkOnly.400x240.Current512.CantShrinkBelowNeeded"
            );
            yield return new TestCaseData(400, 240, 1024, 512).SetName(
                "ShrinkOnly.400x240.Current1024.Shrinks512"
            );
            yield return new TestCaseData(400, 240, 2048, 512).SetName(
                "ShrinkOnly.400x240.Current2048.Shrinks512"
            );
            yield return new TestCaseData(100, 100, 256, 128).SetName(
                "ShrinkOnly.100x100.Current256.Shrinks128"
            );
            yield return new TestCaseData(100, 100, 128, 128).SetName(
                "ShrinkOnly.100x100.Current128.StaysAtTight"
            );
            yield return new TestCaseData(257, 64, 512, 512).SetName(
                "ShrinkOnly.257x64.Current512.CantShrinkBelowNeeded"
            );
            yield return new TestCaseData(256, 256, 512, 256).SetName(
                "ShrinkOnly.256x256.Current512.Shrinks256"
            );
            yield return new TestCaseData(1, 1, 2048, 1).SetName(
                "ShrinkOnly.1x1.Current2048.Shrinks1"
            );
            yield return new TestCaseData(2048, 2048, 4096, 2048).SetName(
                "ShrinkOnly.2048x2048.Current4096.Shrinks2048"
            );
            yield return new TestCaseData(100, 300, 1024, 512).SetName(
                "ShrinkOnly.100x300.Portrait.Shrinks512"
            );
            yield return new TestCaseData(300, 100, 256, 256).SetName(
                "ShrinkOnly.300x100.Current256.CantGrowStays256"
            );
            // Additional edge cases - strip dimensions
            yield return new TestCaseData(1, 512, 2048, 512).SetName(
                "ShrinkOnly.1x512.Current2048.TallStripShrinks512"
            );
            yield return new TestCaseData(512, 1, 2048, 512).SetName(
                "ShrinkOnly.512x1.Current2048.WideStripShrinks512"
            );
            yield return new TestCaseData(2, 2, 1024, 2).SetName(
                "ShrinkOnly.2x2.Current1024.VerySmallShrinks2"
            );
            yield return new TestCaseData(64, 64, 64, 64).SetName(
                "ShrinkOnly.64x64.Current64.ExactPOTNoChange"
            );
            yield return new TestCaseData(4096, 4096, 8192, 4096).SetName(
                "ShrinkOnly.4096x4096.Current8192.LargeShrinks4096"
            );
            yield return new TestCaseData(255, 255, 512, 256).SetName(
                "ShrinkOnly.255x255.Current512.JustUnderPOTShrinks256"
            );
        }

        [Test]
        [TestCaseSource(nameof(RoundToNearestModeTestCases))]
        public void RoundToNearestModeCalculatesCorrectSize(
            int width,
            int height,
            int currentMaxSize,
            int expectedSize
        )
        {
            string path = Path.Combine(Root, $"roundNearest_{width}x{height}.png").SanitizePath();
            CreatePng(path, width, height, Color.red);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null, "Importer should exist");
            imp.maxTextureSize = currentMaxSize;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.RoundToNearest;
            window._minAllowedTextureSize = 1; // Allow edge case testing of very small textures
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            _ = window.CalculateTextureChanges(true);

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            Assert.That(
                imp.maxTextureSize,
                Is.EqualTo(expectedSize),
                $"RoundToNearest for size {width}x{height} should be {expectedSize}"
            );
        }

        private static IEnumerable<TestCaseData> RoundToNearestModeTestCases()
        {
            yield return new TestCaseData(400, 240, 128, 512).SetName(
                "RoundToNearest.400x240.CloserTo512.Rounds512"
            );
            yield return new TestCaseData(300, 200, 1024, 256).SetName(
                "RoundToNearest.300x200.CloserTo256.Rounds256"
            );
            yield return new TestCaseData(384, 240, 128, 512).SetName(
                "RoundToNearest.384x240.Tie.RoundsUp512"
            );
            yield return new TestCaseData(256, 256, 128, 256).SetName(
                "RoundToNearest.256x256.ExactPOT.Stays256"
            );
            yield return new TestCaseData(128, 128, 64, 128).SetName(
                "RoundToNearest.128x128.ExactPOT.Stays128"
            );
            yield return new TestCaseData(1, 1, 512, 1).SetName(
                "RoundToNearest.1x1.Minimum.Rounds1"
            );
            yield return new TestCaseData(2, 2, 1024, 2).SetName(
                "RoundToNearest.2x2.VerySmall.Rounds2"
            );
            yield return new TestCaseData(192, 100, 512, 256).SetName(
                "RoundToNearest.192x100.CloserTo256.Rounds256"
            );
            yield return new TestCaseData(200, 192, 128, 256).SetName(
                "RoundToNearest.200x192.Portrait.Rounds256"
            );
            yield return new TestCaseData(768, 500, 256, 1024).SetName(
                "RoundToNearest.768x500.Tie.RoundsUp1024"
            );
            yield return new TestCaseData(600, 400, 128, 512).SetName(
                "RoundToNearest.600x400.CloserTo512.Rounds512"
            );
            yield return new TestCaseData(1536, 1000, 512, 2048).SetName(
                "RoundToNearest.1536x1000.Tie.RoundsUp2048"
            );
            // Additional tie cases at different scales
            yield return new TestCaseData(96, 50, 256, 128).SetName(
                "RoundToNearest.96x50.Tie64To128.RoundsUp128"
            );
            yield return new TestCaseData(48, 30, 512, 64).SetName(
                "RoundToNearest.48x30.Tie32To64.RoundsUp64"
            );
            yield return new TestCaseData(3072, 2000, 1024, 4096).SetName(
                "RoundToNearest.3072x2000.Tie2048To4096.RoundsUp4096"
            );
            // Strip dimensions
            yield return new TestCaseData(1, 300, 512, 256).SetName(
                "RoundToNearest.1x300.TallStrip.CloserTo256"
            );
            yield return new TestCaseData(300, 1, 512, 256).SetName(
                "RoundToNearest.300x1.WideStrip.CloserTo256"
            );
            // Just under/over POT boundaries
            yield return new TestCaseData(257, 100, 128, 256).SetName(
                "RoundToNearest.257x100.JustOver256.CloserTo256"
            );
            yield return new TestCaseData(255, 100, 128, 256).SetName(
                "RoundToNearest.255x100.JustUnder256.CloserTo256"
            );
            yield return new TestCaseData(129, 50, 64, 128).SetName(
                "RoundToNearest.129x50.JustOver128.CloserTo128"
            );
        }

        [Test]
        [TestCaseSource(nameof(EdgeCaseTestCases))]
        public void EdgeCasesAreHandledCorrectly(
            int width,
            int height,
            FitMode mode,
            int currentMaxSize,
            int expectedSize
        )
        {
            string path = Path.Combine(Root, $"edge_{width}x{height}_{mode}.png").SanitizePath();
            CreatePng(path, width, height, Color.yellow);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null, "Importer should exist");
            imp.maxTextureSize = currentMaxSize;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = mode;
            window._minAllowedTextureSize = 1; // Allow edge case testing of very small textures
            window._maxAllowedTextureSize = 16384; // Allow testing of sizes beyond default 8192 cap
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            _ = window.CalculateTextureChanges(true);

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            Assert.That(
                imp.maxTextureSize,
                Is.EqualTo(expectedSize),
                $"Edge case {mode} for {width}x{height} should be {expectedSize}"
            );
        }

        private static IEnumerable<TestCaseData> EdgeCaseTestCases()
        {
            yield return new TestCaseData(1, 1, FitMode.GrowAndShrink, 1024, 1).SetName(
                "Edge.1x1.GrowAndShrink.MinimumSize"
            );
            yield return new TestCaseData(1, 1, FitMode.GrowOnly, 1, 1).SetName(
                "Edge.1x1.GrowOnly.NoChangeNeeded"
            );
            yield return new TestCaseData(1, 1, FitMode.ShrinkOnly, 2048, 1).SetName(
                "Edge.1x1.ShrinkOnly.ShrinksToMin"
            );
            yield return new TestCaseData(1, 1, FitMode.RoundToNearest, 512, 1).SetName(
                "Edge.1x1.RoundToNearest.RoundsToMin"
            );
            yield return new TestCaseData(2, 2, FitMode.GrowAndShrink, 256, 2).SetName(
                "Edge.2x2.GrowAndShrink.VerySmall"
            );
            yield return new TestCaseData(1000, 1, FitMode.GrowAndShrink, 512, 1024).SetName(
                "Edge.1000x1.GrowAndShrink.WideStrip"
            );
            yield return new TestCaseData(1, 1000, FitMode.GrowAndShrink, 512, 1024).SetName(
                "Edge.1x1000.GrowAndShrink.TallStrip"
            );
            yield return new TestCaseData(4096, 1, FitMode.GrowOnly, 2048, 4096).SetName(
                "Edge.4096x1.GrowOnly.VeryWide"
            );
            yield return new TestCaseData(1, 4096, FitMode.GrowOnly, 2048, 4096).SetName(
                "Edge.1x4096.GrowOnly.VeryTall"
            );
            yield return new TestCaseData(2048, 2048, FitMode.GrowAndShrink, 2048, 2048).SetName(
                "Edge.2048x2048.GrowAndShrink.LargePOT"
            );
            yield return new TestCaseData(4096, 4096, FitMode.RoundToNearest, 1024, 4096).SetName(
                "Edge.4096x4096.RoundToNearest.VeryLarge"
            );
            yield return new TestCaseData(3, 5, FitMode.GrowAndShrink, 1024, 8).SetName(
                "Edge.3x5.GrowAndShrink.OddDimensions"
            );
            yield return new TestCaseData(7, 3, FitMode.RoundToNearest, 256, 8).SetName(
                "Edge.7x3.RoundToNearest.OddDimensions"
            );
            yield return new TestCaseData(100, 500, FitMode.GrowAndShrink, 256, 512).SetName(
                "Edge.100x500.GrowAndShrink.PortraitNonSquare"
            );
            yield return new TestCaseData(500, 100, FitMode.GrowAndShrink, 256, 512).SetName(
                "Edge.500x100.GrowAndShrink.LandscapeNonSquare"
            );
            yield return new TestCaseData(1023, 1023, FitMode.GrowAndShrink, 512, 1024).SetName(
                "Edge.1023x1023.GrowAndShrink.JustUnder1024"
            );
            yield return new TestCaseData(1025, 1025, FitMode.GrowAndShrink, 1024, 2048).SetName(
                "Edge.1025x1025.GrowAndShrink.JustOver1024"
            );
            // Prime number dimensions
            yield return new TestCaseData(17, 19, FitMode.GrowAndShrink, 128, 32).SetName(
                "Edge.17x19.GrowAndShrink.SmallPrimes"
            );
            yield return new TestCaseData(31, 37, FitMode.GrowAndShrink, 256, 64).SetName(
                "Edge.31x37.GrowAndShrink.MediumPrimes"
            );
            yield return new TestCaseData(127, 131, FitMode.GrowAndShrink, 64, 256).SetName(
                "Edge.127x131.GrowAndShrink.LargerPrimes"
            );
            yield return new TestCaseData(509, 521, FitMode.RoundToNearest, 128, 512).SetName(
                "Edge.509x521.RoundToNearest.PrimesNear512"
            );
            // ShrinkOnly edge cases
            yield return new TestCaseData(1000, 1, FitMode.ShrinkOnly, 2048, 1024).SetName(
                "Edge.1000x1.ShrinkOnly.WideStrip"
            );
            yield return new TestCaseData(1, 1000, FitMode.ShrinkOnly, 2048, 1024).SetName(
                "Edge.1x1000.ShrinkOnly.TallStrip"
            );
            // Boundary tests at 8192 (common Unity max)
            yield return new TestCaseData(8192, 8192, FitMode.GrowAndShrink, 4096, 8192).SetName(
                "Edge.8192x8192.GrowAndShrink.MaxCommonSize"
            );
            yield return new TestCaseData(8193, 1, FitMode.GrowAndShrink, 4096, 16384).SetName(
                "Edge.8193x1.GrowAndShrink.JustOver8192"
            );
        }

        [Test]
        public void DefaultMinClampingPreventsVerySmallSizes()
        {
            string path = Path.Combine(Root, "defaultClamp_1x1.png").SanitizePath();
            CreatePng(path, 1, 1, Color.white);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null, "Importer should exist");
            imp.maxTextureSize = 2048;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.GrowAndShrink;
            // Intentionally NOT setting _minAllowedTextureSize to verify default clamping behavior
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            _ = window.CalculateTextureChanges(true);

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            Assert.That(
                imp.maxTextureSize,
                Is.EqualTo(32),
                "Default _minAllowedTextureSize=32 should clamp 1x1 texture to 32, not 1"
            );
        }

        [Test]
        [TestCaseSource(nameof(MinClampingTestCases))]
        public void MinClampingAppliesCorrectly(
            int width,
            int height,
            int minAllowedSize,
            int expectedSize
        )
        {
            string path = Path.Combine(Root, $"minClamp_{width}x{height}_{minAllowedSize}.png")
                .SanitizePath();
            CreatePng(path, width, height, Color.cyan);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null, "Importer should exist");
            imp.maxTextureSize = 8192;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.GrowAndShrink;
            window._minAllowedTextureSize = minAllowedSize;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            _ = window.CalculateTextureChanges(true);

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            Assert.That(
                imp.maxTextureSize,
                Is.EqualTo(expectedSize),
                $"With _minAllowedTextureSize={minAllowedSize}, {width}x{height} should result in {expectedSize}"
            );
        }

        private static IEnumerable<TestCaseData> MinClampingTestCases()
        {
            // Test min clamping with different thresholds
            yield return new TestCaseData(1, 1, 1, 1).SetName("MinClamp.1x1.MinAllowed1.Returns1");
            yield return new TestCaseData(1, 1, 16, 16).SetName(
                "MinClamp.1x1.MinAllowed16.Returns16"
            );
            yield return new TestCaseData(1, 1, 32, 32).SetName(
                "MinClamp.1x1.MinAllowed32.Returns32"
            );
            yield return new TestCaseData(1, 1, 64, 64).SetName(
                "MinClamp.1x1.MinAllowed64.Returns64"
            );
            yield return new TestCaseData(2, 2, 1, 2).SetName("MinClamp.2x2.MinAllowed1.Returns2");
            yield return new TestCaseData(2, 2, 32, 32).SetName(
                "MinClamp.2x2.MinAllowed32.Returns32"
            );
            yield return new TestCaseData(8, 8, 1, 8).SetName("MinClamp.8x8.MinAllowed1.Returns8");
            yield return new TestCaseData(8, 8, 32, 32).SetName(
                "MinClamp.8x8.MinAllowed32.Returns32"
            );
            yield return new TestCaseData(100, 100, 256, 256).SetName(
                "MinClamp.100x100.MinAllowed256.Returns256"
            );
            yield return new TestCaseData(512, 512, 1024, 1024).SetName(
                "MinClamp.512x512.MinAllowed1024.Returns1024"
            );
            // Test when computed POT is already above min
            yield return new TestCaseData(300, 300, 32, 512).SetName(
                "MinClamp.300x300.MinAllowed32.Returns512"
            );
            yield return new TestCaseData(1000, 1000, 32, 1024).SetName(
                "MinClamp.1000x1000.MinAllowed32.Returns1024"
            );
            // Test non-power-of-two min values
            yield return new TestCaseData(1, 1, 50, 50).SetName(
                "MinClamp.1x1.MinAllowed50.Returns50"
            );
            yield return new TestCaseData(1, 1, 100, 100).SetName(
                "MinClamp.1x1.MinAllowed100.Returns100"
            );
        }

        [Test]
        [TestCaseSource(nameof(MaxClampingTestCases))]
        public void MaxClampingAppliesCorrectly(
            int width,
            int height,
            int maxAllowedSize,
            int expectedSize,
            string description
        )
        {
            string path = Path.Combine(Root, $"maxClamp_{width}x{height}_{maxAllowedSize}.png")
                .SanitizePath();
            CreatePng(path, width, height, Color.magenta);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null, "Importer should exist");
            imp.maxTextureSize = 32;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.GrowAndShrink;
            window._minAllowedTextureSize = 1;
            window._maxAllowedTextureSize = maxAllowedSize;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            _ = window.CalculateTextureChanges(true);

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            Assert.That(
                imp.maxTextureSize,
                Is.EqualTo(expectedSize),
                $"With _maxAllowedTextureSize={maxAllowedSize}, {width}x{height} should result in {expectedSize}: {description}"
            );
        }

        private static IEnumerable<TestCaseData> MaxClampingTestCases()
        {
            // Test max clamping when computed size exceeds max
            yield return new TestCaseData(
                2000,
                2000,
                1024,
                1024,
                "Large texture clamped to max"
            ).SetName("MaxClamp.2000x2000.MaxAllowed1024.ClampedTo1024");
            yield return new TestCaseData(
                5000,
                5000,
                2048,
                2048,
                "Very large texture clamped"
            ).SetName("MaxClamp.5000x5000.MaxAllowed2048.ClampedTo2048");
            yield return new TestCaseData(9000, 1, 4096, 4096, "Wide strip clamped").SetName(
                "MaxClamp.9000x1.MaxAllowed4096.ClampedTo4096"
            );
            yield return new TestCaseData(1, 9000, 4096, 4096, "Tall strip clamped").SetName(
                "MaxClamp.1x9000.MaxAllowed4096.ClampedTo4096"
            );

            // Test max clamping when computed size is already under max
            yield return new TestCaseData(
                100,
                100,
                1024,
                128,
                "Small texture unchanged by high max"
            ).SetName("MaxClamp.100x100.MaxAllowed1024.Returns128");
            yield return new TestCaseData(
                256,
                256,
                512,
                256,
                "POT texture unchanged under max"
            ).SetName("MaxClamp.256x256.MaxAllowed512.Returns256");
            yield return new TestCaseData(
                300,
                300,
                1024,
                512,
                "Medium texture unchanged under max"
            ).SetName("MaxClamp.300x300.MaxAllowed1024.Returns512");

            // Test max clamping at exact boundary
            yield return new TestCaseData(512, 512, 512, 512, "Exact POT equals max").SetName(
                "MaxClamp.512x512.MaxAllowed512.Returns512"
            );
            yield return new TestCaseData(1024, 1024, 1024, 1024, "Larger POT equals max").SetName(
                "MaxClamp.1024x1024.MaxAllowed1024.Returns1024"
            );

            // Test non-power-of-two max values
            yield return new TestCaseData(
                500,
                500,
                300,
                300,
                "Non-POT max clamps correctly"
            ).SetName("MaxClamp.500x500.MaxAllowed300.ClampedTo300");
            yield return new TestCaseData(
                2000,
                2000,
                1500,
                1500,
                "Non-POT max clamps large texture"
            ).SetName("MaxClamp.2000x2000.MaxAllowed1500.ClampedTo1500");
        }

        [Test]
        [TestCaseSource(nameof(MinMaxInteractionTestCases))]
        public void MinMaxClampingInteractionWorksCorrectly(
            int width,
            int height,
            int minAllowedSize,
            int maxAllowedSize,
            int expectedSize,
            string description
        )
        {
            string path = Path.Combine(
                    Root,
                    $"minMaxInteraction_{width}x{height}_{minAllowedSize}_{maxAllowedSize}.png"
                )
                .SanitizePath();
            CreatePng(path, width, height, Color.yellow);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null, "Importer should exist");
            imp.maxTextureSize = 2048;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = FitMode.GrowAndShrink;
            window._minAllowedTextureSize = minAllowedSize;
            window._maxAllowedTextureSize = maxAllowedSize;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            _ = window.CalculateTextureChanges(true);

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            Assert.That(
                imp.maxTextureSize,
                Is.EqualTo(expectedSize),
                $"With min={minAllowedSize}, max={maxAllowedSize}, {width}x{height} should result in {expectedSize}: {description}"
            );
        }

        private static IEnumerable<TestCaseData> MinMaxInteractionTestCases()
        {
            // Normal cases where min < computed < max
            yield return new TestCaseData(
                300,
                300,
                128,
                1024,
                512,
                "Computed POT within bounds"
            ).SetName("MinMax.300x300.Min128Max1024.Returns512");
            yield return new TestCaseData(
                500,
                500,
                256,
                2048,
                512,
                "Computed POT within wide bounds"
            ).SetName("MinMax.500x500.Min256Max2048.Returns512");

            // Cases where computed < min (min takes precedence)
            yield return new TestCaseData(
                50,
                50,
                256,
                1024,
                256,
                "Computed below min, clamped to min"
            ).SetName("MinMax.50x50.Min256Max1024.ClampedToMin");
            yield return new TestCaseData(
                1,
                1,
                128,
                512,
                128,
                "Tiny texture clamped to min"
            ).SetName("MinMax.1x1.Min128Max512.ClampedToMin");

            // Cases where computed > max (max takes precedence)
            yield return new TestCaseData(
                2000,
                2000,
                32,
                512,
                512,
                "Computed above max, clamped to max"
            ).SetName("MinMax.2000x2000.Min32Max512.ClampedToMax");
            yield return new TestCaseData(
                4000,
                4000,
                64,
                1024,
                1024,
                "Very large clamped to max"
            ).SetName("MinMax.4000x4000.Min64Max1024.ClampedToMax");

            // Edge case: min equals max (narrow window)
            yield return new TestCaseData(
                100,
                100,
                256,
                256,
                256,
                "Min equals max forces specific size"
            ).SetName("MinMax.100x100.MinEqualsMax256.Returns256");
            yield return new TestCaseData(
                1000,
                1000,
                512,
                512,
                512,
                "Large texture with min equals max"
            ).SetName("MinMax.1000x1000.MinEqualsMax512.Returns512");
            yield return new TestCaseData(
                50,
                50,
                128,
                128,
                128,
                "Small texture with min equals max"
            ).SetName("MinMax.50x50.MinEqualsMax128.Returns128");

            // Cases at exact boundaries
            yield return new TestCaseData(256, 256, 256, 512, 256, "Computed equals min").SetName(
                "MinMax.256x256.ComputedEqualsMin.Returns256"
            );
            yield return new TestCaseData(512, 512, 256, 512, 512, "Computed equals max").SetName(
                "MinMax.512x512.ComputedEqualsMax.Returns512"
            );
        }

        [Test]
        [TestCaseSource(nameof(MaxClampingWithFitModeTestCases))]
        public void MaxClampingWorksWithAllFitModes(
            int width,
            int height,
            FitMode mode,
            int currentMaxSize,
            int maxAllowedSize,
            int expectedSize
        )
        {
            string path = Path.Combine(
                    Root,
                    $"maxClampMode_{width}x{height}_{mode}_{maxAllowedSize}.png"
                )
                .SanitizePath();
            CreatePng(path, width, height, Color.cyan);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null, "Importer should exist");
            imp.maxTextureSize = currentMaxSize;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = mode;
            window._minAllowedTextureSize = 1;
            window._maxAllowedTextureSize = maxAllowedSize;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            _ = window.CalculateTextureChanges(true);

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            Assert.That(
                imp.maxTextureSize,
                Is.EqualTo(expectedSize),
                $"{mode} with max={maxAllowedSize} for {width}x{height} should result in {expectedSize}"
            );
        }

        private static IEnumerable<TestCaseData> MaxClampingWithFitModeTestCases()
        {
            // GrowAndShrink mode with max clamping
            yield return new TestCaseData(
                2000,
                2000,
                FitMode.GrowAndShrink,
                1024,
                512,
                512
            ).SetName("MaxMode.GrowAndShrink.2000x2000.ClampedTo512");
            yield return new TestCaseData(100, 100, FitMode.GrowAndShrink, 512, 64, 64).SetName(
                "MaxMode.GrowAndShrink.100x100.ClampedTo64"
            );

            // GrowOnly mode with max clamping
            yield return new TestCaseData(2000, 2000, FitMode.GrowOnly, 256, 512, 512).SetName(
                "MaxMode.GrowOnly.2000x2000.ClampedTo512"
            );
            yield return new TestCaseData(1000, 1000, FitMode.GrowOnly, 128, 256, 256).SetName(
                "MaxMode.GrowOnly.1000x1000.ClampedTo256"
            );

            // ShrinkOnly mode with max clamping
            yield return new TestCaseData(2000, 2000, FitMode.ShrinkOnly, 4096, 512, 512).SetName(
                "MaxMode.ShrinkOnly.2000x2000.ClampedTo512"
            );
            yield return new TestCaseData(500, 500, FitMode.ShrinkOnly, 2048, 256, 256).SetName(
                "MaxMode.ShrinkOnly.500x500.ClampedTo256"
            );

            // RoundToNearest mode with max clamping
            yield return new TestCaseData(
                2000,
                2000,
                FitMode.RoundToNearest,
                128,
                512,
                512
            ).SetName("MaxMode.RoundToNearest.2000x2000.ClampedTo512");
            yield return new TestCaseData(768, 768, FitMode.RoundToNearest, 256, 512, 512).SetName(
                "MaxMode.RoundToNearest.768x768.ClampedTo512"
            );
        }

        [Test]
        [TestCaseSource(nameof(MinClampingWithFitModeTestCases))]
        public void MinClampingWorksWithAllFitModes(
            int width,
            int height,
            FitMode mode,
            int currentMaxSize,
            int minAllowedSize,
            int expectedSize
        )
        {
            string path = Path.Combine(
                    Root,
                    $"minClampMode_{width}x{height}_{mode}_{minAllowedSize}.png"
                )
                .SanitizePath();
            CreatePng(path, width, height, Color.gray);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null, "Importer should exist");
            imp.maxTextureSize = currentMaxSize;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = mode;
            window._minAllowedTextureSize = minAllowedSize;
            window._maxAllowedTextureSize = 16384;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            _ = window.CalculateTextureChanges(true);

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            Assert.That(
                imp.maxTextureSize,
                Is.EqualTo(expectedSize),
                $"{mode} with min={minAllowedSize} for {width}x{height} should result in {expectedSize}"
            );
        }

        private static IEnumerable<TestCaseData> MinClampingWithFitModeTestCases()
        {
            // GrowAndShrink mode with min clamping
            yield return new TestCaseData(10, 10, FitMode.GrowAndShrink, 1024, 256, 256).SetName(
                "MinMode.GrowAndShrink.10x10.ClampedTo256"
            );
            yield return new TestCaseData(50, 50, FitMode.GrowAndShrink, 512, 128, 128).SetName(
                "MinMode.GrowAndShrink.50x50.ClampedTo128"
            );

            // GrowOnly mode with min clamping
            yield return new TestCaseData(10, 10, FitMode.GrowOnly, 32, 256, 256).SetName(
                "MinMode.GrowOnly.10x10.ClampedTo256"
            );
            yield return new TestCaseData(50, 50, FitMode.GrowOnly, 64, 512, 512).SetName(
                "MinMode.GrowOnly.50x50.ClampedTo512"
            );

            // ShrinkOnly mode with min clamping
            yield return new TestCaseData(10, 10, FitMode.ShrinkOnly, 1024, 256, 256).SetName(
                "MinMode.ShrinkOnly.10x10.ClampedTo256"
            );
            yield return new TestCaseData(50, 50, FitMode.ShrinkOnly, 2048, 128, 128).SetName(
                "MinMode.ShrinkOnly.50x50.ClampedTo128"
            );

            // RoundToNearest mode with min clamping
            yield return new TestCaseData(10, 10, FitMode.RoundToNearest, 512, 256, 256).SetName(
                "MinMode.RoundToNearest.10x10.ClampedTo256"
            );
            yield return new TestCaseData(30, 30, FitMode.RoundToNearest, 128, 64, 64).SetName(
                "MinMode.RoundToNearest.30x30.ClampedTo64"
            );
        }

        [Test]
        [TestCaseSource(nameof(AspectRatioTestCases))]
        public void AspectRatiosAreHandledCorrectly(
            int width,
            int height,
            FitMode mode,
            int expectedSize
        )
        {
            string path = Path.Combine(Root, $"aspect_{width}x{height}_{mode}.png").SanitizePath();
            CreatePng(path, width, height, Color.magenta);
            AssetDatabaseBatchHelper.RefreshIfNotBatching();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null, "Importer should exist");
            imp.maxTextureSize = 128;
            imp.SaveAndReimport();

            FitTextureSizeWindow window = Track(
                ScriptableObject.CreateInstance<FitTextureSizeWindow>()
            );
            window._fitMode = mode;
            window._textureSourcePaths = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };

            _ = window.CalculateTextureChanges(true);

            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            Assert.That(
                imp.maxTextureSize,
                Is.EqualTo(expectedSize),
                $"Aspect ratio test {mode} for {width}x{height} should be {expectedSize}"
            );
        }

        private static IEnumerable<TestCaseData> AspectRatioTestCases()
        {
            // GrowAndShrink mode
            yield return new TestCaseData(1920, 1080, FitMode.GrowAndShrink, 2048).SetName(
                "Aspect.1920x1080.GrowAndShrink.HD"
            );
            yield return new TestCaseData(1080, 1920, FitMode.GrowAndShrink, 2048).SetName(
                "Aspect.1080x1920.GrowAndShrink.PortraitHD"
            );
            yield return new TestCaseData(800, 600, FitMode.GrowAndShrink, 1024).SetName(
                "Aspect.800x600.GrowAndShrink.4By3"
            );
            yield return new TestCaseData(600, 800, FitMode.GrowAndShrink, 1024).SetName(
                "Aspect.600x800.GrowAndShrink.Portrait4By3"
            );
            yield return new TestCaseData(1280, 720, FitMode.GrowAndShrink, 2048).SetName(
                "Aspect.1280x720.GrowAndShrink.720p"
            );
            yield return new TestCaseData(720, 1280, FitMode.GrowAndShrink, 2048).SetName(
                "Aspect.720x1280.GrowAndShrink.Portrait720p"
            );
            yield return new TestCaseData(2560, 1440, FitMode.GrowAndShrink, 4096).SetName(
                "Aspect.2560x1440.GrowAndShrink.1440p"
            );
            yield return new TestCaseData(1440, 2560, FitMode.GrowAndShrink, 4096).SetName(
                "Aspect.1440x2560.GrowAndShrink.Portrait1440p"
            );
            yield return new TestCaseData(512, 64, FitMode.GrowAndShrink, 512).SetName(
                "Aspect.512x64.GrowAndShrink.8To1"
            );
            yield return new TestCaseData(64, 512, FitMode.GrowAndShrink, 512).SetName(
                "Aspect.64x512.GrowAndShrink.1To8"
            );
            // GrowOnly mode with common aspect ratios
            yield return new TestCaseData(1920, 1080, FitMode.GrowOnly, 2048).SetName(
                "Aspect.1920x1080.GrowOnly.HD"
            );
            yield return new TestCaseData(800, 600, FitMode.GrowOnly, 1024).SetName(
                "Aspect.800x600.GrowOnly.4By3"
            );
            yield return new TestCaseData(1280, 720, FitMode.GrowOnly, 2048).SetName(
                "Aspect.1280x720.GrowOnly.720p"
            );
            // RoundToNearest mode with common aspect ratios
            yield return new TestCaseData(1920, 1080, FitMode.RoundToNearest, 2048).SetName(
                "Aspect.1920x1080.RoundToNearest.HD"
            );
            yield return new TestCaseData(800, 600, FitMode.RoundToNearest, 1024).SetName(
                "Aspect.800x600.RoundToNearest.4By3"
            );
            yield return new TestCaseData(1280, 720, FitMode.RoundToNearest, 1024).SetName(
                "Aspect.1280x720.RoundToNearest.720pCloserTo1024"
            );
            // Ultra-wide aspect ratios
            yield return new TestCaseData(2560, 1080, FitMode.GrowAndShrink, 4096).SetName(
                "Aspect.2560x1080.GrowAndShrink.UltraWide"
            );
            yield return new TestCaseData(3440, 1440, FitMode.GrowAndShrink, 4096).SetName(
                "Aspect.3440x1440.GrowAndShrink.UltraWide34"
            );
        }

        private void CreatePng(string relPath, int w, int h, Color c)
        {
            string dir = Path.GetDirectoryName(relPath).SanitizePath();
            EnsureFolder(dir);
            Texture2D t = new(w, h, TextureFormat.RGBA32, false);
            try
            {
                Color[] pix = new Color[w * h];
                for (int i = 0; i < pix.Length; i++)
                {
                    pix[i] = c;
                }

                t.SetPixels(pix);
                t.Apply();
                byte[] data = t.EncodeToPNG();
                File.WriteAllBytes(RelToFull(relPath), data);
                TrackAssetPath(relPath);
            }
            finally
            {
                Object.DestroyImmediate(t); // UNH-SUPPRESS: Cleanup temporary texture in finally block
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
                .SanitizePath();
        }
    }
#endif
}
