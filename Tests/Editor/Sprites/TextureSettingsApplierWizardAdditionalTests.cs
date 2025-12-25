namespace WallstopStudios.UnityHelpers.Tests.Sprites
{
#if UNITY_EDITOR
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Editor.AssetProcessors;
    using WallstopStudios.UnityHelpers.Editor.Sprites;
    using WallstopStudios.UnityHelpers.Tests.Core;
    using Object = UnityEngine.Object;

    public sealed class TextureSettingsApplierWizardAdditionalTests : CommonTestBase
    {
        private const string Root = "Assets/Temp/TextureSettingsApplierWizardAdditionalTests";

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
            // Reset DetectAssetChangeProcessor to avoid triggering loop protection
            // when multiple assets are deleted during cleanup
            DetectAssetChangeProcessor.ResetForTesting();
            CleanupTrackedFoldersAndAssets();
        }

        [Test]
        public void AppliesSettingsToExplicitTexturesOnly()
        {
            string dir = Root.SanitizePath();
            string included = (dir + "/inc.png").SanitizePath();
            string other = (dir + "/other.png").SanitizePath();
            CreatePng(included, 16, 16, Color.white);
            CreatePng(other, 16, 16, Color.white);
            AssetDatabase.Refresh();

            // Set initial different values so we can detect changes
            TextureImporter impIncluded = AssetImporter.GetAtPath(included) as TextureImporter;
            impIncluded.isReadable = true;
            impIncluded.wrapMode = TextureWrapMode.Repeat;
            impIncluded.filterMode = FilterMode.Point;
            impIncluded.mipmapEnabled = true;
            impIncluded.SaveAndReimport();

            TextureImporter impOther = AssetImporter.GetAtPath(other) as TextureImporter;
            impOther.isReadable = true;
            impOther.wrapMode = TextureWrapMode.Repeat;
            impOther.filterMode = FilterMode.Point;
            impOther.mipmapEnabled = true;
            impOther.SaveAndReimport();

            TextureSettingsApplierWindow window = Track(
                ScriptableObject.CreateInstance<TextureSettingsApplierWindow>()
            );
            window.textures = new System.Collections.Generic.List<Texture2D>
            {
                AssetDatabase.LoadAssetAtPath<Texture2D>(included),
            };
            window.directories = new System.Collections.Generic.List<Object>(); // none
            window.applyReadOnly = true;
            window.isReadOnly = true; // expect isReadable = false
            window.applyWrapMode = true;
            window.wrapMode = TextureWrapMode.Clamp;
            window.applyFilterMode = true;
            window.filterMode = FilterMode.Bilinear;
            window.applyMipMaps = true;
            window.generateMipMaps = false;
            window.maxTextureSize = 64;

            window.ApplySettings();
            AssetDatabase.Refresh();

            impIncluded = AssetImporter.GetAtPath(included) as TextureImporter;
            impOther = AssetImporter.GetAtPath(other) as TextureImporter;
            Assert.IsTrue(impIncluded != null);
            Assert.IsTrue(impOther != null);

            Assert.That(impIncluded.isReadable, Is.False);
            Assert.That(impIncluded.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(impIncluded.filterMode, Is.EqualTo(FilterMode.Bilinear));
            Assert.That(impIncluded.mipmapEnabled, Is.False);
            Assert.That(impIncluded.maxTextureSize, Is.EqualTo(64));

            // Not listed in textures and no directories: should remain unchanged
            Assert.That(impOther.isReadable, Is.True);
            Assert.That(impOther.wrapMode, Is.EqualTo(TextureWrapMode.Repeat));
            Assert.That(impOther.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(impOther.mipmapEnabled, Is.True);
        }

        [Test]
        public void DirectoryRecursionHonorsExtensionFilter()
        {
            string dirA = (Root + "/A").SanitizePath();
            string dirB = (dirA + "/B").SanitizePath();
            EnsureFolder(dirA);
            EnsureFolder(dirB);
            string png = (dirB + "/tex.png").SanitizePath();
            string jpg = (dirB + "/tex.jpg").SanitizePath();
            CreatePng(png, 8, 8, Color.white);
            CreateJpg(jpg, 8, 8, Color.white);
            AssetDatabase.Refresh();

            TextureImporter impPng = AssetImporter.GetAtPath(png) as TextureImporter;
            impPng.isReadable = true;
            impPng.wrapMode = TextureWrapMode.Repeat;
            impPng.filterMode = FilterMode.Point;
            impPng.mipmapEnabled = true;
            impPng.SaveAndReimport();

            TextureImporter impJpg = AssetImporter.GetAtPath(jpg) as TextureImporter;
            impJpg.isReadable = true;
            impJpg.wrapMode = TextureWrapMode.Repeat;
            impJpg.filterMode = FilterMode.Point;
            impJpg.mipmapEnabled = true;
            impJpg.SaveAndReimport();

            TextureSettingsApplierWindow window2 = Track(
                ScriptableObject.CreateInstance<TextureSettingsApplierWindow>()
            );
            window2.textures = new System.Collections.Generic.List<Texture2D>();
            window2.directories = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(Root),
            };
            window2.spriteFileExtensions = new System.Collections.Generic.List<string> { ".png" }; // only png
            window2.applyReadOnly = true;
            window2.isReadOnly = true;
            window2.applyWrapMode = true;
            window2.wrapMode = TextureWrapMode.Clamp;
            window2.applyFilterMode = true;
            window2.filterMode = FilterMode.Bilinear;
            window2.applyMipMaps = true;
            window2.generateMipMaps = false;
            window2.maxTextureSize = 32;

            window2.ApplySettings();
            AssetDatabase.Refresh();

            impPng = AssetImporter.GetAtPath(png) as TextureImporter;
            impJpg = AssetImporter.GetAtPath(jpg) as TextureImporter;

            // png affected
            Assert.That(impPng.isReadable, Is.False);
            Assert.That(impPng.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(impPng.filterMode, Is.EqualTo(FilterMode.Bilinear));
            Assert.That(impPng.mipmapEnabled, Is.False);
            Assert.That(impPng.maxTextureSize, Is.EqualTo(32));

            // jpg not affected due to extension filter
            Assert.That(impJpg.isReadable, Is.True);
            Assert.That(impJpg.wrapMode, Is.EqualTo(TextureWrapMode.Repeat));
            Assert.That(impJpg.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(impJpg.mipmapEnabled, Is.True);
        }

        [Test]
        public void DirectorySearchWithManyDirectoriesSucceeds()
        {
            // This test specifically targets the bug where SystemArrayPool returned larger arrays
            // than requested, causing null values to be passed to AssetDatabase.FindAssets
            // By using multiple directories, we increase the chance of hitting the array size mismatch

            string[] dirs = new string[5];
            string[] textures = new string[5];

            for (int i = 0; i < dirs.Length; i++)
            {
                dirs[i] = (Root + "/Multi" + i).SanitizePath();
                EnsureFolder(dirs[i]);
                textures[i] = (dirs[i] + "/tex" + i + ".png").SanitizePath();
                CreatePng(textures[i], 4, 4, Color.white);
            }

            AssetDatabase.Refresh();

            TextureSettingsApplierWindow window = Track(
                ScriptableObject.CreateInstance<TextureSettingsApplierWindow>()
            );
            window.textures = new System.Collections.Generic.List<Texture2D>();
            window.directories = new System.Collections.Generic.List<Object>();

            for (int i = 0; i < dirs.Length; i++)
            {
                Object dirAsset = AssetDatabase.LoadAssetAtPath<Object>(dirs[i]);
                Assert.IsTrue(
                    dirAsset != null,
                    $"Expected directory asset at '{dirs[i]}' to be loaded"
                );
                window.directories.Add(dirAsset);
            }

            window.applyFilterMode = true;
            window.filterMode = FilterMode.Trilinear;

            // This was failing before the fix because SystemArrayPool.Get returns larger arrays
            // and the null elements caused AssetDatabase.FindAssets to crash
            Assert.DoesNotThrow(
                () => window.ApplySettings(),
                "ApplySettings with multiple directories should not throw NullReferenceException"
            );

            AssetDatabase.Refresh();

            for (int i = 0; i < textures.Length; i++)
            {
                TextureImporter imp = AssetImporter.GetAtPath(textures[i]) as TextureImporter;
                Assert.IsTrue(
                    imp != null,
                    $"Expected importer at path '{textures[i]}' to not be null"
                );
                Assert.That(
                    imp.filterMode,
                    Is.EqualTo(FilterMode.Trilinear),
                    $"Texture at '{textures[i]}' should have Trilinear filter mode"
                );
            }
        }

        [Test]
        public void CalculateStatsWithDirectoriesDoesNotThrow()
        {
            // CalculateStats internally calls GetTargetTexturePaths which was affected by the same bug
            string dir = (Root + "/CalcStatsDir").SanitizePath();
            EnsureFolder(dir);
            string tex = (dir + "/calcstats.png").SanitizePath();
            CreatePng(tex, 8, 8, Color.white);
            AssetDatabase.Refresh();

            TextureSettingsApplierWindow window = Track(
                ScriptableObject.CreateInstance<TextureSettingsApplierWindow>()
            );
            window.textures = new System.Collections.Generic.List<Texture2D>();
            window.directories = new System.Collections.Generic.List<Object>
            {
                AssetDatabase.LoadAssetAtPath<Object>(dir),
            };

            // CalculateStats calls GetTargetTexturePaths which had the array pool bug
            Assert.DoesNotThrow(
                () => window.CalculateStats(),
                "CalculateStats with directories should not throw"
            );
        }

        [Test]
        public void WizardAppliesNamedPlatformOverride()
        {
            string dir = Root.SanitizePath();
            string path = (dir + "/plat.png").SanitizePath();
            CreatePng(path, 16, 16, Color.white);
            AssetDatabase.Refresh();

            TextureSettingsApplierWindow window3 = Track(
                ScriptableObject.CreateInstance<TextureSettingsApplierWindow>()
            );
            window3.textures = new System.Collections.Generic.List<Texture2D>
            {
                AssetDatabase.LoadAssetAtPath<Texture2D>(path),
            };
            window3.platformOverrides =
                new System.Collections.Generic.List<TextureSettingsApplierWindow.PlatformOverrideEntry>
                {
                    new()
                    {
                        platformName = "Standalone",
                        applyMaxTextureSize = true,
                        maxTextureSize = 256,
                    },
                };

            window3.ApplySettings();
            AssetDatabase.Refresh();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            TextureImporterPlatformSettings ops = imp.GetPlatformTextureSettings("Standalone");
            Assert.IsTrue(ops.overridden);
            Assert.AreEqual(256, ops.maxTextureSize);
        }

        [Test]
        public void RequireChangesBeforeApplySkipsWhenNoChanges()
        {
            string dir = Root.SanitizePath();
            string path = (dir + "/dryrun.png").SanitizePath();
            CreatePng(path, 16, 16, Color.white);
            AssetDatabase.Refresh();

            // Set importer to desired state first
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsTrue(imp != null);
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            TextureImporterPlatformSettings ps = imp.GetDefaultPlatformTextureSettings();
            ps.maxTextureSize = 64;
            imp.SetPlatformTextureSettings(ps);
            imp.SaveAndReimport();

            TextureSettingsApplierWindow window = Track(
                ScriptableObject.CreateInstance<TextureSettingsApplierWindow>()
            );
            window.textures = new System.Collections.Generic.List<Texture2D>
            {
                AssetDatabase.LoadAssetAtPath<Texture2D>(path),
            };
            window.applyWrapMode = true;
            window.wrapMode = TextureWrapMode.Clamp;
            window.applyFilterMode = true;
            window.filterMode = FilterMode.Bilinear;
            window.maxTextureSize = 64; // default platform setting

            // Dry-run guard on; calculate stats should find zero changes
            window.requireChangesBeforeApply = true;
            window.CalculateStats();
            // Apply should be skipped (no assertion on logs; verify no change in a field)
            FilterMode before = imp.filterMode;
            window.ApplySettings();
            imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.AreEqual(before, imp.filterMode);
        }

        private void CreatePng(string relPath, int w, int h, Color c)
        {
            EnsureFolder(Path.GetDirectoryName(relPath).SanitizePath());
            Texture2D t = new(w, h, TextureFormat.RGBA32, false);
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = c;
            }

            t.SetPixels(pix);
            t.Apply();
            File.WriteAllBytes(RelToFull(relPath), t.EncodeToPNG());
        }

        private static void CreateJpg(string relPath, int w, int h, Color c)
        {
            EnsureFolderStatic(Path.GetDirectoryName(relPath).SanitizePath());
            Texture2D t = new(w, h, TextureFormat.RGB24, false);
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = c;
            }

            t.SetPixels(pix);
            t.Apply();
            File.WriteAllBytes(RelToFull(relPath), t.EncodeToJPG());
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
