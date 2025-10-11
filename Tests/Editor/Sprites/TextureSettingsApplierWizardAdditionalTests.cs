namespace WallstopStudios.UnityHelpers.Tests.Editor.Sprites
{
#if UNITY_EDITOR
    using System.IO;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Editor.Sprites;
    using WallstopStudios.UnityHelpers.Tests.Utils;
    using Object = UnityEngine.Object;

    public sealed class TextureSettingsApplierWizardAdditionalTests : CommonTestBase
    {
        private const string Root = "Assets/Temp/TextureSettingsApplierWizardAdditionalTests";

        [SetUp]
        public void SetUp()
        {
            EnsureFolder(Root);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            AssetDatabase.DeleteAsset("Assets/Temp");
            AssetDatabase.Refresh();
        }

        [Test]
        public void AppliesSettingsToExplicitTexturesOnly()
        {
            string dir = Root.Replace('\\', '/');
            string included = (dir + "/inc.png").Replace('\\', '/');
            string other = (dir + "/other.png").Replace('\\', '/');
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
            Assert.IsNotNull(impIncluded);
            Assert.IsNotNull(impOther);

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
            string dirA = (Root + "/A").Replace('\\', '/');
            string dirB = (dirA + "/B").Replace('\\', '/');
            EnsureFolder(dirA);
            EnsureFolder(dirB);
            string png = (dirB + "/tex.png").Replace('\\', '/');
            string jpg = (dirB + "/tex.jpg").Replace('\\', '/');
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
        public void WizardAppliesNamedPlatformOverride()
        {
            string dir = Root.Replace('\\', '/');
            string path = (dir + "/plat.png").Replace('\\', '/');
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
                    new TextureSettingsApplierWindow.PlatformOverrideEntry
                    {
                        platformName = "Standalone",
                        applyMaxTextureSize = true,
                        maxTextureSize = 256,
                    },
                };

            window3.ApplySettings();
            AssetDatabase.Refresh();

            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(imp);
            TextureImporterPlatformSettings ops = imp.GetPlatformTextureSettings("Standalone");
            Assert.IsTrue(ops.overridden);
            Assert.AreEqual(256, ops.maxTextureSize);
        }

        private static void EnsureFolder(string relPath)
        {
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

        private static void CreateJpg(string relPath, int w, int h, Color c)
        {
            EnsureFolder(Path.GetDirectoryName(relPath).Replace('\\', '/'));
            Texture2D t = new(w, h, TextureFormat.RGB24, false);
            Color[] pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = c;
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
                .Replace('\\', '/');
        }
    }
#endif
}
