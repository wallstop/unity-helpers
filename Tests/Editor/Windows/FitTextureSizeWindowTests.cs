namespace WallstopStudios.UnityHelpers.Tests.Windows
{
#if UNITY_EDITOR
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor;
    using WallstopStudios.UnityHelpers.Tests.Core;

    public sealed class FitTextureSizeWindowTests : CommonTestBase
    {
        private const string Root = "Assets/Temp/FitTextureSizeTests";

        [SetUp]
        public override void BaseSetUp()
        {
            base.BaseSetUp();
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
            AssetDatabase.Refresh();

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
            AssetDatabase.Refresh();

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
            Assert.That(
                imp.maxTextureSize,
                Is.EqualTo(256),
                "Max size should shrink to tight POT above size"
            );
        }

        [Test]
        public void ShrinkOnlyKeepsExactPowerOfTwo()
        {
            string path = Path.Combine(Root, "shrinkExact.png").SanitizePath();
            CreatePng(path, 256, 128, Color.yellow);
            AssetDatabase.Refresh();

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
            AssetDatabase.Refresh();

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
            Assert.That(imp.maxTextureSize, Is.EqualTo(256), "Should shrink to 256");
        }

        [Test]
        public void GrowOnlyDoesNotShrinkWhenAlreadyLarge()
        {
            string path = Path.Combine(Root, "growNoChange.png").SanitizePath();
            CreatePng(path, 300, 100, Color.white);
            AssetDatabase.Refresh();

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
            AssetDatabase.Refresh();

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
            AssetDatabase.Refresh();

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
            AssetDatabase.Refresh();

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
            AssetDatabase.Refresh();

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
            Assert.That(spriteImp.maxTextureSize, Is.EqualTo(256));
            Assert.That(texImp.maxTextureSize, Is.EqualTo(1024));
        }

        [Test]
        public void NameFilterContainsOnlyMatches()
        {
            string heroPath = Path.Combine(Root, "hero_idle.png").SanitizePath();
            string villPath = Path.Combine(Root, "villain_idle.png").SanitizePath();
            CreatePng(heroPath, 300, 100, Color.white);
            CreatePng(villPath, 300, 100, Color.white);
            AssetDatabase.Refresh();

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
            AssetDatabase.Refresh();

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
            AssetDatabase.Refresh();

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
            AssetDatabase.Refresh();

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
            AssetDatabase.Refresh();

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
            AssetDatabase.Refresh();

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
            AssetDatabase.Refresh();

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
            AssetDatabase.Refresh();

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
            AssetDatabase.Refresh();

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
            AssetDatabase.Refresh();

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
            AssetDatabase.Refresh();

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
            AssetDatabase.Refresh();

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
                Object.DestroyImmediate(t);
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
